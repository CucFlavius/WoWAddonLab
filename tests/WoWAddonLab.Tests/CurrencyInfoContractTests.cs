using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class CurrencyInfoContractTests
{
    [Fact]
    public void RegistersExactSurfaceEnumsConstantsAndNativeEmptyContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "43:0:0:0:1:nil:2:false:5:0:1553:2003:1560:" +
            "133788:133789:133786:133787:133784:133785:" +
            "0:1:2:3:0:2:0:5:10:11:0:10:" +
            "1:16777216:-2147483648:32:2048:4:" +
            "134400:463448:463449:463450:463451:0:100000000",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_CurrencyInfo) do count=count+1 end;" +
                "local can,reason=C_CurrencyInfo.CanTransferCurrency(99);" +
                "return table.concat({" +
                "count," +
                "select('#',C_CurrencyInfo.GetCurrencyInfo(99))," +
                "select('#',C_CurrencyInfo.GetBasicCurrencyInfo(99))," +
                "select('#',C_CurrencyInfo.GetCurrencyListInfo(1))," +
                "select('#',C_CurrencyInfo.GetCurrencyDescription(99))," +
                "tostring(C_CurrencyInfo.GetCurrencyDescription(99))," +
                "select('#',C_CurrencyInfo.CanTransferCurrency(99))," +
                "tostring(can),reason,C_CurrencyInfo.GetCurrencyListSize()," +
                "C_CurrencyInfo.GetAzeriteCurrencyID()," +
                "C_CurrencyInfo.GetDragonIslesSuppliesCurrencyID()," +
                "C_CurrencyInfo.GetWarResourcesCurrencyID()," +
                "C_CurrencyInfo.GetCoinIcon(0)," +
                "C_CurrencyInfo.GetCoinIcon(10)," +
                "C_CurrencyInfo.GetCoinIcon(100)," +
                "C_CurrencyInfo.GetCoinIcon(1000)," +
                "C_CurrencyInfo.GetCoinIcon(10000)," +
                "C_CurrencyInfo.GetCoinIcon(100000)," +
                "Enum.CurrencyFilterType.None," +
                "Enum.CurrencyFilterType.DiscoveredOnly," +
                "Enum.CurrencyFilterType.DiscoveredAndAllAccountTransferable," +
                "Enum.CurrencyFilterTypeMeta.NumValues," +
                "Enum.CurrencyFilterTypeMeta.MinValue," +
                "Enum.CurrencyFilterTypeMeta.MaxValue," +
                "Enum.AccountCurrencyTransferResult.Success," +
                "Enum.AccountCurrencyTransferResult.InvalidCurrency," +
                "Enum.AccountCurrencyTransferResult.CurrencyTransferDisabled," +
                "Enum.AccountCurrencyTransferResultMeta.NumValues," +
                "Enum.AccountCurrencyTransferResultMeta.MinValue," +
                "Enum.AccountCurrencyTransferResultMeta.MaxValue," +
                "Enum.CurrencyFlags.CurrencyTradable," +
                "Enum.CurrencyFlags.CurrencyAccountWide," +
                "Enum.CurrencyFlags.CurrencyUsesLedgerBalance," +
                "Enum.CurrencyFlagsMeta.NumValues," +
                "Enum.CurrencyFlagsB.CurrencyBNoBonusXP," +
                "Enum.CurrencyTokenCategoryFlags.Hidden," +
                "Constants.CurrencyConsts.QUESTIONMARK_INV_ICON," +
                "Constants.CurrencyConsts.PVP_CURRENCY_CONQUEST_ALLIANCE_INV_ICON," +
                "Constants.CurrencyConsts.PVP_CURRENCY_CONQUEST_HORDE_INV_ICON," +
                "Constants.CurrencyConsts.PVP_CURRENCY_HONOR_ALLIANCE_INV_ICON," +
                "Constants.CurrencyConsts.PVP_CURRENCY_HONOR_HORDE_INV_ICON," +
                "Constants.CurrencyConsts.CURRENCY_WALLET_TYPE_WOWMONEY," +
                "Constants.CurrencyConsts.MAX_CURRENCY_QUANTITY},':')"));

        Assert.Equal(
            "1602:Conquest:0:0:1792:Honor:0:0",
            session.Lua.Evaluate(
                "local conquest=C_CurrencyInfo.GetCurrencyInfo(" +
                "Constants.CurrencyConsts.CONQUEST_CURRENCY_ID);" +
                "local honor=C_CurrencyInfo.GetCurrencyInfo(" +
                "Constants.CurrencyConsts.HONOR_CURRENCY_ID);" +
                "return table.concat({conquest.currencyID,conquest.name," +
                "conquest.maxQuantity,conquest.totalEarned,honor.currencyID," +
                "honor.name,honor.maxQuantity,honor.totalEarned},':')"));
    }

    [Fact]
    public void EnforcesRecoveredNumericBooleanIndexAndLinkContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:false:false:false:true:false:false:" +
            "42:42:0:true:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_CurrencyInfo.GetCurrencyInfo,'42'))," +
                "tostring(ok(C_CurrencyInfo.SetCurrencyFilter,'2'))," +
                "tostring(ok(C_CurrencyInfo.SetCurrencyFilter,3))," +
                "tostring(ok(C_CurrencyInfo.ExpandCurrencyList,0,true))," +
                "tostring(ok(C_CurrencyInfo.ExpandCurrencyList,1,1))," +
                "tostring(ok(C_CurrencyInfo.GetCoinText,'123'))," +
                "tostring(ok(C_CurrencyInfo.GetCoinText,-1))," +
                "tostring(ok(C_CurrencyInfo.GetCurrencyInfo,{}))," +
                "C_CurrencyInfo.GetCurrencyIDFromLink('|Hcurrency:42:9|h[x]|h')," +
                "C_CurrencyInfo.GetCurrencyIDFromLink('x currency:0x2A:0')," +
                "C_CurrencyInfo.GetCurrencyIDFromLink('item:42')," +
                "tostring(ok(C_CurrencyInfo.GetPlayerCurrencyCategoryInfo,1,nil))," +
                "tostring(ok(C_CurrencyInfo.GetPlayerCurrencyCategoryInfo,1,1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredCurrencyInfoAndBasicInfoShapes()
    {
        using var session = new EmulatorSession();
        session.Lua.CurrencyInfo.Currencies[77] = new WowCurrencyDefinition
        {
            CurrencyId = 77,
            Name = "Test Coin",
            Description = "A test currency.",
            CurrencyListDepth = 2,
            IsTypeUnused = true,
            IsShowInBackpack = true,
            Quantity = 30,
            TrackedQuantity = 8,
            IconFileId = 901,
            MaxQuantity = 100,
            CanEarnPerWeek = true,
            QuantityEarnedThisWeek = 4,
            IsTradeable = true,
            Quality = 3,
            MaxWeeklyQuantity = 20,
            TotalEarned = 44,
            Discovered = true,
            UseTotalEarnedForMaxQuantity = true,
            IsAccountWide = true,
            TransferPercentage = 50,
            RechargingCycleDurationMilliseconds = 60000,
            RechargingAmountPerCycle = 5,
            FactionId = 123,
            WarModeBonusApplies = true,
            LimitWarModeBonusOncePerTooltip = false
        };

        Assert.Equal(
            "Test Coin:A test currency.:77:false:false:2:true:true:" +
            "30:8:901:100:true:4:true:3:20:44:true:true:true:true:" +
            "50:60000:5:" +
            "Test Coin:A test currency.:901:3:12:12:" +
            "A test currency.:123:true:false:true",
            session.Lua.Evaluate(
                "local i=C_CurrencyInfo.GetCurrencyInfo(77);" +
                "local b=C_CurrencyInfo.GetBasicCurrencyInfo(77,12);" +
                "local wm,once=C_CurrencyInfo.DoesWarModeBonusApply(77);" +
                "return table.concat({" +
                "i.name,i.description,i.currencyID,tostring(i.isHeader)," +
                "tostring(i.isHeaderExpanded),i.currencyListDepth," +
                "tostring(i.isTypeUnused),tostring(i.isShowInBackpack)," +
                "i.quantity,i.trackedQuantity,i.iconFileID,i.maxQuantity," +
                "tostring(i.canEarnPerWeek),i.quantityEarnedThisWeek," +
                "tostring(i.isTradeable),i.quality,i.maxWeeklyQuantity," +
                "i.totalEarned,tostring(i.discovered)," +
                "tostring(i.useTotalEarnedForMaxQty)," +
                "tostring(i.isAccountWide),tostring(i.isAccountTransferable)," +
                "i.transferPercentage,i.rechargingCycleDurationMS," +
                "i.rechargingAmountPerCycle," +
                "b.name,b.description,b.icon,b.quality,b.displayAmount,b.actualAmount," +
                "C_CurrencyInfo.GetCurrencyDescription(77)," +
                "C_CurrencyInfo.GetFactionGrantedByCurrency(77)," +
                "tostring(wm),tostring(once)," +
                "tostring(C_CurrencyInfo.IsAccountWideCurrency(77))},':')"));

        session.Lua.CurrencyInfo.Currencies[78] = new WowCurrencyDefinition
        {
            CurrencyId = 78,
            Name = "Non-transferable"
        };
        Assert.Equal(
            "0:false",
            session.Lua.Evaluate(
                "local i=C_CurrencyInfo.GetCurrencyInfo(78);" +
                "return i.transferPercentage..':'.." +
                "tostring(i.isAccountTransferable)"));
    }

    [Fact]
    public void ProjectsListBackpackCategoryLinksAndTransferMath()
    {
        using var session = new EmulatorSession();
        var header = new WowCurrencyDefinition
        {
            CurrencyId = 1,
            Name = "Header",
            IsHeader = true
        };
        var coin = new WowCurrencyDefinition
        {
            CurrencyId = 2,
            Name = "Coin",
            Description = "Currency",
            Quantity = 25,
            IconFileId = 44,
            Quality = 2,
            IsShowInBackpack = true,
            TransferPercentage = 50
        };
        session.Lua.CurrencyInfo.Currencies[1] = header;
        session.Lua.CurrencyInfo.Currencies[2] = coin;
        session.Lua.CurrencyInfo.CurrencyList.Add(1);
        session.Lua.CurrencyInfo.CurrencyList.Add(2);
        session.Lua.CurrencyInfo.Categories[9] =
            new WowPlayerCurrencyCategoryInfo("Category", [2], [10, 11]);
        session.Lua.CurrencyInfo.ContainerInfo[(2, 5)] =
            new WowBasicCurrencyInfo("Box", "Contains currency.", 55, 4, 5, 7);

        Assert.Equal(
            "2:Header:true:Coin:25:44:2:Category:2:10:11:" +
            "|cnIQ2:|Hcurrency:2:12|h[Coin]|h|r:" +
            "|cnIQ2:|Hcurrency:2:0|h[Coin]|h|r:" +
            "20:5:true:Box:7",
            session.Lua.Evaluate(
                "C_CurrencyInfo.ExpandCurrencyList(1,true);" +
                "local h=C_CurrencyInfo.GetCurrencyListInfo(1);" +
                "local b=C_CurrencyInfo.GetBackpackCurrencyInfo(1);" +
                "local c=C_CurrencyInfo.GetPlayerCurrencyCategoryInfo(9,true);" +
                "local box=C_CurrencyInfo.GetCurrencyContainerInfo(2,5);" +
                "return table.concat({" +
                "C_CurrencyInfo.GetCurrencyListSize(),h.name," +
                "tostring(h.isHeaderExpanded),b.name,b.quantity,b.iconFileID," +
                "b.currencyTypesID,c.categoryName,c.currencyTypes[1]," +
                "c.childCategories[1],c.childCategories[2]," +
                "C_CurrencyInfo.GetCurrencyLink(2,12)," +
                "C_CurrencyInfo.GetCurrencyListLink(2)," +
                "C_CurrencyInfo.GetCostToTransferCurrency(2,10)," +
                "C_CurrencyInfo.GetMaxTransferableAmountFromQuantity(2,10)," +
                "tostring(C_CurrencyInfo.IsCurrencyContainer(2,5))," +
                "box.name,box.actualAmount},':')"));

        Assert.Equal(
            "nil:nil:false",
            session.Lua.Evaluate(
                "local bad=pcall(C_CurrencyInfo.GetMaxTransferableAmountFromQuantity,{},1); " +
                "return table.concat({" +
                "tostring(C_CurrencyInfo.GetMaxTransferableAmountFromQuantity(nil,0))," +
                "tostring(C_CurrencyInfo.GetMaxTransferableAmountFromQuantity(2,nil))," +
                "tostring(bad)},':')"));
    }

    [Fact]
    public void ProjectsAccountCharacterAndTransactionArrays()
    {
        using var session = new EmulatorSession();
        session.Lua.CurrencyInfo.AccountCharacterData[7] =
        [
            new WowCharacterCurrencyData(
                "Player-1",
                "Alpha",
                "Alpha-Realm",
                7,
                88)
        ];
        session.Lua.CurrencyInfo.TransferTransactions.Add(
            new WowCurrencyTransferTransaction(
                "Player-1",
                "Alpha",
                "Alpha-Realm",
                "Player-2",
                "Beta",
                "Beta-Realm",
                7,
                10,
                20,
                1234));

        Assert.Equal(
            "1:Player-1:Alpha:Alpha-Realm:7:88:" +
            "1:Player-1:Alpha:Player-2:Beta:7:10:20:1234",
            session.Lua.Evaluate(
                "local c=C_CurrencyInfo.FetchCurrencyDataFromAccountCharacters(7)[1];" +
                "local t=C_CurrencyInfo.FetchCurrencyTransferTransactions()[1];" +
                "return table.concat({" +
                "1,c.characterGUID,c.characterName,c.fullCharacterName," +
                "c.currencyID,c.quantity,1,t.sourceCharacterGUID," +
                "t.sourceCharacterName,t.destinationCharacterGUID," +
                "t.destinationCharacterName,t.currencyType," +
                "t.quantityTransferred,t.totalQuantityConsumed,t.timestamp},':')"));
    }

    [Fact]
    public void RetainsRecoveredFilterListAndTransferMutations()
    {
        using var session = new EmulatorSession();
        session.Lua.CurrencyInfo.Currencies[2] = new WowCurrencyDefinition
        {
            CurrencyId = 2,
            Name = "Coin",
            MaxQuantity = 10,
            MaxWeeklyQuantity = 5,
            Quantity = 10,
            QuantityEarnedThisWeek = 5
        };
        session.Lua.CurrencyInfo.CurrencyList.Add(2);

        Assert.Equal(
            "2:true:true:2:true:true:false:false:true:true",
            session.Lua.Evaluate(
                "C_CurrencyInfo.SetCurrencyFilter(2);" +
                "C_CurrencyInfo.SetCurrencyBackpack(1,true);" +
                "C_CurrencyInfo.SetCurrencyUnused(1,true);" +
                "C_CurrencyInfo.PickupCurrency(2);" +
                "C_CurrencyInfo.RequestCurrencyFromAccountCharacter(" +
                "'Player-1',2,3);" +
                "local i=C_CurrencyInfo.GetCurrencyInfo(2);" +
                "return table.concat({" +
                "C_CurrencyInfo.GetCurrencyFilter()," +
                "tostring(C_CurrencyInfo.DoesCurrentFilterRequireAccountCurrencyData())," +
                "tostring(C_CurrencyInfo.IsAccountCharacterCurrencyDataReady())," +
                "select('#',C_CurrencyInfo.CanTransferCurrency(2))," +
                "tostring(i.isShowInBackpack),tostring(i.isTypeUnused)," +
                "tostring(C_CurrencyInfo.IsAccountTransferableCurrency(2))," +
                "tostring(C_CurrencyInfo.IsAccountWideCurrency(2))," +
                "tostring(C_CurrencyInfo.PlayerHasMaxQuantity(2))," +
                "tostring(C_CurrencyInfo.PlayerHasMaxWeeklyQuantity(2))" +
                "},':')"));

        Assert.Equal(2, session.Lua.CurrencyInfo.LastPickedUpCurrencyId);
        Assert.Equal(
            ("Player-1", 2, 3u),
            session.Lua.CurrencyInfo.LastCurrencyTransferRequest);
    }
}
