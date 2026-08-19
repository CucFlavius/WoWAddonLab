using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGossipInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CloseGossip", "ForceGossip", "GetActiveQuests",
        "GetAvailableQuests", "GetCompletedOptionDescriptionString",
        "GetCustomGossipDescriptionString",
        "GetFriendshipReputation", "GetFriendshipReputationRanks",
        "GetNumActiveQuests", "GetNumAvailableQuests",
        "GetOptionUIWidgetSetsAndTypesByOptionID", "GetOptions",
        "GetPoiForUiMapID", "GetPoiInfo", "GetText",
        "RefreshOptions", "SelectActiveQuest",
        "SelectAvailableQuest", "SelectOption", "SelectOptionByIndex"
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
        lua_setglobal(state, "C_GossipInfo");
    }

    internal static void RegisterEnums(lua_State state)
    {
        lua_newtable(state);
        SetNumber(state, "Item", 0);
        SetNumber(state, "Currency", 1);
        lua_setfield(state, -2, "GossipOptionRewardType");
        PushEnumMeta(state, "GossipOptionRewardTypeMeta", 2, 0, 1);

        lua_newtable(state);
        SetNumber(state, "Available", 0);
        SetNumber(state, "Unavailable", 1);
        SetNumber(state, "Locked", 2);
        SetNumber(state, "AlreadyComplete", 3);
        lua_setfield(state, -2, "GossipOptionStatus");
        PushEnumMeta(state, "GossipOptionStatusMeta", 4, 0, 3);

        lua_newtable(state);
        SetNumber(state, "Modifiers", 0);
        SetNumber(state, "Background", 1);
        lua_setfield(state, -2, "GossipOptionUIWidgetSetTypes");
        PushEnumMeta(
            state,
            "GossipOptionUIWidgetSetTypesMeta",
            2,
            0,
            1);
    }

    private static int Dispatch(lua_State state)
    {
        var gossip = LuaBindings.GetRuntime(state).GossipInfo;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CloseGossip":
                gossip.IsOpen = false;
                gossip.CloseRequests++;
                return 0;
            case "ForceGossip":
                PushBoolean(state, gossip.CanForceGossip);
                return 1;
            case "GetActiveQuests":
                PushQuestArray(state, gossip.ActiveQuests);
                return 1;
            case "GetAvailableQuests":
                PushQuestArray(state, gossip.AvailableQuests);
                return 1;
            case "GetCompletedOptionDescriptionString":
                PushOptionalString(
                    state,
                    gossip.CompletedOptionDescription);
                return 1;
            case "GetCustomGossipDescriptionString":
                PushOptionalString(state, gossip.CustomDescription);
                return 1;
            case "GetFriendshipReputation":
            {
                const string usage =
                    "Usage: local reputationInfo = " +
                    "C_GossipInfo.GetFriendshipReputation(" +
                    "friendshipFactionID)";
                var factionId = RequiredInt32(state, 1, usage);
                if (!gossip.FriendshipReputationByFactionId.TryGetValue(
                        factionId,
                        out var reputation) &&
                    !(LuaBindings.GetRuntime(state).FactionProvider?
                        .TryGetFriendshipReputation(factionId, out reputation) ?? false))
                {
                    if (factionId <= 0)
                        return 0;

                    reputation = new WowGossipFriendshipReputationState(
                        0,
                        0,
                        0,
                        null,
                        string.Empty,
                        0,
                        string.Empty,
                        0,
                        null,
                        false,
                        null);
                }

                PushFriendshipReputation(state, reputation);
                return 1;
            }
            case "GetFriendshipReputationRanks":
            {
                const string usage =
                    "Usage: local rankInfo = " +
                    "C_GossipInfo.GetFriendshipReputationRanks(" +
                    "friendshipFactionID)";
                var factionId = RequiredInt32(state, 1, usage);
                if (!gossip.FriendshipRanksByFactionId.TryGetValue(
                        factionId,
                        out var ranks))
                    LuaBindings.GetRuntime(state).FactionProvider?
                        .TryGetFriendshipRanks(factionId, out ranks);
                lua_createtable(state, 0, 2);
                SetNumber(state, "currentLevel", ranks?.CurrentLevel ?? 0);
                SetNumber(state, "maxLevel", ranks?.MaxLevel ?? 0);
                return 1;
            }
            case "GetNumActiveQuests":
                lua_pushnumber(state, gossip.ActiveQuests.Count);
                return 1;
            case "GetNumAvailableQuests":
                lua_pushnumber(state, gossip.AvailableQuests.Count);
                return 1;
            case "GetOptionUIWidgetSetsAndTypesByOptionID":
            {
                const string usage =
                    "Usage: local gossipOptionUIWidgetSetsAndTypes = " +
                    "C_GossipInfo." +
                    "GetOptionUIWidgetSetsAndTypesByOptionID(" +
                    "gossipOptionID)";
                var optionId = RequiredInt32(state, 1, usage);
                gossip.WidgetSetsByOptionId.TryGetValue(
                    optionId,
                    out var widgetSets);
                PushWidgetSetArray(state, widgetSets);
                return 1;
            }
            case "GetOptions":
                PushOptionArray(state, gossip.Options);
                return 1;
            case "GetPoiForUiMapID":
            {
                const string usage =
                    "Usage: local gossipPoiID = " +
                    "C_GossipInfo.GetPoiForUiMapID(uiMapID)";
                var uiMapId = RequiredInt32(state, 1, usage);
                PushOptionalNumber(
                    state,
                    gossip.PoiIdByUiMapId.TryGetValue(
                        uiMapId,
                        out var poiId)
                        ? poiId
                        : null);
                return 1;
            }
            case "GetPoiInfo":
            {
                const string usage =
                    "Usage: local gossipPoiInfo = " +
                    "C_GossipInfo.GetPoiInfo(uiMapID, gossipPoiID)";
                var uiMapId = RequiredInt32(state, 1, usage);
                var poiId = RequiredInt32(state, 2, usage);
                if (gossip.PoiInfoByMapAndPoiId.TryGetValue(
                        (uiMapId, poiId),
                        out var poi))
                {
                    PushPoiInfo(state, poi);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetText":
                PushOptionalString(state, gossip.Text);
                return 1;
            case "RefreshOptions":
                gossip.RefreshRequests++;
                return 0;
            case "SelectActiveQuest":
            {
                const string usage =
                    "Usage: C_GossipInfo.SelectActiveQuest(optionID)";
                var optionId = RequiredInt32(state, 1, usage);
                if (gossip.ActiveQuests.Any(
                        quest => quest.QuestId == optionId))
                {
                    gossip.SelectionRequests.Add(
                        new WowGossipSelectionRequest(
                            operation,
                            optionId,
                            null,
                            null));
                }
                return 0;
            }
            case "SelectAvailableQuest":
            {
                const string usage =
                    "Usage: C_GossipInfo.SelectAvailableQuest(optionID)";
                var optionId = RequiredInt32(state, 1, usage);
                if (gossip.AvailableQuests.Any(
                        quest => quest.QuestId == optionId))
                {
                    gossip.SelectionRequests.Add(
                        new WowGossipSelectionRequest(
                            operation,
                            optionId,
                            null,
                            null));
                }
                return 0;
            }
            case "SelectOption":
            case "SelectOptionByIndex":
            {
                var usage =
                    $"Usage: C_GossipInfo.{operation}" +
                    "(optionID [, text, confirmed])";
                var optionId = RequiredInt32(state, 1, usage);
                var text = OptionalString(state, 2, usage);
                var confirmed = OptionalBoolean(state, 3, usage);
                gossip.SelectionRequests.Add(
                    new WowGossipSelectionRequest(
                        operation,
                        optionId,
                        text,
                        confirmed));
                return 0;
            }
            default:
                return 0;
        }
    }

    private static void PushQuestArray(
        lua_State state,
        IList<WowGossipQuestInfoState> quests)
    {
        lua_createtable(state, quests.Count, 0);
        for (var index = 0; index < quests.Count; index++)
        {
            var quest = quests[index];
            lua_createtable(state, 0, 12);
            SetString(state, "title", quest.Title);
            SetNumber(state, "questLevel", quest.QuestLevel);
            SetBoolean(state, "isTrivial", quest.IsTrivial);
            SetOptionalNumber(state, "frequency", quest.Frequency);
            SetOptionalBoolean(state, "repeatable", quest.Repeatable);
            SetOptionalBoolean(state, "isComplete", quest.IsComplete);
            SetBoolean(state, "isLegendary", quest.IsLegendary);
            SetBoolean(state, "isIgnored", quest.IsIgnored);
            SetNumber(state, "questID", quest.QuestId);
            SetBoolean(state, "isImportant", quest.IsImportant);
            SetBoolean(state, "isMeta", quest.IsMeta);
            SetNumber(state, "questInfoID", quest.QuestInfoId);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOptionArray(
        lua_State state,
        IList<WowGossipOptionInfoState> options)
    {
        lua_createtable(state, options.Count, 0);
        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            lua_createtable(state, 0, 11);
            SetOptionalNumber(
                state,
                "gossipOptionID",
                option.GossipOptionId);
            SetString(state, "name", option.Name);
            SetFileAsset(state, "icon", option.Icon);
            PushRewardArray(state, option.Rewards);
            lua_setfield(state, -2, "rewards");
            SetNumber(state, "status", option.Status);
            SetOptionalNumber(state, "spellID", option.SpellId);
            SetNumber(state, "flags", option.Flags);
            SetOptionalFileAsset(
                state,
                "overrideIconID",
                option.OverrideIconId);
            SetBoolean(
                state,
                "selectOptionWhenOnlyOption",
                option.SelectOptionWhenOnlyOption);
            SetNumber(state, "orderIndex", option.OrderIndex);
            SetOptionalString(
                state,
                "failureDescription",
                option.FailureDescription);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushRewardArray(
        lua_State state,
        IList<WowGossipOptionRewardInfoState> rewards)
    {
        lua_createtable(state, rewards.Count, 0);
        for (var index = 0; index < rewards.Count; index++)
        {
            var reward = rewards[index];
            lua_createtable(state, 0, 4);
            SetNumber(state, "id", reward.Id);
            SetNumber(state, "quantity", reward.Quantity);
            SetNumber(state, "rewardType", reward.RewardType);
            SetNumber(state, "context", reward.Context);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushFriendshipReputation(
        lua_State state,
        WowGossipFriendshipReputationState reputation)
    {
        lua_createtable(state, 0, 11);
        SetNumber(
            state,
            "friendshipFactionID",
            reputation.FriendshipFactionId);
        SetNumber(state, "standing", reputation.Standing);
        SetNumber(state, "maxRep", reputation.MaxRep);
        SetOptionalString(state, "name", reputation.Name);
        SetString(state, "text", reputation.Text);
        SetNumber(state, "texture", reputation.Texture);
        SetString(state, "reaction", reputation.Reaction);
        SetNumber(
            state,
            "reactionThreshold",
            reputation.ReactionThreshold);
        SetOptionalNumber(
            state,
            "nextThreshold",
            reputation.NextThreshold);
        SetBoolean(
            state,
            "reversedColor",
            reputation.ReversedColor);
        SetOptionalNumber(
            state,
            "overrideColor",
            reputation.OverrideColor);
    }

    private static void PushWidgetSetArray(
        lua_State state,
        IList<WowGossipWidgetSetState>? widgetSets)
    {
        lua_createtable(state, widgetSets?.Count ?? 0, 0);
        if (widgetSets is null)
            return;

        for (var index = 0; index < widgetSets.Count; index++)
        {
            var widgetSet = widgetSets[index];
            lua_createtable(state, 0, 2);
            SetNumber(state, "widgetType", widgetSet.WidgetType);
            SetNumber(state, "uiWidgetSetID", widgetSet.UiWidgetSetId);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushPoiInfo(
        lua_State state,
        WowGossipPoiInfoState poi)
    {
        lua_createtable(state, 0, 4);
        SetString(state, "name", poi.Name);
        SetNumber(state, "textureIndex", poi.TextureIndex);
        lua_createtable(state, 0, 2);
        SetNumber(state, "x", poi.X);
        SetNumber(state, "y", poi.Y);
        lua_setfield(state, -2, "position");
        SetBoolean(state, "inBattleMap", poi.InBattleMap);
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

    private static string? OptionalString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        if (lua_isstring(state, index) == 0)
        {
            RaiseArgumentError(state, usage);
            return null;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static bool? OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            RaiseArgumentError(state, usage);
            return null;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int RaiseArgumentError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void PushEnumMeta(
        lua_State state,
        string name,
        int count,
        int minimum,
        int maximum)
    {
        lua_newtable(state);
        SetNumber(state, "NumValues", count);
        SetNumber(state, "MinValue", minimum);
        SetNumber(state, "MaxValue", maximum);
        lua_setfield(state, -2, name);
    }

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);

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
        PushOptionalString(state, value);
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

    private static void SetOptionalBoolean(
        lua_State state,
        string field,
        bool? value)
    {
        if (value.HasValue)
            PushBoolean(state, value.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetFileAsset(
        lua_State state,
        string field,
        int value)
    {
        if (value == 0)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalFileAsset(
        lua_State state,
        string field,
        int? value)
    {
        if (value is null or 0)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
        lua_setfield(state, -2, field);
    }
}
