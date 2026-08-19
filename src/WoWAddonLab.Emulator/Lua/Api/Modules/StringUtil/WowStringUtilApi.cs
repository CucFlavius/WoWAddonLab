using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed partial class WowStringUtilApi : LuaApiModule
{
    private const string MetatableName = "SecondsFormatter";
    private const int StorageMagic = 0x53464D54;

    private static readonly lua_CFunction GarbageCollectCallback = GarbageCollect;
    private static readonly lua_CFunction IndexCallback = Index;
    private static readonly lua_CFunction NewIndexCallback = NewIndex;
    private static readonly lua_CFunction EqualCallback = Equal;
    private static readonly lua_CFunction ToStringCallback = ToStringValue;
    private static readonly lua_CFunction DumpCallback = Dump;
    private static readonly lua_CFunction CreateSecondsFormatterCallback =
        CreateSecondsFormatter;

    private static readonly IReadOnlyDictionary<string, lua_CFunction> Methods =
        new Dictionary<string, lua_CFunction>(StringComparer.Ordinal)
        {
            ["FormatNumber"] = state => Dispatch(state, "FormatNumber"),
            ["CanApproximate"] = state => Dispatch(state, "CanApproximate"),
            ["CanRoundUpIntervals"] = state => Dispatch(state, "CanRoundUpIntervals"),
            ["CanRoundUpLastUnit"] = state => Dispatch(state, "CanRoundUpLastUnit"),
            ["EvaluateDesiredUnitCount"] = state => Dispatch(state, "EvaluateDesiredUnitCount"),
            ["EvaluateMaxInterval"] = state => Dispatch(state, "EvaluateMaxInterval"),
            ["EvaluateMinInterval"] = state => Dispatch(state, "EvaluateMinInterval"),
            ["Format"] = state => Dispatch(state, "Format"),
            ["FormatZero"] = state => Dispatch(state, "FormatZero"),
            ["GetApproximationSeconds"] = state => Dispatch(state, "GetApproximationSeconds"),
            ["GetRounding"] = state => Dispatch(state, "GetRounding"),
            ["SetRounding"] = state => Dispatch(state, "SetRounding"),
            ["GetConvertToLower"] = state => Dispatch(state, "GetConvertToLower"),
            ["GetDefaultAbbreviation"] = state => Dispatch(state, "GetDefaultAbbreviation"),
            ["GetDesiredUnitCount"] = state => Dispatch(state, "GetDesiredUnitCount"),
            ["GetDesiredUnitCountCurve"] = state => Dispatch(state, "GetDesiredUnitCountCurve"),
            ["GetMaxInterval"] = state => Dispatch(state, "GetMaxInterval"),
            ["GetMaxIntervalCurve"] = state => Dispatch(state, "GetMaxIntervalCurve"),
            ["GetMillisecondsThreshold"] = state => Dispatch(state, "GetMillisecondsThreshold"),
            ["GetMinInterval"] = state => Dispatch(state, "GetMinInterval"),
            ["GetMinIntervalCurve"] = state => Dispatch(state, "GetMinIntervalCurve"),
            ["GetStripIntervalWhitespace"] = state => Dispatch(state, "GetStripIntervalWhitespace"),
            ["Reset"] = state => Dispatch(state, "Reset"),
            ["SetApproximationSeconds"] = state => Dispatch(state, "SetApproximationSeconds"),
            ["SetCanRoundUpIntervals"] = state => Dispatch(state, "SetCanRoundUpIntervals"),
            ["SetCanRoundUpLastUnit"] = state => Dispatch(state, "SetCanRoundUpLastUnit"),
            ["SetConvertToLower"] = state => Dispatch(state, "SetConvertToLower"),
            ["SetDefaultAbbreviation"] = state => Dispatch(state, "SetDefaultAbbreviation"),
            ["SetDesiredUnitCount"] = state => Dispatch(state, "SetDesiredUnitCount"),
            ["SetDesiredUnitCountCurve"] = state => Dispatch(state, "SetDesiredUnitCountCurve"),
            ["SetMaxInterval"] = state => Dispatch(state, "SetMaxInterval"),
            ["SetMaxIntervalCurve"] = state => Dispatch(state, "SetMaxIntervalCurve"),
            ["SetMillisecondsThreshold"] = state => Dispatch(state, "SetMillisecondsThreshold"),
            ["SetMinInterval"] = state => Dispatch(state, "SetMinInterval"),
            ["SetMinIntervalCurve"] = state => Dispatch(state, "SetMinIntervalCurve"),
            ["SetStripIntervalWhitespace"] = state => Dispatch(state, "SetStripIntervalWhitespace")
        };

    private static readonly IntervalDescriptor[] Intervals =
    [
        new(1, "D_SECONDS", "SECONDS_ABBR", "SECOND_ONELETTER_ABBR",
            "%d |4second:seconds;", "%d sec", "%ds"),
        new(60, "D_MINUTES", "MINUTES_ABBR", "MINUTE_ONELETTER_ABBR",
            "%d |4minute:minutes;", "%d min", "%dm"),
        new(3_600, "D_HOURS", "HOURS_ABBR", "HOUR_ONELETTER_ABBR",
            "%d |4hour:hours;", "%d hr", "%dh"),
        new(86_400, "D_DAYS", "DAYS_ABBR", "DAY_ONELETTER_ABBR",
            "%d |4day:days;", "%d day", "%dd")
    ];

    [GeneratedRegex(@"%(?:\d+\$)?(?:[-+0 #]*\d*)?[diu]", RegexOptions.CultureInvariant)]
    private static partial Regex IntegerSpecifier();

    [GeneratedRegex(@"%(?:\d+\$)?(?:[-+0 #]*\d*)?(?:\.\d+)?[fFgG]", RegexOptions.CultureInvariant)]
    private static partial Regex FloatSpecifier();

    public override void Register(lua_State state)
    {
        RegisterMetatable(state);
        WowAbbreviatedNumberFormatterApi.Register(state);
        WowNumericRuleFormatterApi.Register(state);
        RegisterEnums(state);

        lua_getglobal(state, "C_StringUtil");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }
        lua_pushcfunction(state, CreateSecondsFormatterCallback);
        lua_setfield(state, -2, "CreateSecondsFormatter");
        WowAbbreviatedNumberFormatterApi.RegisterFactory(state);
        WowNumericRuleFormatterApi.RegisterFactory(state);
        lua_setglobal(state, "C_StringUtil");
    }

    internal static bool TryReadFormatter(
        lua_State state,
        int index,
        out IWowNumericFormatterState? formatter)
    {
        if (TryReadSecondsFormatter(state, index, out var secondsFormatter))
        {
            formatter = secondsFormatter;
            return true;
        }
        if (WowAbbreviatedNumberFormatterApi.TryRead(state, index, out formatter))
            return true;
        return WowNumericRuleFormatterApi.TryRead(state, index, out formatter);
    }

    private static bool TryReadSecondsFormatter(
        lua_State state,
        int index,
        out WowSecondsFormatterState? formatter)
    {
        formatter = null;
        unsafe
        {
            if (!TryGetStorage(state, index, out var storage) ||
                storage->StateHandle == IntPtr.Zero)
            {
                return false;
            }

            formatter = GCHandle.FromIntPtr(storage->StateHandle).Target
                as WowSecondsFormatterState;
            return formatter is not null;
        }
    }

    internal static string Format(
        lua_State state,
        IWowNumericFormatterState formatter,
        double value) => formatter switch
        {
            WowSecondsFormatterState secondsFormatter => FormatCore(
                LuaBindings.GetRuntime(state),
                secondsFormatter,
                value,
                secondsFormatter.DefaultAbbreviation),
            WowAbbreviatedNumberFormatterState abbreviatedFormatter =>
                WowAbbreviatedNumberFormatterApi.Format(
                    LuaBindings.GetRuntime(state),
                    abbreviatedFormatter,
                    value),
            WowNumericRuleFormatterState numericRuleFormatter =>
                WowNumericRuleFormatterApi.Format(
                    state,
                    numericRuleFormatter,
                    value),
            _ => value.ToString("G14", CultureInfo.InvariantCulture)
        };

    private static void RegisterMetatable(lua_State state)
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

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        SetEnum(state, "SecondsFormatterAbbreviation",
            ("None", 0), ("Truncate", 1), ("OneLetter", 2));
        SetEnumMeta(state, "SecondsFormatterAbbreviationMeta", 3, 0, 2);
        SetEnum(state, "SecondsFormatterInterval",
            ("Seconds", 0), ("Minutes", 1), ("Hours", 2), ("Days", 3));
        SetEnumMeta(state, "SecondsFormatterIntervalMeta", 4, 0, 3);
        SetEnum(state, "SecondsFormatterRounding",
            ("RoundUp", 0), ("Truncate", 1));
        SetEnumMeta(state, "SecondsFormatterRoundingMeta", 2, 0, 1);
        SetEnum(state, "SecondsFormatterIntervalWhitespace",
            ("Preserve", 0), ("Strip", 1), ("StripIgnoreLocale", 2));
        SetEnumMeta(state, "SecondsFormatterIntervalWhitespaceMeta", 3, 0, 2);
        lua_setglobal(state, "Enum");
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] values)
    {
        lua_createtable(state, 0, values.Length);
        foreach (var value in values)
        {
            lua_pushinteger(state, value.Value);
            lua_setfield(state, -2, value.Name);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int count,
        int minimum,
        int maximum)
    {
        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", count);
        SetInteger(state, "MinValue", minimum);
        SetInteger(state, "MaxValue", maximum);
        lua_setfield(state, -2, name);
    }

    private static int CreateSecondsFormatter(lua_State state)
    {
        Push(state, new WowSecondsFormatterState());
        return 1;
    }

    private static int Dispatch(lua_State state, string operation)
    {
        var usage = Usage(operation);
        if (!TryReadSecondsFormatter(state, 1, out var formatter))
            return luaL_error(state, usage);

        var runtime = LuaBindings.GetRuntime(state);
        switch (operation)
        {
            case "FormatNumber":
            case "Format":
                if (lua_gettop(state) < 2 ||
                    operation == "FormatNumber" && lua_gettop(state) != 2 ||
                    operation == "Format" && lua_gettop(state) > 3 ||
                    !TryReadFiniteNumber(state, 2, out var seconds) ||
                    !TryReadOptionalEnum(
                        state,
                        3,
                        (byte)formatter!.DefaultAbbreviation,
                        2,
                        out var abbreviation))
                {
                    return luaL_error(state, usage);
                }
                lua_pushstring(
                    state,
                    FormatCore(
                        runtime,
                        formatter,
                        seconds,
                        (SecondsFormatterAbbreviation)abbreviation));
                return 1;
            case "FormatZero":
                if (lua_gettop(state) is < 1 or > 2 ||
                    !TryReadOptionalEnum(
                        state,
                        2,
                        (byte)formatter!.DefaultAbbreviation,
                        2,
                        out var zeroAbbreviation))
                {
                    return luaL_error(state, usage);
                }
                lua_pushstring(
                    state,
                    FormatZero(
                        runtime,
                        formatter,
                        (SecondsFormatterAbbreviation)zeroAbbreviation));
                return 1;
            case "CanApproximate":
                if (!TryReadUnaryNumber(state, out var approximateSeconds))
                    return luaL_error(state, usage);
                lua_pushboolean(
                    state,
                    approximateSeconds > 0 &&
                    approximateSeconds < formatter!.ApproximationSeconds
                        ? 1
                        : 0);
                return 1;
            case "EvaluateDesiredUnitCount":
                if (!TryReadUnaryNumber(state, out var desiredSeconds))
                    return luaL_error(state, usage);
                lua_pushnumber(state, EvaluateDesiredUnitCount(formatter!, desiredSeconds));
                return 1;
            case "EvaluateMaxInterval":
                if (!TryReadUnaryNumber(state, out var maxSeconds))
                    return luaL_error(state, usage);
                lua_pushinteger(state, EvaluateInterval(formatter!.MaxInterval, maxSeconds));
                return 1;
            case "EvaluateMinInterval":
                if (!TryReadUnaryNumber(state, out var minSeconds))
                    return luaL_error(state, usage);
                lua_pushinteger(state, EvaluateInterval(formatter!.MinInterval, minSeconds));
                return 1;
            case "CanRoundUpIntervals":
                return PushBooleanGetter(state, usage, formatter!.CanRoundUpIntervals);
            case "CanRoundUpLastUnit":
                return PushBooleanGetter(state, usage, formatter!.CanRoundUpLastUnit);
            case "GetConvertToLower":
                return PushBooleanGetter(state, usage, formatter!.ConvertToLower);
            case "GetApproximationSeconds":
                return PushNumberGetter(state, usage, formatter!.ApproximationSeconds);
            case "GetMillisecondsThreshold":
                return PushNumberGetter(state, usage, formatter!.MillisecondsThreshold);
            case "GetDefaultAbbreviation":
                return PushIntegerGetter(state, usage, (byte)formatter!.DefaultAbbreviation);
            case "GetStripIntervalWhitespace":
                return PushIntegerGetter(state, usage, (byte)formatter!.Whitespace);
            case "GetDesiredUnitCount":
                return PushStaticValue(state, usage, formatter!.DesiredUnitCount);
            case "GetMaxInterval":
                return PushStaticValue(state, usage, formatter!.MaxInterval);
            case "GetMinInterval":
                return PushStaticValue(state, usage, formatter!.MinInterval);
            case "GetDesiredUnitCountCurve":
                return PushCurveValue(state, usage, formatter!.DesiredUnitCount);
            case "GetMaxIntervalCurve":
                return PushCurveValue(state, usage, formatter!.MaxInterval);
            case "GetMinIntervalCurve":
                return PushCurveValue(state, usage, formatter!.MinInterval);
            case "Reset":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                formatter!.Reset(runtime);
                return 0;
            case "SetApproximationSeconds":
                if (!TryReadUnaryNumber(state, out var newApproximation))
                    return luaL_error(state, usage);
                formatter!.ApproximationSeconds = newApproximation;
                return 0;
            case "SetMillisecondsThreshold":
                if (!TryReadUnaryNumber(state, out var newThreshold))
                    return luaL_error(state, usage);
                formatter!.MillisecondsThreshold = newThreshold;
                return 0;
            case "SetCanRoundUpIntervals":
                if (!TryReadRequiredBoolean(state, out var roundIntervals))
                    return luaL_error(state, usage);
                formatter!.CanRoundUpIntervals = roundIntervals;
                return 0;
            case "SetCanRoundUpLastUnit":
                if (!TryReadRequiredBoolean(state, out var roundLast))
                    return luaL_error(state, usage);
                formatter!.CanRoundUpLastUnit = roundLast;
                return 0;
            case "SetConvertToLower":
                if (!TryReadRequiredBoolean(state, out var lower))
                    return luaL_error(state, usage);
                formatter!.ConvertToLower = lower;
                return 0;
            case "SetDefaultAbbreviation":
                if (!TryReadRequiredEnum(state, 2, 2, out var newAbbreviation))
                    return luaL_error(state, usage);
                formatter!.DefaultAbbreviation =
                    (SecondsFormatterAbbreviation)newAbbreviation;
                return 0;
            case "SetStripIntervalWhitespace":
                if (!TryReadRequiredEnum(state, 2, 2, out var whitespace))
                    return luaL_error(state, usage);
                formatter!.Whitespace = (SecondsFormatterIntervalWhitespace)whitespace;
                return 0;
            case "SetRounding":
                if (!TryReadRequiredEnum(state, 2, 1, out var rounding))
                    return luaL_error(state, usage);
                formatter!.Rounding = (SecondsFormatterRounding)rounding;
                return 0;
            case "GetRounding":
                lua_pushinteger(state, (int)formatter!.Rounding);
                return 1;
            case "SetDesiredUnitCount":
                if (!TryReadRequiredByte(state, 2, out var count))
                    return luaL_error(state, usage);
                formatter!.DesiredUnitCount.SetStatic(runtime, count);
                return 0;
            case "SetMaxInterval":
                if (!TryReadRequiredEnum(state, 2, 3, out var maxInterval))
                    return luaL_error(state, usage);
                formatter!.MaxInterval.SetStatic(runtime, maxInterval);
                return 0;
            case "SetMinInterval":
                if (!TryReadRequiredEnum(state, 2, 3, out var minInterval))
                    return luaL_error(state, usage);
                formatter!.MinInterval.SetStatic(runtime, minInterval);
                return 0;
            case "SetDesiredUnitCountCurve":
                return SetCurve(state, usage, formatter!.DesiredUnitCount);
            case "SetMaxIntervalCurve":
                return SetCurve(state, usage, formatter!.MaxInterval);
            case "SetMinIntervalCurve":
                return SetCurve(state, usage, formatter!.MinInterval);
            default:
                return 0;
        }
    }

    private static int SetCurve(
        lua_State state,
        string usage,
        WowSecondsFormatterValue value)
    {
        if (lua_gettop(state) != 2 ||
            !WowCurveApi.TryRead(state, 2, out var curve))
        {
            return luaL_error(state, usage);
        }

        var runtime = LuaBindings.GetRuntime(state);
        value.SetCurve(runtime, curve!, LuaRuntime.CaptureValue(state, 2));
        return 0;
    }

    private static int PushBooleanGetter(
        lua_State state,
        string usage,
        bool value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushNumberGetter(
        lua_State state,
        string usage,
        double value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        lua_pushnumber(state, value);
        return 1;
    }

    private static int PushIntegerGetter(
        lua_State state,
        string usage,
        int value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        lua_pushinteger(state, value);
        return 1;
    }

    private static int PushStaticValue(
        lua_State state,
        string usage,
        WowSecondsFormatterValue value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        if (value.Curve is null)
            lua_pushnumber(state, value.StaticValue);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int PushCurveValue(
        lua_State state,
        string usage,
        WowSecondsFormatterValue value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        if (value.Curve is null || value.CurveReference <= 0)
            lua_pushnil(state);
        else
            lua_rawgeti(state, LUA_REGISTRYINDEX, value.CurveReference);
        return 1;
    }

    private static string FormatCore(
        LuaRuntime runtime,
        WowSecondsFormatterState formatter,
        double rawSeconds,
        SecondsFormatterAbbreviation abbreviation)
    {
        if (rawSeconds < formatter.MillisecondsThreshold)
        {
            var template = GlobalString(runtime, "COOLDOWN_DURATION_TEN_SEC", "%.1f");
            return ReplaceFloat(template, rawSeconds);
        }

        var seconds = Math.Ceiling(rawSeconds);
        if (seconds <= 0)
            return FormatZero(runtime, formatter, abbreviation);

        var minInterval = (int)EvaluateInterval(formatter.MinInterval, seconds);
        var maxInterval = Math.Max(
            minInterval,
            (int)EvaluateInterval(formatter.MaxInterval, seconds));

        if (seconds > 0 && seconds < formatter.ApproximationSeconds)
        {
            var interval = Math.Max(minInterval, (int)SecondsFormatterInterval.Minutes);
            while (interval < maxInterval &&
                   seconds > Intervals[interval + 1].Seconds)
            {
                interval++;
            }

            var units = (long)Math.Ceiling(seconds / Intervals[interval].Seconds);
            var formatted = FormatUnit(runtime, formatter, interval, abbreviation, units);
            return ReplaceString(
                GlobalString(runtime, "LESS_THAN_OPERAND", "< %s"),
                formatted);
        }

        var desiredCount = EvaluateDesiredUnitCount(formatter, seconds);
        var intervalUnits = new long[Intervals.Length];
        var appendedCount = 0;
        for (var interval = maxInterval;
             appendedCount < desiredCount && interval >= minInterval;
             interval--)
        {
            var intervalSeconds = Intervals[interval].Seconds;
            if (seconds < intervalSeconds)
                continue;

            appendedCount++;
            var quotient = seconds / intervalSeconds;
            if (quotient <= 0)
                break;
            intervalUnits[interval] =
                formatter.CanRoundUpLastUnit &&
                (interval == minInterval || appendedCount == desiredCount)
                    ? (long)Math.Ceiling(quotient)
                    : (long)Math.Floor(quotient);
            seconds %= intervalSeconds;
        }

        if (formatter.CanRoundUpIntervals)
        {
            for (var interval = minInterval; interval < maxInterval; interval++)
            {
                if (intervalUnits[interval] != Intervals[interval].Seconds)
                    continue;
                intervalUnits[interval + 1]++;
                intervalUnits[interval] = 0;
            }
        }

        var output = new List<string>(Intervals.Length);
        for (var interval = maxInterval; interval >= minInterval; interval--)
        {
            if (intervalUnits[interval] > 0)
            {
                output.Add(
                    FormatUnit(
                        runtime,
                        formatter,
                        interval,
                        abbreviation,
                        intervalUnits[interval]));
            }
        }

        return output.Count == 0
            ? FormatZero(runtime, formatter, abbreviation)
            : string.Join(GlobalString(runtime, "TIME_UNIT_DELIMITER", " "), output);
    }

    private static string FormatZero(
        LuaRuntime runtime,
        WowSecondsFormatterState formatter,
        SecondsFormatterAbbreviation abbreviation) =>
        FormatUnit(
            runtime,
            formatter,
            EvaluateInterval(formatter.MinInterval, 0),
            abbreviation,
            0);

    private static string FormatUnit(
        LuaRuntime runtime,
        WowSecondsFormatterState formatter,
        int interval,
        SecondsFormatterAbbreviation abbreviation,
        long units)
    {
        var descriptor = Intervals[interval];
        var (key, fallback) = abbreviation switch
        {
            SecondsFormatterAbbreviation.Truncate =>
                (descriptor.AbbreviatedKey, descriptor.AbbreviatedFallback),
            SecondsFormatterAbbreviation.OneLetter =>
                (descriptor.OneLetterKey, descriptor.OneLetterFallback),
            _ => (descriptor.FullKey, descriptor.FullFallback)
        };
        var template = GlobalString(runtime, key, fallback);
        if (formatter.ConvertToLower)
            template = template.ToLowerInvariant();
        if (formatter.Whitespace == SecondsFormatterIntervalWhitespace.StripIgnoreLocale ||
            formatter.Whitespace == SecondsFormatterIntervalWhitespace.Strip &&
            runtime.Localization.CurrentLocale is not WowClientLocale.DeDE and
                not WowClientLocale.RuRU)
        {
            template = template.Replace(" ", string.Empty, StringComparison.Ordinal);
        }
        return WowTextMarkup.ProcessStoredGrammar(ReplaceInteger(template, units));
    }

    private static byte EvaluateDesiredUnitCount(
        WowSecondsFormatterState formatter,
        double seconds) =>
        formatter.DesiredUnitCount.Curve is { } curve
            ? unchecked((byte)(int)WowCurveApi.Evaluate(curve, (float)seconds))
            : formatter.DesiredUnitCount.StaticValue;

    private static byte EvaluateInterval(
        WowSecondsFormatterValue value,
        double seconds)
    {
        var result = value.Curve is { } curve
            ? unchecked((byte)(int)WowCurveApi.Evaluate(curve, (float)seconds))
            : value.StaticValue;
        return result <= (byte)SecondsFormatterInterval.Days ? result : (byte)0;
    }

    private static string GlobalString(
        LuaRuntime runtime,
        string key,
        string fallback) =>
        runtime.GlobalStringProvider?.Strings.TryGetValue(key, out var value) == true
            ? value
            : fallback;

    private static string ReplaceInteger(string format, long value)
    {
        var replacement = value.ToString(CultureInfo.InvariantCulture);
        return IntegerSpecifier().Replace(format, replacement, 1);
    }

    private static string ReplaceFloat(string format, double value)
    {
        var match = FloatSpecifier().Match(format);
        if (!match.Success)
            return format;
        var decimals = 1;
        var dot = match.Value.IndexOf('.');
        if (dot >= 0)
        {
            var end = match.Value.Length - 1;
            if (int.TryParse(
                    match.Value.AsSpan(dot + 1, end - dot - 1),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var parsed))
            {
                decimals = parsed;
            }
        }
        return format.Remove(match.Index, match.Length).Insert(
            match.Index,
            value.ToString($"F{decimals}", CultureInfo.InvariantCulture));
    }

    private static string ReplaceString(string format, string value)
    {
        foreach (var token in new[] { "%1$s", "%s" })
        {
            var index = format.IndexOf(token, StringComparison.Ordinal);
            if (index >= 0)
                return format.Remove(index, token.Length).Insert(index, value);
        }
        return format;
    }

    private static bool TryReadUnaryNumber(lua_State state, out double value)
    {
        value = 0;
        return lua_gettop(state) == 2 &&
               TryReadFiniteNumber(state, 2, out value);
    }

    private static bool TryReadFiniteNumber(
        lua_State state,
        int index,
        out double value)
    {
        value = 0;
        if (lua_isnumber(state, index) == 0)
            return false;
        value = lua_tonumber(state, index);
        return double.IsFinite(value);
    }

    private static bool TryReadRequiredBoolean(lua_State state, out bool value)
    {
        value = false;
        if (lua_gettop(state) != 2 || lua_isnil(state, 2) != 0)
            return false;
        value = lua_toboolean(state, 2) != 0;
        return true;
    }

    private static bool TryReadRequiredByte(
        lua_State state,
        int index,
        out byte value)
    {
        value = 0;
        if (lua_gettop(state) != index ||
            !TryReadFiniteNumber(state, index, out var number) ||
            number is < 0 or > byte.MaxValue)
        {
            return false;
        }
        value = (byte)number;
        return true;
    }

    private static bool TryReadRequiredEnum(
        lua_State state,
        int index,
        byte maximum,
        out byte value)
    {
        value = 0;
        if (lua_gettop(state) != index ||
            !TryReadFiniteNumber(state, index, out var number) ||
            number is < int.MinValue or > int.MaxValue)
        {
            return false;
        }
        value = unchecked((byte)(int)number);
        return value <= maximum;
    }

    private static bool TryReadOptionalEnum(
        lua_State state,
        int index,
        byte fallback,
        byte maximum,
        out byte value)
    {
        value = fallback;
        var top = lua_gettop(state);
        if (top < index || top == index && lua_isnil(state, index) != 0)
            return true;
        return top == index && TryReadRequiredEnum(state, index, maximum, out value);
    }

    private static unsafe void Push(
        lua_State state,
        WowSecondsFormatterState formatter)
    {
        var runtime = LuaBindings.GetRuntime(state);
        lua_newtable(state);
        var propertyTableReference = LuaRuntime.CaptureValue(state, -1);
        lua_pop(state, 1);

        var storage = (FormatterStorage*)lua_newuserdata(
            state,
            (UIntPtr)sizeof(FormatterStorage));
        storage->Magic = StorageMagic;
        storage->PropertyTableReference = propertyTableReference;
        storage->StateHandle = GCHandle.ToIntPtr(GCHandle.Alloc(formatter));
        luaL_getmetatable(state, MetatableName);
        lua_setmetatable(state, -2);
    }

    private static int GarbageCollect(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return 0;
            if (LuaBindings.TryGetRuntime(state, out var runtime))
            {
                runtime!.ReleaseReference(storage->PropertyTableReference);
                if (storage->StateHandle != IntPtr.Zero &&
                    GCHandle.FromIntPtr(storage->StateHandle).Target is
                        WowSecondsFormatterState formatter)
                {
                    formatter.ReleaseCurveReferences(runtime);
                }
            }
            storage->PropertyTableReference = 0;
            if (storage->StateHandle != IntPtr.Zero)
            {
                var handle = GCHandle.FromIntPtr(storage->StateHandle);
                if (handle.IsAllocated)
                    handle.Free();
                storage->StateHandle = IntPtr.Zero;
            }
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
            $"SecondsFormatter: 0x{lua_topointer(state, 1).ToUInt64():X}");
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
        out FormatterStorage* storage)
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
        storage = (FormatterStorage*)lua_touserdata(state, index);
        return storage is not null && storage->Magic == StorageMagic;
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static string LuaKeyText(lua_State state, int index) =>
        lua_type(state, index) == LUA_TSTRING
            ? lua_tostring(state, index) ?? string.Empty
            : lua_typename(state, lua_type(state, index)) ?? "unknown";

    private static string Usage(string operation) => operation switch
    {
        "FormatNumber" => "Usage: local formattedNumber = self:FormatNumber(number)",
        "CanApproximate" => "Usage: local canApproximate = self:CanApproximate(seconds)",
        "EvaluateDesiredUnitCount" => "Usage: local count = self:EvaluateDesiredUnitCount(seconds)",
        "EvaluateMaxInterval" => "Usage: local interval = self:EvaluateMaxInterval(seconds)",
        "EvaluateMinInterval" => "Usage: local interval = self:EvaluateMinInterval(seconds)",
        "Format" => "Usage: local formattedSeconds = self:Format(seconds [, abbreviation])",
        "FormatZero" => "Usage: local formattedSeconds = self:FormatZero([abbreviation])",
        var name when name.StartsWith("Set", StringComparison.Ordinal) =>
            $"Usage: self:{name}(value)",
        var name when name is "Reset" => "Usage: self:Reset()",
        var name => $"Usage: local value = self:{name}()"
    };

    private readonly record struct IntervalDescriptor(
        int Seconds,
        string FullKey,
        string AbbreviatedKey,
        string OneLetterKey,
        string FullFallback,
        string AbbreviatedFallback,
        string OneLetterFallback);

    [StructLayout(LayoutKind.Sequential)]
    private struct FormatterStorage
    {
        public int Magic;
        public int PropertyTableReference;
        public IntPtr StateHandle;
    }
}
