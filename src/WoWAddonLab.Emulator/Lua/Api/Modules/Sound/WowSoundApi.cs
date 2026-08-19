using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSoundApi : LuaApiModule
{
    private const string PlaySoundUsage =
        "Usage: local success, soundHandle = C_Sound.PlaySound(params)";
    private const string PlaySoundFileUsage =
        "Usage: PlaySoundFile(\"soundFile\" or soundFileID, " +
        "optional[\"SFX\",\"Music\",\"Ambience\" or \"Master\"])";
    private const string StopSoundUsage =
        "Usage: StopSound(soundHandleID, [optional: fadeout time in ms])";
    private const string MuteSoundFileUsage =
        "Usage: MuteSoundFile(\"soundFile\" or fileDataID)";
    private const string UnmuteSoundFileUsage =
        "Usage: UnmuteSoundFile(\"soundFile\" or fileDataID)";
    private const string GetScaledVolumeUsage =
        "Usage: local scaledVolume = C_Sound.GetSoundScaledVolume(soundHandle)";
    private const string IsPlayingUsage =
        "Usage: local isPlaying = C_Sound.IsPlaying(soundHandle)";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] NamespaceFunctions =
    [
        "GetSoundScaledVolume",
        "IsPlaying",
        "PlaySound"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in NamespaceFunctions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Sound");

        foreach (var function in new[]
                 {
                     "MuteSoundFile",
                     "PlaySound",
                     "PlaySoundFile",
                     "StopSound",
                     "UnmuteSoundFile"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var sound = runtime.Sound;
        switch (lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty)
        {
            case "GetSoundScaledVolume":
            {
                var handle = RequiredUInt32(state, 1, GetScaledVolumeUsage);
                var volume = handle <= int.MaxValue &&
                             sound.ActivePlaybacks.TryGetValue((int)handle, out var playback)
                    ? playback.ScaledVolume
                    : 0;
                lua_pushnumber(state, volume);
                return 1;
            }
            case "IsPlaying":
            {
                var handle = RequiredUInt32(state, 1, IsPlayingUsage);
                lua_pushboolean(
                    state,
                    handle <= int.MaxValue && sound.ActivePlaybacks.ContainsKey((int)handle)
                        ? 1
                        : 0);
                return 1;
            }
            case "PlaySound":
                return PlaySound(runtime);
            case "PlaySoundFile":
                return PlaySoundFile(runtime);
            case "MuteSoundFile":
                runtime.Sound.MutedFileDataIds.Add(ResolveSoundFileDataId(
                    runtime,
                    MuteSoundFileUsage,
                    "MuteSoundFile Error - Invalid fileDataID for sound."));
                return 0;
            case "StopSound":
                return StopSound(runtime);
            case "UnmuteSoundFile":
                runtime.Sound.MutedFileDataIds.Remove(ResolveSoundFileDataId(
                    runtime,
                    UnmuteSoundFileUsage,
                    "UnmuteSoundFile Error - Invalid fileDataID for sound."));
                return 0;
            default:
                return 0;
        }
    }

    private static int PlaySound(LuaRuntime runtime)
    {
        var state = runtime.State;
        var soundKitId = RequiredInt32(state, 1, PlaySoundUsage);
        var uiSoundSubType = OptionalUiSoundSubType(state, 2, PlaySoundUsage);
        var forceNoDuplicates = OptionalBoolean(state, 3);
        var runFinishCallback = OptionalBoolean(state, 4);
        var overridePriority = OptionalInt32(state, 5, PlaySoundUsage);

        var sound = runtime.Sound;
        if (!CanPlay(sound) || soundKitId <= 0 ||
            sound.UnavailableSoundKitIds.Contains(soundKitId) ||
            sound.SoundKitExists?.Invoke(soundKitId) == false)
        {
            return 0;
        }

        var playback = new WowSoundPlayback
        {
            Handle = checked((int)runtime.NextSoundHandle()),
            SourceKind = WowSoundSourceKind.SoundKit,
            SoundKitId = soundKitId,
            UiSoundSubType = uiSoundSubType,
            Channel = UiSoundSubTypeName(uiSoundSubType),
            ForceNoDuplicates = forceNoDuplicates,
            RunFinishCallback = runFinishCallback,
            OverridePriority = overridePriority
        };
        RecordPlayback(sound, playback);
        PushAcceptedPlayback(state, playback.Handle);
        return 2;
    }

    private static int PlaySoundFile(LuaRuntime runtime)
    {
        var state = runtime.State;
        uint? fileDataId = null;
        string? filePath = null;
        WowSoundSourceKind sourceKind;

        if (lua_isnumber(state, 1) != 0)
        {
            fileDataId = unchecked((uint)RequiredInt32(state, 1, PlaySoundFileUsage));
            sourceKind = WowSoundSourceKind.FileDataId;
        }
        else if (lua_isstring(state, 1) != 0)
        {
            filePath = lua_tostring(state, 1) ?? string.Empty;
            sourceKind = WowSoundSourceKind.FilePath;
        }
        else
        {
            return luaL_error(state, PlaySoundFileUsage);
        }

        var channel = "SFX";
        if (lua_isstring(state, 2) != 0)
            channel = NormalizeChannel(lua_tostring(state, 2));

        var sound = runtime.Sound;
        if (!CanPlay(sound))
            return 0;

        if (fileDataId is { } numericId)
        {
            if (numericId == 0 || sound.UnavailableFileDataIds.Contains(numericId))
                return 0;
        }
        else
        {
            if (string.IsNullOrEmpty(filePath) ||
                sound.UnavailableFilePaths.Contains(filePath))
            {
                return 0;
            }

            if (sound.ResolveFileDataId is { } resolver)
            {
                var resolved = resolver(filePath);
                if (resolved == 0)
                    return 0;
                fileDataId = resolved;
            }
        }

        var playback = new WowSoundPlayback
        {
            Handle = checked((int)runtime.NextSoundHandle()),
            SourceKind = sourceKind,
            FileDataId = fileDataId,
            FilePath = filePath,
            UiSoundSubType = UiSoundSubType(channel),
            Channel = channel
        };
        RecordPlayback(sound, playback);
        PushAcceptedPlayback(state, playback.Handle);
        return 2;
    }

    private static int StopSound(LuaRuntime runtime)
    {
        var state = runtime.State;
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, StopSoundUsage);

        var handle = unchecked((int)lua_tonumber(state, 1));
        int? fadeoutMilliseconds = null;
        if (lua_isnumber(state, 2) != 0)
            fadeoutMilliseconds = unchecked((int)lua_tonumber(state, 2));

        runtime.Sound.StopRequests.Add(
            new WowSoundStopRequest(handle, fadeoutMilliseconds));
        runtime.Sound.ActivePlaybacks.Remove(handle);
        return 0;
    }

    private static uint ResolveSoundFileDataId(
        LuaRuntime runtime,
        string usage,
        string invalidFileError)
    {
        var state = runtime.State;
        uint fileDataId;
        if (lua_isnumber(state, 1) != 0)
        {
            fileDataId = unchecked((uint)RequiredInt32(state, 1, usage));
        }
        else
        {
            if (lua_isstring(state, 1) == 0)
                return unchecked((uint)luaL_error(state, usage));
            var path = lua_tostring(state, 1) ?? string.Empty;
            fileDataId = runtime.Sound.ResolveFileDataId?.Invoke(path) ?? 0;
        }

        if (fileDataId == 0)
            return unchecked((uint)luaL_error(state, invalidFileError));
        return fileDataId;
    }

    private static bool CanPlay(WowSoundState sound) =>
        sound.Available && !sound.PlaybackSuppressed;

    private static void RecordPlayback(
        WowSoundState sound,
        WowSoundPlayback playback)
    {
        sound.PlaybackRequests.Add(playback);
        sound.ActivePlaybacks[playback.Handle] = playback;
    }

    private static void PushAcceptedPlayback(lua_State state, int handle)
    {
        lua_pushboolean(state, 1);
        lua_pushnumber(state, handle);
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static uint RequiredUInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((uint)luaL_error(state, usage));
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            return unchecked((uint)luaL_error(state, usage));
        return unchecked((uint)value);
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnoneornil(state, index) != 0)
            return null;
        return RequiredInt32(state, index, usage);
    }

    private static bool OptionalBoolean(lua_State state, int index) =>
        lua_isnoneornil(state, index) == 0 && lua_toboolean(state, index) != 0;

    private static int OptionalUiSoundSubType(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnoneornil(state, index) != 0)
            return 3;
        if (lua_type(state, index) != LUA_TSTRING)
            return luaL_error(state, usage);
        return UiSoundSubType(NormalizeChannel(lua_tostring(state, index)));
    }

    private static string NormalizeChannel(string? value) =>
        value switch
        {
            { } name when name.Equals("Ambience", StringComparison.OrdinalIgnoreCase) =>
                "Ambience",
            { } name when name.Equals("Dialog", StringComparison.OrdinalIgnoreCase) =>
                "Dialog",
            { } name when name.Equals("Master", StringComparison.OrdinalIgnoreCase) =>
                "Master",
            { } name when name.Equals("Music", StringComparison.OrdinalIgnoreCase) =>
                "Music",
            { } name when name.Equals("Talking Head", StringComparison.OrdinalIgnoreCase) =>
                "Talking Head",
            _ => "SFX"
        };

    private static int UiSoundSubType(string channel) => channel switch
    {
        "Ambience" => 4,
        "Dialog" => 5,
        "Master" => 6,
        "Music" => 7,
        "Talking Head" => 8,
        _ => 3
    };

    private static string UiSoundSubTypeName(int value) => value switch
    {
        4 => "Ambience",
        5 => "Dialog",
        6 => "Master",
        7 => "Music",
        8 => "Talking Head",
        _ => "SFX"
    };
}
