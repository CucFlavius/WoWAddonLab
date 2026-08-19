namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPvpState
{
    public bool IsActiveBattlefieldArena { get; set; }
    public bool HasActiveBattlefieldArenaMatch { get; set; }
    public bool IsActiveBattlefield { get; set; }
    public bool IsInActiveWorldPvp { get; set; }
    public bool IsPvpTimerRunning { get; set; }
    public int ActiveMatchState { get; set; }
    public bool IsInBrawl { get; set; }
    public bool IsMatchConsideredArena { get; set; }
    public bool IsPvpMap { get; set; }
    public int MaximumBattlefieldId { get; set; }
    public int BattlefieldFlagPositionCount { get; set; }
    public bool CanHearthAndResurrectFromArea { get; set; }
    public int ArenaOpponentSpecCount { get; set; }
    public int ArenaOpponentCount { get; set; }
    public int PvpTalentsUnlockedLevel { get; set; }
    public bool ArePvpTalentsUnlocked { get; set; }
    public bool AreTrainingGroundsEnabled { get; set; }
    public bool CanToggleWarMode { get; set; }
    public bool? CanEnableWarMode { get; set; }
    public bool? CanDisableWarMode { get; set; }
    public bool CanToggleWarModeInArea { get; set; }
    public bool IsWarModeActive { get; set; }
    public bool IsWarModeDesired { get; set; }
    public bool IsWarModeFeatureEnabled { get; set; }
    public int WarModeRewardBonusDefault { get; set; }
    public int WarModeRewardBonus { get; set; }
    public bool CanPlayerUseRatedPvpUi { get; set; }
    public string RatedPvpUiFailureReason { get; set; } = string.Empty;
    public bool CanPlayerUseTrainingGroundsUi { get; set; }
    public string TrainingGroundsUiFailureReason { get; set; } = string.Empty;
    public bool BattlegroundEnlistmentBonusActive { get; set; }
    public bool BrawlEnlistmentBonusActive { get; set; }
    public bool PlayerRoleTank { get; set; }
    public bool PlayerRoleHealer { get; set; }
    public bool PlayerRoleDamage { get; set; } = true;
    public int CurrentArenaSeason { get; set; }
    public int? BattlefieldWinner { get; set; }
    public string? ZonePvpType { get; set; }
    public bool IsSubZonePvp { get; set; }
    public bool BattlemasterOpen { get; set; }
    public string? ZoneFactionName { get; set; }
    public IDictionary<int, int> ArenaOpponentSpecializations { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, int> ArenaOpponentGenders { get; } =
        new Dictionary<int, int>();
    public ISet<int> QuestRewardDataAvailable { get; } = new HashSet<int>();
    public ISet<int> RequestedQuestRewardDataIds { get; } = new HashSet<int>();
    public IDictionary<string, WowArenaCrowdControlInfoState>
        ArenaCrowdControlByUnitToken { get; } =
            new Dictionary<string, WowArenaCrowdControlInfoState>(
                StringComparer.OrdinalIgnoreCase);
    public IDictionary<(uint MapId, int Index), WowBattlefieldFlagPositionState>
        BattlefieldFlagPositions { get; } =
            new Dictionary<
                (uint MapId, int Index),
                WowBattlefieldFlagPositionState>();
    public IDictionary<int, IList<WowBattlefieldVehicleInfoState>>
        BattlefieldVehiclesByMapId { get; } =
            new Dictionary<int, IList<WowBattlefieldVehicleInfoState>>();
    public IDictionary<int, long> OutdoorPvpWaitTimes { get; } =
        new Dictionary<int, long>();
    public WowRandomBattlegroundInfoState RandomBattlegroundInfo { get; set; } =
        new(false, 0, 0, false, 0, 0, string.Empty);
    public WowRandomBattlegroundInfoState RandomEpicBattlegroundInfo
        { get; set; } =
            new(false, 0, 0, false, 0, 0, string.Empty);
    public IList<WowBattlegroundInfoState> Battlegrounds { get; } =
        new List<WowBattlegroundInfoState>();
    public IList<WowBattlegroundInfoState> TrainingGrounds { get; } =
        new List<WowBattlegroundInfoState>();
    public IDictionary<int, WowSkirmishInfoState> SkirmishInfoByBracket
        { get; } =
            new Dictionary<int, WowSkirmishInfoState>();
    public WowBrawlInfoState? AvailableBrawlInfo { get; set; }
    public WowBrawlInfoState? SpecialEventBrawlInfo { get; set; }
    public IDictionary<int, WowPvpRewardState> ArenaRewardsByTeamSize
        { get; } =
            new Dictionary<int, WowPvpRewardState>();
    public WowPvpRewardState ArenaSkirmishRewards { get; set; } =
        EmptyRewards();
    public WowPvpRewardState RandomBattlegroundRewards { get; set; } =
        EmptyRewards();
    public WowPvpRewardState RandomEpicBattlegroundRewards { get; set; } =
        EmptyRewards();
    public WowPvpRewardState RandomTrainingGroundRewards { get; set; } =
        EmptyRewards();
    public WowPvpRewardState RatedBattlegroundRewards { get; set; } =
        EmptyRewards();
    public WowPvpRewardState RatedSoloRbgRewards { get; set; } =
        EmptyRewards();
    public WowPvpRewardState RatedSoloShuffleRewards { get; set; } =
        EmptyRewards();
    public IDictionary<int, WowBrawlRewardState> BrawlRewardsByType
        { get; } =
            new Dictionary<int, WowBrawlRewardState>();
    public int RatedSoloRbgMinItemLevel { get; set; }
    public int RatedSoloShuffleMinItemLevel { get; set; }
    public IDictionary<int, WowPvpRewardItemLevelsState> RewardItemLevelsByTier
        { get; } =
            new Dictionary<int, WowPvpRewardItemLevelsState>();
    public int SeasonBestTier { get; set; }
    public int? SeasonBestRewardId { get; set; }
    public IDictionary<uint, WowHonorRewardInfoState> HonorRewardsByLevel
        { get; } =
            new Dictionary<uint, WowHonorRewardInfoState>();
    public ISet<int> HonorLevelsWithRewards { get; } =
        new SortedSet<int>();
    public WowPvpSpecStatsState? PersonalRatedBgBlitzSpecStats { get; set; }
    public WowPvpSpecStatsState? PersonalRatedSoloShuffleSpecStats
        { get; set; }
    public int PvpSeasonRewardAchievementId { get; set; }
    public IDictionary<uint, WowPvpTierInfoState> PvpTiersById { get; } =
        new Dictionary<uint, WowPvpTierInfoState>();
    public IList<ushort> BattlefieldJoinRequests { get; } =
        new List<ushort>();
    public IList<bool> BrawlJoinRequests { get; } =
        new List<bool>();
    public int RandomTrainingGroundJoinRequestCount { get; set; }
    public int RatedBgBlitzJoinRequestCount { get; set; }
    public IList<uint> TrainingGroundJoinRequests { get; } =
        new List<uint>();
    public IList<string> CrowdControlSpellRequestUnitTokens { get; } =
        new List<string>();
    public int PvpRewardsRequestCount { get; set; }
    public int PvpOptionsEnabledRequestCount { get; set; }
    public int RandomBattlegroundInstanceInfoRequestCount { get; set; }
    public int RatedInfoRequestCount { get; set; }
    public IDictionary<int, WowWorldPvpQueueState> WorldPvpQueues { get; } =
        new Dictionary<int, WowWorldPvpQueueState>();

    private static WowPvpRewardState EmptyRewards() =>
        new(0, 0, null, null, null);
}
