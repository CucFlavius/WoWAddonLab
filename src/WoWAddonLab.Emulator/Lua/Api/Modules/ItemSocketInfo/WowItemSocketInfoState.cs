using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowItemSocketInfoState
{
    public bool IsOpen { get; set; }
    public uint CurrentUiType { get; set; }
    public IList<WowItemSocketState> Sockets { get; } =
        new List<WowItemSocketState>();
    public string? SocketItemName { get; set; }
    public uint? SocketItemIconFileDataId { get; set; }
    public byte SocketItemQuality { get; set; }
    public bool SocketItemBoundTradeable { get; set; }
    public bool SocketItemRefundable { get; set; }
    public bool HasBoundGemProposed { get; set; }
    public ISet<int> ArtifactRelicItemIds { get; } = new HashSet<int>();

    public int AcceptSocketsRequests { get; internal set; }
    public int ClickSocketButtonRequests { get; internal set; }
    public uint? LastClickedSocketIndex { get; internal set; }
    public int CloseSocketInfoRequests { get; internal set; }
    public int CompleteSocketingRequests { get; internal set; }
}
