using WoWAddonLab.Assets;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class TransmogAppearanceContractTests
{
    [Theory]
    [InlineData("dbfilesclient/item.db2", 841626U)]
    [InlineData("dbfilesclient/itemmodifiedappearance.db2", 982457U)]
    [InlineData("dbfilesclient/itemsparse.db2", 1572924U)]
    public void AppearanceTablesUseTheirClientFileDataIds(string path, uint expected)
    {
        Assert.True(Db2FileDataIds.TryGet(path, out var fileDataId));
        Assert.Equal(expected, fileDataId);
    }

    [Fact]
    public void GetSourceInfoCombinesClientDefinitionWithCollectionState()
    {
        using var session = new EmulatorSession();
        session.TransmogAppearanceProvider = new TestAppearanceProvider();
        session.Lua.TransmogSets.CollectedSourceIds.Add(200);

        Assert.Equal(
            "1:100:200:true:300:4:5:2:true:true:true:3:6:Test Blade:4:nil:nil:nil:nil",
            session.Lua.Evaluate(
                "local info=C_TransmogCollection.GetSourceInfo(200); " +
                "return table.concat({select('#',C_TransmogCollection.GetSourceInfo(200))," +
                "info.visualID,info.sourceID,tostring(info.isCollected),info.itemID," +
                "info.itemModID,info.invType,info.categoryID," +
                "tostring(info.playerCanCollect),tostring(info.isValidSourceForPlayer)," +
                "tostring(info.canDisplayOnPlayer),info.inventorySlot,info.sourceType," +
                "info.name,info.quality,tostring(info.useError),tostring(info.useErrorType)," +
                "tostring(info.meetsTransmogPlayerCondition),tostring(info.isHideVisual)},':')"));
    }

    [Fact]
    public void GetSourceInfoRejectsInvalidArgumentsAndReturnsNothingForUnknownSources()
    {
        using var session = new EmulatorSession();
        session.TransmogAppearanceProvider = new TestAppearanceProvider();

        Assert.Equal(
            "0:false:true",
            session.Lua.Evaluate(
                "local ok,errorText=pcall(C_TransmogCollection.GetSourceInfo); " +
                "return table.concat({select('#',C_TransmogCollection.GetSourceInfo(999))," +
                "tostring(ok),tostring(string.find(errorText,'Usage:',1,true)~=nil)},':')"));
    }

    [Fact]
    public void AppearanceQueriesUseTheSharedClientCatalog()
    {
        using var session = new EmulatorSession();
        session.TransmogAppearanceProvider = new TestAppearanceProvider();
        session.Lua.TransmogSets.CollectedSourceIds.Add(200);
        session.Lua.Evaluate("C_TransmogCollection.SetIsAppearanceFavorite(100,true)");

        Assert.Equal(
            "1:100:true:true:7:1:200:1:200:100:200:2:1:1:true:true:12345:2:100:12345",
            session.Lua.Evaluate(
                "local appearances=C_TransmogCollection.GetCategoryAppearances(2); " +
                "local appearance=appearances[1]; " +
                "local sources=C_TransmogCollection.GetAppearanceSources(100); " +
                "local ids=C_TransmogCollection.GetAllAppearanceSources(100); " +
                "local visual,source=C_TransmogCollection.GetItemInfo(300); " +
                "local hasData,canCollect=C_TransmogCollection.PlayerCanCollectSource(200); " +
                "local data=C_TransmogCollection.GetAppearanceSourceInfo(200); " +
                "return table.concat({#appearances,appearance.visualID," +
                "tostring(appearance.isCollected),tostring(appearance.isFavorite),appearance.uiOrder," +
                "#sources,sources[1].sourceID,#ids,ids[1],visual,source," +
                "C_TransmogCollection.GetCategoryForItem(200)," +
                "C_TransmogCollection.GetCategoryTotal(2)," +
                "C_TransmogCollection.GetCategoryCollectedCount(2)," +
                "tostring(hasData),tostring(canCollect)," +
                "C_TransmogCollection.GetSourceIcon(200),data.category," +
                "data.itemAppearanceID,data.icon},':')"));
    }

    private sealed class TestAppearanceProvider : IWowTransmogAppearanceProvider
    {
        private static readonly WowAppearanceSourceDefinition Source = new(
            100,
            200,
            300,
            4,
            7,
            12345,
            5,
            2,
            7,
            3,
            6,
            "Test Blade",
            4,
            -1,
            0,
            null,
            null);

        public int Count => 1;

        public bool TryGetSource(
            int sourceId,
            out WowAppearanceSourceDefinition definition)
        {
            definition = sourceId == Source.SourceId ? Source : null!;
            return definition is not null;
        }

        public bool TryGetSourceForItem(
            int itemId,
            int? itemModId,
            out WowAppearanceSourceDefinition definition)
        {
            definition = itemId == Source.ItemId &&
                         (itemModId is null || itemModId == Source.ItemModId)
                ? Source
                : null!;
            return definition is not null;
        }

        public IReadOnlyList<WowAppearanceSourceDefinition> GetSourcesByCategory(int categoryId) =>
            categoryId == Source.CategoryId ? [Source] : [];

        public IReadOnlyList<WowAppearanceSourceDefinition> GetSourcesByVisual(int visualId) =>
            visualId == Source.VisualId ? [Source] : [];
    }
}
