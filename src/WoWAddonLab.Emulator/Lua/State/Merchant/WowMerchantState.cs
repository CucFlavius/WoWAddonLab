namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMerchantState
{
    public bool IsOpen { get; set; }
    public IList<WowMerchantItemData> Items { get; } = [];
    public IList<WowBuybackItemData> BuybackItems { get; } = [];
    public IList<int> CurrencyIds { get; } = [];
    public int Filter { get; set; } = 2;

    public bool SellAllJunkEnabled { get; set; }
    public int NumJunkItems { get; set; }
    public int SellAllJunkRequestCount { get; set; }

    public bool CanRepair { get; set; }
    public bool CanGuildBankRepair { get; set; }
    public bool IsInRepairMode { get; set; }
    public int RepairAllCost { get; set; }
    public int RepairAllRequestCount { get; set; }
    public bool LastRepairAllUsedGuildBank { get; set; }

    public int CloseRequestCount { get; set; }
    public int ResetFilterRequestCount { get; set; }
    public int? LastPickedItemIndex { get; set; }
    public WowMerchantPurchaseRequest? LastPurchaseRequest { get; set; }
    public int? LastBuybackItemIndex { get; set; }
    public int? BuybackSellCursorMode { get; set; }
}
