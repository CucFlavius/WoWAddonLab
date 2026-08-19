namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftRecipeRequest(
    int RecipeSpellId,
    uint NumCasts,
    IReadOnlyList<WowCraftingReagentInfo>? CraftingReagents,
    int? RecipeLevelIndex,
    ulong? OrderId,
    bool? ApplyConcentration);
