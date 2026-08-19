namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitEntryInfoState(
    int? DefinitionId,
    int? SubTreeId,
    int Type,
    int MaxRanks,
    bool IsAvailable,
    bool IsDisplayError,
    IReadOnlyList<int> ConditionIds);
