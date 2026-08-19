using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTradeSkillUiApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanStoreEnchantInItem",
        "AnyRecipeCategoriesFiltered",
        "AreAnyInventorySlotsFiltered",
        "CancelProfessionRespec",
        "CheckRespecNPC",
        "ClearInventorySlotFilter",
        "ClearRecipeCategoryFilter",
        "ClearRecipeSourceTypeFilter",
        "CloseTradeSkill",
        "ConfirmProfessionRespec",
        "CraftEnchant",
        "CraftRecipe",
        "CraftSalvage",
        "DoesRecraftingRecipeAcceptItem",
        "GetAllProfessionTradeSkillLines",
        "GetAllFilterableInventorySlotsCount",
        "GetBaseProfessionInfo",
        "GetChildProfessionInfo",
        "GetChildProfessionInfos",
        "GetConcentrationCurrencyID",
        "GetCraftableCount",
        "GetCraftingOperationInfo",
        "GetCraftingOperationInfoForOrder",
        "GetCraftingReagentBonusText",
        "GetCraftingTargetItems",
        "GetDependentReagents",
        "GetEnchantItems",
        "GetFactionSpecificOutputItem",
        "GetFilterableInventorySlotName",
        "GetFilteredRecipeIDs",
        "GetGatheringOperationInfo",
        "GetHideUnownedFlags",
        "GetItemCraftedQualityByItemInfo",
        "GetItemCraftedQualityInfo",
        "GetItemReagentQualityByItemInfo",
        "GetItemReagentQualityInfo",
        "GetItemSlotModifications",
        "GetItemSlotModificationsForOrder",
        "GetOriginalCraftRecipeID",
        "GetOnlyShowFirstCraftRecipes",
        "GetOnlyShowMakeableRecipes",
        "GetOnlyShowSkillUpRecipes",
        "GetProfessionByInventorySlot",
        "GetProfessionChildSkillLineID",
        "GetProfessionForCursorItem",
        "GetProfessionInfoByRecipeID",
        "GetProfessionInfoBySkillLineID",
        "GetProfessionInventorySlots",
        "GetProfessionNameForSkillLineAbility",
        "GetProfessionSkillLineID",
        "GetProfessionSlots",
        "GetProfessionSpells",
        "GetQualitiesForRecipe",
        "GetReagentDifficultyText",
        "GetReagentSlotStatus",
        "GetRecipeDescription",
        "GetRecipeInfo",
        "GetRecipeInfoForSkillLineAbility",
        "GetRecipeItemLevelFilter",
        "GetRecipeItemNameFilter",
        "GetRecipeItemQualityInfo",
        "GetRecipeOutputItemData",
        "GetRecipeQualityItemIDs",
        "GetRecipeQualityReagentLink",
        "GetRecipeRequirements",
        "GetRecipeSchematic",
        "GetRecipesTracked",
        "GetRecraftItems",
        "GetRecraftRemovalWarnings",
        "GetRemainingRecasts",
        "GetSalvagableItemIDs",
        "GetShowLearned",
        "GetShowUnlearned",
        "GetSkillLineForGear",
        "GetSourceTypeFilter",
        "GetTradeSkillDisplayName",
        "HasFavoriteOrderRecipes",
        "IsEnchantTargetValid",
        "IsAnyRecipeFromSource",
        "IsGuildTradeSkillsEnabled",
        "IsNPCCrafting",
        "IsNearProfessionSpellFocus",
        "IsOriginalCraftRecipeLearned",
        "IsInventorySlotFiltered",
        "IsRecipeFirstCraft",
        "IsRecipeInBaseSkillLine",
        "IsRecipeInSkillLine",
        "IsRecipeSourceTypeFiltered",
        "IsRecipeProfessionLearned",
        "IsRecipeTracked",
        "IsRecraftItemEquipped",
        "IsRecraftReagentValid",
        "IsRuneforging",
        "IsTradeSkillGuild",
        "IsTradeSkillGuildMember",
        "IsTradeSkillLinked",
        "OpenRecipe",
        "OpenTradeSkill",
        "RecraftLimitCategoryValid",
        "RecraftRecipe",
        "RecraftRecipeForOrder",
        "SetOnlyShowAvailableForOrders",
        "SetOnlyShowFirstCraftRecipes",
        "SetOnlyShowMakeableRecipes",
        "SetOnlyShowSkillUpRecipes",
        "SetProfessionChildSkillLineID",
        "SetInventorySlotFilter",
        "SetRecipeItemLevelFilter",
        "SetRecipeItemNameFilter",
        "SetRecipeSourceTypeFilter",
        "SetRecipeTracked",
        "SetShowLearned",
        "SetShowUnlearned",
        "SetSourceTypeFilter"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_TradeSkillUI");
        LuaBindings.RegisterClosureGlobal(state, "GetTradeSkillTexture", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var tradeSkill = LuaBindings.GetRuntime(state).TradeSkillUi;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "AnyRecipeCategoriesFiltered":
                lua_pushboolean(
                    state,
                    tradeSkill.FilteredRecipeCategories.Count > 0 ? 1 : 0);
                return 1;
            case "AreAnyInventorySlotsFiltered":
                lua_pushboolean(
                    state,
                    tradeSkill.FilteredInventorySlots.Count > 0 ? 1 : 0);
                return 1;
            case "ClearInventorySlotFilter":
                tradeSkill.FilteredInventorySlots.Clear();
                return 0;
            case "ClearRecipeCategoryFilter":
                tradeSkill.FilteredRecipeCategories.Clear();
                return 0;
            case "ClearRecipeSourceTypeFilter":
                tradeSkill.FilteredRecipeSourceTypes.Clear();
                return 0;
            case "GetAllFilterableInventorySlotsCount":
                lua_pushinteger(
                    state,
                    tradeSkill.FilterableInventorySlotNames.Count);
                return 1;
            case "GetFilterableInventorySlotName":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: local name = C_TradeSkillUI." +
                    "GetFilterableInventorySlotName(index)");
                if (index < tradeSkill.FilterableInventorySlotNames.Count)
                    lua_pushstring(
                        state,
                        tradeSkill.FilterableInventorySlotNames[index]);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "GetFilteredRecipeIDs":
                return PushReusableInt32Array(
                    state,
                    tradeSkill.FilteredRecipeIds,
                    "Usage: local recipeIDs = C_TradeSkillUI." +
                    "GetFilteredRecipeIDs([table])");
            case "GetOnlyShowFirstCraftRecipes":
                lua_pushboolean(
                    state,
                    tradeSkill.OnlyShowFirstCraftRecipes ? 1 : 0);
                return 1;
            case "GetOnlyShowMakeableRecipes":
                lua_pushboolean(
                    state,
                    tradeSkill.OnlyShowMakeableRecipes ? 1 : 0);
                return 1;
            case "GetOnlyShowSkillUpRecipes":
                lua_pushboolean(
                    state,
                    tradeSkill.OnlyShowSkillUpRecipes ? 1 : 0);
                return 1;
            case "GetRecipeItemNameFilter":
                lua_pushstring(state, tradeSkill.RecipeItemNameFilter);
                return 1;
            case "GetRecipeItemLevelFilter":
                lua_pushinteger(state, tradeSkill.MinimumRecipeItemLevel);
                lua_pushinteger(state, tradeSkill.MaximumRecipeItemLevel);
                return 2;
            case "IsAnyRecipeFromSource":
            {
                var source = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: local hasRecipes = C_TradeSkillUI." +
                    "IsAnyRecipeFromSource(sourceType)");
                lua_pushboolean(
                    state,
                    tradeSkill.RecipeSourceTypes.Contains(source + 1) ? 1 : 0);
                return 1;
            }
            case "IsInventorySlotFiltered":
            {
                var slot = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: local filtered = C_TradeSkillUI." +
                    "IsInventorySlotFiltered(index)");
                lua_pushboolean(
                    state,
                    tradeSkill.FilteredInventorySlots.Contains(slot + 1) ? 1 : 0);
                return 1;
            }
            case "IsRecipeSourceTypeFiltered":
            {
                var source = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: local filtered = C_TradeSkillUI." +
                    "IsRecipeSourceTypeFiltered(sourceType)");
                lua_pushboolean(
                    state,
                    tradeSkill.FilteredRecipeSourceTypes.Contains(source + 1)
                        ? 1
                        : 0);
                return 1;
            }
            case "SetInventorySlotFilter":
            {
                var slot = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.SetInventorySlotFilter(" +
                    "index, filtered [, exclusive])");
                var filtered = RequiredBoolean(
                    state,
                    2,
                    "Usage: C_TradeSkillUI.SetInventorySlotFilter(" +
                    "index, filtered [, exclusive])");
                if (lua_toboolean(state, 3) != 0)
                    tradeSkill.FilteredInventorySlots.Clear();
                if (filtered)
                    tradeSkill.FilteredInventorySlots.Add(slot + 1);
                else
                    tradeSkill.FilteredInventorySlots.Remove(slot + 1);
                return 0;
            }
            case "SetOnlyShowFirstCraftRecipes":
                tradeSkill.OnlyShowFirstCraftRecipes = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_TradeSkillUI." +
                    "SetOnlyShowFirstCraftRecipes(flag)");
                return 0;
            case "SetOnlyShowMakeableRecipes":
                tradeSkill.OnlyShowMakeableRecipes = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.SetOnlyShowMakeableRecipes(flag)");
                return 0;
            case "SetOnlyShowSkillUpRecipes":
                tradeSkill.OnlyShowSkillUpRecipes = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.SetOnlyShowSkillUpRecipes(flag)");
                return 0;
            case "SetRecipeItemNameFilter":
                tradeSkill.RecipeItemNameFilter =
                    OptionalString(
                        state,
                        1,
                        "Usage: C_TradeSkillUI.SetRecipeItemNameFilter(text)") ??
                    string.Empty;
                return 0;
            case "SetRecipeItemLevelFilter":
                tradeSkill.MinimumRecipeItemLevel = RequiredInt32(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.SetRecipeItemLevelFilter(min, max)");
                tradeSkill.MaximumRecipeItemLevel = RequiredInt32(
                    state,
                    2,
                    "Usage: C_TradeSkillUI.SetRecipeItemLevelFilter(min, max)");
                return 0;
            case "SetRecipeSourceTypeFilter":
            {
                var source = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.SetRecipeSourceTypeFilter(" +
                    "sourceType, filtered)");
                var filtered = RequiredBoolean(
                    state,
                    2,
                    "Usage: C_TradeSkillUI.SetRecipeSourceTypeFilter(" +
                    "sourceType, filtered)");
                if (filtered)
                    tradeSkill.FilteredRecipeSourceTypes.Add(source + 1);
                else
                    tradeSkill.FilteredRecipeSourceTypes.Remove(source + 1);
                return 0;
            }
            case "CanStoreEnchantInItem":
            {
                var guid = RequiredString(
                    state,
                    1,
                    "Usage: local canStore = " +
                    "C_TradeSkillUI.CanStoreEnchantInItem(itemGUID)");
                lua_pushboolean(
                    state,
                    tradeSkill.EnchantStorableItemGuids.Contains(guid)
                        ? 1
                        : 0);
                return 1;
            }
            case "CancelProfessionRespec":
                tradeSkill.HasPendingProfessionRespec = false;
                tradeSkill.CancelProfessionRespecCount++;
                return 0;
            case "CheckRespecNPC":
                lua_pushboolean(
                    state,
                    tradeSkill.CanRespecAtNpc ? 1 : 0);
                return 1;
            case "GetBaseProfessionInfo":
                PushProfessionInfo(state, tradeSkill.BaseProfessionInfo);
                return 1;
            case "GetChildProfessionInfo":
                PushProfessionInfo(state, tradeSkill.ChildProfessionInfo);
                return 1;
            case "GetChildProfessionInfos":
                PushProfessionInfos(state, tradeSkill.ChildProfessionInfos);
                return 1;
            case "ConfirmProfessionRespec":
                tradeSkill.HasPendingProfessionRespec = false;
                tradeSkill.ConfirmProfessionRespecCount++;
                return 0;
            case "CraftEnchant":
            {
                const string usage =
                    "Usage: C_TradeSkillUI.CraftEnchant(" +
                    "recipeSpellID [, numCasts, craftingReagents, " +
                    "itemTarget, applyConcentration])";
                var recipeSpellId = RequiredInt32(state, 1, usage);
                var numCasts = OptionalUInt32(state, 2, 1, usage);
                var reagents = NullableCraftingReagentInfoArray(
                    state,
                    3,
                    usage);
                var itemTarget = OptionalItemLocation(
                    state,
                    4,
                    usage);
                var applyConcentration = OptionalBoolean(
                    state,
                    5,
                    usage);
                tradeSkill.CraftEnchantRequests.Add(
                    new WowCraftEnchantRequest(
                        recipeSpellId,
                        numCasts,
                        reagents,
                        itemTarget,
                        applyConcentration));
                return 0;
            }
            case "CraftRecipe":
            {
                const string usage =
                    "Usage: C_TradeSkillUI.CraftRecipe(" +
                    "recipeSpellID [, numCasts, craftingReagents, " +
                    "recipeLevel, orderID, applyConcentration])";
                var recipeSpellId = RequiredInt32(state, 1, usage);
                var numCasts = OptionalUInt32(state, 2, 1, usage);
                var reagents = NullableCraftingReagentInfoArray(
                    state,
                    3,
                    usage);
                var recipeLevel = OptionalOneBasedIndex(
                    state,
                    4,
                    usage);
                var orderId = OptionalUInt64(state, 5, usage);
                var applyConcentration = OptionalBoolean(
                    state,
                    6,
                    usage);
                tradeSkill.CraftRecipeRequests.Add(
                    new WowCraftRecipeRequest(
                        recipeSpellId,
                        numCasts,
                        reagents,
                        recipeLevel,
                        orderId,
                        applyConcentration));
                return 0;
            }
            case "CraftSalvage":
            {
                const string usage =
                    "Usage: C_TradeSkillUI.CraftSalvage(" +
                    "recipeSpellID [, numCasts], itemTarget " +
                    "[, craftingReagents, applyConcentration])";
                var recipeSpellId = RequiredInt32(state, 1, usage);
                var numCasts = OptionalUInt32(state, 2, 1, usage);
                var itemTarget = WowItemApi.RequiredItemLocation(
                    state,
                    3,
                    usage);
                var reagents = NullableCraftingReagentInfoArray(
                    state,
                    4,
                    usage);
                var applyConcentration = OptionalBoolean(
                    state,
                    5,
                    usage);
                tradeSkill.CraftSalvageRequests.Add(
                    new WowCraftSalvageRequest(
                        recipeSpellId,
                        numCasts,
                        itemTarget,
                        reagents,
                        applyConcentration));
                return 0;
            }
            case "DoesRecraftingRecipeAcceptItem":
            {
                const string usage =
                    "Usage: local result = " +
                    "C_TradeSkillUI.DoesRecraftingRecipeAcceptItem(" +
                    "itemLocation, recipeID)";
                var itemLocation = WowItemApi.RequiredItemLocation(
                    state,
                    1,
                    usage);
                var recipeId = RequiredInt32(state, 2, usage);
                var accepts =
                    tradeSkill.RecraftRecipeIdsByItemLocation.TryGetValue(
                        itemLocation,
                        out var itemRecipeId) &&
                    itemRecipeId == recipeId;
                lua_pushboolean(state, accepts ? 1 : 0);
                return 1;
            }
            case "GetAllProfessionTradeSkillLines":
                PushInt32Array(
                    state,
                    tradeSkill.ProfessionTradeSkillLineIds);
                return 1;
            case "GetConcentrationCurrencyID":
            {
                var skillLineId = RequiredInt32(
                    state,
                    1,
                    "Usage: local currencyType = " +
                    "C_TradeSkillUI.GetConcentrationCurrencyID(" +
                    "skillLineID)");
                tradeSkill.ConcentrationCurrencyIds.TryGetValue(
                    skillLineId,
                    out var currencyId);
                lua_pushinteger(state, currencyId);
                return 1;
            }
            case "GetCraftableCount":
            {
                const string usage =
                    "Usage: local numAvailable = " +
                    "C_TradeSkillUI.GetCraftableCount(" +
                    "recipeSpellID [, recipeLevel])";
                var recipeSpellId = RequiredInt32(state, 1, usage);
                var recipeLevel = OptionalOneBasedIndex(
                    state,
                    2,
                    usage);
                tradeSkill.CraftableCounts.TryGetValue(
                    (recipeSpellId, recipeLevel),
                    out var craftableCount);
                lua_pushinteger(state, craftableCount);
                return 1;
            }
            case "GetFactionSpecificOutputItem":
            {
                var recipeSpellId = RequiredInt32(
                    state,
                    1,
                    "Usage: local itemID = " +
                    "C_TradeSkillUI.GetFactionSpecificOutputItem(" +
                    "recipeSpellID)");
                tradeSkill.FactionSpecificOutputItemIds.TryGetValue(
                    recipeSpellId,
                    out var itemId);
                PushOptionalInteger(state, itemId);
                return 1;
            }
            case "GetCraftingOperationInfo":
            {
                const string usage =
                    "Usage: local info = C_TradeSkillUI." +
                    "GetCraftingOperationInfo(recipeID, " +
                    "craftingReagents [, allocationItemGUID], " +
                    "applyConcentration)";
                var request = new WowCraftingOperationInfoRequest(
                    RequiredInt32(state, 1, usage),
                    RequiredCraftingReagentInfoArray(
                        state,
                        2,
                        usage),
                    OptionalString(state, 3, usage),
                    RequiredBoolean(state, 4, usage));
                var info =
                    tradeSkill.CraftingOperationInfoProvider?.Invoke(
                        request);
                PushCraftingOperationInfo(state, info);
                return 1;
            }
            case "GetCraftingOperationInfoForOrder":
            {
                const string usage =
                    "Usage: local info = C_TradeSkillUI." +
                    "GetCraftingOperationInfoForOrder(recipeID, " +
                    "craftingReagents, orderID, applyConcentration)";
                var request =
                    new WowCraftingOperationInfoForOrderRequest(
                        RequiredInt32(state, 1, usage),
                        RequiredCraftingReagentInfoArray(
                            state,
                            2,
                            usage),
                        RequiredUInt64(state, 3, usage),
                        RequiredBoolean(state, 4, usage));
                var info =
                    tradeSkill.CraftingOperationInfoForOrderProvider
                        ?.Invoke(request);
                PushCraftingOperationInfo(state, info);
                return 1;
            }
            case "GetCraftingReagentBonusText":
            {
                const string usage =
                    "Usage: local bonusText = C_TradeSkillUI." +
                    "GetCraftingReagentBonusText(recipeSpellID, " +
                    "craftingReagentIndex, craftingReagents " +
                    "[, allocationItemGUID])";
                var recipeSpellId = RequiredInt32(state, 1, usage);
                var reagentIndex = RequiredOneBasedIndex(
                    state,
                    2,
                    usage);
                var reagents = RequiredCraftingReagentInfoArray(
                    state,
                    3,
                    usage);
                var allocationItemGuid = OptionalString(
                    state,
                    4,
                    usage);
                var text =
                    tradeSkill.CraftingReagentBonusTextProvider?.Invoke(
                        recipeSpellId,
                        reagentIndex,
                        reagents,
                        allocationItemGuid) ??
                    [];
                PushStringArray(state, text);
                return 1;
            }
            case "GetCraftingTargetItems":
            {
                var itemIds = RequiredInt32Array(
                    state,
                    1,
                    "Usage: local items = " +
                    "C_TradeSkillUI.GetCraftingTargetItems(itemIDs)");
                var items =
                    tradeSkill.CraftingTargetItemsProvider?.Invoke(
                        itemIds) ??
                    [];
                PushCraftingTargetItems(state, items);
                return 1;
            }
            case "GetDependentReagents":
            {
                const string usage =
                    "Usage: local reagents = " +
                    "C_TradeSkillUI.GetDependentReagents(reagent)";
                var reagent = RequiredCraftingReagentInfo(
                    state,
                    1,
                    usage);
                var reagents =
                    tradeSkill.DependentReagentsProvider?.Invoke(
                        reagent) ??
                    [];
                PushCraftingReagentInfos(state, reagents);
                return 1;
            }
            case "GetEnchantItems":
            {
                const string usage =
                    "Usage: local items = " +
                    "C_TradeSkillUI.GetEnchantItems(" +
                    "recipeID [, craftingReagents])";
                var recipeId = RequiredInt32(state, 1, usage);
                var reagents = NullableCraftingReagentInfoArray(
                    state,
                    2,
                    usage);
                var itemGuids =
                    tradeSkill.EnchantItemsProvider?.Invoke(
                        recipeId,
                        reagents) ??
                    [];
                PushStringArray(state, itemGuids);
                return 1;
            }
            case "GetHideUnownedFlags":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local cannotModifyHideUnowned, " +
                    "alwaysShowUnowned = " +
                    "C_TradeSkillUI.GetHideUnownedFlags(recipeID)");
                tradeSkill.HideUnownedFlags.TryGetValue(
                    recipeId,
                    out var flags);
                lua_pushboolean(state, flags.CannotModify ? 1 : 0);
                lua_pushboolean(state, flags.AlwaysShow ? 1 : 0);
                return 2;
            }
            case "GetItemCraftedQualityByItemInfo":
            {
                const string usage =
                    "Usage: local quality = " +
                    "C_TradeSkillUI.GetItemCraftedQualityByItemInfo(" +
                    "itemInfo)";
                var runtime = LuaBindings.GetRuntime(state);
                var itemId = WowItemApi.RequiredItemId(
                    state,
                    runtime.Items,
                    usage);
                int? quality = null;
                if (itemId.HasValue)
                {
                    tradeSkill.ItemCraftedQualitiesByItemId.TryGetValue(
                        itemId.Value,
                        out quality);
                }
                PushOptionalInteger(state, quality);
                return 1;
            }
            case "GetItemCraftedQualityInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_TradeSkillUI.GetItemCraftedQualityInfo(itemInfo)";
                var runtime = LuaBindings.GetRuntime(state);
                var itemId = WowItemApi.RequiredItemId(
                    state,
                    runtime.Items,
                    usage);
                WowItemReagentQualityInfo? info = null;
                if (itemId.HasValue)
                {
                    tradeSkill.ItemCraftedQualityInfosByItemId.TryGetValue(
                        itemId.Value,
                        out info);
                }
                PushItemReagentQualityInfo(state, info);
                return 1;
            }
            case "GetItemReagentQualityByItemInfo":
            {
                const string usage =
                    "Usage: local quality = " +
                    "C_TradeSkillUI.GetItemReagentQualityByItemInfo(" +
                    "itemInfo)";
                var runtime = LuaBindings.GetRuntime(state);
                var itemId = WowItemApi.RequiredItemId(
                    state,
                    runtime.Items,
                    usage);
                int? quality = null;
                if (itemId.HasValue)
                {
                    tradeSkill.ItemReagentQualitiesByItemId.TryGetValue(
                        itemId.Value,
                        out quality);
                }
                PushOptionalInteger(state, quality);
                return 1;
            }
            case "GetItemReagentQualityInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_TradeSkillUI.GetItemReagentQualityInfo(itemInfo)";
                var runtime = LuaBindings.GetRuntime(state);
                var itemId = WowItemApi.RequiredItemId(
                    state,
                    runtime.Items,
                    usage);
                WowItemReagentQualityInfo? info = null;
                if (itemId.HasValue)
                {
                    tradeSkill.ItemReagentQualityInfosByItemId.TryGetValue(
                        itemId.Value,
                        out info);
                }
                PushItemReagentQualityInfo(state, info);
                return 1;
            }
            case "GetItemSlotModifications":
            {
                var itemGuid = RequiredString(
                    state,
                    1,
                    "Usage: local slotMods = " +
                    "C_TradeSkillUI.GetItemSlotModifications(itemGUID)");
                tradeSkill.ItemSlotModificationsByItemGuid.TryGetValue(
                    itemGuid,
                    out var modifications);
                PushCraftingItemSlotModifications(
                    state,
                    modifications ?? []);
                return 1;
            }
            case "GetItemSlotModificationsForOrder":
            {
                var orderId = RequiredUInt64(
                    state,
                    1,
                    "Usage: local slotMods = " +
                    "C_TradeSkillUI." +
                    "GetItemSlotModificationsForOrder(orderID)");
                tradeSkill.ItemSlotModificationsByOrderId.TryGetValue(
                    orderId,
                    out var modifications);
                PushCraftingItemSlotModifications(
                    state,
                    modifications ?? []);
                return 1;
            }
            case "GetProfessionChildSkillLineID":
                lua_pushinteger(
                    state,
                    tradeSkill.ProfessionChildSkillLineId);
                return 1;
            case "GetOriginalCraftRecipeID":
            {
                var itemGuid = RequiredString(
                    state,
                    1,
                    "Usage: local recipeID, skillLineAbilityID = " +
                    "C_TradeSkillUI.GetOriginalCraftRecipeID(itemGUID)");
                tradeSkill.OriginalCraftRecipeIdsByItemGuid.TryGetValue(
                    itemGuid,
                    out var values);
                PushOptionalInteger(state, values.RecipeId);
                PushOptionalInteger(state, values.SkillLineAbilityId);
                return 2;
            }
            case "GetProfessionByInventorySlot":
            {
                var slotIndex = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: local profession = " +
                    "C_TradeSkillUI.GetProfessionByInventorySlot(slot)");
                tradeSkill.ProfessionsByInventorySlotIndex.TryGetValue(
                    slotIndex,
                    out var profession);
                PushOptionalInteger(state, profession);
                return 1;
            }
            case "GetProfessionForCursorItem":
                PushOptionalInteger(
                    state,
                    tradeSkill.ProfessionForCursorItem);
                return 1;
            case "GetProfessionInfoByRecipeID":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local info = " +
                    "C_TradeSkillUI.GetProfessionInfoByRecipeID(" +
                    "recipeID)");
                tradeSkill.ProfessionInfosByRecipeId.TryGetValue(
                    recipeId,
                    out var info);
                PushProfessionInfo(
                    state,
                    info ?? new WowProfessionInfo());
                return 1;
            }
            case "GetProfessionInfoBySkillLineID":
            {
                var skillLineId = RequiredInt32(
                    state,
                    1,
                    "Usage: local info = " +
                    "C_TradeSkillUI.GetProfessionInfoBySkillLineID(" +
                    "skillLineID)");
                tradeSkill.ProfessionInfosBySkillLineId.TryGetValue(
                    skillLineId,
                    out var info);
                PushProfessionInfo(
                    state,
                    info ?? new WowProfessionInfo());
                return 1;
            }
            case "GetProfessionInventorySlots":
                PushUInt32Array(
                    state,
                    tradeSkill.ProfessionInventorySlots);
                return 1;
            case "GetProfessionNameForSkillLineAbility":
            {
                var skillLineAbilityId = RequiredInt32(
                    state,
                    1,
                    "Usage: local professionNmae = " +
                    "C_TradeSkillUI." +
                    "GetProfessionNameForSkillLineAbility(" +
                    "skillLineAbilityID)");
                tradeSkill.ProfessionNamesBySkillLineAbilityId.TryGetValue(
                    skillLineAbilityId,
                    out var professionName);
                if (professionName is null)
                {
                    lua_pushnil(state);
                }
                else
                {
                    lua_pushstring(state, professionName);
                }
                return 1;
            }
            case "GetProfessionSkillLineID":
                RequiredInt32Enum(
                    state,
                    1,
                    0,
                    14,
                    "Usage: local skillLineID = " +
                    "C_TradeSkillUI.GetProfessionSkillLineID(profession)");
                lua_pushinteger(state, tradeSkill.ProfessionSkillLineId);
                return 1;
            case "GetProfessionSlots":
            {
                var profession = RequiredInt32Enum(
                    state,
                    1,
                    0,
                    14,
                    "Usage: local slots = " +
                    "C_TradeSkillUI.GetProfessionSlots(profession)");
                tradeSkill.ProfessionSlots.TryGetValue(
                    profession,
                    out var slots);
                PushUInt32Array(state, slots ?? []);
                return 1;
            }
            case "GetProfessionSpells":
            {
                const string usage =
                    "Usage: local knownSpells = " +
                    "C_TradeSkillUI.GetProfessionSpells(" +
                    "professionID [, skillLineID])";
                var professionId = RequiredInt32(state, 1, usage);
                var skillLineId = OptionalInt32(state, 2, usage);
                tradeSkill.ProfessionSpells.TryGetValue(
                    (professionId, skillLineId),
                    out var spells);
                PushInt32Array(state, spells ?? []);
                return 1;
            }
            case "GetQualitiesForRecipe":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local qualityIDs = " +
                    "C_TradeSkillUI.GetQualitiesForRecipe(recipeID)");
                tradeSkill.QualityIdsByRecipeId.TryGetValue(
                    recipeId,
                    out var qualityIds);
                if (qualityIds is null)
                {
                    lua_pushnil(state);
                }
                else
                {
                    PushInt32Array(state, qualityIds);
                }
                return 1;
            }
            case "GetReagentDifficultyText":
            {
                const string usage =
                    "Usage: local bonusText = " +
                    "C_TradeSkillUI.GetReagentDifficultyText(" +
                    "craftingReagentIndex, craftingReagents)";
                var reagentIndex = RequiredOneBasedIndex(
                    state,
                    1,
                    usage);
                var reagents = RequiredCraftingReagentInfoArray(
                    state,
                    2,
                    usage);
                var text =
                    tradeSkill.ReagentDifficultyTextProvider?.Invoke(
                        reagentIndex,
                        reagents) ??
                    string.Empty;
                lua_pushstring(state, text);
                return 1;
            }
            case "GetReagentSlotStatus":
            {
                const string usage =
                    "Usage: local locked, lockedReason = " +
                    "C_TradeSkillUI.GetReagentSlotStatus(" +
                    "mcrSlotID, recipeSpellID, skillLineAbilityID)";
                var mcrSlotId = RequiredInt32(state, 1, usage);
                var recipeSpellId = RequiredInt32(state, 2, usage);
                var skillLineAbilityId =
                    RequiredInt32(state, 3, usage);
                tradeSkill.ReagentSlotStatuses.TryGetValue(
                    (
                        mcrSlotId,
                        recipeSpellId,
                        skillLineAbilityId
                    ),
                    out var status);
                lua_pushboolean(state, status.Locked ? 1 : 0);
                lua_pushstring(
                    state,
                    status.LockedReason ?? string.Empty);
                return 2;
            }
            case "GetRecipeDescription":
            {
                const string usage =
                    "Usage: local description = " +
                    "C_TradeSkillUI.GetRecipeDescription(" +
                    "recipeID, craftingReagents " +
                    "[, allocationItemGUID])";
                var recipeId = RequiredInt32(state, 1, usage);
                var reagents = RequiredCraftingReagentInfoArray(
                    state,
                    2,
                    usage);
                var allocationItemGuid = OptionalString(
                    state,
                    3,
                    usage);
                var description =
                    tradeSkill.RecipeDescriptionProvider?.Invoke(
                        recipeId,
                        reagents,
                        allocationItemGuid) ??
                    string.Empty;
                lua_pushstring(state, description);
                return 1;
            }
            case "GetGatheringOperationInfo":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local info = " +
                    "C_TradeSkillUI.GetGatheringOperationInfo(recipeID)");
                tradeSkill.GatheringOperationInfosByRecipeId.TryGetValue(
                    recipeId,
                    out var info);
                PushGatheringOperationInfo(state, info);
                return 1;
            }
            case "GetRecipeInfo":
            {
                const string usage =
                    "Usage: local recipeInfo = " +
                    "C_TradeSkillUI.GetRecipeInfo(" +
                    "recipeSpellID [, recipeLevel])";
                var recipeSpellId = RequiredInt32(state, 1, usage);
                var recipeLevelIndex = OptionalOneBasedIndex(
                    state,
                    2,
                    usage);
                tradeSkill.RecipeInfos.TryGetValue(
                    (recipeSpellId, recipeLevelIndex),
                    out var info);
                PushTradeSkillRecipeInfo(state, info);
                return 1;
            }
            case "GetRecipeInfoForSkillLineAbility":
            {
                const string usage =
                    "Usage: local recipeInfo = C_TradeSkillUI." +
                    "GetRecipeInfoForSkillLineAbility(" +
                    "skillLineAbilityID [, recipeLevel])";
                var skillLineAbilityId =
                    RequiredInt32(state, 1, usage);
                var recipeLevelIndex = OptionalOneBasedIndex(
                    state,
                    2,
                    usage);
                tradeSkill.RecipeInfosBySkillLineAbilityId.TryGetValue(
                    (skillLineAbilityId, recipeLevelIndex),
                    out var info);
                PushTradeSkillRecipeInfo(state, info);
                return 1;
            }
            case "GetRecipeItemQualityInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_TradeSkillUI.GetRecipeItemQualityInfo(" +
                    "recipeID, quality)";
                var recipeId = RequiredInt32(state, 1, usage);
                var quality = RequiredInt32(state, 2, usage);
                tradeSkill.RecipeItemQualityInfos.TryGetValue(
                    (recipeId, quality),
                    out var info);
                PushItemReagentQualityInfo(state, info);
                return 1;
            }
            case "GetRecipeOutputItemData":
            {
                const string usage =
                    "Usage: local outputInfo = " +
                    "C_TradeSkillUI.GetRecipeOutputItemData(" +
                    "recipeSpellID [, reagents, allocationItemGUID, " +
                    "overrideQualityID, recraftOrderID])";
                var request = new WowRecipeOutputItemDataRequest(
                    RequiredInt32(state, 1, usage),
                    NullableCraftingReagentInfoArray(
                        state,
                        2,
                        usage),
                    OptionalString(state, 3, usage),
                    OptionalInt32(state, 4, usage),
                    OptionalUInt64(state, 5, usage));
                var output =
                    tradeSkill.RecipeOutputItemDataProvider?.Invoke(
                        request);
                if (output is null)
                {
                    tradeSkill.RecipeOutputItemDataByRecipeId.TryGetValue(
                        request.RecipeSpellId,
                        out output);
                }
                PushRecipeOutputItemData(
                    state,
                    output ?? new WowRecipeOutputItemData());
                return 1;
            }
            case "GetRecipeRequirements":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local requirements = " +
                    "C_TradeSkillUI.GetRecipeRequirements(recipeID)");
                tradeSkill.RecipeRequirementsByRecipeId.TryGetValue(
                    recipeId,
                    out var requirements);
                PushRecipeRequirements(state, requirements ?? []);
                return 1;
            }
            case "GetRecipeSchematic":
            {
                const string usage =
                    "Usage: local schematic = " +
                    "C_TradeSkillUI.GetRecipeSchematic(" +
                    "recipeSpellID, isRecraft [, recipeLevel])";
                var recipeSpellId = RequiredInt32(state, 1, usage);
                var isRecraft = RequiredBoolean(state, 2, usage);
                var recipeLevelIndex = OptionalOneBasedIndex(
                    state,
                    3,
                    usage);
                tradeSkill.RecipeSchematics.TryGetValue(
                    (recipeSpellId, isRecraft, recipeLevelIndex),
                    out var schematic);
                PushRecipeSchematic(
                    state,
                    schematic ??
                    new WowCraftingRecipeSchematic(
                        RecipeId: recipeSpellId,
                        IsRecraft: isRecraft));
                return 1;
            }
            case "GetRecipeQualityItemIDs":
            {
                var recipeSpellId = RequiredInt32(
                    state,
                    1,
                    "Usage: local qualityItemIDs = " +
                    "C_TradeSkillUI.GetRecipeQualityItemIDs(" +
                    "recipeSpellID)");
                tradeSkill.QualityItemIdsByRecipeId.TryGetValue(
                    recipeSpellId,
                    out var itemIds);
                if (itemIds is null)
                {
                    lua_pushnil(state);
                }
                else
                {
                    PushUInt32Array(state, itemIds);
                }
                return 1;
            }
            case "GetRecipeQualityReagentLink":
            {
                const string usage =
                    "Usage: local link = " +
                    "C_TradeSkillUI.GetRecipeQualityReagentLink(" +
                    "recipeID, dataSlotIndex, qualityIndex)";
                var recipeId = RequiredInt32(state, 1, usage);
                var dataSlotIndex = RequiredOneBasedIndex(
                    state,
                    2,
                    usage);
                var qualityIndex = RequiredOneBasedIndex(
                    state,
                    3,
                    usage);
                tradeSkill.RecipeQualityReagentLinks.TryGetValue(
                    (recipeId, dataSlotIndex, qualityIndex),
                    out var link);
                if (link is null)
                {
                    lua_pushnil(state);
                }
                else
                {
                    lua_pushstring(state, link);
                }
                return 1;
            }
            case "GetRecraftItems":
            {
                const string usage =
                    "Usage: local items = " +
                    "C_TradeSkillUI.GetRecraftItems([recipeID])";
                var recipeId = OptionalInt32(state, 1, usage);
                IEnumerable<string> itemGuids =
                    tradeSkill.RecraftItemGuids;
                if (recipeId.HasValue)
                {
                    tradeSkill.RecraftItemGuidsByRecipeId.TryGetValue(
                        recipeId.Value,
                        out var recipeItems);
                    itemGuids = recipeItems ?? [];
                }
                PushStringArray(state, itemGuids);
                return 1;
            }
            case "GetRecraftRemovalWarnings":
            {
                const string usage =
                    "Usage: local warnings = " +
                    "C_TradeSkillUI.GetRecraftRemovalWarnings(" +
                    "itemGUID, replacedReagents)";
                var itemGuid = RequiredString(state, 1, usage);
                var replacedReagents =
                    RequiredCraftingReagentInfoArray(
                        state,
                        2,
                        usage);
                var warnings =
                    tradeSkill.RecraftRemovalWarningProvider?.Invoke(
                        itemGuid,
                        replacedReagents) ??
                    [];
                PushOptionalStringArray(state, warnings);
                return 1;
            }
            case "GetRemainingRecasts":
                lua_pushinteger(state, Math.Max(0, tradeSkill.RemainingRecasts));
                return 1;
            case "GetSalvagableItemIDs":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local itemIDs = " +
                    "C_TradeSkillUI.GetSalvagableItemIDs(recipeID)");
                tradeSkill.SalvageableItemIdsByRecipeId.TryGetValue(
                    recipeId,
                    out var itemIds);
                PushInt32Array(state, itemIds ?? []);
                return 1;
            }
            case "GetSkillLineForGear":
            {
                const string usage =
                    "Usage: local skillLineID = " +
                    "C_TradeSkillUI.GetSkillLineForGear(itemInfo)";
                var runtime = LuaBindings.GetRuntime(state);
                var itemId = WowItemApi.RequiredItemId(
                    state,
                    runtime.Items,
                    usage);
                int? skillLineId = null;
                if (itemId.HasValue)
                {
                    tradeSkill.SkillLineForGearByItemId.TryGetValue(
                        itemId.Value,
                        out skillLineId);
                }
                PushOptionalInteger(state, skillLineId);
                return 1;
            }
            case "GetTradeSkillDisplayName":
            {
                var skillLineId = RequiredInt32(
                    state,
                    1,
                    "Usage: local professionDisplayName = " +
                    "C_TradeSkillUI.GetTradeSkillDisplayName(" +
                    "skillLineID)");
                tradeSkill.TradeSkillDisplayNames.TryGetValue(
                    skillLineId,
                    out var displayName);
                if (displayName is null)
                {
                    lua_pushnil(state);
                }
                else
                {
                    lua_pushstring(state, displayName);
                }
                return 1;
            }
            case "GetTradeSkillTexture":
            {
                var skillLineId = RequiredInt32(
                    state,
                    1,
                    "Usage: GetTradeSkillTexture(tradeSkillID)");
                tradeSkill.TradeSkillTextureFileIds.TryGetValue(
                    skillLineId,
                    out var textureFileId);
                PushOptionalInteger(state, textureFileId);
                return 1;
            }
            case "HasFavoriteOrderRecipes":
                lua_pushboolean(
                    state,
                    tradeSkill.HasFavoriteOrderRecipes ? 1 : 0);
                return 1;
            case "IsEnchantTargetValid":
            {
                const string usage =
                    "Usage: local valid = " +
                    "C_TradeSkillUI.IsEnchantTargetValid(" +
                    "recipeID, itemGUID [, craftingReagents])";
                var recipeId = RequiredInt32(state, 1, usage);
                var itemGuid = RequiredString(state, 2, usage);
                var reagents = OptionalCraftingReagentInfoArray(
                    state,
                    3,
                    usage);
                var valid = tradeSkill.EnchantTargetValidator?.Invoke(
                    recipeId,
                    itemGuid,
                    reagents) ?? false;
                lua_pushboolean(state, valid ? 1 : 0);
                return 1;
            }
            case "IsNearProfessionSpellFocus":
            {
                var profession = RequiredInt32Enum(
                    state,
                    1,
                    0,
                    14,
                    "Usage: local nearFocus = " +
                    "C_TradeSkillUI.IsNearProfessionSpellFocus(" +
                    "profession)");
                lua_pushboolean(
                    state,
                    tradeSkill.NearProfessionSpellFocusProfessions.Contains(
                        profession)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsOriginalCraftRecipeLearned":
            {
                var itemGuid = RequiredString(
                    state,
                    1,
                    "Usage: local learned = " +
                    "C_TradeSkillUI.IsOriginalCraftRecipeLearned(" +
                    "itemGUID)");
                lua_pushboolean(
                    state,
                    tradeSkill.OriginalCraftRecipeLearnedItemGuids.Contains(
                        itemGuid)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsRecipeFirstCraft":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result = " +
                    "C_TradeSkillUI.IsRecipeFirstCraft(recipeID)");
                lua_pushboolean(
                    state,
                    tradeSkill.FirstCraftRecipeIds.Contains(recipeId)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsRecipeInBaseSkillLine":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local result = " +
                    "C_TradeSkillUI.IsRecipeInBaseSkillLine(recipeID)");
                lua_pushboolean(
                    state,
                    tradeSkill.BaseSkillLineRecipeIds.Contains(recipeId)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsRecipeInSkillLine":
            {
                const string usage =
                    "Usage: local result = " +
                    "C_TradeSkillUI.IsRecipeInSkillLine(" +
                    "recipeID, skillLineID)";
                var recipeId = RequiredInt32(state, 1, usage);
                var skillLineId = RequiredInt32(state, 2, usage);
                lua_pushboolean(
                    state,
                    tradeSkill.SkillLineRecipes.Contains(
                        (recipeId, skillLineId))
                        ? 1
                        : 0);
                return 1;
            }
            case "IsRecipeProfessionLearned":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local recipeProfessionLearned = " +
                    "C_TradeSkillUI.IsRecipeProfessionLearned(recipeID)");
                lua_pushboolean(
                    state,
                    tradeSkill.ProfessionLearnedRecipeIds.Contains(recipeId)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsRecipeTracked":
            {
                const string usage =
                    "Usage: local tracked = " +
                    "C_TradeSkillUI.IsRecipeTracked(recipeID, isRecraft)";
                var recipeId = RequiredInt32(state, 1, usage);
                var isRecraft = RequiredBoolean(state, 2, usage);
                lua_pushboolean(
                    state,
                    (isRecraft
                        ? tradeSkill.TrackedRecraftRecipeIds
                        : tradeSkill.TrackedRecipeIds).Contains(recipeId)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsRecraftItemEquipped":
            {
                var itemGuid = RequiredString(
                    state,
                    1,
                    "Usage: local isEquipped = " +
                    "C_TradeSkillUI.IsRecraftItemEquipped(" +
                    "recraftItemGUID)");
                lua_pushboolean(
                    state,
                    tradeSkill.EquippedRecraftItemGuids.Contains(itemGuid)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsRecraftReagentValid":
            {
                const string usage =
                    "Usage: local valid = " +
                    "C_TradeSkillUI.IsRecraftReagentValid(" +
                    "itemGUID, reagent)";
                var itemGuid = RequiredString(state, 1, usage);
                var reagent = RequiredCraftingReagentInfo(
                    state,
                    2,
                    usage);
                var valid = tradeSkill.RecraftReagentValidator?.Invoke(
                    itemGuid,
                    reagent) ?? true;
                lua_pushboolean(state, valid ? 1 : 0);
                return 1;
            }
            case "IsRuneforging":
                lua_pushboolean(
                    state,
                    tradeSkill.ProfessionSkillLineId == 960 ? 1 : 0);
                return 1;
            case "IsTradeSkillGuild":
                lua_pushboolean(state, tradeSkill.IsTradeSkillGuild ? 1 : 0);
                return 1;
            case "IsTradeSkillGuildMember":
                lua_pushboolean(
                    state,
                    tradeSkill.IsTradeSkillGuildMember ? 1 : 0);
                return 1;
            case "IsTradeSkillLinked":
                lua_pushboolean(state, tradeSkill.IsTradeSkillLinked ? 1 : 0);
                if (tradeSkill.LinkedTradeSkillPlayerName is { } linkedName)
                    lua_pushstring(state, linkedName);
                else
                    lua_pushnil(state);
                return 2;
            case "RecraftLimitCategoryValid":
            {
                const string usage =
                    "Usage: local recraftValid = " +
                    "C_TradeSkillUI.RecraftLimitCategoryValid(reagent)";
                var reagent = RequiredCraftingReagentInfo(
                    state,
                    1,
                    usage);
                var valid =
                    tradeSkill.RecraftLimitCategoryValidator?.Invoke(
                        reagent) ?? false;
                lua_pushboolean(state, valid ? 1 : 0);
                return 1;
            }
            case "OpenRecipe":
            {
                var recipeId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.OpenRecipe(recipeID)");
                tradeSkill.OpenRecipeRequests.Add(recipeId);
                return 0;
            }
            case "OpenTradeSkill":
            {
                var skillLineId = RequiredInt32(
                    state,
                    1,
                    "Usage: local opened = " +
                    "C_TradeSkillUI.OpenTradeSkill(skillLineID)");
                tradeSkill.OpenTradeSkillRequests.Add(skillLineId);
                var opened =
                    tradeSkill.OpenTradeSkillProvider?.Invoke(skillLineId) ??
                    tradeSkill.OpenableTradeSkillLineIds.Contains(
                        skillLineId);
                lua_pushboolean(state, opened ? 1 : 0);
                return 1;
            }
            case "RecraftRecipe":
            {
                const string usage =
                    "Usage: local result = " +
                    "C_TradeSkillUI.RecraftRecipe(" +
                    "itemGUID [, craftingReagents, " +
                    "removedModifications, applyConcentration])";
                var request = new WowRecraftRecipeRequest(
                    RequiredString(state, 1, usage),
                    NullableCraftingReagentInfoArray(
                        state,
                        2,
                        usage),
                    OptionalCraftingItemSlotModificationArray(
                        state,
                        3,
                        usage),
                    OptionalBoolean(state, 4, usage));
                tradeSkill.RecraftRecipeRequests.Add(request);
                var result =
                    tradeSkill.RecraftRecipeProvider?.Invoke(request) ??
                    false;
                lua_pushboolean(state, result ? 1 : 0);
                return 1;
            }
            case "RecraftRecipeForOrder":
            {
                const string usage =
                    "Usage: local result = " +
                    "C_TradeSkillUI.RecraftRecipeForOrder(" +
                    "orderID, itemGUID [, craftingReagents, " +
                    "removedModifications, applyConcentration])";
                var request = new WowRecraftRecipeForOrderRequest(
                    RequiredUInt64(state, 1, usage),
                    RequiredString(state, 2, usage),
                    NullableCraftingReagentInfoArray(
                        state,
                        3,
                        usage),
                    OptionalCraftingItemSlotModificationArray(
                        state,
                        4,
                        usage),
                    OptionalBoolean(state, 5, usage));
                tradeSkill.RecraftRecipeForOrderRequests.Add(request);
                var result =
                    tradeSkill.RecraftRecipeForOrderProvider?.Invoke(
                        request) ?? false;
                lua_pushboolean(state, result ? 1 : 0);
                return 1;
            }
            case "GetRecipesTracked":
            {
                var isRecraft = RequiredBoolean(
                    state,
                    1,
                    "Usage: local recipeIDs = " +
                    "C_TradeSkillUI.GetRecipesTracked(isRecraft)");
                PushInt32Array(
                    state,
                    isRecraft
                        ? tradeSkill.TrackedRecraftRecipeIds
                        : tradeSkill.TrackedRecipeIds);
                return 1;
            }
            case "SetRecipeTracked":
            {
                const string usage =
                    "Usage: C_TradeSkillUI.SetRecipeTracked(" +
                    "recipeID, tracked, isRecraft)";
                var recipeId = RequiredInt32(state, 1, usage);
                var tracked = RequiredBoolean(state, 2, usage);
                var isRecraft = RequiredBoolean(state, 3, usage);
                var recipes = isRecraft
                    ? tradeSkill.TrackedRecraftRecipeIds
                    : tradeSkill.TrackedRecipeIds;
                if (tracked)
                {
                    recipes.Add(recipeId);
                }
                else
                {
                    recipes.Remove(recipeId);
                }
                return 0;
            }
            case "SetOnlyShowAvailableForOrders":
                tradeSkill.OnlyShowAvailableForOrders = RequiredBoolean(
                    state,
                    1,
                    "Usage: " +
                    "C_TradeSkillUI.SetOnlyShowAvailableForOrders(flag)");
                return 0;
            case "SetProfessionChildSkillLineID":
            {
                var skillLineId = RequiredInt32(
                    state,
                    1,
                    "Usage: " +
                    "C_TradeSkillUI.SetProfessionChildSkillLineID(" +
                    "skillLineID)");
                if (tradeSkill.SelectableProfessionChildSkillLineIds
                    .Contains(skillLineId))
                {
                    tradeSkill.ProfessionChildSkillLineId = skillLineId;
                }
                return 0;
            }
            case "CloseTradeSkill":
                tradeSkill.CloseTradeSkillCount++;
                return 0;
            case "GetShowLearned":
                lua_pushboolean(state, tradeSkill.ShowLearned ? 1 : 0);
                return 1;
            case "SetShowLearned":
                tradeSkill.ShowLearned = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.SetShowLearned(flag)");
                return 0;
            case "GetShowUnlearned":
                lua_pushboolean(state, tradeSkill.ShowUnlearned ? 1 : 0);
                return 1;
            case "SetShowUnlearned":
                tradeSkill.ShowUnlearned = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.SetShowUnlearned(flag)");
                return 0;
            case "GetSourceTypeFilter":
                lua_pushinteger(state, tradeSkill.SourceTypeFilter);
                return 1;
            case "SetSourceTypeFilter":
                tradeSkill.SourceTypeFilter = RequiredUInt16(
                    state,
                    1,
                    "Usage: C_TradeSkillUI.SetSourceTypeFilter(" +
                    "sourceTypeFilter)");
                return 0;
            case "IsGuildTradeSkillsEnabled":
                lua_pushboolean(
                    state,
                    tradeSkill.IsGuildTradeSkillsEnabled ? 1 : 0);
                return 1;
            case "IsNPCCrafting":
                lua_pushboolean(state, tradeSkill.IsNpcCrafting ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static void PushProfessionInfo(
        lua_State state,
        WowProfessionInfo info)
    {
        lua_createtable(state, 0, 11);
        SetOptionalInteger(state, "profession", info.Profession);
        SetInteger(state, "professionID", info.ProfessionId);
        SetInteger(state, "sourceCounter", info.SourceCounter);
        SetOptionalString(state, "professionName", info.ProfessionName);
        SetOptionalString(state, "expansionName", info.ExpansionName);
        SetInteger(state, "skillLevel", info.SkillLevel);
        SetInteger(state, "maxSkillLevel", info.MaxSkillLevel);
        SetInteger(state, "skillModifier", info.SkillModifier);
        lua_pushboolean(state, info.IsPrimaryProfession ? 1 : 0);
        lua_setfield(state, -2, "isPrimaryProfession");
        SetOptionalInteger(
            state,
            "parentProfessionID",
            info.ParentProfessionId);
        SetOptionalString(
            state,
            "parentProfessionName",
            info.ParentProfessionName);
    }

    private static void PushProfessionInfos(
        lua_State state,
        IEnumerable<WowProfessionInfo> infos)
    {
        var values = infos as IReadOnlyCollection<WowProfessionInfo> ??
            infos.ToArray();
        lua_createtable(state, values.Count, 0);
        var index = 1;
        foreach (var info in values)
        {
            PushProfessionInfo(state, info);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void PushItemReagentQualityInfo(
        lua_State state,
        WowItemReagentQualityInfo? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_createtable(state, 0, 12);
        SetInteger(state, "quality", info.Quality);
        SetOptionalString(state, "icon", info.Icon);
        SetOptionalString(state, "iconSmall", info.IconSmall);
        SetOptionalString(state, "iconInventory", info.IconInventory);
        SetOptionalString(state, "iconMixed", info.IconMixed);
        SetOptionalString(state, "iconAppear", info.IconAppear);
        SetOptionalString(state, "iconDissolve", info.IconDissolve);
        SetOptionalString(state, "barFill", info.BarFill);
        SetOptionalString(
            state,
            "barBackground",
            info.BarBackground);
        SetOptionalString(
            state,
            "barBackgroundCap",
            info.BarBackgroundCap);
        SetOptionalString(state, "barHighlight", info.BarHighlight);
        SetOptionalString(state, "iconChat", info.IconChat);
    }

    private static void PushCraftingItemSlotModifications(
        lua_State state,
        IReadOnlyList<WowCraftingItemSlotModification> modifications)
    {
        lua_createtable(state, modifications.Count, 0);
        for (var index = 0; index < modifications.Count; index++)
        {
            var modification = modifications[index];
            lua_createtable(state, 0, 2);
            SetInteger(
                state,
                "dataSlotIndex",
                modification.DataSlotIndex + 1);
            lua_createtable(state, 0, 2);
            SetOptionalUInt32(
                state,
                "itemID",
                modification.Reagent.ItemId);
            SetOptionalUInt32(
                state,
                "currencyID",
                modification.Reagent.CurrencyId);
            lua_setfield(state, -2, "reagent");
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushRecipeOutputItemData(
        lua_State state,
        WowRecipeOutputItemData output)
    {
        lua_createtable(state, 0, 3);
        SetInteger(state, "icon", output.Icon);
        SetOptionalString(state, "hyperlink", output.Hyperlink);
        SetOptionalInteger(state, "itemID", output.ItemId);
    }

    private static void PushCraftingTargetItems(
        lua_State state,
        IReadOnlyList<WowCraftingTargetItem> items)
    {
        lua_createtable(state, items.Count, 0);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            lua_createtable(state, 0, 4);
            SetInteger(state, "itemID", item.ItemId);
            lua_pushstring(state, item.ItemGuid);
            lua_setfield(state, -2, "itemGUID");
            SetOptionalString(state, "hyperlink", item.Hyperlink);
            SetInteger(state, "quantity", item.Quantity);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCraftingReagentInfos(
        lua_State state,
        IReadOnlyList<WowCraftingReagentInfo> reagents)
    {
        lua_createtable(state, reagents.Count, 0);
        for (var index = 0; index < reagents.Count; index++)
        {
            var reagent = reagents[index];
            lua_createtable(state, 0, 2);
            SetOptionalUInt32(state, "itemID", reagent.ItemId);
            SetOptionalUInt32(
                state,
                "currencyID",
                reagent.CurrencyId);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushGatheringOperationInfo(
        lua_State state,
        WowGatheringOperationInfo? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_createtable(state, 0, 5);
        SetInteger(state, "spellID", info.SpellId);
        SetInteger(state, "maxDifficulty", info.MaxDifficulty);
        SetInteger(state, "baseSkill", info.BaseSkill);
        SetInteger(state, "bonusSkill", info.BonusSkill);
        PushCraftingOperationBonusStats(state, info.BonusStats);
        lua_setfield(state, -2, "bonusStats");
    }

    private static void PushCraftingOperationInfo(
        lua_State state,
        WowCraftingOperationInfo? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_createtable(state, 0, 17);
        SetInteger(state, "recipeID", info.RecipeId);
        SetInteger(state, "baseDifficulty", info.BaseDifficulty);
        SetInteger(state, "bonusDifficulty", info.BonusDifficulty);
        SetInteger(state, "baseSkill", info.BaseSkill);
        SetInteger(state, "bonusSkill", info.BonusSkill);
        SetBoolean(state, "isQualityCraft", info.IsQualityCraft);
        lua_pushnumber(state, info.Quality);
        lua_setfield(state, -2, "quality");
        SetInteger(state, "craftingQuality", info.CraftingQuality);
        SetInteger(
            state,
            "craftingQualityID",
            info.CraftingQualityId);
        SetInteger(state, "craftingDataID", info.CraftingDataId);
        SetInteger(
            state,
            "lowerSkillThreshold",
            info.LowerSkillThreshold);
        SetInteger(
            state,
            "upperSkillTreshold",
            info.UpperSkillThreshold);
        SetInteger(
            state,
            "guaranteedCraftingQualityID",
            info.GuaranteedCraftingQualityId);
        PushCraftingOperationBonusStats(state, info.BonusStats);
        lua_setfield(state, -2, "bonusStats");
        SetInteger(
            state,
            "concentrationCurrencyID",
            info.ConcentrationCurrencyId);
        SetInteger(
            state,
            "concentrationCost",
            info.ConcentrationCost);
        SetInteger(state, "ingenuityRefund", info.IngenuityRefund);
    }

    private static void PushCraftingOperationBonusStats(
        lua_State state,
        IReadOnlyList<WowCraftingOperationBonusStatInfo> stats)
    {
        lua_createtable(state, stats.Count, 0);
        for (var index = 0; index < stats.Count; index++)
        {
            var stat = stats[index];
            lua_createtable(state, 0, 5);
            lua_pushstring(state, stat.BonusStatName);
            lua_setfield(state, -2, "bonusStatName");
            SetInteger(
                state,
                "bonusStatValue",
                stat.BonusStatValue);
            lua_pushstring(state, stat.RatingDescription);
            lua_setfield(state, -2, "ratingDescription");
            lua_pushnumber(state, stat.RatingPercent);
            lua_setfield(state, -2, "ratingPct");
            lua_pushnumber(state, stat.BonusRatingPercent);
            lua_setfield(state, -2, "bonusRatingPct");
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushRecipeRequirements(
        lua_State state,
        IReadOnlyList<WowCraftingRecipeRequirement> requirements)
    {
        lua_createtable(state, requirements.Count, 0);
        for (var index = 0; index < requirements.Count; index++)
        {
            var requirement = requirements[index];
            lua_createtable(state, 0, 3);
            SetOptionalString(state, "name", requirement.Name);
            SetBoolean(state, "met", requirement.Met);
            lua_pushnumber(state, requirement.Type);
            lua_setfield(state, -2, "type");
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushRecipeSchematic(
        lua_State state,
        WowCraftingRecipeSchematic schematic)
    {
        lua_createtable(state, 0, 11);
        SetInteger(state, "recipeID", schematic.RecipeId);
        SetInteger(state, "icon", schematic.Icon);
        SetInteger(state, "quantityMin", schematic.QuantityMin);
        SetInteger(state, "quantityMax", schematic.QuantityMax);
        SetOptionalString(state, "name", schematic.Name);
        lua_pushnumber(state, schematic.RecipeType);
        lua_setfield(state, -2, "recipeType");
        SetOptionalInteger(
            state,
            "productQuality",
            schematic.ProductQuality);
        SetOptionalInteger(
            state,
            "outputItemID",
            schematic.OutputItemId);
        PushReagentSlotSchematics(
            state,
            schematic.ReagentSlotSchematics ?? []);
        lua_setfield(state, -2, "reagentSlotSchematics");
        SetBoolean(state, "isRecraft", schematic.IsRecraft);
        SetBoolean(
            state,
            "hasCraftingOperationInfo",
            schematic.HasCraftingOperationInfo);
    }

    private static void PushReagentSlotSchematics(
        lua_State state,
        IReadOnlyList<WowCraftingReagentSlotSchematic> slots)
    {
        lua_createtable(state, slots.Count, 0);
        for (var index = 0; index < slots.Count; index++)
        {
            var slot = slots[index];
            lua_createtable(state, 0, 11);
            PushCraftingReagentInfos(state, slot.Reagents);
            lua_setfield(state, -2, "reagents");
            lua_pushnumber(state, slot.ReagentType);
            lua_setfield(state, -2, "reagentType");
            PushCraftingReagentQuantities(
                state,
                slot.VariableQuantities);
            lua_setfield(state, -2, "variableQuantities");
            SetInteger(
                state,
                "quantityRequired",
                slot.QuantityRequired);
            PushReagentSlotInfo(state, slot.SlotInfo);
            lua_setfield(state, -2, "slotInfo");
            lua_pushnumber(state, slot.DataSlotType);
            lua_setfield(state, -2, "dataSlotType");
            SetInteger(
                state,
                "dataSlotIndex",
                slot.DataSlotIndex + 1);
            SetInteger(state, "slotIndex", slot.SlotIndex + 1);
            SetOptionalInteger(
                state,
                "orderSource",
                slot.OrderSource);
            SetBoolean(state, "required", slot.Required);
            SetBoolean(
                state,
                "hiddenInCraftingForm",
                slot.HiddenInCraftingForm);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCraftingReagentQuantities(
        lua_State state,
        IReadOnlyList<WowCraftingReagentQuantity> quantities)
    {
        lua_createtable(state, quantities.Count, 0);
        for (var index = 0; index < quantities.Count; index++)
        {
            var quantity = quantities[index];
            lua_createtable(state, 0, 2);
            PushCraftingReagentInfos(state, [quantity.Reagent]);
            lua_rawgeti(state, -1, 1);
            lua_remove(state, -2);
            lua_setfield(state, -2, "reagent");
            SetInteger(state, "quantity", quantity.Quantity);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushReagentSlotInfo(
        lua_State state,
        WowCraftingReagentSlotInfo? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_createtable(state, 0, 3);
        SetInteger(state, "mcrSlotID", info.McrSlotId);
        SetInteger(
            state,
            "requiredSkillRank",
            info.RequiredSkillRank);
        SetOptionalString(state, "slotText", info.SlotText);
    }

    private static void PushTradeSkillRecipeInfo(
        lua_State state,
        WowTradeSkillRecipeInfo? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_createtable(state, 0, 41);
        SetInteger(state, "categoryID", info.CategoryId);
        SetOptionalString(state, "name", info.Name);
        SetOptionalInteger(
            state,
            "relativeDifficulty",
            info.RelativeDifficulty);
        SetInteger(state, "maxTrivialLevel", info.MaxTrivialLevel);
        SetInteger(state, "itemLevel", info.ItemLevel);
        SetOptionalString(
            state,
            "alternateVerb",
            info.AlternateVerb);
        SetInteger(state, "numSkillUps", info.NumSkillUps);
        SetBoolean(state, "canSkillUp", info.CanSkillUp);
        SetBoolean(state, "firstCraft", info.FirstCraft);
        SetOptionalInteger(state, "sourceType", info.SourceType);
        SetBoolean(state, "learned", info.Learned);
        SetBoolean(state, "disabled", info.Disabled);
        SetBoolean(state, "favorite", info.Favorite);
        SetBoolean(
            state,
            "supportsQualities",
            info.SupportsQualities);
        SetBoolean(state, "craftable", info.Craftable);
        SetOptionalString(
            state,
            "disabledReason",
            info.DisabledReason);
        SetInteger(state, "recipeID", info.RecipeId);
        SetInteger(
            state,
            "skillLineAbilityID",
            info.SkillLineAbilityId);
        SetOptionalInteger(
            state,
            "previousRecipeID",
            info.PreviousRecipeId);
        SetOptionalInteger(state, "nextRecipeID", info.NextRecipeId);
        SetOptionalInteger(state, "icon", info.Icon);
        SetOptionalString(state, "hyperlink", info.Hyperlink);
        SetOptionalInteger(
            state,
            "currentRecipeExperience",
            info.CurrentRecipeExperience);
        SetOptionalInteger(
            state,
            "nextLevelRecipeExperience",
            info.NextLevelRecipeExperience);
        SetOptionalInteger(
            state,
            "unlockedRecipeLevel",
            info.UnlockedRecipeLevel);
        SetOptionalInteger(
            state,
            "earnedExperience",
            info.EarnedExperience);
        SetBoolean(
            state,
            "supportsCraftingStats",
            info.SupportsCraftingStats);
        SetBoolean(
            state,
            "hasSingleItemOutput",
            info.HasSingleItemOutput);
        SetOptionalInt32Array(
            state,
            "qualityItemIDs",
            info.QualityItemIds);
        SetOptionalInt32Array(
            state,
            "qualityIlvlBonuses",
            info.QualityItemLevelBonuses);
        SetBoolean(
            state,
            "alwaysUsesLowestQuality",
            info.AlwaysUsesLowestQuality);
        SetOptionalInteger(state, "maxQuality", info.MaxQuality);
        SetOptionalInt32Array(
            state,
            "qualityIDs",
            info.QualityIds);
        SetBoolean(
            state,
            "canCreateMultiple",
            info.CanCreateMultiple);
        SetOptionalString(state, "abilityVerb", info.AbilityVerb);
        SetOptionalString(
            state,
            "abilityAllVerb",
            info.AbilityAllVerb);
        SetBoolean(state, "isRecraft", info.IsRecraft);
        SetBoolean(state, "isDummyRecipe", info.IsDummyRecipe);
        SetBoolean(
            state,
            "isGatheringRecipe",
            info.IsGatheringRecipe);
        SetBoolean(
            state,
            "isEnchantingRecipe",
            info.IsEnchantingRecipe);
        SetBoolean(
            state,
            "isSalvageRecipe",
            info.IsSalvageRecipe);
    }

    private static void PushInt32Array(
        lua_State state,
        IEnumerable<int> values)
    {
        var array = values as IReadOnlyCollection<int> ?? values.ToArray();
        lua_createtable(state, array.Count, 0);
        var index = 1;
        foreach (var value in array)
        {
            lua_pushinteger(state, value);
            lua_rawseti(state, -2, index++);
        }
    }

    private static int PushReusableInt32Array(
        lua_State state,
        IList<int> values,
        string usage)
    {
        var argumentType = lua_type(state, 1);
        if (argumentType is not (LUA_TNONE or LUA_TNIL or LUA_TTABLE))
            return luaL_error(state, usage);

        if (argumentType == LUA_TTABLE)
        {
            lua_pushvalue(state, 1);
            for (var index = (int)lua_objlen(state, -1); index > 0; index--)
            {
                lua_pushnil(state);
                lua_rawseti(state, -2, index);
            }
        }
        else
        {
            lua_createtable(state, values.Count, 0);
        }

        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushUInt32Array(
        lua_State state,
        IEnumerable<uint> values)
    {
        var array = values as IReadOnlyCollection<uint> ?? values.ToArray();
        lua_createtable(state, array.Count, 0);
        var index = 1;
        foreach (var value in array)
        {
            lua_pushnumber(state, value);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void PushStringArray(
        lua_State state,
        IEnumerable<string> values)
    {
        var array = values as IReadOnlyCollection<string> ??
            values.ToArray();
        lua_createtable(state, array.Count, 0);
        var index = 1;
        foreach (var value in array)
        {
            lua_pushstring(state, value);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void PushOptionalStringArray(
        lua_State state,
        IEnumerable<string?> values)
    {
        var array = values as IReadOnlyCollection<string?> ??
            values.ToArray();
        lua_createtable(state, array.Count, 0);
        var index = 1;
        foreach (var value in array)
        {
            if (value is null)
            {
                lua_pushnil(state);
            }
            else
            {
                lua_pushstring(state, value);
            }
            lua_rawseti(state, -2, index++);
        }
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnil(state, index) != 0)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static int RequiredInt32Enum(
        lua_State state,
        int index,
        int minimum,
        int maximum,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < minimum || value > maximum)
        {
            luaL_error(state, usage);
            return minimum;
        }
        return value;
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        return RequiredInt32(state, index, usage);
    }

    private static int? OptionalOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }

        var value = RequiredInt32(state, index, usage);
        if (value <= 0)
        {
            luaL_error(state, usage);
            return null;
        }
        return value - 1;
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage) =>
        OptionalOneBasedIndex(state, index, usage) ??
        ThrowRequiredOneBasedIndex(state, usage);

    private static int ThrowRequiredOneBasedIndex(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < uint.MinValue ||
            number > uint.MaxValue ||
            Math.Truncate(number) != number)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)number;
    }

    private static uint OptionalUInt32(
        lua_State state,
        int index,
        uint defaultValue,
        string usage) =>
        lua_type(state, index) is LUA_TNONE or LUA_TNIL
            ? defaultValue
            : RequiredUInt32(state, index, usage);

    private static ulong RequiredUInt64(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < 0 ||
            number >= 18446744073709551616d ||
            Math.Truncate(number) != number)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (ulong)number;
    }

    private static ulong? OptionalUInt64(
        lua_State state,
        int index,
        string usage) =>
        lua_type(state, index) is LUA_TNONE or LUA_TNIL
            ? null
            : RequiredUInt64(state, index, usage);

    private static bool? OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }

        _ = usage;
        return lua_toboolean(state, index) != 0;
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string usage) =>
        lua_type(state, index) is LUA_TNONE or LUA_TNIL
            ? null
            : RequiredString(state, index, usage);

    private static WowItemLocation? OptionalItemLocation(
        lua_State state,
        int index,
        string usage) =>
        lua_type(state, index) is LUA_TNONE or LUA_TNIL
            ? null
            : WowItemApi.RequiredItemLocation(state, index, usage);

    private static IReadOnlyList<WowCraftingReagentInfo>?
        NullableCraftingReagentInfoArray(
            lua_State state,
            int index,
            string usage) =>
        lua_type(state, index) is LUA_TNONE or LUA_TNIL
            ? null
            : RequiredCraftingReagentInfoArray(state, index, usage);

    private static IReadOnlyList<WowCraftingReagentInfo>
        OptionalCraftingReagentInfoArray(
            lua_State state,
            int index,
            string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return [];
        }
        if (lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return [];
        }

        var absoluteIndex = index > 0
            ? index
            : lua_gettop(state) + index + 1;
        var count = checked((int)lua_objlen(state, absoluteIndex));
        var result = new List<WowCraftingReagentInfo>(count);
        for (var itemIndex = 1; itemIndex <= count; itemIndex++)
        {
            lua_rawgeti(state, absoluteIndex, itemIndex);
            result.Add(
                RequiredCraftingReagentInfo(state, -1, usage));
            lua_pop(state, 1);
        }
        return result;
    }

    private static IReadOnlyList<WowCraftingReagentInfo>
        RequiredCraftingReagentInfoArray(
            lua_State state,
            int index,
            string usage)
    {
        if (lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return [];
        }
        return OptionalCraftingReagentInfoArray(state, index, usage);
    }

    private static IReadOnlyList<int> RequiredInt32Array(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return [];
        }

        var absoluteIndex = index > 0
            ? index
            : lua_gettop(state) + index + 1;
        var count = checked((int)lua_objlen(state, absoluteIndex));
        var result = new List<int>(count);
        for (var itemIndex = 1; itemIndex <= count; itemIndex++)
        {
            lua_rawgeti(state, absoluteIndex, itemIndex);
            result.Add(RequiredInt32(state, -1, usage));
            lua_pop(state, 1);
        }
        return result;
    }

    private static IReadOnlyList<WowCraftingItemSlotModification>?
        OptionalCraftingItemSlotModificationArray(
            lua_State state,
            int index,
            string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        if (lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return null;
        }

        var absoluteIndex = index > 0
            ? index
            : lua_gettop(state) + index + 1;
        var count = checked((int)lua_objlen(state, absoluteIndex));
        var result =
            new List<WowCraftingItemSlotModification>(count);
        for (var itemIndex = 1; itemIndex <= count; itemIndex++)
        {
            lua_rawgeti(state, absoluteIndex, itemIndex);
            result.Add(
                RequiredCraftingItemSlotModification(
                    state,
                    -1,
                    usage));
            lua_pop(state, 1);
        }
        return result;
    }

    private static WowCraftingItemSlotModification
        RequiredCraftingItemSlotModification(
            lua_State state,
            int index,
            string usage)
    {
        if (lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return new WowCraftingItemSlotModification(
                0,
                new WowCraftingReagentInfo());
        }

        var absoluteIndex = index > 0
            ? index
            : lua_gettop(state) + index + 1;
        lua_getfield(state, absoluteIndex, "dataSlotIndex");
        var dataSlotIndex = OptionalOneBasedIndex(state, -1, usage);
        lua_pop(state, 1);
        if (!dataSlotIndex.HasValue)
        {
            luaL_error(state, usage);
        }

        lua_getfield(state, absoluteIndex, "reagent");
        var reagent = RequiredCraftingReagentInfo(state, -1, usage);
        lua_pop(state, 1);
        return new WowCraftingItemSlotModification(
            dataSlotIndex ?? 0,
            reagent);
    }

    private static WowCraftingReagentInfo RequiredCraftingReagentInfo(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return new WowCraftingReagentInfo();
        }

        var absoluteIndex = index > 0
            ? index
            : lua_gettop(state) + index + 1;
        return new WowCraftingReagentInfo(
            OptionalUInt32Field(
                state,
                absoluteIndex,
                "itemID",
                usage),
            OptionalUInt32Field(
                state,
                absoluteIndex,
                "currencyID",
                usage));
    }

    private static uint? OptionalUInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        try
        {
            if (lua_isnil(state, -1) != 0)
            {
                return null;
            }
            if (lua_isnumber(state, -1) == 0)
            {
                luaL_error(state, usage);
                return null;
            }

            var number = lua_tonumber(state, -1);
            if (!double.IsFinite(number) ||
                number < uint.MinValue ||
                number > uint.MaxValue)
            {
                luaL_error(state, usage);
                return null;
            }
            return (uint)number;
        }
        finally
        {
            lua_pop(state, 1);
        }
    }

    private static ushort RequiredUInt16(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < ushort.MinValue ||
            number > ushort.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (ushort)number;
    }

    private static void SetInteger(
        lua_State state,
        string field,
        int value)
    {
        lua_pushinteger(state, value);
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

    private static void SetOptionalInt32Array(
        lua_State state,
        string field,
        IReadOnlyList<int>? values)
    {
        if (values is null)
        {
            lua_pushnil(state);
        }
        else
        {
            PushInt32Array(state, values);
        }
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        if (value.HasValue)
        {
            lua_pushinteger(state, value.Value);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalUInt32(
        lua_State state,
        string field,
        uint? value)
    {
        if (value.HasValue)
        {
            lua_pushnumber(state, value.Value);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, field);
    }

    private static void PushOptionalInteger(
        lua_State state,
        int? value)
    {
        if (value.HasValue)
        {
            lua_pushinteger(state, value.Value);
        }
        else
        {
            lua_pushnil(state);
        }
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            lua_pushstring(state, value);
        }
        lua_setfield(state, -2, field);
    }

    private static void RegisterEnums(lua_State state)
    {
        EnsureGlobalTable(state, "Enum");

        SetEnum(
            state,
            "CraftingReagentItemFlag",
            [("TooltipShowsAsStatModifications", 1)]);
        SetEnumMeta(state, "CraftingReagentItemFlagMeta", 1, 1, 1);

        SetEnum(
            state,
            "TradeskillSlotDataType",
            [("Reagent", 1), ("ModifiedReagent", 2), ("Currency", 3)]);
        SetEnumMeta(state, "TradeskillSlotDataTypeMeta", 3, 1, 3);

        SetEnum(
            state,
            "RecipeRequirementType",
            [("SpellFocus", 0), ("Totem", 1), ("Area", 2)]);
        SetEnumMeta(state, "RecipeRequirementTypeMeta", 3, 0, 2);

        SetEnum(
            state,
            "TradeskillRecipeType",
            [
                ("Item", 1),
                ("Salvage", 2),
                ("Enchant", 3),
                ("Gathering", 4)
            ]);
        SetEnumMeta(state, "TradeskillRecipeTypeMeta", 4, 1, 4);

        SetEnum(
            state,
            "TradeskillOrderDuration",
            [("Short", 1), ("Medium", 2), ("Long", 3)]);
        SetEnumMeta(state, "TradeskillOrderDurationMeta", 3, 1, 3);

        SetEnum(
            state,
            "TradeskillOrderRecipient",
            [("Public", 1), ("Guild", 2), ("Private", 3)]);
        SetEnumMeta(state, "TradeskillOrderRecipientMeta", 3, 1, 3);

        SetEnum(
            state,
            "TradeskillOrderStatus",
            [
                ("Unclaimed", 1),
                ("Started", 2),
                ("Completed", 3),
                ("Expired", 4)
            ]);
        SetEnumMeta(state, "TradeskillOrderStatusMeta", 4, 1, 4);

        SetEnum(
            state,
            "TradeskillRelativeDifficulty",
            [
                ("Optimal", 0),
                ("Medium", 1),
                ("Easy", 2),
                ("Trivial", 3)
            ]);
        SetEnumMeta(state, "TradeskillRelativeDifficultyMeta", 4, 0, 3);

        lua_pop(state, 1);
    }

    private static void EnsureGlobalTable(lua_State state, string name)
    {
        lua_getglobal(state, name);
        if (lua_istable(state, -1) != 0)
        {
            return;
        }

        lua_pop(state, 1);
        lua_newtable(state);
        lua_pushvalue(state, -1);
        lua_setglobal(state, name);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        IReadOnlyList<(string Name, int Value)> members)
    {
        lua_newtable(state);
        foreach (var member in members)
        {
            lua_pushinteger(state, member.Value);
            lua_setfield(state, -2, member.Name);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int numValues,
        int minValue,
        int maxValue) =>
        SetEnum(
            state,
            name,
            [
                ("NumValues", numValues),
                ("MinValue", minValue),
                ("MaxValue", maxValue)
            ]);
}
