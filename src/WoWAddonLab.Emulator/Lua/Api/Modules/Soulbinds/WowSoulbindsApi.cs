using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSoulbindsApi : LuaApiModule
{
    private const int PlayerInteractionType = 50;
    private const string Namespace = "C_Soulbinds";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ActivateSoulbind",
        "CanActivateSoulbind",
        "CanModifySoulbind",
        "CanResetConduitsInSoulbind",
        "CanSwitchActiveSoulbindTreeBranch",
        "CloseUI",
        "CommitPendingConduitsInSoulbind",
        "FindNodeIDActuallyInstalled",
        "FindNodeIDAppearingInstalled",
        "FindNodeIDPendingInstall",
        "FindNodeIDPendingUninstall",
        "GetActiveSoulbindID",
        "GetConduitCollection",
        "GetConduitCollectionCount",
        "GetConduitCollectionData",
        "GetConduitCollectionDataAtCursor",
        "GetConduitCollectionDataByVirtualID",
        "GetConduitDisplayed",
        "GetConduitHyperlink",
        "GetConduitIDPendingInstall",
        "GetConduitQuality",
        "GetConduitRank",
        "GetConduitSpellID",
        "GetInstalledConduitID",
        "GetNode",
        "GetSoulbindData",
        "GetSpecsAssignedToSoulbind",
        "GetTree",
        "HasAnyInstalledConduitInSoulbind",
        "HasAnyPendingConduits",
        "HasPendingConduitsInSoulbind",
        "IsConduitInstalled",
        "IsConduitInstalledInSoulbind",
        "IsItemConduitByItemInfo",
        "IsNodePendingModify",
        "IsUnselectedConduitPendingInSoulbind",
        "ModifyNode",
        "SelectNode",
        "UnmodifyNode"
    ];

    public override void Register(lua_State state)
    {
        lua_createtable(state, 0, Functions.Length);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, Namespace);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var soulbinds = runtime.Soulbinds;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "ActivateSoulbind":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_Soulbinds.ActivateSoulbind(soulbindID)");
                soulbinds.ActivationRequests.Add(soulbindId);
                return 0;
            }
            case "CanActivateSoulbind":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result, errorDescription = " +
                    "C_Soulbinds.CanActivateSoulbind(soulbindID)");
                soulbinds.ActivationResults.TryGetValue(
                    soulbindId,
                    out var result);
                return PushResultAndError(state, result);
            }
            case "CanModifySoulbind":
                return PushBoolean(state, soulbinds.CanModifySoulbind);
            case "CanResetConduitsInSoulbind":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result, errorDescription = " +
                    "C_Soulbinds.CanResetConduitsInSoulbind(soulbindID)");
                soulbinds.ResetResults.TryGetValue(
                    soulbindId,
                    out var result);
                return PushResultAndError(state, result);
            }
            case "CanSwitchActiveSoulbindTreeBranch":
                return PushBoolean(
                    state,
                    soulbinds.CanSwitchActiveSoulbindTreeBranch);
            case "CloseUI":
                ClearInteraction(runtime.PlayerInteractions);
                soulbinds.ViewedSoulbindId = 0;
                soulbinds.PendingModifications.Clear();
                return 0;
            case "CommitPendingConduitsInSoulbind":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_Soulbinds." +
                    "CommitPendingConduitsInSoulbind(soulbindID)");
                soulbinds.CommitRequests.Add(soulbindId);
                return 0;
            }
            case "FindNodeIDActuallyInstalled":
            {
                const string usage =
                    "Usage: local nodeID = C_Soulbinds." +
                    "FindNodeIDActuallyInstalled(soulbindID, conduitID)";
                var soulbindId = RequiredInt32(state, 1, usage);
                var conduitId = RequiredInt32(state, 2, usage);
                return PushNumber(
                    state,
                    FindActuallyInstalled(
                        soulbinds,
                        soulbindId,
                        conduitId));
            }
            case "FindNodeIDAppearingInstalled":
            {
                const string usage =
                    "Usage: local nodeID = C_Soulbinds." +
                    "FindNodeIDAppearingInstalled(soulbindID, conduitID)";
                var soulbindId = RequiredInt32(state, 1, usage);
                var conduitId = RequiredInt32(state, 2, usage);
                var pendingInstall = FindPending(
                    soulbinds,
                    soulbindId,
                    conduitId,
                    0);
                var result = pendingInstall != 0
                    ? pendingInstall
                    : FindPending(soulbinds, soulbindId, conduitId, 1) != 0
                        ? 0
                        : FindActuallyInstalled(
                            soulbinds,
                            soulbindId,
                            conduitId);
                return PushNumber(state, result);
            }
            case "FindNodeIDPendingInstall":
            case "FindNodeIDPendingUninstall":
            {
                var usage =
                    "Usage: local nodeID = C_Soulbinds." +
                    operation + "(soulbindID, conduitID)";
                var soulbindId = RequiredInt32(state, 1, usage);
                var conduitId = RequiredInt32(state, 2, usage);
                return PushNumber(
                    state,
                    FindPending(
                        soulbinds,
                        soulbindId,
                        conduitId,
                        operation == "FindNodeIDPendingInstall" ? 0 : 1));
            }
            case "GetActiveSoulbindID":
                return PushNumber(state, soulbinds.ActiveSoulbindId);
            case "GetConduitCollection":
            {
                const string usage =
                    "Usage: local collectionData = C_Soulbinds." +
                    "GetConduitCollection(conduitType)";
                var conduitType = RequiredEnum(state, 1, 3, usage);
                PushConduitCollection(
                    state,
                    GetCollection(soulbinds, conduitType));
                return 1;
            }
            case "GetConduitCollectionCount":
                return PushNumber(
                    state,
                    soulbinds.RelevantConduitCount ??
                    GetAllConduitData(soulbinds)
                        .Select(data => data.ConduitId)
                        .Distinct()
                        .Count());
            case "GetConduitCollectionData":
            {
                const string usage =
                    "Usage: local collectionData = C_Soulbinds." +
                    "GetConduitCollectionData(conduitID)";
                var conduitId = RequiredInt32(state, 1, usage);
                PushOptionalConduitData(
                    state,
                    FindConduitData(soulbinds, conduitId));
                return 1;
            }
            case "GetConduitCollectionDataAtCursor":
                PushOptionalConduitData(
                    state,
                    soulbinds.ConduitCollectionDataAtCursor);
                return 1;
            case "GetConduitCollectionDataByVirtualID":
            {
                const string usage =
                    "Usage: local collectionData = C_Soulbinds." +
                    "GetConduitCollectionDataByVirtualID(virtualID)";
                var virtualId = RequiredInt32(state, 1, usage);
                PushOptionalConduitData(
                    state,
                    soulbinds.ConduitIdsByVirtualId.TryGetValue(
                        virtualId,
                        out var conduitId)
                        ? FindConduitData(soulbinds, conduitId)
                        : null);
                return 1;
            }
            case "GetConduitDisplayed":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local conduitID = " +
                    "C_Soulbinds.GetConduitDisplayed(nodeID)");
                return PushNumber(
                    state,
                    GetDisplayedConduit(soulbinds, nodeId));
            }
            case "GetConduitHyperlink":
            {
                const string usage =
                    "Usage: local link = C_Soulbinds." +
                    "GetConduitHyperlink(conduitID, rank)";
                var conduitId = RequiredInt32(state, 1, usage);
                var rank = RequiredInt32(state, 2, usage);
                soulbinds.ConduitHyperlinks.TryGetValue(
                    (conduitId, rank),
                    out var link);
                PushOptionalString(state, link);
                return 1;
            }
            case "GetConduitIDPendingInstall":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local conduitID = C_Soulbinds." +
                    "GetConduitIDPendingInstall(nodeID)");
                return PushNumber(
                    state,
                    soulbinds.PendingModifications.FirstOrDefault(
                        pending =>
                            pending.NodeId == nodeId &&
                            pending.Type == 0)?.ConduitId ?? 0);
            }
            case "GetConduitQuality":
            {
                const string usage =
                    "Usage: local quality = C_Soulbinds." +
                    "GetConduitQuality(conduitID, rank)";
                _ = RequiredInt32(state, 1, usage);
                var rank = RequiredInt32(state, 2, usage);
                return PushNumber(
                    state,
                    soulbinds.ConduitQualitiesByRank.TryGetValue(
                        rank,
                        out var quality)
                        ? quality
                        : 4);
            }
            case "GetConduitRank":
            {
                var conduitId = RequiredInt32(
                    state,
                    1,
                    "Usage: local conduitRank = " +
                    "C_Soulbinds.GetConduitRank(conduitID)");
                return PushNumber(
                    state,
                    FindConduitData(soulbinds, conduitId)?.ConduitRank ??
                    0);
            }
            case "GetConduitSpellID":
            {
                const string usage =
                    "Usage: local spellID = C_Soulbinds." +
                    "GetConduitSpellID(conduitID, conduitRank)";
                var conduitId = RequiredInt32(state, 1, usage);
                var rank = RequiredInt32(state, 2, usage);
                return PushNumber(
                    state,
                    soulbinds.ConduitSpellIds.TryGetValue(
                        (conduitId, rank),
                        out var spellId)
                        ? spellId
                        : 0);
            }
            case "GetInstalledConduitID":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local conduitID = " +
                    "C_Soulbinds.GetInstalledConduitID(nodeID)");
                return PushNumber(
                    state,
                    GetInstalledConduit(soulbinds, nodeId));
            }
            case "GetNode":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local node = C_Soulbinds.GetNode(nodeID)");
                PushNode(
                    state,
                    soulbinds.Nodes.TryGetValue(nodeId, out var node)
                        ? node
                        : WowSoulbindNodeData.Empty);
                return 1;
            }
            case "GetSoulbindData":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: local data = " +
                    "C_Soulbinds.GetSoulbindData(soulbindID)");
                PushSoulbindData(
                    state,
                    soulbinds.Soulbinds.TryGetValue(
                        soulbindId,
                        out var data)
                        ? data
                        : WowSoulbindData.Empty);
                return 1;
            }
            case "GetSpecsAssignedToSoulbind":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: local specIDs = C_Soulbinds." +
                    "GetSpecsAssignedToSoulbind(soulbindID)");
                PushInt32Array(
                    state,
                    soulbinds.SpecsAssignedToSoulbind.TryGetValue(
                        soulbindId,
                        out var specs)
                        ? specs
                        : []);
                return 1;
            }
            case "GetTree":
            {
                var treeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local tree = C_Soulbinds.GetTree(treeID)");
                PushTree(
                    state,
                    soulbinds.Trees.TryGetValue(treeId, out var tree)
                        ? tree
                        : WowSoulbindTreeData.Empty);
                return 1;
            }
            case "HasAnyInstalledConduitInSoulbind":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result = C_Soulbinds." +
                    "HasAnyInstalledConduitInSoulbind(soulbindID)");
                return PushBoolean(
                    state,
                    EnumerateSoulbindNodes(soulbinds, soulbindId)
                        .Any(nodeId =>
                            GetInstalledConduit(soulbinds, nodeId) != 0));
            }
            case "HasAnyPendingConduits":
                return PushBoolean(
                    state,
                    soulbinds.PendingModifications.Count != 0);
            case "HasPendingConduitsInSoulbind":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result = C_Soulbinds." +
                    "HasPendingConduitsInSoulbind(soulbindID)");
                return PushBoolean(
                    state,
                    soulbinds.PendingModifications.Any(
                        pending => pending.SoulbindId == soulbindId));
            }
            case "IsConduitInstalled":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result = " +
                    "C_Soulbinds.IsConduitInstalled(nodeID)");
                return PushBoolean(
                    state,
                    GetInstalledConduit(soulbinds, nodeId) != 0);
            }
            case "IsConduitInstalledInSoulbind":
            {
                const string usage =
                    "Usage: local result = C_Soulbinds." +
                    "IsConduitInstalledInSoulbind(soulbindID, conduitID)";
                var soulbindId = RequiredInt32(state, 1, usage);
                var conduitId = RequiredInt32(state, 2, usage);
                return PushBoolean(
                    state,
                    FindActuallyInstalled(
                        soulbinds,
                        soulbindId,
                        conduitId) != 0);
            }
            case "IsItemConduitByItemInfo":
            {
                const string usage =
                    "Usage: local result = C_Soulbinds." +
                    "IsItemConduitByItemInfo(itemInfo)";
                var itemId =
                    WowItemApi.RequiredItemId(state, runtime.Items, usage);
                return PushBoolean(
                    state,
                    itemId is { } id &&
                    soulbinds.ConduitItemIds.Contains(id));
            }
            case "IsNodePendingModify":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result = " +
                    "C_Soulbinds.IsNodePendingModify(nodeID)");
                return PushBoolean(
                    state,
                    soulbinds.PendingModifications.Any(
                        pending => pending.NodeId == nodeId));
            }
            case "IsUnselectedConduitPendingInSoulbind":
            {
                var soulbindId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result = C_Soulbinds." +
                    "IsUnselectedConduitPendingInSoulbind(soulbindID)");
                return PushBoolean(
                    state,
                    soulbinds.PendingModifications.Any(
                        pending =>
                            pending.SoulbindId == soulbindId &&
                            soulbinds.UnselectedNodeIds.Contains(
                                pending.NodeId)));
            }
            case "ModifyNode":
            {
                const string usage =
                    "Usage: C_Soulbinds." +
                    "ModifyNode(nodeID, conduitID, type)";
                var nodeId = RequiredInt32(state, 1, usage);
                var conduitId = RequiredInt32(state, 2, usage);
                var type = RequiredEnum(state, 3, 1, usage);
                ModifyNode(
                    runtime,
                    soulbinds,
                    nodeId,
                    conduitId,
                    type);
                return 0;
            }
            case "SelectNode":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_Soulbinds.SelectNode(nodeID)");
                soulbinds.SelectNodeRequests.Add(nodeId);
                return 0;
            }
            case "UnmodifyNode":
            {
                var nodeId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_Soulbinds.UnmodifyNode(nodeID)");
                UnmodifyNode(runtime, soulbinds, nodeId);
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int PushResultAndError(
        lua_State state,
        WowSoulbindOperationResult? result)
    {
        lua_pushboolean(state, result?.Allowed == true ? 1 : 0);
        PushOptionalString(state, result?.ErrorDescription);
        return 2;
    }

    private static IReadOnlyList<WowSoulbindConduitData> GetCollection(
        WowSoulbindsState state,
        int conduitType)
    {
        if (state.ConduitCollections.TryGetValue(
                conduitType,
                out var collection))
        {
            return collection;
        }

        return state.Conduits.Values
            .Where(data => data.ConduitType == conduitType)
            .ToArray();
    }

    private static IEnumerable<WowSoulbindConduitData> GetAllConduitData(
        WowSoulbindsState state) =>
        state.Conduits.Values.Concat(
            state.ConduitCollections.Values.SelectMany(
                collection => collection));

    private static WowSoulbindConduitData? FindConduitData(
        WowSoulbindsState state,
        int conduitId)
    {
        if (state.Conduits.TryGetValue(conduitId, out var data))
            return data.ConduitRank > 0 ? data : null;

        data = state.ConduitCollections.Values
            .SelectMany(collection => collection)
            .FirstOrDefault(candidate =>
                candidate.ConduitId == conduitId);
        return data?.ConduitRank > 0 ? data : null;
    }

    private static int FindActuallyInstalled(
        WowSoulbindsState state,
        int soulbindId,
        int conduitId) =>
        EnumerateSoulbindNodes(state, soulbindId)
            .FirstOrDefault(nodeId =>
                GetInstalledConduit(state, nodeId) == conduitId);

    private static int FindPending(
        WowSoulbindsState state,
        int soulbindId,
        int conduitId,
        int type) =>
        state.PendingModifications.FirstOrDefault(
            pending =>
                pending.SoulbindId == soulbindId &&
                pending.ConduitId == conduitId &&
                pending.Type == type)?.NodeId ?? 0;

    private static IEnumerable<int> EnumerateSoulbindNodes(
        WowSoulbindsState state,
        int soulbindId)
    {
        var known = new HashSet<int>();
        foreach (var pair in state.NodeSoulbindIds)
        {
            if (pair.Value == soulbindId && known.Add(pair.Key))
                yield return pair.Key;
        }

        foreach (var pending in state.PendingModifications)
        {
            if (pending.SoulbindId == soulbindId &&
                known.Add(pending.NodeId))
            {
                yield return pending.NodeId;
            }
        }
    }

    private static int GetInstalledConduit(
        WowSoulbindsState state,
        int nodeId)
    {
        if (state.InstalledConduitsByNode.TryGetValue(
                nodeId,
                out var conduitId))
        {
            return conduitId;
        }

        return state.Nodes.TryGetValue(nodeId, out var node)
            ? node.ConduitId
            : 0;
    }

    private static int GetDisplayedConduit(
        WowSoulbindsState state,
        int nodeId)
    {
        var hasPendingUninstall = false;
        foreach (var pending in state.PendingModifications)
        {
            if (pending.NodeId != nodeId)
                continue;
            if (pending.Type == 0)
                return pending.ConduitId;
            if (pending.Type == 1)
                hasPendingUninstall = true;
        }

        return hasPendingUninstall
            ? 0
            : GetInstalledConduit(state, nodeId);
    }

    private static void ModifyNode(
        LuaRuntime runtime,
        WowSoulbindsState state,
        int nodeId,
        int conduitId,
        int type)
    {
        var soulbindId = state.NodeSoulbindIds.TryGetValue(
            nodeId,
            out var knownSoulbindId)
            ? knownSoulbindId
            : 0;

        if (type == 0)
        {
            var previouslyInstalledNode = FindActuallyInstalled(
                state,
                soulbindId,
                conduitId);
            if (previouslyInstalledNode != 0 &&
                previouslyInstalledNode != nodeId)
            {
                AddPending(
                    runtime,
                    state,
                    new WowSoulbindPendingModification(
                        previouslyInstalledNode,
                        conduitId,
                        1,
                        soulbindId,
                        true));
            }

            var installedHere = GetInstalledConduit(state, nodeId);
            if (installedHere != 0 && installedHere != conduitId)
            {
                AddPending(
                    runtime,
                    state,
                    new WowSoulbindPendingModification(
                        nodeId,
                        installedHere,
                        1,
                        soulbindId,
                        true));
            }
        }

        AddPending(
            runtime,
            state,
            new WowSoulbindPendingModification(
                nodeId,
                conduitId,
                type,
                soulbindId,
                false));
        state.ModifyNodeRequests.Add(
            new WowSoulbindModifyNodeRequest(nodeId, conduitId, type));
    }

    private static void AddPending(
        LuaRuntime runtime,
        WowSoulbindsState state,
        WowSoulbindPendingModification pending)
    {
        for (var index = state.PendingModifications.Count - 1;
             index >= 0;
             index--)
        {
            var existing = state.PendingModifications[index];
            if (existing.NodeId == pending.NodeId &&
                existing.Type == pending.Type)
            {
                state.PendingModifications.RemoveAt(index);
            }
        }

        state.PendingModifications.Add(pending);
        runtime.TriggerEvent(
            "SOULBIND_PENDING_CONDUIT_CHANGED",
            pending.NodeId,
            pending.ConduitId);
    }

    private static void UnmodifyNode(
        LuaRuntime runtime,
        WowSoulbindsState state,
        int nodeId)
    {
        var removed = state.PendingModifications
            .Where(pending => pending.NodeId == nodeId)
            .ToArray();
        foreach (var pending in removed)
        {
            state.PendingModifications.Remove(pending);
            runtime.TriggerEvent(
                "SOULBIND_PENDING_CONDUIT_CHANGED",
                pending.NodeId,
                pending.ConduitId);
        }
        state.UnmodifyNodeRequests.Add(nodeId);
    }

    private static void PushConduitCollection(
        lua_State state,
        IReadOnlyList<WowSoulbindConduitData> collection)
    {
        lua_createtable(state, collection.Count, 0);
        for (var index = 0; index < collection.Count; index++)
        {
            PushConduitData(state, collection[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOptionalConduitData(
        lua_State state,
        WowSoulbindConduitData? data)
    {
        if (data is null || data.ConduitRank <= 0)
        {
            lua_pushnil(state);
            return;
        }
        PushConduitData(state, data);
    }

    private static void PushConduitData(
        lua_State state,
        WowSoulbindConduitData data)
    {
        lua_createtable(state, 0, 9);
        SetNumber(state, "conduitID", data.ConduitId);
        SetNumber(state, "conduitRank", data.ConduitRank);
        SetNumber(state, "conduitItemLevel", data.ConduitItemLevel);
        SetNumber(state, "conduitType", data.ConduitType);
        SetNumber(state, "conduitSpecSetID", data.ConduitSpecSetId);
        PushInt32Array(state, data.ConduitSpecIds);
        lua_setfield(state, -2, "conduitSpecIDs");
        SetOptionalString(
            state,
            "conduitSpecName",
            data.ConduitSpecName);
        SetOptionalNumber(state, "covenantID", data.CovenantId);
        SetNumber(state, "conduitItemID", data.ConduitItemId);
    }

    private static void PushNode(
        lua_State state,
        WowSoulbindNodeData node)
    {
        lua_createtable(state, 0, 13);
        SetNumber(state, "ID", node.Id);
        SetNumber(state, "row", node.Row);
        SetNumber(state, "column", node.Column);
        SetNumber(state, "icon", node.Icon);
        SetNumber(state, "spellID", node.SpellId);
        SetOptionalString(
            state,
            "playerConditionReason",
            node.PlayerConditionReason);
        SetNumber(state, "conduitID", node.ConduitId);
        SetNumber(state, "conduitRank", node.ConduitRank);
        SetNumber(state, "state", node.State);
        SetOptionalNumber(state, "conduitType", node.ConduitType);
        PushInt32Array(state, node.ParentNodeIds);
        lua_setfield(state, -2, "parentNodeIDs");
        SetOptionalNumber(
            state,
            "failureRenownRequirement",
            node.FailureRenownRequirement);
        SetOptionalBoolean(
            state,
            "socketEnhanced",
            node.SocketEnhanced);
    }

    private static void PushTree(
        lua_State state,
        WowSoulbindTreeData tree)
    {
        lua_createtable(state, 0, 2);
        SetBoolean(state, "editable", tree.Editable);
        lua_createtable(state, tree.Nodes.Count, 0);
        for (var index = 0; index < tree.Nodes.Count; index++)
        {
            PushNode(state, tree.Nodes[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "nodes");
    }

    private static void PushSoulbindData(
        lua_State state,
        WowSoulbindData data)
    {
        lua_createtable(state, 0, 11);
        SetNumber(state, "ID", data.Id);
        SetNumber(state, "covenantID", data.CovenantId);
        SetOptionalString(state, "name", data.Name);
        SetOptionalString(state, "description", data.Description);
        SetOptionalString(state, "textureKit", data.TextureKit);
        SetBoolean(state, "unlocked", data.Unlocked);
        SetNumber(state, "cvarIndex", data.CvarIndex);
        PushTree(state, data.Tree);
        lua_setfield(state, -2, "tree");
        PushModelSceneData(state, data.ModelSceneData);
        lua_setfield(state, -2, "modelSceneData");
        SetNumber(
            state,
            "activationSoundKitID",
            data.ActivationSoundKitId);
        SetOptionalString(
            state,
            "playerConditionReason",
            data.PlayerConditionReason);
    }

    private static void PushModelSceneData(
        lua_State state,
        WowSoulbindModelSceneData data)
    {
        lua_createtable(state, 0, 2);
        SetNumber(
            state,
            "creatureDisplayInfoID",
            data.CreatureDisplayInfoId);
        SetNumber(
            state,
            "modelSceneActorID",
            data.ModelSceneActorId);
    }

    private static void PushInt32Array(
        lua_State state,
        IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushnumber(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index ||
            lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static int RequiredEnum(
        lua_State state,
        int index,
        int maximum,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < 0 || value > maximum)
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushNumber(lua_State state, int value)
    {
        lua_pushnumber(state, value);
        return 1;
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

    private static void SetNumber(
        lua_State state,
        string field,
        int value)
    {
        lua_pushnumber(state, value);
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

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        PushOptionalString(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string field,
        int? value)
    {
        if (value is { } number)
            lua_pushnumber(state, number);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string field,
        bool? value)
    {
        if (value is { } boolean)
            lua_pushboolean(state, boolean ? 1 : 0);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void ClearInteraction(
        WowPlayerInteractionManagerState interactions)
    {
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = PlayerInteractionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != PlayerInteractionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Remove(PlayerInteractionType);
    }
}
