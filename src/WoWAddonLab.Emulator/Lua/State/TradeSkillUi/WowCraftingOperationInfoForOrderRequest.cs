namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOperationInfoForOrderRequest(
    int RecipeId,
    IReadOnlyList<WowCraftingReagentInfo> CraftingReagents,
    ulong OrderId,
    bool ApplyConcentration);
