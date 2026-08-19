using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLootDropState(
    byte LootListId,
    string ItemHyperlink,
    uint PlayerRollState,
    WowLootPlayerInfoState? CurrentLeader,
    bool IsTied,
    WowLootPlayerInfoState? Winner,
    bool AllPassed,
    IReadOnlyList<WowLootPlayerInfoState> RollInfos,
    int StartTime,
    int Duration);
