using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class ReportedAddonCompatibilityTests
{
    [Fact]
    public void AddonQueriesUseEmulatorState()
    {
        using var session = new EmulatorSession();
        session.Lua.PlayerInfo.HasAccountInventoryLock = true;
        session.Lua.Chat.InChatMessagingLockdown = true;
        session.Lua.Chat.RegisteredAddonMessagePrefixes.Add("DBM");
        session.Lua.ClassTalents.ActiveHeroTalentSpec = 42;
        session.Lua.Scenario.CurrentScenarioId = 17;
        session.Lua.Scenario.Info = new WowScenarioInfoState(
            "Delve",
            2,
            3,
            0,
            false,
            100,
            25,
            1,
            "Cave",
            null);

        Assert.Equal(
            "true:true:false:true:42:Delve:2:17",
            session.Lua.Evaluate(
                "local info=C_ScenarioInfo.GetScenarioInfo();" +
                "return table.concat({" +
                "tostring(C_PlayerInfo.HasAccountInventoryLock())," +
                "tostring(C_ChatInfo.IsAddonMessagePrefixRegistered('DBM'))," +
                "tostring(C_ChatInfo.IsAddonMessagePrefixRegistered('ATT'))," +
                "tostring(C_ChatInfo.InChatMessagingLockdown())," +
                "C_ClassTalents.GetActiveHeroTalentSpec()," +
                "info.name,info.currentStage,info.scenarioID},':')"));
    }

    [Fact]
    public void LegacyMacroBodyReturnsTheStoredText()
    {
        using var session = new EmulatorSession();
        session.Lua.Macros.Create("Test", "INV_Misc_QuestionMark", "/say hi", false);

        Assert.Equal(
            "/say hi:nil",
            session.Lua.Evaluate(
                "return tostring(GetMacroBody(1)) .. ':' .. " +
                "tostring(GetMacroBody(99))"));
    }

    [Fact]
    public void ItemQueriesResolveLocationsAndDetailedData()
    {
        using var session = new EmulatorSession();
        var location = WowItemLocation.Bag(0, 1);
        session.Lua.Items.Items[42] = new WowItemData
        {
            ItemId = 42,
            Link = "|Hitem:42|h[Test]|h",
            ItemLevel = 120,
            SparseItemLevel = 100,
            Family = 32
        };
        session.Lua.Items.LocationItemIds[location] = 42;
        session.Lua.Items.LocationsByGuid["Item-0-42"] = location;

        Assert.Equal(
            "120:nil:100:32:42:|Hitem:42|h[Test]|h:0:1",
            session.Lua.Evaluate(
                "local actual,preview,sparse=" +
                "C_Item.GetDetailedItemLevelInfo(42);" +
                "local loc=C_Item.GetItemLocation('Item-0-42');" +
                "return table.concat({actual,tostring(preview),sparse," +
                "C_Item.GetItemFamily(42),C_Item.GetItemID(loc)," +
                "C_Item.GetItemLink(loc),loc.bagID,loc.slotIndex},':')"));
    }

    [Fact]
    public void WidgetMethodsResolveSelfFromTheCallingCoroutine()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:FontString",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent);" +
                "local label=owner:CreateFontString();label:Hide();" +
                "local thread=coroutine.create(function() label:Show() end);" +
                "local ok,message=coroutine.resume(thread);" +
                "return tostring(ok)..':'..(ok and label:GetObjectType() or message)"));
    }
}
