namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDamageMeterCombatSessionSource(
    IReadOnlyList<WowDamageMeterCombatSpell> CombatSpells,
    int MaxAmount,
    int TotalAmount);
