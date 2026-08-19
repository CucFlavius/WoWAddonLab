namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUnitStatState(
    double Current,
    double Effective,
    double PositiveBuff,
    double NegativeBuff);
