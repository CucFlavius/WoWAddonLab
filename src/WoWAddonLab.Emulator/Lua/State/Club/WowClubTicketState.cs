namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubTicketState
{
    public string TicketId { get; init; } = string.Empty;
    public int AllowedRedeemCount { get; init; }
    public int CurrentRedeemCount { get; init; }
    public double CreationTime { get; init; }
    public double ExpirationTime { get; init; }
    public ulong? DefaultStreamId { get; init; }
    public WowClubMemberInfoState Creator { get; init; } = new();
}
