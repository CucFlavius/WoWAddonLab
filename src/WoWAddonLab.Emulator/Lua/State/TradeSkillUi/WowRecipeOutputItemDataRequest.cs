namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRecipeOutputItemDataRequest(
    int RecipeSpellId,
    IReadOnlyList<WowCraftingReagentInfo>? CraftingReagents,
    string? AllocationItemGuid,
    int? OverrideQualityId,
    ulong? RecraftOrderId);
