namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSpellAuraStatChanges(
    int HealthChange,
    IReadOnlyList<WowSpellAuraPowerChange> PowerTypeChanges);
