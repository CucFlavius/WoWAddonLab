namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOperationInfoRequest(
    int RecipeId,
    IReadOnlyList<WowCraftingReagentInfo> CraftingReagents,
    string? AllocationItemGuid,
    bool ApplyConcentration);
