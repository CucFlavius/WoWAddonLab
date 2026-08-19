using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMapExplorationApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "GetExploredAreaIDsAtPosition", "GetExploredMapTextures" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_MapExplorationInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetExploredAreaIDsAtPosition":
                return GetExploredAreaIdsAtPosition(runtime, state);
            case "GetExploredMapTextures":
                return GetExploredMapTextures(runtime, state);
            default:
                return 0;
        }
    }

    private static int GetExploredAreaIdsAtPosition(LuaRuntime runtime, lua_State state)
    {
        const string usage =
            "Usage: local areaID = C_MapExplorationInfo.GetExploredAreaIDsAtPosition(uiMapID, normalizedPosition)";
        var mapId = ReadInt32(state, 1, usage);
        var (x, y) = ReadVector2(state, 2, usage);
        var areaIds = runtime.MapExploration.AreaIdsResolver?.Invoke(mapId, x, y);
        if (areaIds is null)
        {
            runtime.MapExploration.AreaIdsByPosition.TryGetValue(
                (mapId, x, y),
                out areaIds);
        }

        if (areaIds is null)
        {
            lua_pushnil(state);
            return 1;
        }

        PushInt32Array(state, areaIds);
        return 1;
    }

    private static int GetExploredMapTextures(LuaRuntime runtime, lua_State state)
    {
        const string usage =
            "Usage: local overlayInfo = C_MapExplorationInfo.GetExploredMapTextures(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        if (!runtime.MapExploration.TexturesByMapId.TryGetValue(mapId, out var textures) ||
            textures.Count == 0)
        {
            return 0;
        }

        lua_createtable(state, textures.Count, 0);
        for (var index = 0; index < textures.Count; index++)
        {
            PushTextureInfo(state, textures[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushTextureInfo(
        lua_State state,
        WowMapExplorationTextureInfo info)
    {
        lua_createtable(state, 0, 8);
        SetNumber(state, "textureWidth", info.TextureWidth);
        SetNumber(state, "textureHeight", info.TextureHeight);
        SetNumber(state, "offsetX", info.OffsetX);
        SetNumber(state, "offsetY", info.OffsetY);
        SetBoolean(state, "isShownByMouseOver", info.IsShownByMouseOver);
        SetBoolean(state, "isDrawOnTopLayer", info.IsDrawOnTopLayer);
        PushInt32Array(state, info.FileDataIds);
        lua_setfield(state, -2, "fileDataIDs");
        PushHitRect(state, info.HitRect);
        lua_setfield(state, -2, "hitRect");
    }

    private static void PushHitRect(lua_State state, WowMapExplorationHitRect hitRect)
    {
        lua_createtable(state, 0, 4);
        SetNumber(state, "top", hitRect.Top);
        SetNumber(state, "bottom", hitRect.Bottom);
        SetNumber(state, "left", hitRect.Left);
        SetNumber(state, "right", hitRect.Right);
    }

    private static void PushInt32Array(lua_State state, IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static (float X, float Y) ReadVector2(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_istable(state, index) == 0)
            return ErrorVector2(state, usage);

        var absoluteIndex = index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;
        lua_getfield(state, absoluteIndex, "x");
        var x = ReadFloat(state, -1, usage);
        lua_pop(state, 1);
        lua_getfield(state, absoluteIndex, "y");
        var y = ReadFloat(state, -1, usage);
        lua_pop(state, 1);
        return (x, y);
    }

    private static (float X, float Y) ErrorVector2(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return default;
    }

    private static int ReadInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (double.IsNaN(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static float ReadFloat(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (float)lua_tonumber(state, index);
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetBoolean(lua_State state, string key, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, key);
    }
}
