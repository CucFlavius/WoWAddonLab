using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPerksApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterNamespace(
            state,
            "C_PerksProgram",
            "GetAvailableVendorItemIDs", "GetAvailableCategoryIDs", "GetCategoryInfo",
            "GetCurrencyAmount", "GetFrozenPerksVendorItemInfo", "RequestPendingChestRewards");
        RegisterNamespace(
            state,
            "C_PerksActivities",
            "GetAllPerksActivityTags", "GetPerksActivitiesInfo",
            "GetPerksActivityChatLink", "GetPerksActivityInfo",
            "GetPerksUIThemePrefix", "GetTrackedPerksActivities", "RemoveTrackedPerksActivity");
    }

    private static void RegisterNamespace(lua_State state, string name, params string[] functions)
    {
        lua_newtable(state);
        foreach (var function in functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, name);
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAvailableVendorItemIDs":
            case "GetAvailableCategoryIDs":
                lua_newtable(state);
                return 1;
            case "GetAllPerksActivityTags":
                lua_newtable(state);
                lua_newtable(state);
                lua_setfield(state, -2, "tagName");
                return 1;
            case "GetPerksActivitiesInfo":
                lua_newtable(state);
                lua_newtable(state);
                lua_setfield(state, -2, "activities");
                return 1;
            case "GetTrackedPerksActivities":
                lua_newtable(state);
                lua_newtable(state);
                lua_setfield(state, -2, "trackedIDs");
                return 1;
            case "GetPerksActivityInfo":
            case "GetFrozenPerksVendorItemInfo":
                lua_pushnil(state);
                return 1;
            case "GetPerksActivityChatLink":
                lua_pushstring(state, string.Empty);
                return 1;
            case "GetCurrencyAmount":
                lua_pushinteger(state, 0);
                return 1;
            case "GetPerksUIThemePrefix":
                lua_pushstring(state, string.Empty);
                return 1;
            default:
                return 0;
        }
    }
}
