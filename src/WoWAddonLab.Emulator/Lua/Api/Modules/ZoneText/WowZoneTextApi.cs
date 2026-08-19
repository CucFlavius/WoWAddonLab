using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowZoneTextApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetMinimapZoneText",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetZoneText", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetRealZoneText", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSubZoneText", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var client = LuaBindings.GetRuntime(state).Client;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var text = operation switch
        {
            "GetMinimapZoneText" => client.MinimapZoneText,
            "GetSubZoneText" => client.SubZoneText,
            _ => client.ZoneText
        };
        lua_pushstring(state, text ?? string.Empty);
        return 1;
    }
}
