namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCommentatorTimedEffectState(
    double StartTime,
    double Duration,
    bool Enabled);
