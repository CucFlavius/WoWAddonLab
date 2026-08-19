namespace WoWAddonLab.Emulator.Lua;

public sealed class WowRecruitingClubInfoState
{
    public string ClubFinderGuid { get; init; } = string.Empty;
    public int NumActiveMembers { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public string GuildLeader { get; init; } = string.Empty;
    public bool IsGuild { get; init; }
    public int EmblemInfo { get; init; }
    public WowClubFinderTabardInfoState? TabardInfo { get; init; }
    public IReadOnlyList<int> RecruitingSpecIds { get; init; } = [];
    public int RecruitmentFlags { get; init; }
    public bool LocaleSet { get; init; }
    public int RecruitmentLocale { get; init; }
    public int MinItemLevel { get; init; }
    public int Cached { get; init; }
    public int CacheRequested { get; init; }
    public string LastPosterGuid { get; init; } = string.Empty;
    public ulong ClubId { get; init; }
    public int LastUpdatedTime { get; init; }
    public bool IsCrossFaction { get; init; }
    public string? RealmName { get; init; }
}
