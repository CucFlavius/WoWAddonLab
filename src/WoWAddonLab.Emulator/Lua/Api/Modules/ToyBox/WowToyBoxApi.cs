using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowToyBoxApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetNumFilteredToys", "GetNumLearnedDisplayedToys",
                     "ForceToyRefilter", "GetNumTotalDisplayedToys", "GetToyFromIndex",
                     "GetCollectedShown", "GetIsFavorite", "GetToyInfo", "GetToyLink",
                     "GetUncollectedShown", "GetUnusableShown",
                     "HasFavorites",
                     "IsExpansionTypeFilterChecked", "IsSourceTypeFilterChecked", "IsToyUsable",
                     "PickupToyBoxItem", "SetAllExpansionTypeFilters", "SetAllSourceTypeFilters",
                     "SetCollectedShown", "SetExpansionTypeFilter", "SetFilterString",
                     "SetIsFavorite", "SetSourceTypeFilter", "SetUncollectedShown", "SetUnusableShown"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ToyBox");

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "ClearFanfare", "IsToySourceValid", "IsUsingDefaultFilters",
                     "SetDefaultFilters"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ToyBoxInfo");

        LuaBindings.RegisterClosureGlobal(state, "PlayerHasToy", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "PlayerHasToy")
        {
            if (!TryReadRequiredInt32(state, 1, out var itemId))
                return luaL_error(state, "Usage: PlayerHasToy(itemID)");
            var runtime = LuaBindings.GetRuntime(state);
            lua_pushboolean(
                state,
                runtime.Units.Find("player") is not null &&
                itemId != 0 &&
                runtime.ToyBox.OwnedItemIds.Contains(itemId)
                    ? 1
                    : 0);
            return 1;
        }

        if (operation is
            "GetNumFilteredToys" or "GetNumLearnedDisplayedToys" or "GetNumTotalDisplayedToys")
            lua_pushinteger(state, 0);
        else if (operation == "GetToyFromIndex")
            lua_pushinteger(state, -1);
        else if (operation == "GetToyInfo")
            return 0;
        else if (operation == "GetToyLink")
            return 0;
        else if (operation is
                 "GetCollectedShown" or "GetUncollectedShown" or
                 "IsExpansionTypeFilterChecked" or "IsSourceTypeFilterChecked")
            lua_pushboolean(state, 1);
        else if (operation is
                 "ForceToyRefilter" or "PickupToyBoxItem" or
                 "SetAllExpansionTypeFilters" or "SetAllSourceTypeFilters" or
                 "SetCollectedShown" or "SetExpansionTypeFilter" or "SetFilterString" or
                 "SetIsFavorite" or "SetSourceTypeFilter" or "SetUncollectedShown" or
                 "SetUnusableShown")
            return 0;
        else if (operation == "IsUsingDefaultFilters")
            lua_pushboolean(state, 1);
        else if (operation is "ClearFanfare" or "SetDefaultFilters")
            return 0;
        else
            lua_pushboolean(state, 0);
        return 1;
    }

    private static bool TryReadRequiredInt32(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }
}
