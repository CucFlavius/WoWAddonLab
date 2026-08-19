namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRecraftRecipeRequest(
    string ItemGuid,
    IReadOnlyList<WowCraftingReagentInfo>? CraftingReagents,
    IReadOnlyList<WowCraftingItemSlotModification>? RemovedModifications,
    bool? ApplyConcentration);
