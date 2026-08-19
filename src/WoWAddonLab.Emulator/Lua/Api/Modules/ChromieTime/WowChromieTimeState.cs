using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowChromieTimeState
{
    public IList<WowChromieTimeExpansionInfoState> ExpansionOptions { get; } =
        new List<WowChromieTimeExpansionInfoState>();
    public int? LastSelectedExpansionInfoId { get; set; }
}
