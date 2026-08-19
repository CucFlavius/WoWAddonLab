namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTradeSkillUiState
{
    public WowProfessionInfo BaseProfessionInfo { get; set; } = new();
    public WowProfessionInfo ChildProfessionInfo { get; set; } = new();
    public IList<WowProfessionInfo> ChildProfessionInfos { get; } =
        new List<WowProfessionInfo>();

    public ISet<int> TrackedRecipeIds { get; } = new SortedSet<int>();
    public ISet<int> TrackedRecraftRecipeIds { get; } = new SortedSet<int>();

    public ISet<string> EnchantStorableItemGuids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IList<int> ProfessionTradeSkillLineIds { get; } = new List<int>();
    public IDictionary<int, int> ConcentrationCurrencyIds { get; } =
        new Dictionary<int, int>();
    public IDictionary<(int RecipeSpellId, int? RecipeLevelIndex), int>
        CraftableCounts { get; } =
        new Dictionary<(int, int?), int>();
    public IDictionary<int, (bool CannotModify, bool AlwaysShow)>
        HideUnownedFlags { get; } =
        new Dictionary<int, (bool, bool)>();
    public IDictionary<int, IList<uint>> ProfessionSlots { get; } =
        new Dictionary<int, IList<uint>>();
    public IDictionary<(int ProfessionId, int? SkillLineId), IList<int>>
        ProfessionSpells { get; } =
        new Dictionary<(int, int?), IList<int>>();
    public ISet<int> NearProfessionSpellFocusProfessions { get; } =
        new HashSet<int>();
    public ISet<string> OriginalCraftRecipeLearnedItemGuids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ISet<int> FirstCraftRecipeIds { get; } = new HashSet<int>();
    public ISet<int> BaseSkillLineRecipeIds { get; } = new HashSet<int>();
    public ISet<(int RecipeId, int SkillLineId)> SkillLineRecipes { get; } =
        new HashSet<(int, int)>();
    public ISet<int> ProfessionLearnedRecipeIds { get; } =
        new HashSet<int>();
    public ISet<string> EquippedRecraftItemGuids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IList<string> RecraftItemGuids { get; } = new List<string>();
    public IDictionary<int, IList<string>> RecraftItemGuidsByRecipeId
        { get; } = new Dictionary<int, IList<string>>();
    public IDictionary<int, IList<int>> SalvageableItemIdsByRecipeId
        { get; } = new Dictionary<int, IList<int>>();
    public IDictionary<int, int?> SkillLineForGearByItemId { get; } =
        new Dictionary<int, int?>();
    public IDictionary<int, string?> TradeSkillDisplayNames { get; } =
        new Dictionary<int, string?>();
    public IDictionary<int, int?> TradeSkillTextureFileIds { get; } =
        new Dictionary<int, int?>();
    public IDictionary<WowItemLocation, int>
        RecraftRecipeIdsByItemLocation { get; } =
        new Dictionary<WowItemLocation, int>();
    public IDictionary<int, int?> FactionSpecificOutputItemIds { get; } =
        new Dictionary<int, int?>();
    public IDictionary<string, (int? RecipeId, int? SkillLineAbilityId)>
        OriginalCraftRecipeIdsByItemGuid { get; } =
        new Dictionary<
            string,
            (int? RecipeId, int? SkillLineAbilityId)>(
            StringComparer.OrdinalIgnoreCase);
    public IDictionary<int, int?> ProfessionsByInventorySlotIndex
        { get; } = new Dictionary<int, int?>();
    public IList<uint> ProfessionInventorySlots { get; } =
        new List<uint>();
    public IDictionary<int, string?>
        ProfessionNamesBySkillLineAbilityId { get; } =
        new Dictionary<int, string?>();
    public IDictionary<int, WowProfessionInfo> ProfessionInfosByRecipeId
        { get; } = new Dictionary<int, WowProfessionInfo>();
    public IDictionary<int, WowProfessionInfo> ProfessionInfosBySkillLineId
        { get; } = new Dictionary<int, WowProfessionInfo>();
    public IDictionary<int, int?> ItemCraftedQualitiesByItemId { get; } =
        new Dictionary<int, int?>();
    public IDictionary<int, WowItemReagentQualityInfo?>
        ItemCraftedQualityInfosByItemId { get; } =
        new Dictionary<int, WowItemReagentQualityInfo?>();
    public IDictionary<string,
        IReadOnlyList<WowCraftingItemSlotModification>>
        ItemSlotModificationsByItemGuid { get; } =
        new Dictionary<
            string,
            IReadOnlyList<WowCraftingItemSlotModification>>(
            StringComparer.OrdinalIgnoreCase);
    public IDictionary<ulong,
        IReadOnlyList<WowCraftingItemSlotModification>>
        ItemSlotModificationsByOrderId { get; } =
        new Dictionary<
            ulong,
            IReadOnlyList<WowCraftingItemSlotModification>>();
    public IDictionary<(int RecipeId, int Quality),
        WowItemReagentQualityInfo?> RecipeItemQualityInfos { get; } =
        new Dictionary<
            (int RecipeId, int Quality),
            WowItemReagentQualityInfo?>();
    public IDictionary<int, WowRecipeOutputItemData>
        RecipeOutputItemDataByRecipeId { get; } =
        new Dictionary<int, WowRecipeOutputItemData>();
    public IDictionary<(int RecipeSpellId, int? RecipeLevelIndex),
        WowTradeSkillRecipeInfo?> RecipeInfos { get; } =
        new Dictionary<
            (int RecipeSpellId, int? RecipeLevelIndex),
            WowTradeSkillRecipeInfo?>();
    public IDictionary<
        (int SkillLineAbilityId, int? RecipeLevelIndex),
        WowTradeSkillRecipeInfo?> RecipeInfosBySkillLineAbilityId
        { get; } =
        new Dictionary<
            (int SkillLineAbilityId, int? RecipeLevelIndex),
            WowTradeSkillRecipeInfo?>();
    public IDictionary<int, WowGatheringOperationInfo?>
        GatheringOperationInfosByRecipeId { get; } =
        new Dictionary<int, WowGatheringOperationInfo?>();
    public IDictionary<int, IReadOnlyList<WowCraftingRecipeRequirement>>
        RecipeRequirementsByRecipeId { get; } =
        new Dictionary<
            int,
            IReadOnlyList<WowCraftingRecipeRequirement>>();
    public IDictionary<(int RecipeSpellId, bool IsRecraft,
        int? RecipeLevelIndex), WowCraftingRecipeSchematic>
        RecipeSchematics { get; } =
        new Dictionary<
            (int RecipeSpellId, bool IsRecraft, int? RecipeLevelIndex),
            WowCraftingRecipeSchematic>();
    public IDictionary<int, int?> ItemReagentQualitiesByItemId { get; } =
        new Dictionary<int, int?>();
    public IDictionary<int, WowItemReagentQualityInfo?>
        ItemReagentQualityInfosByItemId { get; } =
        new Dictionary<int, WowItemReagentQualityInfo?>();
    public IDictionary<int, IReadOnlyList<int>?> QualityIdsByRecipeId
        { get; } = new Dictionary<int, IReadOnlyList<int>?>();
    public IDictionary<(int McrSlotId, int RecipeSpellId,
        int SkillLineAbilityId), (bool Locked, string LockedReason)>
        ReagentSlotStatuses { get; } =
        new Dictionary<(int, int, int), (bool, string)>();
    public IDictionary<int, IReadOnlyList<uint>?> QualityItemIdsByRecipeId
        { get; } = new Dictionary<int, IReadOnlyList<uint>?>();
    public IDictionary<(int RecipeId, int DataSlotIndex, int QualityIndex),
        string?> RecipeQualityReagentLinks { get; } =
        new Dictionary<(int, int, int), string?>();
    public ISet<int> OpenableTradeSkillLineIds { get; } =
        new HashSet<int>();
    public ISet<int> SelectableProfessionChildSkillLineIds { get; } =
        new HashSet<int>();
    public IList<int> OpenRecipeRequests { get; } = new List<int>();
    public IList<int> OpenTradeSkillRequests { get; } = new List<int>();
    public IList<WowCraftEnchantRequest> CraftEnchantRequests { get; } =
        new List<WowCraftEnchantRequest>();
    public IList<WowCraftRecipeRequest> CraftRecipeRequests { get; } =
        new List<WowCraftRecipeRequest>();
    public IList<WowCraftSalvageRequest> CraftSalvageRequests { get; } =
        new List<WowCraftSalvageRequest>();
    public IList<WowRecraftRecipeRequest> RecraftRecipeRequests { get; } =
        new List<WowRecraftRecipeRequest>();
    public IList<WowRecraftRecipeForOrderRequest>
        RecraftRecipeForOrderRequests { get; } =
        new List<WowRecraftRecipeForOrderRequest>();

    public Func<
        int,
        string,
        IReadOnlyList<WowCraftingReagentInfo>,
        bool>? EnchantTargetValidator { get; set; }
    public Func<
        string,
        WowCraftingReagentInfo,
        bool>? RecraftReagentValidator { get; set; }
    public Func<WowCraftingReagentInfo, bool>?
        RecraftLimitCategoryValidator { get; set; }
    public Func<
        string,
        IReadOnlyList<WowCraftingReagentInfo>,
        IReadOnlyList<string?>>? RecraftRemovalWarningProvider
        { get; set; }
    public Func<int, bool>? OpenTradeSkillProvider { get; set; }
    public Func<WowRecraftRecipeRequest, bool>? RecraftRecipeProvider
        { get; set; }
    public Func<WowRecraftRecipeForOrderRequest, bool>?
        RecraftRecipeForOrderProvider { get; set; }
    public Func<
        int,
        IReadOnlyList<WowCraftingReagentInfo>,
        string>? ReagentDifficultyTextProvider { get; set; }
    public Func<
        int,
        IReadOnlyList<WowCraftingReagentInfo>,
        string?,
        string>? RecipeDescriptionProvider { get; set; }
    public Func<
        WowRecipeOutputItemDataRequest,
        WowRecipeOutputItemData>? RecipeOutputItemDataProvider
        { get; set; }
    public Func<
        int,
        int,
        IReadOnlyList<WowCraftingReagentInfo>,
        string?,
        IReadOnlyList<string>>? CraftingReagentBonusTextProvider
        { get; set; }
    public Func<
        IReadOnlyList<int>,
        IReadOnlyList<WowCraftingTargetItem>>?
        CraftingTargetItemsProvider { get; set; }
    public Func<
        WowCraftingReagentInfo,
        IReadOnlyList<WowCraftingReagentInfo>>?
        DependentReagentsProvider { get; set; }
    public Func<
        int,
        IReadOnlyList<WowCraftingReagentInfo>?,
        IReadOnlyList<string>>? EnchantItemsProvider { get; set; }
    public Func<
        WowCraftingOperationInfoRequest,
        WowCraftingOperationInfo?>? CraftingOperationInfoProvider
        { get; set; }
    public Func<
        WowCraftingOperationInfoForOrderRequest,
        WowCraftingOperationInfo?>? CraftingOperationInfoForOrderProvider
        { get; set; }

    public bool CanRespecAtNpc { get; set; }
    public bool HasPendingProfessionRespec { get; set; }
    public bool HasFavoriteOrderRecipes { get; set; }
    public int ProfessionChildSkillLineId { get; set; }
    public int ProfessionSkillLineId { get; set; }
    public int? ProfessionForCursorItem { get; set; }
    public int RemainingRecasts { get; set; }

    public bool ShowLearned { get; set; } = true;
    public bool ShowUnlearned { get; set; } = true;
    public ushort SourceTypeFilter { get; set; }
    public bool IsGuildTradeSkillsEnabled { get; set; }
    public bool IsTradeSkillGuild { get; set; }
    public bool IsTradeSkillGuildMember { get; set; }
    public bool IsTradeSkillLinked { get; set; }
    public string? LinkedTradeSkillPlayerName { get; set; }
    public bool IsNpcCrafting { get; set; }
    public bool OnlyShowAvailableForOrders { get; set; }
    public bool OnlyShowMakeableRecipes { get; set; }
    public bool OnlyShowSkillUpRecipes { get; set; }
    public bool OnlyShowFirstCraftRecipes { get; set; }
    public string RecipeItemNameFilter { get; set; } = string.Empty;
    public int MinimumRecipeItemLevel { get; set; }
    public int MaximumRecipeItemLevel { get; set; }
    public IList<string> FilterableInventorySlotNames { get; } = [];
    public ISet<int> FilteredInventorySlots { get; } = new HashSet<int>();
    public ISet<int> FilteredRecipeCategories { get; } = new HashSet<int>();
    public ISet<int> FilteredRecipeSourceTypes { get; } = new HashSet<int>();
    public ISet<int> RecipeSourceTypes { get; } = new HashSet<int>();
    public IList<int> FilteredRecipeIds { get; } = [];

    public int CloseTradeSkillCount { get; internal set; }
    public int CancelProfessionRespecCount { get; internal set; }
    public int ConfirmProfessionRespecCount { get; internal set; }
}
