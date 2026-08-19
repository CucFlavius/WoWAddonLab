namespace WoWAddonLab.Emulator.Lua;

public sealed record WowNextActivitiesIncreaseState(
    bool HasSeasonData,
    int? NextActivityTierId,
    int? NextLevel,
    int? ItemLevel);
