namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClubInvitationCandidateQuery(
    string? Filter,
    uint? MaxResults,
    int? CursorPosition,
    bool? AllowFullMatch,
    ulong ClubId);
