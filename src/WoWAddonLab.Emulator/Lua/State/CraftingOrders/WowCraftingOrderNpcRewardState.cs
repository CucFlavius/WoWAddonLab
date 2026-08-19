namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderNpcRewardState(
    string? ItemLink,
    int? CurrencyType,
    int Count);
