namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClubFinderMembershipRequest(
    string ClubFinderGuid,
    string Comment,
    IReadOnlyList<int> SpecIds);
