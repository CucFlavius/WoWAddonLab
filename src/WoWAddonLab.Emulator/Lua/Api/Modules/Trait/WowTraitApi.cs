using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTraitApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanEditConfig",
        "CascadeRepurchaseRanks",
        "ClearCascadeRepurchaseHistory",
        "CommitConfig",
        "GetConditionInfo",
        "GetConfigIDBySystemID",
        "GetConfigIDByTreeID",
        "GetConfigInfo",
        "GetDefinitionInfo",
        "GetEntryInfo",
        "GetIncreasedTraitData",
        "GetNodeCost",
        "GetNodeInfo",
        "GetSubTreeInfo",
        "GetTraitCurrencyInfo",
        "GetTraitSystemFlags",
        "GetTraitSystemWidgetSetID",
        "GetTreeCurrencyInfo",
        "GetTreeInfo",
        "GetTreeNodes",
        "HasValidInspectData",
        "IsReadyForCommit",
        "PurchaseRank",
        "RefundAllRanks",
        "RefundRank",
        "RollbackConfig",
        "SetSelection"
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
        lua_setglobal(state, "C_Traits");
    }

    private static int Dispatch(lua_State state)
    {
        var traits = LuaBindings.GetRuntime(state).Traits;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CanEditConfig":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local canEdit, errorMessage = C_Traits.CanEditConfig(configID)");
                var editability = traits.ConfigEditability.TryGetValue(
                    configId,
                    out var configured)
                    ? configured
                    : new WowTraitEditabilityState(false);
                lua_pushboolean(state, editability.CanEdit ? 1 : 0);
                PushOptionalString(state, editability.ErrorMessage);
                return 2;
            }
            case "IsReadyForCommit":
                lua_pushboolean(state, traits.IsReadyForCommit ? 1 : 0);
                return 1;
            case "HasValidInspectData":
                lua_pushboolean(state, traits.HasValidInspectData ? 1 : 0);
                return 1;
            case "GetTraitSystemFlags":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local flags = C_Traits.GetTraitSystemFlags(configID)");
                return PushMappedInteger(
                    state,
                    traits.TraitSystemFlagsByConfigId,
                    configId);
            }
            case "GetTraitSystemWidgetSetID":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local uiWidgetSetID = C_Traits.GetTraitSystemWidgetSetID(configID)");
                return PushMappedInteger(
                    state,
                    traits.TraitSystemWidgetSetIdsByConfigId,
                    configId);
            }
            case "GetConfigIDBySystemID":
            {
                var systemId = RequiredInt32(
                    state,
                    1,
                    "Usage: local configID = C_Traits.GetConfigIDBySystemID(systemID)");
                return PushMappedInteger(
                    state,
                    traits.ConfigIdsBySystemId,
                    systemId);
            }
            case "GetConfigIDByTreeID":
            {
                var treeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local configID = C_Traits.GetConfigIDByTreeID(treeID)");
                return PushMappedInteger(
                    state,
                    traits.ConfigIdsByTreeId,
                    treeId);
            }
            case "GetTreeNodes":
            {
                var treeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local nodeIDs = C_Traits.GetTreeNodes(treeID)");
                lua_newtable(state);
                if (traits.TreeNodesByTreeId.TryGetValue(treeId, out var nodeIds))
                {
                    for (var index = 0; index < nodeIds.Count; index++)
                    {
                        lua_pushinteger(state, nodeIds[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetConditionInfo":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local conditionInfo = C_Traits.GetConditionInfo(configID, conditionID)");
                var conditionId = RequiredInt32(
                    state,
                    2,
                    "Usage: local conditionInfo = C_Traits.GetConditionInfo(configID, conditionID)");
                if (!traits.ConditionInfo.TryGetValue(
                        (configId, conditionId),
                        out var info))
                {
                    return 0;
                }

                PushConditionInfo(state, info);
                return 1;
            }
            case "GetConfigInfo":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local configInfo = C_Traits.GetConfigInfo(configID)");
                if (!traits.ConfigInfo.TryGetValue(configId, out var info))
                {
                    return 0;
                }

                PushConfigInfo(state, info);
                return 1;
            }
            case "GetDefinitionInfo":
            {
                var definitionId = RequiredInt32(
                    state,
                    1,
                    "Usage: local definitionInfo = C_Traits.GetDefinitionInfo(definitionID)");
                if (!traits.DefinitionInfo.TryGetValue(definitionId, out var info))
                {
                    return 0;
                }

                PushDefinitionInfo(state, info);
                return 1;
            }
            case "GetEntryInfo":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local entryInfo = C_Traits.GetEntryInfo(configID, entryID)");
                var entryId = RequiredInt32(
                    state,
                    2,
                    "Usage: local entryInfo = C_Traits.GetEntryInfo(configID, entryID)");
                if (!traits.EntryInfo.TryGetValue((configId, entryId), out var info))
                {
                    return 0;
                }

                PushEntryInfo(state, info);
                return 1;
            }
            case "GetIncreasedTraitData":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local increasedTraitData = C_Traits.GetIncreasedTraitData(nodeID, entryID)");
                var entryId = RequiredInt32(
                    state,
                    2,
                    "Usage: local increasedTraitData = C_Traits.GetIncreasedTraitData(nodeID, entryID)");
                lua_newtable(state);
                if (traits.IncreasedTraitData.TryGetValue(
                        (nodeId, entryId),
                        out var entries))
                {
                    for (var index = 0; index < entries.Count; index++)
                    {
                        PushIncreasedTraitData(state, entries[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetNodeCost":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local costs = C_Traits.GetNodeCost(configID, nodeID)");
                var nodeId = RequiredInt32(
                    state,
                    2,
                    "Usage: local costs = C_Traits.GetNodeCost(configID, nodeID)");
                lua_newtable(state);
                if (traits.NodeCosts.TryGetValue((configId, nodeId), out var costs))
                {
                    for (var index = 0; index < costs.Count; index++)
                    {
                        lua_newtable(state);
                        SetInteger(state, "ID", costs[index].Id);
                        SetInteger(state, "amount", costs[index].Amount);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetNodeInfo":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local nodeInfo = C_Traits.GetNodeInfo(configID, nodeID)");
                var nodeId = RequiredInt32(
                    state,
                    2,
                    "Usage: local nodeInfo = C_Traits.GetNodeInfo(configID, nodeID)");
                if (!traits.NodeInfo.TryGetValue((configId, nodeId), out var info))
                {
                    return 0;
                }

                PushNodeInfo(state, info);
                return 1;
            }
            case "GetSubTreeInfo":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local subTreeInfo = C_Traits.GetSubTreeInfo(configID, subTreeID)");
                var subTreeId = RequiredInt32(
                    state,
                    2,
                    "Usage: local subTreeInfo = C_Traits.GetSubTreeInfo(configID, subTreeID)");
                if (!traits.SubTreeInfo.TryGetValue((configId, subTreeId), out var info))
                {
                    return 0;
                }

                PushSubTreeInfo(state, info);
                return 1;
            }
            case "GetTraitCurrencyInfo":
            {
                var traitCurrencyId = RequiredInt32(
                    state,
                    1,
                    "Usage: local flags, type, currencyTypesID, icon = C_Traits.GetTraitCurrencyInfo(traitCurrencyID)");
                if (!traits.TraitCurrencyInfo.TryGetValue(
                        traitCurrencyId,
                        out var info))
                {
                    lua_pushinteger(state, 0);
                    lua_pushinteger(state, 0);
                    lua_pushnil(state);
                    lua_pushnil(state);
                    return 4;
                }

                lua_pushinteger(state, info.Flags);
                lua_pushinteger(state, info.Type);
                PushOptionalInteger(state, info.CurrencyTypesId);
                PushOptionalInteger(state, info.Icon);
                return 4;
            }
            case "GetTreeCurrencyInfo":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local currencyInfo = C_Traits.GetTreeCurrencyInfo(configID, treeID, excludeStagedChanges)");
                var treeId = RequiredInt32(
                    state,
                    2,
                    "Usage: local currencyInfo = C_Traits.GetTreeCurrencyInfo(configID, treeID, excludeStagedChanges)");
                var excludeStagedChanges = RequiredBoolean(
                    state,
                    3,
                    "Usage: local currencyInfo = C_Traits.GetTreeCurrencyInfo(configID, treeID, excludeStagedChanges)");
                lua_newtable(state);
                if (traits.TreeCurrencyInfo.TryGetValue(
                        (configId, treeId, excludeStagedChanges),
                        out var currencies))
                {
                    for (var index = 0; index < currencies.Count; index++)
                    {
                        var currency = currencies[index];
                        lua_newtable(state);
                        SetInteger(
                            state,
                            "traitCurrencyID",
                            currency.TraitCurrencyId);
                        SetInteger(state, "quantity", currency.Quantity);
                        SetOptionalInteger(
                            state,
                            "maxQuantity",
                            currency.MaxQuantity);
                        SetInteger(state, "spent", currency.Spent);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetTreeInfo":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local treeInfo = C_Traits.GetTreeInfo(configID, treeID)");
                var treeId = RequiredInt32(
                    state,
                    2,
                    "Usage: local treeInfo = C_Traits.GetTreeInfo(configID, treeID)");
                if (!traits.TreeInfo.TryGetValue((configId, treeId), out var info))
                {
                    return 0;
                }

                PushTreeInfo(state, info);
                return 1;
            }
            case "CascadeRepurchaseRanks":
            {
                const string usage =
                    "Usage: local success = C_Traits.CascadeRepurchaseRanks(configID, nodeID [, entryID])";
                var configId = RequiredInt32(state, 1, usage);
                var nodeId = RequiredInt32(state, 2, usage);
                var entryId = OptionalInt32(state, 3, usage);
                traits.CascadeRepurchaseRequests.Add(
                    new WowTraitCascadeRepurchaseRequest(
                        configId,
                        nodeId,
                        entryId));
                var success = traits.CascadeRepurchaseResults.TryGetValue(
                    (configId, nodeId, entryId),
                    out var configured) && configured;
                if (success)
                {
                    traits.IsReadyForCommit = true;
                }
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "ClearCascadeRepurchaseHistory":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_Traits.ClearCascadeRepurchaseHistory(configID)");
                traits.ClearedCascadeRepurchaseHistoryConfigIds.Add(configId);
                return 0;
            }
            case "CommitConfig":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local success = C_Traits.CommitConfig(configID)");
                traits.CommitConfigRequests.Add(configId);
                var success = traits.CommitConfigResults.TryGetValue(
                    configId,
                    out var configured) && configured;
                if (success)
                {
                    traits.IsReadyForCommit = false;
                }
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "PurchaseRank":
            {
                const string usage =
                    "Usage: local success = C_Traits.PurchaseRank(configID, nodeID)";
                var configId = RequiredInt32(state, 1, usage);
                var nodeId = RequiredInt32(state, 2, usage);
                traits.PurchaseRankRequests.Add(
                    new WowTraitNodeRequest(configId, nodeId));
                var success = traits.PurchaseRankResults.TryGetValue(
                    (configId, nodeId),
                    out var configured) && configured;
                if (success)
                {
                    traits.IsReadyForCommit = true;
                }
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "RefundAllRanks":
            {
                const string usage =
                    "Usage: local success = C_Traits.RefundAllRanks(configID, nodeID)";
                var configId = RequiredInt32(state, 1, usage);
                var nodeId = RequiredInt32(state, 2, usage);
                traits.RefundAllRanksRequests.Add(
                    new WowTraitNodeRequest(configId, nodeId));
                var success = traits.RefundAllRanksResults.TryGetValue(
                    (configId, nodeId),
                    out var configured) && configured;
                if (success)
                {
                    traits.IsReadyForCommit = true;
                }
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "RefundRank":
            {
                const string usage =
                    "Usage: local success = C_Traits.RefundRank(configID, nodeID [, clearEdges])";
                var configId = RequiredInt32(state, 1, usage);
                var nodeId = RequiredInt32(state, 2, usage);
                var clearEdges = OptionalBoolean(state, 3, usage);
                traits.RefundRankRequests.Add(
                    new WowTraitRefundRankRequest(
                        configId,
                        nodeId,
                        clearEdges));
                var success = traits.RefundRankResults.TryGetValue(
                    (configId, nodeId, clearEdges),
                    out var configured) && configured;
                if (success)
                {
                    traits.IsReadyForCommit = true;
                }
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "RollbackConfig":
            {
                var configId = RequiredInt32(
                    state,
                    1,
                    "Usage: local success = C_Traits.RollbackConfig(configID)");
                traits.RollbackConfigRequests.Add(configId);
                var success = traits.RollbackConfigResults.TryGetValue(
                    configId,
                    out var configured) && configured;
                if (success)
                {
                    traits.IsReadyForCommit = false;
                }
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "SetSelection":
            {
                const string usage =
                    "Usage: local success = C_Traits.SetSelection(configID, nodeID [, nodeEntryID, clearEdges])";
                var configId = RequiredInt32(state, 1, usage);
                var nodeId = RequiredInt32(state, 2, usage);
                var nodeEntryId = OptionalInt32(state, 3, usage);
                var clearEdges = OptionalBoolean(state, 4, usage);
                traits.SelectionRequests.Add(
                    new WowTraitSelectionRequest(
                        configId,
                        nodeId,
                        nodeEntryId,
                        clearEdges));
                var success = traits.SetSelectionResults.TryGetValue(
                    (configId, nodeId, nodeEntryId, clearEdges),
                    out var configured) && configured;
                if (success)
                {
                    traits.IsReadyForCommit = true;
                }
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            default:
                lua_pushnil(state);
                return 1;
        }
    }

    private static int PushMappedInteger(
        lua_State state,
        IDictionary<int, int> values,
        int key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return 0;
        }

        lua_pushinteger(state, value);
        return 1;
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            return luaL_error(state, usage);
        }

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }

        return (int)value;
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
        }

        return lua_toboolean(state, index) != 0;
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }

        return RequiredInt32(state, index, usage);
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return false;
        }

        return RequiredBoolean(state, index, usage);
    }

    private static void PushConditionInfo(
        lua_State state,
        WowTraitConditionInfoState info)
    {
        lua_newtable(state);
        SetInteger(state, "condID", info.ConditionId);
        SetOptionalInteger(state, "ranksGranted", info.RanksGranted);
        SetBoolean(state, "isAlwaysMet", info.IsAlwaysMet);
        SetBoolean(state, "isMet", info.IsMet);
        SetBoolean(state, "isGate", info.IsGate);
        SetBoolean(state, "isSufficient", info.IsSufficient);
        SetInteger(state, "type", info.Type);
        SetOptionalInteger(state, "questID", info.QuestId);
        SetOptionalInteger(state, "achievementID", info.AchievementId);
        SetOptionalInteger(state, "specSetID", info.SpecSetId);
        SetOptionalInteger(state, "playerLevel", info.PlayerLevel);
        SetOptionalInteger(state, "traitCurrencyID", info.TraitCurrencyId);
        SetOptionalInteger(
            state,
            "spentAmountRequired",
            info.SpentAmountRequired);
        SetOptionalString(state, "tooltipFormat", info.TooltipFormat);
        SetOptionalInteger(
            state,
            "traitCondAccountElementID",
            info.TraitConditionAccountElementId);
    }

    private static void PushConfigInfo(
        lua_State state,
        WowTraitConfigInfoState info)
    {
        lua_newtable(state);
        SetInteger(state, "ID", info.Id);
        SetNumber(state, "type", info.Type);
        SetString(state, "name", info.Name);
        PushIntegerArray(state, info.TreeIds);
        lua_setfield(state, -2, "treeIDs");
        SetBoolean(state, "usesSharedActionBars", info.UsesSharedActionBars);
    }

    private static void PushDefinitionInfo(
        lua_State state,
        WowTraitDefinitionInfoState info)
    {
        lua_newtable(state);
        SetOptionalInteger(state, "spellID", info.SpellId);
        SetOptionalString(state, "overrideName", info.OverrideName);
        SetOptionalString(state, "overrideSubtext", info.OverrideSubtext);
        SetOptionalString(
            state,
            "overrideDescription",
            info.OverrideDescription);
        SetOptionalInteger(state, "overrideIcon", info.OverrideIcon);
        SetOptionalInteger(state, "overriddenSpellID", info.OverriddenSpellId);
        SetOptionalInteger(state, "subType", info.SubType);
    }

    private static void PushEntryInfo(
        lua_State state,
        WowTraitEntryInfoState info)
    {
        lua_newtable(state);
        SetOptionalInteger(state, "definitionID", info.DefinitionId);
        SetOptionalInteger(state, "subTreeID", info.SubTreeId);
        SetInteger(state, "type", info.Type);
        SetInteger(state, "maxRanks", info.MaxRanks);
        SetBoolean(state, "isAvailable", info.IsAvailable);
        SetBoolean(state, "isDisplayError", info.IsDisplayError);
        PushIntegerArray(state, info.ConditionIds);
        lua_setfield(state, -2, "conditionIDs");
    }

    private static void PushIncreasedTraitData(
        lua_State state,
        WowIncreasedTraitDataState info)
    {
        lua_newtable(state);
        SetString(state, "itemNameIncreasing", info.ItemNameIncreasing);
        SetInteger(state, "itemQualityIncreasing", info.ItemQualityIncreasing);
        SetInteger(state, "numPointsIncreased", info.NumPointsIncreased);
    }

    private static void PushNodeInfo(
        lua_State state,
        WowTraitNodeInfoState info)
    {
        lua_newtable(state);
        SetInteger(state, "ID", info.Id);
        SetInteger(state, "posX", info.PosX);
        SetInteger(state, "posY", info.PosY);
        SetInteger(state, "flags", info.Flags);
        PushIntegerArray(state, info.EntryIds);
        lua_setfield(state, -2, "entryIDs");
        PushIntegerArray(state, info.EntryIdsWithCommittedRanks);
        lua_setfield(state, -2, "entryIDsWithCommittedRanks");
        SetBoolean(state, "canPurchaseRank", info.CanPurchaseRank);
        SetBoolean(state, "canRefundRank", info.CanRefundRank);
        SetBoolean(state, "isAvailable", info.IsAvailable);
        SetBoolean(state, "isVisible", info.IsVisible);
        SetBoolean(state, "isDisplayError", info.IsDisplayError);
        SetInteger(state, "ranksPurchased", info.RanksPurchased);
        SetInteger(state, "ranksIncreased", info.RanksIncreased);
        PushIntegerMap(state, info.EntryIdToRanksIncreased);
        lua_setfield(state, -2, "entryIDToRanksIncreased");
        SetInteger(state, "activeRank", info.ActiveRank);
        SetInteger(state, "currentRank", info.CurrentRank);
        PushOptionalNodeEntry(state, info.ActiveEntry);
        lua_setfield(state, -2, "activeEntry");
        PushOptionalNodeEntry(state, info.NextEntry);
        lua_setfield(state, -2, "nextEntry");
        SetInteger(state, "maxRanks", info.MaxRanks);
        SetInteger(state, "totalMaxRanks", info.TotalMaxRanks);
        SetInteger(state, "type", info.Type);
        PushVisibleEdges(state, info.VisibleEdges);
        lua_setfield(state, -2, "visibleEdges");
        SetBoolean(
            state,
            "meetsEdgeRequirements",
            info.MeetsEdgeRequirements);
        PushIntegerArray(state, info.GroupIds);
        lua_setfield(state, -2, "groupIDs");
        PushIntegerArray(state, info.ConditionIds);
        lua_setfield(state, -2, "conditionIDs");
        SetBoolean(
            state,
            "isCascadeRepurchasable",
            info.IsCascadeRepurchasable);
        SetOptionalInteger(
            state,
            "cascadeRepurchaseEntryID",
            info.CascadeRepurchaseEntryId);
        SetOptionalInteger(state, "subTreeID", info.SubTreeId);
        SetOptionalBoolean(state, "subTreeActive", info.SubTreeActive);
    }

    private static void PushSubTreeInfo(
        lua_State state,
        WowTraitSubTreeInfoState info)
    {
        lua_newtable(state);
        SetInteger(state, "ID", info.Id);
        SetOptionalString(state, "name", info.Name);
        SetOptionalString(state, "description", info.Description);
        SetInteger(state, "iconElementID", info.IconElementId);
        SetOptionalInteger(state, "traitCurrencyID", info.TraitCurrencyId);
        SetBoolean(state, "isActive", info.IsActive);
        PushIntegerArray(state, info.SubTreeSelectionNodeIds);
        lua_setfield(state, -2, "subTreeSelectionNodeIDs");
        SetInteger(state, "posX", info.PosX);
        SetInteger(state, "posY", info.PosY);
    }

    private static void PushTreeInfo(
        lua_State state,
        WowTraitTreeInfoState info)
    {
        lua_newtable(state);
        SetInteger(state, "ID", info.Id);
        lua_newtable(state);
        for (var index = 0; index < info.Gates.Count; index++)
        {
            lua_newtable(state);
            SetInteger(
                state,
                "topLeftNodeID",
                info.Gates[index].TopLeftNodeId);
            SetInteger(state, "conditionID", info.Gates[index].ConditionId);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "gates");
        SetBoolean(
            state,
            "hideSingleRankNumbers",
            info.HideSingleRankNumbers);
        SetOptionalInteger(state, "rootNodeID", info.RootNodeId);
        SetString(state, "uiTextureKit", info.UiTextureKit);
        SetOptionalString(state, "titleText", info.TitleText);
    }

    private static void PushIntegerArray(
        lua_State state,
        IEnumerable<int> values)
    {
        lua_newtable(state);
        var index = 1;
        foreach (var value in values)
        {
            lua_pushinteger(state, value);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void PushIntegerMap(
        lua_State state,
        IReadOnlyDictionary<int, int> values)
    {
        lua_newtable(state);
        foreach (var (key, value) in values)
        {
            lua_pushinteger(state, key);
            lua_pushinteger(state, value);
            lua_settable(state, -3);
        }
    }

    private static void PushOptionalNodeEntry(
        lua_State state,
        WowTraitNodeEntryState? entry)
    {
        if (entry is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_newtable(state);
        SetInteger(state, "entryID", entry.EntryId);
        SetInteger(state, "rank", entry.Rank);
    }

    private static void PushVisibleEdges(
        lua_State state,
        IReadOnlyList<WowTraitVisibleEdgeState> edges)
    {
        lua_newtable(state);
        for (var index = 0; index < edges.Count; index++)
        {
            var edge = edges[index];
            lua_newtable(state);
            SetInteger(state, "targetNode", edge.TargetNode);
            SetInteger(state, "type", edge.Type);
            SetInteger(state, "visualStyle", edge.VisualStyle);
            SetBoolean(state, "isActive", edge.IsActive);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetInteger(lua_State state, string name, long value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string name,
        int? value)
    {
        PushOptionalInteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } integer)
        {
            lua_pushinteger(state, integer);
        }
        else
        {
            lua_pushnil(state);
        }
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string name,
        bool? value)
    {
        if (value is { } boolean)
        {
            lua_pushboolean(state, boolean ? 1 : 0);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(
        lua_State state,
        string name,
        string? value)
    {
        PushOptionalString(state, value);
        lua_setfield(state, -2, name);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            lua_pushstring(state, value);
        }
    }
}
