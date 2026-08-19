namespace WoWAddonLab.Emulator.Lua;

public sealed record WowConquestWeeklyProgressState(
    int Progress,
    int MaxProgress,
    int DisplayType,
    int UnlocksCompleted,
    int MaxUnlocks,
    string SampleItemHyperlink);
