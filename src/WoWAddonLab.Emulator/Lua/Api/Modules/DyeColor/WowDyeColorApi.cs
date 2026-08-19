using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDyeColorApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = GetDyeColorInfo;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushcfunction(state, Callback);
        lua_setfield(state, -2, "GetDyeColorInfo");
        lua_setglobal(state, "C_DyeColor");
    }

    private static int GetDyeColorInfo(lua_State state)
    {
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, "Usage: local dyeColorInfo = C_DyeColor.GetDyeColorInfo(dyeColorID)");
        return 0;
    }
}
