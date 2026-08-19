using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGuildBankApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushstring(state, "IsGuildBankEnabled");
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, "IsGuildBankEnabled");
        lua_setglobal(state, "C_GuildBank");
        foreach (var function in new[]
                 {
                     "GetGuildBankTabInfo", "GetGuildBankWithdrawMoney",
                     "GetGuildBankItemInfo", "GetGuildBankItemLink", "GetGuildBankMoney",
                     "GetGuildBankMoneyTransaction", "GetGuildBankTabCost", "GetGuildBankText",
                     "GetGuildBankTransaction", "GetNumGuildBankMoneyTransactions",
                     "GetNumGuildBankTransactions", "CanEditGuildBankTabInfo",
                     "CanGuildBankRepair", "CanWithdrawGuildBankMoney",
                     "PickupGuildBankMoney",
                     "QueryGuildBankLog", "QueryGuildBankTab", "QueryGuildBankText",
                     "CloseGuildBankFrame"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var runtime = LuaBindings.GetRuntime(state);
        var client = runtime.Client;
        if (operation == "GetGuildBankTabInfo")
            return 0;
        if (operation == "GetGuildBankWithdrawMoney")
        {
            lua_pushinteger(state, 0);
            return 1;
        }
        if (operation == "GetGuildBankMoney")
        {
            lua_pushnumber(state, client.GuildBankMoney);
            return 1;
        }
        if (operation is "GetGuildBankTabCost" or
            "GetNumGuildBankMoneyTransactions" or "GetNumGuildBankTransactions")
        {
            lua_pushinteger(state, 0);
            return 1;
        }
        if (operation == "PickupGuildBankMoney")
        {
            const string usage = "Usage: PickupGuildBankMoney(amount)";
            if (lua_type(state, 1) != LUA_TNUMBER)
                return luaL_error(state, usage);
            var value = lua_tonumber(state, 1);
            if (!double.IsFinite(value) || value < 0 || value > ulong.MaxValue)
                return luaL_error(state, usage);
            var amount = (ulong)value;
            if (amount > 0 &&
                amount <= client.GuildBankMoney &&
                amount < 100_000_000_000UL)
            {
                runtime.Cursor.SetMoney(amount);
            }
            return 0;
        }
        if (operation == "CanGuildBankRepair")
        {
            lua_pushboolean(state, runtime.Merchant.CanGuildBankRepair ? 1 : 0);
            return 1;
        }
        if (operation is "CanEditGuildBankTabInfo" or
            "CanWithdrawGuildBankMoney")
        {
            lua_pushboolean(state, 0);
            return 1;
        }
        if (operation is "GetGuildBankItemInfo" or "GetGuildBankItemLink"
            or "GetGuildBankMoneyTransaction" or "GetGuildBankText"
            or "GetGuildBankTransaction")
            return 0;
        if (operation is "QueryGuildBankLog" or "QueryGuildBankTab"
            or "QueryGuildBankText" or "CloseGuildBankFrame")
            return 0;
        lua_pushboolean(state, 0);
        return 1;
    }
}
