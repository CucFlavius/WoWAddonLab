namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemUpgradeCurrencyCostState(
    int Cost,
    int CurrencyId,
    WowItemUpgradeDiscountInfoState DiscountInfo);
