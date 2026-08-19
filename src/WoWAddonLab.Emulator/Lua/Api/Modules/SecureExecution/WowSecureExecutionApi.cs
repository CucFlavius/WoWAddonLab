using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSecureExecutionApi : LuaApiModule
{
    private static readonly lua_CFunction SecureCallMethodCallback = SecureCallMethod;
    private static readonly lua_CFunction StoreSecureReferenceCallback =
        StoreSecureReference;
    private static readonly lua_CFunction IsSecureVariableCallback =
        IsSecureVariable;
    private static readonly lua_CFunction SecureCmdOptionParseCallback =
        SecureCmdOptionParse;

    public override void Register(lua_State state)
    {
        lua_pushcfunction(state, SecureCallMethodCallback);
        lua_setglobal(state, "securecallmethod");
        lua_pushcfunction(state, StoreSecureReferenceCallback);
        lua_setglobal(state, "StoreSecureReference");
        lua_pushcfunction(state, IsSecureVariableCallback);
        lua_setglobal(state, "issecurevariable");
        lua_pushcfunction(state, SecureCmdOptionParseCallback);
        lua_setglobal(state, "SecureCmdOptionParse");
    }

    private static int SecureCmdOptionParse(lua_State state)
    {
        if (lua_isstring(state, 1) == 0)
            return luaL_error(state, "Usage: SecureCmdOptionParse(\"options\")");

        var runtime = LuaBindings.GetRuntime(state);
        var result = WowSecureCommandOptionParser.Parse(
            runtime,
            lua_tostring(state, 1) ?? string.Empty);
        if (result is null)
            return 0;

        lua_pushstring(state, result.Command);
        if (result.Target is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, result.Target);
        return 2;
    }

    private static int IsSecureVariable(lua_State state)
    {
        var variableIndex = lua_istable(state, 1) != 0 ? 2 : 1;
        if (lua_isstring(state, variableIndex) == 0)
        {
            return luaL_error(
                state,
                "Usage: issecurevariable([table,] \"variable\")");
        }

        lua_pushboolean(state, 1);
        lua_pushnil(state);
        return 2;
    }

    private static int StoreSecureReference(lua_State state)
    {
        if (lua_isstring(state, 1) == 0)
        {
            return luaL_error(
                state,
                "Usage: StoreSecureReference(\"name\", obj)");
        }

        if (lua_type(state, 2) != LUA_TTABLE)
        {
            return luaL_error(
                state,
                "Attempt to find 'this' in non-table object " +
                "(used '.' instead of ':' ?)");
        }

        var runtime = LuaBindings.GetRuntime(state);
        var value = LuaBindings.GetObject(runtime, 2);
        if (value is null)
        {
            return luaL_error(
                state,
                "Attempt to find 'this' in non-framescript object");
        }

        runtime.SecureExecution.TryStoreReference(
            lua_tostring(state, 1) ?? string.Empty,
            value.Id);
        return 0;
    }

    private static int SecureCallMethod(lua_State state)
    {
        var argumentTop = lua_gettop(state);
        luaL_checktype(state, 1, LUA_TTABLE);
        luaL_checktype(state, 2, LUA_TSTRING);

        lua_pushstring(state, lua_tostring(state, 2) ?? string.Empty);
        lua_rawget(state, 1);
        if (lua_isfunction(state, -1) == 0)
        {
            lua_pop(state, 1);
            return 0;
        }

        lua_pushvalue(state, 1);
        for (var index = 3; index <= argumentTop; index++)
            lua_pushvalue(state, index);
        if (lua_pcall(state, argumentTop - 1, LUA_MULTRET, 0) != 0)
        {
            lua_pop(state, 1);
            return 0;
        }
        return lua_gettop(state) - argumentTop;
    }
}
