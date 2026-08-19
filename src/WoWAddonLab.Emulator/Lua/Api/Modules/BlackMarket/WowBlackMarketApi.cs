using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowBlackMarketApi : LuaApiModule
{
    private const int BlackMarketInteractionType = 27;

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "Close",
        "GetHotItem",
        "GetItemInfoByID",
        "GetItemInfoByIndex",
        "GetNumItems",
        "IsViewOnly",
        "ItemPlaceBid",
        "RequestItems"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_BlackMarket");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var blackMarket = runtime.BlackMarket;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "Close":
                Close(runtime);
                return 0;
            case "RequestItems":
                if (IsAvailable(runtime))
                    blackMarket.RequestCount++;
                return 0;
            case "GetNumItems":
                if (!IsAvailable(runtime))
                    return 0;
                lua_pushnumber(state, blackMarket.Items.Count);
                return 1;
            case "GetItemInfoByIndex":
                if (!IsAvailable(runtime) ||
                    !TryReadInt32(state, 1, out var oneBasedIndex))
                {
                    return 0;
                }
                var zeroBasedIndex = unchecked(oneBasedIndex - 1);
                if ((uint)zeroBasedIndex >= blackMarket.Items.Count)
                    return 0;
                return PushItemInfo(state, blackMarket.Items[zeroBasedIndex]);
            case "GetItemInfoByID":
                if (!IsAvailable(runtime) ||
                    !TryReadInt32(state, 1, out var marketId))
                {
                    return 0;
                }
                var item = blackMarket.Items.FirstOrDefault(
                    value => value.MarketId == marketId);
                return item is null ? 0 : PushItemInfo(state, item);
            case "GetHotItem":
                if (!IsAvailable(runtime))
                    return 0;
                var hotItem = blackMarket.Items
                    .Where(value => value.TimeLeftSeconds > 0)
                    .Select((value, index) => (Value: value, Index: index))
                    .OrderBy(value => value.Value.NumBids)
                    .ThenBy(value => value.Index)
                    .LastOrDefault()
                    .Value;
                return hotItem is null ? 0 : PushItemInfo(state, hotItem);
            case "IsViewOnly":
                if (!IsAvailable(runtime))
                    return 0;
                lua_pushboolean(state, blackMarket.IsViewOnly ? 1 : 0);
                return 1;
            case "ItemPlaceBid":
                PlaceBid(state, runtime, blackMarket);
                return 0;
            default:
                return 0;
        }
    }

    private static void Close(LuaRuntime runtime)
    {
        var interactions = runtime.PlayerInteractions;
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = BlackMarketInteractionType;
        if (!IsAvailable(runtime))
            return;

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
        runtime.TriggerEvent("BLACK_MARKET_CLOSE");
    }

    private static void PlaceBid(
        lua_State state,
        LuaRuntime runtime,
        WowBlackMarketState blackMarket)
    {
        if (!IsAvailable(runtime) ||
            !TryReadInt32(state, 1, out var marketId) ||
            !TryReadUInt64(state, 2, out var bidAmount))
        {
            return;
        }

        var item = blackMarket.Items.FirstOrDefault(
            value => value.MarketId == marketId);
        if (item is null ||
            item.TimeLeftSeconds <= 0 ||
            bidAmount < item.MinBid ||
            bidAmount < SaturatingAdd(item.CurrentBid, item.MinIncrement))
        {
            return;
        }

        blackMarket.LastBidRequest =
            new WowBlackMarketBidRequest(marketId, bidAmount);
    }

    private static ulong SaturatingAdd(ulong left, ulong right) =>
        ulong.MaxValue - left < right ? ulong.MaxValue : left + right;

    private static int PushItemInfo(
        lua_State state,
        WowBlackMarketItemState item)
    {
        lua_pushstring(state, item.Name);
        PushOptionalInteger(state, item.TextureFileId);
        lua_pushnumber(state, item.Quantity);
        PushOptionalString(state, item.ItemType);
        lua_pushboolean(state, item.IsUsable ? 1 : 0);
        lua_pushnumber(state, item.Level);
        PushOptionalString(state, item.LevelType);
        PushOptionalString(state, item.SellerName);
        lua_pushnumber(state, item.MinBid);
        lua_pushnumber(state, item.MinIncrement);
        lua_pushnumber(state, item.CurrentBid);
        lua_pushboolean(state, item.HasPlayerBid ? 1 : 0);
        lua_pushnumber(state, item.NumBids);
        lua_pushnumber(state, TimeLeftBand(item.TimeLeftSeconds));
        PushOptionalString(state, item.Link);
        lua_pushnumber(state, item.MarketId);
        lua_pushnumber(state, item.Quality);
        return 17;
    }

    private static int TimeLeftBand(int seconds) =>
        seconds switch
        {
            <= 0 => 0,
            < 1_800 => 1,
            < 7_200 => 2,
            < 43_200 => 3,
            _ => 4
        };

    private static bool TryReadInt32(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            return false;
        }
        value = unchecked((int)number);
        return true;
    }

    private static bool TryReadUInt64(
        lua_State state,
        int index,
        out ulong value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < 0 ||
            number > ulong.MaxValue)
        {
            return false;
        }
        value = unchecked((ulong)number);
        return true;
    }

    private static void PushOptionalInteger(lua_State state, uint? value)
    {
        if (value is { } integer)
            lua_pushnumber(state, integer);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static bool IsAvailable(LuaRuntime runtime) =>
        runtime.PlayerInteractions.HasActiveInteraction &&
        runtime.PlayerInteractions.CurrentInteractionType ==
            BlackMarketInteractionType;
}
