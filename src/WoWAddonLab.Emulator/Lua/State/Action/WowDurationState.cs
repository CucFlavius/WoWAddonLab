namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDurationState(
    double StartTime = 0,
    double Duration = 0,
    double ModRate = 1);
