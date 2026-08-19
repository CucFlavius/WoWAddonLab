namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLfgRoleUpdateState(
    bool InProgress,
    int SlotCount,
    int MemberCount,
    int? Category,
    int? DungeonId,
    bool IsBattlegroundQueue);
