namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLfgServerInfoState(
    int DungeonId,
    int Category,
    bool InParty,
    bool Joined,
    bool Queued,
    bool NoPartialClear,
    bool Achievements,
    string Comment,
    int SlotCount,
    bool Leader,
    bool Tank,
    bool Healer,
    bool Damage,
    int TrailingValue);
