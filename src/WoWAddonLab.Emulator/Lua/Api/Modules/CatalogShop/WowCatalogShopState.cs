using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCatalogShopState
{
    public bool? IsShop2Enabled { get; set; } = true;
    public IList<int> AvailableCategoryIds { get; } = [];
    public IList<WowCatalogShopAvailableRaceInfo> AvailableTransmogRaceInfos { get; } = [];
    public IList<int> NewProductIds { get; } = [];
    public IList<WowCatalogShopVcProductInfo> VcProductInfos { get; } = [];
    public IList<WowCatalogShopRefundableDecorInfo> RefundableDecors { get; } = [];
    public IDictionary<int, WowCatalogShopCategoryInfo> Categories { get; } =
        new Dictionary<int, WowCatalogShopCategoryInfo>();
    public IDictionary<(int CategoryId, int SectionId), WowCatalogShopCategorySectionInfo>
        CategorySections { get; } =
            new Dictionary<(int CategoryId, int SectionId), WowCatalogShopCategorySectionInfo>();
    public IDictionary<int, int> FirstCategoryIdsByProductId { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, WowCatalogShopProductDisplayInfo> ProductDisplayInfos { get; } =
        new Dictionary<int, WowCatalogShopProductDisplayInfo>();
    public IDictionary<int, WowCatalogShopProductInfo> Products { get; } =
        new Dictionary<int, WowCatalogShopProductInfo>();
    public IDictionary<int, IList<WowCatalogShopBundleChildInfo>> BundleChildren { get; } =
        new Dictionary<int, IList<WowCatalogShopBundleChildInfo>>();
    public IDictionary<int, IList<int>> ProductIdsByCategory { get; } =
        new Dictionary<int, IList<int>>();
    public IDictionary<(int CategoryId, int SectionId), IList<int>>
        ProductIdsByCategorySection { get; } =
            new Dictionary<(int CategoryId, int SectionId), IList<int>>();
    public IDictionary<int, IList<int>> SectionIdsByCategory { get; } =
        new Dictionary<int, IList<int>>();
    public IDictionary<(int CategoryId, int SectionId, int ProductId), int>
        ProductSortOrders { get; } =
            new Dictionary<(int CategoryId, int SectionId, int ProductId), int>();
    public IDictionary<int, int> ProductAvailabilitySeconds { get; } =
        new Dictionary<int, int>();
    public IDictionary<uint, WowCatalogShopSpellVisualInfo> MountSpellVisualInfos { get; } =
        new Dictionary<uint, WowCatalogShopSpellVisualInfo>();
    public IDictionary<string, string> VirtualCurrencyBalances { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
    public IDictionary<(string CurrencyCode, uint AmountNeeded), int>
        BestCurrencyProducts { get; } =
            new Dictionary<(string CurrencyCode, uint AmountNeeded), int>();
    public ISet<int> ProductsIncludedInBundles { get; } = new HashSet<int>();
    public IDictionary<int, bool> PurchaseResults { get; } =
        new Dictionary<int, bool>();
    public IList<WowCatalogShopTelemetryEntry> Telemetry { get; } = [];
    public IList<WowCatalogShopRequest> Requests { get; } = [];

    public bool BulkPurchaseResult { get; set; }
    public bool ShouldShowHousingWarning { get; set; }
    public int FailureType { get; set; }
    public int MinimumRefundableDecorTimeRemainingSeconds { get; set; }
    public string? ShoppingSessionUuid { get; set; }
    public WowCatalogShopInteractionSource InteractionSource { get; internal set; }
    public int RefreshRefundableDecorsCount { get; internal set; }
    public string? LastVirtualCurrencyRefreshCode { get; internal set; }
    public int? PendingHousingVcProductId { get; internal set; }
}
