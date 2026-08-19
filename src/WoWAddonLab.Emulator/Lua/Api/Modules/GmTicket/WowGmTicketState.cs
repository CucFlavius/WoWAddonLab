using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGmTicketState
{
    public bool RequestServiceAvailable { get; set; } = true;
    public int WebTicketRequestCount { get; set; }
    public int GmStatusRequestCount { get; set; }
}
