namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDamageMeterCombatSpell(
    int SpellId,
    int TotalAmount,
    float AmountPerSecond,
    string? CreatureName,
    int OverkillAmount,
    bool IsAvoidable,
    bool IsDeadly,
    WowDamageMeterCombatSpellDetails CombatSpellDetails);
