using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowWorldTimerApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetWorldElapsedTime", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetWorldElapsedTimers", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var timers = LuaBindings.GetRuntime(state).WorldTimers.Timers;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "GetWorldElapsedTimers")
        {
            lua_pushnumber(state, timers.Count);
            return 1;
        }

        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, "Usage: GetWorldElapsedTime(timerID)");

        var number = lua_tonumber(state, 1);
        if (!double.IsFinite(number) ||
            number is < int.MinValue or > int.MaxValue)
        {
            return luaL_error(state, "Usage: GetWorldElapsedTime(timerID)");
        }

        var id = (int)number;
        if (!timers.TryGetValue(id, out var timer))
        {
            lua_pushstring(state, string.Empty);
            lua_pushnumber(state, 0);
            lua_pushnumber(state, 0);
            return 3;
        }
        lua_pushstring(state, timer.Name);
        lua_pushnumber(state, timer.ElapsedTime);
        lua_pushnumber(state, timer.Type);
        return 3;
    }
}
