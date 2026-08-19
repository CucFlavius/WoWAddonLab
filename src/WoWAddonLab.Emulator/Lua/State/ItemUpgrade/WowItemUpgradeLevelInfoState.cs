namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemUpgradeLevelInfoState(
    int UpgradeLevel,
    int DisplayQuality,
    int ItemLevelIncrement,
    IReadOnlyList<WowItemUpgradeStatState> LevelStats,
    IReadOnlyList<WowItemUpgradeCurrencyCostState> CurrencyCostsToUpgrade,
    IReadOnlyList<WowItemUpgradeItemCostState> ItemCostsToUpgrade,
    long? MoneyCost,
    string? FailureMessage);
