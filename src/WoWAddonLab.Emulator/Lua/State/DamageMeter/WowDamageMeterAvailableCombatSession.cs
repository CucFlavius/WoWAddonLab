namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDamageMeterAvailableCombatSession(
    int SessionId,
    string? Name,
    double? DurationSeconds);
