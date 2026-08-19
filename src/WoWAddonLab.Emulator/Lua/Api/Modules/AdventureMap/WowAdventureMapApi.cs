using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAdventureMapApi : LuaApiModule
{
    private const int AdventureMapInteractionType = 28;
    private const int AdventureMapPlayerInteractionType = 32;

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "Close",
        "GetAdventureMapTextureKit",
        "GetMapID",
        "GetMapInsetDetailTileInfo",
        "GetMapInsetInfo",
        "GetNumMapInsets",
        "GetNumQuestOffers",
        "GetNumZoneChoices",
        "GetQuestInfo",
        "GetQuestOfferInfo",
        "GetQuestPortraitInfo",
        "GetZoneChoiceInfo",
        "StartQuest"
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
        lua_setglobal(state, "C_AdventureMap");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var adventureMap = runtime.AdventureMap;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "Close":
                Close(runtime);
                return 0;
            case "GetAdventureMapTextureKit":
                PushOptionalString(state, adventureMap.TextureKit);
                return 1;
            case "GetMapID":
                if (!IsAvailable(runtime))
                    return 0;
                lua_pushnumber(state, adventureMap.MapId);
                return 1;
            case "GetNumMapInsets":
                return PushAvailableCount(state, runtime, adventureMap.Insets.Count);
            case "GetNumQuestOffers":
                return PushAvailableCount(state, runtime, adventureMap.QuestOffers.Count);
            case "GetNumZoneChoices":
                return PushAvailableCount(state, runtime, adventureMap.ZoneChoices.Count);
            case "GetZoneChoiceInfo":
                return GetZoneChoiceInfo(state, runtime, adventureMap);
            case "GetQuestOfferInfo":
                return GetQuestOfferInfo(state, runtime, adventureMap);
            case "GetMapInsetInfo":
                return GetMapInsetInfo(state, runtime, adventureMap);
            case "GetMapInsetDetailTileInfo":
                return GetMapInsetDetailTileInfo(state, runtime, adventureMap);
            case "GetQuestInfo":
                return GetQuestInfo(state, runtime, adventureMap);
            case "GetQuestPortraitInfo":
                return GetQuestPortraitInfo(state, runtime, adventureMap);
            case "StartQuest":
                StartQuest(state, runtime, adventureMap);
                return 0;
            default:
                return 0;
        }
    }

    private static void Close(LuaRuntime runtime)
    {
        var interactions = runtime.PlayerInteractions;
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = AdventureMapInteractionType;
        if (!IsAvailable(runtime))
            return;

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
        runtime.TriggerEvent("ADVENTURE_MAP_CLOSE");
    }

    private static int PushAvailableCount(
        lua_State state,
        LuaRuntime runtime,
        int count)
    {
        if (!IsAvailable(runtime))
            return 0;
        lua_pushnumber(state, count);
        return 1;
    }

    private static int GetZoneChoiceInfo(
        lua_State state,
        LuaRuntime runtime,
        WowAdventureMapState adventureMap)
    {
        const string usage = "Usage: GetZoneChoiceInfo(choiceIndex)";
        var index = RequiredInt32(state, 1, usage) - 1;
        if (!IsAvailable(runtime) ||
            index < 0 ||
            index >= adventureMap.ZoneChoices.Count)
        {
            return 0;
        }

        var choice = adventureMap.ZoneChoices[index];
        lua_pushnumber(state, choice.QuestId);
        lua_pushstring(state, choice.TextureKit);
        lua_pushstring(state, choice.Name);
        lua_pushstring(state, choice.Description);
        PushOptionalNumber(state, choice.NormalizedX);
        PushOptionalNumber(state, choice.NormalizedY);
        PushOptionalInteger(state, choice.InsetIndex);
        return 7;
    }

    private static int GetQuestOfferInfo(
        lua_State state,
        LuaRuntime runtime,
        WowAdventureMapState adventureMap)
    {
        const string usage = "Usage: GetQuestOfferInfo(offerIndex)";
        var index = RequiredInt32(state, 1, usage) - 1;
        if (!IsAvailable(runtime) ||
            index < 0 ||
            index >= adventureMap.QuestOffers.Count)
        {
            return 0;
        }

        var offer = adventureMap.QuestOffers[index];
        lua_pushnumber(state, offer.QuestId);
        lua_pushboolean(state, offer.IsTrivial ? 1 : 0);
        lua_pushnumber(state, offer.Frequency);
        lua_pushboolean(state, offer.IsLegendary ? 1 : 0);
        lua_pushstring(state, offer.Title);
        lua_pushstring(state, offer.Description);
        PushOptionalNumber(state, offer.NormalizedX);
        PushOptionalNumber(state, offer.NormalizedY);
        PushOptionalInteger(state, offer.InsetIndex);
        return 9;
    }

    private static int GetMapInsetInfo(
        lua_State state,
        LuaRuntime runtime,
        WowAdventureMapState adventureMap)
    {
        const string usage = "Usage: GetMapInsetInfo(insetIndex)";
        var index = RequiredInt32(state, 1, usage) - 1;
        if (!IsAvailable(runtime) ||
            index < 0 ||
            index >= adventureMap.Insets.Count)
        {
            return 0;
        }

        var inset = adventureMap.Insets[index];
        lua_pushnumber(state, inset.MapId);
        lua_pushstring(state, inset.Title);
        lua_pushstring(state, inset.Description);
        lua_pushstring(state, inset.CollapsedIcon);
        lua_pushnumber(state, inset.AreaTableId);
        lua_pushnumber(state, inset.NumDetailTiles);
        PushOptionalNumber(state, inset.NormalizedX);
        PushOptionalNumber(state, inset.NormalizedY);
        lua_pushnumber(state, inset.LinkId);
        return 9;
    }

    private static int GetMapInsetDetailTileInfo(
        lua_State state,
        LuaRuntime runtime,
        WowAdventureMapState adventureMap)
    {
        const string usage =
            "Usage: GetMapInsetDetailTileInfo(insetIndex, tileIndex)";
        var insetIndex = RequiredInt32(state, 1, usage) - 1;
        var tileIndex = RequiredInt32(state, 2, usage) - 1;
        if (!IsAvailable(runtime) ||
            insetIndex < 0 ||
            insetIndex >= adventureMap.Insets.Count ||
            tileIndex < 0 ||
            tileIndex >= 12)
        {
            return 0;
        }

        var tiles = adventureMap.Insets[insetIndex].DetailTileFileIds;
        if (tileIndex >= tiles.Count || tiles[tileIndex] is not { } fileId)
            return 0;
        lua_pushnumber(state, fileId);
        return 1;
    }

    private static int GetQuestInfo(
        lua_State state,
        LuaRuntime runtime,
        WowAdventureMapState adventureMap)
    {
        const string usage = "Usage: GetQuestInfo(questID)";
        var questId = RequiredInt32(state, 1, usage);
        if (!IsAvailable(runtime) ||
            !adventureMap.Quests.TryGetValue(questId, out var quest))
        {
            return 0;
        }

        lua_pushstring(state, quest.Title);
        lua_pushstring(state, quest.Description);
        lua_pushstring(state, quest.Objective);
        return 3;
    }

    private static int GetQuestPortraitInfo(
        lua_State state,
        LuaRuntime runtime,
        WowAdventureMapState adventureMap)
    {
        const string usage =
            "Usage: local info = C_AdventureMap.GetQuestPortraitInfo(questID)";
        var questId = RequiredInt32(state, 1, usage);
        if (!IsAvailable(runtime) ||
            !adventureMap.QuestPortraits.TryGetValue(questId, out var portrait))
        {
            return 0;
        }

        lua_createtable(state, 0, 5);
        SetNumber(state, "portraitDisplayID", portrait.PortraitDisplayId);
        SetNumber(state, "mountPortraitDisplayID", portrait.MountPortraitDisplayId);
        SetString(state, "name", portrait.Name);
        SetString(state, "text", portrait.Text);
        if (portrait.ModelSceneId is { } modelSceneId)
            SetNumber(state, "modelSceneID", modelSceneId);
        return 1;
    }

    private static void StartQuest(
        lua_State state,
        LuaRuntime runtime,
        WowAdventureMapState adventureMap)
    {
        const string usage = "Usage: StartQuest(questID)";
        var questId = RequiredInt32(state, 1, usage);
        if (IsAvailable(runtime) &&
            (adventureMap.StartableQuestIds.Contains(questId) ||
             adventureMap.Quests.ContainsKey(questId)))
        {
            adventureMap.LastStartedQuestId = questId;
        }
    }

    private static bool IsAvailable(LuaRuntime runtime) =>
        runtime.PlayerInteractions.HasActiveInteraction &&
        runtime.PlayerInteractions.CurrentInteractionType is
            AdventureMapInteractionType or AdventureMapPlayerInteractionType;

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

    private static int RaiseArgumentError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void PushOptionalNumber(lua_State state, double? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetString(lua_State state, string key, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }
}
