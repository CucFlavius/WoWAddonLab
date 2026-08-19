using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAuctionHouseState
{
    public bool FavoritesAvailable { get; set; } = true;
    public bool HasFullBidResults { get; set; }
    public bool HasFullBrowseResults { get; set; } = true;
    public bool HasFullOwnedAuctionResults { get; set; }
    public bool IsThrottledMessageSystemReady { get; set; } = true;
    public int AvailablePostCount { get; set; }
    public int OwnedAuctionCount { get; set; }
    public int BidCount { get; set; }
    public int ReplicateItemCount { get; set; }
    public int QuoteDurationRemaining { get; set; }
    public ulong? MaxBidItemBid { get; set; }
    public ulong? MaxBidItemBuyout { get; set; }
    public ulong? MaxOwnedAuctionBid { get; set; }
    public ulong? MaxOwnedAuctionBuyout { get; set; }
    public ISet<int> CancelableAuctionIds { get; } = new HashSet<int>();
    public ISet<WowAuctionHouseItemKey> FavoriteItemKeys { get; } =
        new HashSet<WowAuctionHouseItemKey>();
    public IList<WowAuctionHouseItemKey> BidTypes { get; } =
        new List<WowAuctionHouseItemKey>();
    public IList<WowAuctionHouseItemKey> OwnedAuctionTypes { get; } =
        new List<WowAuctionHouseItemKey>();
    public IDictionary<int, int> CommoditySearchResultCounts { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, int> CommoditySearchResultQuantities { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, bool> FullCommoditySearchResults { get; } =
        new Dictionary<int, bool>();
    public IDictionary<int, ulong> MaxCommoditySearchResultPrices { get; } =
        new Dictionary<int, ulong>();
    public IDictionary<WowAuctionHouseItemKey, int> ItemSearchResultCounts
        { get; } = new Dictionary<WowAuctionHouseItemKey, int>();
    public IDictionary<WowAuctionHouseItemKey, int> ItemSearchResultQuantities
        { get; } = new Dictionary<WowAuctionHouseItemKey, int>();
    public IDictionary<WowAuctionHouseItemKey, bool> FullItemSearchResults
        { get; } = new Dictionary<WowAuctionHouseItemKey, bool>();
    public IDictionary<WowAuctionHouseItemKey, int> ItemKeyRequiredLevels
        { get; } = new Dictionary<WowAuctionHouseItemKey, int>();
    public IDictionary<WowAuctionHouseItemKey, ulong> MaxItemSearchResultBids
        { get; } = new Dictionary<WowAuctionHouseItemKey, ulong>();
    public IDictionary<WowAuctionHouseItemKey, ulong> MaxItemSearchResultBuyouts
        { get; } = new Dictionary<WowAuctionHouseItemKey, ulong>();
    public IDictionary<int, string> OwnedAuctionBidderNames { get; } =
        new Dictionary<int, string>();
    public IList<string> Requests { get; } = new List<string>();
}
