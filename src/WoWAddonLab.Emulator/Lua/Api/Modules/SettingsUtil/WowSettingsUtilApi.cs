using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSettingsUtilApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "NotifySettingsLoaded", "OpenSettingsPanel" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_SettingsUtil");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var settings = runtime.SettingsUtil;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "NotifySettingsLoaded":
                settings.SettingsLoaded = true;
                runtime.TriggerEvent("SETTINGS_LOADED");
                return 0;
            case "OpenSettingsPanel":
                settings.OpenCategoryId = OptionalInt32(
                    state,
                    1,
                    "Usage: C_SettingsUtil.OpenSettingsPanel([openToCategoryID, scrollToElementName])");
                settings.ScrollToElementName = OptionalString(
                    state,
                    2,
                    "Usage: C_SettingsUtil.OpenSettingsPanel([openToCategoryID, scrollToElementName])");
                runtime.TriggerEvent(
                    "SETTINGS_PANEL_OPEN",
                    settings.OpenCategoryId,
                    settings.ScrollToElementName);
                return 0;
            default:
                return 0;
        }
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
        }
        return (int)number;
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
        }
        return lua_tostring(state, index) ?? string.Empty;
    }
}
