using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCovenantCallingsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "AreCallingsUnlocked", "RequestCallings" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_CovenantCallings");
    }

    private static int Dispatch(lua_State state)
    {
        var callings = LuaBindings.GetRuntime(state).CovenantCallings;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "AreCallingsUnlocked")
        {
            lua_pushboolean(state, callings.AreCallingsUnlocked ? 1 : 0);
            return 1;
        }

        callings.RequestCount++;
        return 0;
    }
}
