using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTooltipInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetHyperlink",
        "GetItemByItemModifiedAppearanceID",
        "GetOwnedItemByID"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_TooltipInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetOwnedItemByID":
                RequireNumber(state, 1, "Usage: local data = C_TooltipInfo.GetOwnedItemByID(itemID)");
                return 0;
            case "GetItemByItemModifiedAppearanceID":
                RequireNumber(
                    state,
                    1,
                    "Usage: local data = C_TooltipInfo.GetItemByItemModifiedAppearanceID(itemModifiedAppearanceID)");
                return 0;
            case "GetHyperlink":
                if (lua_type(state, 1) != LUA_TSTRING)
                    return luaL_error(
                        state,
                        "Usage: local data = C_TooltipInfo.GetHyperlink(hyperlink [, optionalArg1, optionalArg2, hideVendorPrice])");
                return 0;
            default:
                return 0;
        }
    }

    private static void RequireNumber(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            luaL_error(state, usage);
    }
}
