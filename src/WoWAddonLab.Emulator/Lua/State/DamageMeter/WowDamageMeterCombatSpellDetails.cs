namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDamageMeterCombatSpellDetails(
    string? UnitName,
    string? UnitClassFilename,
    string? Classification,
    bool IsPet,
    bool IsMob,
    int Amount,
    int SpecIconId);
