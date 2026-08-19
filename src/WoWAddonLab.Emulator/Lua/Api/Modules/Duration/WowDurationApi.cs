using System.Globalization;
using System.Runtime.InteropServices;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDurationApi : LuaApiModule
{
    private const string MetatableName = "LuaDurationObject";
    private const int StorageMagic = 0x44555241;

    private static readonly lua_CFunction GarbageCollectCallback = GarbageCollect;
    private static readonly lua_CFunction IndexCallback = Index;
    private static readonly lua_CFunction NewIndexCallback = NewIndex;
    private static readonly lua_CFunction EqualCallback = Equal;
    private static readonly lua_CFunction ToStringCallback = ToStringValue;
    private static readonly lua_CFunction DumpCallback = Dump;

    private static readonly IReadOnlyDictionary<string, lua_CFunction> Methods =
        new Dictionary<string, lua_CFunction>(StringComparer.Ordinal)
        {
            ["Assign"] = state => Dispatch(state, "Assign"),
            ["Copy"] = state => Dispatch(state, "Copy"),
            ["EvaluateElapsedDuration"] = state => Dispatch(state, "EvaluateElapsedDuration"),
            ["EvaluateElapsedPercent"] = state => Dispatch(state, "EvaluateElapsedPercent"),
            ["EvaluateRemainingDuration"] = state => Dispatch(state, "EvaluateRemainingDuration"),
            ["EvaluateRemainingPercent"] = state => Dispatch(state, "EvaluateRemainingPercent"),
            ["EvaluateTotalDuration"] = state => Dispatch(state, "EvaluateTotalDuration"),
            ["FormatElapsedDuration"] = state => Dispatch(state, "FormatElapsedDuration"),
            ["FormatRemainingDuration"] = state => Dispatch(state, "FormatRemainingDuration"),
            ["FormatTotalDuration"] = state => Dispatch(state, "FormatTotalDuration"),
            ["GetClock"] = state => Dispatch(state, "GetClock"),
            ["GetClockTime"] = state => Dispatch(state, "GetClockTime"),
            ["GetElapsedDuration"] = state => Dispatch(state, "GetElapsedDuration"),
            ["GetElapsedPercent"] = state => Dispatch(state, "GetElapsedPercent"),
            ["GetEndTime"] = state => Dispatch(state, "GetEndTime"),
            ["GetModRate"] = state => Dispatch(state, "GetModRate"),
            ["GetRemainingDuration"] = state => Dispatch(state, "GetRemainingDuration"),
            ["GetRemainingPercent"] = state => Dispatch(state, "GetRemainingPercent"),
            ["GetStartTime"] = state => Dispatch(state, "GetStartTime"),
            ["GetTotalDuration"] = state => Dispatch(state, "GetTotalDuration"),
            ["HasExpired"] = state => Dispatch(state, "HasExpired"),
            ["HasSecretValues"] = state => Dispatch(state, "HasSecretValues"),
            ["HasStarted"] = state => Dispatch(state, "HasStarted"),
            ["IsActive"] = state => Dispatch(state, "IsActive"),
            ["IsZero"] = state => Dispatch(state, "IsZero"),
            ["Reset"] = state => Dispatch(state, "Reset"),
            ["SetClock"] = state => Dispatch(state, "SetClock"),
            ["SetTimeFromEnd"] = state => Dispatch(state, "SetTimeFromEnd"),
            ["SetTimeFromStart"] = state => Dispatch(state, "SetTimeFromStart"),
            ["SetTimeSpan"] = state => Dispatch(state, "SetTimeSpan"),
            ["SetToDefaults"] = state => Dispatch(state, "SetToDefaults")
        };

    public override void Register(lua_State state)
    {
        if (luaL_newmetatable(state, MetatableName) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        foreach (var (name, callback) in Methods)
        {
            lua_pushcfunction(state, callback);
            lua_setfield(state, -2, name);
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

    public static void Push(lua_State state, WowDurationState? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
            return;
        }

        PushFromStart(state, value.StartTime, value.Duration, value.ModRate);
    }

    public static void Push(lua_State state, UiDurationState value) =>
        PushFromStart(state, value.StartTime, value.Duration, value.ModRate);

    public static void PushDefault(lua_State state) =>
        PushStorage(state, DurationVariant.Default, 0, 0, 0);

    public static bool TryRead(
        lua_State state,
        int index,
        out UiDurationState value)
    {
        value = new UiDurationState(0, 0, 1);
        unsafe
        {
            if (!TryGetStorage(state, index, out var storage))
                return false;
            value = Snapshot(*storage);
            return true;
        }
    }

    internal static bool TryReadMetrics(
        lua_State state,
        int index,
        bool modified,
        out WowDurationMetrics metrics)
    {
        metrics = default;
        unsafe
        {
            if (!TryGetStorage(state, index, out var storage))
                return false;

            var now = ClockMilliseconds(state, storage);
            metrics = new WowDurationMetrics(
                TotalMilliseconds(*storage, false) == 0,
                unchecked((uint)EndMilliseconds(*storage, modified)) <= now,
                StartMilliseconds(*storage, modified) * 0.001,
                EndMilliseconds(*storage, modified) * 0.001,
                TotalMilliseconds(*storage, modified) * 0.001,
                ElapsedMilliseconds(*storage, now, modified) * 0.001,
                RemainingMilliseconds(*storage, now, modified) * 0.001,
                ElapsedPercent(*storage, now, modified),
                RemainingPercent(*storage, now, modified));
            return true;
        }
    }

    private static void PushFromStart(
        lua_State state,
        double startTime,
        double duration,
        double modRate)
    {
        PushStorage(
            state,
            DurationVariant.FromStart,
            SecondsToMilliseconds(startTime),
            SecondsToMilliseconds(duration),
            ToFloat(modRate));
    }

    private static unsafe void PushStorage(
        lua_State state,
        DurationVariant variant,
        int firstMilliseconds,
        int secondMilliseconds,
        float modRate,
        WowDurationClockState? clock = null)
    {
        var runtime = LuaBindings.GetRuntime(state);
        lua_newtable(state);
        var propertyTableReference = LuaRuntime.CaptureValue(state, -1);
        lua_pop(state, 1);

        var storage = (DurationStorage*)lua_newuserdata(
            state,
            (UIntPtr)sizeof(DurationStorage));
        storage->PropertyTableReference = propertyTableReference;
        storage->Variant = variant;
        storage->FirstMilliseconds = firstMilliseconds;
        storage->SecondMilliseconds = secondMilliseconds;
        storage->ModRate = modRate;
        storage->ClockHandle = IntPtr.Zero;
        SetClock(storage, clock);
        storage->Magic = StorageMagic;
        luaL_getmetatable(state, MetatableName);
        lua_setmetatable(state, -2);
    }

    private static int Dispatch(lua_State state, string operation)
    {
        var usage = Usage(operation);
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return luaL_error(state, usage);

            switch (operation)
            {
                case "Assign":
                    if (lua_gettop(state) != 2 ||
                        !TryGetStorage(state, 2, out var other))
                    {
                        return luaL_error(state, usage);
                    }
                    CopyTiming(other, storage);
                    return 0;
                case "Copy":
                    if (lua_gettop(state) != 1)
                        return luaL_error(state, usage);
                    PushStorage(
                        state,
                        storage->Variant,
                        storage->FirstMilliseconds,
                        storage->SecondMilliseconds,
                        storage->ModRate,
                        GetClock(storage));
                    return 1;
                case "GetClock":
                    if (lua_gettop(state) != 1)
                        return luaL_error(state, usage);
                    var clock = GetClock(storage);
                    if (clock is null)
                        lua_pushnil(state);
                    else
                        WowDurationClockApi.PushBase(state, clock);
                    return 1;
                case "GetClockTime":
                    if (lua_gettop(state) != 1)
                        return luaL_error(state, usage);
                    PushMillisecondsSeconds(
                        state,
                        unchecked((int)ClockMilliseconds(state, storage)));
                    return 1;
                case "GetElapsedDuration":
                    if (!TryReadModifier(state, usage, out var elapsedModifier))
                        return 0;
                    PushMillisecondsSeconds(
                        state,
                        ElapsedMilliseconds(
                            *storage,
                            ClockMilliseconds(state, storage),
                            elapsedModifier));
                    return 1;
                case "GetElapsedPercent":
                    if (!TryReadModifier(state, usage, out var elapsedPercentModifier))
                        return 0;
                    lua_pushnumber(
                        state,
                        ElapsedPercent(
                            *storage,
                            ClockMilliseconds(state, storage),
                            elapsedPercentModifier));
                    return 1;
                case "GetEndTime":
                    if (!TryReadModifier(state, usage, out var endModifier))
                        return 0;
                    PushMillisecondsSeconds(state, EndMilliseconds(*storage, endModifier));
                    return 1;
                case "GetModRate":
                    if (lua_gettop(state) != 1)
                        return luaL_error(state, usage);
                    lua_pushnumber(state, ModRate(*storage));
                    return 1;
                case "GetRemainingDuration":
                    if (!TryReadModifier(state, usage, out var remainingModifier))
                        return 0;
                    PushMillisecondsSeconds(
                        state,
                        RemainingMilliseconds(
                            *storage,
                            ClockMilliseconds(state, storage),
                            remainingModifier));
                    return 1;
                case "GetRemainingPercent":
                    if (!TryReadModifier(state, usage, out var remainingPercentModifier))
                        return 0;
                    lua_pushnumber(
                        state,
                        RemainingPercent(
                            *storage,
                            ClockMilliseconds(state, storage),
                            remainingPercentModifier));
                    return 1;
                case "GetStartTime":
                    if (!TryReadModifier(state, usage, out var startModifier))
                        return 0;
                    PushMillisecondsSeconds(state, StartMilliseconds(*storage, startModifier));
                    return 1;
                case "GetTotalDuration":
                    if (!TryReadModifier(state, usage, out var totalModifier))
                        return 0;
                    PushMillisecondsSeconds(state, TotalMilliseconds(*storage, totalModifier));
                    return 1;
                case "HasExpired":
                    if (!TryReadModifier(state, usage, out var expiredModifier))
                        return 0;
                    lua_pushboolean(
                        state,
                        unchecked((uint)EndMilliseconds(*storage, expiredModifier)) <=
                        ClockMilliseconds(state, storage)
                            ? 1
                            : 0);
                    return 1;
                case "HasSecretValues":
                    if (lua_gettop(state) != 1)
                        return luaL_error(state, usage);
                    lua_pushboolean(state, 0);
                    return 1;
                case "HasStarted":
                    if (!TryReadModifier(state, usage, out var startedModifier))
                        return 0;
                    lua_pushboolean(
                        state,
                        ClockMilliseconds(state, storage) >=
                        unchecked((uint)StartMilliseconds(*storage, startedModifier))
                            ? 1
                            : 0);
                    return 1;
                case "IsActive":
                    if (!TryReadModifier(state, usage, out var activeModifier))
                        return 0;
                    var now = ClockMilliseconds(state, storage);
                    var start = unchecked((uint)StartMilliseconds(*storage, activeModifier));
                    var end = unchecked((uint)EndMilliseconds(*storage, activeModifier));
                    lua_pushboolean(state, now >= start && now < end ? 1 : 0);
                    return 1;
                case "IsZero":
                    if (lua_gettop(state) != 1)
                        return luaL_error(state, usage);
                    lua_pushboolean(state, TotalMilliseconds(*storage, false) == 0 ? 1 : 0);
                    return 1;
                case "Reset":
                case "SetToDefaults":
                    if (lua_gettop(state) != 1)
                        return luaL_error(state, usage);
                    Reset(storage);
                    return 0;
                case "SetClock":
                    if (lua_gettop(state) is < 1 or > 2)
                    {
                        return luaL_error(state, usage);
                    }
                    if (lua_gettop(state) == 1 || lua_isnil(state, 2) != 0)
                    {
                        SetClock(storage, null);
                        return 0;
                    }
                    if (!WowDurationClockApi.TryRead(state, 2, out var newClock))
                        return luaL_error(state, usage);
                    SetClock(storage, newClock);
                    return 0;
                case "SetTimeFromEnd":
                    if (!TryReadSetTimingArguments(
                            state,
                            usage,
                            out var endTime,
                            out var endDuration,
                            out var endModRate))
                    {
                        return 0;
                    }
                    storage->Variant = DurationVariant.FromEnd;
                    storage->FirstMilliseconds = endTime;
                    storage->SecondMilliseconds = endDuration;
                    storage->ModRate = endModRate;
                    return 0;
                case "SetTimeFromStart":
                    if (!TryReadSetTimingArguments(
                            state,
                            usage,
                            out var startTime,
                            out var startDuration,
                            out var startModRate))
                    {
                        return 0;
                    }
                    storage->Variant = DurationVariant.FromStart;
                    storage->FirstMilliseconds = startTime;
                    storage->SecondMilliseconds = startDuration;
                    storage->ModRate = startModRate;
                    return 0;
                case "SetTimeSpan":
                    if (lua_gettop(state) != 3 ||
                        !TryReadMilliseconds(state, 2, out var spanStart) ||
                        !TryReadMilliseconds(state, 3, out var spanEnd))
                    {
                        return luaL_error(state, usage);
                    }
                    if (unchecked((uint)spanStart) >= unchecked((uint)spanEnd))
                        spanEnd = spanStart;
                    storage->Variant = DurationVariant.TimeSpan;
                    storage->FirstMilliseconds = spanStart;
                    storage->SecondMilliseconds = spanEnd;
                    storage->ModRate = 0;
                    return 0;
                case "EvaluateElapsedDuration":
                    if (!TryReadCurveEvaluationArguments(
                            state,
                            usage,
                            out var elapsedDurationCurve,
                            out var evaluatedElapsedModifier))
                    {
                        return 0;
                    }
                    PushCurveEvaluation(
                        state,
                        elapsedDurationCurve!,
                        ElapsedMilliseconds(
                            *storage,
                            ClockMilliseconds(state, storage),
                            evaluatedElapsedModifier) * 0.001);
                    return 1;
                case "EvaluateElapsedPercent":
                    if (!TryReadCurveEvaluationArguments(
                            state,
                            usage,
                            out var elapsedPercentCurve,
                            out var evaluatedElapsedPercentModifier))
                    {
                        return 0;
                    }
                    lua_pushnumber(
                        state,
                        WowCurveApi.Evaluate(
                            elapsedPercentCurve!,
                            ElapsedPercent(
                                *storage,
                                ClockMilliseconds(state, storage),
                                evaluatedElapsedPercentModifier)));
                    return 1;
                case "EvaluateRemainingDuration":
                    if (!TryReadCurveEvaluationArguments(
                            state,
                            usage,
                            out var remainingDurationCurve,
                            out var evaluatedRemainingModifier))
                    {
                        return 0;
                    }
                    PushCurveEvaluation(
                        state,
                        remainingDurationCurve!,
                        RemainingMilliseconds(
                            *storage,
                            ClockMilliseconds(state, storage),
                            evaluatedRemainingModifier) * 0.001);
                    return 1;
                case "EvaluateRemainingPercent":
                    if (!TryReadCurveEvaluationArguments(
                            state,
                            usage,
                            out var remainingPercentCurve,
                            out var evaluatedRemainingPercentModifier))
                    {
                        return 0;
                    }
                    lua_pushnumber(
                        state,
                        WowCurveApi.Evaluate(
                            remainingPercentCurve!,
                            RemainingPercent(
                                *storage,
                                ClockMilliseconds(state, storage),
                                evaluatedRemainingPercentModifier)));
                    return 1;
                case "EvaluateTotalDuration":
                    if (!TryReadCurveEvaluationArguments(
                            state,
                            usage,
                            out var totalDurationCurve,
                            out var evaluatedTotalModifier))
                    {
                        return 0;
                    }
                    PushCurveEvaluation(
                        state,
                        totalDurationCurve!,
                        TotalMilliseconds(*storage, evaluatedTotalModifier) * 0.001);
                    return 1;
                case "FormatElapsedDuration":
                case "FormatRemainingDuration":
                case "FormatTotalDuration":
                    if (lua_gettop(state) is < 2 or > 3 ||
                        !WowStringUtilApi.TryReadFormatter(
                            state,
                            2,
                            out var formatter))
                    {
                        return luaL_error(state, usage);
                    }
                    if (!TryReadOptionalModifier(
                            state,
                            3,
                            usage,
                            out var formatModifier))
                        return 0;

                    var milliseconds = operation switch
                    {
                        "FormatElapsedDuration" => ElapsedMilliseconds(
                            *storage,
                            ClockMilliseconds(state, storage),
                            formatModifier),
                        "FormatRemainingDuration" => RemainingMilliseconds(
                            *storage,
                            ClockMilliseconds(state, storage),
                            formatModifier),
                        _ => TotalMilliseconds(*storage, formatModifier)
                    };
                    lua_pushstring(
                        state,
                        WowStringUtilApi.Format(
                            state,
                            formatter!,
                            milliseconds * 0.001));
                    return 1;
                default:
                    return 0;
            }
        }
    }

    private static bool TryReadModifier(
        lua_State state,
        string usage,
        out bool modified)
        => TryReadOptionalModifier(state, 2, usage, out modified);

    private static bool TryReadCurveEvaluationArguments(
        lua_State state,
        string usage,
        out WowCurveState? curve,
        out bool modified)
    {
        curve = null;
        modified = false;
        if (lua_gettop(state) is < 2 or > 3 ||
            !WowCurveApi.TryRead(state, 2, out curve))
        {
            luaL_error(state, usage);
            return false;
        }

        return TryReadOptionalModifier(state, 3, usage, out modified);
    }

    private static bool TryReadOptionalModifier(
        lua_State state,
        int index,
        string usage,
        out bool modified)
    {
        modified = false;
        var top = lua_gettop(state);
        if (top == index - 1 || top == index && lua_isnil(state, index) != 0)
            return true;
        if (top != index || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return false;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number != Math.Truncate(number) ||
            number is < 0 or > 1)
        {
            luaL_error(state, usage);
            return false;
        }
        modified = number == 1;
        return true;
    }

    private static void PushCurveEvaluation(
        lua_State state,
        WowCurveState curve,
        double value) =>
        lua_pushnumber(state, WowCurveApi.Evaluate(curve, (float)value));

    private static bool TryReadSetTimingArguments(
        lua_State state,
        string usage,
        out int first,
        out int duration,
        out float modRate)
    {
        first = 0;
        duration = 0;
        modRate = 1;
        var top = lua_gettop(state);
        if (top is < 3 or > 4 ||
            !TryReadMilliseconds(state, 2, out first) ||
            !TryReadMilliseconds(state, 3, out duration) ||
            top == 4 && lua_isnil(state, 4) == 0 &&
            !TryReadFloat(state, 4, out modRate))
        {
            luaL_error(state, usage);
            return false;
        }
        return true;
    }

    private static bool TryReadMilliseconds(lua_State state, int index, out int value)
    {
        value = 0;
        if (lua_isnumber(state, index) == 0)
            return false;
        var seconds = lua_tonumber(state, index);
        if (!double.IsFinite(seconds))
            return false;
        value = SecondsToMilliseconds(seconds);
        return true;
    }

    private static bool TryReadFloat(lua_State state, int index, out float value)
    {
        value = 0;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (double.IsNaN(number) || number is < -float.MaxValue or > float.MaxValue)
            return false;
        value = (float)number;
        return true;
    }

    private static int GarbageCollect(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return 0;
            if (LuaBindings.TryGetRuntime(state, out var runtime))
                runtime!.ReleaseReference(storage->PropertyTableReference);
            storage->PropertyTableReference = 0;
            SetClock(storage, null);
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

            luaL_getmetatable(state, MetatableName);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            if (lua_isnil(state, -1) == 0)
            {
                lua_remove(state, -2);
                return 1;
            }
            lua_pop(state, 2);

            if (storage->PropertyTableReference <= 0)
            {
                lua_pushnil(state);
                return 1;
            }
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

            luaL_getmetatable(state, MetatableName);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            var readOnly = lua_isnil(state, -1) == 0;
            lua_pop(state, 2);
            if (readOnly)
            {
                return luaL_error(
                    state,
                    $"Attempted to assign to read-only key {LuaKeyText(state, 2)}");
            }

            if (storage->PropertyTableReference <= 0)
                return 0;
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
        unsafe
        {
            var equal = TryGetStorage(state, 1, out var left) &&
                        TryGetStorage(state, 2, out var right) &&
                        left == right;
            lua_pushboolean(state, equal ? 1 : 0);
            return 1;
        }
    }

    private static int ToStringValue(lua_State state)
    {
        lua_pushstring(
            state,
            $"LuaDurationObject: 0x{lua_topointer(state, 1).ToUInt64():X}");
        return 1;
    }

    private static int Dump(lua_State state)
    {
        lua_pushnil(state);
        return 1;
    }

    private static unsafe bool TryGetStorage(
        lua_State state,
        int index,
        out DurationStorage* storage)
    {
        storage = null;
        if (lua_type(state, index) != LUA_TUSERDATA ||
            lua_getmetatable(state, index) == 0)
        {
            return false;
        }

        luaL_getmetatable(state, MetatableName);
        var matches = lua_rawequal(state, -1, -2) != 0;
        lua_pop(state, 2);
        if (!matches)
            return false;
        storage = (DurationStorage*)lua_touserdata(state, index);
        return storage is not null && storage->Magic == StorageMagic;
    }

    private static unsafe void CopyTiming(
        DurationStorage* source,
        DurationStorage* destination)
    {
        destination->Variant = source->Variant;
        destination->FirstMilliseconds = source->FirstMilliseconds;
        destination->SecondMilliseconds = source->SecondMilliseconds;
        destination->ModRate = source->ModRate;
        SetClock(destination, GetClock(source));
    }

    private static unsafe WowDurationClockState? GetClock(DurationStorage* storage)
    {
        if (storage->ClockHandle == IntPtr.Zero)
            return null;
        return GCHandle.FromIntPtr(storage->ClockHandle).Target as WowDurationClockState;
    }

    private static unsafe void SetClock(
        DurationStorage* storage,
        WowDurationClockState? clock)
    {
        if (storage->ClockHandle != IntPtr.Zero)
        {
            var previous = GCHandle.FromIntPtr(storage->ClockHandle);
            if (previous.IsAllocated)
                previous.Free();
            storage->ClockHandle = IntPtr.Zero;
        }
        if (clock is not null)
            storage->ClockHandle = GCHandle.ToIntPtr(GCHandle.Alloc(clock));
    }

    private static unsafe uint ClockMilliseconds(
        lua_State state,
        DurationStorage* storage) =>
        GetClock(storage)?.TimeMilliseconds ??
        LuaBindings.GetRuntime(state).FrameTime.TickMilliseconds;

    private static unsafe void Reset(DurationStorage* storage)
    {
        storage->Variant = DurationVariant.Default;
        storage->FirstMilliseconds = 0;
        storage->SecondMilliseconds = 0;
        storage->ModRate = 0;
        SetClock(storage, null);
    }

    private static UiDurationState Snapshot(DurationStorage storage) =>
        new(
            StartMilliseconds(storage, false) * 0.001,
            TotalMilliseconds(storage, false) * 0.001,
            ModRate(storage));

    private static int StartMilliseconds(DurationStorage storage, bool modified) =>
        storage.Variant switch
        {
            DurationVariant.FromStart => storage.FirstMilliseconds,
            DurationVariant.FromEnd => unchecked(
                storage.FirstMilliseconds -
                (modified
                    ? ModifiedDurationMilliseconds(storage)
                    : storage.SecondMilliseconds)),
            DurationVariant.TimeSpan => storage.FirstMilliseconds,
            _ => 0
        };

    private static int EndMilliseconds(DurationStorage storage, bool modified) =>
        storage.Variant switch
        {
            DurationVariant.FromStart => unchecked(
                storage.FirstMilliseconds +
                (modified
                    ? ModifiedDurationMilliseconds(storage)
                    : storage.SecondMilliseconds)),
            DurationVariant.FromEnd => storage.FirstMilliseconds,
            DurationVariant.TimeSpan => storage.SecondMilliseconds,
            _ => 0
        };

    private static int TotalMilliseconds(DurationStorage storage, bool modified) =>
        storage.Variant switch
        {
            DurationVariant.FromStart or DurationVariant.FromEnd =>
                modified
                    ? ModifiedDurationMilliseconds(storage)
                    : storage.SecondMilliseconds,
            DurationVariant.TimeSpan => unchecked(
                storage.SecondMilliseconds - storage.FirstMilliseconds),
            _ => 0
        };

    private static int ModifiedDurationMilliseconds(DurationStorage storage)
    {
        var modRate = storage.ModRate == 0 ? 1 : storage.ModRate;
        return (int)(storage.SecondMilliseconds * 0.001 / modRate * 1000.0);
    }

    private static float ModRate(DurationStorage storage) =>
        storage.Variant is DurationVariant.FromStart or DurationVariant.FromEnd
            ? storage.ModRate
            : 1;

    private static int RemainingMilliseconds(
        DurationStorage storage,
        uint now,
        bool modified)
    {
        var start = unchecked((uint)StartMilliseconds(storage, modified));
        var end = unchecked((uint)EndMilliseconds(storage, modified));
        if (start >= end || now >= end)
            return 0;
        if (now > start)
            return unchecked((int)(end - now));
        return TotalMilliseconds(storage, modified);
    }

    private static int ElapsedMilliseconds(
        DurationStorage storage,
        uint now,
        bool modified)
    {
        var start = unchecked((uint)StartMilliseconds(storage, modified));
        var end = unchecked((uint)EndMilliseconds(storage, modified));
        if (start >= end || now <= start)
            return 0;
        if (now < end)
            return unchecked((int)(now - start));
        return TotalMilliseconds(storage, modified);
    }

    private static float RemainingPercent(
        DurationStorage storage,
        uint now,
        bool modified)
    {
        var total = TotalMilliseconds(storage, modified) * 0.001;
        if (total == 0)
            return 0;
        return (float)(RemainingMilliseconds(storage, now, modified) * 0.001 / total);
    }

    private static float ElapsedPercent(
        DurationStorage storage,
        uint now,
        bool modified)
    {
        var total = TotalMilliseconds(storage, modified) * 0.001;
        if (total == 0)
            return 1;
        return (float)(ElapsedMilliseconds(storage, now, modified) * 0.001 / total);
    }

    private static void PushMillisecondsSeconds(lua_State state, int milliseconds) =>
        lua_pushnumber(state, milliseconds * 0.001);

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

    private static float ToFloat(double value) =>
        double.IsNaN(value) || value is < -float.MaxValue or > float.MaxValue
            ? 0
            : (float)value;

    private static string LuaKeyText(lua_State state, int index) =>
        lua_type(state, index) == LUA_TSTRING
            ? lua_tostring(state, index) ?? string.Empty
            : lua_typename(state, lua_type(state, index)) ?? "unknown";

    private static string Usage(string operation) => operation switch
    {
        "Assign" => "Usage: self:Assign(other)",
        "Copy" => "Usage: local copy = self:Copy()",
        "EvaluateElapsedDuration" =>
            "Usage: local result = self:EvaluateElapsedDuration(curve [, modifier])",
        "EvaluateElapsedPercent" =>
            "Usage: local result = self:EvaluateElapsedPercent(curve [, modifier])",
        "EvaluateRemainingDuration" =>
            "Usage: local result = self:EvaluateRemainingDuration(curve [, modifier])",
        "EvaluateRemainingPercent" =>
            "Usage: local result = self:EvaluateRemainingPercent(curve [, modifier])",
        "EvaluateTotalDuration" =>
            "Usage: local result = self:EvaluateTotalDuration(curve [, modifier])",
        "FormatElapsedDuration" =>
            "Usage: local formatted = self:FormatElapsedDuration(formatter [, modifier])",
        "FormatRemainingDuration" =>
            "Usage: local formatted = self:FormatRemainingDuration(formatter [, modifier])",
        "FormatTotalDuration" =>
            "Usage: local formatted = self:FormatTotalDuration(formatter [, modifier])",
        "GetClock" => "Usage: local clock = self:GetClock()",
        "GetClockTime" => "Usage: local clockTime = self:GetClockTime()",
        "GetElapsedDuration" =>
            "Usage: local elapsedDuration = self:GetElapsedDuration([modifier])",
        "GetElapsedPercent" =>
            "Usage: local elapsedPercent = self:GetElapsedPercent([modifier])",
        "GetEndTime" => "Usage: local endTime = self:GetEndTime([modifier])",
        "GetModRate" => "Usage: local modRate = self:GetModRate()",
        "GetRemainingDuration" =>
            "Usage: local remainingDuration = self:GetRemainingDuration([modifier])",
        "GetRemainingPercent" =>
            "Usage: local remainingPercent = self:GetRemainingPercent([modifier])",
        "GetStartTime" => "Usage: local startTime = self:GetStartTime([modifier])",
        "GetTotalDuration" =>
            "Usage: local totalDuration = self:GetTotalDuration([modifier])",
        "HasExpired" => "Usage: local hasExpired = self:HasExpired([modifier])",
        "HasSecretValues" =>
            "Usage: local hasSecretValues = self:HasSecretValues()",
        "HasStarted" => "Usage: local hasStarted = self:HasStarted([modifier])",
        "IsActive" => "Usage: local isActive = self:IsActive([modifier])",
        "IsZero" => "Usage: local isZero = self:IsZero()",
        "Reset" => "Usage: self:Reset()",
        "SetClock" => "Usage: self:SetClock([clock])",
        "SetTimeFromEnd" =>
            "Usage: self:SetTimeFromEnd(endTime, duration [, modRate])",
        "SetTimeFromStart" =>
            "Usage: self:SetTimeFromStart(startTime, duration [, modRate])",
        "SetTimeSpan" => "Usage: self:SetTimeSpan(startTime, endTime)",
        "SetToDefaults" => "Usage: self:SetToDefaults()",
        _ => $"Usage: self:{operation}()"
    };

    private enum DurationVariant
    {
        Default,
        FromStart,
        FromEnd,
        TimeSpan
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DurationStorage
    {
        public int PropertyTableReference;
        public DurationVariant Variant;
        public int FirstMilliseconds;
        public int SecondMilliseconds;
        public float ModRate;
        public IntPtr ClockHandle;
        public int Magic;
    }
}
