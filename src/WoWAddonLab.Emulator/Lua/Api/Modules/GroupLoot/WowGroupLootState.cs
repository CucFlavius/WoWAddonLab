using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGroupLootState
{
    public Dictionary<int, WowLootRollItemInfo> ActiveRolls { get; } = [];
}
