using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowFunctionContainersApi : LuaApiModule
{
    private const string MetatableName = "LuaFunctionContainer";

    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly lua_CFunction ContainerCallback =
        DispatchContainerMetamethod;

    private static readonly string[] GlobalFunctions =
    [
        "RegisterEventCallback",
        "RegisterUnitEventCallback",
        "UnregisterEventCallback",
        "UnregisterUnitEventCallback"
    ];

    private static readonly HashSet<string> CallbackEvents =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CLASS_TALENTS_SWITCH_TO_LOADOUT_BY_INDEX",
            "CLASS_TALENTS_SWITCH_TO_LOADOUT_BY_NAME",
            "CLASS_TALENTS_SWITCH_TO_SPECIALIZATION_BY_INDEX",
            "CLASS_TALENTS_SWITCH_TO_SPECIALIZATION_BY_NAME",
            "COMBAT_LOG_APPLY_FILTER_SETTINGS",
            "COMBAT_LOG_EVENT",
            "COMBAT_LOG_EVENT_INTERNAL_UNFILTERED",
            "COMBAT_LOG_EVENT_UNFILTERED",
            "COMBAT_LOG_REFILTER_ENTRIES",
            "ENCOUNTER_STATE_CHANGED",
            "MINIMAP_PING",
            "TOOLTIP_SHOW_ITEM_COMPARISON"
        };

    internal static bool IsCallbackEvent(string eventName) =>
        CallbackEvents.Contains(eventName);

    internal static bool IsUnitCallbackEvent(string eventName) =>
        eventName.Equals("MINIMAP_PING", StringComparison.OrdinalIgnoreCase);

    public override void Register(lua_State state)
    {
        EnsureContainerMetatable(state);

        lua_newtable(state);
        lua_pushstring(state, "CreateCallback");
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, "CreateCallback");
        lua_setglobal(state, "C_FunctionContainers");

        foreach (var function in GlobalFunctions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setglobal(state, function);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "CreateCallback")
        {
            if (lua_type(state, 1) != LUA_TFUNCTION)
            {
                return luaL_error(
                    state,
                    "Usage: C_FunctionContainers.CreateCallback(func)");
            }

            var functionReference = runtime.CaptureFunction(state, 1);
            lua_newtable(state);
            var propertyTableReference = LuaRuntime.CaptureValue(state, -1);
            lua_pop(state, 1);
            unsafe
            {
                var storage = (int*)lua_newuserdata(state, 4 * sizeof(int));
                storage[0] = functionReference;
                storage[1] = propertyTableReference;
                storage[2] = 0;
                storage[3] = 0;
            }
            luaL_getmetatable(state, MetatableName);
            lua_setmetatable(state, -2);
            return 1;
        }

        if (lua_isstring(state, 1) == 0)
            return 0;
        var eventName = lua_tostring(state, 1) ?? string.Empty;
        var isUnitOperation =
            operation.Contains("Unit", StringComparison.Ordinal);
        if (!IsCallbackEvent(eventName) ||
            isUnitOperation && !IsUnitCallbackEvent(eventName))
        {
            return 0;
        }

        if (!TryCaptureCallback(runtime, state, 2, out var pointer, out var reference))
        {
            return luaL_error(
                state,
                $"Usage: {operation}(eventName, callback" +
                (isUnitOperation
                    ? ", unit"
                    : string.Empty) +
                ")");
        }

        string? unit = null;
        if (isUnitOperation)
        {
            if (lua_isstring(state, 3) == 0)
            {
                runtime.ReleaseReference(reference);
                return luaL_error(
                    state,
                    $"Usage: {operation}(eventName, callback, unit)");
            }
            unit = lua_tostring(state, 3) ?? string.Empty;
        }

        bool changed;
        if (operation.StartsWith("Unregister", StringComparison.Ordinal))
        {
            runtime.ReleaseReference(reference);
            changed = runtime.UnregisterGlobalEventCallback(
                eventName,
                pointer,
                unit);
        }
        else
        {
            runtime.RegisterGlobalEventCallback(
                eventName,
                pointer,
                reference,
                unit);
            changed = true;
        }

        lua_pushboolean(state, changed ? 1 : 0);
        return 1;
    }

    internal static bool TryCaptureCallback(
        LuaRuntime runtime,
        lua_State state,
        int index,
        out UIntPtr pointer,
        out int reference)
    {
        pointer = lua_topointer(state, index);
        if (lua_type(state, index) == LUA_TFUNCTION)
        {
            reference = runtime.CaptureFunction(state, index);
            return true;
        }

        unsafe
        {
            if (!IsFunctionContainer(state, index))
            {
                reference = 0;
                return false;
            }

            var storage = (int*)lua_touserdata(state, index);
            if (storage is null || *storage <= 0)
            {
                reference = 0;
                return false;
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, *storage);
            reference = runtime.CaptureFunction(state, -1);
            lua_pop(state, 1);
            return true;
        }
    }

    private static void EnsureContainerMetatable(lua_State state)
    {
        if (luaL_newmetatable(state, MetatableName) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        foreach (var metamethod in new[]
                 {
                     "__gc", "__index", "__newindex", "__eq", "__tostring"
                 })
        {
            lua_pushstring(state, metamethod);
            lua_pushcclosure(state, ContainerCallback, 1);
            lua_setfield(state, -2, metamethod);
        }
        lua_pushboolean(state, 0);
        lua_setfield(state, -2, "__metatable");
        lua_pop(state, 1);
    }

    private static int DispatchContainerMetamethod(lua_State state)
    {
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "__gc":
                unsafe
                {
                    var storage = (int*)lua_touserdata(state, 1);
                    if (storage is not null)
                    {
                        if (LuaBindings.TryGetRuntime(state, out var runtime))
                        {
                            runtime!.ReleaseReference(storage[0]);
                            runtime.ReleaseReference(storage[1]);
                        }
                        storage[0] = 0;
                        storage[1] = 0;
                    }
                }
                return 0;
            case "__index":
                if (lua_type(state, 2) == LUA_TSTRING &&
                    string.Equals(
                        lua_tostring(state, 2),
                        "Dump",
                        StringComparison.Ordinal))
                {
                    lua_pushstring(state, "Dump");
                    lua_pushcclosure(state, ContainerCallback, 1);
                    return 1;
                }
                unsafe
                {
                    var storage = (int*)lua_touserdata(state, 1);
                    if (storage is null || storage[1] <= 0)
                    {
                        lua_pushnil(state);
                        return 1;
                    }
                    lua_rawgeti(state, LUA_REGISTRYINDEX, storage[1]);
                    lua_pushvalue(state, 2);
                    lua_rawget(state, -2);
                    lua_remove(state, -2);
                    return 1;
                }
            case "__newindex":
                if (lua_type(state, 2) == LUA_TSTRING &&
                    string.Equals(
                        lua_tostring(state, 2),
                        "Dump",
                        StringComparison.Ordinal))
                {
                    return luaL_error(
                        state,
                        "Attempted to assign to read-only key Dump");
                }
                unsafe
                {
                    var storage = (int*)lua_touserdata(state, 1);
                    if (storage is null || storage[1] <= 0)
                        return 0;
                    lua_rawgeti(state, LUA_REGISTRYINDEX, storage[1]);
                    lua_pushvalue(state, 2);
                    lua_pushvalue(state, 3);
                    lua_settable(state, -3);
                    lua_pop(state, 1);
                    return 0;
                }
            case "__eq":
                lua_pushboolean(
                    state,
                    lua_topointer(state, 1) == lua_topointer(state, 2)
                        ? 1
                        : 0);
                return 1;
            case "__tostring":
                lua_pushstring(
                    state,
                    $"LuaFunctionContainer: 0x{lua_topointer(state, 1):X}");
                return 1;
            case "Dump":
                unsafe
                {
                    var storage = (int*)lua_touserdata(state, 1);
                    if (storage is null || storage[1] <= 0)
                        lua_pushnil(state);
                    else
                        lua_rawgeti(
                            state,
                            LUA_REGISTRYINDEX,
                            storage[1]);
                    return 1;
                }
            default:
                return 0;
        }
    }

    internal static bool IsFunctionContainer(lua_State state, int index)
    {
        if (lua_type(state, index) != LUA_TUSERDATA ||
            lua_getmetatable(state, index) == 0)
        {
            return false;
        }

        luaL_getmetatable(state, MetatableName);
        var matches = lua_rawequal(state, -1, -2) != 0;
        lua_pop(state, 2);
        return matches;
    }
}
