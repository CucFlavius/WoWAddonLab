using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCombatLogApi : LuaApiModule
{
    private const string GetEntryCountUsage =
        "Usage: local count = C_CombatLog.GetEntryCount([ignoreFilter])";
    private const string SecureGetEntryCountUsage =
        "Usage: local count = C_CombatLogSecure.GetEntryCount([ignoreFilter])";
    private const string SeekToNewestEntryUsage =
        "Usage: local isValidEntry = C_CombatLog.SeekToNewestEntry([ignoreFilter])";
    private const string SecureSeekToNewestEntryUsage =
        "Usage: local isValidEntry = C_CombatLogSecure.SeekToNewestEntry([ignoreFilter])";
    private const string SeekToPreviousEntryUsage =
        "Usage: local isValidEntry = C_CombatLog.SeekToPreviousEntry([ignoreFilter])";
    private const string SecureSeekToPreviousEntryUsage =
        "Usage: local isValidEntry = C_CombatLogSecure.SeekToPreviousEntry([ignoreFilter])";
    private const string CreateCombatLogMessageUsage =
        "Usage: C_CombatLogSecure.CreateCombatLogMessage(message, color, order)";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] PublicFunctions =
    [
        "AddEventFilter", "ApplyFilterSettings", "AreFilteredEventsEnabled", "ClearEntries",
        "ClearEventFilters", "DoesObjectMatchFilter", "GetCurrentEntryInfo",
        "GetCurrentEventInfo", "GetEntryCount", "GetEntryRetentionTime", "GetMessageLimit",
        "IsCombatLogRestricted", "RefilterEntries", "SeekToNewestEntry", "SeekToPreviousEntry",
        "SetEntryRetentionTime", "SetFilteredEventsEnabled", "SetMessageLimit",
        "ShouldShowCurrentEntry"
    ];

    private static readonly string[] SecureFunctions =
    [
        "AddEventFilter", "ClearEventFilters", "CreateCombatLogMessage",
        "GetCurrentEntryInfo", "GetCurrentEventInfo", "GetEntryCount", "SeekToNewestEntry",
        "SeekToPreviousEntry", "ShouldShowCurrentEntry"
    ];

    public override void Register(lua_State state)
    {
        RegisterNamespace(state, "C_CombatLog", PublicFunctions);
        RegisterNamespace(state, "C_CombatLogSecure", SecureFunctions);
        RegisterNamespace(state, "C_CombatLogInternal", ["GetCurrentEventInfo"]);
    }

    private static void RegisterNamespace(
        lua_State state,
        string namespaceName,
        IEnumerable<string> functions)
    {
        lua_newtable(state);
        foreach (var function in functions)
        {
            lua_pushstring(state, $"{namespaceName}.{function}");
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, namespaceName);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var combatLog = runtime.CombatLog;
        var qualifiedOperation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var separator = qualifiedOperation.LastIndexOf('.');
        var operation = separator >= 0
            ? qualifiedOperation[(separator + 1)..]
            : qualifiedOperation;
        var secure = qualifiedOperation.StartsWith("C_CombatLogSecure.", StringComparison.Ordinal);

        switch (operation)
        {
            case "AddEventFilter":
                AddEventFilter(state, combatLog);
                return 0;
            case "ApplyFilterSettings":
                if (lua_type(state, 1) != LUA_TTABLE)
                    return luaL_error(state, "Usage: C_CombatLog.ApplyFilterSettings(settings)");
                combatLog.ApplyFilterSettingsCount++;
                return 0;
            case "AreFilteredEventsEnabled":
                lua_pushboolean(state, combatLog.FilteredEventsEnabled ? 1 : 0);
                return 1;
            case "ClearEntries":
                combatLog.Entries.Clear();
                combatLog.CurrentEvent = null;
                combatLog.CurrentEntryIndex = null;
                return 0;
            case "ClearEventFilters":
                combatLog.EventFilters.Clear();
                return 0;
            case "CreateCombatLogMessage":
                combatLog.LastCreatedMessage = new WowCombatLogMessage(
                    RequiredString(state, 1, CreateCombatLogMessageUsage),
                    RequiredNormalizedByte(state, 2, CreateCombatLogMessageUsage),
                    RequiredNormalizedByte(state, 3, CreateCombatLogMessageUsage),
                    RequiredNormalizedByte(state, 4, CreateCombatLogMessageUsage),
                    RequiredOrder(state, 5, CreateCombatLogMessageUsage));
                return 0;
            case "DoesObjectMatchFilter":
            {
                const string usage =
                    "Usage: local matches = C_CombatLog.DoesObjectMatchFilter(mask, flags)";
                var mask = RequiredUInt32(state, 1, usage);
                var flags = RequiredUInt32(state, 2, usage);
                lua_pushboolean(state, DoesObjectMatchFilter(mask, flags) ? 1 : 0);
                return 1;
            }
            case "GetCurrentEntryInfo":
                return PushValues(runtime, CurrentEntry(combatLog)?.Info);
            case "GetCurrentEventInfo":
                return PushValues(runtime, combatLog.CurrentEvent?.Info);
            case "GetEntryCount":
            {
                var ignoreFilter = OptionalBoolean(
                    state,
                    1,
                    secure ? SecureGetEntryCountUsage : GetEntryCountUsage);
                var count = ShouldApplyFilters(combatLog, ignoreFilter)
                    ? combatLog.Entries.Count(entry => entry.MatchesEventFilters)
                    : combatLog.Entries.Count;
                lua_pushinteger(state, count);
                return 1;
            }
            case "GetEntryRetentionTime":
                lua_pushinteger(state, combatLog.EntryRetentionTime);
                return 1;
            case "GetMessageLimit":
                lua_pushinteger(state, combatLog.MessageLimit);
                return 1;
            case "IsCombatLogRestricted":
                lua_pushboolean(state, 1);
                return 1;
            case "RefilterEntries":
                combatLog.RefilterEntriesCount++;
                return 0;
            case "SeekToNewestEntry":
            {
                var ignoreFilter = OptionalBoolean(
                    state,
                    1,
                    secure ? SecureSeekToNewestEntryUsage : SeekToNewestEntryUsage);
                var found = SeekToNewestEntry(combatLog, ignoreFilter);
                lua_pushboolean(state, found ? 1 : 0);
                return 1;
            }
            case "SeekToPreviousEntry":
            {
                var ignoreFilter = OptionalBoolean(
                    state,
                    1,
                    secure ? SecureSeekToPreviousEntryUsage : SeekToPreviousEntryUsage);
                var found = SeekToPreviousEntry(combatLog, ignoreFilter);
                lua_pushboolean(state, found ? 1 : 0);
                return 1;
            }
            case "SetEntryRetentionTime":
                combatLog.EntryRetentionTime = RequiredInt32(
                    state,
                    1,
                    "Usage: C_CombatLog.SetEntryRetentionTime(retentionTime)");
                return 0;
            case "SetFilteredEventsEnabled":
                combatLog.FilteredEventsEnabled = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_CombatLog.SetFilteredEventsEnabled(enabled)");
                return 0;
            case "SetMessageLimit":
                combatLog.MessageLimit = (int)Math.Min(
                    RequiredUInt32(
                        state,
                        1,
                        "Usage: C_CombatLog.SetMessageLimit(messageLimit)"),
                    1000u);
                return 0;
            case "ShouldShowCurrentEntry":
                lua_pushboolean(state, CurrentEntry(combatLog)?.ShouldShow == true ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static void AddEventFilter(lua_State state, WowCombatLogState combatLog)
    {
        var source = ReadLegacyFilterValue(state, 2);
        var destination = ReadLegacyFilterValue(state, 3);
        ValidateCompleteMask(state, source, "srcMask");
        ValidateCompleteMask(state, destination, "dstMask");
        combatLog.EventFilters.Add(new WowCombatLogEventFilter(
            lua_isstring(state, 1) != 0 ? lua_tostring(state, 1) : null,
            source,
            destination,
            ReadLegacyFilterValue(state, 4)));
    }

    private static object? ReadLegacyFilterValue(lua_State state, int index)
    {
        return lua_type(state, index) switch
        {
            LUA_TNUMBER => unchecked((int)lua_tonumber(state, index)),
            LUA_TSTRING => lua_tostring(state, index),
            _ => null
        };
    }

    private static void ValidateCompleteMask(
        lua_State state,
        object? value,
        string argumentName)
    {
        if (value is not int mask ||
            (mask & unchecked((int)0xFFFF0000)) != 0 ||
            HasAllObjectFlagCategories(unchecked((uint)mask)))
        {
            return;
        }

        luaL_error(
            state,
            $"CombatLogAddFilter(): incomplete filter for {argumentName}");
    }

    private static bool DoesObjectMatchFilter(uint mask, uint flags)
    {
        var intersection = mask & flags;
        return (intersection & 0xFFFF0000u) != 0 ||
               HasAllObjectFlagCategories(intersection);
    }

    private static bool HasAllObjectFlagCategories(uint value) =>
        (value & 0xFu) != 0 &&
        (value & 0xF0u) != 0 &&
        (value & 0x300u) != 0 &&
        (value & 0xFC00u) != 0;

    private static WowCombatLogEntry? CurrentEntry(WowCombatLogState combatLog)
    {
        var index = combatLog.CurrentEntryIndex;
        return index is >= 0 && index < combatLog.Entries.Count
            ? combatLog.Entries[index.Value]
            : null;
    }

    private static bool ShouldApplyFilters(WowCombatLogState combatLog, bool ignoreFilter) =>
        !ignoreFilter && combatLog.EventFilters.Count > 0;

    private static bool SeekToNewestEntry(WowCombatLogState combatLog, bool ignoreFilter)
    {
        for (var index = combatLog.Entries.Count - 1; index >= 0; index--)
        {
            if (ShouldApplyFilters(combatLog, ignoreFilter) &&
                !combatLog.Entries[index].MatchesEventFilters)
            {
                continue;
            }
            combatLog.CurrentEntryIndex = index;
            return true;
        }

        combatLog.CurrentEntryIndex = null;
        return false;
    }

    private static bool SeekToPreviousEntry(WowCombatLogState combatLog, bool ignoreFilter)
    {
        if (combatLog.CurrentEntryIndex is not { } current)
        {
            combatLog.CurrentEntryIndex = null;
            return false;
        }

        for (var index = current - 1; index >= 0; index--)
        {
            if (ShouldApplyFilters(combatLog, ignoreFilter) &&
                !combatLog.Entries[index].MatchesEventFilters)
            {
                continue;
            }
            combatLog.CurrentEntryIndex = index;
            return true;
        }

        combatLog.CurrentEntryIndex = null;
        return false;
    }

    private static int PushValues(LuaRuntime runtime, IReadOnlyList<object?>? values)
    {
        if (values is null)
            return 0;
        foreach (var value in values)
            runtime.PushValue(value);
        return values.Count;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return false;
        return RequiredBoolean(state, index, usage);
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((uint)luaL_error(state, usage));
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            return unchecked((uint)luaL_error(state, usage));
        return unchecked((uint)value);
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static byte RequiredNormalizedByte(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((byte)luaL_error(state, usage));
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value))
            return unchecked((byte)luaL_error(state, usage));
        return (byte)Math.Floor(Math.Clamp(value, 0, 1) * 255 + 0.5);
    }

    private static int RequiredOrder(
        lua_State state,
        int index,
        string usage)
    {
        var order = RequiredInt32(state, index, usage);
        return order is 0 or 1 ? order : luaL_error(state, usage);
    }
}
