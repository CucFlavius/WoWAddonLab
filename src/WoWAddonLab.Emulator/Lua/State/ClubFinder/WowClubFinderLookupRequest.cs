namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClubFinderLookupRequest(
    string ClubFinderGuid,
    bool IsLinkedPosting);
