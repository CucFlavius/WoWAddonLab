using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGmTicketApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetWebTicket", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetGMStatus", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var gmTicket = LuaBindings.GetRuntime(state).GmTicket;
        if (!gmTicket.RequestServiceAvailable)
            return 0;

        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "GetWebTicket")
            gmTicket.WebTicketRequestCount++;
        else
            gmTicket.GmStatusRequestCount++;
        return 0;
    }
}
