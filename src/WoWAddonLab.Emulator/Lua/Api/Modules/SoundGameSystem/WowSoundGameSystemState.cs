using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSoundGameSystemState
{
    public bool Available { get; set; }
    public string NoneName { get; set; } = "None";
    public string SystemDefaultName { get; set; } = "System Default";
    public IList<string> OutputDriverNames { get; } = new List<string>();
}
