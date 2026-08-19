namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMerchantCost
{
    public int? CurrencyId { get; set; }
    public int? TextureFileId { get; set; }
    public int Quantity { get; set; }
    public string? Link { get; set; }
    public string? Name { get; set; }
    public bool IsCurrency { get; set; }
}
