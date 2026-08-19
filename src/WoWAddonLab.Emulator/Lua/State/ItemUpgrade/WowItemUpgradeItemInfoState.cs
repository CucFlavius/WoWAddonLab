namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemUpgradeItemInfoState(
    int IconId,
    string Name,
    bool ItemUpgradeable,
    int DisplayQuality,
    int HighWatermarkSlot,
    int CurrentUpgrade,
    int MaximumUpgrade,
    int MinimumItemLevel,
    int MaximumItemLevel,
    IReadOnlyList<WowItemUpgradeLevelInfoState> UpgradeLevelInfos,
    string? CustomUpgradeString,
    IReadOnlyList<WowItemUpgradeCostTypeForSeasonState>
        UpgradeCostTypesForSeason);
