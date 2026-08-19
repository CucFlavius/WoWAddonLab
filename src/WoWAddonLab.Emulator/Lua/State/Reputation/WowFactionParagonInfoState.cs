namespace WoWAddonLab.Emulator.Lua;

public sealed record WowFactionParagonInfoState(
    int CurrentValue,
    int Threshold,
    int RewardQuestId,
    bool HasRewardPending,
    bool TooLowLevelForParagon,
    int ParagonStorageLevel);
