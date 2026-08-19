namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDamageMeterCombatSource(
    string? SourceGuid,
    int? SourceCreatureId,
    string? Name,
    string? ClassFilename,
    int SpecIconId,
    int TotalAmount,
    double AmountPerSecond,
    bool IsLocalPlayer,
    int DeathRecapId,
    int DeathTimeSeconds,
    string? Classification,
    WowDamageMeterSourceDisplayType SourceDisplayType,
    string? FactionGroup);
