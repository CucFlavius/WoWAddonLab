using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowNeighborhoodInitiativeApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AddTrackedInitiativeTask",
        "GetActiveNeighborhood",
        "GetAvailableHouseXP",
        "GetInitiativeActivityLogInfo",
        "GetInitiativeTaskChatLink",
        "GetInitiativeTaskInfo",
        "GetNeighborhoodInitiativeInfo",
        "GetRequiredLevel",
        "GetTrackedInitiativeTasks",
        "IsInitiativeEnabled",
        "IsPlayerInNeighborhoodGroup",
        "IsViewingActiveNeighborhood",
        "PlayerHasInitiativeAccess",
        "PlayerMeetsRequiredLevel",
        "RemoveTrackedInitiativeTask",
        "RequestInitiativeActivityLog",
        "RequestNeighborhoodInitiativeInfo",
        "SetActiveNeighborhood",
        "SetViewingNeighborhood"
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
        lua_setglobal(state, "C_NeighborhoodInitiative");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetTrackedInitiativeTasks":
                lua_newtable(state);
                lua_newtable(state);
                lua_setfield(state, -2, "trackedIDs");
                return 1;
            case "GetInitiativeActivityLogInfo":
            case "GetInitiativeTaskInfo":
            case "GetNeighborhoodInitiativeInfo":
                lua_pushnil(state);
                return 1;
            case "GetInitiativeTaskChatLink":
            case "GetActiveNeighborhood":
                lua_pushstring(state, string.Empty);
                return 1;
            case "GetAvailableHouseXP":
            case "GetRequiredLevel":
                lua_pushinteger(state, 0);
                return 1;
            case "IsInitiativeEnabled":
            case "IsPlayerInNeighborhoodGroup":
            case "IsViewingActiveNeighborhood":
            case "PlayerHasInitiativeAccess":
            case "PlayerMeetsRequiredLevel":
                lua_pushboolean(state, 0);
                return 1;
            default:
                return 0;
        }
    }
}
