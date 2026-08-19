using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowQuestInfoSystemApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = GetQuestClassification;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushcfunction(state, Callback);
        lua_setfield(state, -2, "GetQuestClassification");
        lua_setglobal(state, "C_QuestInfoSystem");
    }

    private static int GetQuestClassification(lua_State state)
    {
        for (var index = 1; index <= Math.Min(2, lua_gettop(state)); index++)
        {
            if (lua_isnoneornil(state, index) == 0 && lua_isnumber(state, index) == 0)
                return luaL_error(
                    state,
                    "Usage: local classification = " +
                    "C_QuestInfoSystem.GetQuestClassification([questID, questInfoID])");
        }
        lua_pushinteger(state, 0);
        return 1;
    }
}
