namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemUpgradeCostTypeForSeasonState(
    int? ItemId,
    int? CurrencyId,
    int OrderIndex,
    string? SourceString);
