namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMythicPlusState
{
    public IReadOnlyList<WowMythicPlusAffixState>? CurrentAffixes { get; set; }
    public int CurrentSeason { get; set; }
    public WowMythicPlusSeasonValuesState CurrentSeasonValues { get; set; } =
        new(0, 0, 0);
    public int? CurrentUiDisplaySeason { get; set; }
    public int? OwnedKeystoneChallengeMapId { get; set; }
    public int? OwnedKeystoneLevel { get; set; }
    public int? OwnedKeystoneMapId { get; set; }
    public WowMythicPlusExpansionRatingState? SeasonBestRatingFromExpansion { get; set; }
    public WowMythicPlusWeeklyChestRewardState WeeklyChestReward { get; set; } =
        new(0, 0, 0, 0);
    public bool IsActive { get; set; }
    public int CurrentAffixRequestCount { get; set; }
    public int MapInfoRequestCount { get; set; }
    public int RewardsRequestCount { get; set; }

    public IList<WowMythicPlusRunState> RunHistory { get; } =
        new List<WowMythicPlusRunState>();

    public IDictionary<int, int?> EndOfRunGearSequenceLevels { get; } =
        new Dictionary<int, int?>();

    public IDictionary<int, WowMythicPlusRewardLevelsState> RewardLevelsByDifficulty
        { get; } =
        new Dictionary<int, WowMythicPlusRewardLevelsState>();

    public IDictionary<int, int?> RewardLevelsByKeystoneLevel { get; } =
        new Dictionary<int, int?>();

    public IDictionary<int, WowMythicPlusAffixScoreInfoState> SeasonBestAffixScores
        { get; } =
        new Dictionary<int, WowMythicPlusAffixScoreInfoState>();

    public IDictionary<int, WowMythicPlusSeasonBestState> SeasonBestsByMap { get; } =
        new Dictionary<int, WowMythicPlusSeasonBestState>();

    public IDictionary<int, WowMythicPlusBestRunState> WeeklyBestsByMap { get; } =
        new Dictionary<int, WowMythicPlusBestRunState>();
}
