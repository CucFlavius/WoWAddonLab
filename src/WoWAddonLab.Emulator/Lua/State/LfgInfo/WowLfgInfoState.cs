namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLfgInfoState
{
    public IDictionary<string, WowLfgEligibility> Eligibility { get; } =
        new Dictionary<string, WowLfgEligibility>(StringComparer.Ordinal);

    public IDictionary<int, bool> CrossFactionQueuesAllowed { get; } =
        new Dictionary<int, bool>();

    public IDictionary<int, bool> ActivePartyMeetsPremadeLaunchCount {
        get;
    } = new Dictionary<int, bool>();

    public IDictionary<int, bool> CrossFactionQueueRequiresFullPremade {
        get;
    } = new Dictionary<int, bool>();

    public IDictionary<int, IReadOnlyList<uint>> EntriesByCategory { get; } =
        new Dictionary<int, IReadOnlyList<uint>>();

    public IDictionary<int, WowLfgDungeonInfo> Dungeons { get; } =
        new Dictionary<int, WowLfgDungeonInfo>();

    public IList<WowLfgLockState> LockStates { get; } = [];

    public IDictionary<WowLfgLevelUpKey, IReadOnlyList<int>>
        LevelUpInstances { get; } =
            new Dictionary<WowLfgLevelUpKey, IReadOnlyList<int>>();

    public ISet<uint> VisibleNameDungeonIds { get; } = new HashSet<uint>();
    public ISet<uint> FollowerDungeonIds { get; } = new HashSet<uint>();

    public IDictionary<int, IReadOnlyList<int>> QueuedDungeonIdsByCategory {
        get;
    } = new Dictionary<int, IReadOnlyList<int>>();

    public IList<int> ChoiceOrder { get; } = [];
    public IDictionary<int, bool> ChoiceCollapseState { get; } =
        new Dictionary<int, bool>();
    public ISet<int> EnabledChoiceIds { get; } = new HashSet<int>();

    public int? RoleCheckDifficultyId { get; set; }
    public bool RoleCheckIsRaid { get; set; }
    public bool IsInFollowerDungeon { get; set; }
    public int ConfirmExpandSearchCount { get; set; }

    public double? DeserterExpiration { get; set; }
    public WowLfgRoles Roles { get; set; }
    public bool CanShowSetRoleButton { get; set; } = true;
    public bool HasRestrictions { get; set; }
    public bool CanPartyBackfill { get; set; }
    public bool PlayerLockInfoRequestAllowed { get; set; } = true;
    public bool PartyLockInfoRequestAllowed { get; set; } = true;
    public int PlayerLockInfoRequestCount { get; set; }
    public int PartyLockInfoRequestCount { get; set; }
    public int RandomDungeonCount { get; set; }
    public int RaidFinderDungeonCount { get; set; }

    public WowLfgProposalState? CurrentProposal { get; set; }
    public IDictionary<int, WowLfgServerInfoState> ServerInfoByDungeonId {
        get;
    } = new Dictionary<int, WowLfgServerInfoState>();
    public WowLfgRoleUpdateState RoleUpdate { get; set; } =
        new(false, 0, 0, null, null, false);
    public bool ReadyCheckInProgress { get; set; }
    public bool ReadyCheckIsBattlegroundQueue { get; set; }
    public int? PartyLfgDungeonId { get; set; }
    public int? PartyLfgSecondaryDungeonId { get; set; }
    public bool IsAllowedToUserTeleport { get; set; }
    public IDictionary<int, int> DungeonCategoryById { get; } =
        new Dictionary<int, int>();
}
