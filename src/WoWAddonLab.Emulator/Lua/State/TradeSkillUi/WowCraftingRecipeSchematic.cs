namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingRecipeSchematic(
    int RecipeId = 0,
    int Icon = 0,
    int QuantityMin = 0,
    int QuantityMax = 0,
    string? Name = null,
    uint RecipeType = 0,
    int? ProductQuality = null,
    int? OutputItemId = null,
    IReadOnlyList<WowCraftingReagentSlotSchematic>?
        ReagentSlotSchematics = null,
    bool IsRecraft = false,
    bool HasCraftingOperationInfo = false);
