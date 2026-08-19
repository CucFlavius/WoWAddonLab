using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowStoreSecureState
{
    public bool AckFailureResult { get; set; }
    public string? PreGeneratedExternalTransactionId { get; set; }
    public string? BnetTransferGuid { get; set; }
    public IList<string> BnetTransferInfo { get; } = [];
    public IDictionary<string, WowStoreCharacterInfo> CharactersByGuid { get; } =
        new Dictionary<string, WowStoreCharacterInfo>(StringComparer.OrdinalIgnoreCase);
    public ISet<string> GuildMasterGuids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public WowStoreConfirmationInfo? ConfirmationInfo { get; set; }
    public int CurrencyId { get; set; }
    public WowStoreCurrencyInfo? CurrencyInfo { get; set; }
    public int CurrencyRegion { get; set; }
    public IDictionary<(string Guid, int ServiceType), IList<WowStoreEligibleRaceInfo>>
        EligibleRaces { get; } =
            new Dictionary<(string Guid, int ServiceType), IList<WowStoreEligibleRaceInfo>>();
    public IDictionary<int, WowStoreEntryInfo> Entries { get; } =
        new Dictionary<int, WowStoreEntryInfo>();
    public int? FailureType { get; set; }
    public int? FailureErrorId { get; set; }
    public int LastProductListResponseError { get; set; }
    public IDictionary<int, WowStoreProductGroupInfo> ProductGroupInfos { get; } =
        new Dictionary<int, WowStoreProductGroupInfo>();
    public IList<WowStoreProductGroup> ProductGroups { get; } = [];
    public IDictionary<int, WowStoreProductInfo> Products { get; } =
        new Dictionary<int, WowStoreProductInfo>();
    public IDictionary<uint, IList<int>> ProductIdsByGroup { get; } =
        new Dictionary<uint, IList<int>>();
    public (string Name, string Description, string Icon)? UnrevokedBoostInfo
    {
        get;
        set;
    }
    public WowStoreVasCompletionInfo? VasCompletionInfo { get; set; }
    public IList<int> VasErrors { get; } = [];
    public IDictionary<string, WowStoreGuildFollowInfo> GuildFollowInfos { get; } =
        new Dictionary<string, WowStoreGuildFollowInfo>(
            StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, WowStoreGuildMasterInfo> GuildMasterInfos { get; } =
        new Dictionary<string, WowStoreGuildMasterInfo>(
            StringComparer.OrdinalIgnoreCase);
    public IList<WowStoreRealmInfo> Realms { get; } = [];
    public IList<WowStoreRealmInfo> VasRealms { get; } = [];
    public IDictionary<int, int> VasServiceTypesByProductId { get; } =
        new Dictionary<int, int>();
    public IDictionary<(string AccountName, bool IsLocal), string> WowAccountGuids
    {
        get;
    } = new Dictionary<(string AccountName, bool IsLocal), string>();
    public bool HasDistributionList { get; set; }
    public ISet<int> DynamicPriceProductIds { get; } = new HashSet<int>();
    public bool HasProductList { get; set; }
    public ISet<int> ProductTypeIds { get; } = new HashSet<int>();
    public bool HasPurchaseInProgress { get; set; }
    public bool HasPurchaseList { get; set; }
    public bool IsAvailable { get; set; }
    public ISet<int> DynamicBundleProductIds { get; } = new HashSet<int>();
    public bool IsRegionLocked { get; set; }
    public ISet<string> VasEligibleCharacterGuids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<int, bool> ProductPurchaseResults { get; } =
        new Dictionary<int, bool>();
    public bool ProductPurchaseConfirmResult { get; set; }
    public bool VasPurchaseResult { get; set; }
    public bool DisconnectOnLogout { get; internal set; }
    public bool VasProductReady { get; internal set; }
    public IList<WowStoreSecureRequest> Requests { get; } = [];
}
