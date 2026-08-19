using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAccountStoreState
{
    public IDictionary<int, WowAccountStoreCurrencyState> Currencies { get; } =
        new Dictionary<int, WowAccountStoreCurrencyState>();

    public IDictionary<int, int> CurrencyIdsByStoreFront { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, WowAccountStoreFrontState> StoreFrontStates { get; } =
        new Dictionary<int, WowAccountStoreFrontState>();

    public IDictionary<int, WowAccountStoreItemState> Items { get; } =
        new Dictionary<int, WowAccountStoreItemState>();

    public int? PendingPurchaseItemId { get; internal set; }
    public int? PendingRefundItemId { get; internal set; }
    public int? RequestedStoreFrontId { get; internal set; }
}
