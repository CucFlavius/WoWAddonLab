using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowStorePublicApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "DoesGroupHavePurchaseableProducts",
                     "EventStoreUISetShown",
                     "IsEnabled"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_StorePublic");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var store = LuaBindings.GetRuntime(state).StorePublic;
        switch (operation)
        {
            case "DoesGroupHavePurchaseableProducts":
            {
                const string usage =
                    "Usage: local hasPurchaseableProducts = " +
                    "C_StorePublic.DoesGroupHavePurchaseableProducts(groupID)";
                var groupId = RequiredUInt32(state, 1, usage);
                lua_pushboolean(
                    state,
                    store.PurchaseableProductGroupIds.Contains(groupId) ? 1 : 0);
                return 1;
            }
            case "EventStoreUISetShown":
            {
                const string usage =
                    "Usage: C_StorePublic.EventStoreUISetShown(" +
                    "newShown [, contextKey])";
                if (lua_gettop(state) < 1)
                {
                    luaL_error(state, usage);
                    return 0;
                }

                store.LastReportedShown = lua_toboolean(state, 1) != 0;
                store.LastContextKey = OptionalStringValue(state, 2, usage);
                store.UiShownReportCount++;
                return 0;
            }
            case "IsEnabled":
                lua_pushboolean(state, store.IsEnabled ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }

        return (uint)value;
    }

    private static string? OptionalStringValue(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return null;
        }

        return lua_tostring(state, index);
    }
}
