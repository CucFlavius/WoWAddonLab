namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTreeCurrencyInfoState(
    int TraitCurrencyId,
    int Quantity,
    int? MaxQuantity,
    int Spent);
