namespace WoWAddonLab.Emulator.Lua;

public sealed record WowActionChargeInfo(
    int CurrentCharges = 0,
    int MaxCharges = 0,
    double CooldownStartTime = 0,
    double CooldownDuration = 0,
    double ChargeModRate = 0,
    bool IsActive = false);
