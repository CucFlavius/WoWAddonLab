using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTaskQuestApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "DoesMapShowTaskQuestObjectives",
        "GetQuestInfoByQuestID",
        "GetQuestLocation",
        "GetQuestProgressBarInfo",
        "GetQuestTimeLeftMinutes",
        "GetQuestTimeLeftSeconds",
        "GetQuestUIWidgetSetByType",
        "GetQuestZoneID",
        "GetQuestsOnMap",
        "GetThreatQuests",
        "IsActive",
        "RequestPreloadRewardData"
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
        lua_setglobal(state, "C_TaskQuest");
    }

    private static int Dispatch(lua_State state)
    {
        var taskQuest = LuaBindings.GetRuntime(state).TaskQuest;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "DoesMapShowTaskQuestObjectives":
            {
                const string usage =
                    "Usage: local showsTaskQuestObjectives = " +
                    "C_TaskQuest.DoesMapShowTaskQuestObjectives(uiMapID)";
                var uiMapId = RequiredInt32(state, 1, usage);
                PushBoolean(
                    state,
                    taskQuest.MapsShowingTaskQuestObjectives.Contains(
                        uiMapId));
                return 1;
            }
            case "GetQuestInfoByQuestID":
            {
                const string usage =
                    "Usage: local questTitle, factionID, capped, " +
                    "displayAsObjective = " +
                    "C_TaskQuest.GetQuestInfoByQuestID(questID)";
                var questId = RequiredInt32(state, 1, usage);
                if (!taskQuest.QuestInfoByQuestId.TryGetValue(
                        questId,
                        out var info))
                {
                    return 0;
                }

                PushOptionalString(state, info.QuestTitle);
                PushOptionalNumber(state, info.FactionId);
                PushOptionalBoolean(state, info.Capped);
                PushOptionalBoolean(state, info.DisplayAsObjective);
                return 4;
            }
            case "GetQuestLocation":
            {
                const string usage =
                    "Usage: local x, y = " +
                    "C_TaskQuest.GetQuestLocation(questID, uiMapID)";
                var questId = RequiredInt32(state, 1, usage);
                var uiMapId = RequiredInt32(state, 2, usage);
                if (!taskQuest.QuestLocations.TryGetValue(
                        (questId, uiMapId),
                        out var location))
                {
                    return 0;
                }

                lua_pushnumber(state, location.X);
                lua_pushnumber(state, location.Y);
                return 2;
            }
            case "GetQuestProgressBarInfo":
            {
                const string usage =
                    "Usage: local progress = " +
                    "C_TaskQuest.GetQuestProgressBarInfo(questID)";
                var questId = RequiredInt32(state, 1, usage);
                if (!taskQuest.ProgressByQuestId.TryGetValue(
                        questId,
                        out var progress))
                {
                    return 0;
                }

                lua_pushnumber(state, progress);
                return 1;
            }
            case "GetQuestTimeLeftMinutes":
            {
                const string usage =
                    "Usage: local minutesLeft = " +
                    "C_TaskQuest.GetQuestTimeLeftMinutes(questID)";
                var questId = RequiredInt32(state, 1, usage);
                if (!taskQuest.SecondsLeftByQuestId.TryGetValue(
                        questId,
                        out var secondsLeft))
                {
                    return 0;
                }

                var minutesLeft = secondsLeft / 60;
                if (secondsLeft != minutesLeft * 60)
                    minutesLeft++;
                lua_pushnumber(state, minutesLeft);
                return 1;
            }
            case "GetQuestTimeLeftSeconds":
            {
                const string usage =
                    "Usage: local secondsLeft = " +
                    "C_TaskQuest.GetQuestTimeLeftSeconds(questID)";
                var questId = RequiredInt32(state, 1, usage);
                if (!taskQuest.SecondsLeftByQuestId.TryGetValue(
                        questId,
                        out var secondsLeft))
                {
                    return 0;
                }

                lua_pushnumber(state, secondsLeft);
                return 1;
            }
            case "GetQuestUIWidgetSetByType":
            {
                const string usage =
                    "Usage: local widgetSet = " +
                    "C_TaskQuest.GetQuestUIWidgetSetByType(questID, type)";
                var questId = RequiredInt32(state, 1, usage);
                var type = RequiredWidgetSetType(state, 2, usage);
                if (!taskQuest.WidgetSets.TryGetValue(
                        (questId, type),
                        out var widgetSet))
                {
                    return 0;
                }

                lua_pushnumber(state, widgetSet);
                return 1;
            }
            case "GetQuestZoneID":
            {
                const string usage =
                    "Usage: local uiMapID = " +
                    "C_TaskQuest.GetQuestZoneID(questID)";
                var questId = RequiredInt32(state, 1, usage);
                if (!taskQuest.ZoneByQuestId.TryGetValue(
                        questId,
                        out var uiMapId))
                {
                    return 0;
                }

                lua_pushnumber(state, uiMapId);
                return 1;
            }
            case "GetQuestsOnMap":
            {
                const string usage =
                    "Usage: local taskPOIs = " +
                    "C_TaskQuest.GetQuestsOnMap(uiMapID)";
                var uiMapId = RequiredInt32(state, 1, usage);
                if (taskQuest.UnavailableQuestMaps.Contains(uiMapId))
                    return 0;

                taskQuest.QuestsByUiMapId.TryGetValue(
                    uiMapId,
                    out var quests);
                PushQuestPoiMapInfoArray(state, quests);
                return 1;
            }
            case "GetThreatQuests":
                PushInt32Array(state, taskQuest.ThreatQuestIds);
                return 1;
            case "IsActive":
            {
                const string usage =
                    "Usage: local active = " +
                    "C_TaskQuest.IsActive(questID)";
                var questId = RequiredInt32(state, 1, usage);
                PushBoolean(
                    state,
                    taskQuest.ActiveQuestIds.Contains(questId));
                return 1;
            }
            case "RequestPreloadRewardData":
            {
                const string usage =
                    "Usage: C_TaskQuest.RequestPreloadRewardData(questID)";
                var questId = RequiredInt32(state, 1, usage);
                if (taskQuest.ActiveQuestIds.Contains(questId))
                    taskQuest.PreloadRewardDataRequests.Add(questId);
                return 0;
            }
            default:
                return 0;
        }
    }

    private static void PushQuestPoiMapInfoArray(
        lua_State state,
        IList<WowTaskQuestPoiMapInfoState>? quests)
    {
        lua_createtable(state, quests?.Count ?? 0, 0);
        if (quests is null)
            return;

        for (var index = 0; index < quests.Count; index++)
        {
            var quest = quests[index];
            lua_createtable(state, 0, 13);
            SetOptionalNumber(state, "childDepth", quest.ChildDepth);
            SetOptionalNumber(
                state,
                "questTagType",
                quest.QuestTagType);
            SetNumber(state, "questID", quest.QuestId);
            SetNumber(state, "numObjectives", quest.NumObjectives);
            SetNumber(state, "mapID", quest.MapId);
            SetNumber(state, "x", quest.X);
            SetNumber(state, "y", quest.Y);
            SetBoolean(state, "isQuestStart", quest.IsQuestStart);
            SetBoolean(state, "isDaily", quest.IsDaily);
            SetBoolean(
                state,
                "isCombatAllyQuest",
                quest.IsCombatAllyQuest);
            SetBoolean(state, "isMeta", quest.IsMeta);
            SetBoolean(state, "inProgress", quest.InProgress);
            SetBoolean(
                state,
                "isMapIndicatorQuest",
                quest.IsMapIndicatorQuest);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushInt32Array(
        lua_State state,
        IList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushnumber(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static byte RequiredWidgetSetType(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        var type = unchecked((byte)value);
        if (type > 2)
            return (byte)RaiseArgumentError(state, usage);
        return type;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return RaiseArgumentError(state, usage);

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return RaiseArgumentError(state, usage);
        }
        return unchecked((int)value);
    }

    private static int RaiseArgumentError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
    }

    private static void PushOptionalString(
        lua_State state,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void PushOptionalNumber(
        lua_State state,
        int? value)
    {
        if (value.HasValue)
            lua_pushnumber(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalBoolean(
        lua_State state,
        bool? value)
    {
        if (value.HasValue)
            PushBoolean(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string field,
        int? value)
    {
        PushOptionalNumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(
        lua_State state,
        string field,
        bool value)
    {
        PushBoolean(state, value);
        lua_setfield(state, -2, field);
    }
}
