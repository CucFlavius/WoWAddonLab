using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowFogOfWarApiState
{
    public Dictionary<int, int> FogOfWarIdByMap { get; } = [];
    public Dictionary<int, WowFogOfWarInfo> InfoById { get; } = [];
}
