namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDamageMeterCombatSession(
    IReadOnlyList<WowDamageMeterCombatSource> CombatSources,
    int MaxAmount,
    int TotalAmount,
    double? DurationSeconds);
