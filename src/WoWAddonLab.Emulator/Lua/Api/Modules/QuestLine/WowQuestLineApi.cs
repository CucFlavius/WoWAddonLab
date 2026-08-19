using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowQuestLineApi : LuaApiModule
{
    private const double RequestCacheSeconds = 30;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetAvailableQuestLines",
        "GetForceVisibleQuests",
        "GetQuestLineInfo",
        "GetQuestLineQuests",
        "IsComplete",
        "QuestLineIgnoresAccountCompletedFiltering",
        "RequestQuestLinesForMap"
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
        lua_setglobal(state, "C_QuestLine");
    }

    internal static void RegisterEnums(lua_State state)
    {
        lua_newtable(state);
        SetIntegerField(state, "Above", 0);
        SetIntegerField(state, "Below", 1);
        SetIntegerField(state, "Same", 2);
        lua_setfield(state, -2, "QuestLineFloorLocation");

        lua_newtable(state);
        SetIntegerField(state, "NumValues", 3);
        SetIntegerField(state, "MinValue", 0);
        SetIntegerField(state, "MaxValue", 2);
        lua_setfield(state, -2, "QuestLineFloorLocationMeta");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var questLines = runtime.QuestLines;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAvailableQuestLines":
            {
                var mapId = RequiredInt32(
                    state,
                    1,
                    1,
                    "Usage: local questLines = C_QuestLine.GetAvailableQuestLines(uiMapID)");
                PushQuestLineInfoArray(
                    state,
                    questLines.AvailableQuestLinesByMapId.TryGetValue(mapId, out var lines)
                        ? lines
                        : []);
                return 1;
            }
            case "GetForceVisibleQuests":
            {
                var mapId = RequiredInt32(
                    state,
                    1,
                    1,
                    "Usage: local questIDs = C_QuestLine.GetForceVisibleQuests(uiMapID)");
                PushIntegerArray(
                    state,
                    questLines.ForceVisibleQuestIdsByMapId.TryGetValue(mapId, out var questIds)
                        ? questIds
                        : []);
                return 1;
            }
            case "GetQuestLineInfo":
                return GetQuestLineInfo(state, questLines);
            case "GetQuestLineQuests":
            {
                var questLineId = RequiredInt32(
                    state,
                    1,
                    1,
                    "Usage: local questIDs = C_QuestLine.GetQuestLineQuests(questLineID)");
                PushIntegerArray(
                    state,
                    questLines.QuestIdsByQuestLineId.TryGetValue(
                        questLineId,
                        out var questIds)
                        ? questIds
                        : []);
                return 1;
            }
            case "IsComplete":
            {
                var questLineId = RequiredInt32(
                    state,
                    1,
                    1,
                    "Usage: local isComplete = C_QuestLine.IsComplete(questLineID)");
                lua_pushboolean(
                    state,
                    questLines.CompletedQuestLineIds.Contains(questLineId) ? 1 : 0);
                return 1;
            }
            case "QuestLineIgnoresAccountCompletedFiltering":
            {
                const string usage =
                    "Usage: local questLineIgnoresAccountCompletedFiltering = " +
                    "C_QuestLine.QuestLineIgnoresAccountCompletedFiltering(uiMapID, questLineID)";
                RequireArgumentCount(state, 2, usage);
                var mapId = ReadInt32(state, 1, usage);
                var questLineId = ReadInt32(state, 2, usage);
                lua_pushboolean(
                    state,
                    questLines.IgnoreAccountCompletedFiltering.Contains(
                        (mapId, questLineId))
                        ? 1
                        : 0);
                return 1;
            }
            case "RequestQuestLinesForMap":
            {
                var mapId = RequiredInt32(
                    state,
                    1,
                    1,
                    "Usage: C_QuestLine.RequestQuestLinesForMap(uiMapID)");
                if (!questLines.LastRequestTimeByMapId.TryGetValue(
                        mapId,
                        out var lastRequest) ||
                    runtime.Time - lastRequest >= RequestCacheSeconds)
                {
                    questLines.LastRequestTimeByMapId[mapId] = runtime.Time;
                    questLines.RequestedMapIds.Add(mapId);
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int GetQuestLineInfo(
        lua_State state,
        WowQuestLineState questLines)
    {
        const string usage =
            "Usage: local questLineInfo = " +
            "C_QuestLine.GetQuestLineInfo(questID [, uiMapID, displayableOnly])";
        var argumentCount = lua_gettop(state);
        if (argumentCount is < 1 or > 3)
            return luaL_error(state, usage);

        var questId = ReadInt32(state, 1, usage);
        int? mapId = null;
        if (argumentCount >= 2 && lua_isnoneornil(state, 2) == 0)
            mapId = ReadInt32(state, 2, usage);

        var displayableOnly = false;
        if (argumentCount >= 3 && lua_isnoneornil(state, 3) == 0)
        {
            if (lua_type(state, 3) != LUA_TBOOLEAN)
                return luaL_error(state, usage);
            displayableOnly = lua_toboolean(state, 3) != 0;
        }

        WowQuestLineInfo? info = null;
        if (mapId is { } uiMapId)
            questLines.QuestLineInfoByQuestAndMapId.TryGetValue(
                (questId, uiMapId),
                out info);
        if (info is null)
            questLines.QuestLineInfoByQuestId.TryGetValue(questId, out info);

        if (info is null || displayableOnly && !info.IsDisplayable)
        {
            lua_pushnil(state);
            return 1;
        }

        PushQuestLineInfo(state, info);
        return 1;
    }

    private static void PushQuestLineInfoArray(
        lua_State state,
        IReadOnlyList<WowQuestLineInfo> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            PushQuestLineInfo(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushQuestLineInfo(lua_State state, WowQuestLineInfo value)
    {
        lua_createtable(state, 0, 19);
        SetOptionalStringField(state, "questLineName", value.QuestLineName);
        SetOptionalStringField(state, "questName", value.QuestName);
        SetIntegerField(state, "questLineID", value.QuestLineId);
        SetIntegerField(state, "questID", value.QuestId);
        SetNumberField(state, "x", value.X);
        SetNumberField(state, "y", value.Y);
        SetBooleanField(state, "isHidden", value.IsHidden);
        SetBooleanField(state, "isLegendary", value.IsLegendary);
        SetBooleanField(state, "isLocalStory", value.IsLocalStory);
        SetBooleanField(state, "isDaily", value.IsDaily);
        SetBooleanField(state, "isCampaign", value.IsCampaign);
        SetBooleanField(state, "isImportant", value.IsImportant);
        SetBooleanField(state, "isAccountCompleted", value.IsAccountCompleted);
        SetBooleanField(state, "isCombatAllyQuest", value.IsCombatAllyQuest);
        SetBooleanField(state, "isMeta", value.IsMeta);
        SetBooleanField(state, "inProgress", value.InProgress);
        SetBooleanField(state, "isQuestStart", value.IsQuestStart);
        SetNumberField(state, "floorLocation", value.FloorLocation);
        SetIntegerField(state, "startMapID", value.StartMapId);
    }

    private static void PushIntegerArray(
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

    private static int RequiredInt32(
        lua_State state,
        int index,
        int expectedArguments,
        string usage)
    {
        RequireArgumentCount(state, expectedArguments, usage);
        return ReadInt32(state, index, usage);
    }

    private static int ReadInt32(lua_State state, int index, string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
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

    private static void SetOptionalStringField(
        lua_State state,
        string name,
        string? value)
    {
        if (value is null)
            return;
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetIntegerField(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
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
