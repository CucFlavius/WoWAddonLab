using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLocalizationApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        foreach (var function in new[]
                 {
                     "BreakUpLargeNumbers",
                     "AbbreviateLargeNumbers",
                     "AbbreviateNumbers",
                     "GetAvailableLocales",
                     "GetAvailableLocaleInfo",
                     "GetCurrentRegion",
                     "GetLocale",
                     "GetOSLocale",
                     "IsEuropeanNumbers",
                     "SetEuropeanNumbers"
                 })
        {
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var localization = LuaBindings.GetRuntime(state).Localization;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "BreakUpLargeNumbers":
            {
                const string usage =
                    "Usage: local result = " +
                    "BreakUpLargeNumbers(largeNumber [, natural])";
                var value = RequireFiniteNumber(state, usage);
                var natural = ReadOptionalTruthiness(state, 2);
                lua_pushstring(
                    state,
                    natural
                        ? AbbreviateLargeNumber(value)
                        : FormatNumber(value, localization.EuropeanNumbers));
                return 1;
            }
            case "AbbreviateLargeNumbers":
            case "AbbreviateNumbers":
            {
                var usage = operation == "AbbreviateLargeNumbers"
                    ? "Usage: local result = " +
                      "AbbreviateLargeNumbers(number [, options])"
                    : "Usage: local result = " +
                      "AbbreviateNumbers(number [, options])";
                var value = RequireFiniteNumber(state, usage);
                RequireOptionalOptionsTable(state, usage);
                lua_pushstring(state, AbbreviateLargeNumber(value));
                return 1;
            }
            case "GetAvailableLocales":
                ReadOptionalTruthiness(state, 1);
                foreach (var locale in localization.AvailableLocales)
                    lua_pushstring(state, locale);
                return localization.AvailableLocales.Count;
            case "GetAvailableLocaleInfo":
                ReadOptionalTruthiness(state, 1);
                lua_newtable(state);
                for (var index = 0; index < localization.AvailableLocales.Count; index++)
                {
                    var locale = localization.AvailableLocales[index];
                    lua_newtable(state);
                    lua_pushnumber(
                        state,
                        localization.LocaleIds.GetValueOrDefault(locale));
                    lua_setfield(state, -2, "localeId");
                    lua_pushstring(state, locale);
                    lua_setfield(state, -2, "localeName");
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetCurrentRegion":
                lua_pushnumber(state, localization.CurrentRegion);
                return 1;
            case "GetLocale":
                PushOptionalLocaleName(state, localization.CurrentLocale);
                return 1;
            case "GetOSLocale":
                PushOptionalLocaleName(state, localization.OsLocale);
                return 1;
            case "IsEuropeanNumbers":
                lua_pushboolean(state, localization.EuropeanNumbers ? 1 : 0);
                return 1;
            case "SetEuropeanNumbers":
                RequireValue(
                    state,
                    1,
                    "Usage: SetEuropeanNumbers(enabled)");
                localization.EuropeanNumbers = lua_toboolean(state, 1) != 0;
                return 0;
            default:
                return 0;
        }
    }

    internal static string FormatNumber(double value, bool europeanNumbers)
    {
        var negative = value < 0;
        var magnitude = Math.Abs(value);
        string result;
        if (magnitude < 1000)
        {
            result = magnitude.ToString(
                magnitude == Math.Truncate(magnitude) ? "0" : "0.0",
                CultureInfo.InvariantCulture);
            if (europeanNumbers)
                result = result.Replace('.', ',');
        }
        else
        {
            var digits = Math.Truncate(magnitude)
                .ToString("0", CultureInfo.InvariantCulture);
            var separator = europeanNumbers ? "." : ",";
            for (var insertAt = digits.Length - 3;
                 insertAt > 0;
                 insertAt -= 3)
            {
                digits = digits.Insert(insertAt, separator);
            }
            result = digits;
        }
        return negative ? $"-{result}" : result;
    }

    internal static string AbbreviateLargeNumber(double value)
    {
        var magnitude = Math.Abs(value);
        var (divisor, suffix) = magnitude switch
        {
            >= 1_000_000_000_000 => (1_000_000_000_000d, "T"),
            >= 1_000_000_000 => (1_000_000_000d, "B"),
            >= 1_000_000 => (1_000_000d, "M"),
            >= 1_000 => (1_000d, "K"),
            _ => (1d, string.Empty)
        };
        if (divisor == 1)
            return FormatNumber(value, false);
        var significand = value / divisor;
        var format = Math.Abs(significand) < 10 ? "0.0" : "0";
        return significand.ToString(format, CultureInfo.InvariantCulture) + suffix;
    }

    internal static string RegionName(int region) => region switch
    {
        1 => "US",
        2 => "KR",
        3 => "EU",
        4 => "TW",
        5 => "CN",
        _ => string.Empty
    };

    private static void PushOptionalLocaleName(
        lua_State state,
        WowClientLocale locale)
    {
        var localeName = WowLocalizationState.LocaleName(locale);
        if (localeName is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, localeName);
    }

    private static double RequireFiniteNumber(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = lua_tonumber(state, 1);
        if (double.IsFinite(value))
            return value;
        luaL_error(state, usage);
        return 0;
    }

    private static bool ReadOptionalTruthiness(lua_State state, int index) =>
        index <= lua_gettop(state) &&
        lua_type(state, index) != LUA_TNIL &&
        lua_toboolean(state, index) != 0;

    private static void RequireOptionalOptionsTable(
        lua_State state,
        string usage)
    {
        if (lua_gettop(state) < 2 || lua_type(state, 2) == LUA_TNIL)
            return;
        if (lua_type(state, 2) != LUA_TTABLE)
            luaL_error(state, usage);
    }

    private static void RequireValue(
        lua_State state,
        int index,
        string usage)
    {
        if (index <= lua_gettop(state) && lua_type(state, index) != LUA_TNIL)
            return;
        luaL_error(state, usage);
    }
}
