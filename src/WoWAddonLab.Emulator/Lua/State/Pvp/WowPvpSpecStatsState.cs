namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPvpSpecStatsState(
    int WeeklyMostPlayedSpecId,
    int WeeklyMostPlayedSpecCount,
    int SeasonMostPlayedSpecId,
    int SeasonMostPlayedSpecCount);
