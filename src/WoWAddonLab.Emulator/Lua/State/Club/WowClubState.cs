namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubState
{
    public bool Enabled { get; set; } = true;
    public uint RestrictionReason { get; set; }
    public bool AllowBattleNetClubType { get; set; } = true;
    public bool AllowCharacterClubType { get; set; } = true;
    public bool AnyCommunityHasUnreadMessages { get; set; }
    public ulong? GuildClubId { get; set; }

    public IDictionary<int, IList<int>> AvatarIdsByClubType { get; } =
        new Dictionary<int, IList<int>>
        {
            [0] = new List<int>(),
            [1] = new List<int>(),
            [2] = new List<int>(),
            [3] = new List<int>()
        };

    public IDictionary<ulong, WowClubInfoState> ClubInfoById { get; } =
        new Dictionary<ulong, WowClubInfoState>();

    public IList<WowClubInfoState> SubscribedClubs { get; } =
        new List<WowClubInfoState>();

    public IDictionary<ulong, WowClubMemberInfoState> SelfMemberInfoByClubId
        { get; } = new Dictionary<ulong, WowClubMemberInfoState>();

    public IList<WowClubSelfInvitationState> InvitationsForSelf { get; } =
        new List<WowClubSelfInvitationState>();

    public IDictionary<ulong, IList<WowClubInvitationCandidateState>>
        InvitationCandidatesByClubId { get; } =
            new Dictionary<ulong, IList<WowClubInvitationCandidateState>>();

    public IDictionary<ulong, IList<WowClubTicketState>> TicketsByClubId
        { get; } = new Dictionary<ulong, IList<WowClubTicketState>>();

    public IList<WowClubInvitationCandidateQuery> InvitationCandidateQueries
        { get; } = new List<WowClubInvitationCandidateQuery>();

    public IList<ulong> TicketRequests { get; } = new List<ulong>();

    public int ClearAutoAdvanceStreamViewMarkerRequests { get; internal set; }
    public int ClearClubPresenceSubscriptionRequests { get; internal set; }
    public int FlushRequests { get; internal set; }
}
