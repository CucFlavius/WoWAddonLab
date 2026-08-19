using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMailApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "CloseMail", Callback);
        LuaBindings.RegisterClosureGlobal(state, "SetSendMailShowing", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetInboxNumItems", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSendMailMoney", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSendMailCOD", Callback);
        LuaBindings.RegisterClosureGlobal(state, "PickupSendMailMoney", Callback);
        LuaBindings.RegisterClosureGlobal(state, "PickupSendMailCOD", Callback);

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "CanCheckInbox", "GetCraftingOrderMailInfo", "HasInboxMoney",
                     "IsCommandPending", "SetOpeningAll"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Mail");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var client = LuaBindings.GetRuntime(state).Client;
        switch (operation)
        {
            case "GetInboxNumItems":
                lua_pushinteger(state, 0);
                lua_pushinteger(state, 0);
                return 2;
            case "CanCheckInbox":
                lua_pushboolean(state, 0);
                lua_pushnumber(state, 0);
                return 2;
            case "GetCraftingOrderMailInfo":
                lua_pushnil(state);
                return 1;
            case "HasInboxMoney":
            case "IsCommandPending":
                lua_pushboolean(state, 0);
                return 1;
            case "GetSendMailMoney":
                lua_pushnumber(state, client.SendMailMoney);
                return 1;
            case "GetSendMailCOD":
                lua_pushnumber(state, client.SendMailCod);
                return 1;
            case "PickupSendMailMoney":
            case "PickupSendMailCOD":
                return 0;
            default:
                return 0;
        }
    }
}
