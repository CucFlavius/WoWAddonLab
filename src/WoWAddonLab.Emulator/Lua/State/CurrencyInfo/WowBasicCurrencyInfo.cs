namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBasicCurrencyInfo(
    string Name,
    string Description,
    int Icon,
    int Quality,
    int DisplayAmount,
    int ActualAmount);
