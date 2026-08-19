namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClubInvitationCandidateState(
    ulong MemberId,
    string Name,
    int Priority,
    uint Status);
