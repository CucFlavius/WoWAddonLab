namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubMemberInfoState
{
    public bool IsSelf { get; init; }
    public ulong MemberId { get; init; }
    public string? Name { get; init; }
    public int? Role { get; init; }
    public uint Presence { get; init; }
    public int? ClubType { get; init; }
    public string? Guid { get; init; }
    public int? BnetAccountId { get; init; }
    public string? MemberNote { get; init; }
    public string? OfficerNote { get; init; }
    public int? ClassId { get; init; }
    public int? Race { get; init; }
    public int? Level { get; init; }
    public string? Zone { get; init; }
    public int? AchievementPoints { get; init; }
    public int? Profession1Id { get; init; }
    public int? Profession1Rank { get; init; }
    public string? Profession1Name { get; init; }
    public int? Profession2Id { get; init; }
    public int? Profession2Rank { get; init; }
    public string? Profession2Name { get; init; }
    public int? LastOnlineYear { get; init; }
    public int? LastOnlineMonth { get; init; }
    public int? LastOnlineDay { get; init; }
    public int? LastOnlineHour { get; init; }
    public string? GuildRank { get; init; }
    public int? GuildRankOrder { get; init; }
    public bool? IsRemoteChat { get; init; }
    public int? OverallDungeonScore { get; init; }
    public int? Faction { get; init; }
    public int? TimerunningSeasonId { get; init; }
}
