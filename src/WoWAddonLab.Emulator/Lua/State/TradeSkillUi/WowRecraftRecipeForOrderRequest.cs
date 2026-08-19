namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRecraftRecipeForOrderRequest(
    ulong OrderId,
    string ItemGuid,
    IReadOnlyList<WowCraftingReagentInfo>? CraftingReagents,
    IReadOnlyList<WowCraftingItemSlotModification>? RemovedModifications,
    bool? ApplyConcentration);
