using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTradeInfoState
{
    public bool IsTradeOpen { get; set; }
    public bool ShouldShowTradeOfferWarning { get; set; }

    public int CloseTradeRequests { get; internal set; }
    public int AddTradeMoneyRequests { get; internal set; }
    public int SuccessfulAddTradeMoneyRequests { get; internal set; }
    public int PickupTradeMoneyRequests { get; internal set; }
    public int SuccessfulPickupTradeMoneyRequests { get; internal set; }
    public ulong? LastPickupTradeMoneyAmount { get; internal set; }
    public int SetTradeMoneyRequests { get; internal set; }
    public int SuccessfulSetTradeMoneyRequests { get; internal set; }
    public ulong? LastRequestedTradeMoneyAmount { get; internal set; }
    public ulong? LastClampedTradeMoneyAmount { get; internal set; }
}
