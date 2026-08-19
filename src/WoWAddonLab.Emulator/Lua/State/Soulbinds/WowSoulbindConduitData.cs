namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSoulbindConduitData(
    int ConduitId,
    int ConduitRank,
    int ConduitItemLevel,
    int ConduitType,
    int ConduitSpecSetId,
    IReadOnlyList<int> ConduitSpecIds,
    string? ConduitSpecName,
    int? CovenantId,
    int ConduitItemId);
