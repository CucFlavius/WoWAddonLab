using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowKioskApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "EnableGodMode",
        "GetCharacterTemplateSetIndex",
        "IsCompetitiveModeEnabled",
        "IsEnabled",
        "IsExpired",
        "IsHousingResetPending",
        "IsInLobby",
        "RequestHousingReset",
        "StartSession"
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
        lua_setglobal(state, "Kiosk");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "IsEnabled":
                lua_pushboolean(state, runtime.Kiosk.IsEnabled ? 1 : 0);
                return 1;
            case "IsCompetitiveModeEnabled":
                lua_pushboolean(state, runtime.Kiosk.IsCompetitiveModeEnabled ? 1 : 0);
                return 1;
            case "IsExpired":
                return PushEnabledBoolean(
                    state,
                    runtime.Kiosk,
                    runtime.Kiosk.SessionState == WowKioskSessionState.Expired);
            case "IsHousingResetPending":
                return PushEnabledBoolean(
                    state,
                    runtime.Kiosk,
                    runtime.Kiosk.IsHousingResetPending);
            case "IsInLobby":
                return PushEnabledBoolean(
                    state,
                    runtime.Kiosk,
                    runtime.Kiosk.SessionState == WowKioskSessionState.Lobby);
            case "GetCharacterTemplateSetIndex":
                if (!runtime.Kiosk.IsEnabled)
                {
                    return 0;
                }
                if (runtime.Kiosk.CharacterTemplateSetIndex is { } templateSetIndex)
                {
                    lua_pushinteger(state, templateSetIndex);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            case "EnableGodMode":
                if (runtime.Kiosk.IsEnabled)
                {
                    runtime.Kiosk.IsGodModeRequested = true;
                }
                return 0;
            case "RequestHousingReset":
                if (runtime.Kiosk.IsEnabled)
                {
                    runtime.Kiosk.IsHousingResetPending = true;
                }
                return 0;
            case "StartSession":
                if (runtime.Kiosk.IsEnabled)
                {
                    runtime.Kiosk.SessionState = WowKioskSessionState.Active;
                }
                return 0;
            default:
                return 0;
        }
    }

    private static int PushEnabledBoolean(
        lua_State state,
        WowKioskState kiosk,
        bool value)
    {
        if (!kiosk.IsEnabled)
        {
            return 0;
        }

        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }
}
