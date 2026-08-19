namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemUpgradeItemCostState(
    int Cost,
    int ItemId,
    WowItemUpgradeDiscountInfoState DiscountInfo);
