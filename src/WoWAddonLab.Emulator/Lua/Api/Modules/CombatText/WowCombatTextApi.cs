using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCombatTextApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "GetActiveUnit", "GetCurrentEventInfo", "SetActiveUnit" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_CombatText");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        switch (lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty)
        {
            case "GetActiveUnit":
                if (runtime.CombatText.ActiveUnit is { } activeUnit)
                {
                    lua_pushstring(state, activeUnit);
                    return 1;
                }
                return 0;
            case "GetCurrentEventInfo":
                foreach (var value in runtime.CombatText.CurrentEventInfo)
                    runtime.PushValue(value);
                return runtime.CombatText.CurrentEventInfo.Count;
            case "SetActiveUnit":
                const string usage =
                    "Usage: C_CombatText.SetActiveUnit(unitToken)";
                var type = lua_type(state, 1);
                if (type is not (LUA_TSTRING or LUA_TNUMBER))
                    return luaL_error(state, usage);

                var token = lua_tostring(state, 1);
                runtime.CombatText.ActiveUnit =
                    runtime.Units.Find(token)?.Token;
                return 0;
            default:
                return 0;
        }
    }
}
