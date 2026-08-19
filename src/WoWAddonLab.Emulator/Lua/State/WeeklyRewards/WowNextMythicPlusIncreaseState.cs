namespace WoWAddonLab.Emulator.Lua;

public sealed record WowNextMythicPlusIncreaseState(
    bool HasSeasonData,
    int? NextMythicPlusLevel,
    int? ItemLevel);
