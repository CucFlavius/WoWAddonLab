using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowIncomingSummonApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "HasIncomingSummon", "IncomingSummonStatus" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_IncomingSummon");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var unitToken = RequiredUnitToken(state, operation);
        var status = runtime.Units.Find(unitToken)?.IncomingSummonStatus ?? 0;
        if (operation == "HasIncomingSummon")
            lua_pushboolean(state, status != 0 ? 1 : 0);
        else
            lua_pushinteger(state, status);
        return 1;
    }

    private static string RequiredUnitToken(lua_State state, string operation)
    {
        if (lua_isstring(state, 1) == 0)
        {
            luaL_error(state, $"Usage: C_IncomingSummon.{operation}(unit)");
            return string.Empty;
        }
        var unitToken = lua_tostring(state, 1) ?? string.Empty;
        if (!LuaBindings.IsRecognizedUnitToken(unitToken))
        {
            luaL_error(state, $"Usage: C_IncomingSummon.{operation}(unit)");
            return string.Empty;
        }
        return unitToken;
    }
}
