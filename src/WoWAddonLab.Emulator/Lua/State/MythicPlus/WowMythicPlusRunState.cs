namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMythicPlusRunState(
    int MapChallengeModeId,
    int Level,
    bool ThisWeek,
    bool Completed,
    int RunScore,
    int DurationSec,
    WowMythicPlusDateState CompletionDate,
    int Season);
