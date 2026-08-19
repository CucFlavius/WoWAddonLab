using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowEventUtilsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "IsCallbackEvent", "IsEventValid" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_EventUtils");
    }

    private static int Dispatch(lua_State state)
    {
        if (lua_isstring(state, 1) == 0)
            return luaL_error(state, "Usage: local valid = C_EventUtils.IsEventValid(eventName)");
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var eventName = lua_tostring(state, 1) ?? string.Empty;
        lua_pushboolean(
            state,
            operation == "IsEventValid" &&
            LuaBindings.GetRuntime(state).EventUtils.ValidEvents.Contains(eventName)
                ? 1
                : 0);
        return 1;
    }
}
