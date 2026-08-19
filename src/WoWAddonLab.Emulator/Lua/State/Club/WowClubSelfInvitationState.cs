namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubSelfInvitationState
{
    public ulong InvitationId { get; init; }
    public WowClubInfoState Club { get; init; } = new();
    public WowClubMemberInfoState Inviter { get; init; } = new();
    public IReadOnlyList<WowClubMemberInfoState> Leaders { get; init; } = [];
}
