namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMerchantItemData
{
    public WowMerchantItemKind Kind { get; set; } = WowMerchantItemKind.Item;
    public int? ItemId { get; set; }
    public string? Name { get; set; }
    public int? TextureFileId { get; set; }
    public double Price { get; set; }
    public int StackCount { get; set; } = 1;
    public int NumAvailable { get; set; }
    public bool IsPurchasable { get; set; }
    public bool IsUsable { get; set; }
    public bool HasExtendedCost { get; set; }
    public int? CurrencyId { get; set; }
    public int? SpellId { get; set; }
    public bool IsQuestStartItem { get; set; }
    public string? Link { get; set; }
    public int MaxStack { get; set; } = 1;
    public bool? CanAfford { get; set; }
    public bool IsRefundable { get; set; }
    public IList<WowMerchantCost> Costs { get; } = [];
}
