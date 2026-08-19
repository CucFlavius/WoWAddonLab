using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowContributionCollectorState
{
    public IList<int> ActiveContributionIds { get; } = [];
    public IDictionary<uint, IReadOnlyList<string?>> AtlasesByContributionId { get; } =
        new Dictionary<uint, IReadOnlyList<string?>>();
    public IDictionary<uint, IReadOnlyList<int>> BuffIdsByContributionId { get; } =
        new Dictionary<uint, IReadOnlyList<int>>();
    public IDictionary<(uint ContributionId, uint State), WowContributionAppearance>
        AppearanceByContributionAndState { get; } =
        new Dictionary<(uint ContributionId, uint State), WowContributionAppearance>();
    public IDictionary<int, IReadOnlyList<WowContributionMapInfo>> CollectorsByMapId { get; } =
        new Dictionary<int, IReadOnlyList<WowContributionMapInfo>>();
    public IDictionary<uint, WowContributionDefinition> DefinitionsById { get; } =
        new Dictionary<uint, WowContributionDefinition>();
    public IDictionary<int, IReadOnlyList<int>> ManagedContributionIdsByCreatureId { get; } =
        new Dictionary<int, IReadOnlyList<int>>();
    public IDictionary<uint, WowContributionStateInfo> StateByContributionId { get; } =
        new Dictionary<uint, WowContributionStateInfo>();
    public IDictionary<uint, byte> ResultByContributionId { get; } =
        new Dictionary<uint, byte>();
    public ISet<uint> PendingContributionIds { get; } = new HashSet<uint>();
    public ISet<uint> AwaitingRewardQuestDataIds { get; } = new HashSet<uint>();

    public int ContributionRequestCount { get; internal set; }
    public uint? LastContributionId { get; internal set; }
}
