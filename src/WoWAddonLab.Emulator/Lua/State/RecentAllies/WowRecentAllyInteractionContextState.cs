namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRecentAllyInteractionContextState(
    int? ItemId,
    string? LocationName,
    int? ActivityDifficultyId,
    int? ActivityDifficultyLevel);
