namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubFinderState
{
    public bool Enabled { get; set; } = true;
    public bool CommunityFinderEnabled { get; set; } = true;
    public bool ShouldShow { get; set; }
    public int? DisableReason { get; set; }
    public uint PlayerApplicantLocaleFlags { get; set; }
    public uint RecruitmentLocale { get; set; }

    public WowClubFinderSettingsState PlayerApplicantSettings { get; } = new();
    public WowClubFinderSettingsState ClubRecruitmentSettings { get; } = new();

    public IDictionary<string, bool> PlayerBelongsToClubByFinderGuid { get; } =
        new Dictionary<string, bool>(StringComparer.Ordinal);

    public IDictionary<string, int> ClubTypeByFinderGuid { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public IDictionary<string, int> PostingIdByFinderGuid { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public IDictionary<string, int> ApplicationStatusByFinderGuid { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public IDictionary<string, WowRecruitingClubInfoState>
        RecruitingClubInfoByFinderGuid { get; } =
            new Dictionary<string, WowRecruitingClubInfoState>(
                StringComparer.Ordinal);

    public IDictionary<ulong, WowRecruitingClubInfoState>
        RecruitingClubInfoByClubId { get; } =
            new Dictionary<ulong, WowRecruitingClubInfoState>();

    public IList<WowRecruitingClubInfoState> ClubInvitations { get; } =
        new List<WowRecruitingClubInfoState>();

    public IList<WowRecruitingClubInfoState> PendingCommunities { get; } =
        new List<WowRecruitingClubInfoState>();

    public IList<WowRecruitingClubInfoState> PendingGuilds { get; } =
        new List<WowRecruitingClubInfoState>();

    public IList<WowRecruitingClubInfoState> MatchingCommunities { get; } =
        new List<WowRecruitingClubInfoState>();

    public IList<WowRecruitingClubInfoState> MatchingGuilds { get; } =
        new List<WowRecruitingClubInfoState>();

    public IDictionary<ulong, IList<WowClubFinderApplicantInfoState>>
        ClubApplicantsByClubId { get; } =
            new Dictionary<ulong, IList<WowClubFinderApplicantInfoState>>();

    public IDictionary<ulong, IList<WowClubFinderApplicantInfoState>>
        PendingClubApplicantsByClubId { get; } =
            new Dictionary<ulong, IList<WowClubFinderApplicantInfoState>>();

    public IDictionary<ulong, IList<int>> PostingStatusFlagsById { get; } =
        new Dictionary<ulong, IList<int>>();

    public ISet<ulong> DelistedPostingIds { get; } = new HashSet<ulong>();
    public ISet<ulong> BannedPostingIds { get; } = new HashSet<ulong>();
    public ISet<string> AppliedFinderGuids { get; } =
        new HashSet<string>(StringComparer.Ordinal);
    public ISet<string> InvalidSearchStrings { get; } =
        new HashSet<string>(StringComparer.Ordinal);
    public ISet<ulong> PostingInformationAvailableForClubIds { get; } =
        new HashSet<ulong>();

    public int TotalMatchingCommunityListSize { get; set; }
    public int TotalMatchingGuildListSize { get; set; }
    public bool InvitationListAvailable { get; set; } = true;
    public bool PostClubSucceeds { get; set; }

    public IList<string> AcceptedInvitationGuids { get; } = new List<string>();
    public IList<string> DeclinedInvitationGuids { get; } = new List<string>();
    public IList<string> CancelledMembershipGuids { get; } = new List<string>();
    public IList<WowClubFinderLookupRequest> LookupRequests { get; } =
        new List<WowClubFinderLookupRequest>();
    public IList<int> PendingClubListRequestTypes { get; } = new List<int>();
    public IList<int> ApplicantListRequestTypes { get; } = new List<int>();
    public IList<WowClubFinderClubsListRequest> ClubsListRequests { get; } =
        new List<WowClubFinderClubsListRequest>();
    public IList<WowClubFinderPostClubRequest> PostClubRequests { get; } =
        new List<WowClubFinderPostClubRequest>();
    public IList<WowClubFinderMembershipRequest> MembershipRequests { get; } =
        new List<WowClubFinderMembershipRequest>();
    public IList<WowClubFinderApplicantResponse> ApplicantResponses { get; } =
        new List<WowClubFinderApplicantResponse>();
    public IList<WowClubFinderWhisperRequest> WhisperRequests { get; } =
        new List<WowClubFinderWhisperRequest>();
    public IList<WowClubFinderPageRequest> CommunityPageRequests { get; } =
        new List<WowClubFinderPageRequest>();
    public IList<WowClubFinderPageRequest> GuildPageRequests { get; } =
        new List<WowClubFinderPageRequest>();
    public IList<ulong> PostingInformationRequests { get; } = new List<ulong>();

    public int ClearAllFinderCacheRequests { get; internal set; }
    public int ClearClubApplicantsCacheRequests { get; internal set; }
    public int ClearClubFinderPostingsCacheRequests { get; internal set; }
    public int RequestSubscribedClubPostingIdsRequests { get; internal set; }
    public int ResetClubPostingMapCacheRequests { get; internal set; }
}
