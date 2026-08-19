namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBrawlRewardState(
    WowPvpRewardState Rewards,
    bool HasWon);
