using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTradeInfoApi : LuaApiModule
{
    private const ulong MaximumTradeMoneyExclusive = 100_000_000_000;
    private const ulong MaximumExactlyRepresentableLuaInteger =
        9_007_199_254_740_991;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AddTradeMoney", "PickupTradeMoney", "SetTradeMoney",
        "ShouldShowTradeOfferWarning"
    ];

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "CloseTrade", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetPlayerTradeMoney",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetTargetTradeMoney",
            Callback);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_TradeInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var client = runtime.Client;
        var trade = runtime.TradeInfo;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CloseTrade":
                trade.CloseTradeRequests++;
                trade.IsTradeOpen = false;
                return 0;
            case "GetPlayerTradeMoney":
                lua_pushnumber(state, client.PlayerTradeMoney);
                return 1;
            case "GetTargetTradeMoney":
                lua_pushnumber(state, client.TargetTradeMoney);
                return 1;
            case "AddTradeMoney":
            {
                trade.AddTradeMoneyRequests++;
                var cursorMoney = runtime.Cursor.Money;
                if (runtime.Cursor.Payload?.Kind != WowCursorPayloadKind.Money ||
                    cursorMoney == 0 ||
                    cursorMoney > ulong.MaxValue - client.PlayerTradeMoney)
                {
                    return 0;
                }

                var resultingOffer = client.PlayerTradeMoney + cursorMoney;
                var availableMoney = (ulong)Math.Max(0, client.Money);
                if (resultingOffer > availableMoney)
                    return 0;

                client.PlayerTradeMoney = resultingOffer;
                runtime.Cursor.ClearPayload();
                trade.SuccessfulAddTradeMoneyRequests++;
                return 0;
            }
            case "PickupTradeMoney":
            {
                var amount = RequiredMoneyAmount(
                    state,
                    1,
                    "Usage: C_TradeInfo.PickupTradeMoney(amount)");
                trade.PickupTradeMoneyRequests++;
                trade.LastPickupTradeMoneyAmount = amount;
                if (amount > client.PlayerTradeMoney)
                    return 0;

                if (amount > 0)
                    client.PlayerTradeMoney -= amount;
                runtime.Cursor.SetMoney(amount);
                trade.SuccessfulPickupTradeMoneyRequests++;
                return 0;
            }
            case "SetTradeMoney":
            {
                var requestedAmount = RequiredMoneyAmount(
                    state,
                    1,
                    "Usage: C_TradeInfo.SetTradeMoney(amount)");
                var amount = (requestedAmount & (1UL << 63)) != 0
                    ? 0
                    : Math.Min(
                        requestedAmount,
                        MaximumTradeMoneyExclusive - 1);
                trade.SetTradeMoneyRequests++;
                trade.LastRequestedTradeMoneyAmount = requestedAmount;
                trade.LastClampedTradeMoneyAmount = amount;

                var availableMoney = (ulong)Math.Max(0, client.Money);
                if (amount > availableMoney ||
                    amount >= MaximumTradeMoneyExclusive)
                {
                    return 0;
                }

                client.PlayerTradeMoney = amount;
                trade.SuccessfulSetTradeMoneyRequests++;
                return 0;
            }
            case "ShouldShowTradeOfferWarning":
                lua_pushboolean(
                    state,
                    trade.ShouldShowTradeOfferWarning ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static ulong RequiredMoneyAmount(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = lua_tonumber(state, index);
        if (value < 0 ||
            value > MaximumExactlyRepresentableLuaInteger)
        {
            luaL_error(state, usage);
            return 0;
        }

        return double.IsNaN(value)
            ? 1UL << 63
            : (ulong)value;
    }
}
