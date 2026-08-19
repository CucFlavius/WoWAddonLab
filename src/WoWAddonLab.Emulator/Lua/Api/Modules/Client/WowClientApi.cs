using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowClientApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanAutoSetGamePadCursorControl",
        "CanExitVehicle",
        "FlashClientIcon",
        "GetFramerate",
        "GetBackgroundLoadingStatus",
        "GetAlternativeDefaultLanguage",
        "GetDefaultLanguage",
        "GetDefaultScale",
        "GetFileStreamingStatus",
        "GetNetStats",
        "GetProtocolTypes",
        "GetAvailableBandwidth",
        "GetDownloadedPercentage",
        "GetMovieDownloadProgress",
        "GetRestState",
        "GetMirrorTimerInfo",
        "GetArchaeologyInfo",
        "GetMoney",
        "GetText",
        "GetXPExhaustion",
        "IsPlayerAtEffectiveMaxLevel",
        "IsPlayerInWorld",
        "IsPlayerMoving",
        "IsResting",
        "IsInJailersTower",
        "InCombatLockdown",
        "IsBetaBuild",
        "IsTestBuild",
        "IsThreatWarningEnabled",
        "IsXPUserDisabled",
        "NoPlayTime",
        "PartialPlayTime",
        "PlayerIsTimerunning",
        "PlayerGetTimerunningSeasonID",
        "Logout",
        "Quit",
        "GetSendMailPrice",
        "VehicleExit",
        "SetGamePadCursorControl",
        "SupportsClipCursor"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        lua_newtable(state);
        lua_pushstring(state, "SupportsClipCursor");
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, "SupportsClipCursor");
        lua_setglobal(state, "C_Client");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "SupportsClipCursor":
                lua_pushboolean(state, runtime.Input.SupportsClipCursor ? 1 : 0);
                return 1;
            case "CanExitVehicle":
                lua_pushboolean(state, runtime.Units.Player.IsInVehicle ? 1 : 0);
                return 1;
            case "VehicleExit":
                runtime.Client.VehicleExitRequested = true;
                return 0;
            case "Logout":
            case "Quit":
                runtime.Client.SessionActionRequested = operation;
                return 0;
            case "FlashClientIcon":
                runtime.Client.ClientIconFlashRequested = true;
                runtime.Client.ClientIconFlashBriefly =
                    lua_gettop(state) >= 1 && lua_toboolean(state, 1) != 0;
                return 0;
            case "GetFramerate":
                lua_pushnumber(state, runtime.FrameRate);
                return 1;
            case "GetText":
            {
                var key = lua_type(state, 1) == LUA_TSTRING
                    ? lua_tostring(state, 1) ?? string.Empty
                    : string.Empty;
                lua_getglobal(state, key);
                if (lua_type(state, -1) == LUA_TSTRING)
                    return 1;
                lua_pop(state, 1);
                lua_pushstring(state, key);
                return 1;
            }
            case "CanAutoSetGamePadCursorControl":
            {
                var requestedEnabled = lua_toboolean(state, 1) != 0;
                var canTransition =
                    runtime.Client.CanAutoSetGamePadCursorControl &&
                    requestedEnabled != runtime.Client.GamePadCursorControlEnabled;
                lua_pushboolean(state, canTransition ? 1 : 0);
                return 1;
            }
            case "SetGamePadCursorControl":
                runtime.Client.GamePadCursorControlEnabled =
                    lua_toboolean(state, 1) != 0;
                return 0;
            case "GetMoney":
                lua_pushnumber(state, runtime.Client.Money);
                return 1;
            case "GetSendMailPrice":
            {
                var attachmentCount = Math.Clamp(
                    runtime.Client.SendMailAttachmentCount,
                    0,
                    16);
                var postage = Math.Max(30L, attachmentCount * 30L);
                lua_pushnumber(
                    state,
                    runtime.Client.SendMailMoney + (double)postage);
                return 1;
            }
            case "PlayerGetTimerunningSeasonID":
                if (runtime.Client.TimerunningSeasonId is { } timerunningSeasonId)
                    lua_pushinteger(state, timerunningSeasonId);
                else
                    lua_pushnil(state);
                return 1;
            case "GetMirrorTimerInfo":
            {
                const string usage =
                    "Usage: local name, startValue, maxValue, scale, paused, " +
                    "label, spellID = GetMirrorTimerInfo(timerIndex)";
                if (lua_type(state, 1) != LUA_TNUMBER)
                    return luaL_error(state, usage);
                var luaIndex = lua_tonumber(state, 1);
                if (luaIndex < 0 || luaIndex > uint.MaxValue)
                    return luaL_error(state, usage);
                var internalIndex = (int)luaIndex - 1;
                if (internalIndex is < 0 or > 2)
                    return 0;

                var timer = runtime.Client.MirrorTimers.GetValueOrDefault(internalIndex) ??
                    new WowMirrorTimerState("UNKNOWN", 0, 0, 0, 0, null, 0);
                lua_pushstring(state, timer.Name);
                lua_pushnumber(state, timer.StartValue);
                lua_pushnumber(state, timer.MaximumValue);
                lua_pushnumber(state, timer.Scale);
                lua_pushnumber(state, timer.Paused);
                if (timer.Label is null)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, timer.Label);
                lua_pushnumber(state, timer.SpellId);
                return 7;
            }
            case "GetArchaeologyInfo":
                lua_pushstring(state, runtime.Client.ArchaeologyRaceName);
                return 1;
            case "GetFileStreamingStatus":
                lua_pushinteger(state, runtime.Client.FileStreamingStatus);
                return 1;
            case "GetBackgroundLoadingStatus":
                lua_pushinteger(state, runtime.Client.BackgroundLoadingStatus);
                return 1;
            case "GetDefaultLanguage":
                lua_pushstring(state, runtime.Client.DefaultLanguage);
                lua_pushinteger(state, runtime.Client.DefaultLanguageId);
                return 2;
            case "GetDefaultScale":
                lua_pushnumber(state, runtime.Ui.DefaultUiScale);
                return 1;
            case "GetAlternativeDefaultLanguage":
                if (runtime.Client.AlternativeDefaultLanguage is null)
                    return 0;
                lua_pushstring(state, runtime.Client.AlternativeDefaultLanguage);
                if (runtime.Client.AlternativeDefaultLanguageId is { } alternativeLanguageId)
                    lua_pushinteger(state, alternativeLanguageId);
                else
                    lua_pushnil(state);
                return 2;
            case "GetNetStats":
                lua_pushnumber(state, runtime.Client.IncomingBandwidthKilobytesPerSecond);
                lua_pushnumber(state, runtime.Client.OutgoingBandwidthKilobytesPerSecond);
                lua_pushinteger(state, runtime.Client.HomeLatencyMilliseconds);
                lua_pushinteger(state, runtime.Client.WorldLatencyMilliseconds);
                return 4;
            case "GetProtocolTypes":
                lua_pushinteger(state, runtime.Client.HomeProtocolType);
                lua_pushinteger(state, runtime.Client.WorldProtocolType);
                return 2;
            case "GetAvailableBandwidth":
                lua_pushnumber(state, runtime.Client.AvailableBandwidth);
                return 1;
            case "GetDownloadedPercentage":
                lua_pushnumber(state, runtime.Client.DownloadedPercentage);
                return 1;
            case "GetMovieDownloadProgress":
            {
                const string usage =
                    "Usage: local inProgress, downloaded, total = " +
                    "GetMovieDownloadProgress(movieId)";
                if (lua_type(state, 1) != LUA_TNUMBER)
                    return luaL_error(state, usage);
                var movieIdValue = lua_tonumber(state, 1);
                if (movieIdValue < int.MinValue || movieIdValue > int.MaxValue)
                    return luaL_error(state, usage);
                var movieId = (int)movieIdValue;
                var progress = runtime.Client.MovieDownloadProgress.GetValueOrDefault(movieId);
                lua_pushboolean(state, progress.InProgress ? 1 : 0);
                lua_pushnumber(state, progress.Downloaded);
                lua_pushnumber(state, progress.Total);
                return 3;
            }
            case "IsResting":
                lua_pushboolean(state, runtime.Client.IsResting ? 1 : 0);
                return 1;
            case "IsPlayerInWorld":
                lua_pushboolean(state, runtime.Client.IsPlayerInWorld ? 1 : 0);
                return 1;
            case "IsPlayerMoving":
                lua_pushboolean(state, runtime.Client.IsPlayerMoving ? 1 : 0);
                return 1;
            case "IsInJailersTower":
                lua_pushboolean(state, runtime.Client.IsInJailersTower ? 1 : 0);
                return 1;
            case "InCombatLockdown":
                lua_pushboolean(state, runtime.Client.InCombatLockdown ? 1 : 0);
                return 1;
            case "IsBetaBuild":
            case "IsTestBuild":
                lua_pushboolean(state, runtime.Client.IsTestBuild ? 1 : 0);
                return 1;
            case "GetXPExhaustion":
                if (runtime.Client.ExperienceExhaustion is { } exhaustion)
                    lua_pushnumber(state, exhaustion);
                else
                    lua_pushnil(state);
                return 1;
            case "GetRestState":
                if (runtime.Client.RestState is not { } restState)
                    return 0;
                lua_pushinteger(state, restState.ExhaustionId);
                lua_pushstring(state, restState.Name);
                lua_pushnumber(state, restState.Factor);
                return 3;
            case "IsPlayerAtEffectiveMaxLevel":
                lua_pushboolean(state, runtime.Client.IsPlayerAtEffectiveMaxLevel ? 1 : 0);
                return 1;
            case "IsXPUserDisabled":
                lua_pushboolean(state, runtime.Client.IsXpUserDisabled ? 1 : 0);
                return 1;
            case "PlayerIsTimerunning":
                lua_pushboolean(
                    state,
                    runtime.Client.TimerunningSeasonId is > 0 ? 1 : 0);
                return 1;
            case "PartialPlayTime":
                if (runtime.Client.HasPartialPlayTime)
                    lua_pushboolean(state, 1);
                else
                    lua_pushnil(state);
                return 1;
            case "NoPlayTime":
                if (runtime.Client.HasNoPlayTime)
                    lua_pushboolean(state, 1);
                else
                    lua_pushnil(state);
                return 1;
            case "IsThreatWarningEnabled":
                lua_pushboolean(state, runtime.Client.ThreatWarningEnabled ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }
}
