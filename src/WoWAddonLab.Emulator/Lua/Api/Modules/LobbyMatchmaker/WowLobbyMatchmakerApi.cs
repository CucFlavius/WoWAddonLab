using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLobbyMatchmakerApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AbandonQueue",
        "GetCurrQueuePlaylistEntry",
        "GetCurrQueueState",
        "GetQueueStartTime",
        "IsInQueue"
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
        lua_setglobal(state, "C_LobbyMatchmakerInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var matchmaker = LuaBindings.GetRuntime(state).LobbyMatchmaker;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "IsInQueue":
                lua_pushboolean(state, matchmaker.IsInQueue ? 1 : 0);
                return 1;
            case "GetCurrQueuePlaylistEntry":
                lua_pushnumber(state, matchmaker.CurrentPlaylistEntry);
                return 1;
            case "GetCurrQueueState":
                lua_pushnumber(state, matchmaker.CurrentQueueState);
                return 1;
            case "GetQueueStartTime":
                lua_pushinteger(state, matchmaker.QueueStartTime);
                return 1;
            case "AbandonQueue":
                matchmaker.IsInQueue = false;
                matchmaker.CurrentQueueState = 0;
                return 0;
            default:
                return 0;
        }
    }
}
