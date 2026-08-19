namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClubFinderClubsListRequest(
    bool GuildListRequested,
    string SearchString,
    IReadOnlyList<int> SpecIds);
