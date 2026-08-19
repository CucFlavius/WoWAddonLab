using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowConnectionApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CancelLogout", "ForceLogout", "ForceQuit", "GetNativeRealmID",
        "GetNetIpTypes", "GetNormalizedRealmName", "GetRealmID", "GetRealmName",
        "IsOnTournamentRealm", "SelectedRealmName"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var client = LuaBindings.GetRuntime(state).Client;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CancelLogout":
                client.SessionActionRequested = null;
                return 0;
            case "ForceLogout":
            case "ForceQuit":
                client.SessionActionRequested = operation;
                return 0;
            case "GetNativeRealmID":
                lua_pushinteger(state, client.NativeRealmId);
                return 1;
            case "GetRealmID":
                lua_pushinteger(state, client.RealmId);
                return 1;
            case "GetRealmName":
                lua_pushstring(state, client.RealmName);
                return 1;
            case "GetNormalizedRealmName":
                if (!client.IsPlayerInWorld)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, client.ResolveNormalizedRealmName());
                return 1;
            case "SelectedRealmName":
                lua_pushstring(state, client.SelectedRealmName);
                return 1;
            case "IsOnTournamentRealm":
                lua_pushboolean(state, client.IsTournamentRealm ? 1 : 0);
                return 1;
            case "GetNetIpTypes":
                lua_pushinteger(state, client.HomeProtocolType);
                lua_pushinteger(state, client.WorldProtocolType);
                return 2;
            default:
                return 0;
        }
    }
}
