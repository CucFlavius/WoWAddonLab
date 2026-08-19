using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowBlackMarketState
{
    public bool IsViewOnly { get; set; }
    public int RequestCount { get; set; }
    public IList<WowBlackMarketItemState> Items { get; } =
        new List<WowBlackMarketItemState>();
    public WowBlackMarketBidRequest? LastBidRequest { get; set; }
}
