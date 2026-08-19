using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowQuestLogApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetMaxNumQuests",
        "GetActiveThreatMaps",
        "GetAllCompletedQuestIDs",
        "GetBountySetInfoForMapID",
        "GetBountiesForMapID",
        "GetNumQuestLogEntries",
        "GetNumQuestWatches",
        "GetNumWorldQuestWatches",
        "GetQuestIDForQuestWatchIndex",
        "GetQuestIDForWorldQuestWatchIndex",
        "GetTitleForQuestID",
        "GetQuestAdditionalHighlights",
        "GetQuestsOnMap",
        "GetZoneStoryInfo",
        "HasActiveThreats",
        "IsOnQuest",
        "IsQuestFlaggedCompleted",
        "IsQuestFlaggedCompletedOnAccount",
        "IsWorldQuest",
        "ReadyForTurnIn",
        "RequestLoadQuestByID",
        "SetMapForQuestPOIs",
        "UpdateCampaignHeaders"
    ];

    private static readonly string[] GlobalFunctions =
    [
        "QuestMapUpdateAllQuests",
        "QuestPOIUpdateIcons",
        "AddAutoQuestPopUp",
        "GetAutoQuestPopUp",
        "GetNumAutoQuestPopUps",
        "GetTasksTable",
        "RemoveAutoQuestPopUp",
        "SortQuests",
        "SortQuestSortTypes"
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
        lua_setglobal(state, "C_QuestLog");

        foreach (var function in GlobalFunctions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setglobal(state, function);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var questLog = LuaBindings.GetRuntime(state).QuestLog;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetMaxNumQuests":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: local maxNumQuests = C_QuestLog.GetMaxNumQuests()");
                lua_pushinteger(state, questLog.MaxNumQuests);
                return 1;
            case "GetNumQuestLogEntries":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: local numQuestLogEntries, numQuests = C_QuestLog.GetNumQuestLogEntries()");
                lua_pushinteger(state, questLog.NumQuestLogEntries);
                lua_pushinteger(state, questLog.NumQuests);
                return 2;
            case "GetNumQuestWatches":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: local numQuestWatches = C_QuestLog.GetNumQuestWatches()");
                lua_pushinteger(state, questLog.QuestWatchIds.Count);
                return 1;
            case "GetNumWorldQuestWatches":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: local numWorldQuestWatches = C_QuestLog.GetNumWorldQuestWatches()");
                lua_pushinteger(state, questLog.WorldQuestWatchIds.Count);
                return 1;
            case "GetQuestIDForQuestWatchIndex":
                return PushWatchQuestId(
                    state,
                    questLog.QuestWatchIds,
                    "Usage: local questID = C_QuestLog.GetQuestIDForQuestWatchIndex(questWatchIndex)");
            case "GetQuestIDForWorldQuestWatchIndex":
                return PushWatchQuestId(
                    state,
                    questLog.WorldQuestWatchIds,
                    "Usage: local questID = C_QuestLog.GetQuestIDForWorldQuestWatchIndex(questWatchIndex)");
            case "GetTitleForQuestID":
                return GetTitleForQuestId(state, LuaBindings.GetRuntime(state), questLog);
            case "GetActiveThreatMaps":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: local uiMapIDs = C_QuestLog.GetActiveThreatMaps()");
                if (questLog.ActiveThreatMaps.Count == 0)
                    return 0;
                PushIntegerArray(state, questLog.ActiveThreatMaps);
                return 1;
            case "GetAllCompletedQuestIDs":
                PushIntegerArray(
                    state,
                    questLog.CompletedQuestIds.Order().ToArray());
                return 1;
            case "GetBountySetInfoForMapID":
                return GetBountySetInfo(state, questLog);
            case "GetZoneStoryInfo":
                return GetZoneStoryInfo(state, questLog);
            case "GetBountiesForMapID":
                return GetBountiesForMapId(state, questLog);
            case "GetQuestsOnMap":
                return GetQuestsOnMap(state, questLog);
            case "HasActiveThreats":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: local hasActiveThreats = C_QuestLog.HasActiveThreats()");
                lua_pushboolean(state, questLog.ActiveThreatMaps.Count > 0 ? 1 : 0);
                return 1;
            case "IsOnQuest":
                return PushQuestSetContains(
                    state,
                    questLog.ActiveQuestIds,
                    "Usage: local isOnQuest = C_QuestLog.IsOnQuest(questID)");
            case "IsQuestFlaggedCompleted":
                return PushQuestSetContains(
                    state,
                    questLog.CompletedQuestIds,
                    "Usage: local isCompleted = C_QuestLog.IsQuestFlaggedCompleted(questID)");
            case "IsQuestFlaggedCompletedOnAccount":
                return PushQuestSetContains(
                    state,
                    questLog.CompletedOnAccountQuestIds,
                    "Usage: local isCompletedOnAccount = C_QuestLog.IsQuestFlaggedCompletedOnAccount(questID)");
            case "IsWorldQuest":
                return PushQuestSetContains(
                    state,
                    questLog.WorldQuestIds,
                    "Usage: local isWorldQuest = C_QuestLog.IsWorldQuest(questID)");
            case "ReadyForTurnIn":
            {
                var questId = RequiredInt32(
                    state,
                    "Usage: local readyForTurnIn = C_QuestLog.ReadyForTurnIn(questID)");
                lua_pushboolean(state, questLog.ReadyForTurnInQuestIds.Contains(questId) ? 1 : 0);
                return 1;
            }
            case "GetQuestAdditionalHighlights":
                return GetQuestAdditionalHighlights(state, questLog);
            case "RequestLoadQuestByID":
                questLog.QuestLoadRequests.Add(
                    RequiredInt32(
                        state,
                        "Usage: C_QuestLog.RequestLoadQuestByID(questID)"));
                return 0;
            case "UpdateCampaignHeaders":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: C_QuestLog.UpdateCampaignHeaders()");
                questLog.CampaignHeaderUpdateCount++;
                return 0;
            case "SetMapForQuestPOIs":
                questLog.QuestPoiMapId = RequiredInt32(
                    state,
                    "Usage: C_QuestLog.SetMapForQuestPOIs(uiMapID)");
                return 0;
            case "QuestMapUpdateAllQuests":
                questLog.QuestMapUpdateCount++;
                lua_pushinteger(state, questLog.QuestMapVisibleQuestCount);
                return 1;
            case "QuestPOIUpdateIcons":
                questLog.QuestPoiUpdateCount++;
                return 0;
            case "GetNumAutoQuestPopUps":
                lua_pushinteger(state, questLog.AutoQuestPopups.Count);
                return 1;
            case "GetAutoQuestPopUp":
                return GetAutoQuestPopUp(state, questLog);
            case "AddAutoQuestPopUp":
                return AddAutoQuestPopUp(state, questLog);
            case "GetTasksTable":
                return GetTasksTable(state, questLog);
            case "RemoveAutoQuestPopUp":
                RemoveAutoQuestPopUp(state, questLog);
                return 0;
            case "SortQuests":
                questLog.QuestSortCount++;
                return 0;
            case "SortQuestSortTypes":
                questLog.QuestSortTypeSortCount++;
                return 0;
            default:
                return 0;
        }
    }

    private static int GetTitleForQuestId(
        lua_State state,
        LuaRuntime runtime,
        WowQuestLogState questLog)
    {
        var questId = RequiredInt32(
            state,
            "Usage: local title = C_QuestLog.GetTitleForQuestID(questID)");
        if (runtime.QuestProvider?.TryGetTitle(questId, out var title) != true &&
            !questLog.QuestTitles.TryGetValue(questId, out title))
        {
            lua_pushnil(state);
            return 1;
        }

        lua_pushstring(state, title);
        return 1;
    }

    private static int GetBountySetInfo(lua_State state, WowQuestLogState questLog)
    {
        var mapId = RequiredInt32(
            state,
            "Usage: local displayLocation, lockQuestID, bountySetID, isActivitySet = " +
            "C_QuestLog.GetBountySetInfoForMapID(uiMapID)");
        if (!questLog.BountySetsByMap.TryGetValue(mapId, out var info))
            return 0;

        lua_pushinteger(state, info.DisplayLocation);
        lua_pushinteger(state, info.LockQuestId);
        lua_pushinteger(state, info.BountySetId);
        lua_pushboolean(state, info.IsActivitySet ? 1 : 0);
        return 4;
    }

    private static int GetZoneStoryInfo(lua_State state, WowQuestLogState questLog)
    {
        var mapId = RequiredInt32(
            state,
            "Usage: local achievementID, storyMapID = C_QuestLog.GetZoneStoryInfo(uiMapID)");
        if (!questLog.ZoneStories.TryGetValue(mapId, out var story))
            return 0;

        lua_pushinteger(state, story.AchievementId);
        lua_pushinteger(state, story.StoryMapId);
        return 2;
    }

    private static int GetBountiesForMapId(lua_State state, WowQuestLogState questLog)
    {
        var mapId = RequiredInt32(
            state,
            "Usage: local bounties = C_QuestLog.GetBountiesForMapID(uiMapID)");
        if (!questLog.BountiesByMap.TryGetValue(mapId, out var bounties))
        {
            lua_pushnil(state);
            return 1;
        }

        lua_createtable(state, bounties.Count, 0);
        for (var index = 0; index < bounties.Count; index++)
        {
            var bounty = bounties[index];
            lua_createtable(state, 0, 5);
            SetIntegerField(state, "questID", bounty.QuestId);
            SetIntegerField(state, "factionID", bounty.FactionId);
            SetIntegerField(state, "icon", bounty.Icon);
            SetIntegerField(state, "numObjectives", bounty.NumObjectives);
            if (bounty.TurninRequirementText is not null)
            {
                lua_pushstring(state, bounty.TurninRequirementText);
                lua_setfield(state, -2, "turninRequirementText");
            }
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetQuestsOnMap(lua_State state, WowQuestLogState questLog)
    {
        var mapId = RequiredInt32(
            state,
            "Usage: local quests = C_QuestLog.GetQuestsOnMap(uiMapID)");
        if (!questLog.QuestsByMap.TryGetValue(mapId, out var quests))
            return 0;

        lua_createtable(state, quests.Count, 0);
        for (var index = 0; index < quests.Count; index++)
        {
            var quest = quests[index];
            lua_createtable(state, 0, 13);
            SetOptionalIntegerField(state, "childDepth", quest.ChildDepth);
            SetOptionalIntegerField(state, "questTagType", quest.QuestTagType);
            SetIntegerField(state, "questID", quest.QuestId);
            SetIntegerField(state, "numObjectives", quest.NumObjectives);
            SetIntegerField(state, "mapID", quest.MapId);
            SetNumberField(state, "x", quest.X);
            SetNumberField(state, "y", quest.Y);
            SetBooleanField(state, "isQuestStart", quest.IsQuestStart);
            SetBooleanField(state, "isDaily", quest.IsDaily);
            SetBooleanField(state, "isCombatAllyQuest", quest.IsCombatAllyQuest);
            SetBooleanField(state, "isMeta", quest.IsMeta);
            SetBooleanField(state, "inProgress", quest.InProgress);
            SetBooleanField(state, "isMapIndicatorQuest", quest.IsMapIndicatorQuest);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetQuestAdditionalHighlights(
        lua_State state,
        WowQuestLogState questLog)
    {
        var questId = RequiredInt32(
            state,
            "Usage: local uiMapID, worldQuests, worldQuestsElite, dungeons, treasures = " +
            "C_QuestLog.GetQuestAdditionalHighlights(questID)");
        var highlights = questLog.AdditionalHighlights.TryGetValue(questId, out var value)
            ? value
            : new WowQuestAdditionalHighlights(0, false, false, false, false);
        lua_pushinteger(state, highlights.UiMapId);
        lua_pushboolean(state, highlights.WorldQuests ? 1 : 0);
        lua_pushboolean(state, highlights.WorldQuestsElite ? 1 : 0);
        lua_pushboolean(state, highlights.Dungeons ? 1 : 0);
        lua_pushboolean(state, highlights.Treasures ? 1 : 0);
        return 5;
    }

    private static int PushQuestSetContains(
        lua_State state,
        HashSet<int> questIds,
        string usage)
    {
        var questId = RequiredInt32(state, usage);
        lua_pushboolean(state, questIds.Contains(questId) ? 1 : 0);
        return 1;
    }

    private static int PushWatchQuestId(
        lua_State state,
        IReadOnlyList<int> questIds,
        string usage)
    {
        var oneBasedIndex = RequiredInt32(state, usage);
        if (oneBasedIndex <= 0 || oneBasedIndex > questIds.Count)
            lua_pushnil(state);
        else
            lua_pushinteger(state, questIds[oneBasedIndex - 1]);
        return 1;
    }

    private static int AddAutoQuestPopUp(lua_State state, WowQuestLogState questLog)
    {
        const string usage = "Usage: AddAutoQuestPopUp(questID, type)";
        if (lua_isnumber(state, 1) == 0 || lua_isstring(state, 2) == 0)
            return luaL_error(state, usage);

        var questId = ToInt32(state, 1, usage);
        var type = (lua_tostring(state, 2) ?? string.Empty).ToUpperInvariant();
        if (type is not ("OFFER" or "COMPLETE"))
            return luaL_error(state, $"AddAutoQuestPopUp: Unknown pop-up type {type}");

        var canAdd = questId != 0 &&
            (questLog.ActiveQuestIds.Contains(questId) ||
             questLog.PendingQuestOfferId == questId);
        var changed = false;
        if (canAdd)
        {
            var index = questLog.AutoQuestPopups.FindIndex(popup => popup.QuestId == questId);
            if (index >= 0)
            {
                if (!string.Equals(
                        questLog.AutoQuestPopups[index].Type,
                        type,
                        StringComparison.Ordinal))
                {
                    questLog.AutoQuestPopups[index] = new WowAutoQuestPopup(questId, type);
                    changed = true;
                }
            }
            else
            {
                questLog.AutoQuestPopups.Add(new WowAutoQuestPopup(questId, type));
                changed = true;
            }
        }

        lua_pushboolean(state, changed ? 1 : 0);
        return 1;
    }

    private static int GetAutoQuestPopUp(lua_State state, WowQuestLogState questLog)
    {
        const string usage = "Usage: GetAutoQuestPopUp(index)";
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);

        var oneBasedIndex = ToInt32(state, 1, usage);
        if (oneBasedIndex <= 0 || oneBasedIndex > questLog.AutoQuestPopups.Count)
            return 0;

        var popup = questLog.AutoQuestPopups[oneBasedIndex - 1];
        lua_pushinteger(state, popup.QuestId);
        lua_pushstring(state, popup.Type);
        return 2;
    }

    private static int GetTasksTable(lua_State state, WowQuestLogState questLog)
    {
        if (lua_gettop(state) > 0 && lua_isnoneornil(state, 1) == 0)
        {
            if (lua_istable(state, 1) == 0)
                return luaL_error(state, "Usage: GetTasksTable([table])");
            lua_pushvalue(state, 1);
        }
        else
        {
            lua_newtable(state);
        }

        for (var index = 0; index < questLog.TaskQuestIds.Count; index++)
        {
            lua_pushinteger(state, questLog.TaskQuestIds[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void RemoveAutoQuestPopUp(lua_State state, WowQuestLogState questLog)
    {
        var questId = lua_isnumber(state, 1) != 0
            ? ToInt32(state, 1, "Usage: RemoveAutoQuestPopUp([questID])")
            : questLog.SelectedQuestId.GetValueOrDefault();
        if (questId != 0)
            questLog.AutoQuestPopups.RemoveAll(popup => popup.QuestId == questId);
    }

    private static int RequiredInt32(lua_State state, string usage)
    {
        RequireArgumentCount(state, 1, usage);
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);
        return ToInt32(state, 1, usage);
    }

    private static int ToInt32(lua_State state, int index, string usage)
    {
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return (int)value;
    }

    private static void RequireArgumentCount(
        lua_State state,
        int expected,
        string usage)
    {
        if (lua_gettop(state) != expected)
            luaL_error(state, usage);
    }

    private static void PushIntegerArray(lua_State state, IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetIntegerField(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalIntegerField(lua_State state, string name, int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushinteger(state, value.Value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumberField(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBooleanField(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }
}
