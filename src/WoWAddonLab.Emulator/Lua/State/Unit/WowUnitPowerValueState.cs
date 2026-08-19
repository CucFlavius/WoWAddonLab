namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUnitPowerValueState(
    long Current,
    long Maximum,
    long? UnmodifiedCurrent = null,
    long? UnmodifiedMaximum = null);
