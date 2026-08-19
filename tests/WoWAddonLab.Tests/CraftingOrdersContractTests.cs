using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class CraftingOrdersContractTests
{
    private static readonly string[] NativeFunctions =
    [
        "AreOrderNotesDisabled",
        "CalculateCraftingOrderPostingFee",
        "CanOrderSkillAbility",
        "CancelOrder",
        "ClaimOrder",
        "CloseCrafterCraftingOrders",
        "CloseCustomerCraftingOrders",
        "FulfillOrder",
        "GetClaimedOrder",
        "GetCrafterBuckets",
        "GetCrafterOrders",
        "GetCraftingOrderTime",
        "GetCustomerCategories",
        "GetCustomerOptions",
        "GetCustomerOrders",
        "GetDefaultOrdersSkillLine",
        "GetMyOrders",
        "GetNumFavoriteCustomerOptions",
        "GetOrderClaimInfo",
        "GetPersonalOrdersInfo",
        "HasFavoriteCustomerOptions",
        "IsCustomerOptionFavorited",
        "ListMyOrders",
        "OpenCrafterCraftingOrders",
        "OpenCustomerCraftingOrders",
        "OrderCanBeRecrafted",
        "ParseCustomerOptions",
        "PlaceNewOrder",
        "RejectOrder",
        "ReleaseOrder",
        "RequestCrafterOrders",
        "RequestCustomerOrders",
        "SetCustomerOptionFavorited",
        "ShouldShowCraftingOrderTab",
        "SkillLineHasOrders",
        "UpdateIgnoreList"
    ];

    [Fact]
    public void RegistersExactRecoveredFunctionSurface()
    {
        using var session = new EmulatorSession();
        var names = string.Join(
            ",",
            NativeFunctions.Select(name => $"'{name}'"));

        Assert.Equal(
            "36:36:true",
            session.Lua.Evaluate(
                $"local expected={{{names}}}; local count=0; " +
                "for _ in pairs(C_CraftingOrders) do count=count+1 end; " +
                "local found=0; local functions=true; " +
                "for _,name in ipairs(expected) do " +
                "if C_CraftingOrders[name]~=nil then found=found+1 end; " +
                "functions=functions and type(C_CraftingOrders[name])=='function' end; " +
                "return table.concat({count,found,tostring(functions)},':')"));
    }

    [Fact]
    public void ReturnsRecoveredCrafterStateAndRecordShapes()
    {
        using var session = new EmulatorSession();
        var state = session.Lua.CraftingOrders;
        var order = CreateOrder(77) with
        {
            Reagents =
            [
                new WowCraftingOrderReagentState(
                    new WowCraftingOrderReagentInfoState(
                        new WowCraftingReagentInfo(ItemId: 11),
                        2,
                        3),
                    4,
                    1,
                    true)
            ],
            NpcOrderRewards =
            [
                new WowCraftingOrderNpcRewardState("Item-1", null, 5),
                new WowCraftingOrderNpcRewardState(null, 6, 7)
            ]
        };
        state.OrderableSkillLineAbilityIds.Add(17);
        state.CrafterBuckets.Add(
            new WowCraftingOrderBucketInfoState(1, 2, 17, 300, 900, 4));
        state.CrafterOrders.Add(order);
        state.ClaimedOrder = order;
        state.CraftingOrderTime = 123456;
        state.DefaultOrdersSkillLine = 282;
        state.OrderClaimInfo[3] = new WowCraftingOrderClaimInfoState(2, 45);
        state.RecraftableOrderIds.Add(77);
        state.ShouldShowCraftingOrderTab = true;
        state.SkillLinesWithOrders.Add(282);

        Assert.Equal(
            "true:false:77:1:2:17:300:900:4:1:11:nil:2:3:4:1:true:" +
            "2:Item-1:nil:5:nil:6:7:123456:282:2:45:true:false:true:" +
            "true:false",
            session.Lua.Evaluate(
                "local claimed=C_CraftingOrders.GetClaimedOrder(); " +
                "local buckets=C_CraftingOrders.GetCrafterBuckets(); " +
                "local bucket=buckets[1]; " +
                "local orders=C_CraftingOrders.GetCrafterOrders(); " +
                "local reagent=orders[1].reagents[1]; " +
                "local rewards=orders[1].npcOrderRewards; " +
                "local claim=C_CraftingOrders.GetOrderClaimInfo(3); " +
                "return table.concat({" +
                "tostring(C_CraftingOrders.CanOrderSkillAbility(17))," +
                "tostring(C_CraftingOrders.CanOrderSkillAbility(18))," +
                "claimed.orderID,bucket.itemID,bucket.spellID," +
                "bucket.skillLineAbilityID,bucket.tipAmountAvg," +
                "bucket.tipAmountMax,bucket.numAvailable," +
                "#orders,reagent.reagentInfo.reagent.itemID," +
                "tostring(reagent.reagentInfo.reagent.currencyID)," +
                "reagent.reagentInfo.dataSlotIndex," +
                "reagent.reagentInfo.quantity,reagent.slotIndex," +
                "reagent.source,tostring(reagent.isBasicReagent)," +
                "#rewards,rewards[1].itemLink," +
                "tostring(rewards[1].currencyType),rewards[1].count," +
                "tostring(rewards[2].itemLink),rewards[2].currencyType," +
                "rewards[2].count," +
                "C_CraftingOrders.GetCraftingOrderTime()," +
                "C_CraftingOrders.GetDefaultOrdersSkillLine()," +
                "claim.claimsRemaining,claim.secondsToRecharge," +
                "tostring(C_CraftingOrders.OrderCanBeRecrafted(77))," +
                "tostring(C_CraftingOrders.OrderCanBeRecrafted(78))," +
                "tostring(C_CraftingOrders.ShouldShowCraftingOrderTab())," +
                "tostring(C_CraftingOrders.SkillLineHasOrders(282))," +
                "tostring(C_CraftingOrders.SkillLineHasOrders(283))},':')"));
    }

    [Fact]
    public void MissingOptionalResultsUseNativeNilAndDefaultClaimShape()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "nil:nil:0:nil",
            session.Lua.Evaluate(
                "local claimed=C_CraftingOrders.GetClaimedOrder(); " +
                "local skill=C_CraftingOrders.GetDefaultOrdersSkillLine(); " +
                "local claim=C_CraftingOrders.GetOrderClaimInfo(0); " +
                "return table.concat({tostring(claimed),tostring(skill)," +
                "claim.claimsRemaining,tostring(claim.secondsToRecharge)},':')"));
    }

    [Fact]
    public void ParsesRecoveredCrafterMutationContracts()
    {
        using var session = new EmulatorSession();

        session.Lua.Evaluate(
            "C_CraftingOrders.ClaimOrder(101,14); " +
            "C_CraftingOrders.FulfillOrder(102,'done',0); " +
            "C_CraftingOrders.RejectOrder(103,'declined',7); " +
            "C_CraftingOrders.ReleaseOrder(104,3); " +
            "C_CraftingOrders.RequestCrafterOrders({" +
            "orderType=2,selectedSkillLineAbility=17,searchFavorites=true," +
            "initialNonPublicSearch=false," +
            "primarySort={sortType=2,reversed=true}," +
            "secondarySort={sortType=0,reversed=false},forCrafter=true," +
            "offset=4,callback=function() end,profession=3}); " +
            "C_CraftingOrders.UpdateIgnoreList(); return ''");

        var state = session.Lua.CraftingOrders;
        Assert.Equal(
            new WowCraftingOrderActionState(101, 14),
            state.LastClaimedOrder);
        Assert.Equal(
            new WowCraftingOrderNoteActionState(102, "done", 0),
            state.LastFulfilledOrder);
        Assert.Equal(
            new WowCraftingOrderNoteActionState(103, "declined", 7),
            state.LastRejectedOrder);
        Assert.Equal(
            new WowCraftingOrderActionState(104, 3),
            state.LastReleasedOrder);
        Assert.Equal(17U, state.LastCrafterOrdersRequest?.SelectedSkillLineAbility);
        Assert.True(state.LastCrafterOrdersRequest?.ForCrafter);
        Assert.Equal((byte)3, state.LastCrafterOrdersRequest?.Profession);
        Assert.Equal(1, state.UpdateIgnoreListCount);
    }

    [Fact]
    public void ParsesPlacementArraysAndRecoveredOptionalFields()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:false:false:false",
            session.Lua.Evaluate(
                "local params={categoryFilters={},minLevel=0,maxLevel=0," +
                "uncollectedOnly=false,usableOnly=false,upgradesOnly=false," +
                "currentExpansionOnly=false,includePoor=false," +
                "includeCommon=false,includeUncommon=false,includeRare=false," +
                "includeEpic=false,includeLegendary=false," +
                "includeArtifact=false,isFavoritesSearch=false}; " +
                "local options=pcall(C_CraftingOrders.GetCustomerOptions,params); " +
                "local placed=pcall(C_CraftingOrders.PlaceNewOrder,{" +
                "skillLineAbilityID=17,orderType=3,orderDuration=2," +
                "tipAmount=500,customerNotes='notes'," +
                "reagentInfos={{reagent={itemID=11},quantity=2}}," +
                "craftingReagentItems={{dataSlotIndex=1," +
                "reagent={currencyID=22}}}}); " +
                "local badSlot=pcall(C_CraftingOrders.PlaceNewOrder,{" +
                "skillLineAbilityID=17,orderType=3,orderDuration=2," +
                "tipAmount=500,customerNotes='',reagentInfos={}," +
                "craftingReagentItems={{dataSlotIndex=0,reagent={}}}}); " +
                "local badProfession=pcall(C_CraftingOrders.RequestCrafterOrders,{" +
                "orderType=2,searchFavorites=false," +
                "initialNonPublicSearch=false," +
                "primarySort={sortType=0,reversed=false}," +
                "secondarySort={sortType=0,reversed=false},forCrafter=true," +
                "offset=0,callback=function() end,profession=15}); " +
                "local badReagent=pcall(C_CraftingOrders.PlaceNewOrder,{" +
                "skillLineAbilityID=17,orderType=3,orderDuration=2," +
                "tipAmount=500,customerNotes='',reagentInfos={1}," +
                "craftingReagentItems={}}); " +
                "return table.concat({tostring(options),tostring(placed)," +
                "tostring(badSlot),tostring(badProfession)," +
                "tostring(badReagent)},':')"));

        var placement = session.Lua.CraftingOrders.LastPlacedOrder;
        Assert.Null(placement?.OrderTarget);
        Assert.Null(placement?.RecraftItem);
        Assert.Equal(11U, placement?.ReagentInfos?[0].Reagent.ItemId);
        Assert.Equal(2, placement?.ReagentInfos?[0].Quantity);
        Assert.Equal(1, placement?.CraftingReagentItems?[0].DataSlotIndex);
        Assert.Equal(22U, placement?.CraftingReagentItems?[0].Reagent.CurrencyId);
    }

    [Fact]
    public void CloseCrafterClearsNativeOwnedResultState()
    {
        using var session = new EmulatorSession();
        var state = session.Lua.CraftingOrders;
        state.CrafterBuckets.Add(
            new WowCraftingOrderBucketInfoState(1, 2, 3, 4, 5, 6));
        state.CrafterOrders.Add(CreateOrder(7));
        state.ClaimedOrder = CreateOrder(8);

        session.Lua.Evaluate(
            "C_CraftingOrders.OpenCrafterCraftingOrders(); return ''");
        Assert.True(state.IsCrafterCraftingOrdersOpen);

        Assert.Equal(
            "0:0:nil",
            session.Lua.Evaluate(
                "C_CraftingOrders.CloseCrafterCraftingOrders(); " +
                "return table.concat({" +
                "#C_CraftingOrders.GetCrafterBuckets()," +
                "#C_CraftingOrders.GetCrafterOrders()," +
                "tostring(C_CraftingOrders.GetClaimedOrder())},':')"));
        Assert.False(state.IsCrafterCraftingOrdersOpen);
    }

    [Fact]
    public void RejectsMissingArgumentsAndProfessionOutsideNativeRange()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_CraftingOrders.CanOrderSkillAbility))," +
                "tostring(pcall(C_CraftingOrders.ClaimOrder,1,15))," +
                "tostring(pcall(C_CraftingOrders.FulfillOrder,1,nil,0))," +
                "tostring(pcall(C_CraftingOrders.GetOrderClaimInfo,-1))," +
                "tostring(pcall(C_CraftingOrders.OrderCanBeRecrafted))," +
                "tostring(pcall(C_CraftingOrders.ReleaseOrder,1,15))," +
                "tostring(pcall(C_CraftingOrders.SkillLineHasOrders))},':')"));
    }

    private static WowCraftingOrderInfoState CreateOrder(ulong orderId) =>
        new(
            orderId,
            ItemId: 1,
            SpellId: 2,
            SkillLineAbilityId: 3,
            OrderType: 0,
            OrderState: 0,
            ExpirationTime: 0,
            ClaimEndTime: 0,
            MinimumQuality: 0,
            TipAmount: 0,
            ConsortiumCut: 0,
            IsRecraft: false,
            IsFulfillable: true,
            ReagentState: 0);
}
