namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCraftingOrdersState
{
    public bool AreOrderNotesDisabled { get; set; }
    public ulong DefaultPostingFee { get; set; } = 100;
    public IDictionary<(int SkillLineAbilityId, byte OrderType, byte OrderDuration), ulong>
        PostingFees { get; } =
        new Dictionary<(int SkillLineAbilityId, byte OrderType, byte OrderDuration), ulong>();
    public ISet<int> FavoriteCustomerOptionRecipeIds { get; } = new HashSet<int>();
    public ISet<int> OrderableSkillLineAbilityIds { get; } = new HashSet<int>();
    public IList<WowCraftingOrderCustomerCategoryState> CustomerCategories { get; } =
        new List<WowCraftingOrderCustomerCategoryState>();
    public IList<WowCraftingOrderCustomerOptionState> CustomerOptions { get; } =
        new List<WowCraftingOrderCustomerOptionState>();
    public int? CustomerOptionsExtraColumnType { get; set; }
    public IList<WowCraftingOrderInfoState> CustomerOrders { get; } =
        new List<WowCraftingOrderInfoState>();
    public IList<WowCraftingOrderInfoState> MyOrders { get; } =
        new List<WowCraftingOrderInfoState>();
    public IList<WowCraftingOrderBucketInfoState> CrafterBuckets { get; } =
        new List<WowCraftingOrderBucketInfoState>();
    public IList<WowCraftingOrderInfoState> CrafterOrders { get; } =
        new List<WowCraftingOrderInfoState>();
    public WowCraftingOrderInfoState? ClaimedOrder { get; set; }
    public IList<WowPersonalCraftingOrderInfoState> PersonalOrders { get; } =
        new List<WowPersonalCraftingOrderInfoState>();
    public ulong CraftingOrderTime { get; set; }
    public int? DefaultOrdersSkillLine { get; set; }
    public IDictionary<byte, WowCraftingOrderClaimInfoState> OrderClaimInfo { get; } =
        new Dictionary<byte, WowCraftingOrderClaimInfoState>();
    public ISet<ulong> RecraftableOrderIds { get; } = new HashSet<ulong>();
    public bool ShouldShowCraftingOrderTab { get; set; }
    public ISet<int> SkillLinesWithOrders { get; } = new HashSet<int>();
    public bool IsCustomerCraftingOrdersOpen { get; set; }
    public bool IsCrafterCraftingOrdersOpen { get; set; }
    public ulong? LastCancelledOrderId { get; set; }
    public WowCraftingOrderActionState? LastClaimedOrder { get; set; }
    public WowCraftingOrderNoteActionState? LastFulfilledOrder { get; set; }
    public WowCraftingOrderNoteActionState? LastRejectedOrder { get; set; }
    public WowCraftingOrderActionState? LastReleasedOrder { get; set; }
    public WowCraftingOrderListRequestState? LastListMyOrdersRequest { get; set; }
    public WowCraftingOrderCustomerRequestState? LastCustomerOrdersRequest { get; set; }
    public WowCraftingOrderCustomerRequestState? LastCrafterOrdersRequest { get; set; }
    public WowCraftingOrderPlacementState? LastPlacedOrder { get; set; }
    public int ParseCustomerOptionsCount { get; set; }
    public int UpdateIgnoreListCount { get; set; }
}
