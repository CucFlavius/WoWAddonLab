using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowReputationApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AreLegacyReputationsShown", "CollapseAllFactionHeaders",
        "CollapseFactionHeader", "ExpandAllFactionHeaders", "ExpandFactionHeader",
        "GetFactionDataByID", "GetFactionDataByIndex", "GetFactionParagonInfo",
        "GetGuildFactionData", "GetGuildRepExpirationTime", "GetNumFactions",
        "GetReputationSortType", "GetSelectedFaction", "GetWatchedFactionData",
        "IsAccountWideReputation", "IsFactionActive", "IsFactionParagon",
        "IsFactionParagonForCurrentPlayer", "IsMajorFaction",
        "RequestFactionParagonPreloadRewardData", "SetFactionActive",
        "SetLegacyReputationsShown", "SetReputationSortType",
        "SetSelectedFaction", "SetWatchedFactionByID",
        "SetWatchedFactionByIndex", "ToggleFactionAtWar"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Reputation");
    }

    private static int Dispatch(lua_State state)
    {
        var reputation = LuaBindings.GetRuntime(state).Reputation;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetNumFactions":
                lua_pushinteger(state, reputation.Factions.Count);
                return 1;
            case "GetFactionDataByIndex":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: local factionData = C_Reputation.GetFactionDataByIndex(factionSortIndex)");
                return PushOptionalFaction(
                    state,
                    index >= 0 && index < reputation.Factions.Count
                        ? reputation.Factions[index]
                        : null);
            }
            case "GetFactionDataByID":
            {
                var factionId = RequiredInt32(
                    state,
                    "Usage: local factionData = C_Reputation.GetFactionDataByID(factionID)");
                return PushOptionalFaction(
                    state,
                    reputation.Factions.FirstOrDefault(value =>
                        value.FactionId == factionId));
            }
            case "GetWatchedFactionData":
                return PushOptionalFaction(state, reputation.WatchedFaction);
            case "GetSelectedFaction":
                lua_pushinteger(state, reputation.SelectedFactionIndex);
                return 1;
            case "GetReputationSortType":
                lua_pushinteger(state, reputation.SortType);
                return 1;
            case "AreLegacyReputationsShown":
                lua_pushboolean(state, reputation.LegacyReputationsShown ? 1 : 0);
                return 1;
            case "IsAccountWideReputation":
            {
                var factionId = RequiredInt32(
                    state,
                    "Usage: local isAccountWide = C_Reputation.IsAccountWideReputation(factionID)");
                var faction = reputation.Factions.FirstOrDefault(value =>
                    value.FactionId == factionId);
                lua_pushboolean(state, faction?.IsAccountWide == true ? 1 : 0);
                return 1;
            }
            case "IsFactionActive":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: local isActive = C_Reputation.IsFactionActive(factionSortIndex)");
                var active = index < reputation.Factions.Count &&
                             reputation.Factions[index].IsActive;
                lua_pushboolean(state, active ? 1 : 0);
                return 1;
            }
            case "IsFactionParagon":
            {
                var faction = FindFaction(
                    reputation,
                    RequiredInt32(
                        state,
                        "Usage: local factionIsParagon = C_Reputation.IsFactionParagon(factionID)"));
                lua_pushboolean(state, faction?.IsParagon == true ? 1 : 0);
                return 1;
            }
            case "IsFactionParagonForCurrentPlayer":
            {
                var faction = FindFaction(
                    reputation,
                    RequiredInt32(
                        state,
                        "Usage: local currentPlayerHasParagon = C_Reputation.IsFactionParagonForCurrentPlayer(factionID)"));
                lua_pushboolean(
                    state,
                    faction?.IsParagonForCurrentPlayer == true ? 1 : 0);
                return 1;
            }
            case "IsMajorFaction":
            {
                var faction = FindFaction(
                    reputation,
                    RequiredInt32(
                        state,
                        "Usage: local isMajorFaction = C_Reputation.IsMajorFaction(factionID)"));
                lua_pushboolean(state, faction?.IsMajorFaction == true ? 1 : 0);
                return 1;
            }
            case "CollapseAllFactionHeaders":
                foreach (var faction in reputation.Factions.Where(value => value.IsHeader))
                    faction.IsCollapsed = true;
                return 0;
            case "ExpandAllFactionHeaders":
                foreach (var faction in reputation.Factions.Where(value => value.IsHeader))
                    faction.IsCollapsed = false;
                return 0;
            case "CollapseFactionHeader":
            case "ExpandFactionHeader":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    operation == "CollapseFactionHeader"
                        ? "Usage: C_Reputation.CollapseFactionHeader(factionSortIndex)"
                        : "Usage: C_Reputation.ExpandFactionHeader(factionSortIndex)");
                if (index < reputation.Factions.Count &&
                    reputation.Factions[index].IsHeader)
                {
                    reputation.Factions[index].IsCollapsed =
                        operation == "CollapseFactionHeader";
                }
                return 0;
            }
            case "GetFactionParagonInfo":
            {
                var factionId = RequiredInt32(
                    state,
                    "Usage: local currentValue, threshold, rewardQuestID, hasRewardPending, tooLowLevelForParagon, paragonStorageLevel = C_Reputation.GetFactionParagonInfo(factionID)");
                if (!reputation.ParagonInfoByFactionId.TryGetValue(
                        factionId,
                        out var info))
                {
                    return 0;
                }
                lua_pushnumber(state, info.CurrentValue);
                lua_pushnumber(state, info.Threshold);
                lua_pushnumber(state, info.RewardQuestId);
                lua_pushboolean(state, info.HasRewardPending ? 1 : 0);
                lua_pushboolean(state, info.TooLowLevelForParagon ? 1 : 0);
                lua_pushnumber(state, info.ParagonStorageLevel);
                return 6;
            }
            case "GetGuildFactionData":
                return PushOptionalFaction(
                    state,
                    reputation.GuildFaction ??
                    FindFaction(reputation, 1168));
            case "GetGuildRepExpirationTime":
                PushOptionalInteger(state, reputation.GuildRepExpirationTime);
                return 1;
            case "RequestFactionParagonPreloadRewardData":
            {
                var factionId = RequiredInt32(
                    state,
                    "Usage: C_Reputation.RequestFactionParagonPreloadRewardData(factionID)");
                if (reputation.ParagonInfoByFactionId.TryGetValue(
                        factionId,
                        out var info) &&
                    info.RewardQuestId != 0)
                {
                    reputation.ParagonPreloadRequests.Add(factionId);
                }
                return 0;
            }
            case "SetFactionActive":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: C_Reputation.SetFactionActive(factionSortIndex, setActive)");
                var active = RequiredLuaBoolean(
                    state,
                    2,
                    "Usage: C_Reputation.SetFactionActive(factionSortIndex, setActive)");
                if (index < reputation.Factions.Count &&
                    reputation.Factions[index].CanSetInactive)
                {
                    reputation.Factions[index].IsActive = active;
                }
                return 0;
            }
            case "SetLegacyReputationsShown":
                reputation.LegacyReputationsShown = RequiredLuaBoolean(
                    state,
                    1,
                    "Usage: C_Reputation.SetLegacyReputationsShown(showLegacyReputations)");
                return 0;
            case "SetReputationSortType":
                reputation.SortType = RequiredEnum(
                    state,
                    0,
                    2,
                    "Usage: C_Reputation.SetReputationSortType(sortType)");
                return 0;
            case "SetSelectedFaction":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: C_Reputation.SetSelectedFaction(factionSortIndex)");
                reputation.SelectedFactionIndex =
                    index < reputation.Factions.Count ? index + 1 : 0;
                return 0;
            }
            case "SetWatchedFactionByID":
            {
                var factionId = RequiredInt32(
                    state,
                    "Usage: C_Reputation.SetWatchedFactionByID(factionID)");
                SetWatchedFaction(reputation, FindFaction(reputation, factionId));
                return 0;
            }
            case "SetWatchedFactionByIndex":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: C_Reputation.SetWatchedFactionByIndex(factionSortIndex)");
                SetWatchedFaction(
                    reputation,
                    index < reputation.Factions.Count
                        ? reputation.Factions[index]
                        : null);
                return 0;
            }
            case "ToggleFactionAtWar":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: C_Reputation.ToggleFactionAtWar(factionSortIndex)");
                if (index < reputation.Factions.Count)
                {
                    var faction = reputation.Factions[index];
                    if (faction.CanToggleAtWar)
                        faction.AtWarWith = !faction.AtWarWith;
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int PushOptionalFaction(lua_State state, WowFactionDataState? faction)
    {
        if (faction is null)
        {
            lua_pushnil(state);
            return 1;
        }
        lua_createtable(state, 0, 17);
        SetNumber(state, "factionID", faction.FactionId);
        SetOptionalString(state, "name", faction.Name);
        SetOptionalString(state, "description", faction.Description);
        SetNumber(state, "reaction", faction.Reaction);
        SetNumber(state, "currentReactionThreshold", faction.CurrentReactionThreshold);
        SetNumber(state, "nextReactionThreshold", faction.NextReactionThreshold);
        SetNumber(state, "currentStanding", faction.CurrentStanding);
        SetBoolean(state, "atWarWith", faction.AtWarWith);
        SetBoolean(state, "canToggleAtWar", faction.CanToggleAtWar);
        SetBoolean(state, "isChild", faction.IsChild);
        SetBoolean(state, "isHeader", faction.IsHeader);
        SetBoolean(state, "isHeaderWithRep", faction.IsHeaderWithRep);
        SetBoolean(state, "isCollapsed", faction.IsCollapsed);
        SetBoolean(state, "isWatched", faction.IsWatched);
        SetBoolean(state, "hasBonusRepGain", faction.HasBonusRepGain);
        SetBoolean(state, "canSetInactive", faction.CanSetInactive);
        SetBoolean(state, "isAccountWide", faction.IsAccountWide);
        return 1;
    }

    private static WowFactionDataState? FindFaction(
        WowReputationState reputation,
        int factionId) =>
        reputation.Factions.FirstOrDefault(value =>
            value.FactionId == factionId);

    private static void SetWatchedFaction(
        WowReputationState reputation,
        WowFactionDataState? faction)
    {
        foreach (var candidate in reputation.Factions)
            candidate.IsWatched = ReferenceEquals(candidate, faction);
        reputation.WatchedFaction = faction;
    }

    private static int RequiredInt32(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return unchecked((int)value);
    }

    private static int RequiredOneBasedIndex(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)(uint)value - 1);
    }

    private static bool RequiredLuaBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int RequiredEnum(
        lua_State state,
        int minimum,
        int maximum,
        string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        var result = unchecked((int)value);
        return result >= minimum && result <= maximum
            ? result
            : luaL_error(state, usage);
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
    }

    private static void SetOptionalString(
        lua_State state,
        string key,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetBoolean(lua_State state, string key, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, key);
    }
}
