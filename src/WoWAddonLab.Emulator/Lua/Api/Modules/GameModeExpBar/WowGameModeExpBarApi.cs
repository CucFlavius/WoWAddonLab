using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGameModeExpBarApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "GetCurrentLevel", "GetCurrentXP", "GetMaxXPForLevel" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_GameModeExpBar");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var expBar = LuaBindings.GetRuntime(state).GameModeExpBar;
        switch (operation)
        {
            case "GetCurrentLevel":
                lua_pushinteger(state, expBar.CurrentLevel);
                return 1;
            case "GetCurrentXP":
                lua_pushinteger(state, expBar.CurrentXp);
                return 1;
            case "GetMaxXPForLevel":
                lua_pushinteger(state, expBar.MaxXpForLevel);
                return 1;
            default:
                return 0;
        }
    }
}
