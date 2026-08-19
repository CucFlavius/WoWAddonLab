using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class TradeSkillUiContractTests
{
    private static readonly string[] NativeFunctions =
    [
        "AnyRecipeCategoriesFiltered",
        "AreAnyInventorySlotsFiltered",
        "CanStoreEnchantInItem",
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
        "IsInventorySlotFiltered",
        "IsOriginalCraftRecipeLearned",
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
        "SetInventorySlotFilter",
        "SetProfessionChildSkillLineID",
        "SetRecipeItemLevelFilter",
        "SetRecipeItemNameFilter",
        "SetRecipeSourceTypeFilter",
        "SetRecipeTracked",
        "SetShowLearned",
        "SetShowUnlearned",
        "SetSourceTypeFilter"
    ];

    [Fact]
    public void RegistersExactNativeSurfaceAndRemovesLegacyEntries()
    {
        using var session = new EmulatorSession();
        var expected = string.Join(
            ",",
            NativeFunctions.Order(StringComparer.Ordinal));

        Assert.Equal(
            expected,
            session.Lua.Evaluate(
                "local names={}; for name in pairs(C_TradeSkillUI) do " +
                "names[#names+1]=name end; table.sort(names); " +
                "return table.concat(names,',')"));

        Assert.Equal(
            "function:function:function:function:function:function:nil:nil:nil:nil",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_TradeSkillUI.GetAllFilterableInventorySlotsCount)," +
                "type(C_TradeSkillUI.GetOnlyShowMakeableRecipes)," +
                "type(C_TradeSkillUI.GetOnlyShowSkillUpRecipes)," +
                "type(C_TradeSkillUI.GetOnlyShowFirstCraftRecipes)," +
                "type(C_TradeSkillUI.AreAnyInventorySlotsFiltered)," +
                "type(C_TradeSkillUI.AnyRecipeCategoriesFiltered)," +
                "type(C_TradeSkillUI.CloseObliterumForge)," +
                "type(C_TradeSkillUI.GetPendingObliterateItemID)," +
                "type(C_TradeSkillUI.ObliterateItem)," +
                "type(C_TradeSkillUI.CloseCrafterCraftingOrders)},':')"));
    }

    [Fact]
    public void RecipeFiltersUseRuntimeState()
    {
        using var session = new EmulatorSession();
        session.Lua.TradeSkillUi.FilterableInventorySlotNames.Add("Weapon");
        session.Lua.TradeSkillUi.RecipeSourceTypes.Add(2);
        session.Lua.TradeSkillUi.FilteredRecipeIds.Add(123);

        Assert.Equal(
            "1:Weapon:true:true:true:true:ore:10:20:true:123:true",
            session.Lua.Evaluate(
                "C_TradeSkillUI.SetInventorySlotFilter(1,true);" +
                "C_TradeSkillUI.SetRecipeSourceTypeFilter(2,true);" +
                "C_TradeSkillUI.SetOnlyShowMakeableRecipes(true);" +
                "C_TradeSkillUI.SetRecipeItemNameFilter('ore');" +
                "C_TradeSkillUI.SetRecipeItemLevelFilter(10,20);" +
                "local minLevel,maxLevel=C_TradeSkillUI.GetRecipeItemLevelFilter();" +
                "return table.concat({" +
                "C_TradeSkillUI.GetAllFilterableInventorySlotsCount()," +
                "C_TradeSkillUI.GetFilterableInventorySlotName(1)," +
                "tostring(C_TradeSkillUI.AreAnyInventorySlotsFiltered())," +
                "tostring(C_TradeSkillUI.IsInventorySlotFiltered(1))," +
                "tostring(C_TradeSkillUI.IsRecipeSourceTypeFiltered(2))," +
                "tostring(C_TradeSkillUI.GetOnlyShowMakeableRecipes())," +
                "C_TradeSkillUI.GetRecipeItemNameFilter(),minLevel,maxLevel," +
                "tostring(C_TradeSkillUI.IsAnyRecipeFromSource(2))," +
                "C_TradeSkillUI.GetFilteredRecipeIDs({99})[1]," +
                "tostring(C_TradeSkillUI.GetFilteredRecipeIDs({}) ~= nil)},':')"));
    }

    [Fact]
    public void HiddenTradeSkillContextQueriesUseRuntimeState()
    {
        using var session = new EmulatorSession();
        session.Lua.TradeSkillUi.IsTradeSkillGuild = true;
        session.Lua.TradeSkillUi.IsTradeSkillGuildMember = true;
        session.Lua.TradeSkillUi.IsTradeSkillLinked = true;
        session.Lua.TradeSkillUi.LinkedTradeSkillPlayerName = "Crafter-Realm";

        Assert.Equal(
            "true:true:true:Crafter-Realm:2",
            session.Lua.Evaluate(
                "local linked,name=C_TradeSkillUI.IsTradeSkillLinked();" +
                "return table.concat({" +
                "tostring(C_TradeSkillUI.IsTradeSkillGuild())," +
                "tostring(C_TradeSkillUI.IsTradeSkillGuildMember())," +
                "tostring(linked),name," +
                "select('#',C_TradeSkillUI.IsTradeSkillLinked())},':')"));
    }

    [Fact]
    public void RegistersRecoveredTradeSkillEnums()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1:1:1:1:1:2:3:3:1:3:0:1:2:3:0:2:" +
            "1:2:3:4:4:1:4:1:2:3:3:1:3:" +
            "1:2:3:3:1:3:1:2:3:4:4:1:4:" +
            "0:1:2:3:4:0:3",
            session.Lua.Evaluate(
                "local E=Enum; return table.concat({" +
                "E.CraftingReagentItemFlag.TooltipShowsAsStatModifications," +
                "E.CraftingReagentItemFlagMeta.NumValues," +
                "E.CraftingReagentItemFlagMeta.MinValue," +
                "E.CraftingReagentItemFlagMeta.MaxValue," +
                "E.TradeskillSlotDataType.Reagent," +
                "E.TradeskillSlotDataType.ModifiedReagent," +
                "E.TradeskillSlotDataType.Currency," +
                "E.TradeskillSlotDataTypeMeta.NumValues," +
                "E.TradeskillSlotDataTypeMeta.MinValue," +
                "E.TradeskillSlotDataTypeMeta.MaxValue," +
                "E.RecipeRequirementType.SpellFocus," +
                "E.RecipeRequirementType.Totem," +
                "E.RecipeRequirementType.Area," +
                "E.RecipeRequirementTypeMeta.NumValues," +
                "E.RecipeRequirementTypeMeta.MinValue," +
                "E.RecipeRequirementTypeMeta.MaxValue," +
                "E.TradeskillRecipeType.Item," +
                "E.TradeskillRecipeType.Salvage," +
                "E.TradeskillRecipeType.Enchant," +
                "E.TradeskillRecipeType.Gathering," +
                "E.TradeskillRecipeTypeMeta.NumValues," +
                "E.TradeskillRecipeTypeMeta.MinValue," +
                "E.TradeskillRecipeTypeMeta.MaxValue," +
                "E.TradeskillOrderDuration.Short," +
                "E.TradeskillOrderDuration.Medium," +
                "E.TradeskillOrderDuration.Long," +
                "E.TradeskillOrderDurationMeta.NumValues," +
                "E.TradeskillOrderDurationMeta.MinValue," +
                "E.TradeskillOrderDurationMeta.MaxValue," +
                "E.TradeskillOrderRecipient.Public," +
                "E.TradeskillOrderRecipient.Guild," +
                "E.TradeskillOrderRecipient.Private," +
                "E.TradeskillOrderRecipientMeta.NumValues," +
                "E.TradeskillOrderRecipientMeta.MinValue," +
                "E.TradeskillOrderRecipientMeta.MaxValue," +
                "E.TradeskillOrderStatus.Unclaimed," +
                "E.TradeskillOrderStatus.Started," +
                "E.TradeskillOrderStatus.Completed," +
                "E.TradeskillOrderStatus.Expired," +
                "E.TradeskillOrderStatusMeta.NumValues," +
                "E.TradeskillOrderStatusMeta.MinValue," +
                "E.TradeskillOrderStatusMeta.MaxValue," +
                "E.TradeskillRelativeDifficulty.Optimal," +
                "E.TradeskillRelativeDifficulty.Medium," +
                "E.TradeskillRelativeDifficulty.Easy," +
                "E.TradeskillRelativeDifficulty.Trivial," +
                "E.TradeskillRelativeDifficultyMeta.NumValues," +
                "E.TradeskillRelativeDifficultyMeta.MinValue," +
                "E.TradeskillRelativeDifficultyMeta.MaxValue},':')"));
    }

    [Fact]
    public void ProjectsProfessionInfoAndStatefulFilterContracts()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.BaseProfessionInfo = new WowProfessionInfo(
            4,
            171,
            9,
            "Alchemy",
            "Khaz Algar Alchemy",
            76,
            100,
            5,
            true,
            164,
            "Blacksmithing");
        tradeSkill.ChildProfessionInfo = new WowProfessionInfo(
            ProfessionId: 2871,
            ProfessionName: "Test Child");
        tradeSkill.ChildProfessionInfos.Add(
            tradeSkill.ChildProfessionInfo);
        tradeSkill.IsGuildTradeSkillsEnabled = true;
        tradeSkill.IsNpcCrafting = true;

        Assert.Equal(
            "4:171:9:Alchemy:Khaz Algar Alchemy:76:100:5:true:" +
            "164:Blacksmithing:2871:1:true:true",
            session.Lua.Evaluate(
                "local b=C_TradeSkillUI.GetBaseProfessionInfo();" +
                "local c=C_TradeSkillUI.GetChildProfessionInfo();" +
                "local cs=C_TradeSkillUI.GetChildProfessionInfos();" +
                "return table.concat({b.profession,b.professionID," +
                "b.sourceCounter,b.professionName,b.expansionName," +
                "b.skillLevel,b.maxSkillLevel,b.skillModifier," +
                "tostring(b.isPrimaryProfession),b.parentProfessionID," +
                "b.parentProfessionName,c.professionID,#cs," +
                "tostring(C_TradeSkillUI.IsGuildTradeSkillsEnabled())," +
                "tostring(C_TradeSkillUI.IsNPCCrafting())},':')"));

        Assert.Equal(
            "true:true:65535:2:42:77:1:99:0:0",
            session.Lua.Evaluate(
                "C_TradeSkillUI.SetShowLearned(false);" +
                "C_TradeSkillUI.SetShowUnlearned(nil or true);" +
                "C_TradeSkillUI.SetSourceTypeFilter(65535);" +
                "C_TradeSkillUI.SetRecipeTracked(42.8,true,false);" +
                "C_TradeSkillUI.SetRecipeTracked(77,true,false);" +
                "C_TradeSkillUI.SetRecipeTracked(99,true,true);" +
                "local normal=C_TradeSkillUI.GetRecipesTracked(false);" +
                "local recraft=C_TradeSkillUI.GetRecipesTracked(true);" +
                "C_TradeSkillUI.CloseTradeSkill();" +
                "return table.concat({" +
                "tostring(not C_TradeSkillUI.GetShowLearned())," +
                "tostring(C_TradeSkillUI.GetShowUnlearned())," +
                "C_TradeSkillUI.GetSourceTypeFilter(),#normal," +
                "normal[1],normal[2],#recraft,recraft[1]," +
                "select('#',C_TradeSkillUI.CloseTradeSkill())," +
                "select('#',C_TradeSkillUI.SetRecipeTracked(" +
                "42,false,false))},':')"));
        Assert.Equal(2, tradeSkill.CloseTradeSkillCount);
        Assert.DoesNotContain(42, tradeSkill.TrackedRecipeIds);
    }

    [Fact]
    public void EnforcesRecoveredRequiredArgumentParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:true:true:false:false:false:true:false:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_TradeSkillUI.GetRecipesTracked))," +
                "tostring(ok(C_TradeSkillUI.GetRecipesTracked,nil))," +
                "tostring(ok(C_TradeSkillUI.GetRecipesTracked,0))," +
                "tostring(ok(C_TradeSkillUI.GetRecipesTracked,{}))," +
                "tostring(ok(C_TradeSkillUI.SetRecipeTracked,1,true))," +
                "tostring(ok(C_TradeSkillUI.SetRecipeTracked,{},true,false))," +
                "tostring(ok(C_TradeSkillUI.SetShowLearned))," +
                "tostring(ok(C_TradeSkillUI.SetShowLearned,0))," +
                "tostring(ok(C_TradeSkillUI.SetSourceTypeFilter,-1))," +
                "tostring(ok(C_TradeSkillUI.SetSourceTypeFilter,65536))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredProfessionAndRecipeLookupState()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.EnchantStorableItemGuids.Add(
            "Item-3676-000000000001");
        tradeSkill.ProfessionTradeSkillLineIds.Add(164);
        tradeSkill.ProfessionTradeSkillLineIds.Add(171);
        tradeSkill.ConcentrationCurrencyIds[2871] = 3058;
        tradeSkill.CraftableCounts[(445466, null)] = 8;
        tradeSkill.CraftableCounts[(445466, 2)] = 3;
        tradeSkill.HideUnownedFlags[445466] = (true, false);
        tradeSkill.ProfessionChildSkillLineId = 2871;
        tradeSkill.ProfessionSkillLineId = 171;
        tradeSkill.ProfessionSlots[4] = [5, 11, uint.MaxValue];
        tradeSkill.ProfessionSpells[(171, null)] = [2259, 11611];
        tradeSkill.ProfessionSpells[(171, 2871)] = [423321];
        tradeSkill.CanRespecAtNpc = true;
        tradeSkill.HasPendingProfessionRespec = true;

        Assert.Equal(
            "true:false:true:2:164:171:3058:0:8:3:true:false:" +
            "2871:171:3:5:11:4294967295:2:2259:11611:1:423321",
            session.Lua.Evaluate(
                "local lines=" +
                "C_TradeSkillUI.GetAllProfessionTradeSkillLines();" +
                "local slots=C_TradeSkillUI.GetProfessionSlots(4);" +
                "local spells=C_TradeSkillUI.GetProfessionSpells(171);" +
                "local childSpells=" +
                "C_TradeSkillUI.GetProfessionSpells(171,2871);" +
                "local cannotModify,alwaysShow=" +
                "C_TradeSkillUI.GetHideUnownedFlags(445466);" +
                "return table.concat({" +
                "tostring(C_TradeSkillUI.CanStoreEnchantInItem(" +
                "'Item-3676-000000000001'))," +
                "tostring(C_TradeSkillUI.CanStoreEnchantInItem(" +
                "'Item-3676-000000000002'))," +
                "tostring(C_TradeSkillUI.CheckRespecNPC())," +
                "#lines,lines[1],lines[2]," +
                "C_TradeSkillUI.GetConcentrationCurrencyID(2871)," +
                "C_TradeSkillUI.GetConcentrationCurrencyID(0)," +
                "C_TradeSkillUI.GetCraftableCount(445466)," +
                "C_TradeSkillUI.GetCraftableCount(445466,3)," +
                "tostring(cannotModify),tostring(alwaysShow)," +
                "C_TradeSkillUI.GetProfessionChildSkillLineID()," +
                "C_TradeSkillUI.GetProfessionSkillLineID(4)," +
                "#slots,slots[1],slots[2],slots[3]," +
                "#spells,spells[1],spells[2]," +
                "#childSpells,childSpells[1]},':')"));

        Assert.Equal(
            "0:0:false:1:1",
            session.Lua.Evaluate(
                "local a=C_TradeSkillUI.CancelProfessionRespec();" +
                "local pendingAfterCancel=" +
                "tostring(false);" +
                "C_TradeSkillUI.ConfirmProfessionRespec();" +
                "return table.concat({" +
                "select('#',C_TradeSkillUI.CancelProfessionRespec())," +
                "select('#',C_TradeSkillUI.ConfirmProfessionRespec())," +
                "pendingAfterCancel,1,1},':')"));
        Assert.False(tradeSkill.HasPendingProfessionRespec);
        Assert.Equal(2, tradeSkill.CancelProfessionRespecCount);
        Assert.Equal(2, tradeSkill.ConfirmProfessionRespecCount);
    }

    [Fact]
    public void EnforcesRecoveredProfessionLookupParsersAndNeutralShapes()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:true:false:false:false:true:false:true:" +
            "false:true:false:true:0:0:false:false:0:0:0",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "local a,b=C_TradeSkillUI.GetHideUnownedFlags(1);" +
                "return table.concat({" +
                "tostring(ok(C_TradeSkillUI.CanStoreEnchantInItem))," +
                "tostring(ok(C_TradeSkillUI.CanStoreEnchantInItem,1))," +
                "tostring(ok(C_TradeSkillUI.CanStoreEnchantInItem,'x'))," +
                "tostring(ok(C_TradeSkillUI.GetConcentrationCurrencyID))," +
                "tostring(ok(C_TradeSkillUI.GetCraftableCount))," +
                "tostring(ok(C_TradeSkillUI.GetCraftableCount,1,0))," +
                "tostring(ok(C_TradeSkillUI.GetCraftableCount,1,nil))," +
                "tostring(ok(C_TradeSkillUI.GetProfessionSkillLineID,-1))," +
                "tostring(ok(C_TradeSkillUI.GetProfessionSkillLineID,14))," +
                "tostring(ok(C_TradeSkillUI.GetProfessionSkillLineID,15))," +
                "tostring(ok(C_TradeSkillUI.GetProfessionSpells,1,nil))," +
                "tostring(ok(C_TradeSkillUI.GetProfessionSpells,{}))," +
                "tostring(ok(C_TradeSkillUI.GetHideUnownedFlags,1))," +
                "C_TradeSkillUI.GetConcentrationCurrencyID(1)," +
                "C_TradeSkillUI.GetCraftableCount(1)," +
                "tostring(a),tostring(b)," +
                "#C_TradeSkillUI.GetProfessionSlots(0)," +
                "#C_TradeSkillUI.GetProfessionSpells(1)," +
                "#C_TradeSkillUI.GetAllProfessionTradeSkillLines()" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredRecipePredicateStateAndReagentTables()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.HasFavoriteOrderRecipes = true;
        tradeSkill.NearProfessionSpellFocusProfessions.Add(4);
        tradeSkill.OriginalCraftRecipeLearnedItemGuids.Add("Item-1");
        tradeSkill.FirstCraftRecipeIds.Add(101);
        tradeSkill.BaseSkillLineRecipeIds.Add(102);
        tradeSkill.SkillLineRecipes.Add((103, 171));
        tradeSkill.ProfessionLearnedRecipeIds.Add(104);
        tradeSkill.TrackedRecipeIds.Add(105);
        tradeSkill.TrackedRecraftRecipeIds.Add(106);
        tradeSkill.EquippedRecraftItemGuids.Add("Item-2");
        tradeSkill.ProfessionSkillLineId = 960;

        tradeSkill.EnchantTargetValidator =
            (recipeId, guid, reagents) =>
                recipeId == 107 &&
                guid == "Item-3" &&
                reagents.Count == 2 &&
                reagents[0] == new WowCraftingReagentInfo(55, null) &&
                reagents[1] == new WowCraftingReagentInfo(null, 66);
        tradeSkill.RecraftReagentValidator =
            (guid, reagent) =>
                guid == "Item-4" &&
                reagent == new WowCraftingReagentInfo(77, 88);
        tradeSkill.RecraftLimitCategoryValidator =
            reagent =>
                reagent == new WowCraftingReagentInfo(99, null);

        Assert.Equal(
            "true:true:false:true:false:true:false:true:false:true:" +
            "false:true:false:true:false:true:true:true:true:true:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(C_TradeSkillUI.HasFavoriteOrderRecipes())," +
                "tostring(C_TradeSkillUI.IsNearProfessionSpellFocus(4))," +
                "tostring(C_TradeSkillUI.IsNearProfessionSpellFocus(5))," +
                "tostring(C_TradeSkillUI.IsOriginalCraftRecipeLearned(" +
                "'Item-1'))," +
                "tostring(C_TradeSkillUI.IsOriginalCraftRecipeLearned(" +
                "'Item-x'))," +
                "tostring(C_TradeSkillUI.IsRecipeFirstCraft(101))," +
                "tostring(C_TradeSkillUI.IsRecipeFirstCraft(1))," +
                "tostring(C_TradeSkillUI.IsRecipeInBaseSkillLine(102))," +
                "tostring(C_TradeSkillUI.IsRecipeInBaseSkillLine(1))," +
                "tostring(C_TradeSkillUI.IsRecipeInSkillLine(103,171))," +
                "tostring(C_TradeSkillUI.IsRecipeInSkillLine(103,164))," +
                "tostring(C_TradeSkillUI.IsRecipeProfessionLearned(104))," +
                "tostring(C_TradeSkillUI.IsRecipeProfessionLearned(1))," +
                "tostring(C_TradeSkillUI.IsRecipeTracked(105,false))," +
                "tostring(C_TradeSkillUI.IsRecipeTracked(105,true))," +
                "tostring(C_TradeSkillUI.IsRecipeTracked(106,true))," +
                "tostring(C_TradeSkillUI.IsRecraftItemEquipped('Item-2'))," +
                "tostring(C_TradeSkillUI.IsRuneforging())," +
                "tostring(C_TradeSkillUI.IsEnchantTargetValid(" +
                "107,'Item-3',{{itemID=55},{currencyID=66}}))," +
                "tostring(C_TradeSkillUI.IsRecraftReagentValid(" +
                "'Item-4',{itemID=77,currencyID=88}))," +
                "tostring(C_TradeSkillUI.RecraftLimitCategoryValid(" +
                "{itemID=99}))},':')"));
    }

    [Fact]
    public void EnforcesRecoveredRecipePredicateParsersAndDefaults()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:true:false:false:false:true:false:" +
            "false:true:false:false:true:false:false:false:false:" +
            "false:true:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_TradeSkillUI.IsNearProfessionSpellFocus))," +
                "tostring(ok(C_TradeSkillUI.IsNearProfessionSpellFocus,15))," +
                "tostring(ok(C_TradeSkillUI.IsOriginalCraftRecipeLearned,1))," +
                "tostring(ok(C_TradeSkillUI.IsOriginalCraftRecipeLearned,'x'))," +
                "tostring(ok(C_TradeSkillUI.IsRecipeFirstCraft))," +
                "tostring(ok(C_TradeSkillUI.IsRecipeInSkillLine,1))," +
                "tostring(ok(C_TradeSkillUI.IsRecipeTracked,1))," +
                "tostring(ok(C_TradeSkillUI.IsRecipeTracked,1,0))," +
                "tostring(ok(C_TradeSkillUI.IsRecraftItemEquipped,{}))," +
                "tostring(ok(C_TradeSkillUI.IsRecraftReagentValid,'x'))," +
                "tostring(ok(C_TradeSkillUI.IsRecraftReagentValid," +
                "'x',{}))," +
                "tostring(ok(C_TradeSkillUI.RecraftLimitCategoryValid,1))," +
                "tostring(ok(C_TradeSkillUI.IsEnchantTargetValid," +
                "1,'x',1))," +
                "tostring(ok(C_TradeSkillUI.IsEnchantTargetValid," +
                "1,'x',{}))," +
                "tostring(C_TradeSkillUI.HasFavoriteOrderRecipes())," +
                "tostring(C_TradeSkillUI.IsRecipeFirstCraft(1))," +
                "tostring(C_TradeSkillUI.IsRecipeTracked(1,false))," +
                "tostring(C_TradeSkillUI.IsRecraftItemEquipped('x'))," +
                "tostring(C_TradeSkillUI.IsRuneforging())," +
                "tostring(C_TradeSkillUI.IsRecraftReagentValid('x',{}))," +
                "tostring(C_TradeSkillUI.RecraftLimitCategoryValid({}))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredRecraftSalvageAndGearLookups()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.RecraftItemGuids.Add("Item-A");
        tradeSkill.RecraftItemGuids.Add("Item-B");
        tradeSkill.RecraftItemGuidsByRecipeId[501] = ["Item-C"];
        tradeSkill.RemainingRecasts = 4;
        tradeSkill.SalvageableItemIdsByRecipeId[502] = [1001, 1002];
        tradeSkill.SkillLineForGearByItemId[1003] = 164;
        tradeSkill.TradeSkillDisplayNames[164] = "Blacksmithing";
        tradeSkill.RecraftRemovalWarningProvider =
            (guid, reagents) =>
                guid == "Item-D" &&
                reagents.SequenceEqual(
                    [
                        new WowCraftingReagentInfo(1, null),
                        new WowCraftingReagentInfo(null, 2)
                    ])
                    ? ["Warning one", "Warning two"]
                    : [];

        Assert.Equal(
            "2:Item-A:Item-B:1:Item-C:2:Warning one:Warning two:" +
            "4:2:1001:1002:164:Blacksmithing:1:0",
            session.Lua.Evaluate(
                "local all=C_TradeSkillUI.GetRecraftItems();" +
                "local one=C_TradeSkillUI.GetRecraftItems(501);" +
                "local warnings=" +
                "C_TradeSkillUI.GetRecraftRemovalWarnings(" +
                "'Item-D',{{itemID=1},{currencyID=2}});" +
                "local salvage=" +
                "C_TradeSkillUI.GetSalvagableItemIDs(502);" +
                "return table.concat({" +
                "#all,all[1],all[2],#one,one[1]," +
                "#warnings,warnings[1],warnings[2]," +
                "C_TradeSkillUI.GetRemainingRecasts()," +
                "#salvage,salvage[1],salvage[2]," +
                "C_TradeSkillUI.GetSkillLineForGear('item:1003')," +
                "C_TradeSkillUI.GetTradeSkillDisplayName(164)," +
                "select('#',C_TradeSkillUI.GetTradeSkillDisplayName(0))," +
                "#C_TradeSkillUI.GetSalvagableItemIDs(0)" +
                "},':')"));
    }

    [Fact]
    public void EnforcesRecoveredRecraftAndGearParsersAndNeutralShapes()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:false:false:false:true:false:false:false:true:" +
            "0:0:0:1:true",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_TradeSkillUI.GetRecraftItems))," +
                "tostring(ok(C_TradeSkillUI.GetRecraftItems,{}))," +
                "tostring(ok(C_TradeSkillUI.GetRecraftRemovalWarnings," +
                "'x'))," +
                "tostring(ok(C_TradeSkillUI.GetRecraftRemovalWarnings," +
                "'x',nil))," +
                "tostring(ok(C_TradeSkillUI.GetRecraftRemovalWarnings," +
                "'x',{}))," +
                "tostring(ok(C_TradeSkillUI.GetSalvagableItemIDs))," +
                "tostring(ok(C_TradeSkillUI.GetSkillLineForGear))," +
                "tostring(ok(C_TradeSkillUI.GetTradeSkillDisplayName))," +
                "tostring(ok(C_TradeSkillUI.GetTradeSkillDisplayName,0))," +
                "#C_TradeSkillUI.GetRecraftItems()," +
                "#C_TradeSkillUI.GetRecraftRemovalWarnings('x',{})," +
                "C_TradeSkillUI.GetRemainingRecasts()," +
                "select('#',C_TradeSkillUI.GetSkillLineForGear(1))," +
                "tostring(C_TradeSkillUI.GetSkillLineForGear(1)==nil)" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredTradeSkillOpeningAndFilterState()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.OpenableTradeSkillLineIds.Add(171);
        tradeSkill.SelectableProfessionChildSkillLineIds.Add(2871);

        Assert.Equal(
            "0:true:false:0:0:0:true:2871",
            session.Lua.Evaluate(
                "local openRecipeResults=" +
                "select('#',C_TradeSkillUI.OpenRecipe(445466));" +
                "local opened=C_TradeSkillUI.OpenTradeSkill(171);" +
                "local notOpened=C_TradeSkillUI.OpenTradeSkill(164);" +
                "local setFilterResults=select('#'," +
                "C_TradeSkillUI.SetOnlyShowAvailableForOrders(true));" +
                "local rejectedResults=select('#'," +
                "C_TradeSkillUI.SetProfessionChildSkillLineID(999));" +
                "local acceptedResults=select('#'," +
                "C_TradeSkillUI.SetProfessionChildSkillLineID(2871));" +
                "return table.concat({" +
                "openRecipeResults,tostring(opened)," +
                "tostring(notOpened),setFilterResults,rejectedResults," +
                "acceptedResults,tostring(true)," +
                "C_TradeSkillUI.GetProfessionChildSkillLineID()},':')"));

        Assert.Equal([445466], tradeSkill.OpenRecipeRequests);
        Assert.Equal([171, 164], tradeSkill.OpenTradeSkillRequests);
        Assert.True(tradeSkill.OnlyShowAvailableForOrders);
        Assert.Equal(2871, tradeSkill.ProfessionChildSkillLineId);
    }

    [Fact]
    public void EnforcesRecoveredTradeSkillOpeningAndFilterParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:true:false:true:false:true",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_TradeSkillUI.OpenRecipe))," +
                "tostring(ok(C_TradeSkillUI.OpenRecipe,1))," +
                "tostring(ok(C_TradeSkillUI.OpenTradeSkill))," +
                "tostring(ok(C_TradeSkillUI.OpenTradeSkill,1))," +
                "tostring(ok(" +
                "C_TradeSkillUI.SetOnlyShowAvailableForOrders))," +
                "tostring(ok(" +
                "C_TradeSkillUI.SetOnlyShowAvailableForOrders,0))," +
                "tostring(ok(" +
                "C_TradeSkillUI.SetProfessionChildSkillLineID))," +
                "tostring(ok(" +
                "C_TradeSkillUI.SetProfessionChildSkillLineID,1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredCraftAndRecraftRequests()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.RecraftRecipeProvider =
            request =>
                request.ItemGuid == "Item-1" &&
                request.ApplyConcentration == true;
        tradeSkill.RecraftRecipeForOrderProvider =
            request =>
                request.OrderId == 9001 &&
                request.ItemGuid == "Item-2";

        Assert.Equal(
            "0:0:0:true:true",
            session.Lua.Evaluate(
                "local a=select('#',C_TradeSkillUI.CraftEnchant(" +
                "101,nil,{{itemID=11}}," +
                "{bagID=2,slotIndex=3},false));" +
                "local b=select('#',C_TradeSkillUI.CraftRecipe(" +
                "102,4,{{currencyID=12}},3,8001,true));" +
                "local c=select('#',C_TradeSkillUI.CraftSalvage(" +
                "103,nil,{equipmentSlotIndex=5},nil,false));" +
                "local d=C_TradeSkillUI.RecraftRecipe(" +
                "'Item-1',{{itemID=13}}," +
                "{{dataSlotIndex=2,reagent={currencyID=14}}},true);" +
                "local e=C_TradeSkillUI.RecraftRecipeForOrder(" +
                "9001,'Item-2',nil,{},false);" +
                "return table.concat({" +
                "a,b,c,tostring(d),tostring(e)},':')"));

        var enchant = Assert.Single(tradeSkill.CraftEnchantRequests);
        Assert.Equal(101, enchant.RecipeSpellId);
        Assert.Equal(1u, enchant.NumCasts);
        Assert.Equal(
            [new WowCraftingReagentInfo(11, null)],
            enchant.CraftingReagents);
        Assert.Equal(WowItemLocation.Bag(2, 3), enchant.ItemTarget);
        Assert.False(enchant.ApplyConcentration);

        var craft = Assert.Single(tradeSkill.CraftRecipeRequests);
        Assert.Equal(102, craft.RecipeSpellId);
        Assert.Equal(4u, craft.NumCasts);
        Assert.Equal(2, craft.RecipeLevelIndex);
        Assert.Equal(8001ul, craft.OrderId);
        Assert.True(craft.ApplyConcentration);

        var salvage = Assert.Single(tradeSkill.CraftSalvageRequests);
        Assert.Equal(WowItemLocation.Equipment(5), salvage.ItemTarget);
        Assert.Null(salvage.CraftingReagents);

        var recraft = Assert.Single(tradeSkill.RecraftRecipeRequests);
        Assert.Equal(
            [
                new WowCraftingItemSlotModification(
                    1,
                    new WowCraftingReagentInfo(null, 14))
            ],
            recraft.RemovedModifications);

        var order =
            Assert.Single(tradeSkill.RecraftRecipeForOrderRequests);
        Assert.Null(order.CraftingReagents);
        Assert.Empty(order.RemovedModifications!);
    }

    [Fact]
    public void EnforcesRecoveredCraftAndRecraftParsersAndDefaults()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:false:true:false:false:true:false:false:" +
            "true:false:false:true:false:false:true:false:false:true:" +
            "true:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_TradeSkillUI.CraftEnchant))," +
                "tostring(ok(C_TradeSkillUI.CraftEnchant,1))," +
                "tostring(ok(C_TradeSkillUI.CraftEnchant,1,-1))," +
                "tostring(ok(C_TradeSkillUI.CraftEnchant,1,1,1))," +
                "tostring(ok(C_TradeSkillUI.CraftEnchant,1,nil,nil,nil))," +
                "tostring(ok(C_TradeSkillUI.CraftRecipe))," +
                "tostring(ok(C_TradeSkillUI.CraftRecipe,1,1,{},0))," +
                "tostring(ok(C_TradeSkillUI.CraftRecipe,1,nil,nil,nil,nil,nil))," +
                "tostring(ok(C_TradeSkillUI.CraftSalvage,1))," +
                "tostring(ok(C_TradeSkillUI.CraftSalvage,1,nil,nil))," +
                "tostring(ok(C_TradeSkillUI.CraftSalvage,1,nil," +
                "{bagID=0,slotIndex=1}))," +
                "tostring(ok(C_TradeSkillUI.RecraftRecipe))," +
                "tostring(ok(C_TradeSkillUI.RecraftRecipe,1))," +
                "tostring(ok(C_TradeSkillUI.RecraftRecipe,'x'))," +
                "tostring(ok(C_TradeSkillUI.RecraftRecipe," +
                "'x',{},{{dataSlotIndex=0,reagent={}}}))," +
                "tostring(ok(C_TradeSkillUI.RecraftRecipeForOrder))," +
                "tostring(ok(C_TradeSkillUI.RecraftRecipeForOrder," +
                "1,'x'))," +
                "tostring(ok(C_TradeSkillUI.RecraftRecipeForOrder," +
                "-1,'x'))," +
                "tostring(ok(C_TradeSkillUI.RecraftRecipeForOrder," +
                "1,1))," +
                "tostring(C_TradeSkillUI.RecraftRecipe('x')==false)," +
                "tostring(C_TradeSkillUI.RecraftRecipeForOrder(" +
                "1,'x')==false)," +
                "tostring(ok(C_TradeSkillUI.CraftRecipe,1,1,{},1,-1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredTradeSkillItemAndProfessionLookups()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        var location = WowItemLocation.Bag(2, 3);
        tradeSkill.RecraftRecipeIdsByItemLocation[location] = 445466;
        tradeSkill.FactionSpecificOutputItemIds[101] = 202;
        tradeSkill.OriginalCraftRecipeIdsByItemGuid["Item-1"] =
            (303, 404);
        tradeSkill.ProfessionsByInventorySlotIndex[19] = 4;
        tradeSkill.ProfessionForCursorItem = 10;
        tradeSkill.ProfessionInventorySlots.Add(20);
        tradeSkill.ProfessionInventorySlots.Add(21);
        tradeSkill.ProfessionNamesBySkillLineAbilityId[404] =
            "Blacksmithing";

        Assert.Equal(
            "true:false:202:1:true:303:404:2:true:4:10:2:20:21:" +
            "Blacksmithing:1:true",
            session.Lua.Evaluate(
                "local recipe,ability=" +
                "C_TradeSkillUI.GetOriginalCraftRecipeID('Item-1');" +
                "local missingRecipe,missingAbility=" +
                "C_TradeSkillUI.GetOriginalCraftRecipeID('Item-X');" +
                "local slots=" +
                "C_TradeSkillUI.GetProfessionInventorySlots();" +
                "return table.concat({" +
                "tostring(C_TradeSkillUI." +
                "DoesRecraftingRecipeAcceptItem(" +
                "{bagID=2,slotIndex=3},445466))," +
                "tostring(C_TradeSkillUI." +
                "DoesRecraftingRecipeAcceptItem(" +
                "{bagID=2,slotIndex=3},1))," +
                "C_TradeSkillUI.GetFactionSpecificOutputItem(101)," +
                "select('#'," +
                "C_TradeSkillUI.GetFactionSpecificOutputItem(0))," +
                "tostring(C_TradeSkillUI." +
                "GetFactionSpecificOutputItem(0)==nil)," +
                "recipe,ability," +
                "select('#',missingRecipe,missingAbility)," +
                "tostring(missingRecipe==nil and missingAbility==nil)," +
                "C_TradeSkillUI.GetProfessionByInventorySlot(20)," +
                "C_TradeSkillUI.GetProfessionForCursorItem()," +
                "#slots,slots[1],slots[2]," +
                "C_TradeSkillUI." +
                "GetProfessionNameForSkillLineAbility(404)," +
                "select('#',C_TradeSkillUI." +
                "GetProfessionNameForSkillLineAbility(0))," +
                "tostring(C_TradeSkillUI." +
                "GetProfessionNameForSkillLineAbility(0)==nil)" +
                "},':')"));
    }

    [Fact]
    public void EnforcesRecoveredTradeSkillItemAndProfessionParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:true:false:true:false:true:false:false:true:" +
            "true:false:true:0:2:true:true",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "local a,b=" +
                "C_TradeSkillUI.GetOriginalCraftRecipeID('x');" +
                "return table.concat({" +
                "tostring(ok(C_TradeSkillUI." +
                "DoesRecraftingRecipeAcceptItem))," +
                "tostring(ok(C_TradeSkillUI." +
                "DoesRecraftingRecipeAcceptItem,{},1))," +
                "tostring(ok(C_TradeSkillUI." +
                "DoesRecraftingRecipeAcceptItem," +
                "{bagID=0,slotIndex=1},1))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetFactionSpecificOutputItem))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetFactionSpecificOutputItem,1))," +
                "tostring(ok(C_TradeSkillUI.GetOriginalCraftRecipeID,1))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetOriginalCraftRecipeID,'x'))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetProfessionByInventorySlot))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetProfessionByInventorySlot,0))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetProfessionByInventorySlot,1))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetProfessionForCursorItem))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetProfessionNameForSkillLineAbility))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetProfessionNameForSkillLineAbility,1))," +
                "#C_TradeSkillUI.GetProfessionInventorySlots()," +
                "select('#',a,b),tostring(a==nil),tostring(b==nil)" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredProfessionInfoQueriesAndNeutralTables()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.ProfessionInfosByRecipeId[101] =
            new WowProfessionInfo(
                Profession: 4,
                ProfessionId: 164,
                ProfessionName: "Blacksmithing");
        tradeSkill.ProfessionInfosBySkillLineId[2871] =
            new WowProfessionInfo(
                Profession: 7,
                ProfessionId: 171,
                ProfessionName: "Alchemy",
                ExpansionName: "Dragon Isles",
                SkillLevel: 42);

        Assert.Equal(
            "4:164:Blacksmithing:7:171:Alchemy:Dragon Isles:42:" +
            "table:0::0:true:false",
            session.Lua.Evaluate(
                "local byRecipe=" +
                "C_TradeSkillUI.GetProfessionInfoByRecipeID(101);" +
                "local byLine=" +
                "C_TradeSkillUI.GetProfessionInfoBySkillLineID(2871);" +
                "local missing=" +
                "C_TradeSkillUI.GetProfessionInfoByRecipeID(0);" +
                "return table.concat({" +
                "byRecipe.profession,byRecipe.professionID," +
                "byRecipe.professionName,byLine.profession," +
                "byLine.professionID,byLine.professionName," +
                "byLine.expansionName,byLine.skillLevel," +
                "type(missing),missing.professionID," +
                "missing.professionName,missing.skillLevel," +
                "tostring(missing.profession==nil)," +
                "tostring(pcall(" +
                "C_TradeSkillUI.GetProfessionInfoByRecipeID))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredReagentQualityAndRecipeTextLookups()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.ItemReagentQualitiesByItemId[1001] = 3;
        tradeSkill.ItemReagentQualityInfosByItemId[1001] =
            new WowItemReagentQualityInfo(
                Quality: 3,
                Icon: "Professions-Icon-Quality-Tier3",
                IconSmall: "Professions-Icon-Quality-Tier3-Small",
                BarFill: "Professions-Specialization-Bar-Fill");
        tradeSkill.QualityIdsByRecipeId[101] = [1, 2, 3];
        tradeSkill.ReagentSlotStatuses[(7, 101, 202)] =
            (true, "Requires a specialization");
        tradeSkill.QualityItemIdsByRecipeId[101] = [1001, 1002];
        tradeSkill.RecipeQualityReagentLinks[(101, 1, 2)] =
            "|cff0070dd|Hitem:1001|h[Reagent]|h|r";
        tradeSkill.ReagentDifficultyTextProvider =
            (index, reagents) =>
                index == 1 &&
                reagents.SequenceEqual(
                    [new WowCraftingReagentInfo(1001, null)])
                    ? "Difficulty bonus"
                    : string.Empty;
        tradeSkill.RecipeDescriptionProvider =
            (recipeId, reagents, guid) =>
                recipeId == 101 &&
                reagents.Count == 1 &&
                guid == "Item-1"
                    ? "Crafts a test item."
                    : string.Empty;

        Assert.Equal(
            "3:3:Professions-Icon-Quality-Tier3:" +
            "Professions-Icon-Quality-Tier3-Small:" +
            "Professions-Specialization-Bar-Fill:true:" +
            "3:1:2:3:true:Difficulty bonus:true:" +
            "Requires a specialization:Crafts a test item.:" +
            "2:1001:1002:true:" +
            "|cff0070dd|Hitem:1001|h[Reagent]|h|r:true",
            session.Lua.Evaluate(
                "local info=" +
                "C_TradeSkillUI.GetItemReagentQualityInfo(1001);" +
                "local qualities=" +
                "C_TradeSkillUI.GetQualitiesForRecipe(101);" +
                "local locked,reason=" +
                "C_TradeSkillUI.GetReagentSlotStatus(7,101,202);" +
                "local items=" +
                "C_TradeSkillUI.GetRecipeQualityItemIDs(101);" +
                "return table.concat({" +
                "C_TradeSkillUI." +
                "GetItemReagentQualityByItemInfo(1001)," +
                "info.quality,info.icon,info.iconSmall,info.barFill," +
                "tostring(info.iconChat==nil)," +
                "#qualities,qualities[1],qualities[2],qualities[3]," +
                "tostring(" +
                "C_TradeSkillUI.GetQualitiesForRecipe(0)==nil)," +
                "C_TradeSkillUI.GetReagentDifficultyText(" +
                "2,{{itemID=1001}})," +
                "tostring(locked),reason," +
                "C_TradeSkillUI.GetRecipeDescription(" +
                "101,{{itemID=1001}},'Item-1')," +
                "#items,items[1],items[2]," +
                "tostring(" +
                "C_TradeSkillUI.GetRecipeQualityItemIDs(0)==nil)," +
                "C_TradeSkillUI.GetRecipeQualityReagentLink(101,2,3)," +
                "tostring(C_TradeSkillUI." +
                "GetRecipeQualityReagentLink(0,1,1)==nil)" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredCraftedItemQualityLookups()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.ItemCraftedQualitiesByItemId[2001] = 5;
        tradeSkill.ItemCraftedQualityInfosByItemId[2001] =
            new WowItemReagentQualityInfo(
                Quality: 5,
                Icon: "Professions-Icon-Quality-Tier5",
                IconSmall: "Professions-Icon-Quality-Tier5-Small",
                IconInventory:
                    "Professions-Icon-Quality-Tier5-Inv",
                IconChat: "Professions-Icon-Quality-Tier5-Chat");
        tradeSkill.ItemReagentQualitiesByItemId[2001] = 2;

        Assert.Equal(
            "5:5:Professions-Icon-Quality-Tier5:" +
            "Professions-Icon-Quality-Tier5-Small:" +
            "Professions-Icon-Quality-Tier5-Inv:" +
            "Professions-Icon-Quality-Tier5-Chat:2:true:true",
            session.Lua.Evaluate(
                "local info=" +
                "C_TradeSkillUI.GetItemCraftedQualityInfo(2001);" +
                "return table.concat({" +
                "C_TradeSkillUI." +
                "GetItemCraftedQualityByItemInfo(2001)," +
                "info.quality,info.icon,info.iconSmall," +
                "info.iconInventory,info.iconChat," +
                "C_TradeSkillUI." +
                "GetItemReagentQualityByItemInfo(2001)," +
                "tostring(C_TradeSkillUI." +
                "GetItemCraftedQualityByItemInfo(0)==nil)," +
                "tostring(C_TradeSkillUI." +
                "GetItemCraftedQualityInfo(0)==nil)" +
                "},':')"));
    }

    [Fact]
    public void EnforcesRecoveredCraftedItemQualityParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemCraftedQualityByItemInfo))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemCraftedQualityByItemInfo,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemCraftedQualityInfo))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemCraftedQualityInfo,1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredItemSlotModificationLookups()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.ItemSlotModificationsByItemGuid["Item-1"] =
        [
            new WowCraftingItemSlotModification(
                0,
                new WowCraftingReagentInfo(ItemId: 1001)),
            new WowCraftingItemSlotModification(
                14,
                new WowCraftingReagentInfo(CurrencyId: 2001))
        ];
        tradeSkill.ItemSlotModificationsByOrderId[42] =
        [
            new WowCraftingItemSlotModification(
                3,
                new WowCraftingReagentInfo(
                    ItemId: 1002,
                    CurrencyId: 2002))
        ];

        Assert.Equal(
            "2:1:1001:true:15:true:2001:" +
            "1:4:1002:2002:0:0",
            session.Lua.Evaluate(
                "local item=" +
                "C_TradeSkillUI.GetItemSlotModifications('Item-1');" +
                "local order=" +
                "C_TradeSkillUI." +
                "GetItemSlotModificationsForOrder(42);" +
                "return table.concat({" +
                "#item,item[1].dataSlotIndex," +
                "item[1].reagent.itemID," +
                "tostring(item[1].reagent.currencyID==nil)," +
                "item[2].dataSlotIndex," +
                "tostring(item[2].reagent.itemID==nil)," +
                "item[2].reagent.currencyID," +
                "#order,order[1].dataSlotIndex," +
                "order[1].reagent.itemID," +
                "order[1].reagent.currencyID," +
                "#C_TradeSkillUI." +
                "GetItemSlotModifications('Missing')," +
                "#C_TradeSkillUI." +
                "GetItemSlotModificationsForOrder(0)" +
                "},':')"));
    }

    [Fact]
    public void EnforcesRecoveredItemSlotModificationParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:true:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemSlotModifications))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemSlotModifications,'Item-1'))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemSlotModificationsForOrder))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemSlotModificationsForOrder,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetItemSlotModificationsForOrder,-1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredRecipeQualityAndOutputItemData()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.RecipeItemQualityInfos[(101, 4)] =
            new WowItemReagentQualityInfo(
                Quality: 4,
                Icon: "Professions-Icon-Quality-Tier4",
                BarHighlight: "Professions-Bar-Highlight");
        WowRecipeOutputItemDataRequest? captured = null;
        tradeSkill.RecipeOutputItemDataProvider = request =>
        {
            captured = request;
            return new WowRecipeOutputItemData(
                136243,
                "|cffa335ee|Hitem:2001|h[Output]|h|r",
                2001);
        };

        Assert.Equal(
            "4:Professions-Icon-Quality-Tier4:" +
            "Professions-Bar-Highlight:true:136243:" +
            "|cffa335ee|Hitem:2001|h[Output]|h|r:2001",
            session.Lua.Evaluate(
                "local quality=" +
                "C_TradeSkillUI.GetRecipeItemQualityInfo(101,4);" +
                "local output=" +
                "C_TradeSkillUI.GetRecipeOutputItemData(" +
                "101,{{itemID=1001}},'Item-1',4,42);" +
                "return table.concat({" +
                "quality.quality,quality.icon," +
                "quality.barHighlight," +
                "tostring(C_TradeSkillUI." +
                "GetRecipeItemQualityInfo(101,3)==nil)," +
                "output.icon,output.hyperlink,output.itemID" +
                "},':')"));

        Assert.NotNull(captured);
        Assert.Equal(101, captured.RecipeSpellId);
        Assert.Equal(
            [new WowCraftingReagentInfo(ItemId: 1001)],
            captured.CraftingReagents);
        Assert.Equal("Item-1", captured.AllocationItemGuid);
        Assert.Equal(4, captured.OverrideQualityId);
        Assert.Equal(42UL, captured.RecraftOrderId);
    }

    [Fact]
    public void EnforcesRecoveredRecipeQualityAndOutputParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:true:false:true:true:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeItemQualityInfo))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeItemQualityInfo,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeItemQualityInfo,1,2))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeOutputItemData))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeOutputItemData,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeOutputItemData,1,nil,nil,nil,nil))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeOutputItemData,1,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeOutputItemData,1,{},nil,nil,-1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredRecipeInfoTablesAndIndices()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        var info = new WowTradeSkillRecipeInfo(
            CategoryId: 10,
            Name: "Test Recipe",
            RelativeDifficulty: 2,
            MaxTrivialLevel: 100,
            ItemLevel: 610,
            AlternateVerb: "Forge",
            NumSkillUps: 3,
            CanSkillUp: true,
            FirstCraft: true,
            SourceType: 7,
            Learned: true,
            Favorite: true,
            SupportsQualities: true,
            Craftable: true,
            RecipeId: 101,
            SkillLineAbilityId: 202,
            PreviousRecipeId: 100,
            NextRecipeId: 102,
            Icon: 136243,
            Hyperlink: "|Hspell:101|h[Test Recipe]|h",
            CurrentRecipeExperience: 5,
            NextLevelRecipeExperience: 10,
            UnlockedRecipeLevel: 2,
            EarnedExperience: 4,
            SupportsCraftingStats: true,
            HasSingleItemOutput: true,
            QualityItemIds: [1001, 1002],
            QualityItemLevelBonuses: [0, 3],
            MaxQuality: 5,
            QualityIds: [1, 2, 3, 4, 5],
            CanCreateMultiple: true,
            AbilityVerb: "Craft",
            AbilityAllVerb: "Craft All",
            IsEnchantingRecipe: true);
        tradeSkill.RecipeInfos[(101, 1)] = info;
        tradeSkill.RecipeInfosBySkillLineAbilityId[(202, null)] = info;

        Assert.Equal(
            "40:10:Test Recipe:2:100:610:Forge:3:" +
            "true:true:7:true:false:true:true:true:true:" +
            "101:202:100:102:136243:" +
            "|Hspell:101|h[Test Recipe]|h:5:10:2:4:" +
            "true:true:2:1001:2:3:false:5:5:5:true:" +
            "Craft:Craft All:false:false:false:true:false:" +
            "Test Recipe:true",
            session.Lua.Evaluate(
                "local function count(t) local n=0;" +
                "for _ in pairs(t) do n=n+1 end;return n end;" +
                "local i=C_TradeSkillUI.GetRecipeInfo(101,2);" +
                "local s=C_TradeSkillUI." +
                "GetRecipeInfoForSkillLineAbility(202);" +
                "return table.concat({" +
                "count(i),i.categoryID,i.name,i.relativeDifficulty," +
                "i.maxTrivialLevel,i.itemLevel,i.alternateVerb," +
                "i.numSkillUps,tostring(i.canSkillUp)," +
                "tostring(i.firstCraft),i.sourceType," +
                "tostring(i.learned),tostring(i.disabled)," +
                "tostring(i.favorite)," +
                "tostring(i.supportsQualities)," +
                "tostring(i.craftable)," +
                "tostring(i.disabledReason==nil),i.recipeID," +
                "i.skillLineAbilityID,i.previousRecipeID," +
                "i.nextRecipeID,i.icon,i.hyperlink," +
                "i.currentRecipeExperience," +
                "i.nextLevelRecipeExperience," +
                "i.unlockedRecipeLevel,i.earnedExperience," +
                "tostring(i.supportsCraftingStats)," +
                "tostring(i.hasSingleItemOutput)," +
                "#i.qualityItemIDs,i.qualityItemIDs[1]," +
                "#i.qualityIlvlBonuses," +
                "i.qualityIlvlBonuses[2]," +
                "tostring(i.alwaysUsesLowestQuality)," +
                "i.maxQuality,#i.qualityIDs,i.qualityIDs[5]," +
                "tostring(i.canCreateMultiple),i.abilityVerb," +
                "i.abilityAllVerb,tostring(i.isRecraft)," +
                "tostring(i.isDummyRecipe)," +
                "tostring(i.isGatheringRecipe)," +
                "tostring(i.isEnchantingRecipe)," +
                "tostring(i.isSalvageRecipe),s.name," +
                "tostring(C_TradeSkillUI.GetRecipeInfo(0)==nil)" +
                "},':')"));
    }

    [Fact]
    public void EnforcesRecoveredRecipeInfoParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:true:false:true:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_TradeSkillUI.GetRecipeInfo))," +
                "tostring(pcall(C_TradeSkillUI.GetRecipeInfo,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeInfo,1,0))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeInfo,1,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeInfoForSkillLineAbility))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeInfoForSkillLineAbility,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeInfoForSkillLineAbility,1,0))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeInfoForSkillLineAbility,1,1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredCraftingTargetsAndGatheringInfo()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.CraftingReagentBonusTextProvider =
            (recipeId, index, reagents, guid) =>
                recipeId == 101 &&
                index == 1 &&
                reagents.Count == 1 &&
                guid == "Item-1"
                    ? ["+10 Skill", "+5 Quality"]
                    : [];
        tradeSkill.CraftingTargetItemsProvider = itemIds =>
            itemIds.SequenceEqual([1001, 1002])
                ?
                [
                    new WowCraftingTargetItem(
                        1001,
                        "Item-1",
                        "|Hitem:1001|h[Target]|h",
                        3)
                ]
                : [];
        tradeSkill.DependentReagentsProvider = reagent =>
            reagent.ItemId == 1001
                ?
                [
                    new WowCraftingReagentInfo(ItemId: 1002),
                    new WowCraftingReagentInfo(CurrencyId: 2001)
                ]
                : [];
        tradeSkill.EnchantItemsProvider = (recipeId, reagents) =>
            recipeId == 101 &&
            reagents is { Count: 1 }
                ? ["Item-1", "Item-2"]
                : [];
        tradeSkill.GatheringOperationInfosByRecipeId[101] =
            new WowGatheringOperationInfo(
                201,
                100,
                80,
                12,
                [
                    new WowCraftingOperationBonusStatInfo(
                        "Perception",
                        25,
                        "5% chance",
                        5.5f,
                        1.25f)
                ]);

        Assert.Equal(
            "2:+10 Skill:+5 Quality:1:1001:Item-1:" +
            "|Hitem:1001|h[Target]|h:3:2:1002:true:" +
            "true:2001:2:Item-1:Item-2:201:100:80:12:" +
            "1:Perception:25:5% chance:5.5:1.25:true",
            session.Lua.Evaluate(
                "local bonus=C_TradeSkillUI." +
                "GetCraftingReagentBonusText(" +
                "101,2,{{itemID=1001}},'Item-1');" +
                "local targets=C_TradeSkillUI." +
                "GetCraftingTargetItems({1001,1002});" +
                "local deps=C_TradeSkillUI." +
                "GetDependentReagents({itemID=1001});" +
                "local ench=C_TradeSkillUI." +
                "GetEnchantItems(101,{{itemID=1001}});" +
                "local gather=C_TradeSkillUI." +
                "GetGatheringOperationInfo(101);" +
                "local stat=gather.bonusStats[1];" +
                "return table.concat({" +
                "#bonus,bonus[1],bonus[2],#targets," +
                "targets[1].itemID,targets[1].itemGUID," +
                "targets[1].hyperlink,targets[1].quantity," +
                "#deps,deps[1].itemID," +
                "tostring(deps[1].currencyID==nil)," +
                "tostring(deps[2].itemID==nil)," +
                "deps[2].currencyID,#ench,ench[1],ench[2]," +
                "gather.spellID,gather.maxDifficulty," +
                "gather.baseSkill,gather.bonusSkill," +
                "#gather.bonusStats,stat.bonusStatName," +
                "stat.bonusStatValue,stat.ratingDescription," +
                "stat.ratingPct,stat.bonusRatingPct," +
                "tostring(C_TradeSkillUI." +
                "GetGatheringOperationInfo(0)==nil)" +
                "},':')"));
    }

    [Fact]
    public void EnforcesRecoveredCraftingTargetAndGatheringParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:true:false:true:false:true:" +
            "false:true:true:false:true:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingReagentBonusText,1,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingReagentBonusText,1,0,{}))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingReagentBonusText,1,1,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingReagentBonusText,1,1,{}))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingTargetItems))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingTargetItems,{}))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetDependentReagents))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetDependentReagents,{itemID=1}))," +
                "tostring(pcall(C_TradeSkillUI.GetEnchantItems))," +
                "tostring(pcall(C_TradeSkillUI.GetEnchantItems,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetEnchantItems,1,nil))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetEnchantItems,1,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetEnchantItems,1,{}))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetGatheringOperationInfo))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetGatheringOperationInfo,1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredRecipeRequirementsAndSchematic()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        tradeSkill.RecipeRequirementsByRecipeId[101] =
        [
            new WowCraftingRecipeRequirement(
                "Requires Forge",
                true,
                1),
            new WowCraftingRecipeRequirement(null, false, 3)
        ];
        tradeSkill.RecipeSchematics[(101, true, 1)] =
            new WowCraftingRecipeSchematic(
                RecipeId: 101,
                Icon: 136243,
                QuantityMin: 1,
                QuantityMax: 2,
                Name: "Test Schematic",
                RecipeType: 2,
                ProductQuality: 4,
                OutputItemId: 2001,
                ReagentSlotSchematics:
                [
                    new WowCraftingReagentSlotSchematic(
                        Reagents:
                        [
                            new WowCraftingReagentInfo(
                                ItemId: 1001)
                        ],
                        ReagentType: 1,
                        VariableQuantities:
                        [
                            new WowCraftingReagentQuantity(
                                new WowCraftingReagentInfo(
                                    CurrencyId: 3001),
                                5)
                        ],
                        QuantityRequired: 3,
                        SlotInfo: new WowCraftingReagentSlotInfo(
                            7,
                            50,
                            "Optional reagent"),
                        DataSlotType: 2,
                        DataSlotIndex: 0,
                        SlotIndex: 2,
                        OrderSource: 1,
                        Required: true,
                        HiddenInCraftingForm: false)
                ],
                IsRecraft: true,
                HasCraftingOperationInfo: true);

        Assert.Equal(
            "2:Requires Forge:true:1:true:false:3:" +
            "101:136243:1:2:Test Schematic:2:4:2001:" +
            "1:1:1001:1:1:true:3001:5:3:7:50:" +
            "Optional reagent:2:1:3:1:true:false:" +
            "true:true:0:0",
            session.Lua.Evaluate(
                "local req=C_TradeSkillUI." +
                "GetRecipeRequirements(101);" +
                "local s=C_TradeSkillUI." +
                "GetRecipeSchematic(101,true,2);" +
                "local slot=s.reagentSlotSchematics[1];" +
                "local var=slot.variableQuantities[1];" +
                "local missing=C_TradeSkillUI." +
                "GetRecipeSchematic(0,false);" +
                "return table.concat({" +
                "#req,req[1].name,tostring(req[1].met)," +
                "req[1].type,tostring(req[2].name==nil)," +
                "tostring(req[2].met),req[2].type," +
                "s.recipeID,s.icon,s.quantityMin,s.quantityMax," +
                "s.name,s.recipeType,s.productQuality," +
                "s.outputItemID,#s.reagentSlotSchematics," +
                "#slot.reagents,slot.reagents[1].itemID," +
                "slot.reagentType,#slot.variableQuantities," +
                "tostring(var.reagent.itemID==nil)," +
                "var.reagent.currencyID,var.quantity," +
                "slot.quantityRequired,slot.slotInfo.mcrSlotID," +
                "slot.slotInfo.requiredSkillRank," +
                "slot.slotInfo.slotText,slot.dataSlotType," +
                "slot.dataSlotIndex,slot.slotIndex," +
                "slot.orderSource,tostring(slot.required)," +
                "tostring(slot.hiddenInCraftingForm)," +
                "tostring(s.isRecraft)," +
                "tostring(s.hasCraftingOperationInfo)," +
                "#C_TradeSkillUI.GetRecipeRequirements(0)," +
                "#missing.reagentSlotSchematics" +
                "},':')"));
    }

    [Fact]
    public void EnforcesRecoveredRecipeRequirementAndSchematicParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:false:true:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeRequirements))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeRequirements,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeSchematic))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeSchematic,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeSchematic,1,false))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeSchematic,1,false,0))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetRecipeSchematic,1,false,1))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredCraftingOperationInfo()
    {
        using var session = new EmulatorSession();
        var tradeSkill = session.Lua.TradeSkillUi;
        WowCraftingOperationInfoRequest? directRequest = null;
        WowCraftingOperationInfoForOrderRequest? orderRequest = null;
        tradeSkill.CraftingOperationInfoProvider = request =>
        {
            if (request.RecipeId == 101)
            {
                directRequest = request;
            }
            return request.RecipeId == 101
                ? CreateCraftingOperationInfo(101)
                : null;
        };
        tradeSkill.CraftingOperationInfoForOrderProvider = request =>
        {
            if (request.OrderId == 9001)
            {
                orderRequest = request;
            }
            return request.OrderId == 9001
                ? CreateCraftingOperationInfo(202)
                : null;
        };

        Assert.Equal(
            "17:101:100:12:80:9:true:3.5:4:4001:5001:" +
            "75:125:4002:1:Ingenuity:20:Refund chance:" +
            "2.5:1.25:2003:30:7:true:202:true:true",
            session.Lua.Evaluate(
                "local function count(t) local n=0;" +
                "for _ in pairs(t) do n=n+1 end;return n end;" +
                "local i=C_TradeSkillUI.GetCraftingOperationInfo(" +
                "101,{{itemID=1001}},'Item-1',false);" +
                "local o=C_TradeSkillUI." +
                "GetCraftingOperationInfoForOrder(" +
                "101,{{currencyID=2001}},9001,true);" +
                "local s=i.bonusStats[1];" +
                "return table.concat({" +
                "count(i),i.recipeID,i.baseDifficulty," +
                "i.bonusDifficulty,i.baseSkill,i.bonusSkill," +
                "tostring(i.isQualityCraft),i.quality," +
                "i.craftingQuality,i.craftingQualityID," +
                "i.craftingDataID,i.lowerSkillThreshold," +
                "i.upperSkillTreshold," +
                "i.guaranteedCraftingQualityID,#i.bonusStats," +
                "s.bonusStatName,s.bonusStatValue," +
                "s.ratingDescription,s.ratingPct,s.bonusRatingPct," +
                "i.concentrationCurrencyID,i.concentrationCost," +
                "i.ingenuityRefund," +
                "tostring(i.upperSkillThreshold==nil)," +
                "o.recipeID,tostring(C_TradeSkillUI." +
                "GetCraftingOperationInfo(0,{},nil,false)==nil)," +
                "tostring(C_TradeSkillUI." +
                "GetCraftingOperationInfoForOrder(" +
                "1,{},0,false)==nil)" +
                "},':')"));

        Assert.NotNull(directRequest);
        Assert.Equal(101, directRequest.RecipeId);
        Assert.Single(directRequest.CraftingReagents);
        Assert.Equal(
            (uint)1001,
            directRequest.CraftingReagents[0].ItemId);
        Assert.Equal("Item-1", directRequest.AllocationItemGuid);
        Assert.False(directRequest.ApplyConcentration);

        Assert.NotNull(orderRequest);
        Assert.Equal(101, orderRequest.RecipeId);
        Assert.Single(orderRequest.CraftingReagents);
        Assert.Equal(
            (uint)2001,
            orderRequest.CraftingReagents[0].CurrencyId);
        Assert.Equal((ulong)9001, orderRequest.OrderId);
        Assert.True(orderRequest.ApplyConcentration);
    }

    [Fact]
    public void EnforcesRecoveredCraftingOperationInfoParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:true:true:false:false:" +
            "false:true:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfo))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfo,1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfo,1,1,nil,false))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfo,1,{}))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfo,1,{},nil,false))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfo,1,{},'Item-1',true))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfoForOrder))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfoForOrder,1,{}))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfoForOrder,1,{},1))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfoForOrder,1,{},1,false))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfoForOrder,1,{},-1,false))," +
                "tostring(pcall(C_TradeSkillUI." +
                "GetCraftingOperationInfoForOrder,'1',{},'2',true))" +
                "},':')"));
    }

    private static WowCraftingOperationInfo CreateCraftingOperationInfo(
        int recipeId) =>
        new(
            RecipeId: recipeId,
            BaseDifficulty: 100,
            BonusDifficulty: 12,
            BaseSkill: 80,
            BonusSkill: 9,
            IsQualityCraft: true,
            Quality: 3.5f,
            CraftingQuality: 4,
            CraftingQualityId: 4001,
            CraftingDataId: 5001,
            LowerSkillThreshold: 75,
            UpperSkillThreshold: 125,
            GuaranteedCraftingQualityId: 4002,
            BonusStats:
            [
                new WowCraftingOperationBonusStatInfo(
                    "Ingenuity",
                    20,
                    "Refund chance",
                    2.5f,
                    1.25f)
            ],
            ConcentrationCurrencyId: 2003,
            ConcentrationCost: 30,
            IngenuityRefund: 7);

    [Fact]
    public void EnforcesRecoveredReagentQualityAndRecipeTextParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:true:false:false:true:false:true:false:" +
            "true:false:true:false:false:true:true",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_TradeSkillUI." +
                "GetItemReagentQualityByItemInfo))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetItemReagentQualityByItemInfo,1))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetItemReagentQualityInfo))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetItemReagentQualityInfo,1))," +
                "tostring(ok(C_TradeSkillUI.GetQualitiesForRecipe))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetReagentDifficultyText,0,{}))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetReagentDifficultyText,1,{}))," +
                "tostring(ok(C_TradeSkillUI.GetReagentSlotStatus,1,2))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetReagentSlotStatus,1,2,3))," +
                "tostring(ok(C_TradeSkillUI.GetRecipeDescription,1))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetRecipeDescription,1,{}))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetRecipeQualityItemIDs))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetRecipeQualityItemIDs,1))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetRecipeQualityReagentLink,1,0,1))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetRecipeQualityReagentLink,1,1,0))," +
                "tostring(ok(C_TradeSkillUI." +
                "GetRecipeQualityReagentLink,1,1,1))," +
                "tostring(C_TradeSkillUI." +
                "GetItemReagentQualityInfo(1)==nil)" +
                "},':')"));
    }
}
