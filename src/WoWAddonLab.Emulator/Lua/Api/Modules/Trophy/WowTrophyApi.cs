using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTrophyApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "MonumentChangeAppearanceToTrophyID", "MonumentGetCount",
                     "MonumentGetSelectedTrophyID", "MonumentGetTrophyInfoByIndex",
                     "MonumentLoadList", "MonumentLoadSelectedTrophyID",
                     "MonumentRevertAppearanceToSaved", "MonumentSaveSelection"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Trophy");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation is "MonumentGetCount" or "MonumentGetSelectedTrophyID")
        {
            lua_pushinteger(state, 0);
            return 1;
        }
        return 0;
    }
}
