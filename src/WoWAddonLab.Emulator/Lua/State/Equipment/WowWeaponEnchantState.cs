namespace WoWAddonLab.Emulator.Lua;

public sealed record WowWeaponEnchantState(
    bool HasEnchant,
    double ExpirationMilliseconds,
    int Charges,
    int EnchantId);
