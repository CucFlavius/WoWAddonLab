namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRangedDamageState(
    double AttackTime = 0,
    double Minimum = 0,
    double Maximum = 0,
    int PositiveBonus = 0,
    int NegativeBonus = 0,
    double PercentModifier = 0);
