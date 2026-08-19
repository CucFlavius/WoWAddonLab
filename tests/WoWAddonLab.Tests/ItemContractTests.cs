using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class ItemContractTests
{
    [Fact]
    public void ItemCountHonorsStorageAndUseFlags()
    {
        using var session = new EmulatorSession();
        session.Lua.Items.Counts[19019] = new WowItemCountData
        {
            Backpack = 2,
            Bank = 3,
            ReagentBank = 5,
            AccountBank = 7,
            Uses = 11
        };

        Assert.Equal(
            "2:5:10:17:11",
            session.Lua.Evaluate(
                "return table.concat({" +
                "C_Item.GetItemCount(19019)," +
                "C_Item.GetItemCount(19019,true)," +
                "C_Item.GetItemCount(19019,true,false,true)," +
                "C_Item.GetItemCount(19019,true,false,true,true)," +
                "C_Item.GetItemCount(19019,false,true)" +
                "},':')"));
    }

    [Fact]
    public void ItemCompatibilityQueriesExposeNativeReturnShapes()
    {
        using var session = new EmulatorSession();
        session.Lua.Items.Classes[2] = "Weapon";
        session.Lua.Items.SpecializationIds[19019] = [71, 72, 73];

        Assert.Equal(
            "Weapon:19019:71,72,73:0",
            session.Lua.Evaluate(
                "local specs=C_Item.GetItemSpecInfo('item:19019'); " +
                "return table.concat({" +
                "C_Item.GetItemClassInfo(2)," +
                "C_Item.GetItemIDForItemInfo('item:19019')," +
                "table.concat(specs,',')," +
                "select('#',C_Item.GetItemSpecInfo(1))" +
                "},':')"));
    }

    [Fact]
    public void ItemNameByIdReturnsCachedNamesAndNilForUncachedItems()
    {
        using var session = new EmulatorSession();
        session.Lua.Items.Items[19019] = new WowItemData
        {
            ItemId = 19019,
            Name = "Thunderfury"
        };

        Assert.Equal(
            "Thunderfury:1:nil:false",
            session.Lua.Evaluate(
                "local missing=C_Item.GetItemNameByID(1);" +
                "return table.concat({C_Item.GetItemNameByID('item:19019')," +
                "select('#',C_Item.GetItemNameByID(1)),tostring(missing)," +
                "tostring(pcall(C_Item.GetItemNameByID,{}))},':')"));
    }

    [Fact]
    public void ItemQualityEnumUsesTheNativeZeroBasedRange()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:1:4:8:0:8:9",
            session.Lua.Evaluate(
                "return table.concat({Enum.ItemQuality.Poor,Enum.ItemQuality.Common," +
                "Enum.ItemQuality.Epic,Enum.ItemQuality.WoWToken," +
                "Enum.ItemQualityMeta.MinValue,Enum.ItemQualityMeta.MaxValue," +
                "Enum.ItemQualityMeta.NumValues},':')"));
    }

    [Fact]
    public void ClientItemClassesAndTooltipInfoUseNativeAvailabilityShapes()
    {
        using var session = new EmulatorSession();
        session.ItemClassProvider = new TestItemClassProvider(
            new Dictionary<int, string> { [5] = "Reagent" },
            new Dictionary<(int, int), WowItemSubClassData>
            {
                [(5, 1)] = new("Crafting Reagent", true)
            });

        Assert.Equal(
            "Reagent:Crafting Reagent:true:function:0:0:0",
            session.Lua.Evaluate(
                "local subClass,usesInventoryType=" +
                "C_Item.GetItemSubClassInfo(5,1); " +
                "return table.concat({" +
                "C_Item.GetItemClassInfo(5)," +
                "subClass,tostring(usesInventoryType)," +
                "type(C_TooltipInfo.GetOwnedItemByID)," +
                "select('#',C_TooltipInfo.GetOwnedItemByID(1))," +
                "select('#',C_TooltipInfo.GetItemByItemModifiedAppearanceID(1))," +
                "select('#',C_TooltipInfo.GetHyperlink('item:1'))" +
                "},':')"));
    }

    private sealed class TestItemClassProvider(
        IReadOnlyDictionary<int, string> classes,
        IReadOnlyDictionary<(int ClassId, int SubClassId), WowItemSubClassData>
            subClasses)
        : IWowItemClassProvider
    {
        public IReadOnlyDictionary<int, string> Classes { get; } = classes;
        public IReadOnlyDictionary<(int ClassId, int SubClassId), WowItemSubClassData>
            SubClasses { get; } = subClasses;
    }
}
