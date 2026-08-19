using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLfgInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AreCrossFactionGroupQueuesAllowed",
        "CanPlayerUseGroupFinder",
        "CanPlayerUseLFD",
        "CanPlayerUseLFR",
        "CanPlayerUsePVP",
        "CanPlayerUsePremadeGroup",
        "CanPlayerUseScenarioFinder",
        "ConfirmLfgExpandSearch",
        "DoesActivePartyMeetPremadeLaunchCount",
        "DoesCrossFactionQueueRequireFullPremade",
        "GetAllEntriesForCategory",
        "GetDungeonInfo",
        "GetLFDLockStates",
        "GetLevelUpInstances",
        "GetRoleCheckDifficultyDetails",
        "HideNameFromUI",
        "IsGroupFinderEnabled",
        "IsInLFGFollowerDungeon",
        "IsLFDEnabled",
        "IsLFGFollowerDungeon",
        "IsLFREnabled"
    ];

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetLFGDeserterExpiration",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetLFGProposal", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetLFGInfoServer", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetLFGRoleUpdate", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetPartyLFGID", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetLFGCategoryForID", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetLFGReadyCheckUpdate",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsPartyLFG", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "IsAllowedToUserTeleport",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetLFGQueuedList", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetLFGRoles", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "CanShowSetRoleButton",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "HasLFGRestrictions", Callback);
        LuaBindings.RegisterClosureGlobal(state, "CanPartyLFGBackfill", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "RequestLFDPlayerLockInfo",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "RequestLFDPartyLockInfo",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetLFDChoiceOrder", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetLFDChoiceCollapseState",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetLFDChoiceEnabledState",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetNumRandomDungeons",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetNumRFDungeons", Callback);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_LFGInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var lfg = LuaBindings.GetRuntime(state).LfgInfo;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        return operation switch
        {
            "AreCrossFactionGroupQueuesAllowed" =>
                AreCrossFactionGroupQueuesAllowed(state, lfg),
            "CanPlayerUseGroupFinder" or
            "CanPlayerUseLFD" or
            "CanPlayerUseLFR" or
            "CanPlayerUsePVP" or
            "CanPlayerUsePremadeGroup" or
            "CanPlayerUseScenarioFinder" =>
                PushEligibility(state, lfg, operation),
            "ConfirmLfgExpandSearch" => ConfirmExpandSearch(lfg),
            "DoesActivePartyMeetPremadeLaunchCount" =>
                DoesActivePartyMeetPremadeLaunchCount(state, lfg),
            "DoesCrossFactionQueueRequireFullPremade" =>
                DoesCrossFactionQueueRequireFullPremade(state, lfg),
            "GetAllEntriesForCategory" =>
                GetAllEntriesForCategory(state, lfg),
            "GetDungeonInfo" => GetDungeonInfo(state, lfg),
            "GetLFDLockStates" => GetLfdLockStates(state, lfg),
            "GetLevelUpInstances" => GetLevelUpInstances(state, lfg),
            "GetRoleCheckDifficultyDetails" =>
                GetRoleCheckDifficultyDetails(state, lfg),
            "HideNameFromUI" => HideNameFromUi(state, lfg),
            "IsGroupFinderEnabled" => PushBoolean(state, true),
            "IsInLFGFollowerDungeon" =>
                PushBoolean(state, lfg.IsInFollowerDungeon),
            "IsLFDEnabled" => PushBoolean(state, true),
            "IsLFGFollowerDungeon" =>
                IsLfgFollowerDungeon(state, lfg),
            "IsLFREnabled" => PushBoolean(state, true),
            _ => DispatchLegacy(state, lfg, operation)
        };
    }

    private static int AreCrossFactionGroupQueuesAllowed(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: local areCrossFactionGroupQueuesAllowed = C_LFGInfo.AreCrossFactionGroupQueuesAllowed(lfgDungeonID)";
        var dungeonId = RequiredInt32(state, 1, usage);
        return PushBoolean(
            state,
            lfg.CrossFactionQueuesAllowed.TryGetValue(
                dungeonId,
                out var allowed) &&
            allowed);
    }

    private static int PushEligibility(
        lua_State state,
        WowLfgInfoState lfg,
        string operation)
    {
        var eligibility = lfg.Eligibility.TryGetValue(
            operation,
            out var configured)
                ? configured
                : new WowLfgEligibility(true, string.Empty);
        lua_pushboolean(state, eligibility.CanUse ? 1 : 0);
        lua_pushstring(state, eligibility.Reason);
        return 2;
    }

    private static int ConfirmExpandSearch(WowLfgInfoState lfg)
    {
        lfg.ConfirmExpandSearchCount++;
        return 0;
    }

    private static int DoesActivePartyMeetPremadeLaunchCount(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: local doesActivePartyMeetPremadeLaunchCount = C_LFGInfo.DoesActivePartyMeetPremadeLaunchCount(lfgDungeonID)";
        var dungeonId = RequiredInt32(state, 1, usage);
        return PushBoolean(
            state,
            lfg.ActivePartyMeetsPremadeLaunchCount.TryGetValue(
                dungeonId,
                out var meetsCount) &&
            meetsCount);
    }

    private static int DoesCrossFactionQueueRequireFullPremade(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: local doesCrossFactionQueueRequireFullPremade = C_LFGInfo.DoesCrossFactionQueueRequireFullPremade(lfgDungeonID)";
        var dungeonId = RequiredInt32(state, 1, usage);
        return PushBoolean(
            state,
            lfg.CrossFactionQueueRequiresFullPremade.TryGetValue(
                dungeonId,
                out var requiresFullPremade) &&
            requiresFullPremade);
    }

    private static int GetAllEntriesForCategory(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: local lfgDungeonIDs = C_LFGInfo.GetAllEntriesForCategory(category)";
        var category = RequiredOneBasedIndex(state, 1, usage);
        lfg.EntriesByCategory.TryGetValue(category, out var entries);
        PushUIntArray(state, entries ?? []);
        return 1;
    }

    private static int GetDungeonInfo(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: local dungeonInfo = C_LFGInfo.GetDungeonInfo(lfgDungeonID)";
        var dungeonId = RequiredInt32(state, 1, usage);
        if (!lfg.Dungeons.TryGetValue(dungeonId, out var dungeon))
            return 0;

        lua_createtable(state, 0, 3);
        lua_pushstring(state, dungeon.Name);
        lua_setfield(state, -2, "name");
        if (dungeon.IconId is > 0)
            lua_pushinteger(state, dungeon.IconId.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, "iconID");
        if (dungeon.Link is not null)
            lua_pushstring(state, dungeon.Link);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, "link");
        return 1;
    }

    private static int GetLfdLockStates(
        lua_State state,
        WowLfgInfoState lfg)
    {
        lua_createtable(state, lfg.LockStates.Count, 0);
        for (var index = 0; index < lfg.LockStates.Count; index++)
        {
            var lockState = lfg.LockStates[index];
            lua_createtable(state, 0, 3);
            lua_pushinteger(state, lockState.LfgId);
            lua_setfield(state, -2, "lfgID");
            lua_pushinteger(state, lockState.Reason);
            lua_setfield(state, -2, "reason");
            lua_pushboolean(state, lockState.HideEntry ? 1 : 0);
            lua_setfield(state, -2, "hideEntry");
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetLevelUpInstances(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: local instances = C_LFGInfo.GetLevelUpInstances(currPlayerLevel, isRaid)";
        var playerLevel = RequiredUInt32(state, 1, usage);
        var isRaid = RequiredBoolean(state, 2, usage);
        lfg.LevelUpInstances.TryGetValue(
            new WowLfgLevelUpKey(playerLevel, isRaid),
            out var instances);
        PushIntArray(state, instances ?? []);
        return 1;
    }

    private static int GetRoleCheckDifficultyDetails(
        lua_State state,
        WowLfgInfoState lfg)
    {
        if (lfg.RoleCheckDifficultyId.HasValue)
            lua_pushinteger(state, lfg.RoleCheckDifficultyId.Value);
        else
            lua_pushnil(state);
        lua_pushboolean(state, lfg.RoleCheckIsRaid ? 1 : 0);
        return 2;
    }

    private static int HideNameFromUi(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: local shouldHide = C_LFGInfo.HideNameFromUI(dungeonID)";
        return PushBoolean(
            state,
            !lfg.VisibleNameDungeonIds.Contains(
                RequiredUInt32(state, 1, usage)));
    }

    private static int IsLfgFollowerDungeon(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: local result = C_LFGInfo.IsLFGFollowerDungeon(dungeonID)";
        return PushBoolean(
            state,
            lfg.FollowerDungeonIds.Contains(
                RequiredUInt32(state, 1, usage)));
    }

    private static int DispatchLegacy(
        lua_State state,
        WowLfgInfoState lfg,
        string operation)
    {
        switch (operation)
        {
            case "GetLFGProposal":
                return GetLfgProposal(state, lfg);
            case "GetLFGInfoServer":
                return GetLfgInfoServer(state, lfg);
            case "GetLFGRoleUpdate":
                return GetLfgRoleUpdate(state, lfg);
            case "GetPartyLFGID":
                return GetPartyLfgId(state, lfg);
            case "GetLFGCategoryForID":
                return GetLfgCategoryForId(state, lfg);
            case "GetLFGReadyCheckUpdate":
                lua_pushboolean(state, lfg.ReadyCheckInProgress ? 1 : 0);
                lua_pushboolean(
                    state,
                    lfg.ReadyCheckIsBattlegroundQueue ? 1 : 0);
                return 2;
            case "IsPartyLFG":
                return PushBoolean(
                    state,
                    lfg.PartyLfgDungeonId is not null and not 0);
            case "IsAllowedToUserTeleport":
                return PushBoolean(state, lfg.IsAllowedToUserTeleport);
            case "GetLFGDeserterExpiration":
                if (!lfg.DeserterExpiration.HasValue)
                    return 0;
                lua_pushnumber(state, lfg.DeserterExpiration.Value);
                return 1;
            case "GetLFGQueuedList":
                return GetLfgQueuedList(state, lfg);
            case "GetLFDChoiceOrder":
                return GetLfdChoiceOrder(state, lfg);
            case "GetLFDChoiceCollapseState":
                return GetLfdChoiceCollapseState(state, lfg);
            case "GetLFDChoiceEnabledState":
                return GetLfdChoiceEnabledState(state, lfg);
            case "GetNumRandomDungeons":
                lua_pushinteger(state, lfg.RandomDungeonCount);
                return 1;
            case "GetNumRFDungeons":
                lua_pushinteger(state, lfg.RaidFinderDungeonCount);
                return 1;
            case "GetLFGRoles":
                lua_pushboolean(state, lfg.Roles.Leader ? 1 : 0);
                lua_pushboolean(state, lfg.Roles.Tank ? 1 : 0);
                lua_pushboolean(state, lfg.Roles.Healer ? 1 : 0);
                lua_pushboolean(state, lfg.Roles.Damage ? 1 : 0);
                return 4;
            case "CanShowSetRoleButton":
                return PushBoolean(state, lfg.CanShowSetRoleButton);
            case "HasLFGRestrictions":
                return PushBoolean(state, lfg.HasRestrictions);
            case "CanPartyLFGBackfill":
                return PushBoolean(state, lfg.CanPartyBackfill);
            case "RequestLFDPlayerLockInfo":
                lfg.PlayerLockInfoRequestCount++;
                return PushBoolean(
                    state,
                    lfg.PlayerLockInfoRequestAllowed);
            case "RequestLFDPartyLockInfo":
                lfg.PartyLockInfoRequestCount++;
                return PushBoolean(
                    state,
                    lfg.PartyLockInfoRequestAllowed);
            default:
                return 0;
        }
    }

    private static int GetLfgProposal(
        lua_State state,
        WowLfgInfoState lfg)
    {
        if (lfg.CurrentProposal is not { } proposal)
            return 0;

        lua_pushboolean(state, 1);
        lua_pushinteger(state, proposal.DungeonId);
        lua_pushinteger(state, proposal.TypeId);
        lua_pushinteger(state, proposal.SubtypeId);
        lua_pushstring(state, proposal.Name);
        PushOptionalInteger(state, proposal.BackgroundTexture);
        lua_pushstring(state, proposal.Role);
        lua_pushboolean(state, proposal.HasResponded ? 1 : 0);
        lua_pushinteger(state, proposal.TotalEncounters);
        lua_pushinteger(state, proposal.CompletedEncounters);
        lua_pushinteger(state, proposal.MemberCount);
        lua_pushboolean(state, proposal.IsLeader ? 1 : 0);
        lua_pushboolean(state, proposal.IsHoliday ? 1 : 0);
        lua_pushinteger(state, proposal.Category);
        lua_pushboolean(state, proposal.IsSilent ? 1 : 0);
        return 15;
    }

    private static int GetLfgInfoServer(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: GetLFGInfoServer(LE_LFG_CATEGORY[, lfgID])";
        var category = RequiredInt32(state, 1, usage);
        if (category is < 1 or > 7)
            return luaL_error(state, "Invalid category");

        WowLfgServerInfoState? info = null;
        if (lua_isnumber(state, 2) != 0)
        {
            var number = lua_tonumber(state, 2);
            if (double.IsFinite(number) &&
                number is >= int.MinValue and <= int.MaxValue)
            {
                var dungeonId = (int)number;
                if (lfg.ServerInfoByDungeonId.TryGetValue(
                        dungeonId,
                        out var byDungeonId) &&
                    byDungeonId.Category == category)
                {
                    info = byDungeonId;
                }
            }
        }
        else
        {
            info = lfg.ServerInfoByDungeonId.Values.FirstOrDefault(
                value => value.Category == category);
        }

        lua_pushboolean(state, info?.InParty == true ? 1 : 0);
        lua_pushboolean(state, info?.Joined == true ? 1 : 0);
        lua_pushboolean(state, info?.Queued == true ? 1 : 0);
        lua_pushboolean(state, info?.NoPartialClear == true ? 1 : 0);
        lua_pushboolean(state, info?.Achievements == true ? 1 : 0);
        lua_pushstring(state, info?.Comment ?? string.Empty);
        lua_pushinteger(state, info?.SlotCount ?? 0);
        PushOptionalInteger(state, info?.Category);
        lua_pushboolean(state, info?.Leader == true ? 1 : 0);
        lua_pushboolean(state, info?.Tank == true ? 1 : 0);
        lua_pushboolean(state, info?.Healer == true ? 1 : 0);
        lua_pushboolean(state, info?.Damage == true ? 1 : 0);
        lua_pushinteger(state, 0);
        lua_pushinteger(state, 0);
        lua_pushinteger(state, 0);
        lua_pushinteger(state, info?.TrailingValue ?? 0);
        return 16;
    }

    private static int GetLfgRoleUpdate(
        lua_State state,
        WowLfgInfoState lfg)
    {
        var update = lfg.RoleUpdate;
        lua_pushboolean(state, update.InProgress ? 1 : 0);
        lua_pushinteger(state, update.SlotCount);
        lua_pushinteger(state, update.MemberCount);
        if (update.Category.HasValue && update.DungeonId.HasValue)
        {
            lua_pushinteger(state, update.Category.Value);
            lua_pushinteger(state, update.DungeonId.Value);
        }
        else
        {
            lua_pushnil(state);
            lua_pushnil(state);
        }
        lua_pushboolean(state, update.IsBattlegroundQueue ? 1 : 0);
        return 6;
    }

    private static int GetPartyLfgId(
        lua_State state,
        WowLfgInfoState lfg)
    {
        if (lfg.PartyLfgDungeonId is not { } dungeonId || dungeonId == 0)
            return 0;

        lua_pushinteger(state, dungeonId & 0xFFFFF);
        var maskedSecondaryDungeonId =
            lfg.PartyLfgSecondaryDungeonId is { } secondary
                ? secondary & 0xFFFFF
                : 0;
        PushOptionalInteger(
            state,
            maskedSecondaryDungeonId != 0
                ? maskedSecondaryDungeonId
                : null);
        return 2;
    }

    private static int GetLfgCategoryForId(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage = "Usage: GetLFGCategoryForID(dungeonID)";
        var dungeonId = RequiredInt32(state, 1, usage);
        if (!lfg.DungeonCategoryById.TryGetValue(dungeonId, out var category) ||
            category is < 1 or > 7)
        {
            return 0;
        }

        lua_pushinteger(state, category);
        return 1;
    }

    private static int GetLfgQueuedList(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: GetLFDQueuedList(category[, table])";
        var category = RequiredInt32(state, 1, usage);
        if (category is < 1 or > 7)
            return luaL_error(state, "Invalid category");

        PushReusableTable(state, 2, validateOptionalTable: false, usage);
        if (!lfg.QueuedDungeonIdsByCategory.TryGetValue(
                category,
                out var dungeonIds))
        {
            return 1;
        }

        foreach (var dungeonId in dungeonIds)
        {
            lua_pushinteger(state, dungeonId);
            lua_pushboolean(state, 1);
            lua_settable(state, -3);
        }
        return 1;
    }

    private static int GetLfdChoiceOrder(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage = "Usage: GetLFDChoiceOrder([table])";
        PushReusableTable(state, 1, validateOptionalTable: true, usage);
        for (var index = 0; index < lfg.ChoiceOrder.Count; index++)
        {
            lua_pushinteger(state, lfg.ChoiceOrder[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetLfdChoiceCollapseState(
        lua_State state,
        WowLfgInfoState lfg)
    {
        const string usage =
            "Usage: GetLFDChoiceCollapseState([table])";
        PushReusableTable(state, 1, validateOptionalTable: true, usage);
        foreach (var (dungeonId, collapsed) in lfg.ChoiceCollapseState)
        {
            lua_pushinteger(state, dungeonId);
            lua_pushboolean(state, collapsed ? 1 : 0);
            lua_settable(state, -3);
        }
        return 1;
    }

    private static int GetLfdChoiceEnabledState(
        lua_State state,
        WowLfgInfoState lfg)
    {
        PushReusableTable(
            state,
            1,
            validateOptionalTable: false,
            string.Empty);
        foreach (var dungeonId in lfg.EnabledChoiceIds)
        {
            lua_pushinteger(state, dungeonId);
            lua_pushboolean(state, 1);
            lua_settable(state, -3);
        }
        return 1;
    }

    private static void PushReusableTable(
        lua_State state,
        int argumentIndex,
        bool validateOptionalTable,
        string usage)
    {
        if (lua_istable(state, argumentIndex) != 0)
        {
            ClearTable(state, argumentIndex);
            lua_pushvalue(state, argumentIndex);
            return;
        }

        if (validateOptionalTable &&
            lua_gettop(state) >= argumentIndex &&
            lua_isnil(state, argumentIndex) == 0)
        {
            luaL_error(state, usage);
            return;
        }

        lua_newtable(state);
    }

    private static void ClearTable(lua_State state, int tableIndex)
    {
        var absoluteIndex = tableIndex > 0 || tableIndex <= LUA_REGISTRYINDEX
            ? tableIndex
            : lua_gettop(state) + tableIndex + 1;
        lua_newtable(state);
        var keysIndex = lua_gettop(state);
        var keyCount = 0;

        lua_pushnil(state);
        while (lua_next(state, absoluteIndex) != 0)
        {
            lua_pop(state, 1);
            lua_pushvalue(state, -1);
            lua_rawseti(state, keysIndex, ++keyCount);
        }

        for (var index = 1; index <= keyCount; index++)
        {
            lua_rawgeti(state, keysIndex, index);
            lua_pushnil(state);
            lua_settable(state, absoluteIndex);
        }

        lua_pop(state, 1);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (int)value;
    }

    private static uint RequiredUInt32(
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
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)value;
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage) =>
        unchecked((int)RequiredUInt32(state, index, usage));

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state))
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void PushUIntArray(
        lua_State state,
        IReadOnlyList<uint> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushnumber(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushIntArray(
        lua_State state,
        IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }
}
