using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTimerApi : LuaApiModule
{
    private const string HandleMetatableName = "CppFunctionContainer";
    private const double MaximumDurationSeconds = uint.MaxValue / 1000.0;
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly lua_CFunction HandleCallback = DispatchHandle;

    public override void Register(lua_State state)
    {
        EnsureHandleMetatable(state);

        lua_newtable(state);
        foreach (var operation in new[] { "After", "NewTicker", "NewTimer" })
        {
            lua_pushstring(state, operation);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, operation);
        }
        lua_setglobal(state, "C_Timer");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var usage = operation switch
        {
            "After" => "Usage: C_Timer.After(seconds, callback)",
            "NewTicker" =>
                "Usage: local cbObject = C_Timer.NewTicker(seconds, callback [, iterations])",
            _ => "Usage: local cbObject = C_Timer.NewTimer(seconds, callback)"
        };
        if (!TryReadRequiredFiniteNumber(state, 1, out var seconds))
            return luaL_error(state, usage);
        var durationOwner = operation == "NewTimer" ? "C_Timer.NewTicker" : $"C_Timer.{operation}";
        if (seconds < 0)
            return luaL_error(state, $"{durationOwner} requires a non-negative duration");
        if (seconds > MaximumDurationSeconds)
            return luaL_error(state, $"{durationOwner} duration is too large");

        uint? iterations = null;
        if (operation == "NewTicker" &&
            !TryReadOptionalUInt32(state, 3, out iterations))
        {
            return luaL_error(state, usage);
        }

        var runtime = LuaBindings.GetRuntime(state);
        if (!WowFunctionContainersApi.TryCaptureCallback(
                runtime,
                state,
                2,
                out _,
                out var callbackReference))
        {
            return luaL_error(state, usage);
        }
        var milliseconds = (uint)Math.Truncate(seconds * 1000.0);
        if (operation == "After")
        {
            runtime.ScheduleTimer(milliseconds, callbackReference);
            return 0;
        }

        var timerId = runtime.ScheduleTimer(
            milliseconds,
            callbackReference,
            repeating: true,
            intervalMilliseconds: operation == "NewTicker" ? milliseconds : 0,
            iterations: operation == "NewTimer" ? 1u : iterations);
        PushHandle(state, runtime, timerId);
        return 1;
    }

    private static unsafe void PushHandle(lua_State state, LuaRuntime runtime, long timerId)
    {
        var storage = (long*)lua_newuserdata(state, sizeof(long));
        *storage = timerId;
        luaL_getmetatable(state, HandleMetatableName);
        lua_setmetatable(state, -2);
        var reference = LuaRuntime.CaptureValue(state, -1);
        runtime.AttachTimerHandle(timerId, reference);
    }

    private static int DispatchHandle(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (!TryGetHandleId(state, 1, out var timerId))
        {
            var usage = operation == "Invoke"
                ? "Usage: callback:Invoke([args])"
                : $"Usage: callback:{operation}()";
            return luaL_error(state, usage);
        }

        switch (operation)
        {
            case "Cancel":
                runtime.CancelTimer(timerId);
                return 0;
            case "IsCancelled":
                lua_pushboolean(state, runtime.IsTimerCancelled(timerId) ? 1 : 0);
                return 1;
            case "Invoke":
                if (runtime.IsTimerCancelled(timerId))
                    return 0;
                var argumentCount = lua_gettop(state) - 1;
                if (argumentCount > 1 ||
                    argumentCount == 1 && !IsCallbackArgument(state, 2))
                {
                    return luaL_error(
                        state,
                        "Invalid arguments to invoke callback object.");
                }
                runtime.InvokeTimer(timerId, 2, argumentCount);
                return 0;
            default:
                return 0;
        }
    }

    private static void EnsureHandleMetatable(lua_State state)
    {
        if (luaL_newmetatable(state, HandleMetatableName) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        foreach (var operation in new[] { "Cancel", "IsCancelled", "Invoke" })
        {
            lua_pushstring(state, operation);
            lua_pushcclosure(state, HandleCallback, 1);
            lua_setfield(state, -2, operation);
        }
        lua_pushvalue(state, -1);
        lua_setfield(state, -2, "__index");
        lua_pushboolean(state, 0);
        lua_setfield(state, -2, "__metatable");
        lua_pop(state, 1);
    }

    private static bool IsCallbackArgument(lua_State state, int index) =>
        lua_isnil(state, index) != 0 ||
        lua_type(state, index) == LUA_TFUNCTION ||
        WowFunctionContainersApi.IsFunctionContainer(state, index) ||
        TryGetHandleId(state, index, out _);

    private static unsafe bool TryGetHandleId(
        lua_State state,
        int index,
        out long timerId)
    {
        timerId = 0;
        if (lua_type(state, index) != LUA_TUSERDATA ||
            lua_getmetatable(state, index) == 0)
        {
            return false;
        }

        luaL_getmetatable(state, HandleMetatableName);
        var matches = lua_rawequal(state, -1, -2) != 0;
        lua_pop(state, 2);
        if (!matches)
            return false;

        var storage = (long*)lua_touserdata(state, index);
        if (storage is null || *storage <= 0)
            return false;
        timerId = *storage;
        return true;
    }

    private static bool TryReadRequiredFiniteNumber(
        lua_State state,
        int index,
        out double value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        value = lua_tonumber(state, index);
        return double.IsFinite(value);
    }

    private static bool TryReadOptionalUInt32(
        lua_State state,
        int index,
        out uint? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < 0 or > uint.MaxValue)
            return false;
        value = (uint)number;
        return true;
    }
}
