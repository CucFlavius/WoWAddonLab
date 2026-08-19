using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCraftingOrdersApi : LuaApiModule
{
    private const int MaximumFavoriteCustomerOptions = 100;
    private const ulong MaximumPostingFee = 99_999_999_999;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
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

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_CraftingOrders");
    }

    private static int Dispatch(lua_State state)
    {
        var craftingOrders = LuaBindings.GetRuntime(state).CraftingOrders;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AreOrderNotesDisabled":
                return PushBoolean(state, craftingOrders.AreOrderNotesDisabled);
            case "CalculateCraftingOrderPostingFee":
                return CalculateCraftingOrderPostingFee(state, craftingOrders);
            case "CanOrderSkillAbility":
            {
                var skillLineAbilityId = RequiredInt32(
                    state,
                    1,
                    "Usage: local canOrder = " +
                    "C_CraftingOrders.CanOrderSkillAbility(skillLineAbilityID)");
                return PushBoolean(
                    state,
                    craftingOrders.OrderableSkillLineAbilityIds.Contains(
                        skillLineAbilityId));
            }
            case "CancelOrder":
                craftingOrders.LastCancelledOrderId = RequiredUInt64(
                    state,
                    1,
                    "Usage: C_CraftingOrders.CancelOrder(orderID)");
                return 0;
            case "ClaimOrder":
                craftingOrders.LastClaimedOrder = ParseOrderAction(
                    state,
                    "ClaimOrder");
                return 0;
            case "CloseCrafterCraftingOrders":
                CloseCrafterCraftingOrders(craftingOrders);
                return 0;
            case "CloseCustomerCraftingOrders":
                CloseCustomerCraftingOrders(craftingOrders);
                return 0;
            case "FulfillOrder":
                craftingOrders.LastFulfilledOrder = ParseOrderNoteAction(
                    state,
                    "FulfillOrder");
                return 0;
            case "GetClaimedOrder":
                PushOptionalOrder(state, craftingOrders.ClaimedOrder);
                return 1;
            case "GetCrafterBuckets":
                PushCrafterBuckets(state, craftingOrders.CrafterBuckets);
                return 1;
            case "GetCrafterOrders":
                PushOrderArray(state, craftingOrders.CrafterOrders);
                return 1;
            case "GetCraftingOrderTime":
                lua_pushnumber(state, craftingOrders.CraftingOrderTime);
                return 1;
            case "GetCustomerCategories":
                PushCustomerCategories(state, craftingOrders.CustomerCategories);
                return 1;
            case "GetCustomerOptions":
                ValidateCustomerOptionsParameters(state);
                PushCustomerOptionsResult(state, craftingOrders);
                return 1;
            case "GetCustomerOrders":
                PushOrderArray(state, craftingOrders.CustomerOrders);
                return 1;
            case "GetDefaultOrdersSkillLine":
                PushOptionalNumber(state, craftingOrders.DefaultOrdersSkillLine);
                return 1;
            case "GetMyOrders":
                PushOrderArray(state, craftingOrders.MyOrders);
                return 1;
            case "GetNumFavoriteCustomerOptions":
                lua_pushinteger(
                    state,
                    craftingOrders.FavoriteCustomerOptionRecipeIds.Count);
                return 1;
            case "GetOrderClaimInfo":
                return GetOrderClaimInfo(state, craftingOrders);
            case "GetPersonalOrdersInfo":
                PushPersonalOrders(state, craftingOrders.PersonalOrders);
                return 1;
            case "HasFavoriteCustomerOptions":
                return PushBoolean(
                    state,
                    craftingOrders.FavoriteCustomerOptionRecipeIds.Count != 0);
            case "IsCustomerOptionFavorited":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local favorited = " +
                    "C_CraftingOrders.IsCustomerOptionFavorited(recipeID)");
                return PushBoolean(
                    state,
                    craftingOrders.FavoriteCustomerOptionRecipeIds.Contains(recipeId));
            }
            case "ListMyOrders":
                craftingOrders.LastListMyOrdersRequest = ParseListMyOrdersRequest(state);
                return 0;
            case "OpenCrafterCraftingOrders":
                craftingOrders.IsCrafterCraftingOrdersOpen = true;
                return 0;
            case "OpenCustomerCraftingOrders":
                craftingOrders.IsCustomerCraftingOrdersOpen = true;
                return 0;
            case "OrderCanBeRecrafted":
            {
                var orderId = RequiredUInt64(
                    state,
                    1,
                    "Usage: local recraftable = " +
                    "C_CraftingOrders.OrderCanBeRecrafted(orderID)");
                return PushBoolean(
                    state,
                    craftingOrders.RecraftableOrderIds.Contains(orderId));
            }
            case "ParseCustomerOptions":
                craftingOrders.ParseCustomerOptionsCount++;
                return 0;
            case "PlaceNewOrder":
                craftingOrders.LastPlacedOrder = ParsePlacement(state);
                return 0;
            case "RejectOrder":
                craftingOrders.LastRejectedOrder = ParseOrderNoteAction(
                    state,
                    "RejectOrder");
                return 0;
            case "ReleaseOrder":
                craftingOrders.LastReleasedOrder = ParseOrderAction(
                    state,
                    "ReleaseOrder");
                return 0;
            case "RequestCrafterOrders":
                craftingOrders.LastCrafterOrdersRequest =
                    ParseCustomerOrdersRequest(state, "RequestCrafterOrders");
                return 0;
            case "RequestCustomerOrders":
                craftingOrders.LastCustomerOrdersRequest =
                    ParseCustomerOrdersRequest(state, "RequestCustomerOrders");
                return 0;
            case "SetCustomerOptionFavorited":
                SetCustomerOptionFavorited(state, craftingOrders);
                return 0;
            case "ShouldShowCraftingOrderTab":
                return PushBoolean(state, craftingOrders.ShouldShowCraftingOrderTab);
            case "SkillLineHasOrders":
            {
                var skillLineId = RequiredInt32(
                    state,
                    1,
                    "Usage: local hasOrders = " +
                    "C_CraftingOrders.SkillLineHasOrders(skillLineID)");
                return PushBoolean(
                    state,
                    craftingOrders.SkillLinesWithOrders.Contains(skillLineId));
            }
            case "UpdateIgnoreList":
                craftingOrders.UpdateIgnoreListCount++;
                return 0;
            default:
                return 0;
        }
    }

    private static WowCraftingOrderActionState ParseOrderAction(
        lua_State state,
        string operation)
    {
        var usage = $"Usage: C_CraftingOrders.{operation}(orderID, profession)";
        return new WowCraftingOrderActionState(
            RequiredUInt64(state, 1, usage),
            RequiredEnum(state, 2, 14, usage));
    }

    private static WowCraftingOrderNoteActionState ParseOrderNoteAction(
        lua_State state,
        string operation)
    {
        var usage =
            $"Usage: C_CraftingOrders.{operation}(orderID, crafterNote, profession)";
        return new WowCraftingOrderNoteActionState(
            RequiredUInt64(state, 1, usage),
            RequiredString(state, 2, usage),
            RequiredEnum(state, 3, 14, usage));
    }

    private static int GetOrderClaimInfo(
        lua_State state,
        WowCraftingOrdersState craftingOrders)
    {
        const string usage =
            "Usage: local claimInfo = " +
            "C_CraftingOrders.GetOrderClaimInfo(profession)";
        var profession = RequiredEnum(state, 1, 14, usage);
        var claimInfo = craftingOrders.OrderClaimInfo.TryGetValue(
            profession,
            out var value)
            ? value
            : new WowCraftingOrderClaimInfoState(0, null);
        lua_newtable(state);
        SetNumber(state, "claimsRemaining", claimInfo.ClaimsRemaining);
        SetOptionalNumber(
            state,
            "secondsToRecharge",
            claimInfo.SecondsToRecharge);
        return 1;
    }

    private static int CalculateCraftingOrderPostingFee(
        lua_State state,
        WowCraftingOrdersState craftingOrders)
    {
        const string usage =
            "Usage: local deposit = " +
            "C_CraftingOrders.CalculateCraftingOrderPostingFee(" +
            "skillLineAbilityID, orderType, orderDuration)";
        var skillLineAbilityId = RequiredInt32(state, 1, usage);
        var orderType = RequiredEnum(state, 2, 3, usage);
        var orderDuration = RequiredEnum(state, 3, 2, usage);
        var key = (skillLineAbilityId, orderType, orderDuration);
        var postingFee = craftingOrders.PostingFees.TryGetValue(key, out var value)
            ? value
            : craftingOrders.DefaultPostingFee;
        postingFee = Math.Min(postingFee, MaximumPostingFee);
        lua_pushnumber(state, postingFee);
        return 1;
    }

    private static void CloseCustomerCraftingOrders(
        WowCraftingOrdersState craftingOrders)
    {
        craftingOrders.IsCustomerCraftingOrdersOpen = false;
        craftingOrders.CustomerCategories.Clear();
        craftingOrders.CustomerOptions.Clear();
        craftingOrders.CustomerOrders.Clear();
        craftingOrders.MyOrders.Clear();
        craftingOrders.CustomerOptionsExtraColumnType = null;
    }

    private static void CloseCrafterCraftingOrders(
        WowCraftingOrdersState craftingOrders)
    {
        craftingOrders.IsCrafterCraftingOrdersOpen = false;
        craftingOrders.CrafterBuckets.Clear();
        craftingOrders.CrafterOrders.Clear();
        craftingOrders.ClaimedOrder = null;
    }

    private static void ValidateCustomerOptionsParameters(lua_State state)
    {
        const string usage =
            "Usage: local results = " +
            "C_CraftingOrders.GetCustomerOptions(params)";
        RequiredTable(state, 1, usage);
        RequiredTableField(state, 1, "categoryFilters", usage);
        OptionalInt32Field(state, -1, "primaryCategoryID", usage);
        OptionalInt32Field(state, -1, "secondaryCategoryID", usage);
        OptionalInt32Field(state, -1, "tertiaryCategoryID", usage);
        lua_pop(state, 1);

        OptionalStringField(state, 1, "searchText", usage);
        RequiredInt32Field(state, 1, "minLevel", usage);
        RequiredInt32Field(state, 1, "maxLevel", usage);
        RequiredBooleanField(state, 1, "uncollectedOnly", usage);
        RequiredBooleanField(state, 1, "usableOnly", usage);
        RequiredBooleanField(state, 1, "upgradesOnly", usage);
        RequiredBooleanField(state, 1, "currentExpansionOnly", usage);
        RequiredBooleanField(state, 1, "includePoor", usage);
        RequiredBooleanField(state, 1, "includeCommon", usage);
        RequiredBooleanField(state, 1, "includeUncommon", usage);
        RequiredBooleanField(state, 1, "includeRare", usage);
        RequiredBooleanField(state, 1, "includeEpic", usage);
        RequiredBooleanField(state, 1, "includeLegendary", usage);
        RequiredBooleanField(state, 1, "includeArtifact", usage);
        RequiredBooleanField(state, 1, "isFavoritesSearch", usage);
    }

    private static WowCraftingOrderListRequestState ParseListMyOrdersRequest(
        lua_State state)
    {
        const string usage =
            "Usage: C_CraftingOrders.ListMyOrders(request)";
        RequiredTable(state, 1, usage);
        var primarySort = RequiredSortField(state, 1, "primarySort", usage);
        var secondarySort = RequiredSortField(state, 1, "secondarySort", usage);
        var offset = RequiredUInt32Field(state, 1, "offset", usage);
        var hasCallback = RequiredFunctionField(state, 1, "callback", usage);
        return new WowCraftingOrderListRequestState(
            primarySort,
            secondarySort,
            offset,
            hasCallback);
    }

    private static WowCraftingOrderCustomerRequestState ParseCustomerOrdersRequest(
        lua_State state,
        string operation)
    {
        var usage = $"Usage: C_CraftingOrders.{operation}(request)";
        RequiredTable(state, 1, usage);
        var orderType = RequiredEnumField(state, 1, "orderType", 3, usage);
        var selectedSkillLineAbility = OptionalUInt32Field(
            state,
            1,
            "selectedSkillLineAbility",
            usage);
        var searchFavorites = RequiredBooleanField(
            state,
            1,
            "searchFavorites",
            usage);
        var initialNonPublicSearch = RequiredBooleanField(
            state,
            1,
            "initialNonPublicSearch",
            usage);
        var primarySort = RequiredSortField(state, 1, "primarySort", usage);
        var secondarySort = RequiredSortField(state, 1, "secondarySort", usage);
        var forCrafter = RequiredBooleanField(
            state,
            1,
            "forCrafter",
            usage);
        var offset = RequiredUInt32Field(state, 1, "offset", usage);
        var hasCallback = RequiredFunctionField(state, 1, "callback", usage);
        var profession = OptionalEnumField(
            state,
            1,
            "profession",
            14,
            usage);
        return new WowCraftingOrderCustomerRequestState(
            orderType,
            selectedSkillLineAbility,
            searchFavorites,
            initialNonPublicSearch,
            primarySort,
            secondarySort,
            forCrafter,
            offset,
            hasCallback,
            profession);
    }

    private static WowCraftingOrderPlacementState ParsePlacement(lua_State state)
    {
        const string usage =
            "Usage: C_CraftingOrders.PlaceNewOrder(orderInfo)";
        RequiredTable(state, 1, usage);
        var skillLineAbilityId = RequiredInt32Field(
            state,
            1,
            "skillLineAbilityID",
            usage);
        var orderType = RequiredEnumField(state, 1, "orderType", 3, usage);
        var orderDuration = RequiredEnumField(
            state,
            1,
            "orderDuration",
            2,
            usage);
        var tipAmount = RequiredUInt64Field(state, 1, "tipAmount", usage);
        var customerNotes = RequiredStringField(
            state,
            1,
            "customerNotes",
            usage);
        var reagentInfos = RequiredRegularReagentInfoArrayField(
            state,
            1,
            "reagentInfos",
            usage);
        var craftingReagentItems = RequiredCraftingItemSlotArrayField(
            state,
            1,
            "craftingReagentItems",
            usage);
        var minimumCraftingQualityId = OptionalInt32Field(
            state,
            1,
            "minCraftingQualityID",
            usage);
        var orderTarget = OptionalStringField(
            state,
            1,
            "orderTarget",
            usage);
        var recraftItem = OptionalStringField(
            state,
            1,
            "recraftItem",
            usage);
        return new WowCraftingOrderPlacementState(
            skillLineAbilityId,
            orderType,
            orderDuration,
            tipAmount,
            customerNotes,
            minimumCraftingQualityId,
            orderTarget,
            recraftItem,
            reagentInfos,
            craftingReagentItems);
    }

    private static void SetCustomerOptionFavorited(
        lua_State state,
        WowCraftingOrdersState craftingOrders)
    {
        const string usage =
            "Usage: C_CraftingOrders.SetCustomerOptionFavorited(" +
            "recipeID, favorited)";
        var recipeId = RequiredInt32(state, 1, usage);
        var favorited = RequiredBoolean(state, 2, usage);
        if (!favorited)
        {
            craftingOrders.FavoriteCustomerOptionRecipeIds.Remove(recipeId);
            return;
        }
        if (craftingOrders.FavoriteCustomerOptionRecipeIds.Count <
            MaximumFavoriteCustomerOptions)
        {
            craftingOrders.FavoriteCustomerOptionRecipeIds.Add(recipeId);
        }
    }

    private static void PushCustomerCategories(
        lua_State state,
        IList<WowCraftingOrderCustomerCategoryState> categories)
    {
        lua_newtable(state);
        for (var index = 0; index < categories.Count; index++)
        {
            var category = categories[index];
            lua_newtable(state);
            SetString(state, "categoryName", category.CategoryName);
            SetNumber(state, "categoryID", category.CategoryId);
            SetNumber(state, "uiSortOrder", category.UiSortOrder);
            SetOptionalNumber(
                state,
                "primaryCategorySortOrder",
                category.PrimaryCategorySortOrder);
            SetOptionalNumber(
                state,
                "secondaryCategorySortOrder",
                category.SecondaryCategorySortOrder);
            SetNumber(state, "type", category.Type);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCustomerOptionsResult(
        lua_State state,
        WowCraftingOrdersState craftingOrders)
    {
        lua_newtable(state);
        lua_newtable(state);
        for (var index = 0; index < craftingOrders.CustomerOptions.Count; index++)
        {
            PushCustomerOption(state, craftingOrders.CustomerOptions[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "options");
        SetOptionalNumber(
            state,
            "extraColumnType",
            craftingOrders.CustomerOptionsExtraColumnType);
    }

    private static void PushCustomerOption(
        lua_State state,
        WowCraftingOrderCustomerOptionState option)
    {
        lua_newtable(state);
        SetNumber(state, "skillLineAbilityID", option.SkillLineAbilityId);
        SetNumber(state, "professionID", option.ProfessionId);
        SetNumber(state, "skillUpSkillLineID", option.SkillUpSkillLineId);
        SetNumber(state, "spellID", option.SpellId);
        SetNumber(state, "itemID", option.ItemId);
        SetString(state, "itemName", option.ItemName);
        SetNumber(state, "primaryCategoryID", option.PrimaryCategoryId);
        SetNumber(state, "iLvlMin", option.ItemLevelMinimum);
        SetOptionalNumber(state, "iLvlMax", option.ItemLevelMaximum);
        SetBoolean(state, "canUse", option.CanUse);
        SetBoolean(state, "bindOnPickup", option.BindOnPickup);
        SetOptionalIntArray(
            state,
            "qualityIlvlBonuses",
            option.QualityItemLevelBonuses);
        SetOptionalIntArray(
            state,
            "craftingQualityIDs",
            option.CraftingQualityIds);
        SetOptionalNumber(state, "quality", option.Quality);
        SetOptionalNumber(state, "slots", option.Slots);
        SetOptionalNumber(state, "level", option.Level);
        SetOptionalNumber(state, "skill", option.Skill);
        SetOptionalNumber(
            state,
            "secondaryCategoryID",
            option.SecondaryCategoryId);
        SetOptionalNumber(
            state,
            "tertiaryCategoryID",
            option.TertiaryCategoryId);
        SetOptionalNumber(state, "expansionID", option.ExpansionId);
    }

    private static void PushOrderArray(
        lua_State state,
        IList<WowCraftingOrderInfoState> orders)
    {
        lua_newtable(state);
        for (var index = 0; index < orders.Count; index++)
        {
            PushOrder(state, orders[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOptionalOrder(
        lua_State state,
        WowCraftingOrderInfoState? order)
    {
        if (order is null)
        {
            lua_pushnil(state);
            return;
        }
        PushOrder(state, order);
    }

    private static void PushCrafterBuckets(
        lua_State state,
        IList<WowCraftingOrderBucketInfoState> buckets)
    {
        lua_newtable(state);
        for (var index = 0; index < buckets.Count; index++)
        {
            var bucket = buckets[index];
            lua_newtable(state);
            SetNumber(state, "itemID", bucket.ItemId);
            SetNumber(state, "spellID", bucket.SpellId);
            SetNumber(
                state,
                "skillLineAbilityID",
                bucket.SkillLineAbilityId);
            SetNumber(state, "tipAmountAvg", bucket.TipAmountAverage);
            SetNumber(state, "tipAmountMax", bucket.TipAmountMaximum);
            SetNumber(state, "numAvailable", bucket.NumberAvailable);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOrder(
        lua_State state,
        WowCraftingOrderInfoState order)
    {
        lua_newtable(state);
        SetNumber(state, "orderID", order.OrderId);
        SetNumber(state, "itemID", order.ItemId);
        SetNumber(state, "spellID", order.SpellId);
        SetNumber(state, "skillLineAbilityID", order.SkillLineAbilityId);
        SetNumber(state, "orderType", order.OrderType);
        SetNumber(state, "orderState", order.OrderState);
        SetNumber(state, "expirationTime", order.ExpirationTime);
        SetNumber(state, "claimEndTime", order.ClaimEndTime);
        SetNumber(state, "minQuality", order.MinimumQuality);
        SetNumber(state, "tipAmount", order.TipAmount);
        SetNumber(state, "consortiumCut", order.ConsortiumCut);
        SetBoolean(state, "isRecraft", order.IsRecraft);
        SetBoolean(state, "isFulfillable", order.IsFulfillable);
        SetNumber(state, "reagentState", order.ReagentState);
        SetOptionalString(state, "customerGuid", order.CustomerGuid);
        SetOptionalString(state, "customerName", order.CustomerName);
        SetOptionalString(state, "crafterGuid", order.CrafterGuid);
        SetOptionalString(state, "crafterName", order.CrafterName);
        SetOptionalNumber(
            state,
            "npcCustomerCreatureID",
            order.NpcCustomerCreatureId);
        SetString(state, "customerNotes", order.CustomerNotes);
        PushOrderReagents(state, order.Reagents ?? []);
        lua_setfield(state, -2, "reagents");
        SetOptionalString(
            state,
            "outputItemHyperlink",
            order.OutputItemHyperlink);
        SetOptionalString(state, "outputItemGUID", order.OutputItemGuid);
        SetOptionalString(
            state,
            "recraftItemHyperlink",
            order.RecraftItemHyperlink);
        PushNpcOrderRewards(state, order.NpcOrderRewards ?? []);
        lua_setfield(state, -2, "npcOrderRewards");
        SetNumber(
            state,
            "npcCraftingOrderSetID",
            order.NpcCraftingOrderSetId);
        SetNumber(state, "npcTreasureID", order.NpcTreasureId);
    }

    private static void PushOrderReagents(
        lua_State state,
        IReadOnlyList<WowCraftingOrderReagentState> reagents)
    {
        lua_newtable(state);
        for (var index = 0; index < reagents.Count; index++)
        {
            var reagent = reagents[index];
            lua_newtable(state);
            lua_newtable(state);
            PushCraftingReagentInfo(state, reagent.ReagentInfo.Reagent);
            lua_setfield(state, -2, "reagent");
            SetNumber(
                state,
                "dataSlotIndex",
                reagent.ReagentInfo.DataSlotIndex);
            SetNumber(state, "quantity", reagent.ReagentInfo.Quantity);
            lua_setfield(state, -2, "reagentInfo");
            SetNumber(state, "slotIndex", reagent.SlotIndex);
            SetNumber(state, "source", reagent.Source);
            SetBoolean(state, "isBasicReagent", reagent.IsBasicReagent);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushNpcOrderRewards(
        lua_State state,
        IReadOnlyList<WowCraftingOrderNpcRewardState> rewards)
    {
        lua_newtable(state);
        for (var index = 0; index < rewards.Count; index++)
        {
            var reward = rewards[index];
            lua_newtable(state);
            SetOptionalString(state, "itemLink", reward.ItemLink);
            SetOptionalNumber(state, "currencyType", reward.CurrencyType);
            SetNumber(state, "count", reward.Count);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCraftingReagentInfo(
        lua_State state,
        WowCraftingReagentInfo reagent)
    {
        lua_newtable(state);
        SetOptionalUInt32(state, "itemID", reagent.ItemId);
        SetOptionalUInt32(state, "currencyID", reagent.CurrencyId);
    }

    private static void PushPersonalOrders(
        lua_State state,
        IList<WowPersonalCraftingOrderInfoState> orders)
    {
        lua_newtable(state);
        for (var index = 0; index < orders.Count; index++)
        {
            var order = orders[index];
            lua_newtable(state);
            SetNumber(state, "profession", order.Profession);
            SetNumber(state, "numPersonalOrders", order.NumberOfPersonalOrders);
            SetOptionalString(state, "professionName", order.ProfessionName);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static WowCraftingOrderSortState RequiredSortField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        RequiredTableField(state, tableIndex, field, usage);
        var sortType = RequiredEnumField(state, -1, "sortType", 7, usage);
        var reversed = RequiredBooleanField(state, -1, "reversed", usage);
        lua_pop(state, 1);
        return new WowCraftingOrderSortState(sortType, reversed);
    }

    private static void RequiredTable(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TTABLE)
            luaL_error(state, usage);
    }

    private static void RequiredTableField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_type(state, -1) == LUA_TTABLE)
            return;
        lua_pop(state, 1);
        luaL_error(state, usage);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
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

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < 0 or > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)number;
    }

    private static ulong RequiredUInt64(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < 0 ||
            number > ulong.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (ulong)number;
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) == LUA_TNIL)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static byte RequiredEnum(
        lua_State state,
        int index,
        byte maximum,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < 0 || value > maximum)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (byte)value;
    }

    private static int RequiredInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static uint RequiredUInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredUInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static ulong RequiredUInt64Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredUInt64(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static byte RequiredEnumField(
        lua_State state,
        int tableIndex,
        string field,
        byte maximum,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredEnum(state, -1, maximum, usage);
        lua_pop(state, 1);
        return value;
    }

    private static byte? OptionalEnumField(
        lua_State state,
        int tableIndex,
        string field,
        byte maximum,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_type(state, -1) == LUA_TNIL)
        {
            lua_pop(state, 1);
            return null;
        }
        var value = RequiredEnum(state, -1, maximum, usage);
        lua_pop(state, 1);
        return value;
    }

    private static bool RequiredBooleanField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredBoolean(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static string RequiredStringField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_isstring(state, -1) == 0)
        {
            lua_pop(state, 1);
            luaL_error(state, usage);
            return string.Empty;
        }
        var value = lua_tostring(state, -1) ?? string.Empty;
        lua_pop(state, 1);
        return value;
    }

    private static bool RequiredFunctionField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_isfunction(state, -1) == 0)
        {
            lua_pop(state, 1);
            luaL_error(state, usage);
            return false;
        }
        lua_pop(state, 1);
        return true;
    }

    private static IReadOnlyList<WowCraftingReagentQuantity>
        RequiredRegularReagentInfoArrayField(
            lua_State state,
            int tableIndex,
            string field,
            string usage)
    {
        RequiredTableField(state, tableIndex, field, usage);
        var count = checked((int)lua_objlen(state, -1));
        var result = new List<WowCraftingReagentQuantity>(count);
        for (var index = 1; index <= count; index++)
        {
            lua_rawgeti(state, -1, index);
            RequiredTable(state, -1, usage);
            var absoluteIndex = lua_gettop(state);
            lua_getfield(state, absoluteIndex, "reagent");
            var reagent = RequiredCraftingReagentInfo(state, -1, usage);
            lua_pop(state, 1);
            var quantity = RequiredInt32Field(
                state,
                absoluteIndex,
                "quantity",
                usage);
            result.Add(new WowCraftingReagentQuantity(reagent, quantity));
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
        return result;
    }

    private static IReadOnlyList<WowCraftingItemSlotModification>
        RequiredCraftingItemSlotArrayField(
            lua_State state,
            int tableIndex,
            string field,
            string usage)
    {
        RequiredTableField(state, tableIndex, field, usage);
        var count = checked((int)lua_objlen(state, -1));
        var result = new List<WowCraftingItemSlotModification>(count);
        for (var index = 1; index <= count; index++)
        {
            lua_rawgeti(state, -1, index);
            RequiredTable(state, -1, usage);
            var absoluteIndex = lua_gettop(state);
            var dataSlotIndex = RequiredPositiveInt32Field(
                state,
                absoluteIndex,
                "dataSlotIndex",
                usage);
            lua_getfield(state, absoluteIndex, "reagent");
            var reagent = RequiredCraftingReagentInfo(state, -1, usage);
            lua_pop(state, 1);
            result.Add(
                new WowCraftingItemSlotModification(dataSlotIndex, reagent));
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
        return result;
    }

    private static WowCraftingReagentInfo RequiredCraftingReagentInfo(
        lua_State state,
        int index,
        string usage)
    {
        RequiredTable(state, index, usage);
        var absoluteIndex = index > 0
            ? index
            : lua_gettop(state) + index + 1;
        return new WowCraftingReagentInfo(
            OptionalUInt32Field(state, absoluteIndex, "itemID", usage),
            OptionalUInt32Field(state, absoluteIndex, "currencyID", usage));
    }

    private static int RequiredPositiveInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        var value = RequiredInt32Field(state, tableIndex, field, usage);
        if (value <= 0)
            luaL_error(state, usage);
        return value;
    }

    private static int? OptionalInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_type(state, -1) == LUA_TNIL)
        {
            lua_pop(state, 1);
            return null;
        }
        var value = RequiredInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static uint? OptionalUInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_type(state, -1) == LUA_TNIL)
        {
            lua_pop(state, 1);
            return null;
        }
        var value = RequiredUInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static string? OptionalStringField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_type(state, -1) == LUA_TNIL)
        {
            lua_pop(state, 1);
            return null;
        }
        if (lua_isstring(state, -1) == 0)
        {
            lua_pop(state, 1);
            luaL_error(state, usage);
            return null;
        }
        var value = lua_tostring(state, -1);
        lua_pop(state, 1);
        return value;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(lua_State state, string field, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string field,
        int? value)
    {
        if (value is { } number)
        {
            SetNumber(state, field, number);
            return;
        }
        lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void PushOptionalNumber(lua_State state, int? value)
    {
        if (value is { } number)
        {
            lua_pushnumber(state, number);
            return;
        }
        lua_pushnil(state);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is not null)
        {
            SetString(state, field, value);
            return;
        }
        lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalUInt32(
        lua_State state,
        string field,
        uint? value)
    {
        if (value is { } number)
        {
            SetNumber(state, field, number);
            return;
        }
        lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalIntArray(
        lua_State state,
        string field,
        IReadOnlyList<int>? values)
    {
        if (values is null)
        {
            lua_pushnil(state);
            lua_setfield(state, -2, field);
            return;
        }
        lua_newtable(state);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, field);
    }
}
