namespace WoWAddonLab.Emulator.Lua;

public sealed record WowHousingActionCooldownInfoState(
    double StartTime,
    double Duration,
    bool IsEnabled,
    bool IsActive,
    double ModRate,
    int? ActiveCategory,
    double? TimeUntilEndOfStartRecovery,
    bool IsOnGcd);
