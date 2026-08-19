using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowEncounterJournalApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetDungeonEntrancesForMap", "GetEncounterJournalLink", "GetEncountersOnMap",
        "GetInstanceForGameMap", "GetLootInfo", "GetLootInfoByIndex",
        "GetSectionIconFlags", "GetSectionInfo", "GetSlotFilter", "InitalizeSelectedTier",
        "InstanceHasLoot", "IsEncounterComplete", "OnClose", "OnOpen", "ResetSlotFilter",
        "SetPreviewMythicPlusLevel", "SetPreviewPvpTier",
        "SetSlotFilter", "SetTab",
        "StartArathiRPE"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);
        foreach (var function in new[]
                 {
                     "CanShowEncounterJournal",
                     "EJ_ClearSearch", "EJ_EndSearch", "EJ_GetContentTuningID",
                     "EJ_GetCreatureInfo", "EJ_GetCurrentTier", "EJ_GetDifficulty",
                     "EJ_GetEncounterInfo", "EJ_GetEncounterInfoByIndex",
                     "EJ_GetInstanceForMap", "EJ_GetMapEncounter",
                     "EJ_GetInstanceByIndex", "EJ_GetInstanceInfo", "EJ_GetInvTypeSortOrder",
                     "EJ_GetLootFilter", "EJ_GetNumEncountersForLootByIndex",
                     "EJ_GetNumLoot", "EJ_GetNumSearchResults", "EJ_GetNumTiers",
                     "EJ_GetSearchProgress", "EJ_GetSearchResult", "EJ_GetSearchSize",
                     "EJ_GetSectionPath", "EJ_GetTierInfo", "EJ_InstanceIsRaid",
                     "EJ_HandleLinkPath",
                     "EJ_IsLootListOutOfDate", "EJ_IsSearchFinished",
                     "EJ_IsValidInstanceDifficulty", "EJ_SelectEncounter",
                     "EJ_SelectInstance", "EJ_SelectTier", "EJ_SetDifficulty",
                     "EJ_SetLootFilter", "EJ_ResetLootFilter", "EJ_SetSearch"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_EncounterJournal");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var journal = runtime.EncounterJournal;
        var provider = runtime.EncounterJournalProvider;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetDungeonEntrancesForMap":
            {
                const string usage =
                    "Usage: local dungeonEntrances = " +
                    "C_EncounterJournal.GetDungeonEntrancesForMap(uiMapID)";
                var uiMapId = RequiredInt32(state, 1, usage);
                journal.DungeonEntrancesByUiMapId.TryGetValue(
                    uiMapId,
                    out var entrances);
                PushDungeonEntrances(state, entrances ?? []);
                return 1;
            }
            case "GetEncounterJournalLink":
            {
                const string usage =
                    "Usage: local link = " +
                    "C_EncounterJournal.GetEncounterJournalLink(" +
                    "linkType, ID, displayText, difficultyID)";
                var linkType = RequiredInt32(state, 1, usage);
                if (linkType is < 0 or > 3)
                    return luaL_error(state, usage);
                var id = RequiredInt32(state, 2, usage);
                var displayText = RequiredStringValue(state, 3, usage);
                var difficultyId = RequiredUInt32(state, 4, usage);
                lua_pushstring(
                    state,
                    $"|cffff00ff|Hjournal:{linkType}:{id}:" +
                    $"{difficultyId}|h[{displayText}]|h|r");
                return 1;
            }
            case "GetEncountersOnMap":
            {
                const string usage =
                    "Usage: local encounters = " +
                    "C_EncounterJournal.GetEncountersOnMap(uiMapID)";
                var uiMapId = RequiredInt32(state, 1, usage);
                journal.EncountersByUiMapId.TryGetValue(
                    uiMapId,
                    out var encounters);
                PushMapEncounters(state, encounters ?? []);
                return 1;
            }
            case "GetInstanceForGameMap":
            {
                const string usage =
                    "Usage: local journalInstanceID = " +
                    "C_EncounterJournal.GetInstanceForGameMap(mapID)";
                var mapId = RequiredInt32(state, 1, usage);
                if (journal.InstanceIdsByGameMapId.TryGetValue(
                        mapId,
                        out var instanceId))
                {
                    lua_pushinteger(state, instanceId);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetLootInfo":
            {
                const string usage =
                    "Usage: local itemInfo = " +
                    "C_EncounterJournal.GetLootInfo(id)";
                var id = RequiredInt32(state, 1, usage);
                journal.LootById.TryGetValue(id, out var info);
                PushLootInfo(state, info ?? new WowEncounterJournalLootInfo());
                return 1;
            }
            case "GetLootInfoByIndex":
            {
                const string usage =
                    "Usage: local itemInfo = " +
                    "C_EncounterJournal.GetLootInfoByIndex(" +
                    "index [, encounterIndex])";
                var index = RequiredOneBasedIndex(state, 1, usage);
                var encounterIndex = OptionalOneBasedIndex(
                    state,
                    2,
                    usage);
                journal.LootListOutOfDate = false;
                if (!journal.LootByIndex.TryGetValue(
                        (index, encounterIndex),
                        out var info))
                {
                    return 0;
                }
                PushLootInfo(state, info);
                return 1;
            }
            case "GetSectionIconFlags":
            {
                const string usage =
                    "Usage: local iconFlags = " +
                    "C_EncounterJournal.GetSectionIconFlags(sectionID)";
                var sectionId = RequiredInt32(state, 1, usage);
                if (!journal.SectionIconFlags.TryGetValue(
                        sectionId,
                        out var flags))
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushIntegerArray(state, flags);
                return 1;
            }
            case "GetSectionInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_EncounterJournal.GetSectionInfo(sectionID)";
                var sectionId = RequiredInt32(state, 1, usage);
                if (!journal.Sections.TryGetValue(sectionId, out var info))
                    return 0;
                PushSectionInfo(state, info);
                return 1;
            }
            case "GetSlotFilter":
                lua_pushinteger(state, journal.SlotFilter);
                return 1;
            case "EJ_GetInstanceByIndex":
            {
                const string usage =
                    "Usage: EJ_GetInstanceByIndex(index, isRaid)";
                var index = RequiredInt32(state, 1, usage);
                if (lua_type(state, 2) != LUA_TBOOLEAN)
                    return luaL_error(state, usage);
                if (provider is null)
                    return 0;
                var raid = Boolean(state, 2);
                var tier = TierAt(provider, journal.CurrentTierIndex);
                if (tier is null || index < 1)
                    return 0;
                var instances = provider.GetInstances(tier.Id, raid);
                if (index > instances.Count)
                    return 0;
                var instance = instances[index - 1];
                lua_pushinteger(state, instance.Id);
                return 1 + PushInstanceInfo(state, instance);
            }
            case "EJ_GetInstanceInfo":
            {
                if (provider is null)
                    return 0;
                const string usage = "Usage: EJ_GetInstanceInfo([ID])";
                var id = lua_isnumber(state, 1) != 0
                    ? RequiredInt32(state, 1, usage)
                    : journal.SelectedInstanceId ?? 0;
                if (!provider.TryGetInstance(id, out var instance))
                    return 0;
                return PushInstanceInfo(state, instance);
            }
            case "EJ_SelectInstance":
            {
                const string usage = "Usage: EJ_SelectInstance(ID)";
                var id = RequiredInt32(state, 1, usage);
                journal.LootListOutOfDate = true;
                journal.SelectedEncounterId = null;
                if (provider?.TryGetInstance(id, out var instance) != true)
                {
                    journal.SelectedInstanceId = null;
                    journal.SelectedInstanceIsRaid = false;
                    return luaL_error(
                        state,
                        "Bad Instance ID or Instance ID needs to be " +
                        "added to a JournalTierXInstance.  " + usage);
                }
                journal.SelectedInstanceId = id;
                journal.SelectedInstanceIsRaid = instance.IsRaid;
                return 0;
            }
            case "EJ_SelectTier":
            {
                const string usage = "Usage: EJ_SelectTier(index)";
                var index = RequiredInt32(state, 1, usage);
                if (provider is null ||
                    index < 1 ||
                    index > provider.Tiers.Count)
                {
                    return luaL_error(
                        state,
                        $"{usage} Invalid index {index}");
                }
                journal.CurrentTierIndex = index;
                return 0;
            }
            case "EJ_SelectEncounter":
            {
                const string usage = "Usage: EJ_SelectEncounter(ID)";
                var id = RequiredInt32(state, 1, usage);
                if (!journal.Encounters.ContainsKey(id))
                    return luaL_error(state, "Bad Encounter ID.  " + usage);
                journal.SelectedEncounterId = id;
                journal.LootListOutOfDate = true;
                return 0;
            }
            case "InitalizeSelectedTier":
                journal.CurrentTierIndex = Math.Clamp(
                    runtime.Account.ServerExpansionLevel + 1,
                    1,
                    Math.Max(1, provider?.Tiers.Count ?? 1));
                journal.SelectedInstanceId = null;
                return 0;
            case "InstanceHasLoot":
            {
                const string usage =
                    "Usage: local hasLoot = " +
                    "C_EncounterJournal.InstanceHasLoot([instanceID])";
                var instanceId = OptionalInt32(state, 1, usage) ??
                    journal.SelectedInstanceId;
                lua_pushboolean(
                    state,
                    instanceId.HasValue &&
                    journal.InstanceIdsWithLoot.Contains(instanceId.Value)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsEncounterComplete":
            {
                const string usage =
                    "Usage: local isEncounterComplete = " +
                    "C_EncounterJournal.IsEncounterComplete(" +
                    "journalEncounterID)";
                var encounterId = RequiredInt32(state, 1, usage);
                lua_pushboolean(
                    state,
                    journal.CompletedEncounterIds.Contains(encounterId)
                        ? 1
                        : 0);
                return 1;
            }
            case "OnClose":
                journal.CloseRequestCount++;
                return 0;
            case "OnOpen":
                journal.OpenRequestCount++;
                return 0;
            case "ResetSlotFilter":
                journal.SlotFilter = 15;
                journal.LootListOutOfDate = true;
                return 0;
            case "SetPreviewMythicPlusLevel":
            {
                const string usage =
                    "Usage: C_EncounterJournal." +
                    "SetPreviewMythicPlusLevel(level)";
                journal.PreviewMythicPlusLevel = Math.Max(
                    2,
                    RequiredInt32(state, 1, usage));
                return 0;
            }
            case "SetPreviewPvpTier":
            {
                const string usage =
                    "Usage: C_EncounterJournal.SetPreviewPvpTier(tier)";
                journal.PreviewPvpTier = Math.Max(
                    -1,
                    RequiredInt32(state, 1, usage));
                return 0;
            }
            case "SetSlotFilter":
            {
                const string usage =
                    "Usage: C_EncounterJournal.SetSlotFilter(filterSlot)";
                var slot = RequiredInt32(state, 1, usage);
                if (slot is < 0 or > 15)
                    return luaL_error(state, usage);
                journal.SlotFilter = (byte)slot;
                journal.LootListOutOfDate = true;
                return 0;
            }
            case "SetTab":
            {
                const string usage =
                    "Usage: C_EncounterJournal.SetTab(tabIdx)";
                journal.SelectedTab = RequiredInt32(state, 1, usage);
                return 0;
            }
            case "StartArathiRPE":
                journal.StartArathiRpeRequestCount++;
                return 0;
            case "EJ_GetNumTiers":
                lua_pushinteger(state, provider?.Tiers.Count ?? 0);
                return 1;
            case "EJ_GetCurrentTier":
                lua_pushinteger(state, journal.CurrentTierIndex);
                return 1;
            case "EJ_GetTierInfo":
            {
                const string usage = "Usage: EJ_GetTierInfo(index)";
                var index = RequiredInt32(state, 1, usage);
                var tier = provider is null ? null : TierAt(provider, index);
                if (tier is null)
                    return luaL_error(
                        state,
                        $"{usage} Invalid index {index}");
                lua_pushstring(state, tier.Name);
                lua_pushstring(
                    state,
                    tier.Link ?? BuildJournalLink(3, index - 1, tier.Name, 0));
                return 2;
            }
            case "EJ_InstanceIsRaid":
                lua_pushboolean(state, journal.SelectedInstanceIsRaid ? 1 : 0);
                return 1;
            case "EJ_GetEncounterInfo":
            {
                const string usage = "Usage: EJ_GetEncounterInfo(ID)";
                var id = RequiredInt32(state, 1, usage);
                return journal.Encounters.TryGetValue(id, out var encounter)
                    ? PushLegacyEncounterInfo(state, encounter)
                    : 0;
            }
            case "EJ_GetEncounterInfoByIndex":
            {
                const string usage =
                    "Usage: EJ_GetEncounterInfoByIndex(index, [instanceID])";
                var index = RequiredInt32(state, 1, usage);
                var instanceId = lua_isnumber(state, 2) != 0
                    ? RequiredInt32(state, 2, usage)
                    : journal.SelectedInstanceId ?? 0;
                if (index < 1 ||
                    !journal.EncounterIdsByInstanceId.TryGetValue(
                        instanceId,
                        out var encounterIds) ||
                    index > encounterIds.Count ||
                    !journal.Encounters.TryGetValue(
                        encounterIds[index - 1],
                        out var encounter))
                {
                    return 0;
                }
                return PushLegacyEncounterInfo(state, encounter);
            }
            case "EJ_GetCreatureInfo":
            {
                const string usage =
                    "Usage: EJ_GetCreatureInfo(index, [encounterID]";
                var index = RequiredInt32(state, 1, usage);
                var encounterId = lua_isnumber(state, 2) != 0
                    ? RequiredInt32(state, 2, usage)
                    : journal.SelectedEncounterId ?? 0;
                if (index < 1 ||
                    !journal.CreaturesByEncounterId.TryGetValue(
                        encounterId,
                        out var creatures) ||
                    index > creatures.Count)
                {
                    return 0;
                }
                return PushLegacyCreatureInfo(state, creatures[index - 1]);
            }
            case "EJ_GetLootFilter":
                lua_pushinteger(state, journal.LootClassId);
                lua_pushinteger(state, journal.LootSpecId);
                return 2;
            case "EJ_SetLootFilter":
            {
                const string usage =
                    "Usage: EJ_SetLootFilter(classID, specID)";
                if (lua_isnumber(state, 1) == 0 &&
                    lua_isnumber(state, 2) == 0)
                {
                    return luaL_error(state, usage);
                }
                journal.LootClassId = Integer(state, 1);
                journal.LootSpecId = Integer(state, 2);
                journal.LootListOutOfDate = true;
                return 0;
            }
            case "EJ_ResetLootFilter":
                journal.LootClassId = 0;
                journal.LootSpecId = 0;
                journal.LootListOutOfDate = true;
                return 0;
            case "EJ_GetNumLoot":
                journal.LootListOutOfDate = false;
                lua_pushinteger(state, journal.LegacyLootEncounterCounts.Count);
                return 1;
            case "EJ_GetNumEncountersForLootByIndex":
            {
                const string usage =
                    "Usage: EJ_GetNumEncountersForLootByIndex(index)";
                var index = RequiredInt32(state, 1, usage);
                if (index < 1 ||
                    index > journal.LegacyLootEncounterCounts.Count)
                {
                    return luaL_error(
                        state,
                        $"{usage}: Invalid Index");
                }
                journal.LootListOutOfDate = false;
                lua_pushinteger(
                    state,
                    journal.LegacyLootEncounterCounts[index - 1]);
                return 1;
            }
            case "EJ_GetContentTuningID":
                lua_pushinteger(state, journal.ContentTuningId);
                return 1;
            case "EJ_GetDifficulty":
                lua_pushinteger(state, journal.DifficultyId);
                return 1;
            case "EJ_GetInvTypeSortOrder":
            {
                const string usage =
                    "Usage: EJ_GetInvTypeSortOrder(invType)";
                var inventoryType = RequiredInt32(state, 1, usage);
                if (inventoryType < 1 ||
                    inventoryType > journal.InventoryTypeSortOrder.Count)
                {
                    return 0;
                }
                lua_pushinteger(
                    state,
                    journal.InventoryTypeSortOrder[inventoryType - 1]);
                return 1;
            }
            case "EJ_SetDifficulty":
            {
                const string usage =
                    "Usage: EJ_SetDifficulty(difficulty)";
                var difficultyId = unchecked(
                    (short)RequiredInt32(state, 1, usage));
                if (difficultyId == 233 ||
                    difficultyId is >= 1 and <= 63)
                {
                    journal.DifficultyId = difficultyId;
                    journal.LootListOutOfDate = true;
                }
                return 0;
            }
            case "EJ_IsValidInstanceDifficulty":
            {
                const string usage =
                    "Usage: EJ_IsValidInstanceDifficulty(difficulty)";
                var difficultyId = unchecked(
                    (ushort)RequiredInt32(state, 1, usage));
                var valid = journal.SelectedInstanceId is { } instanceId &&
                    journal.ValidDifficultyIdsByInstanceId.TryGetValue(
                        instanceId,
                        out var difficulties) &&
                    difficulties.Contains(difficultyId);
                lua_pushboolean(state, valid ? 1 : 0);
                return 1;
            }
            case "EJ_SetSearch":
            {
                const string usage =
                    "Usage: EJ_SetSearch(search string)";
                journal.SearchText = RequiredStringValue(state, 1, usage);
                journal.SearchHasPendingWork = true;
                journal.SearchIsEnding = false;
                return 0;
            }
            case "EJ_ClearSearch":
                journal.SearchText = string.Empty;
                journal.SearchHasPendingWork = true;
                journal.SearchIsEnding = false;
                return 0;
            case "EJ_EndSearch":
                journal.SearchIsEnding = true;
                journal.SearchHasPendingWork = false;
                return 0;
            case "EJ_GetNumSearchResults":
                lua_pushinteger(state, journal.SearchResults.Count);
                return 1;
            case "EJ_GetSearchResult":
            {
                const string usage = "Usage: GetSearchResult(index)";
                var index = RequiredInt32(state, 1, usage);
                if (index < 1 || index > journal.SearchResults.Count)
                    return 0;
                return PushSearchResult(
                    state,
                    journal.SearchResults[index - 1]);
            }
            case "EJ_GetSearchProgress":
                lua_pushinteger(
                    state,
                    journal.SearchProgress + journal.SearchResults.Count);
                return 1;
            case "EJ_GetSearchSize":
                lua_pushinteger(state, journal.SearchSize);
                return 1;
            case "CanShowEncounterJournal":
                lua_pushboolean(state, 1);
                return 1;
            case "EJ_IsSearchFinished":
                lua_pushboolean(
                    state,
                    journal.SearchSize != 0 &&
                    !journal.SearchHasPendingWork &&
                    !journal.SearchIsEnding
                        ? 1
                        : 0);
                return 1;
            case "EJ_IsLootListOutOfDate":
                lua_pushboolean(state, journal.LootListOutOfDate ? 1 : 0);
                return 1;
            case "EJ_GetMapEncounter":
                return GetLegacyMapEncounter(state, journal);
            case "EJ_GetInstanceForMap":
            {
                const string usage =
                    "Usage: EJ_GetInstanceForMap(mapID)";
                var mapId = RequiredInt32(state, 1, usage);
                journal.InstanceIdsByGameMapId.TryGetValue(
                    mapId,
                    out var instanceId);
                lua_pushinteger(state, instanceId);
                return 1;
            }
            case "EJ_HandleLinkPath":
                return HandleLegacyLinkPath(
                    state,
                    journal,
                    provider);
            case "EJ_GetSectionPath":
            {
                const string usage = "Usage: EJ_GetSectionPath(id)";
                var sectionId = RequiredInt32(state, 1, usage);
                return PushSectionPath(state, journal, sectionId);
            }
            default:
                return 0;
        }
    }

    private static int PushLegacyEncounterInfo(
        lua_State state,
        WowEncounterJournalEncounter encounter)
    {
        lua_pushstring(state, encounter.Name);
        lua_pushstring(state, encounter.Description);
        lua_pushinteger(state, encounter.Id);
        lua_pushinteger(state, encounter.RootSectionId);
        lua_pushstring(
            state,
            BuildJournalLink(
                1,
                encounter.Id,
                encounter.Name,
                0));
        lua_pushinteger(state, encounter.JournalInstanceId);
        PushOptionalInteger(state, encounter.DungeonEncounterId);
        PushOptionalInteger(state, encounter.InstanceId);
        return 8;
    }

    private static int PushLegacyCreatureInfo(
        lua_State state,
        WowEncounterJournalCreature creature)
    {
        lua_pushinteger(state, creature.CreatureId);
        lua_pushstring(state, creature.Name);
        lua_pushstring(state, creature.Description);
        lua_pushinteger(state, creature.DisplayInfoId);
        if (creature.IconImage.HasValue)
            lua_pushinteger(state, creature.IconImage.Value);
        else
            lua_pushnil(state);
        lua_pushinteger(state, creature.UiModelSceneId);
        return 6;
    }

    private static int PushSearchResult(
        lua_State state,
        WowEncounterJournalSearchResult result)
    {
        lua_pushinteger(state, result.Type);
        lua_pushinteger(state, result.Id);
        lua_pushinteger(state, result.DifficultyId);
        PushOptionalInteger(state, result.JournalInstanceId);
        PushOptionalInteger(state, result.EncounterId);
        if (result.DisplayName is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, result.DisplayName);
        return 6;
    }

    private static int GetLegacyMapEncounter(
        lua_State state,
        WowEncounterJournalState journal)
    {
        const string usage =
            "Usage: EJ_GetMapEncounter(mapID, index, fromJournal";
        var mapId = RequiredInt32(state, 1, usage);
        var index = RequiredInt32(state, 2, usage);
        var fromJournal = Boolean(state, 3);
        if (!fromJournal ||
            index < 1 ||
            !journal.LegacyMapEncountersByMapId.TryGetValue(
                mapId,
                out var mapEncounters) ||
            index > mapEncounters.Count)
        {
            return 0;
        }

        var mapEncounter = mapEncounters[index - 1];
        if (!journal.Encounters.TryGetValue(
                mapEncounter.EncounterId,
                out var encounter))
        {
            return 0;
        }
        lua_pushnumber(state, mapEncounter.MapX);
        lua_pushnumber(state, mapEncounter.MapY);
        lua_pushinteger(state, mapEncounter.JournalInstanceId);
        return 3 + PushLegacyEncounterInfo(state, encounter);
    }

    private static int HandleLegacyLinkPath(
        lua_State state,
        WowEncounterJournalState journal,
        IWowEncounterJournalProvider? provider)
    {
        const string usage = "Usage: EJ_HandleLinkPath(type, id)";
        if (lua_isnumber(state, 1) == 0 &&
            lua_isnumber(state, 2) == 0)
        {
            return luaL_error(state, usage);
        }

        var type = Integer(state, 1);
        var id = Integer(state, 2);
        int? journalInstanceId = null;
        int? encounterId = null;
        int? sectionId = null;
        int? tierIndex = null;
        switch (type)
        {
            case 0:
                if (provider?.TryGetInstance(id, out _) == true)
                    journalInstanceId = id;
                break;
            case 1:
                if (journal.Encounters.TryGetValue(id, out var encounter))
                {
                    journalInstanceId = encounter.JournalInstanceId;
                    encounterId = id;
                }
                break;
            case 2:
                if (journal.EncounterIdsBySectionId.TryGetValue(
                        id,
                        out var sectionEncounterId) &&
                    journal.Encounters.TryGetValue(
                        sectionEncounterId,
                        out var sectionEncounter))
                {
                    journalInstanceId =
                        sectionEncounter.JournalInstanceId;
                    encounterId = sectionEncounterId;
                    sectionId = id;
                }
                break;
            case 3:
                tierIndex = id;
                break;
        }

        PushOptionalInteger(state, journalInstanceId);
        PushOptionalInteger(state, encounterId);
        PushOptionalInteger(state, sectionId);
        PushOptionalInteger(state, tierIndex);
        return 4;
    }

    private static int PushSectionPath(
        lua_State state,
        WowEncounterJournalState journal,
        int sectionId)
    {
        var count = 0;
        var visited = new HashSet<int>();
        while (journal.Sections.ContainsKey(sectionId) &&
               visited.Add(sectionId))
        {
            lua_pushinteger(state, sectionId);
            count++;
            if (!journal.ParentSectionIds.TryGetValue(
                    sectionId,
                    out sectionId))
            {
                break;
            }
        }
        return count;
    }

    private static void PushOptionalInteger(
        lua_State state,
        int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static string BuildJournalLink(
        int type,
        int id,
        string displayText,
        int difficultyId) =>
        $"|cffff00ff|Hjournal:{type}:{id}:{difficultyId}|" +
        $"h[{displayText}]|h|r";

    private static WowEncounterJournalTier? TierAt(
        IWowEncounterJournalProvider provider,
        int oneBasedIndex) =>
        oneBasedIndex >= 1 && oneBasedIndex <= provider.Tiers.Count
            ? provider.Tiers[oneBasedIndex - 1]
            : null;

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        SetEnum(
            state,
            "ItemSlotFilterType",
            [
                ("Head", 0), ("Neck", 1), ("Shoulder", 2), ("Cloak", 3),
                ("Chest", 4), ("Wrist", 5), ("Hand", 6), ("Waist", 7),
                ("Legs", 8), ("Feet", 9), ("MainHand", 10),
                ("OffHand", 11), ("Finger", 12), ("Trinket", 13),
                ("Other", 14), ("NoFilter", 15)
            ]);
        SetEnumMeta(state, "ItemSlotFilterTypeMeta", 16, 0, 15);

        SetEnum(
            state,
            "JournalEncounterFlags",
            [
                ("Obsolete", 1), ("LimitDifficulties", 2),
                ("AllianceOnly", 4), ("HordeOnly", 8), ("NoMap", 16),
                ("InternalOnly", 32), ("DoNotDisplayEncounter", 64)
            ]);
        SetEnumMeta(state, "JournalEncounterFlagsMeta", 7, 1, 64);

        SetEnum(
            state,
            "JournalEncounterIconFlags",
            [
                ("Tank", 1), ("Dps", 2), ("Healer", 4), ("Heroic", 8),
                ("Deadly", 16), ("Important", 32), ("Interruptible", 64),
                ("Magic", 128), ("Curse", 256), ("Poison", 512),
                ("Disease", 1024), ("Enrage", 2048), ("Mythic", 4096),
                ("Bleed", 8192)
            ]);
        SetEnumMeta(state, "JournalEncounterIconFlagsMeta", 14, 1, 8192);

        SetEnum(
            state,
            "JournalEncounterItemFlags",
            [
                ("Obsolete", 1), ("LimitDifficulties", 2),
                ("DisplayAsPerPlayerLoot", 4),
                ("DisplayAsVeryRare", 8),
                ("DisplayAsExtremelyRare", 16)
            ]);
        SetEnumMeta(state, "JournalEncounterItemFlagsMeta", 5, 1, 16);

        SetEnum(state, "JournalEncounterLocFlags", [("Primary", 1)]);
        SetEnumMeta(state, "JournalEncounterLocFlagsMeta", 1, 1, 1);
        SetEnum(
            state,
            "JournalEncounterSectionFlags",
            [("StartExpanded", 1), ("LimitDifficulties", 2)]);
        SetEnumMeta(
            state,
            "JournalEncounterSectionFlagsMeta",
            2,
            1,
            2);
        SetEnum(
            state,
            "JournalEncounterSecTypes",
            [
                ("Generic", 0), ("Creature", 1), ("Ability", 2),
                ("Overview", 3)
            ]);
        SetEnumMeta(state, "JournalEncounterSecTypesMeta", 4, 0, 3);
        SetEnum(
            state,
            "JournalInstanceFlags",
            [
                ("Timewalker", 1),
                ("HideUserSelectableDifficulty", 2),
                ("DoNotDisplayInstance", 4)
            ]);
        SetEnumMeta(state, "JournalInstanceFlagsMeta", 3, 1, 4);
        SetEnum(
            state,
            "JournalLinkTypes",
            [
                ("Instance", 0), ("Encounter", 1), ("Section", 2),
                ("Tier", 3)
            ]);
        SetEnumMeta(state, "JournalLinkTypesMeta", 4, 0, 3);
        lua_pop(state, 1);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        IEnumerable<(string Name, int Value)> values)
    {
        var entries = values.ToArray();
        lua_createtable(state, 0, entries.Length);
        foreach (var entry in entries)
            SetInteger(state, entry.Name, entry.Value);
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

    private static void PushDungeonEntrances(
        lua_State state,
        IList<WowEncounterJournalDungeonEntrance> entrances)
    {
        lua_createtable(state, entrances.Count, 0);
        for (var index = 0; index < entrances.Count; index++)
        {
            var entrance = entrances[index];
            lua_createtable(state, 0, 6);
            SetInteger(state, "areaPoiID", entrance.AreaPoiId);
            PushVector2(state, entrance.X, entrance.Y);
            lua_setfield(state, -2, "position");
            SetOptionalString(state, "name", entrance.Name);
            SetOptionalString(state, "description", entrance.Description);
            SetString(state, "atlasName", entrance.AtlasName);
            SetInteger(
                state,
                "journalInstanceID",
                entrance.JournalInstanceId);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushMapEncounters(
        lua_State state,
        IList<WowEncounterJournalMapEncounter> encounters)
    {
        lua_createtable(state, encounters.Count, 0);
        for (var index = 0; index < encounters.Count; index++)
        {
            var encounter = encounters[index];
            lua_createtable(state, 0, 3);
            SetInteger(state, "encounterID", encounter.EncounterId);
            SetNumber(state, "mapX", encounter.MapX);
            SetNumber(state, "mapY", encounter.MapY);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushLootInfo(
        lua_State state,
        WowEncounterJournalLootInfo info)
    {
        lua_createtable(state, 0, 15);
        SetInteger(state, "itemID", info.ItemId);
        SetOptionalInteger(state, "encounterID", info.EncounterId);
        SetOptionalString(state, "name", info.Name);
        SetOptionalString(state, "itemQuality", info.ItemQuality);
        SetOptionalInteger(state, "filterType", info.FilterType);
        SetOptionalUnsignedInteger(state, "icon", info.Icon);
        SetOptionalString(state, "slot", info.Slot);
        SetOptionalString(state, "armorType", info.ArmorType);
        SetOptionalString(state, "link", info.Link);
        SetOptionalBoolean(state, "handError", info.HandError);
        SetOptionalBoolean(
            state,
            "weaponTypeError",
            info.WeaponTypeError);
        SetOptionalBoolean(
            state,
            "displayAsPerPlayerLoot",
            info.DisplayAsPerPlayerLoot);
        SetOptionalBoolean(
            state,
            "displayAsVeryRare",
            info.DisplayAsVeryRare);
        SetOptionalBoolean(
            state,
            "displayAsExtremelyRare",
            info.DisplayAsExtremelyRare);
        SetOptionalInteger(
            state,
            "displaySeasonID",
            info.DisplaySeasonId);
    }

    private static void PushSectionInfo(
        lua_State state,
        WowEncounterJournalSectionInfo info)
    {
        lua_createtable(state, 0, 12);
        SetInteger(state, "spellID", info.SpellId);
        SetOptionalString(state, "title", info.Title);
        SetOptionalString(state, "description", info.Description);
        SetInteger(state, "headerType", info.HeaderType);
        SetUnsignedInteger(state, "abilityIcon", info.AbilityIcon);
        SetInteger(
            state,
            "creatureDisplayID",
            info.CreatureDisplayId);
        SetInteger(state, "uiModelSceneID", info.UiModelSceneId);
        SetOptionalInteger(
            state,
            "siblingSectionID",
            info.SiblingSectionId);
        SetOptionalInteger(
            state,
            "firstChildSectionID",
            info.FirstChildSectionId);
        SetBoolean(
            state,
            "filteredByDifficulty",
            info.FilteredByDifficulty);
        SetString(state, "link", info.Link);
        SetBoolean(state, "startsOpen", info.StartsOpen);
    }

    private static void PushIntegerArray(
        lua_State state,
        IList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushVector2(
        lua_State state,
        double x,
        double y)
    {
        lua_createtable(state, 0, 2);
        SetNumber(state, "x", x);
        SetNumber(state, "y", y);

        var target = lua_gettop(state);
        lua_getglobal(state, "Vector2DMixin");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }
        var mixin = lua_gettop(state);
        lua_pushnil(state);
        while (lua_next(state, mixin) != 0)
        {
            lua_pushvalue(state, -2);
            lua_pushvalue(state, -2);
            lua_settable(state, target);
            lua_pop(state, 1);
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
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (int)number;
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
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < 0 || number > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)number;
    }

    private static string RequiredStringValue(
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

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < 1)
            return luaL_error(state, usage);
        return value;
    }

    private static int? OptionalOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return null;
        return RequiredOneBasedIndex(state, index, usage);
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return null;
        return RequiredInt32(state, index, usage);
    }

    private static void SetInteger(
        lua_State state,
        string field,
        int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetUnsignedInteger(
        lua_State state,
        string field,
        uint value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(
        lua_State state,
        string field,
        string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalUnsignedInteger(
        lua_State state,
        string field,
        uint? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(
        lua_State state,
        string field,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string field,
        bool? value)
    {
        if (value.HasValue)
            lua_pushboolean(state, value.Value ? 1 : 0);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static int PushInstanceInfo(
        lua_State state,
        WowEncounterJournalInstance instance)
    {
        lua_pushstring(state, instance.Name);
        lua_pushstring(state, instance.Description);
        lua_pushinteger(state, instance.BackgroundFileDataId);
        lua_pushinteger(state, instance.ButtonFileDataId);
        lua_pushinteger(state, instance.LoreFileDataId);
        lua_pushinteger(state, instance.ButtonSmallFileDataId);
        lua_pushinteger(state, instance.AreaId);
        lua_pushstring(state, BuildJournalLink(0, instance.Id, instance.Name, 0));
        lua_pushboolean(state, instance.ShouldDisplayDifficulty ? 1 : 0);
        lua_pushinteger(state, instance.MapId);
        lua_pushinteger(state, instance.CovenantId);
        lua_pushboolean(state, instance.IsRaid ? 1 : 0);
        return 12;
    }

    private static int Integer(lua_State state, int index) =>
        lua_isnumber(state, index) != 0 ? (int)lua_tonumber(state, index) : 0;

    private static bool Boolean(lua_State state, int index) =>
        lua_toboolean(state, index) != 0;
}
