using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowFogOfWarApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "GetFogOfWarForMap", "GetFogOfWarInfo" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_FogOfWar");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation switch
        {
            "GetFogOfWarForMap" => GetFogOfWarForMap(
                state,
                runtime.FogOfWar),
            "GetFogOfWarInfo" => GetFogOfWarInfo(
                state,
                runtime.FogOfWar),
            _ => 0
        };
    }

    private static int GetFogOfWarForMap(
        lua_State state,
        WowFogOfWarApiState fogOfWar)
    {
        const string usage =
            "Usage: local fogOfWarID = C_FogOfWar.GetFogOfWarForMap(uiMapID)";
        var mapId = RequiredInt32(state, 1, usage);
        if (fogOfWar.FogOfWarIdByMap.TryGetValue(mapId, out var fogOfWarId))
            lua_pushinteger(state, fogOfWarId);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int GetFogOfWarInfo(
        lua_State state,
        WowFogOfWarApiState fogOfWar)
    {
        const string usage =
            "Usage: local fogOfWarInfo = C_FogOfWar.GetFogOfWarInfo(fogOfWarID)";
        var fogOfWarId = RequiredInt32(state, 1, usage);
        if (!fogOfWar.InfoById.TryGetValue(fogOfWarId, out var info))
        {
            lua_pushnil(state);
            return 1;
        }

        lua_createtable(state, 0, 4);
        SetNumber(state, "fogOfWarID", info.FogOfWarId);
        SetString(state, "backgroundAtlas", info.BackgroundAtlas);
        SetString(state, "maskAtlas", info.MaskAtlas);
        SetNumber(state, "maskScalar", (float)info.MaskScalar);
        return 1;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return unchecked((int)value);
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(
        lua_State state,
        string field,
        string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }
}
