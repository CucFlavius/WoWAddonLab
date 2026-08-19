namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSoulbindsState
{
    public int ActiveSoulbindId { get; set; }
    public int ViewedSoulbindId { get; set; }
    public bool CanModifySoulbind { get; set; }
    public bool CanSwitchActiveSoulbindTreeBranch { get; set; }
    public int? RelevantConduitCount { get; set; }

    public IDictionary<int, WowSoulbindOperationResult>
        ActivationResults { get; } =
            new Dictionary<int, WowSoulbindOperationResult>();

    public IDictionary<int, WowSoulbindOperationResult>
        ResetResults { get; } =
            new Dictionary<int, WowSoulbindOperationResult>();

    public IDictionary<int, IReadOnlyList<WowSoulbindConduitData>>
        ConduitCollections { get; } =
            new Dictionary<int, IReadOnlyList<WowSoulbindConduitData>>();

    public IDictionary<int, WowSoulbindConduitData> Conduits { get; } =
        new Dictionary<int, WowSoulbindConduitData>();

    public IDictionary<int, int> ConduitIdsByVirtualId { get; } =
        new Dictionary<int, int>();

    public WowSoulbindConduitData? ConduitCollectionDataAtCursor
    {
        get;
        set;
    }

    public IDictionary<(int ConduitId, int Rank), string?>
        ConduitHyperlinks { get; } =
            new Dictionary<(int, int), string?>();

    public IDictionary<int, int> ConduitQualitiesByRank { get; } =
        new Dictionary<int, int>();

    public IDictionary<(int ConduitId, int Rank), int>
        ConduitSpellIds { get; } =
            new Dictionary<(int, int), int>();

    public IDictionary<int, WowSoulbindNodeData> Nodes { get; } =
        new Dictionary<int, WowSoulbindNodeData>();

    public IDictionary<int, WowSoulbindTreeData> Trees { get; } =
        new Dictionary<int, WowSoulbindTreeData>();

    public IDictionary<int, WowSoulbindData> Soulbinds { get; } =
        new Dictionary<int, WowSoulbindData>();

    public IDictionary<int, IReadOnlyList<int>>
        SpecsAssignedToSoulbind { get; } =
            new Dictionary<int, IReadOnlyList<int>>();

    public IDictionary<int, int> InstalledConduitsByNode { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, int> NodeSoulbindIds { get; } =
        new Dictionary<int, int>();

    public ISet<int> UnselectedNodeIds { get; } =
        new HashSet<int>();

    public ISet<int> ConduitItemIds { get; } =
        new HashSet<int>();

    public IList<WowSoulbindPendingModification>
        PendingModifications { get; } =
            new List<WowSoulbindPendingModification>();

    public IList<int> ActivationRequests { get; } = new List<int>();
    public IList<int> CommitRequests { get; } = new List<int>();
    public IList<int> SelectNodeRequests { get; } = new List<int>();
    public IList<int> UnmodifyNodeRequests { get; } = new List<int>();

    public IList<WowSoulbindModifyNodeRequest>
        ModifyNodeRequests { get; } =
            new List<WowSoulbindModifyNodeRequest>();
}
