namespace WoWAddonLab.Emulator.Lua;

public sealed record WowWeeklyRewardActivityTierProgressState(
    int ActivityTierId,
    int Difficulty,
    int NumPoints);
