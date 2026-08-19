namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftSalvageRequest(
    int RecipeSpellId,
    uint NumCasts,
    WowItemLocation ItemTarget,
    IReadOnlyList<WowCraftingReagentInfo>? CraftingReagents,
    bool? ApplyConcentration);
