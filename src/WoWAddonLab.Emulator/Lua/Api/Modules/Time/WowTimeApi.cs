using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTimeApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetGameTime",
        "GetLocalGameTime",
        "GetServerTime",
        "GetSessionTime",
        "GetTickTime",
        "GetTime",
        "GetTimePreciseSec",
        "debugprofilestart",
        "debugprofilestop",
        "time",
        "IsUsingFixedTimeStep"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetGameTime":
                lua_pushnumber(state, runtime.Client.RealmHour);
                lua_pushnumber(state, runtime.Client.RealmMinute);
                return 2;
            case "GetLocalGameTime":
            {
                var localTime = runtime.DateAndTime.CurrentTime.LocalDateTime;
                lua_pushnumber(state, localTime.Hour);
                lua_pushnumber(state, localTime.Minute);
                return 2;
            }
            case "GetServerTime":
                lua_pushnumber(
                    state,
                    unchecked((int)runtime.DateAndTime.CurrentTime.ToUnixTimeSeconds()));
                return 1;
            case "GetSessionTime":
                lua_pushnumber(state, runtime.FrameTime.SessionTimeSeconds);
                return 1;
            case "GetTickTime":
                lua_pushnumber(state, runtime.FrameTime.TickTimeSeconds);
                return 1;
            case "GetTime":
                lua_pushnumber(state, runtime.FrameTime.TimeSeconds);
                return 1;
            case "GetTimePreciseSec":
                lua_pushnumber(state, runtime.FrameTime.TimeSeconds);
                return 1;
            case "debugprofilestart":
                runtime.FrameTime.DebugProfileStartMilliseconds =
                    runtime.FrameTime.TickMilliseconds;
                return 0;
            case "debugprofilestop":
                lua_pushnumber(
                    state,
                    unchecked((int)(
                        runtime.FrameTime.TickMilliseconds -
                        runtime.FrameTime.DebugProfileStartMilliseconds)));
                return 1;
            case "time":
                if (lua_gettop(state) == 0)
                {
                    lua_pushnumber(state, runtime.DateAndTime.CurrentTime.ToUnixTimeSeconds());
                    return 1;
                }
                return InvokeOsTime(state);
            case "IsUsingFixedTimeStep":
                lua_pushboolean(
                    state,
                    runtime.FrameTime.FixedTimeStepSeconds > 0 ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static int InvokeOsTime(lua_State state)
    {
        lua_getglobal(state, "os");
        lua_getfield(state, -1, "time");
        lua_pushvalue(state, 1);
        if (lua_pcall(state, 1, 1, 0) != 0)
            return lua_error(state);
        return 1;
    }
}
