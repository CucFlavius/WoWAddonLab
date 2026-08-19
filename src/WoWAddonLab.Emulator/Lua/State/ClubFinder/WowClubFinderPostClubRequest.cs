namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClubFinderPostClubRequest(
    ulong ClubId,
    int ItemLevelRequirement,
    string Name,
    string Description,
    uint AvatarId,
    IReadOnlyList<int> SpecIds,
    int ClubType,
    bool CrossFaction);
