using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDeathInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "GetCorpseMapPosition",
        "GetDeathReleasePosition",
        "GetGraveyardsForMap",
        "GetSelfResurrectOptions",
        "UseSelfResurrectOption"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_DeathInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var deathInfo = LuaBindings.GetRuntime(state).DeathInfo;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetCorpseMapPosition":
                return PushMapPosition(
                    state,
                    deathInfo.CorpsePositionsByUiMapId,
                    "Usage: local position = " +
                    "C_DeathInfo.GetCorpseMapPosition(uiMapID)");
            case "GetDeathReleasePosition":
                return PushMapPosition(
                    state,
                    deathInfo.DeathReleasePositionsByUiMapId,
                    "Usage: local position = " +
                    "C_DeathInfo.GetDeathReleasePosition(uiMapID)");
            case "GetGraveyardsForMap":
            {
                const string usage =
                    "Usage: local graveyards = " +
                    "C_DeathInfo.GetGraveyardsForMap(uiMapID)";
                var uiMapId = RequiredInt32(state, 1, usage);
                deathInfo.GraveyardsByUiMapId.TryGetValue(
                    uiMapId,
                    out var graveyards);
                PushGraveyards(state, graveyards);
                return 1;
            }
            case "GetSelfResurrectOptions":
                if (!deathInfo.SelfResurrectOptionsAvailable)
                    return 0;
                PushSelfResurrectOptions(
                    state,
                    deathInfo.SelfResurrectOptions);
                return 1;
            case "UseSelfResurrectOption":
            {
                const string usage =
                    "Usage: C_DeathInfo.UseSelfResurrectOption(" +
                    "optionType, id)";
                var optionType =
                    RequiredSelfResurrectOptionType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                deathInfo.UseSelfResurrectOptionRequests++;
                deathInfo.LastUsedOptionType = optionType;
                deathInfo.LastUsedOptionId = id;
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int PushMapPosition(
        lua_State state,
        IDictionary<int, WowDeathMapPositionState> positions,
        string usage)
    {
        var uiMapId = RequiredInt32(state, 1, usage);
        if (!positions.TryGetValue(uiMapId, out var position))
        {
            lua_pushnil(state);
            return 1;
        }

        PushVector2(state, position.X, position.Y);
        return 1;
    }

    private static void PushGraveyards(
        lua_State state,
        IList<WowGraveyardMapInfoState>? graveyards)
    {
        lua_createtable(state, graveyards?.Count ?? 0, 0);
        if (graveyards is null)
            return;

        for (var index = 0; index < graveyards.Count; index++)
        {
            var graveyard = graveyards[index];
            lua_createtable(state, 0, 6);
            SetNumber(state, "areaPoiID", graveyard.AreaPoiId);
            PushVector2(state, graveyard.X, graveyard.Y);
            lua_setfield(state, -2, "position");
            SetOptionalString(state, "name", graveyard.Name);
            SetNumber(state, "textureIndex", graveyard.TextureIndex);
            SetNumber(state, "graveyardID", graveyard.GraveyardId);
            SetBoolean(
                state,
                "isGraveyardSelectable",
                graveyard.IsGraveyardSelectable);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushSelfResurrectOptions(
        lua_State state,
        IList<WowSelfResurrectOptionState> options)
    {
        lua_createtable(state, options.Count, 0);
        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            lua_createtable(state, 0, 6);
            SetString(state, "name", option.Name);
            SetNumber(state, "optionType", option.OptionType);
            SetNumber(state, "id", option.Id);
            SetBoolean(state, "canUse", option.CanUse);
            SetBoolean(state, "isLimited", option.IsLimited);
            SetNumber(state, "priority", option.Priority);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static byte RequiredSelfResurrectOptionType(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        var optionType = unchecked((byte)value);
        if (optionType > 1)
            return (byte)RaiseArgumentError(state, usage);
        return optionType;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return RaiseArgumentError(state, usage);

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return RaiseArgumentError(state, usage);
        }
        return unchecked((int)value);
    }

    private static int RaiseArgumentError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void PushVector2(
        lua_State state,
        double x,
        double y)
    {
        lua_createtable(state, 0, 2);
        SetNumber(state, "x", x);
        SetNumber(state, "y", y);
        ApplyVector2Mixin(state);
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

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 2);
        SetNumber(state, "Spell", 0);
        SetNumber(state, "Item", 1);
        lua_setfield(state, -2, "SelfResurrectOptionType");

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", 2);
        SetNumber(state, "MinValue", 0);
        SetNumber(state, "MaxValue", 1);
        lua_setfield(state, -2, "SelfResurrectOptionTypeMeta");
        lua_pop(state, 1);
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(
        lua_State state,
        string field,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
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
}
