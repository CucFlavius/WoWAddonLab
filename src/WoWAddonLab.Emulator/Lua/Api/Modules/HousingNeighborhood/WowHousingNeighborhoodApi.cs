using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowHousingNeighborhoodApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterNamespace(
            state,
            "C_HousingNeighborhood",
            "CanReturnAfterVisitingHouse",
            "IsPlayerInOtherPlayersPlot",
            "GetCornerstoneHouseInfo",
            "GetCornerstoneNeighborhoodInfo",
            "GetDiscountedMovePrice",
            "OnBulletinBoardClosed",
            "OnCornerstoneClosed",
            "RequestNeighborhoodInfo",
            "RequestNeighborhoodRoster",
            "RequestPendingNeighborhoodInvites");
    }

    private static void RegisterNamespace(lua_State state, string name, params string[] functions)
    {
        lua_newtable(state);
        foreach (var function in functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, name);
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation is "GetCornerstoneHouseInfo"
            or "GetCornerstoneNeighborhoodInfo"
            or "OnBulletinBoardClosed"
            or "OnCornerstoneClosed"
            or "RequestNeighborhoodInfo"
            or "RequestNeighborhoodRoster"
            or "RequestPendingNeighborhoodInvites")
            return 0;

        if (operation == "GetDiscountedMovePrice")
        {
            lua_pushnumber(state, 0);
            return 1;
        }

        lua_pushboolean(state, 0);
        return 1;
    }
}
