using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowRestrictedActionsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "CheckAllowProtectedFunctions",
                     "GetAddOnRestrictionState",
                     "IsAddOnRestrictionActive"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_RestrictedActions");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "CheckAllowProtectedFunctions")
        {
            var value = LuaBindings.GetObject(runtime, 1);
            if (value is null ||
                lua_gettop(state) >= 2 && lua_type(state, 2) != LUA_TBOOLEAN)
            {
                return luaL_error(
                    state,
                    "Usage: local allowed = " +
                    "C_RestrictedActions.CheckAllowProtectedFunctions(object [, silent])");
            }

            lua_pushboolean(
                state,
                !value.Protected || !runtime.Client.InCombatLockdown ? 1 : 0);
            return 1;
        }

        var restrictionType = ReadRestrictionType(state, operation);
        var restrictionState = runtime.RestrictedActions.GetState(
            restrictionType,
            runtime.Client.InCombatLockdown);
        if (operation == "GetAddOnRestrictionState")
            lua_pushinteger(state, restrictionState);
        else
            lua_pushboolean(state, restrictionState == 2 ? 1 : 0);
        return 1;
    }

    private static int ReadRestrictionType(lua_State state, string operation)
    {
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, Usage(operation));
        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value is < 0 or > 5)
            return luaL_error(state, Usage(operation));
        return (int)Math.Truncate(value);
    }

    private static string Usage(string operation) =>
        $"Usage: local value = C_RestrictedActions.{operation}(type)";
}
