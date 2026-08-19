namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRecentAllyInteractionState(
    byte Type,
    string? Description,
    int Timestamp,
    WowRecentAllyInteractionContextState ContextData);
