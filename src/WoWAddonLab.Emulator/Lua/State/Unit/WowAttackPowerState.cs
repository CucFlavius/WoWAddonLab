namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAttackPowerState(
    int Base = 0,
    int PositiveBonus = 0,
    int NegativeBonus = 0);
