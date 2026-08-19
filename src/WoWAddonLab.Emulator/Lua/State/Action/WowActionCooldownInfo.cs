namespace WoWAddonLab.Emulator.Lua;

public sealed record WowActionCooldownInfo(
    double StartTime = 0,
    double Duration = 0,
    bool IsEnabled = false,
    bool IsActive = false,
    double ModRate = 0,
    int? ActiveCategory = null,
    double? TimeUntilEndOfStartRecovery = null,
    bool? IsOnGlobalCooldown = null);
