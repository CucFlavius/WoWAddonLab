using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowQuestOfferApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = GetQuestRewardCurrencyInfo;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushcfunction(state, Callback);
        lua_setfield(state, -2, "GetQuestRewardCurrencyInfo");
        lua_setglobal(state, "C_QuestOffer");
    }

    private static int GetQuestRewardCurrencyInfo(lua_State state)
    {
        const string usage =
            "Usage: local questRewardCurrencyInfo = " +
            "C_QuestOffer.GetQuestRewardCurrencyInfo(questInfoType, questRewardIndex)";
        if (lua_isstring(state, 1) == 0 || lua_isnumber(state, 2) == 0 || lua_tonumber(state, 2) < 1)
            return luaL_error(state, usage);
        lua_pushnil(state);
        return 1;
    }
}
