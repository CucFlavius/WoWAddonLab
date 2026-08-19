using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAddOnProfilerApi : LuaApiModule
{
    private const string AddMeasuredCallEventUsage =
        "Usage: C_AddOnProfiler.AddMeasuredCallEvent(name)";
    private const string AddPerformanceMessageShownUsage =
        "Usage: C_AddOnProfiler.AddPerformanceMessageShown(msg)";
    private const string GetAddOnMetricUsage =
        "Usage: local result = C_AddOnProfiler.GetAddOnMetric(name, metric)";
    private const string GetApplicationMetricUsage =
        "Usage: local result = C_AddOnProfiler.GetApplicationMetric(metric)";
    private const string GetOverallMetricUsage =
        "Usage: local result = C_AddOnProfiler.GetOverallMetric(metric)";
    private const string GetTopKAddOnsForMetricUsage =
        "Usage: local results = C_AddOnProfiler.GetTopKAddOnsForMetric(metric, k)";
    private const string MeasureCallUsage =
        "Usage: C_AddOnProfiler.MeasureCall(func, ...)";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AddMeasuredCallEvent",
        "AddPerformanceMessageShown",
        "CheckForPerformanceMessage",
        "GetAddOnMetric",
        "GetApplicationMetric",
        "GetOverallMetric",
        "GetTicksPerSecond",
        "GetTopKAddOnsForMetric",
        "IsEnabled",
        "MeasureCall"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_AddOnProfiler");
    }

    private static int Dispatch(lua_State state)
    {
        var profiler = LuaBindings.GetRuntime(state).AddOnProfiler;
        switch (lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty)
        {
            case "AddMeasuredCallEvent":
                profiler.AddMeasuredCallEvent(
                    RequiredString(state, 1, AddMeasuredCallEventUsage));
                return 0;
            case "AddPerformanceMessageShown":
                profiler.RecordPerformanceMessageShown(
                    RequiredPerformanceMessage(state));
                return 0;
            case "CheckForPerformanceMessage":
                if (!profiler.Enabled ||
                    profiler.PendingPerformanceMessage is not { } message)
                {
                    return 0;
                }
                PushPerformanceMessage(state, message);
                return 1;
            case "GetAddOnMetric":
                lua_pushnumber(
                    state,
                    profiler.GetAddOnMetric(
                        RequiredString(state, 1, GetAddOnMetricUsage),
                        RequiredMetric(state, 2, GetAddOnMetricUsage)));
                return 1;
            case "GetApplicationMetric":
                lua_pushnumber(
                    state,
                    profiler.GetApplicationMetric(
                        RequiredMetric(state, 1, GetApplicationMetricUsage)));
                return 1;
            case "GetOverallMetric":
                lua_pushnumber(
                    state,
                    profiler.GetOverallMetric(
                        RequiredMetric(state, 1, GetOverallMetricUsage)));
                return 1;
            case "GetTicksPerSecond":
                lua_pushnumber(state, profiler.TicksPerSecond);
                return 1;
            case "GetTopKAddOnsForMetric":
                PushTopAddOns(
                    state,
                    profiler.GetTopAddOns(
                        RequiredMetric(
                            state,
                            1,
                            GetTopKAddOnsForMetricUsage),
                        RequiredInt32(
                            state,
                            2,
                            GetTopKAddOnsForMetricUsage)));
                return 1;
            case "IsEnabled":
                lua_pushboolean(state, profiler.Enabled ? 1 : 0);
                return 1;
            case "MeasureCall":
                return MeasureCall(state, profiler);
            default:
                return 0;
        }
    }

    private static int MeasureCall(
        lua_State state,
        WowAddOnProfilerState profiler)
    {
        var argumentCount = lua_gettop(state);
        if (argumentCount == 0 || lua_isfunction(state, 1) == 0)
            return luaL_error(state, MeasureCallUsage);

        lua_pushvalue(state, 1);
        for (var index = 2; index <= argumentCount; index++)
            lua_pushvalue(state, index);

        var measurement = profiler.BeginMeasurement();
        var status = lua_pcall(state, argumentCount - 1, LUA_MULTRET, 0);
        if (status != 0)
        {
            profiler.CancelMeasurement(measurement);
            return lua_error(state);
        }

        var results = profiler.EndMeasurement(measurement);
        var returnCount = lua_gettop(state) - argumentCount;
        PushCallResults(state, results);
        lua_insert(state, argumentCount + 1);
        return returnCount + 1;
    }

    private static WowAddOnPerformanceMessage RequiredPerformanceMessage(
        lua_State state)
    {
        if (lua_gettop(state) < 1 || lua_istable(state, 1) == 0)
        {
            luaL_error(state, AddPerformanceMessageShownUsage);
            return new WowAddOnPerformanceMessage(0, 0, null, 0, 0);
        }

        var type = (WowAddOnPerformanceMessageType)RequiredTableEnum(
            state,
            1,
            "type",
            2,
            AddPerformanceMessageShownUsage);
        var metric = (WowAddOnProfilerMetric)RequiredTableEnum(
            state,
            1,
            "metric",
            11,
            AddPerformanceMessageShownUsage);
        var addOnName = OptionalTableString(
            state,
            1,
            "addOnName",
            AddPerformanceMessageShownUsage);
        var metricValue = RequiredTableNumber(
            state,
            1,
            "metricValue",
            AddPerformanceMessageShownUsage);
        var thresholdValue = RequiredTableNumber(
            state,
            1,
            "thresholdValue",
            AddPerformanceMessageShownUsage);
        return new WowAddOnPerformanceMessage(
            type,
            metric,
            addOnName,
            metricValue,
            thresholdValue);
    }

    private static void PushPerformanceMessage(
        lua_State state,
        WowAddOnPerformanceMessage message)
    {
        lua_createtable(state, 0, 5);
        SetNumber(state, "type", (int)message.Type);
        SetNumber(state, "metric", (int)message.Metric);
        if (message.AddOnName is { } addOnName)
        {
            lua_pushstring(state, addOnName);
            lua_setfield(state, -2, "addOnName");
        }
        SetNumber(state, "metricValue", message.MetricValue);
        SetNumber(state, "thresholdValue", message.ThresholdValue);
    }

    private static void PushTopAddOns(
        lua_State state,
        IReadOnlyList<WowAddOnProfilerResult> results)
    {
        lua_createtable(state, results.Count, 0);
        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index];
            lua_createtable(state, 0, 2);
            lua_pushstring(state, result.AddOnName);
            lua_setfield(state, -2, "addOnName");
            SetNumber(state, "metricValue", result.MetricValue);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCallResults(
        lua_State state,
        WowAddOnProfilerCallResults results)
    {
        lua_createtable(state, 0, 5);
        SetNumber(state, "elapsedMilliseconds", results.ElapsedMilliseconds);
        SetNumber(state, "elapsedTicks", results.ElapsedTicks);
        SetNumber(state, "allocatedBytes", results.AllocatedBytes);
        SetNumber(state, "deallocatedBytes", results.DeallocatedBytes);

        lua_createtable(state, results.Events.Count, 0);
        for (var index = 0; index < results.Events.Count; index++)
        {
            var measuredEvent = results.Events[index];
            lua_createtable(state, 0, 5);
            lua_pushstring(state, measuredEvent.Name);
            lua_setfield(state, -2, "name");
            SetNumber(state, "allocatedBytes", measuredEvent.AllocatedBytes);
            SetNumber(state, "deallocatedBytes", measuredEvent.DeallocatedBytes);
            SetNumber(
                state,
                "elapsedMilliseconds",
                measuredEvent.ElapsedMilliseconds);
            SetNumber(state, "elapsedTicks", measuredEvent.ElapsedTicks);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "events");
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static WowAddOnProfilerMetric RequiredMetric(
        lua_State state,
        int index,
        string usage)
    {
        var metric = RequiredInt32(state, index, usage);
        if (metric is < 0 or >= WowAddOnProfilerState.MetricCount)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (WowAddOnProfilerMetric)metric;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)value;
    }

    private static int RequiredTableEnum(
        lua_State state,
        int tableIndex,
        string field,
        int maximum,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredInt32(state, -1, usage);
        lua_pop(state, 1);
        if (value < 0 || value > maximum)
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static string? OptionalTableString(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_isnil(state, -1) != 0)
        {
            lua_pop(state, 1);
            return null;
        }
        if (lua_isstring(state, -1) == 0)
        {
            luaL_error(state, usage);
            return null;
        }
        var value = lua_tostring(state, -1);
        lua_pop(state, 1);
        return value;
    }

    private static double RequiredTableNumber(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_isnumber(state, -1) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, -1);
        lua_pop(state, 1);
        if (!double.IsFinite(value))
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 3);
        SetNumber(state, "SpecificAddOnChatWarning", 0);
        SetNumber(state, "SpecificAddOnErrorDialog", 1);
        SetNumber(state, "OverallAddOnErrorDialog", 2);
        lua_setfield(state, -2, "AddOnPerformanceMessageType");

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", 3);
        SetNumber(state, "MinValue", 0);
        SetNumber(state, "MaxValue", 2);
        lua_setfield(state, -2, "AddOnPerformanceMessageTypeMeta");

        lua_createtable(state, 0, WowAddOnProfilerState.MetricCount);
        SetNumber(state, "SessionAverageTime", 0);
        SetNumber(state, "RecentAverageTime", 1);
        SetNumber(state, "EncounterAverageTime", 2);
        SetNumber(state, "LastTime", 3);
        SetNumber(state, "PeakTime", 4);
        SetNumber(state, "CountTimeOver1Ms", 5);
        SetNumber(state, "CountTimeOver5Ms", 6);
        SetNumber(state, "CountTimeOver10Ms", 7);
        SetNumber(state, "CountTimeOver50Ms", 8);
        SetNumber(state, "CountTimeOver100Ms", 9);
        SetNumber(state, "CountTimeOver500Ms", 10);
        SetNumber(state, "CountTimeOver1000Ms", 11);
        lua_setfield(state, -2, "AddOnProfilerMetric");

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", WowAddOnProfilerState.MetricCount);
        SetNumber(state, "MinValue", 0);
        SetNumber(state, "MaxValue", WowAddOnProfilerState.MetricCount - 1);
        lua_setfield(state, -2, "AddOnProfilerMetricMeta");
        lua_pop(state, 1);
    }

    private static void SetNumber(
        lua_State state,
        string name,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }
}
