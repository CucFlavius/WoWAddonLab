namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUnitPowerBarTimerState(
    double Duration,
    double Expiration,
    int BarId,
    int AuraId);
