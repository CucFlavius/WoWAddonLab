namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitCurrencyInfoState(
    int Flags,
    int Type,
    int? CurrencyTypesId,
    int? Icon);
