using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowRaidLocksApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = IsEncounterComplete;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushcfunction(state, Callback);
        lua_setfield(state, -2, "IsEncounterComplete");
        lua_setglobal(state, "C_RaidLocks");
    }

    private static int IsEncounterComplete(lua_State state)
    {
        const string usage =
            "Usage: local encounterIsComplete = " +
            "C_RaidLocks.IsEncounterComplete(mapID, encounterID [, difficultyID])";
        if (lua_isnumber(state, 1) == 0 || lua_isnumber(state, 2) == 0 ||
            (lua_gettop(state) >= 3 && lua_isnoneornil(state, 3) == 0 && lua_isnumber(state, 3) == 0))
            return luaL_error(state, usage);
        lua_pushboolean(state, 0);
        return 1;
    }
}
