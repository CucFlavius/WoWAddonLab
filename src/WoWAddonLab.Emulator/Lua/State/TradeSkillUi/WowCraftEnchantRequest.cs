namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftEnchantRequest(
    int RecipeSpellId,
    uint NumCasts,
    IReadOnlyList<WowCraftingReagentInfo>? CraftingReagents,
    WowItemLocation? ItemTarget,
    bool? ApplyConcentration);
