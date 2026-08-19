using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowClassApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetNumClasses", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetClassInfo", Callback);

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetClassInfo", "GetCreatureFamilyIDs", "GetCreatureFamilyInfo",
                     "GetCreatureID", "GetCreatureTypeIDs", "GetCreatureTypeInfo",
                     "GetFactionInfo", "GetRaceInfo"
                 })
        {
            lua_pushstring(state, function);
            lua_pushboolean(state, 1);
            lua_pushcclosure(state, Callback, 2);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_CreatureInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var classState = LuaBindings.GetRuntime(state).Classes;
        var classes = classState.Classes;
        if (operation == "GetNumClasses")
        {
            lua_pushinteger(state, classes.Count);
            return 1;
        }

        if (operation == "GetCreatureFamilyIDs")
        {
            PushIntegerArray(state, classState.CreatureFamilies.Keys);
            return 1;
        }

        if (operation == "GetCreatureTypeIDs")
        {
            PushIntegerArray(state, classState.CreatureTypes.Keys);
            return 1;
        }

        if (operation == "GetCreatureID")
        {
            if (lua_gettop(state) < 1 || lua_isstring(state, 1) == 0)
                return luaL_error(
                    state,
                    "Usage: local creatureID = C_CreatureInfo.GetCreatureID(creatureGUID)");
            var guid = lua_tostring(state, 1) ?? string.Empty;
            if (TryGetCreatureId(guid, out var creatureId))
                lua_pushinteger(state, creatureId);
            else
                lua_pushnil(state);
            return 1;
        }

        if (!TryReadRequiredInt32(state, 1, out var id))
            return luaL_error(state, UsageFor(operation));

        switch (operation)
        {
            case "GetClassInfo":
            {
                var value = classes.FirstOrDefault(entry => entry.Id == id);
                if (lua_toboolean(state, lua_upvalueindex(2)) != 0)
                {
                    if (value is null)
                    {
                        lua_pushnil(state);
                        return 1;
                    }

                    lua_createtable(state, 0, 3);
                    SetString(state, "className", GetLocalizedName(state, value));
                    SetString(state, "classFile", value.FileName);
                    SetInteger(state, "classID", value.Id);
                    return 1;
                }

                if (value is null)
                    return 0;
                lua_pushstring(state, GetLocalizedName(state, value));
                lua_pushstring(state, value.FileName);
                lua_pushinteger(state, value.Id);
                return 3;
            }
            case "GetCreatureFamilyInfo":
                if (!classState.CreatureFamilies.TryGetValue(id, out var family))
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_createtable(state, 0, 3);
                SetInteger(state, "id", family.Id);
                SetOptionalString(state, "name", family.Name);
                SetOptionalInteger(state, "iconFile", family.IconFileId);
                return 1;
            case "GetCreatureTypeInfo":
                if (!classState.CreatureTypes.TryGetValue(id, out var type))
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_createtable(state, 0, 2);
                SetInteger(state, "id", type.Id);
                SetOptionalString(state, "name", type.Name);
                return 1;
            case "GetFactionInfo":
                if (!classState.FactionsByRaceId.TryGetValue(id, out var faction))
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_createtable(state, 0, 2);
                SetOptionalString(state, "name", faction.Name);
                SetOptionalString(state, "groupTag", faction.GroupTag);
                return 1;
            case "GetRaceInfo":
                if (!classState.Races.TryGetValue(id, out var race))
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_createtable(state, 0, 3);
                SetString(state, "raceName", race.Name);
                SetString(state, "clientFileString", race.ClientFileString);
                SetInteger(state, "raceID", race.Id);
                return 1;
            default:
                return 0;
        }
    }

    private static string GetLocalizedName(lua_State state, WowClassInfoState value)
    {
        lua_getglobal(state, $"CLASS_{value.FileName}");
        var localized = lua_type(state, -1) == LUA_TSTRING
            ? lua_tostring(state, -1)
            : null;
        lua_pop(state, 1);
        return localized ?? value.Name;
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(lua_State state, string name, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(lua_State state, string name, int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void PushIntegerArray(lua_State state, IEnumerable<int> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        lua_createtable(state, sorted.Length, 0);
        for (var index = 0; index < sorted.Length; index++)
        {
            lua_pushinteger(state, sorted[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static bool TryReadRequiredInt32(lua_State state, int index, out int value)
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

    private static bool TryGetCreatureId(string guid, out int creatureId)
    {
        creatureId = 0;
        var components = guid.Split('-');
        if (components.Length < 6 ||
            (components[0] != "Creature" &&
             components[0] != "Vehicle" &&
             components[0] != "Pet"))
            return false;
        return int.TryParse(
            components[5],
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out creatureId);
    }

    private static string UsageFor(string operation) => operation switch
    {
        "GetClassInfo" =>
            "Usage: local classInfo = C_CreatureInfo.GetClassInfo(classID)",
        "GetCreatureFamilyInfo" =>
            "Usage: local creatureFamilyInfo = C_CreatureInfo.GetCreatureFamilyInfo(creatureFamilyID)",
        "GetCreatureTypeInfo" =>
            "Usage: local creatureTypeInfo = C_CreatureInfo.GetCreatureTypeInfo(creatureTypeID)",
        "GetFactionInfo" =>
            "Usage: local factionInfo = C_CreatureInfo.GetFactionInfo(raceID)",
        "GetRaceInfo" =>
            "Usage: local raceInfo = C_CreatureInfo.GetRaceInfo(raceID)",
        _ => $"Usage: {operation}(id)"
    };
}
