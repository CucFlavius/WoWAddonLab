namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRecipeOutputItemData(
    int Icon = 0,
    string? Hyperlink = null,
    int? ItemId = null);
