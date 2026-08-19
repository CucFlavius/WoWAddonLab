namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDamageState(
    double Minimum = 0,
    double Maximum = 0,
    double MinimumOffHand = 0,
    double MaximumOffHand = 0,
    int PositiveBonus = 0,
    int NegativeBonus = 0,
    double PercentModifier = 0);
