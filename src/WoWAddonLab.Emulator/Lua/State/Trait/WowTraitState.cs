namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTraitState
{
    public bool IsReadyForCommit { get; set; }

    public bool HasValidInspectData { get; set; }

    public IDictionary<int, WowTraitEditabilityState> ConfigEditability { get; } =
        new Dictionary<int, WowTraitEditabilityState>();

    public IDictionary<int, int> ConfigIdsBySystemId { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, int> ConfigIdsByTreeId { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, int> TraitSystemFlagsByConfigId { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, int> TraitSystemWidgetSetIdsByConfigId { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, IList<int>> TreeNodesByTreeId { get; } =
        new Dictionary<int, IList<int>>();

    public IDictionary<(int ConfigId, int ConditionId), WowTraitConditionInfoState>
        ConditionInfo { get; } =
            new Dictionary<(int ConfigId, int ConditionId), WowTraitConditionInfoState>();

    public IDictionary<int, WowTraitConfigInfoState> ConfigInfo { get; } =
        new Dictionary<int, WowTraitConfigInfoState>();

    public IDictionary<int, WowTraitDefinitionInfoState> DefinitionInfo { get; } =
        new Dictionary<int, WowTraitDefinitionInfoState>();

    public IDictionary<(int ConfigId, int EntryId), WowTraitEntryInfoState>
        EntryInfo { get; } =
            new Dictionary<(int ConfigId, int EntryId), WowTraitEntryInfoState>();

    public IDictionary<(int NodeId, int EntryId), IList<WowIncreasedTraitDataState>>
        IncreasedTraitData { get; } =
            new Dictionary<(int NodeId, int EntryId), IList<WowIncreasedTraitDataState>>();

    public IDictionary<(int ConfigId, int NodeId), IList<WowTraitCurrencyCostState>>
        NodeCosts { get; } =
            new Dictionary<(int ConfigId, int NodeId), IList<WowTraitCurrencyCostState>>();

    public IDictionary<(int ConfigId, int NodeId), WowTraitNodeInfoState>
        NodeInfo { get; } =
            new Dictionary<(int ConfigId, int NodeId), WowTraitNodeInfoState>();

    public IDictionary<(int ConfigId, int SubTreeId), WowTraitSubTreeInfoState>
        SubTreeInfo { get; } =
            new Dictionary<(int ConfigId, int SubTreeId), WowTraitSubTreeInfoState>();

    public IDictionary<int, WowTraitCurrencyInfoState> TraitCurrencyInfo { get; } =
        new Dictionary<int, WowTraitCurrencyInfoState>();

    public IDictionary<(int ConfigId, int TreeId, bool ExcludeStagedChanges),
        IList<WowTreeCurrencyInfoState>> TreeCurrencyInfo { get; } =
            new Dictionary<(int ConfigId, int TreeId, bool ExcludeStagedChanges),
                IList<WowTreeCurrencyInfoState>>();

    public IDictionary<(int ConfigId, int TreeId), WowTraitTreeInfoState>
        TreeInfo { get; } =
            new Dictionary<(int ConfigId, int TreeId), WowTraitTreeInfoState>();

    public IDictionary<(int ConfigId, int NodeId, int? EntryId), bool>
        CascadeRepurchaseResults { get; } =
            new Dictionary<(int ConfigId, int NodeId, int? EntryId), bool>();

    public IDictionary<int, bool> CommitConfigResults { get; } =
        new Dictionary<int, bool>();

    public IDictionary<(int ConfigId, int NodeId), bool> PurchaseRankResults
        { get; } =
            new Dictionary<(int ConfigId, int NodeId), bool>();

    public IDictionary<(int ConfigId, int NodeId), bool> RefundAllRanksResults
        { get; } =
            new Dictionary<(int ConfigId, int NodeId), bool>();

    public IDictionary<(int ConfigId, int NodeId, bool ClearEdges), bool>
        RefundRankResults { get; } =
            new Dictionary<(int ConfigId, int NodeId, bool ClearEdges), bool>();

    public IDictionary<int, bool> RollbackConfigResults { get; } =
        new Dictionary<int, bool>();

    public IDictionary<
        (int ConfigId, int NodeId, int? NodeEntryId, bool ClearEdges),
        bool> SetSelectionResults { get; } =
            new Dictionary<
                (int ConfigId, int NodeId, int? NodeEntryId, bool ClearEdges),
                bool>();

    public IList<WowTraitCascadeRepurchaseRequest> CascadeRepurchaseRequests
        { get; } = [];

    public IList<int> ClearedCascadeRepurchaseHistoryConfigIds { get; } = [];

    public IList<int> CommitConfigRequests { get; } = [];

    public IList<WowTraitNodeRequest> PurchaseRankRequests { get; } = [];

    public IList<WowTraitNodeRequest> RefundAllRanksRequests { get; } = [];

    public IList<WowTraitRefundRankRequest> RefundRankRequests { get; } = [];

    public IList<int> RollbackConfigRequests { get; } = [];

    public IList<WowTraitSelectionRequest> SelectionRequests { get; } = [];
}
