using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCatalogShopProductInfo
{
    public int CatalogShopProductId { get; init; }
    public string Name { get; init; } = "";
    public string? Type { get; init; }
    public string Description { get; init; } = "";
    public string IconTexture { get; init; } = "";
    public bool IsFullyOwned { get; init; }
    public bool IsPurchasePending { get; init; }
    public bool Refundable { get; init; }
    public string Price { get; init; } = "";
    public string OriginalPrice { get; init; } = "";
    public int DiscountPercentage { get; init; }
    public int ItemId { get; init; }
    public int MountId { get; init; }
    public string MountTypeName { get; init; } = "";
    public int SpeciesId { get; init; }
    public int TransmogSetId { get; init; }
    public int ItemModifiedAppearanceId { get; init; }
    public IList<WowCatalogShopProductSubItem> SubItems { get; } = [];
    public bool SubItemsLoaded { get; init; }
    public string BackgroundTexture { get; init; } = "";
    public string? ForegroundTexture { get; init; }
    public string? SmallCardBgTexture { get; init; }
    public string? SmallCardFgTexture { get; init; }
    public string? WideCardBgTexture { get; init; }
    public string? WideCardFgTexture { get; init; }
    public string? PreviewIconTexture { get; init; }
    public string? OptionalWideCardBackgroundTexture { get; init; }
    public bool IsBundle { get; init; }
    public int BundleChildrenSize { get; init; }
    public int LicenseTermType { get; init; }
    public int LicenseTermDuration { get; init; }
    public IList<WowCatalogShopVirtualCurrencyGrant> VirtualCurrencies { get; } = [];
    public bool IsHidden { get; init; }
    public bool IsMystery { get; init; }
    public bool HasPendingOrders { get; init; }
    public int NumBundleDetailCards { get; init; }
    public bool IsDynamicallyDiscounted { get; init; }
    public bool ShouldShowOriginalPrice { get; init; }
    public string? WideCardBgOverrideProductUrl { get; init; }
    public string? PreviewBgOverrideProductUrl { get; init; }
    public string? PreviewSmallBgOverrideProductUrl { get; init; }
    public WowCatalogShopDecorQuantity? DecorQuantity { get; init; }
    public bool IsVcProduct { get; init; }
    public bool ContainsHousingItem { get; init; }
}
