using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAuctionHouseApi : LuaApiModule
{
    private const int MaxFavorites = 100;

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CalculateCommodityDeposit",
        "CalculateItemDeposit",
        "CanCancelAuction",
        "CancelAuction",
        "CancelCommoditiesPurchase",
        "CancelSell",
        "CloseAuctionHouse",
        "ConfirmCommoditiesPurchase",
        "ConfirmPostCommodity",
        "ConfirmPostItem",
        "FavoritesAreAvailable",
        "GetAuctionInfoByID",
        "GetAuctionItemSubClasses",
        "GetAvailablePostCount",
        "GetBidInfo",
        "GetBidType",
        "GetBids",
        "GetBrowseResults",
        "GetCancelCost",
        "GetCommoditySearchResultInfo",
        "GetCommoditySearchResultsQuantity",
        "GetExtraBrowseInfo",
        "GetFilterGroups",
        "GetItemCommodityStatus",
        "GetItemKeyFromItem",
        "GetItemKeyInfo",
        "GetItemKeyRequiredLevel",
        "GetItemSearchResultInfo",
        "GetItemSearchResultsQuantity",
        "GetMaxBidItemBid",
        "GetMaxBidItemBuyout",
        "GetMaxCommoditySearchResultPrice",
        "GetMaxItemSearchResultBid",
        "GetMaxItemSearchResultBuyout",
        "GetMaxOwnedAuctionBid",
        "GetMaxOwnedAuctionBuyout",
        "GetNumBidTypes",
        "GetNumBids",
        "GetNumCommoditySearchResults",
        "GetNumItemSearchResults",
        "GetNumOwnedAuctionTypes",
        "GetNumOwnedAuctions",
        "GetNumReplicateItems",
        "GetOwnedAuctionInfo",
        "GetOwnedAuctionType",
        "GetOwnedAuctions",
        "GetQuoteDurationRemaining",
        "GetReplicateItemBattlePetInfo",
        "GetReplicateItemInfo",
        "GetReplicateItemLink",
        "GetReplicateItemTimeLeft",
        "GetTimeLeftBandInfo",
        "HasFavorites",
        "HasFullBidResults",
        "HasFullBrowseResults",
        "HasFullCommoditySearchResults",
        "HasFullItemSearchResults",
        "HasFullOwnedAuctionResults",
        "HasMaxFavorites",
        "HasSearchResults",
        "IsFavoriteItem",
        "IsSellItemValid",
        "IsThrottledMessageSystemReady",
        "MakeItemKey",
        "PlaceBid",
        "PostCommodity",
        "PostItem",
        "QueryBids",
        "QueryOwnedAuctions",
        "RefreshCommoditySearchResults",
        "RefreshItemSearchResults",
        "ReplicateItems",
        "RequestMoreBrowseResults",
        "RequestMoreCommoditySearchResults",
        "RequestMoreItemSearchResults",
        "RequestOwnedAuctionBidderInfo",
        "SearchForFavorites",
        "SearchForItemKeys",
        "SendBrowseQuery",
        "SendSearchQuery",
        "SendSellSearchQuery",
        "SetFavoriteItem",
        "ShouldAutoPopulatePrice",
        "StartCommoditiesPurchase",
        "SupportsCopperValues"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_AuctionHouse");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var auctionHouse = runtime.AuctionHouse;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var usage = $"Usage: C_AuctionHouse.{operation}";

        switch (operation)
        {
            case "CalculateCommodityDeposit":
                _ = RequiredInt32(state, 1, usage);
                _ = RequiredOneBasedIndex(state, 2, usage);
                _ = RequiredUInt32(state, 3, usage);
                return PushNil(state);
            case "CalculateItemDeposit":
                RequiredItemHandle(state, 1, usage);
                _ = RequiredOneBasedIndex(state, 2, usage);
                _ = RequiredUInt32(state, 3, usage);
                return PushNil(state);
            case "CanCancelAuction":
                return PushBoolean(
                    state,
                    auctionHouse.CancelableAuctionIds.Contains(
                        RequiredInt32(state, 1, usage)));
            case "CancelAuction":
                auctionHouse.Requests.Add(
                    $"{operation}:{RequiredInt32(state, 1, usage)}");
                return 0;
            case "CancelCommoditiesPurchase":
            case "CancelSell":
            case "ReplicateItems":
            case "RequestMoreBrowseResults":
                auctionHouse.Requests.Add(operation);
                return 0;
            case "CloseAuctionHouse":
                runtime.TriggerEvent("AUCTION_HOUSE_CLOSED");
                return 0;
            case "ConfirmCommoditiesPurchase":
                _ = RequiredInt32(state, 1, usage);
                _ = RequiredUInt32(state, 2, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "ConfirmPostCommodity":
                RequiredItemHandle(state, 1, usage);
                _ = RequiredOneBasedIndex(state, 2, usage);
                _ = RequiredUInt32(state, 3, usage);
                _ = RequiredUInt64(state, 4, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "ConfirmPostItem":
                ValidatePostItem(state, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "FavoritesAreAvailable":
                return PushBoolean(state, auctionHouse.FavoritesAvailable);
            case "GetAuctionInfoByID":
                _ = RequiredInt32(state, 1, usage);
                return PushNil(state);
            case "GetAuctionItemSubClasses":
                _ = RequiredInt32(state, 1, usage);
                return PushEmptyTable(state);
            case "GetAvailablePostCount":
                RequiredItemHandle(state, 1, usage);
                return PushInteger(state, auctionHouse.AvailablePostCount);
            case "GetBidInfo":
                _ = RequiredOneBasedIndex(state, 1, usage);
                return PushNil(state);
            case "GetBidType":
                return PushIndexedItemKey(
                    state,
                    auctionHouse.BidTypes,
                    RequiredOneBasedIndex(state, 1, usage));
            case "GetBids":
            case "GetBrowseResults":
            case "GetFilterGroups":
            case "GetOwnedAuctions":
                return PushEmptyTable(state);
            case "GetCancelCost":
                _ = RequiredInt32(state, 1, usage);
                return PushInteger(state, 0);
            case "GetCommoditySearchResultInfo":
                _ = RequiredInt32(state, 1, usage);
                _ = RequiredOneBasedIndex(state, 2, usage);
                return PushNil(state);
            case "GetCommoditySearchResultsQuantity":
                return PushInteger(
                    state,
                    GetOrZero(
                        auctionHouse.CommoditySearchResultQuantities,
                        RequiredInt32(state, 1, usage)));
            case "GetExtraBrowseInfo":
                _ = RequiredItemKey(state, 1, usage);
                return 0;
            case "GetItemCommodityStatus":
                RequiredItemHandle(state, 1, usage);
                return PushInteger(state, 0);
            case "GetItemKeyFromItem":
                RequiredItemHandle(state, 1, usage);
                PushItemKey(state, new WowAuctionHouseItemKey(0, 0, 0, 0));
                return 1;
            case "GetItemKeyInfo":
                _ = RequiredItemKey(state, 1, usage);
                _ = OptionalBoolean(state, 2, false);
                return PushNil(state);
            case "GetItemKeyRequiredLevel":
            {
                var itemKey = RequiredItemKey(state, 1, usage);
                if (!auctionHouse.ItemKeyRequiredLevels.TryGetValue(
                        itemKey,
                        out var requiredLevel))
                {
                    return 0;
                }
                return PushInteger(state, requiredLevel);
            }
            case "GetItemSearchResultInfo":
                _ = RequiredItemKey(state, 1, usage);
                _ = RequiredOneBasedIndex(state, 2, usage);
                return PushNil(state);
            case "GetItemSearchResultsQuantity":
                return PushInteger(
                    state,
                    GetOrZero(
                        auctionHouse.ItemSearchResultQuantities,
                        RequiredItemKey(state, 1, usage)));
            case "GetMaxBidItemBid":
                return PushOptionalMoney(state, auctionHouse.MaxBidItemBid);
            case "GetMaxBidItemBuyout":
                return PushOptionalMoney(state, auctionHouse.MaxBidItemBuyout);
            case "GetMaxCommoditySearchResultPrice":
            {
                var itemId = RequiredInt32(state, 1, usage);
                return PushOptionalMoney(
                    state,
                    auctionHouse.MaxCommoditySearchResultPrices.TryGetValue(
                        itemId,
                        out var price)
                        ? price
                        : null);
            }
            case "GetMaxItemSearchResultBid":
            {
                var key = RequiredItemKey(state, 1, usage);
                return PushOptionalMoney(
                    state,
                    auctionHouse.MaxItemSearchResultBids.TryGetValue(
                        key,
                        out var price)
                        ? price
                        : null);
            }
            case "GetMaxItemSearchResultBuyout":
            {
                var key = RequiredItemKey(state, 1, usage);
                return PushOptionalMoney(
                    state,
                    auctionHouse.MaxItemSearchResultBuyouts.TryGetValue(
                        key,
                        out var price)
                        ? price
                        : null);
            }
            case "GetMaxOwnedAuctionBid":
                return PushOptionalMoney(state, auctionHouse.MaxOwnedAuctionBid);
            case "GetMaxOwnedAuctionBuyout":
                return PushOptionalMoney(
                    state,
                    auctionHouse.MaxOwnedAuctionBuyout);
            case "GetNumBidTypes":
                return PushInteger(state, auctionHouse.BidTypes.Count);
            case "GetNumBids":
                return PushInteger(state, auctionHouse.BidCount);
            case "GetNumCommoditySearchResults":
                return PushInteger(
                    state,
                    GetOrZero(
                        auctionHouse.CommoditySearchResultCounts,
                        RequiredInt32(state, 1, usage)));
            case "GetNumItemSearchResults":
                return PushInteger(
                    state,
                    GetOrZero(
                        auctionHouse.ItemSearchResultCounts,
                        RequiredItemKey(state, 1, usage)));
            case "GetNumOwnedAuctionTypes":
                return PushInteger(state, auctionHouse.OwnedAuctionTypes.Count);
            case "GetNumOwnedAuctions":
                return PushInteger(state, auctionHouse.OwnedAuctionCount);
            case "GetNumReplicateItems":
                return PushInteger(state, auctionHouse.ReplicateItemCount);
            case "GetOwnedAuctionInfo":
                _ = RequiredOneBasedIndex(state, 1, usage);
                return PushNil(state);
            case "GetOwnedAuctionType":
                return PushIndexedItemKey(
                    state,
                    auctionHouse.OwnedAuctionTypes,
                    RequiredOneBasedIndex(state, 1, usage));
            case "GetQuoteDurationRemaining":
                return PushInteger(
                    state,
                    Math.Max(0, auctionHouse.QuoteDurationRemaining));
            case "GetReplicateItemBattlePetInfo":
                _ = RequiredUInt32(state, 1, usage);
                return 0;
            case "GetReplicateItemInfo":
                _ = RequiredUInt32(state, 1, usage);
                return PushEmptyReplicateItemInfo(state);
            case "GetReplicateItemLink":
                _ = RequiredUInt32(state, 1, usage);
                return PushNil(state);
            case "GetReplicateItemTimeLeft":
                _ = RequiredUInt32(state, 1, usage);
                return PushInteger(state, 0);
            case "GetTimeLeftBandInfo":
                return PushTimeLeftBand(
                    state,
                    RequiredByteEnum(state, 1, 3, usage));
            case "HasFavorites":
                return PushBoolean(
                    state,
                    auctionHouse.FavoriteItemKeys.Count != 0);
            case "HasFullBidResults":
                return PushBoolean(state, auctionHouse.HasFullBidResults);
            case "HasFullBrowseResults":
                return PushBoolean(state, auctionHouse.HasFullBrowseResults);
            case "HasFullCommoditySearchResults":
            {
                var itemId = RequiredInt32(state, 1, usage);
                return PushBoolean(
                    state,
                    auctionHouse.FullCommoditySearchResults.TryGetValue(
                        itemId,
                        out var hasFullResults) &&
                    hasFullResults);
            }
            case "HasFullItemSearchResults":
            {
                var itemKey = RequiredItemKey(state, 1, usage);
                return PushBoolean(
                    state,
                    auctionHouse.FullItemSearchResults.TryGetValue(
                        itemKey,
                        out var hasFullResults) &&
                    hasFullResults);
            }
            case "HasFullOwnedAuctionResults":
                return PushBoolean(
                    state,
                    auctionHouse.HasFullOwnedAuctionResults);
            case "HasMaxFavorites":
                return PushBoolean(
                    state,
                    auctionHouse.FavoriteItemKeys.Count >= MaxFavorites);
            case "HasSearchResults":
            {
                var itemKey = RequiredItemKey(state, 1, usage);
                return PushBoolean(
                    state,
                    auctionHouse.ItemSearchResultCounts.ContainsKey(itemKey) ||
                    auctionHouse.ItemSearchResultQuantities.ContainsKey(itemKey) ||
                    auctionHouse.FullItemSearchResults.ContainsKey(itemKey));
            }
            case "IsFavoriteItem":
                return PushBoolean(
                    state,
                    auctionHouse.FavoriteItemKeys.Contains(
                        RequiredItemKey(state, 1, usage)));
            case "IsSellItemValid":
                RequiredItemHandle(state, 1, usage);
                _ = OptionalBoolean(state, 2, true);
                return PushBoolean(state, false);
            case "IsThrottledMessageSystemReady":
                return PushBoolean(
                    state,
                    auctionHouse.IsThrottledMessageSystemReady);
            case "MakeItemKey":
            {
                var itemKey = new WowAuctionHouseItemKey(
                    RequiredUInt32(state, 1, usage),
                    OptionalUInt16(state, 2, 0, usage),
                    OptionalUInt16(state, 3, 0, usage),
                    OptionalUInt32(state, 4, 0, usage));
                PushItemKey(state, itemKey);
                return 1;
            }
            case "PlaceBid":
                _ = RequiredInt32(state, 1, usage);
                _ = RequiredUInt64(state, 2, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "PostCommodity":
                RequiredItemHandle(state, 1, usage);
                _ = RequiredOneBasedIndex(state, 2, usage);
                _ = RequiredUInt32(state, 3, usage);
                _ = RequiredUInt64(state, 4, usage);
                auctionHouse.Requests.Add(operation);
                return PushBoolean(state, false);
            case "PostItem":
                ValidatePostItem(state, usage);
                auctionHouse.Requests.Add(operation);
                return PushBoolean(state, false);
            case "QueryBids":
                RequiredTable(state, 1, usage);
                _ = RequiredInt32Array(state, 2, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "QueryOwnedAuctions":
            case "SearchForFavorites":
                RequiredTable(state, 1, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "RefreshCommoditySearchResults":
                _ = RequiredInt32(state, 1, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "RefreshItemSearchResults":
                _ = RequiredItemKey(state, 1, usage);
                _ = OptionalUInt32(state, 2, 0, usage);
                _ = OptionalUInt32(state, 3, 0, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "RequestMoreCommoditySearchResults":
                _ = RequiredInt32(state, 1, usage);
                auctionHouse.Requests.Add(operation);
                return PushBoolean(state, true);
            case "RequestMoreItemSearchResults":
                _ = RequiredItemKey(state, 1, usage);
                auctionHouse.Requests.Add(operation);
                return PushBoolean(state, true);
            case "RequestOwnedAuctionBidderInfo":
            {
                var auctionId = RequiredInt32(state, 1, usage);
                if (auctionHouse.OwnedAuctionBidderNames.TryGetValue(
                        auctionId,
                        out var bidderName))
                {
                    lua_pushstring(state, bidderName);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "SearchForItemKeys":
                _ = RequiredItemKeyArray(state, 1, usage);
                RequiredTable(state, 2, usage);
                auctionHouse.Requests.Add(operation);
                return PushEmptyTable(state);
            case "SendBrowseQuery":
                RequiredTable(state, 1, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "SendSearchQuery":
                _ = RequiredItemKey(state, 1, usage);
                RequiredTable(state, 2, usage);
                _ = RequiredBoolean(state, 3, usage);
                _ = OptionalUInt32(state, 4, 0, usage);
                _ = OptionalUInt32(state, 5, 0, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "SendSellSearchQuery":
                _ = RequiredItemKey(state, 1, usage);
                RequiredTable(state, 2, usage);
                _ = RequiredBoolean(state, 3, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "SetFavoriteItem":
            {
                var itemKey = RequiredItemKey(state, 1, usage);
                if (RequiredBoolean(state, 2, usage))
                {
                    if (auctionHouse.FavoriteItemKeys.Count < MaxFavorites)
                        auctionHouse.FavoriteItemKeys.Add(itemKey);
                }
                else
                {
                    auctionHouse.FavoriteItemKeys.Remove(itemKey);
                }
                return 0;
            }
            case "ShouldAutoPopulatePrice":
                return PushBoolean(state, true);
            case "StartCommoditiesPurchase":
                _ = RequiredInt32(state, 1, usage);
                _ = RequiredUInt32(state, 2, usage);
                auctionHouse.Requests.Add(operation);
                return 0;
            case "SupportsCopperValues":
                return PushBoolean(state, false);
            default:
                return 0;
        }
    }

    private static void ValidatePostItem(lua_State state, string usage)
    {
        RequiredItemHandle(state, 1, usage);
        _ = RequiredOneBasedIndex(state, 2, usage);
        _ = RequiredUInt32(state, 3, usage);
        _ = OptionalUInt64(state, 4, usage);
        _ = OptionalUInt64(state, 5, usage);
    }

    private static int PushEmptyReplicateItemInfo(lua_State state)
    {
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushnumber(state, 0);
        lua_pushnumber(state, 0);
        lua_pushnil(state);
        lua_pushnumber(state, 0);
        lua_pushnil(state);
        lua_pushnumber(state, 0);
        lua_pushnumber(state, 0);
        lua_pushnumber(state, 0);
        lua_pushnumber(state, 0);
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushnumber(state, 0);
        lua_pushnumber(state, 0);
        lua_pushnil(state);
        return 18;
    }

    private static int PushTimeLeftBand(lua_State state, byte band)
    {
        (int Min, int Max)[] bands =
        [
            (0, 1_800),
            (1_800, 7_200),
            (7_200, 43_200),
            (43_200, 172_800)
        ];
        lua_pushnumber(state, bands[band].Min);
        lua_pushnumber(state, bands[band].Max);
        return 2;
    }

    private static int PushIndexedItemKey(
        lua_State state,
        IList<WowAuctionHouseItemKey> values,
        uint zeroBasedIndex)
    {
        if (zeroBasedIndex >= values.Count)
            return PushNil(state);
        PushItemKey(state, values[(int)zeroBasedIndex]);
        return 1;
    }

    private static void PushItemKey(
        lua_State state,
        WowAuctionHouseItemKey itemKey)
    {
        lua_createtable(state, 0, 4);
        SetInteger(state, "itemID", itemKey.ItemId);
        SetInteger(state, "itemLevel", itemKey.ItemLevel);
        SetInteger(state, "itemSuffix", itemKey.ItemSuffix);
        SetInteger(state, "battlePetSpeciesID", itemKey.BattlePetSpeciesId);
    }

    private static WowAuctionHouseItemKey RequiredItemKey(
        lua_State state,
        int index,
        string usage)
    {
        RequiredTable(state, index, usage);
        var absolute = AbsoluteIndex(state, index);
        return new WowAuctionHouseItemKey(
            RequiredUInt32Field(state, absolute, "itemID", usage),
            RequiredUInt16Field(state, absolute, "itemLevel", usage),
            RequiredUInt16Field(state, absolute, "itemSuffix", usage),
            RequiredUInt32Field(
                state,
                absolute,
                "battlePetSpeciesID",
                usage));
    }

    private static IReadOnlyList<WowAuctionHouseItemKey> RequiredItemKeyArray(
        lua_State state,
        int index,
        string usage)
    {
        RequiredTable(state, index, usage);
        var absolute = AbsoluteIndex(state, index);
        var count = checked((int)lua_objlen(state, absolute));
        var result = new List<WowAuctionHouseItemKey>(count);
        for (var item = 1; item <= count; item++)
        {
            lua_rawgeti(state, absolute, item);
            result.Add(RequiredItemKey(state, -1, usage));
            lua_pop(state, 1);
        }
        return result;
    }

    private static IReadOnlyList<int> RequiredInt32Array(
        lua_State state,
        int index,
        string usage)
    {
        RequiredTable(state, index, usage);
        var absolute = AbsoluteIndex(state, index);
        var count = checked((int)lua_objlen(state, absolute));
        var result = new List<int>(count);
        for (var item = 1; item <= count; item++)
        {
            lua_rawgeti(state, absolute, item);
            result.Add(RequiredInt32(state, -1, usage));
            lua_pop(state, 1);
        }
        return result;
    }

    private static uint RequiredUInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var result = RequiredUInt32(state, -1, usage);
        lua_pop(state, 1);
        return result;
    }

    private static ushort RequiredUInt16Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var result = RequiredUInt16(state, -1, usage);
        lua_pop(state, 1);
        return result;
    }

    private static void RequiredTable(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TTABLE)
            RaiseArgumentError(state, usage);
    }

    private static void RequiredItemHandle(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) ||
            lua_type(state, index) is not (LUA_TTABLE or LUA_TUSERDATA))
        {
            RaiseArgumentError(state, usage);
        }
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        var number = RequiredNumber(state, index, usage);
        if (number < int.MinValue || number > int.MaxValue)
            return RaiseArgumentError(state, usage);
        return unchecked((int)number);
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        var number = RequiredNumber(state, index, usage);
        if (number < uint.MinValue || number > uint.MaxValue)
        {
            RaiseArgumentError(state, usage);
            return 0;
        }
        return unchecked((uint)number);
    }

    private static ushort RequiredUInt16(
        lua_State state,
        int index,
        string usage)
    {
        var number = RequiredNumber(state, index, usage);
        if (number < ushort.MinValue || number > ushort.MaxValue)
        {
            RaiseArgumentError(state, usage);
            return 0;
        }
        return unchecked((ushort)number);
    }

    private static ulong RequiredUInt64(
        lua_State state,
        int index,
        string usage)
    {
        var number = RequiredNumber(state, index, usage);
        if (number < 0 || number > ulong.MaxValue)
        {
            RaiseArgumentError(state, usage);
            return 0;
        }
        return unchecked((ulong)number);
    }

    private static uint RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage) =>
        unchecked(RequiredUInt32(state, index, usage) - 1);

    private static byte RequiredByteEnum(
        lua_State state,
        int index,
        byte maximum,
        string usage)
    {
        var value = RequiredUInt32(state, index, usage);
        if (value > maximum)
        {
            RaiseArgumentError(state, usage);
            return 0;
        }
        return (byte)value;
    }

    private static double RequiredNumber(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            RaiseArgumentError(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || Math.Truncate(number) != number)
        {
            RaiseArgumentError(state, usage);
            return 0;
        }
        return number;
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state))
            RaiseArgumentError(state, usage);
        return lua_toboolean(state, index) != 0;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        bool defaultValue)
    {
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return defaultValue;
        return lua_toboolean(state, index) != 0;
    }

    private static uint OptionalUInt32(
        lua_State state,
        int index,
        uint defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return defaultValue;
        return RequiredUInt32(state, index, usage);
    }

    private static ushort OptionalUInt16(
        lua_State state,
        int index,
        ushort defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return defaultValue;
        return RequiredUInt16(state, index, usage);
    }

    private static ulong? OptionalUInt64(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return null;
        return RequiredUInt64(state, index, usage);
    }

    private static int RaiseArgumentError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static int PushInteger(lua_State state, double value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushNil(lua_State state)
    {
        lua_pushnil(state);
        return 1;
    }

    private static int PushOptionalMoney(lua_State state, ulong? value)
    {
        if (value is { } amount)
            lua_pushnumber(state, amount);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int PushEmptyTable(lua_State state)
    {
        lua_newtable(state);
        return 1;
    }

    private static int GetOrZero<TKey>(
        IDictionary<TKey, int> values,
        TKey key)
        where TKey : notnull =>
        values.TryGetValue(key, out var value) ? value : 0;

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static void SetInteger(
        lua_State state,
        string key,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        SetEnum(
            state,
            "AuctionHouseFilterCategory",
            ("Uncategorized", 0),
            ("Equipment", 1),
            ("Rarity", 2));
        SetEnumMeta(state, "AuctionHouseFilterCategoryMeta", 0, 2, 3);
        SetEnum(
            state,
            "AuctionStatus",
            ("Active", 0),
            ("Sold", 1));
        SetEnumMeta(state, "AuctionStatusMeta", 0, 1, 2);
        SetEnum(
            state,
            "ItemCommodityStatus",
            ("Unknown", 0),
            ("Item", 1),
            ("Commodity", 2));
        SetEnumMeta(state, "ItemCommodityStatusMeta", 0, 2, 3);
        lua_setglobal(state, "Enum");
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] values)
    {
        lua_createtable(state, 0, values.Length);
        foreach (var value in values)
            SetInteger(state, value.Name, value.Value);
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int minimum,
        int maximum,
        int count) =>
        SetEnum(
            state,
            name,
            ("MinValue", minimum),
            ("MaxValue", maximum),
            ("NumValues", count));
}
