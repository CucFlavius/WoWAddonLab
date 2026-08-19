namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMovementSpeedState(
    double Current = 0,
    double Run = 0,
    double Flight = 0,
    double Swim = 0);
