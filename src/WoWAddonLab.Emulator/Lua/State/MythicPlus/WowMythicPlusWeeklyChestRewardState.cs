namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMythicPlusWeeklyChestRewardState(
    int CurrentWeekBest,
    int WeeklyRewardLevel,
    int NextDifficultyWeeklyRewardLevel,
    int NextBestLevel);
