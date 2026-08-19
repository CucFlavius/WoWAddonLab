namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubInfoState
{
    public ulong ClubId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Broadcast { get; init; } = string.Empty;
    public int ClubType { get; init; }
    public int AvatarId { get; init; }
    public int? MemberCount { get; init; }
    public long? FavoriteTimeStamp { get; init; }
    public long? JoinTime { get; init; }
    public bool? SocialQueueingEnabled { get; init; }
    public bool? CrossFaction { get; init; }
}
