namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLfgProposalState(
    int DungeonId,
    int TypeId,
    int SubtypeId,
    string Name,
    int? BackgroundTexture,
    string Role,
    bool HasResponded,
    int TotalEncounters,
    int CompletedEncounters,
    int MemberCount,
    bool IsLeader,
    bool IsHoliday,
    int Category,
    bool IsSilent);
