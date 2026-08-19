using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowFrameScriptApi : LuaApiModule
{
    private static readonly lua_CFunction MapValuesCallback = MapValues;

    public override void Register(lua_State state)
    {
        lua_pushcfunction(state, MapValuesCallback);
        lua_setglobal(state, "mapvalues");
    }

    private static int MapValues(lua_State state)
    {
        var argumentCount = lua_gettop(state);
        luaL_checktype(state, 1, LUA_TFUNCTION);

        for (var index = 2; index <= argumentCount; index++)
        {
            lua_pushvalue(state, 1);
            lua_pushvalue(state, index);
            lua_call(state, 1, 1);
            lua_replace(state, index);
        }

        lua_remove(state, 1);
        return argumentCount - 1;
    }
}
