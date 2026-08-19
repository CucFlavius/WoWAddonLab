using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class LegendaryCraftingContractTests
{
    [Fact]
    public void UsesNativeTablesEnumsCostsAndRequests()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "20:3:4:288097:1:nil:false:false:0:0:8:1",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_LegendaryCrafting) do " +
                "count=count+1 end;" +
                "local component=C_LegendaryCrafting." +
                "GetRuneforgeLegendaryComponentInfo(" +
                "{equipmentSlotIndex=1});" +
                "local power=C_LegendaryCrafting." +
                "GetRuneforgePowerInfo(123);" +
                "local fields=0; for _ in pairs(power) do fields=fields+1 end;" +
                "local okItem=pcall(C_LegendaryCrafting." +
                "IsRuneforgeLegendary,nil);" +
                "local okFilter=pcall(C_LegendaryCrafting." +
                "GetRuneforgePowers,nil,4);" +
                "return table.concat({" +
                "count,Enum.RuneforgePowerStateMeta.NumValues," +
                "Enum.RuneforgePowerFilterMeta.NumValues," +
                "C_LegendaryCrafting.GetRuneforgeLegendaryCraftSpellID()," +
                "select('#',C_LegendaryCrafting." +
                "GetRuneforgeItemPreviewInfo({equipmentSlotIndex=1}))," +
                "tostring(C_LegendaryCrafting." +
                "GetRuneforgeItemPreviewInfo({equipmentSlotIndex=1}))," +
                "tostring(okItem),tostring(okFilter)," +
                "component.powerID,#component.modifiers,fields,power.state" +
                "},':')"));

        var crafting = session.Lua.LegendaryCrafting;
        var baseItem = WowItemLocation.Bag(1, 2);
        var legendary = WowItemLocation.Equipment(3);
        var upgrade = WowItemLocation.Bag(4, 5);
        crafting.Currencies.AddRange([1904, 2001]);
        crafting.Modifiers.AddRange([10, 20]);
        crafting.CostsByItem[legendary] =
        [
            new WowRuneforgeCurrencyCost(1904, 100),
            new WowRuneforgeCurrencyCost(2000, 20)
        ];
        crafting.CostsByItem[upgrade] =
        [
            new WowRuneforgeCurrencyCost(1904, 250),
            new WowRuneforgeCurrencyCost(2001, 7)
        ];
        crafting.ComponentsByItem[legendary] =
            new WowRuneforgeLegendaryComponentInfo(88, [10, 20]);
        crafting.Powers[88] = new WowRuneforgePowerInfo(
            88,
            0,
            "Memory",
            1234,
            "Description",
            "Boss",
            9876,
            "Frost",
            true,
            false,
            3,
            ["Head", "Shoulder"]);
        crafting.PowerLists[(baseItem, 2)] =
            new WowRuneforgePowerLists([88], [99]);
        crafting.PowerListsByClassSpecAndCovenant[(1, 2, 3, 1)] =
            [88, 77];
        crafting.PreviewRules.Add(new WowRuneforgePreviewRule(
            baseItem,
            88,
            [10, 20],
            new WowRuneforgeItemPreviewInfo(
                "Item-1",
                291,
                "Crafted Item")));
        crafting.ModifierInfoRules.Add(new WowRuneforgeModifierInfoRule(
            baseItem,
            88,
            1,
            [10],
            "Haste",
            ["Line 1", "Line 2"]));
        crafting.RuneforgeLegendaryItems.Add(legendary);
        crafting.MaxLevelRuneforgeLegendaryItems.Add(legendary);
        crafting.ValidBaseItems.Add(baseItem);
        crafting.ValidUpgradePairs.Add((legendary, upgrade));
        session.Lua.PlayerInteractions.HasActiveInteraction = true;
        session.Lua.PlayerInteractions.CurrentInteractionType = 48;

        Assert.Equal(
            "Item-1:291:Crafted Item:88:2:1904:100:2:1904:150:" +
            "2001:7:2:10:20:2:1904:2001:Haste:2:Line 1:Line 2:" +
            "12:88:0:Memory:1234:Description:Boss:9876:Frost:true:false:" +
            "3:2:Head:Shoulder:88:99:88:77:true:true:true:true:" +
            "1:2:88:2:10:20:1",
            session.Lua.Evaluate(
                "runeforgeClosed=0;" +
                "local listener=CreateFrame('Frame');" +
                "listener:RegisterEvent(" +
                "'RUNEFORGE_LEGENDARY_CRAFTING_CLOSED');" +
                "listener:SetScript('OnEvent',function() " +
                "runeforgeClosed=runeforgeClosed+1 end);" +
                "local preview=C_LegendaryCrafting." +
                "GetRuneforgeItemPreviewInfo(" +
                "{bagID=1,slotIndex=2},88,{10,20});" +
                "local component=C_LegendaryCrafting." +
                "GetRuneforgeLegendaryComponentInfo(" +
                "{equipmentSlotIndex=3});" +
                "local cost=C_LegendaryCrafting." +
                "GetRuneforgeLegendaryCost({equipmentSlotIndex=3});" +
                "local upgradeCost=C_LegendaryCrafting." +
                "GetRuneforgeLegendaryUpgradeCost(" +
                "{equipmentSlotIndex=3},{bagID=4,slotIndex=5});" +
                "local name,description=C_LegendaryCrafting." +
                "GetRuneforgeModifierInfo(" +
                "{bagID=1,slotIndex=2},88,2,{10});" +
                "local power=C_LegendaryCrafting.GetRuneforgePowerInfo(88);" +
                "local fields=0; for _ in pairs(power) do fields=fields+1 end;" +
                "local primary,other=C_LegendaryCrafting." +
                "GetRuneforgePowers({bagID=1,slotIndex=2},2);" +
                "local byClass=C_LegendaryCrafting." +
                "GetRuneforgePowersByClassSpecAndCovenant(1,2,3,1);" +
                "local craft=C_LegendaryCrafting." +
                "MakeRuneforgeCraftDescription(" +
                "{bagID=1,slotIndex=2},88,{10,20});" +
                "C_LegendaryCrafting.CraftRuneforgeLegendary(craft);" +
                "C_LegendaryCrafting.UpgradeRuneforgeLegendary(" +
                "{equipmentSlotIndex=3},{bagID=4,slotIndex=5});" +
                "C_LegendaryCrafting.CloseRuneforgeInteraction();" +
                "return table.concat({" +
                "preview.itemGUID,preview.itemLevel,preview.itemName," +
                "component.powerID,#component.modifiers," +
                "cost[1].currencyID,cost[1].amount,#upgradeCost," +
                "upgradeCost[1].currencyID,upgradeCost[1].amount," +
                "upgradeCost[2].currencyID,upgradeCost[2].amount," +
                "#C_LegendaryCrafting.GetRuneforgeModifiers()," +
                "C_LegendaryCrafting.GetRuneforgeModifiers()[1]," +
                "C_LegendaryCrafting.GetRuneforgeModifiers()[2]," +
                "#C_LegendaryCrafting.GetRuneforgeLegendaryCurrencies()," +
                "C_LegendaryCrafting.GetRuneforgeLegendaryCurrencies()[1]," +
                "C_LegendaryCrafting.GetRuneforgeLegendaryCurrencies()[2]," +
                "name,#description,description[1],description[2]," +
                "fields,power.runeforgePowerID,power.state,power.name," +
                "power.descriptionSpellID,power.description,power.source," +
                "power.iconFileID,power.specName,tostring(power.matchesSpec)," +
                "tostring(power.matchesCovenant),power.covenantID," +
                "#power.slots,power.slots[1],power.slots[2]," +
                "primary[1],other[1],byClass[1],byClass[2]," +
                "tostring(C_LegendaryCrafting.IsRuneforgeLegendary(" +
                "{equipmentSlotIndex=3}))," +
                "tostring(C_LegendaryCrafting." +
                "IsRuneforgeLegendaryMaxLevel({equipmentSlotIndex=3}))," +
                "tostring(C_LegendaryCrafting.IsValidRuneforgeBaseItem(" +
                "{bagID=1,slotIndex=2}))," +
                "tostring(C_LegendaryCrafting." +
                "IsUpgradeItemValidForRuneforgeLegendary(" +
                "{equipmentSlotIndex=3},{bagID=4,slotIndex=5}))," +
                "craft.baseItem.bagID,craft.baseItem.slotIndex," +
                "craft.runeforgePowerID,#craft.modifiers," +
                "craft.modifiers[1],craft.modifiers[2],runeforgeClosed},':')"));

        var craftRequest = Assert.Single(crafting.CraftRequests);
        Assert.Equal(baseItem, craftRequest.BaseItem);
        Assert.Equal(88, craftRequest.PowerId);
        Assert.Equal([10, 20], craftRequest.Modifiers);
        Assert.Equal(
            [new WowRuneforgeUpgradeRequest(legendary, upgrade)],
            crafting.UpgradeRequests);
        Assert.False(session.Lua.PlayerInteractions.HasActiveInteraction);
        Assert.Equal(
            48,
            session.Lua.PlayerInteractions.LastClearInteractionType);
    }
}
