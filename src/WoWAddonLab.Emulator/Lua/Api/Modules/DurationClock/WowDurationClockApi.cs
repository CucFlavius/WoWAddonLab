using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal static class WowDurationClockApi
{
    private const string BaseMetatableName = "LuaDurationClock";
    private const string ManualMetatableName = "LuaDurationManualClock";
    private const int StorageMagic = 0x44434C4B;

    private static readonly lua_CFunction GarbageCollectCallback = GarbageCollect;
    private static readonly lua_CFunction IndexCallback = Index;
    private static readonly lua_CFunction NewIndexCallback = NewIndex;
    private static readonly lua_CFunction EqualCallback = Equal;
    private static readonly lua_CFunction ToStringCallback = ToStringValue;
    private static readonly lua_CFunction DumpCallback = Dump;
    private static readonly lua_CFunction GetTimeCallback = GetTime;
    private static readonly lua_CFunction AdvanceTimeCallback = AdvanceTime;
    private static readonly lua_CFunction ResetTimeCallback = ResetTime;
    private static readonly lua_CFunction RewindTimeCallback = RewindTime;
    private static readonly lua_CFunction SetTimeCallback = SetTime;

    public static void Register(lua_State state)
    {
        RegisterMetatable(state, BaseMetatableName, false);
        RegisterMetatable(state, ManualMetatableName, true);
    }

    public static void PushManual(lua_State state, WowDurationClockState clock) =>
        Push(state, clock, true);

    public static void PushBase(lua_State state, WowDurationClockState clock) =>
        Push(state, clock, false);

    public static unsafe bool TryRead(
        lua_State state,
        int index,
        out WowDurationClockState? clock)
    {
        clock = null;
        if (!TryGetStorage(state, index, out var storage))
            return false;
        clock = GetClock(storage);
        return clock is not null;
    }

    private static void RegisterMetatable(
        lua_State state,
        string metatableName,
        bool manual)
    {
        if (luaL_newmetatable(state, metatableName) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        lua_pushcfunction(state, GetTimeCallback);
        lua_setfield(state, -2, "GetTime");
        if (manual)
        {
            lua_pushcfunction(state, AdvanceTimeCallback);
            lua_setfield(state, -2, "AdvanceTime");
            lua_pushcfunction(state, ResetTimeCallback);
            lua_setfield(state, -2, "ResetTime");
            lua_pushcfunction(state, RewindTimeCallback);
            lua_setfield(state, -2, "RewindTime");
            lua_pushcfunction(state, SetTimeCallback);
            lua_setfield(state, -2, "SetTime");
        }

        lua_pushcfunction(state, GarbageCollectCallback);
        lua_setfield(state, -2, "__gc");
        lua_pushcfunction(state, IndexCallback);
        lua_setfield(state, -2, "__index");
        lua_pushcfunction(state, NewIndexCallback);
        lua_setfield(state, -2, "__newindex");
        lua_pushcfunction(state, EqualCallback);
        lua_setfield(state, -2, "__eq");
        lua_pushcfunction(state, ToStringCallback);
        lua_setfield(state, -2, "__tostring");
        lua_pushcfunction(state, DumpCallback);
        lua_setfield(state, -2, "__dump");
        lua_pushboolean(state, 0);
        lua_setfield(state, -2, "__metatable");
        lua_pop(state, 1);
    }

    private static unsafe void Push(
        lua_State state,
        WowDurationClockState clock,
        bool manual)
    {
        var runtime = LuaBindings.GetRuntime(state);
        lua_newtable(state);
        var propertyTableReference = LuaRuntime.CaptureValue(state, -1);
        lua_pop(state, 1);

        var storage = (ClockStorage*)lua_newuserdata(state, (UIntPtr)sizeof(ClockStorage));
        storage->PropertyTableReference = propertyTableReference;
        storage->ClockHandle = GCHandle.ToIntPtr(GCHandle.Alloc(clock));
        storage->Manual = manual ? 1 : 0;
        storage->Magic = StorageMagic;
        luaL_getmetatable(state, manual ? ManualMetatableName : BaseMetatableName);
        lua_setmetatable(state, -2);
    }

    private static int GetTime(lua_State state)
    {
        if (lua_gettop(state) != 1 || !TryRead(state, 1, out var clock))
            return luaL_error(state, "Usage: local time = self:GetTime()");
        lua_pushnumber(state, clock!.TimeMilliseconds * 0.001);
        return 1;
    }

    private static int AdvanceTime(lua_State state)
    {
        if (!TryReadManualMilliseconds(
                state,
                "Usage: self:AdvanceTime(delta)",
                out var clock,
                out var delta))
        {
            return 0;
        }
        var amount = unchecked((uint)delta);
        clock!.TimeMilliseconds = amount > ~clock.TimeMilliseconds
            ? uint.MaxValue
            : clock.TimeMilliseconds + amount;
        return 0;
    }

    private static int ResetTime(lua_State state)
    {
        if (lua_gettop(state) != 1 ||
            !TryGetManualClock(state, 1, out var clock))
        {
            return luaL_error(state, "Usage: self:ResetTime()");
        }
        clock!.TimeMilliseconds = 0;
        return 0;
    }

    private static int RewindTime(lua_State state)
    {
        if (!TryReadManualMilliseconds(
                state,
                "Usage: self:RewindTime(delta)",
                out var clock,
                out var delta))
        {
            return 0;
        }
        var amount = unchecked((uint)delta);
        clock!.TimeMilliseconds = amount > clock.TimeMilliseconds
            ? 0
            : clock.TimeMilliseconds - amount;
        return 0;
    }

    private static int SetTime(lua_State state)
    {
        if (!TryReadManualMilliseconds(
                state,
                "Usage: self:SetTime(time)",
                out var clock,
                out var time))
        {
            return 0;
        }
        clock!.TimeMilliseconds = unchecked((uint)time);
        return 0;
    }

    private static bool TryReadManualMilliseconds(
        lua_State state,
        string usage,
        out WowDurationClockState? clock,
        out int milliseconds)
    {
        clock = null;
        milliseconds = 0;
        if (lua_gettop(state) != 2 ||
            !TryGetManualClock(state, 1, out clock) ||
            lua_isnumber(state, 2) == 0)
        {
            luaL_error(state, usage);
            return false;
        }
        milliseconds = SecondsToMilliseconds(lua_tonumber(state, 2));
        return true;
    }

    private static unsafe bool TryGetManualClock(
        lua_State state,
        int index,
        out WowDurationClockState? clock)
    {
        clock = null;
        if (!TryGetStorage(state, index, out var storage) || storage->Manual == 0)
            return false;
        clock = GetClock(storage);
        return clock is not null;
    }

    private static int GarbageCollect(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return 0;
            if (LuaBindings.TryGetRuntime(state, out var runtime))
                runtime!.ReleaseReference(storage->PropertyTableReference);
            if (storage->ClockHandle != IntPtr.Zero)
            {
                var handle = GCHandle.FromIntPtr(storage->ClockHandle);
                if (handle.IsAllocated)
                    handle.Free();
            }
            storage->PropertyTableReference = 0;
            storage->ClockHandle = IntPtr.Zero;
            storage->Magic = 0;
            return 0;
        }
    }

    private static int Index(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
            {
                lua_pushnil(state);
                return 1;
            }
            var metatableName = storage->Manual != 0
                ? ManualMetatableName
                : BaseMetatableName;
            luaL_getmetatable(state, metatableName);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            if (lua_isnil(state, -1) == 0)
            {
                lua_remove(state, -2);
                return 1;
            }
            lua_pop(state, 2);
            lua_rawgeti(state, LUA_REGISTRYINDEX, storage->PropertyTableReference);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            lua_remove(state, -2);
            return 1;
        }
    }

    private static int NewIndex(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return 0;
            var metatableName = storage->Manual != 0
                ? ManualMetatableName
                : BaseMetatableName;
            luaL_getmetatable(state, metatableName);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            var readOnly = lua_isnil(state, -1) == 0;
            lua_pop(state, 2);
            if (readOnly)
                return luaL_error(state, "Attempted to assign to read-only key");
            lua_rawgeti(state, LUA_REGISTRYINDEX, storage->PropertyTableReference);
            lua_pushvalue(state, 2);
            lua_pushvalue(state, 3);
            lua_rawset(state, -3);
            lua_pop(state, 1);
            return 0;
        }
    }

    private static int Equal(lua_State state)
    {
        var equal = TryRead(state, 1, out var left) &&
                    TryRead(state, 2, out var right) &&
                    ReferenceEquals(left, right);
        lua_pushboolean(state, equal ? 1 : 0);
        return 1;
    }

    private static int ToStringValue(lua_State state)
    {
        unsafe
        {
            var typeName = TryGetStorage(state, 1, out var storage) && storage->Manual != 0
                ? ManualMetatableName
                : BaseMetatableName;
            lua_pushstring(state, $"{typeName}: 0x{lua_topointer(state, 1).ToUInt64():X}");
            return 1;
        }
    }

    private static int Dump(lua_State state)
    {
        lua_pushnil(state);
        return 1;
    }

    private static unsafe bool TryGetStorage(
        lua_State state,
        int index,
        out ClockStorage* storage)
    {
        storage = null;
        if (lua_type(state, index) != LUA_TUSERDATA || lua_getmetatable(state, index) == 0)
            return false;
        luaL_getmetatable(state, BaseMetatableName);
        var baseMatch = lua_rawequal(state, -1, -2) != 0;
        lua_pop(state, 1);
        luaL_getmetatable(state, ManualMetatableName);
        var manualMatch = lua_rawequal(state, -1, -2) != 0;
        lua_pop(state, 2);
        if (!baseMatch && !manualMatch)
            return false;
        storage = (ClockStorage*)lua_touserdata(state, index);
        return storage is not null && storage->Magic == StorageMagic;
    }

    private static unsafe WowDurationClockState? GetClock(ClockStorage* storage) =>
        storage->ClockHandle == IntPtr.Zero
            ? null
            : GCHandle.FromIntPtr(storage->ClockHandle).Target as WowDurationClockState;

    private static int SecondsToMilliseconds(double seconds)
    {
        var milliseconds = seconds * 1000.0;
        if (!double.IsFinite(milliseconds) ||
            milliseconds < long.MinValue ||
            milliseconds > long.MaxValue)
        {
            return 0;
        }
        return unchecked((int)(long)milliseconds);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ClockStorage
    {
        public int PropertyTableReference;
        public IntPtr ClockHandle;
        public int Manual;
        public int Magic;
    }
}
