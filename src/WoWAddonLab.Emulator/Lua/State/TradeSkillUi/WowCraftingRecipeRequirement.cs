namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingRecipeRequirement(
    string? Name,
    bool Met,
    uint Type);
