using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPerformanceApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        foreach (var function in new[]
                 {
                     "GetAddOnCPUUsage", "GetAddOnMemoryUsage", "GetEventCPUUsage",
                     "GetFrameCPUUsage", "GetFunctionCPUUsage", "GetScriptCPUUsage",
                     "ResetCPUUsage", "UpdateAddOnCPUUsage", "UpdateAddOnMemoryUsage"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? "";
        switch (operation)
        {
            case "GetEventCPUUsage":
                return PushUsageAndCount(state);
            case "GetFrameCPUUsage":
                if (LuaBindings.GetObject(runtime, 1) is null)
                {
                    return luaL_error(
                        state,
                        "Usage: local call_time, call_count = " +
                        "GetFrameCPUUsage(frame [, includeChildren])");
                }
                RequireOptionalBoolean(
                    state,
                    2,
                    "Usage: local call_time, call_count = " +
                    "GetFrameCPUUsage(frame [, includeChildren])");
                return PushUsageAndCount(state);
            case "GetFunctionCPUUsage":
                if (lua_type(state, 1) != LUA_TFUNCTION)
                {
                    return luaL_error(
                        state,
                        "Usage: GetFunctionCPUUsage(function[, includeSubroutines])");
                }
                return PushUsageAndCount(state);
            case "GetAddOnCPUUsage":
                RequireAddOnName(
                    state,
                    runtime,
                    "Usage: local result = GetAddOnCPUUsage(name)");
                return PushZero(state);
            case "GetAddOnMemoryUsage":
                RequireAddOnName(
                    state,
                    runtime,
                    "Usage: local result = GetAddOnMemoryUsage(name)");
                return PushZero(state);
            case "GetScriptCPUUsage":
                return PushZero(state);
            default:
                return 0;
        }
    }

    private static int PushUsageAndCount(lua_State state)
    {
        lua_pushnumber(state, 0);
        lua_pushinteger(state, 0);
        return 2;
    }

    private static int PushZero(lua_State state)
    {
        lua_pushnumber(state, 0);
        return 1;
    }

    private static void RequireAddOnName(
        lua_State state,
        LuaRuntime runtime,
        string usage)
    {
        var type = lua_type(state, 1);
        if (type == LUA_TSTRING)
            return;
        if (type != LUA_TNUMBER)
        {
            luaL_error(state, usage);
            return;
        }

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value))
        {
            luaL_error(state, usage);
            return;
        }

        var index = (int)value - 1;
        if (index < 0 || index >= runtime.AvailableManifests.Count)
            luaL_error(state, usage);
    }

    private static void RequireOptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        var type = lua_type(state, index);
        if (type is LUA_TNONE or LUA_TNIL or LUA_TBOOLEAN)
            return;
        luaL_error(state, usage);
    }
}
