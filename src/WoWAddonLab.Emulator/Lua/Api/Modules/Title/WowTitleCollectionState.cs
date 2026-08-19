using LuaNET.Lua51;
using System.Text;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTitleCollectionState
{
    public List<WowTitleState> Titles { get; } = [];
    public int CurrentTitleId { get; set; }
    public int? RequestedTitleId { get; set; }
}
