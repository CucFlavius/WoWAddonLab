using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDateAndTimeApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AdjustTimeByDays",
        "AdjustTimeByMinutes",
        "AdjustTimeByMonths",
        "CompareCalendarTime",
        "GetCalendarTimeFromEpoch",
        "GetCurrentCalendarTime",
        "GetSecondsUntilDailyReset",
        "GetSecondsUntilWeeklyReset",
        "GetServerTimeLocal",
        "GetWeeklyResetStartTime"
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
        lua_setglobal(state, "C_DateAndTime");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetCurrentCalendarTime":
                PushCalendarTime(
                    state,
                    CalendarTimeValue.FromDateTime(
                        runtime.DateAndTime.CurrentTime.LocalDateTime));
                return 1;
            case "GetCalendarTimeFromEpoch":
            {
                var epochMicroseconds = RequiredEpochTime(
                    state,
                    "Usage: local date = C_DateAndTime.GetCalendarTimeFromEpoch(epoch)");
                var epoch = DateTimeOffset.FromUnixTimeSeconds(
                    checked((long)(epochMicroseconds / 1_000_000)));
                var localTime = epoch.UtcDateTime + runtime.DateAndTime.LocalUtcOffset;
                PushCalendarTime(
                    state,
                    CalendarTimeValue.FromDateTime(localTime));
                return 1;
            }
            case "AdjustTimeByDays":
            {
                const string usage =
                    "Usage: local newDate = C_DateAndTime.AdjustTimeByDays(date, days)";
                var value = ReadCalendarTime(state, 1, usage);
                PushCalendarTime(
                    state,
                    Adjust(
                        value,
                        RequiredInt32(state, 2, usage),
                        static (date, amount) => date.AddDays(amount)));
                return 1;
            }
            case "AdjustTimeByMinutes":
            {
                const string usage =
                    "Usage: local newDate = C_DateAndTime.AdjustTimeByMinutes(date, minutes)";
                var value = ReadCalendarTime(state, 1, usage);
                PushCalendarTime(
                    state,
                    Adjust(
                        value,
                        RequiredInt32(state, 2, usage),
                        static (date, amount) => date.AddMinutes(amount)));
                return 1;
            }
            case "AdjustTimeByMonths":
            {
                const string usage =
                    "Usage: local newDate = C_DateAndTime.AdjustTimeByMonths(date, months)";
                var value = ReadCalendarTime(state, 1, usage);
                PushCalendarTime(
                    state,
                    Adjust(
                        value,
                        RequiredInt32(state, 2, usage),
                        static (date, amount) => date.AddMonths(amount)));
                return 1;
            }
            case "CompareCalendarTime":
            {
                const string usage =
                    "Usage: local comparison = C_DateAndTime.CompareCalendarTime(" +
                    "lhsCalendarTime, rhsCalendarTime)";
                var left = ReadCalendarTime(state, 1, usage);
                var right = ReadCalendarTime(state, 2, usage);
                lua_pushinteger(state, Compare(left, right));
                return 1;
            }
            case "GetSecondsUntilDailyReset":
                lua_pushnumber(state, runtime.DateAndTime.SecondsUntilDailyReset);
                return 1;
            case "GetSecondsUntilWeeklyReset":
                lua_pushnumber(state, runtime.DateAndTime.SecondsUntilWeeklyReset);
                return 1;
            case "GetServerTimeLocal":
                lua_pushnumber(state, runtime.DateAndTime.CurrentTime.ToUnixTimeSeconds());
                return 1;
            case "GetWeeklyResetStartTime":
                lua_pushnumber(state, runtime.DateAndTime.WeeklyResetStartTime);
                return 1;
            default:
                return 0;
        }
    }

    private static CalendarTimeValue ReadCalendarTime(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TTABLE)
            luaL_error(state, usage);

        return new CalendarTimeValue(
            RequiredOneBasedUInt32Field(state, index, "monthDay", usage),
            RequiredOneBasedUInt32Field(state, index, "month", usage),
            RequiredOneBasedUInt32Field(state, index, "weekday", usage),
            RequiredInt32Field(state, index, "year", usage),
            RequiredInt32Field(state, index, "hour", usage),
            RequiredInt32Field(state, index, "minute", usage));
    }

    private static uint RequiredOneBasedUInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        tableIndex = tableIndex < 0 ? lua_gettop(state) + tableIndex + 1 : tableIndex;
        lua_getfield(state, tableIndex, field);
        var value = RequiredUInt32AtTop(state, usage);
        lua_pop(state, 1);
        return value;
    }

    private static int RequiredInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        tableIndex = tableIndex < 0 ? lua_gettop(state) + tableIndex + 1 : tableIndex;
        lua_getfield(state, tableIndex, field);
        var value = RequiredInt32AtTop(state, usage);
        lua_pop(state, 1);
        return value;
    }

    private static void PushCalendarTime(
        lua_State state,
        CalendarTimeValue value)
    {
        lua_newtable(state);
        SetInteger(state, "year", value.Year);
        SetUnsignedInteger(state, "month", value.Month);
        SetUnsignedInteger(state, "monthDay", value.MonthDay);
        SetUnsignedInteger(state, "weekday", value.Weekday);
        SetInteger(state, "hour", value.Hour);
        SetInteger(state, "minute", value.Minute);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetUnsignedInteger(
        lua_State state,
        string name,
        uint value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            luaL_error(state, usage);

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            luaL_error(state, usage);
        return (int)value;
    }

    private static int RequiredInt32AtTop(lua_State state, string usage)
    {
        if (lua_isnumber(state, -1) == 0)
            luaL_error(state, usage);

        var value = lua_tonumber(state, -1);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            luaL_error(state, usage);
        return (int)value;
    }

    private static uint RequiredUInt32AtTop(lua_State state, string usage)
    {
        if (lua_isnumber(state, -1) == 0)
            luaL_error(state, usage);

        var value = lua_tonumber(state, -1);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            luaL_error(state, usage);
        return (uint)value;
    }

    private static ulong RequiredEpochTime(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            luaL_error(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < 0 || value > 9_007_199_254_740_991d)
            luaL_error(state, usage);
        return (ulong)value;
    }

    private static CalendarTimeValue Adjust(
        CalendarTimeValue value,
        int amount,
        Func<DateTime, int, DateTime> adjustment)
    {
        if (!value.TryGetDateTime(out var date))
            return value;

        try
        {
            return CalendarTimeValue.FromDateTime(adjustment(date, amount));
        }
        catch (ArgumentOutOfRangeException)
        {
            return value;
        }
    }

    private static int Compare(
        CalendarTimeValue left,
        CalendarTimeValue right)
    {
        var pairs = new (int Left, int Right)[]
        {
            (left.Year, right.Year),
            (left.SignedMonth, right.SignedMonth),
            (left.SignedMonthDay, right.SignedMonthDay),
            (left.SignedWeekday, right.SignedWeekday),
            (left.Hour, right.Hour),
            (left.Minute, right.Minute)
        };

        foreach (var pair in pairs)
        {
            if (pair.Left < 0 || pair.Right < 0)
                continue;
            if (pair.Left < pair.Right)
                return 1;
            if (pair.Left > pair.Right)
                return -1;
        }
        return 0;
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        lua_newtable(state);
        SetInteger(state, "GlueScreenShortcut", 1);
        SetInteger(state, "WeeklyReset", 2);
        SetInteger(state, "GlobalLaunch", 4);
        lua_setfield(state, -2, "TimeEventFlag");

        lua_newtable(state);
        SetInteger(state, "NumValues", 3);
        SetInteger(state, "MinValue", 1);
        SetInteger(state, "MaxValue", 4);
        lua_setfield(state, -2, "TimeEventFlagMeta");
        lua_setglobal(state, "Enum");
    }

    private readonly record struct CalendarTimeValue(
        uint MonthDay,
        uint Month,
        uint Weekday,
        int Year,
        int Hour,
        int Minute)
    {
        public int SignedMonthDay => unchecked((int)(MonthDay - 1));
        public int SignedMonth => unchecked((int)(Month - 1));
        public int SignedWeekday => unchecked((int)(Weekday - 1));

        public bool TryGetDateTime(out DateTime value)
        {
            try
            {
                value = new DateTime(
                    Year,
                    checked((int)Month),
                    checked((int)MonthDay),
                    Hour,
                    Minute,
                    0,
                    DateTimeKind.Unspecified);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                value = default;
                return false;
            }
            catch (OverflowException)
            {
                value = default;
                return false;
            }
        }

        public static CalendarTimeValue FromDateTime(DateTime value) =>
            new(
                (uint)value.Day,
                (uint)value.Month,
                (uint)value.DayOfWeek + 1,
                value.Year,
                value.Hour,
                value.Minute);
    }
}
