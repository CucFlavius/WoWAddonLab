using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowScenarioInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[] { "GetScenarioIconInfo", "GetScenarioInfo" })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ScenarioInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation == "GetScenarioInfo"
            ? GetScenarioInfo(state)
            : GetScenarioIconInfo(state);
    }

    private static int GetScenarioInfo(lua_State state)
    {
        var scenario = LuaBindings.GetRuntime(state).Scenario;
        var info = scenario.Info;
        if (info is null)
            return 0;

        lua_createtable(state, 0, 11);
        SetString(state, "name", info.Name);
        SetInteger(state, "currentStage", info.CurrentStage);
        SetInteger(state, "numStages", info.NumStages);
        SetInteger(state, "flags", info.Flags);
        SetBoolean(state, "isComplete", info.IsComplete);
        SetInteger(state, "xp", info.Xp);
        SetInteger(state, "money", info.Money);
        SetInteger(state, "type", info.Type);
        SetOptionalString(state, "area", info.AreaName);
        SetOptionalString(state, "uiTextureKit", info.UiTextureKit);
        SetInteger(state, "scenarioID", scenario.CurrentScenarioId);
        return 1;
    }

    private static int GetScenarioIconInfo(lua_State state)
    {
        const string usage =
            "Usage: local scenarioInfos = C_ScenarioInfo.GetScenarioIconInfo(uiMapID)";
        var mapId = ReadRequiredInt32(state, 1, usage);
        var scenarioInfo = LuaBindings.GetRuntime(state).ScenarioInfo;
        if (!scenarioInfo.IconsByMapId.TryGetValue(mapId, out var icons))
            return 0;

        lua_createtable(state, icons.Count, 0);
        for (var index = 0; index < icons.Count; index++)
        {
            var icon = icons[index];
            lua_createtable(state, 0, 4);
            lua_pushnumber(state, icon.X);
            lua_setfield(state, -2, "x");
            lua_pushnumber(state, icon.Y);
            lua_setfield(state, -2, "y");
            PushOptionalString(state, icon.Atlas);
            lua_setfield(state, -2, "atlas");
            PushOptionalString(state, icon.Description);
            lua_setfield(state, -2, "description");
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int ReadRequiredInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);

        var value = lua_tonumber(state, index);
        if (double.IsNaN(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        PushOptionalString(state, value);
        lua_setfield(state, -2, field);
    }
}
