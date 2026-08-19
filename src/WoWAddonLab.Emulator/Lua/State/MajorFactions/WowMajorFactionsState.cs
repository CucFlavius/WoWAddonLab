namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMajorFactionsState
{
    public int RenownNpcFactionId { get; set; }

    public IDictionary<int, WowMajorFactionDataState> Factions { get; } =
        new Dictionary<int, WowMajorFactionDataState>();

    public IDictionary<int, int> CurrentRenownLevels { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, WowMajorFactionRenownInfoState> RenownInfo { get; } =
        new Dictionary<int, WowMajorFactionRenownInfoState>();

    public IDictionary<int, IReadOnlyList<WowMajorFactionRenownLevelState>>
        RenownLevels { get; } =
        new Dictionary<int, IReadOnlyList<WowMajorFactionRenownLevelState>>();

    public IDictionary<(int MajorFactionId, int RenownLevel),
        IReadOnlyList<WowMajorFactionRenownRewardState>> RenownRewards { get; } =
        new Dictionary<(int MajorFactionId, int RenownLevel),
            IReadOnlyList<WowMajorFactionRenownRewardState>>();

    public ISet<int> MaximumRenownFactions { get; } = new HashSet<int>();
    public ISet<int> HiddenFromExpansionPageFactions { get; } = new HashSet<int>();
    public ISet<int> WeeklyCappedFactions { get; } = new HashSet<int>();
    public ISet<int> JourneyDisplayFactions { get; } = new HashSet<int>();
    public ISet<int> JourneyRewardTrackFactions { get; } = new HashSet<int>();
}
