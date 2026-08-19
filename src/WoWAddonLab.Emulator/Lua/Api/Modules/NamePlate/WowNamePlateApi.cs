using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowNamePlateApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetNamePlateForUnit",
                     "GetNamePlateSize",
                     "GetNamePlates",
                     "SetNamePlateSize"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_NamePlate");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var namePlates = runtime.NamePlates;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetNamePlateForUnit":
            {
                const string usage =
                    "Usage: local nameplate = C_NamePlate.GetNamePlateForUnit(unitToken [, includeForbidden])";
                var unit = RequiredUnitToken(state, 1, usage);
                var includeForbidden = lua_toboolean(state, 2) != 0;
                runtime.PushObject(
                    namePlates.ObjectIdsByUnit.TryGetValue(unit, out var objectId) &&
                    (includeForbidden || !namePlates.ForbiddenObjectIds.Contains(objectId))
                        ? runtime.Ui.Find(objectId)
                        : null);
                return 1;
            }
            case "GetNamePlateSize":
                lua_pushnumber(state, namePlates.Width);
                lua_pushnumber(state, namePlates.Height);
                return 2;
            case "GetNamePlates":
            {
                var includeForbidden = lua_toboolean(state, 1) != 0;
                lua_createtable(state, namePlates.ObjectIdsByUnit.Count, 0);
                var index = 1;
                foreach (var objectId in namePlates.ObjectIdsByUnit.Values.Distinct())
                {
                    if (!includeForbidden && namePlates.ForbiddenObjectIds.Contains(objectId))
                        continue;
                    runtime.PushObject(runtime.Ui.Find(objectId));
                    lua_rawseti(state, -2, index++);
                }
                return 1;
            }
            case "SetNamePlateSize":
            {
                const string usage = "Usage: C_NamePlate.SetNamePlateSize(width, height)";
                var width = RequiredUiDimension(state, 1, usage);
                var height = RequiredUiDimension(state, 2, usage);
                namePlates.Width = width;
                namePlates.Height = height;
                return 0;
            }
            default:
                return 0;
        }
    }

    private static string RequiredUnitToken(lua_State state, int index, string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }

        var unitToken = lua_tostring(state, index) ?? string.Empty;
        if (!LuaBindings.IsRecognizedUnitToken(unitToken))
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return unitToken;
    }

    private static float RequiredUiDimension(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (float)lua_tonumber(state, index);
    }
}
