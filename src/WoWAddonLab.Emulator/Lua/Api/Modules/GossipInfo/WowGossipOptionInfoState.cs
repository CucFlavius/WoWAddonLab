using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGossipOptionInfoState(
    int? GossipOptionId,
    string Name,
    int Icon,
    IList<WowGossipOptionRewardInfoState> Rewards,
    uint Status,
    int? SpellId,
    int Flags,
    int? OverrideIconId,
    bool SelectOptionWhenOnlyOption,
    int OrderIndex,
    string? FailureDescription);
