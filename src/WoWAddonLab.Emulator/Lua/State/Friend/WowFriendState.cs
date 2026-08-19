namespace WoWAddonLab.Emulator.Lua;

public sealed class WowFriendState
{
    public bool BattleNetFeaturesEnabled { get; set; } = true;
    public bool BattleNetConnected { get; set; } = true;
    public uint? BattleNetAccountId { get; set; }
    public uint BattleNetToonId { get; set; }
    public string? BattleTag { get; set; }
    public string? BroadcastText { get; set; }
    public bool BattleNetAfk { get; set; }
    public bool BattleNetDnd { get; set; }
    public bool RealIdEnabled { get; set; }
    public int BattleNetFriendCount { get; set; }
    public int BattleNetOnlineFriendCount { get; set; }
    public int BattleNetFavoriteFriendCount { get; set; }
    public int BattleNetOnlineFavoriteFriendCount { get; set; }
    public int? SelectedBattleNetFriend { get; set; }
    public uint? SelectedBattleNetBlockedAccountId { get; set; }
    public bool BattleNetFofRequestResult { get; set; } = true;
    public bool WhoResultsToUi { get; set; }
    public int? SelectedFriend { get; set; }
    public int? SelectedIgnore { get; set; }
    public IList<WowFriendInfoState> Friends { get; } = new List<WowFriendInfoState>();
    public IList<string> Ignores { get; } = new List<string>();
    public IList<WowWhoInfoState> WhoResults { get; } =
        new List<WowWhoInfoState>();
    public IList<WowBattleNetFriendState> BattleNetFriends { get; } =
        new List<WowBattleNetFriendState>();
    public IList<WowBattleNetBlockedState> BattleNetBlockedAccounts { get; } =
        new List<WowBattleNetBlockedState>();
    public IList<WowBattleNetFriendInviteState> BattleNetFriendInvites { get; } =
        new List<WowBattleNetFriendInviteState>();
    public IList<WowBattleNetFofInfoState> BattleNetFofEntries { get; } =
        new List<WowBattleNetFofInfoState>();
    public ISet<uint> KnownBattleNetAccountIds { get; } = new HashSet<uint>();
    public IDictionary<string, WowBattleNetAccountInfoState> BattleNetAccountsByGuid { get; } =
        new Dictionary<string, WowBattleNetAccountInfoState>(StringComparer.OrdinalIgnoreCase);

    public IList<WowAddFriendRequest> AddFriendRequests { get; } =
        new List<WowAddFriendRequest>();

    public IList<string> AddIgnoreRequests { get; } = new List<string>();
    public IList<string> RemoveIgnoreRequests { get; } = new List<string>();
    public IList<string> RemoveFriendRequests { get; } = new List<string>();
    public IList<WowWhoRequest> WhoRequests { get; } =
        new List<WowWhoRequest>();

    public IList<string> WhoSortRequests { get; } = new List<string>();
    public IList<uint> BattleNetSummonFriendRequests { get; } = new List<uint>();
    public IList<uint> BattleNetRemoveFriendRequests { get; } = new List<uint>();
    public IList<WowBattleNetFriendNoteRequest> BattleNetFriendNoteRequests { get; } =
        new List<WowBattleNetFriendNoteRequest>();
    public IList<WowBattleNetFavoriteRequest> BattleNetFavoriteRequests { get; } =
        new List<WowBattleNetFavoriteRequest>();
    public IList<string> BattleNetFriendInviteRequests { get; } = new List<string>();
    public IList<uint> BattleNetFriendInviteByIdRequests { get; } = new List<uint>();
    public IList<uint> BattleNetDeclineFriendInviteRequests { get; } = new List<uint>();
    public IList<string> BattleNetGuildMemberInviteChecks { get; } = new List<string>();
    public IList<string> BattleNetUnitInviteChecks { get; } = new List<string>();
    public IList<string> BattleNetRecentAllyInviteChecks { get; } = new List<string>();
    public IList<uint> BattleNetFofRequests { get; } = new List<uint>();
    public IList<WowBattleNetBlockedRequest> BattleNetBlockedRequests { get; } =
        new List<WowBattleNetBlockedRequest>();
    public IList<WowBattleNetInviteRoleRequest> BattleNetInviteRoleRequests { get; } =
        new List<WowBattleNetInviteRoleRequest>();
    public int ShowFriendsRequests { get; internal set; }

    public bool KnowsBattleNetAccountId(uint accountId) =>
        BattleNetAccountId == accountId ||
        KnownBattleNetAccountIds.Contains(accountId) ||
        BattleNetFriends.Any(friend => friend.AccountId == accountId) ||
        BattleNetBlockedAccounts.Any(blocked => blocked.AccountId == accountId) ||
        BattleNetFriendInvites.Any(invite => invite.AccountId == accountId) ||
        BattleNetFofEntries.Any(entry => entry.AccountId == accountId);

    public string? FindBattleNetDisplayName(uint accountId) =>
        BattleNetAccountId == accountId
            ? BattleTag
            : BattleNetFriends.FirstOrDefault(friend => friend.AccountId == accountId)?.DisplayName ??
              BattleNetBlockedAccounts.FirstOrDefault(blocked => blocked.AccountId == accountId)?.DisplayName ??
              BattleNetFriendInvites.FirstOrDefault(invite => invite.AccountId == accountId)?.DisplayName ??
              BattleNetFofEntries.FirstOrDefault(entry => entry.AccountId == accountId)?.DisplayName;
}
