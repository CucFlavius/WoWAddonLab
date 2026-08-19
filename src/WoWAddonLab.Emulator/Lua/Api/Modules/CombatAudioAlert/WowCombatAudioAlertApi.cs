using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCombatAudioAlertApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "AddToKnownTargetingList", "GetCategoryVoice", "GetCategoryVolume",
        "GetFormatSetting", "GetSpeakerSpeed", "GetSpecSetting", "GetThrottle", "IsEnabled",
        "RemoveFromKnownTargetingList", "SetCategoryVoice", "SetCategoryVolume",
        "SetFormatSetting", "SetSpeakerSpeed", "SetSpecSetting", "SetThrottle", "SpeakText"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_CombatAudioAlert");
    }

    private static int Dispatch(lua_State state)
    {
        var audio = LuaBindings.GetRuntime(state).CombatAudioAlerts;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AddToKnownTargetingList":
            {
                const string usage =
                    "Usage: local added = C_CombatAudioAlert.AddToKnownTargetingList(unit)";
                var unit = RequiredStringValue(state, 1, usage);
                var added = LuaBindings.IsRecognizedUnitToken(unit) &&
                            audio.KnownTargetingUnits.Add(unit.ToLowerInvariant());
                return PushBoolean(state, added);
            }
            case "GetCategoryVoice":
            {
                const string usage =
                    "Usage: local voice = C_CombatAudioAlert.GetCategoryVoice(category)";
                var category = RequiredByteEnum(state, 1, 8, usage);
                lua_pushnumber(
                    state,
                    audio.Available ? GetCategoryVoice(audio, category) : 0);
                return 1;
            }
            case "GetCategoryVolume":
            {
                const string usage =
                    "Usage: local volume = C_CombatAudioAlert.GetCategoryVolume(category)";
                var category = RequiredByteEnum(state, 1, 8, usage);
                lua_pushnumber(
                    state,
                    audio.Available ? GetCategoryVolume(audio, category) : 0);
                return 1;
            }
            case "GetFormatSetting":
            {
                const string usage =
                    "Usage: local formatVal = C_CombatAudioAlert.GetFormatSetting(unit, alertType)";
                var unit = RequiredByteEnum(state, 1, 1, usage);
                var alertType = RequiredByteEnum(state, 2, 1, usage);
                lua_pushnumber(
                    state,
                    audio.Available
                        ? Get(audio.FormatSettings, (unit, alertType))
                        : 0);
                return 1;
            }
            case "GetSpeakerSpeed":
                lua_pushnumber(state, audio.Available ? audio.SpeakerSpeed : 0);
                return 1;
            case "GetSpecSetting":
            {
                const string usage =
                    "Usage: local value = C_CombatAudioAlert.GetSpecSetting(setting)";
                var setting = RequiredByteEnum(state, 1, 8, usage);
                lua_pushnumber(
                    state,
                    audio.Available ? GetSpecSetting(audio, setting) : 0);
                return 1;
            }
            case "GetThrottle":
            {
                const string usage =
                    "Usage: local throttle = C_CombatAudioAlert.GetThrottle(throttleType)";
                var throttleType = RequiredByteEnum(state, 1, 10, usage);
                lua_pushnumber(state, GetThrottle(audio, throttleType));
                return 1;
            }
            case "IsEnabled":
                lua_pushboolean(state, audio.Available && audio.Enabled ? 1 : 0);
                return 1;
            case "RemoveFromKnownTargetingList":
            {
                const string usage =
                    "Usage: local removed = C_CombatAudioAlert.RemoveFromKnownTargetingList(unit)";
                var unit = RequiredStringValue(state, 1, usage);
                var removed = LuaBindings.IsRecognizedUnitToken(unit) &&
                              audio.KnownTargetingUnits.Remove(unit);
                return PushBoolean(state, removed);
            }
            case "SetCategoryVoice":
            {
                const string usage =
                    "Usage: local success = C_CombatAudioAlert.SetCategoryVoice(category, newVal)";
                var category = RequiredByteEnum(state, 1, 8, usage);
                var value = RequiredUInt32(state, 2, usage);
                var success = audio.Available && value < audio.VoiceCount;
                if (success)
                    SetCategoryVoice(audio, category, unchecked((int)value));
                return PushBoolean(state, success);
            }
            case "SetCategoryVolume":
            {
                const string usage =
                    "Usage: local success = C_CombatAudioAlert.SetCategoryVolume(category, newVal)";
                var category = RequiredByteEnum(state, 1, 8, usage);
                var value = RequiredUInt32(state, 2, usage);
                var success = audio.Available && value <= 100;
                if (success)
                    SetCategoryVolume(audio, category, unchecked((int)value));
                return PushBoolean(state, success);
            }
            case "SetFormatSetting":
            {
                const string usage =
                    "Usage: local success = C_CombatAudioAlert.SetFormatSetting(unit, alertType, newVal)";
                var unit = RequiredByteEnum(state, 1, 1, usage);
                var alertType = RequiredByteEnum(state, 2, 1, usage);
                var value = RequiredInt32(state, 3, usage);
                var maximum = (unit, alertType) switch
                {
                    (0, 0) => 5,
                    (0, 1) => 4,
                    (1, 0) => 8,
                    _ => 6
                };
                var success = audio.Available && unchecked((byte)value) <= maximum;
                if (success)
                    audio.FormatSettings[(unit, alertType)] = value;
                return PushBoolean(state, success);
            }
            case "SetSpeakerSpeed":
            {
                const string usage =
                    "Usage: local success = C_CombatAudioAlert.SetSpeakerSpeed(newVal)";
                var value = RequiredInt32(state, 1, usage);
                var success = audio.Available &&
                              unchecked((uint)(value + 10)) <= 20;
                if (success)
                    audio.SpeakerSpeed = value;
                return PushBoolean(state, success);
            }
            case "SetSpecSetting":
            {
                const string usage =
                    "Usage: local success = C_CombatAudioAlert.SetSpecSetting(setting, newVal)";
                var setting = RequiredByteEnum(state, 1, 8, usage);
                var value = RequiredInt32(state, 2, usage);
                var success = audio.Available &&
                              IsValidSpecSetting(audio, setting, value);
                if (success)
                    SetSpecSetting(audio, setting, value);
                return PushBoolean(state, success);
            }
            case "SetThrottle":
            {
                const string usage =
                    "Usage: local success = C_CombatAudioAlert.SetThrottle(throttleType, newVal)";
                var throttleType = RequiredByteEnum(state, 1, 10, usage);
                var value = RequiredFloat(state, 2, usage);
                var success = audio.Available &&
                              throttleType is >= 1 and <= 6 &&
                              value is >= 0 and <= 5;
                if (success)
                    audio.Throttles[throttleType] = value;
                return PushBoolean(state, success);
            }
            case "SpeakText":
            {
                const string usage =
                    "Usage: C_CombatAudioAlert.SpeakText(text, category [, allowOverlap])";
                var text = RequiredStringValue(state, 1, usage);
                var category = RequiredByteEnum(state, 2, 8, usage);
                var allowOverlap = OptionalBoolean(state, 3, usage);
                var request = new WowCombatAudioAlertSpeech(
                    text,
                    category,
                    allowOverlap);
                audio.LastSpeechRequest = request;
                if (audio.Available && audio.Enabled)
                    audio.SpokenRequests.Add(request);
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int Get<TKey>(IDictionary<TKey, int> values, TKey key)
        where TKey : notnull =>
        values.TryGetValue(key, out var value) ? value : 0;

    private static int GetCategoryVoice(
        WowCombatAudioAlertState audio,
        int category) =>
        category switch
        {
            5 => GetSpecSetting(audio, 2),
            6 => GetSpecSetting(audio, 6),
            _ => Get(audio.CategoryVoices, category)
        };

    private static int GetCategoryVolume(
        WowCombatAudioAlertState audio,
        int category) =>
        category switch
        {
            5 => GetSpecSetting(audio, 3),
            6 => GetSpecSetting(audio, 7),
            _ => Get(audio.CategoryVolumes, category)
        };

    private static int GetSpecSetting(
        WowCombatAudioAlertState audio,
        int setting) =>
        Get(
            audio.SpecSettings,
            (setting, Math.Min(audio.ActiveSpecializationIndex, 4u)));

    private static void SetCategoryVoice(
        WowCombatAudioAlertState audio,
        int category,
        int value)
    {
        switch (category)
        {
            case 5:
                SetSpecSetting(audio, 2, value);
                break;
            case 6:
                SetSpecSetting(audio, 6, value);
                break;
            default:
                audio.CategoryVoices[category] = value;
                break;
        }
    }

    private static void SetCategoryVolume(
        WowCombatAudioAlertState audio,
        int category,
        int value)
    {
        switch (category)
        {
            case 5:
                SetSpecSetting(audio, 3, value);
                break;
            case 6:
                SetSpecSetting(audio, 7, value);
                break;
            default:
                audio.CategoryVolumes[category] = value;
                break;
        }
    }

    private static void SetSpecSetting(
        WowCombatAudioAlertState audio,
        int setting,
        int value) =>
        audio.SpecSettings[
            (setting, Math.Min(audio.ActiveSpecializationIndex, 4u))] = value;

    private static float GetThrottle(
        WowCombatAudioAlertState audio,
        int throttleType)
    {
        if (!audio.Available)
            return 0;
        if (throttleType == 0)
            return 1;
        if (throttleType is >= 7 and <= 10)
            return 10;
        return audio.Throttles.TryGetValue(throttleType, out var value)
            ? value
            : 0;
    }

    private static bool IsValidSpecSetting(
        WowCombatAudioAlertState audio,
        int setting,
        int value) =>
        setting switch
        {
            0 or 1 or 4 or 5 => unchecked((byte)value) <= 5,
            2 or 6 => value >= 0 && unchecked((uint)value) < audio.VoiceCount,
            3 or 7 => unchecked((uint)value) <= 100,
            8 => unchecked((byte)value) <= 3,
            _ => false
        };

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int RequiredByteEnum(
        lua_State state,
        int index,
        byte maximum,
        string usage)
    {
        var value = unchecked((byte)RequiredInt32(state, index, usage));
        return value <= maximum ? value : luaL_error(state, usage);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((uint)luaL_error(state, usage));
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            return unchecked((uint)luaL_error(state, usage));
        return unchecked((uint)value);
    }

    private static float RequiredFloat(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (float)lua_tonumber(state, index);
    }

    private static string RequiredStringValue(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return false;
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }
}
