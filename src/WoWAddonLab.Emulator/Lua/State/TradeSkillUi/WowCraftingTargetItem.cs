namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingTargetItem(
    int ItemId,
    string ItemGuid,
    string? Hyperlink,
    int Quantity);
