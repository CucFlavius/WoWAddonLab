namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderPlacementState(
    int SkillLineAbilityId,
    byte OrderType,
    byte OrderDuration,
    ulong TipAmount,
    string CustomerNotes,
    int? MinimumCraftingQualityId,
    string? OrderTarget,
    string? RecraftItem,
    IReadOnlyList<WowCraftingReagentQuantity>? ReagentInfos = null,
    IReadOnlyList<WowCraftingItemSlotModification>? CraftingReagentItems = null);
