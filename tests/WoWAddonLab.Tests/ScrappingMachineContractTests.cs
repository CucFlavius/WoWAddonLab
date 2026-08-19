using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class ScrappingMachineContractTests
{
    [Fact]
    public void UsesNativeSlotsEventsValidationAndRequestSemantics()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "11:265742:0:false:false:0",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_ScrappingMachineUI) do " +
                "count=count+1 end;" +
                "local ok=pcall(C_ScrappingMachineUI." +
                "DropPendingScrapItemFromCursor,nil);" +
                "return table.concat({" +
                "count,C_ScrappingMachineUI.GetScrapSpellID()," +
                "select('#',C_ScrappingMachineUI." +
                "GetScrappingMachineName())," +
                "tostring(C_ScrappingMachineUI.HasScrappableItems())," +
                "tostring(ok),select('#',C_ScrappingMachineUI." +
                "GetCurrentPendingScrapItemLocationByIndex(0))},':')"));

        var state = session.Lua.ScrappingMachine;
        var first = WowItemLocation.Bag(1, 2);
        var second = WowItemLocation.Equipment(3);
        state.ScrappableItems.Add(first);
        state.ScrappableItems.Add(second);
        state.MachineName = "Recycler";
        state.CursorItemLocation = first;
        session.Lua.Cursor.SetPayload(
            WowCursorPayloadKind.Item,
            "item",
            first);
        session.Lua.PlayerInteractions.HasActiveInteraction = true;
        session.Lua.PlayerInteractions.CurrentInteractionType = 40;

        session.Lua.Evaluate(
            "scrapEvents={};" +
            "local listener=CreateFrame('Frame');" +
            "for _,event in ipairs({" +
            "'SCRAPPING_MACHINE_ITEM_ADDED'," +
            "'SCRAPPING_MACHINE_ITEM_REMOVED'," +
            "'SCRAPPING_MACHINE_PENDING_ITEM_CHANGED'," +
            "'SCRAPPING_MACHINE_SCRAPPING_FINISHED'}) do " +
            "listener:RegisterEvent(event) end;" +
            "listener:SetScript('OnEvent',function(_,event,arg) " +
            "scrapEvents[#scrapEvents+1]=" +
            "event..':'..tostring(arg) end);" +
            "machineName=C_ScrappingMachineUI." +
            "GetScrappingMachineName();" +
            "C_ScrappingMachineUI." +
            "DropPendingScrapItemFromCursor(0);" +
            "cursorAfterFirst=CursorHasItem();");

        state.CursorItemLocation = second;
        session.Lua.Cursor.SetPayload(
            WowCursorPayloadKind.Item,
            "item",
            second);
        session.Lua.Evaluate(
            "C_ScrappingMachineUI." +
            "DropPendingScrapItemFromCursor(2);" +
            "firstLocation=C_ScrappingMachineUI." +
            "GetCurrentPendingScrapItemLocationByIndex(0);" +
            "secondLocation=C_ScrappingMachineUI." +
            "GetCurrentPendingScrapItemLocationByIndex(2);");

        state.CursorItemLocation = first;
        session.Lua.Cursor.SetPayload(
            WowCursorPayloadKind.Item,
            "item",
            first);
        session.Lua.Evaluate(
            "C_ScrappingMachineUI." +
            "DropPendingScrapItemFromCursor(1);" +
            "cursorAfterDuplicate=CursorHasItem();");
        session.Lua.Cursor.ClearPayload();
        state.CursorItemLocation = null;

        state.PendingItems[8] = WowItemLocation.Bag(9, 9);
        session.Lua.Evaluate(
            "C_ScrappingMachineUI.ValidateScrappingList();" +
            "invalidResultCount=select('#',C_ScrappingMachineUI." +
            "GetCurrentPendingScrapItemLocationByIndex(8));" +
            "C_ScrappingMachineUI.ScrapItems();" +
            "C_ScrappingMachineUI.RemoveCurrentScrappingItem();" +
            "C_ScrappingMachineUI.RemoveItemToScrap(2);");

        state.PendingItems[4] = first;
        session.Lua.Evaluate(
            "C_ScrappingMachineUI.CloseScrappingMachine();");

        Assert.Equal(
            "Recycler:1:2:3:false:true:0:10:" +
            "SCRAPPING_MACHINE_ITEM_ADDED:0," +
            "SCRAPPING_MACHINE_PENDING_ITEM_CHANGED:nil," +
            "SCRAPPING_MACHINE_ITEM_ADDED:2," +
            "SCRAPPING_MACHINE_PENDING_ITEM_CHANGED:nil," +
            "SCRAPPING_MACHINE_PENDING_ITEM_CHANGED:nil," +
            "SCRAPPING_MACHINE_ITEM_REMOVED:0," +
            "SCRAPPING_MACHINE_PENDING_ITEM_CHANGED:nil," +
            "SCRAPPING_MACHINE_ITEM_REMOVED:2," +
            "SCRAPPING_MACHINE_PENDING_ITEM_CHANGED:nil," +
            "SCRAPPING_MACHINE_PENDING_ITEM_CHANGED:nil",
            session.Lua.Evaluate(
                "return table.concat({" +
                "machineName,firstLocation.bagID," +
                "firstLocation.slotIndex," +
                "secondLocation.equipmentSlotIndex," +
                "tostring(cursorAfterFirst)," +
                "tostring(cursorAfterDuplicate),invalidResultCount," +
                "#scrapEvents,table.concat(scrapEvents,',')},':')"));

        var request = Assert.Single(state.ScrapRequests);
        Assert.Equal([first, second], request.Items);
        Assert.Equal(0, state.CurrentScrappingIndex);
        Assert.True(state.IsScrapping);
        Assert.False(
            session.Lua.PlayerInteractions.HasActiveInteraction);
        Assert.Equal(
            40,
            session.Lua.PlayerInteractions.LastClearInteractionType);
    }
}
