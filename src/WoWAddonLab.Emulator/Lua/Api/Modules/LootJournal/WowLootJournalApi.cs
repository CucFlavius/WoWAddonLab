using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLootJournalApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions = ["GetItemSetItems", "GetItemSets"];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_LootJournal");
    }

    private static int Dispatch(lua_State state)
    {
        var lootJournal = LuaBindings.GetRuntime(state).LootJournal;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetItemSetItems":
            {
                var setId = RequiredInt32(state, 1, operation);
                if (!lootJournal.DataAvailable ||
                    !lootJournal.ItemsBySetId.TryGetValue(setId, out var items))
                {
                    return 0;
                }

                lua_newtable(state);
                for (var index = 0; index < items.Count; index++)
                {
                    var item = items[index];
                    lua_newtable(state);
                    SetInteger(state, "itemID", item.ItemId);
                    SetOptionalFileAsset(state, "icon", item.IconFileDataId);
                    SetInteger(
                        state,
                        "invType",
                        unchecked(item.InventoryTypeIndex + 1));
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetItemSets":
            {
                var classId = OptionalInt32(state, 1, operation);
                var specializationId = OptionalInt32(state, 2, operation);
                if (!lootJournal.DataAvailable)
                    return 0;

                lua_newtable(state);
                var index = 1;
                foreach (var itemSet in lootJournal.ItemSets)
                {
                    if (!MatchesFilter(itemSet.ClassIds, classId) ||
                        !MatchesFilter(
                            itemSet.SpecializationIds,
                            specializationId))
                    {
                        continue;
                    }

                    lua_newtable(state);
                    SetInteger(state, "setID", itemSet.SetId);
                    SetInteger(state, "itemLevel", itemSet.ItemLevel);
                    if (itemSet.Name is not null)
                        lua_pushstring(state, itemSet.Name);
                    else
                        lua_pushnil(state);
                    lua_setfield(state, -2, "name");
                    lua_rawseti(state, -2, index++);
                }
                return 1;
            }
            default:
                return 0;
        }
    }

    private static bool MatchesFilter(
        IReadOnlySet<int>? supportedValues,
        int? requestedValue) =>
        requestedValue is null or 0 ||
        supportedValues is null ||
        supportedValues.Count == 0 ||
        supportedValues.Contains(requestedValue.Value);

    private static int RequiredInt32(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, $"Usage: C_LootJournal.{operation}(...)");
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, $"Usage: C_LootJournal.{operation}(...)");
        return unchecked((int)value);
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredInt32(state, index, operation);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalFileAsset(
        lua_State state,
        string field,
        int? fileDataId)
    {
        if (fileDataId is > 0)
            lua_pushinteger(state, fileDataId.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }
}
