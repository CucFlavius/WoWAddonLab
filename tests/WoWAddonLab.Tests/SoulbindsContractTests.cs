using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class SoulbindsContractTests
{
    [Fact]
    public void UsesNativeDefaultArityTablesAndArgumentContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "39:2:false:nil:2:false:nil:1:nil:1:nil:" +
            "0:0::0:0:0:0:4:false:false:false",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_Soulbinds) do " +
                "count=count+1 end;" +
                "local activateCount=select('#'," +
                "C_Soulbinds.CanActivateSoulbind(1));" +
                "local activate,activateError=" +
                "C_Soulbinds.CanActivateSoulbind(1);" +
                "local resetCount=select('#'," +
                "C_Soulbinds.CanResetConduitsInSoulbind(1));" +
                "local reset,resetError=" +
                "C_Soulbinds.CanResetConduitsInSoulbind(1);" +
                "local linkCount=select('#'," +
                "C_Soulbinds.GetConduitHyperlink(1,1));" +
                "local link=C_Soulbinds.GetConduitHyperlink(1,1);" +
                "local dataCount=select('#'," +
                "C_Soulbinds.GetConduitCollectionData(1));" +
                "local data=C_Soulbinds.GetConduitCollectionData(1);" +
                "local node=C_Soulbinds.GetNode(1);" +
                "local soulbind=C_Soulbinds.GetSoulbindData(1);" +
                "local tree=C_Soulbinds.GetTree(1);" +
                "local collection=C_Soulbinds.GetConduitCollection(0);" +
                "local okCollection=pcall(" +
                "C_Soulbinds.GetConduitCollection,4);" +
                "local okModify=pcall(" +
                "C_Soulbinds.ModifyNode,1,2,2);" +
                "local okRequired=pcall(" +
                "C_Soulbinds.GetConduitRank,nil);" +
                "return table.concat({" +
                "count,activateCount,tostring(activate)," +
                "tostring(activateError),resetCount,tostring(reset)," +
                "tostring(resetError),linkCount,tostring(link)," +
                "dataCount,tostring(data),node.ID,soulbind.ID," +
                "soulbind.name,#tree.nodes,#collection," +
                "C_Soulbinds.GetConduitCollectionCount()," +
                "C_Soulbinds.GetConduitRank(1)," +
                "C_Soulbinds.GetConduitQuality(1,99)," +
                "tostring(okCollection),tostring(okModify)," +
                "tostring(okRequired)},':')"));
    }

    [Fact]
    public void ProjectsNativeRecordsPendingStateEventsAndRequests()
    {
        using var session = new EmulatorSession();
        var soulbinds = session.Lua.Soulbinds;

        var conduit = new WowSoulbindConduitData(
            101,
            3,
            252,
            2,
            5,
            [62, 63],
            "Frost",
            3,
            1901);
        var node = new WowSoulbindNodeData(
            201,
            1,
            2,
            987,
            456,
            "Locked",
            101,
            3,
            2,
            2,
            [199, 200],
            44,
            true);
        var tree = new WowSoulbindTreeData(true, [node]);
        var soulbind = new WowSoulbindData(
            10,
            3,
            "Niya",
            "Soulbind description",
            "ardenweald",
            true,
            2,
            tree,
            new WowSoulbindModelSceneData(123, 456),
            789,
            "Requires Renown");

        soulbinds.ActiveSoulbindId = 10;
        soulbinds.ViewedSoulbindId = 10;
        soulbinds.CanModifySoulbind = true;
        soulbinds.CanSwitchActiveSoulbindTreeBranch = true;
        soulbinds.RelevantConduitCount = 9;
        soulbinds.ActivationResults[10] =
            new WowSoulbindOperationResult(true);
        soulbinds.ResetResults[10] =
            new WowSoulbindOperationResult(false, "Not here");
        soulbinds.Conduits[101] = conduit;
        soulbinds.ConduitCollections[2] = [conduit];
        soulbinds.ConduitIdsByVirtualId[777] = 101;
        soulbinds.ConduitCollectionDataAtCursor = conduit;
        soulbinds.ConduitHyperlinks[(101, 3)] =
            "|cff0070dd|Hconduit:101:3|h[Test]|h|r";
        soulbinds.ConduitQualitiesByRank[3] = 2;
        soulbinds.ConduitSpellIds[(101, 3)] = 3456;
        soulbinds.Nodes[201] = node;
        soulbinds.Trees[50] = tree;
        soulbinds.Soulbinds[10] = soulbind;
        soulbinds.SpecsAssignedToSoulbind[10] = [62, 63];
        soulbinds.InstalledConduitsByNode[201] = 101;
        soulbinds.NodeSoulbindIds[201] = 10;
        soulbinds.NodeSoulbindIds[203] = 10;
        soulbinds.UnselectedNodeIds.Add(201);
        soulbinds.ConduitItemIds.Add(1901);
        session.Lua.PlayerInteractions.HasActiveInteraction = true;
        session.Lua.PlayerInteractions.CurrentInteractionType = 50;

        Assert.Equal(
            "true:nil:false:Not here:true:true:10:9:" +
            "101:3:252:2:5:2:62:63:Frost:3:1901:" +
            "101:101:3:2:3456:" +
            "|cff0070dd|Hconduit:101:3|h[Test]|h|r:" +
            "201:1:2:987:456:Locked:101:3:2:2:2:199:200:44:true:" +
            "10:3:Niya:Soulbind description:ardenweald:true:2:" +
            "true:1:201:123:456:789:Requires Renown:" +
            "true:1:201:2:62:63:true:true:true:true:" +
            "false:true:true:true:true:203:201:203:103:0:" +
            "3:203:103,201:101,203:103:0",
            session.Lua.Evaluate(
                "pendingEvents={}; ended=0;" +
                "local listener=CreateFrame('Frame');" +
                "listener:RegisterEvent(" +
                "'SOULBIND_PENDING_CONDUIT_CHANGED');" +
                "listener:RegisterEvent(" +
                "'SOULBIND_FORGE_INTERACTION_ENDED');" +
                "listener:SetScript('OnEvent',function(_,event,a,b) " +
                "if event=='SOULBIND_PENDING_CONDUIT_CHANGED' then " +
                "pendingEvents[#pendingEvents+1]=a..':'..b " +
                "else ended=ended+1 end end);" +
                "local canActivate,activateError=" +
                "C_Soulbinds.CanActivateSoulbind(10);" +
                "local canReset,resetError=" +
                "C_Soulbinds.CanResetConduitsInSoulbind(10);" +
                "local collection=C_Soulbinds.GetConduitCollection(2);" +
                "local data=C_Soulbinds.GetConduitCollectionData(101);" +
                "local cursor=" +
                "C_Soulbinds.GetConduitCollectionDataAtCursor();" +
                "local virtual=C_Soulbinds." +
                "GetConduitCollectionDataByVirtualID(777);" +
                "local n=C_Soulbinds.GetNode(201);" +
                "local s=C_Soulbinds.GetSoulbindData(10);" +
                "local t=C_Soulbinds.GetTree(50);" +
                "local specs=C_Soulbinds.GetSpecsAssignedToSoulbind(10);" +
                "local actual=C_Soulbinds." +
                "FindNodeIDActuallyInstalled(10,101);" +
                "C_Soulbinds.ActivateSoulbind(10);" +
                "C_Soulbinds.CommitPendingConduitsInSoulbind(10);" +
                "C_Soulbinds.SelectNode(201);" +
                "C_Soulbinds.ModifyNode(203,103,0);" +
                "C_Soulbinds.ModifyNode(201,101,1);" +
                "local pendingInstall=C_Soulbinds." +
                "FindNodeIDPendingInstall(10,103);" +
                "local pendingUninstall=C_Soulbinds." +
                "FindNodeIDPendingUninstall(10,101);" +
                "local appearing=C_Soulbinds." +
                "FindNodeIDAppearingInstalled(10,103);" +
                "local displayedInstall=" +
                "C_Soulbinds.GetConduitDisplayed(203);" +
                "local displayedUninstall=" +
                "C_Soulbinds.GetConduitDisplayed(201);" +
                "local hasAny=C_Soulbinds.HasAnyPendingConduits();" +
                "local hasSoulbind=C_Soulbinds." +
                "HasPendingConduitsInSoulbind(10);" +
                "local pendingNode=C_Soulbinds.IsNodePendingModify(203);" +
                "local pendingUnselected=C_Soulbinds." +
                "IsUnselectedConduitPendingInSoulbind(10);" +
                "local pendingConduit=C_Soulbinds." +
                "GetConduitIDPendingInstall(203);" +
                "C_Soulbinds.UnmodifyNode(203);" +
                "C_Soulbinds.CloseUI();" +
                "return table.concat({" +
                "tostring(canActivate),tostring(activateError)," +
                "tostring(canReset),resetError," +
                "tostring(C_Soulbinds.CanModifySoulbind())," +
                "tostring(C_Soulbinds." +
                "CanSwitchActiveSoulbindTreeBranch())," +
                "C_Soulbinds.GetActiveSoulbindID()," +
                "C_Soulbinds.GetConduitCollectionCount()," +
                "collection[1].conduitID,data.conduitRank," +
                "data.conduitItemLevel,data.conduitType," +
                "data.conduitSpecSetID,#data.conduitSpecIDs," +
                "data.conduitSpecIDs[1],data.conduitSpecIDs[2]," +
                "data.conduitSpecName,data.covenantID," +
                "data.conduitItemID,cursor.conduitID," +
                "virtual.conduitID,C_Soulbinds.GetConduitRank(101)," +
                "C_Soulbinds.GetConduitQuality(999,3)," +
                "C_Soulbinds.GetConduitSpellID(101,3)," +
                "C_Soulbinds.GetConduitHyperlink(101,3)," +
                "n.ID,n.row,n.column,n.icon,n.spellID," +
                "n.playerConditionReason,n.conduitID,n.conduitRank," +
                "n.state,n.conduitType,#n.parentNodeIDs," +
                "n.parentNodeIDs[1],n.parentNodeIDs[2]," +
                "n.failureRenownRequirement,tostring(n.socketEnhanced)," +
                "s.ID,s.covenantID,s.name,s.description,s.textureKit," +
                "tostring(s.unlocked),s.cvarIndex," +
                "tostring(s.tree.editable),#s.tree.nodes," +
                "s.tree.nodes[1].ID," +
                "s.modelSceneData.creatureDisplayInfoID," +
                "s.modelSceneData.modelSceneActorID," +
                "s.activationSoundKitID,s.playerConditionReason," +
                "tostring(t.editable),#t.nodes,t.nodes[1].ID," +
                "#specs,specs[1],specs[2]," +
                "tostring(C_Soulbinds." +
                "HasAnyInstalledConduitInSoulbind(10))," +
                "tostring(C_Soulbinds.IsConduitInstalled(201))," +
                "tostring(C_Soulbinds." +
                "IsConduitInstalledInSoulbind(10,101))," +
                "tostring(C_Soulbinds.IsItemConduitByItemInfo(1901))," +
                "tostring(C_Soulbinds.IsItemConduitByItemInfo(2000))," +
                "tostring(hasAny),tostring(hasSoulbind)," +
                "tostring(pendingNode),tostring(pendingUnselected)," +
                "pendingInstall,pendingUninstall,appearing," +
                "displayedInstall,displayedUninstall,#pendingEvents," +
                "table.concat(pendingEvents,','),ended},':')"));

        Assert.Equal([10], soulbinds.ActivationRequests);
        Assert.Equal([10], soulbinds.CommitRequests);
        Assert.Equal([201], soulbinds.SelectNodeRequests);
        Assert.Equal([203], soulbinds.UnmodifyNodeRequests);
        Assert.Equal(
            [
                new WowSoulbindModifyNodeRequest(203, 103, 0),
                new WowSoulbindModifyNodeRequest(201, 101, 1)
            ],
            soulbinds.ModifyNodeRequests);
        Assert.Empty(soulbinds.PendingModifications);
        Assert.Equal(0, soulbinds.ViewedSoulbindId);
        Assert.False(
            session.Lua.PlayerInteractions.HasActiveInteraction);
        Assert.Equal(
            50,
            session.Lua.PlayerInteractions.LastClearInteractionType);
    }
}
