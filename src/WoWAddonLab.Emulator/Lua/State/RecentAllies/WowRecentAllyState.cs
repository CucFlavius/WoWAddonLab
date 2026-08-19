namespace WoWAddonLab.Emulator.Lua;

public sealed class WowRecentAllyState
{
    public required string Guid { get; init; }
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public string RealmName { get; init; } = string.Empty;
    public int Level { get; init; }
    public int ClassId { get; init; }
    public int RaceId { get; init; }
    public int Sex { get; init; }
    public bool IsOnline { get; set; }
    public bool IsDnd { get; set; }
    public bool IsAfk { get; set; }
    public long? PinExpirationDate { get; set; }
    public bool FriendRequestSentThisSession { get; set; }
    public string? CurrentLocation { get; set; }
    public string? Note { get; set; }
    public IList<WowRecentAllyInteractionState> Interactions { get; } =
        new List<WowRecentAllyInteractionState>();
}
