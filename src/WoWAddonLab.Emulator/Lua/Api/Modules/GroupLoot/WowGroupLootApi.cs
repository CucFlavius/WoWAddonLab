using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGroupLootApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetActiveLootRollIDs",
        "GetLootRollItemInfo"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "GetActiveLootRollIDs")
        {
            lua_newtable(state);
            var index = 1;
            foreach (var rollId in runtime.GroupLoot.ActiveRolls.Keys)
            {
                lua_pushinteger(state, rollId);
                lua_rawseti(state, -2, index++);
            }
            return 1;
        }

        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, "Usage: GetLootRollItemInfo(id)");

        var id = (int)lua_tonumber(state, 1);
        if (!runtime.GroupLoot.ActiveRolls.TryGetValue(id, out var item))
            return 0;

        if (item.TextureFileId is { } textureFileId)
            lua_pushinteger(state, textureFileId);
        else
            lua_pushnil(state);
        if (item.Name is { } name)
            lua_pushstring(state, name);
        else
            lua_pushnil(state);
        lua_pushnumber(state, item.Count);
        lua_pushnumber(state, item.Quality);
        lua_pushboolean(state, item.BindOnPickup ? 1 : 0);
        lua_pushboolean(state, item.CanNeed ? 1 : 0);
        lua_pushboolean(state, item.CanGreed ? 1 : 0);
        lua_pushboolean(state, item.CanDisenchant ? 1 : 0);
        lua_pushnumber(state, item.ReasonNeed);
        lua_pushnumber(state, item.ReasonGreed);
        lua_pushnumber(state, item.ReasonDisenchant);
        lua_pushnumber(state, item.DisenchantSkillRequired);
        lua_pushboolean(state, item.CanTransmog ? 1 : 0);
        return 13;
    }
}
