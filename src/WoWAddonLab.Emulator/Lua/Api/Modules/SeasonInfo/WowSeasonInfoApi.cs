using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSeasonInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetCurrentDisplaySeasonExpansion",
                     "GetCurrentDisplaySeasonID"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_SeasonInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var season = LuaBindings.GetRuntime(state).SeasonInfo;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? "";
        if (operation == "GetCurrentDisplaySeasonID")
        {
            lua_pushinteger(state, season.CurrentDisplaySeasonId);
            return 1;
        }
        if (season.ExpansionBySeasonId.TryGetValue(
                season.CurrentDisplaySeasonId,
                out var expansion))
            lua_pushinteger(state, expansion);
        else
            lua_pushnil(state);
        return 1;
    }
}
