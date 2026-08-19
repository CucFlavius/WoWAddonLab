namespace WoWAddonLab.Emulator.Lua;

public sealed record WowWeeklyRewardEncounterState(
    int EncounterId,
    int BestDifficulty,
    int UiOrder,
    int InstanceId);
