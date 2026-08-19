namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClubFinderApplicantResponse(
    string ClubFinderGuid,
    string PlayerGuid,
    bool ShouldAccept,
    int RequestType,
    string PlayerName,
    bool ForceAccept,
    bool? Reported);
