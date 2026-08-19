namespace WoWAddonLab.Emulator.Lua;

public sealed class WowWeeklyRewardsState
{
    public uint StoredRewardPeriodId { get; set; }
    public uint CurrentRewardPeriodId { get; set; }
    public bool GeneratedRewards { get; set; }
    public bool HasRewardPayload { get; set; }
    public bool InteractionActive { get; set; }
    public bool ClaimInProgress { get; set; }
    public bool IsWeeklyChestRetired { get; set; }
    public bool ShouldShowFinalRetirementMessage { get; set; }
    public bool ShouldShowRetirementMessage { get; set; }
    public int? ClaimedRewardId { get; set; }
    public int CloseInteractionRequests { get; set; }
    public int UiInteractRequests { get; set; }

    public IList<WowWeeklyRewardActivityState> Activities { get; } =
        new List<WowWeeklyRewardActivityState>();

    public IDictionary<(byte Type, int ZeroBasedIndex),
        IReadOnlyList<WowWeeklyRewardEncounterState>>
        EncounterInfo { get; } =
        new Dictionary<(byte Type, int ZeroBasedIndex),
            IReadOnlyList<WowWeeklyRewardEncounterState>>();

    public WowConquestWeeklyProgressState ConquestWeeklyProgress { get; set; } =
        new(0, 0, 0, 0, 0, string.Empty);

    public IDictionary<uint, int> DifficultyIdsByActivityTier { get; } =
        new Dictionary<uint, int>();

    public IDictionary<int, WowWeeklyRewardExampleHyperlinksState> ExampleRewardItemHyperlinks
        { get; } =
        new Dictionary<int, WowWeeklyRewardExampleHyperlinksState>();

    public IDictionary<ulong, string?> ItemHyperlinks { get; } =
        new Dictionary<ulong, string?>();

    public IDictionary<(int ActivityTierId, int Level), WowNextActivitiesIncreaseState>
        NextActivitiesIncreases { get; } =
        new Dictionary<(int ActivityTierId, int Level), WowNextActivitiesIncreaseState>();

    public IDictionary<int, WowNextMythicPlusIncreaseState> NextMythicPlusIncreases { get; } =
        new Dictionary<int, WowNextMythicPlusIncreaseState>();

    public WowCompletedDungeonRunsState CompletedDungeonRuns { get; set; } =
        new(0, 0, 0);

    public IDictionary<(byte Type, bool CombineSharedDifficulty),
        IReadOnlyList<WowWeeklyRewardActivityTierProgressState>> SortedProgress { get; } =
        new Dictionary<(byte Type, bool CombineSharedDifficulty),
            IReadOnlyList<WowWeeklyRewardActivityTierProgressState>>();

    public bool AreRewardsForCurrentRewardPeriod =>
        StoredRewardPeriodId != 0 &&
        StoredRewardPeriodId >= unchecked(CurrentRewardPeriodId - 1u);

    public bool HasAvailableRewards => StoredRewardPeriodId != 0;
    public bool HasGeneratedRewards => GeneratedRewards && HasRewardPayload;
    public bool HasInteraction => InteractionActive;
    public bool CanClaimRewards => HasInteraction && HasGeneratedRewards;
}
