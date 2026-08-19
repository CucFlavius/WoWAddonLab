using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSeasonInfoState
{
    public int CurrentDisplaySeasonId { get; set; }
    public IDictionary<int, int> ExpansionBySeasonId { get; } =
        new Dictionary<int, int>();
}
