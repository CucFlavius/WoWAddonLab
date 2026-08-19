using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPvpApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "ClearBattlemaster", Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsActiveBattlefieldArena", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetMaxBattlefieldID", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetNumBattlefieldFlagPositions", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetBattlefieldWinner", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetWorldPVPQueueStatus", Callback);
        LuaBindings.RegisterClosureGlobal(state, "CanHearthAndResurrectFromArea", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var pvp = LuaBindings.GetRuntime(state).Pvp;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ClearBattlemaster":
                pvp.BattlemasterOpen = false;
                return 0;
            case "GetMaxBattlefieldID":
                lua_pushinteger(state, pvp.MaximumBattlefieldId);
                return 1;
            case "GetNumBattlefieldFlagPositions":
                lua_pushinteger(state, pvp.BattlefieldFlagPositionCount);
                return 1;
            case "GetBattlefieldWinner":
                if (pvp.BattlefieldWinner is { } winner)
                    lua_pushinteger(state, winner);
                else
                    lua_pushnil(state);
                return 1;
            case "GetWorldPVPQueueStatus":
                if (!TryReadRequiredQueueIndex(state, 1, out var queueIndex))
                    return luaL_error(state, "Usage: GetWorldPVPQueueStatus(index)");
                if (queueIndex is < 1 or > 2)
                    return 0;
                pvp.WorldPvpQueues.TryGetValue(queueIndex, out var queue);
                lua_pushstring(state, queue?.Status ?? "none");
                PushOptionalString(state, queue?.MapName);
                lua_pushnumber(state, queue?.QueueId ?? 0);
                lua_pushnumber(state, queue?.ExpireTime ?? 0);
                lua_pushnumber(state, queue?.AverageWaitTime ?? 0);
                lua_pushnumber(state, queue?.QueuedTime ?? 0);
                lua_pushboolean(state, queue?.Suspended == true ? 1 : 0);
                return 7;
            case "CanHearthAndResurrectFromArea":
                lua_pushboolean(state, pvp.CanHearthAndResurrectFromArea ? 1 : 0);
                return 1;
            case "IsActiveBattlefieldArena":
                lua_pushboolean(state, pvp.IsActiveBattlefieldArena ? 1 : 0);
                lua_pushboolean(state, pvp.HasActiveBattlefieldArenaMatch ? 1 : 0);
                return 2;
            default:
                return 0;
        }
    }

    private static bool TryReadRequiredQueueIndex(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (lua_type(state, index) != LUA_TNUMBER)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is not null)
            lua_pushstring(state, value);
        else
            lua_pushnil(state);
    }
}
