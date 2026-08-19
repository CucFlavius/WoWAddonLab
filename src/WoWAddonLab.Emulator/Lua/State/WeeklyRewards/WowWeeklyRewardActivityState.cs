namespace WoWAddonLab.Emulator.Lua;

public sealed record WowWeeklyRewardActivityState(
    byte Type,
    int ZeroBasedIndex,
    int Threshold,
    int Progress,
    int Id,
    int ActivityTierId,
    int Level,
    int? ClaimId,
    string? RaidString,
    IReadOnlyList<WowWeeklyRewardState> Rewards);
