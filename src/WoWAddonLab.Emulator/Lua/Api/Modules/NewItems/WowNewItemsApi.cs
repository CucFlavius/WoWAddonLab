using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowNewItemsApi : LuaApiModule
{
    private const string IsNewItemUsage =
        "Usage: local isNew = C_NewItems.IsNewItem(containerIndex, slotIndex)";
    private const string RemoveNewItemUsage =
        "Usage: C_NewItems.RemoveNewItem(containerIndex, slotIndex)";

    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        Register(state, "ClearAll");
        Register(state, "IsNewItem");
        Register(state, "RemoveNewItem");
        lua_setglobal(state, "C_NewItems");
    }

    private static void Register(lua_State state, string function)
    {
        lua_pushstring(state, function);
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, function);
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var newItems = LuaBindings.GetRuntime(state).NewItems;
        if (operation == "ClearAll")
        {
            newItems.ClearAll();
            return 0;
        }

        var usage = operation == "IsNewItem" ? IsNewItemUsage : RemoveNewItemUsage;
        var containerIndex = RequiredInteger(state, 1, usage);
        var slotIndex = RequiredOneBasedIndex(state, 2, usage);
        if (operation == "IsNewItem")
        {
            lua_pushboolean(
                state,
                newItems.IsNewItem(containerIndex, slotIndex) ? 1 : 0);
            return 1;
        }

        newItems.RemoveNewItem(containerIndex, slotIndex);
        return 0;
    }

    private static int RequiredInteger(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < int.MinValue || number > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)number);
    }

    private static uint RequiredOneBasedIndex(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < uint.MinValue || number > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return unchecked((uint)number);
    }
}
