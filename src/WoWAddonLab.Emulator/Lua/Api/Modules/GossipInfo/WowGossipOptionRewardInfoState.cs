using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGossipOptionRewardInfoState(
    int Id,
    int Quantity,
    uint RewardType,
    byte Context);
