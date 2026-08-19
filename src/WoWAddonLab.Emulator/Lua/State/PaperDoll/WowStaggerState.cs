namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStaggerState(
    double Percentage = 0,
    double? PercentageAgainstTarget = null);
