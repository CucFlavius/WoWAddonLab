namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSoulbindNodeData(
    int Id,
    int Row,
    int Column,
    int Icon,
    int SpellId,
    string? PlayerConditionReason,
    int ConduitId,
    int ConduitRank,
    int State,
    int? ConduitType,
    IReadOnlyList<int> ParentNodeIds,
    int? FailureRenownRequirement,
    bool? SocketEnhanced)
{
    public static WowSoulbindNodeData Empty { get; } =
        new(0, 0, 0, 0, 0, null, 0, 0, 0, null, [], null, null);
}
