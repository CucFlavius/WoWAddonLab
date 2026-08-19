using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowInvasionInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "AreInvasionsAvailable",
                     "GetInvasionForUiMapID",
                     "GetInvasionInfo",
                     "GetInvasionTimeLeft"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_InvasionInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var invasions = LuaBindings.GetRuntime(state).InvasionInfo;
        switch (operation)
        {
            case "AreInvasionsAvailable":
                lua_pushboolean(
                    state,
                    invasions.InvasionsById.Values.Any(
                        invasion => invasion.IsAvailable)
                        ? 1
                        : 0);
                return 1;
            case "GetInvasionForUiMapID":
            {
                const string usage =
                    "Usage: local invasionID = " +
                    "C_InvasionInfo.GetInvasionForUiMapID(uiMapID)";
                var uiMapId = RequiredInt32(state, 1, usage);
                if (invasions.InvasionIdsByUiMapId.TryGetValue(
                        uiMapId,
                        out var invasionId) &&
                    invasions.InvasionsById.TryGetValue(
                        invasionId,
                        out var invasion) &&
                    invasion.IsAvailable)
                {
                    lua_pushinteger(state, invasionId);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetInvasionInfo":
            {
                const string usage =
                    "Usage: local invasionInfo = " +
                    "C_InvasionInfo.GetInvasionInfo(invasionID)";
                var invasionId = RequiredInt32(state, 1, usage);
                if (!invasions.InvasionsById.TryGetValue(
                        invasionId,
                        out var invasion))
                {
                    return 0;
                }

                PushInvasionInfo(state, invasion);
                return 1;
            }
            case "GetInvasionTimeLeft":
            {
                const string usage =
                    "Usage: local timeLeftMinutes = " +
                    "C_InvasionInfo.GetInvasionTimeLeft(invasionID)";
                var invasionId = RequiredInt32(state, 1, usage);
                if (invasions.InvasionsById.TryGetValue(
                        invasionId,
                        out var invasion) &&
                    invasion.TimeLeftMinutes.HasValue)
                {
                    lua_pushinteger(
                        state,
                        Math.Max(0, invasion.TimeLeftMinutes.Value));
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            default:
                return 0;
        }
    }

    private static void PushInvasionInfo(
        lua_State state,
        WowInvasionInfo invasion)
    {
        lua_createtable(state, 0, 5);
        SetInteger(state, "invasionID", invasion.InvasionId);
        SetString(state, "name", invasion.Name);

        lua_createtable(state, 0, 2);
        SetNumber(state, "x", invasion.X);
        SetNumber(state, "y", invasion.Y);
        ApplyVector2Mixin(state);
        lua_setfield(state, -2, "position");

        SetOptionalString(state, "atlasName", invasion.AtlasName);
        SetOptionalInteger(
            state,
            "rewardQuestID",
            invasion.RewardQuestId);
    }

    private static void ApplyVector2Mixin(lua_State state)
    {
        var target = lua_gettop(state);
        lua_getglobal(state, "Vector2DMixin");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        var mixin = lua_gettop(state);
        lua_pushnil(state);
        while (lua_next(state, mixin) != 0)
        {
            lua_pushvalue(state, -2);
            lua_pushvalue(state, -2);
            lua_settable(state, target);
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (int)value;
    }

    private static void SetInteger(
        lua_State state,
        string field,
        int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(
        lua_State state,
        string field,
        string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }
}
