namespace WoWAddonLab.Emulator.Lua;

public sealed record WowInstanceLockEncounterState(
    string? EncounterName,
    string? Texture,
    bool IsKilled,
    bool IsIneligible);
