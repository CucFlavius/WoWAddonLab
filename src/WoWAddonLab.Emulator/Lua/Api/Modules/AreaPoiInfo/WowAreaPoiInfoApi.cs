using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAreaPoiInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "GetAreaPOIForMap", "GetAreaPOIInfo", "GetAreaPOISecondsLeft",
        "GetDelvesForMap", "GetDragonridingRacesForMap", "GetEventsForMap",
        "GetQuestHubsForMap", "IsAreaPOITimed"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_AreaPoiInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var areaPoi = LuaBindings.GetRuntime(state).AreaPoiInfo;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAreaPOIForMap":
                return PushMapPoiIds(
                    state,
                    areaPoi.AreaPoiIdsByMapId,
                    "Usage: local areaPoiIDs = " +
                    "C_AreaPoiInfo.GetAreaPOIForMap(uiMapID)");
            case "GetAreaPOIInfo":
            {
                const string usage =
                    "Usage: local poiInfo = " +
                    "C_AreaPoiInfo.GetAreaPOIInfo(" +
                    "[uiMapID], areaPoiID)";
                var uiMapId = OptionalInt32(state, 1, usage);
                var areaPoiId = RequiredInt32(state, 2, usage);

                WowAreaPoiInfoState? info;
                if (uiMapId.HasValue)
                {
                    areaPoi.PoiInfoByMapAndId.TryGetValue(
                        (uiMapId.Value, areaPoiId),
                        out info);
                }
                else
                {
                    areaPoi.PoiInfoById.TryGetValue(
                        areaPoiId,
                        out info);
                }

                if (info is null)
                    return 0;
                PushAreaPoiInfo(state, info);
                return 1;
            }
            case "GetAreaPOISecondsLeft":
            {
                const string usage =
                    "Usage: local secondsLeft = " +
                    "C_AreaPoiInfo.GetAreaPOISecondsLeft(areaPoiID)";
                var areaPoiId = RequiredInt32(state, 1, usage);
                if (!areaPoi.SecondsLeftByAreaPoiId.TryGetValue(
                        areaPoiId,
                        out var secondsLeft))
                {
                    return 0;
                }
                lua_pushnumber(state, Math.Max(0, secondsLeft));
                return 1;
            }
            case "GetDelvesForMap":
                return PushMapPoiIds(
                    state,
                    areaPoi.DelveIdsByMapId,
                    "Usage: local areaPoiIDs = " +
                    "C_AreaPoiInfo.GetDelvesForMap(uiMapID)");
            case "GetDragonridingRacesForMap":
                return PushMapPoiIds(
                    state,
                    areaPoi.DragonridingRaceIdsByMapId,
                    "Usage: local areaPoiIDs = " +
                    "C_AreaPoiInfo.GetDragonridingRacesForMap(uiMapID)");
            case "GetEventsForMap":
                return PushMapPoiIds(
                    state,
                    areaPoi.EventIdsByMapId,
                    "Usage: local areaPoiIDs = " +
                    "C_AreaPoiInfo.GetEventsForMap(uiMapID)");
            case "GetQuestHubsForMap":
                return PushMapPoiIds(
                    state,
                    areaPoi.QuestHubIdsByMapId,
                    "Usage: local areaPoiIDs = " +
                    "C_AreaPoiInfo.GetQuestHubsForMap(uiMapID)");
            case "IsAreaPOITimed":
            {
                const string usage =
                    "Usage: local isTimed, hideTimerInTooltip = " +
                    "C_AreaPoiInfo.IsAreaPOITimed(areaPoiID)";
                var areaPoiId = RequiredInt32(state, 1, usage);
                if (areaPoi.HideTimerInTooltipByTimedAreaPoiId
                    .TryGetValue(areaPoiId, out var hideTimer))
                {
                    lua_pushboolean(state, 1);
                    lua_pushboolean(state, hideTimer ? 1 : 0);
                }
                else
                {
                    lua_pushboolean(state, 0);
                    lua_pushnil(state);
                }
                return 2;
            }
            default:
                return 0;
        }
    }

    private static int PushMapPoiIds(
        lua_State state,
        IDictionary<int, IList<int>> idsByMapId,
        string usage)
    {
        var uiMapId = RequiredInt32(state, 1, usage);
        idsByMapId.TryGetValue(uiMapId, out var ids);
        lua_createtable(state, ids?.Count ?? 0, 0);
        if (ids is not null)
        {
            for (var index = 0; index < ids.Count; index++)
            {
                lua_pushnumber(state, ids[index]);
                lua_rawseti(state, -2, index + 1);
            }
        }
        return 1;
    }

    private static void PushAreaPoiInfo(
        lua_State state,
        WowAreaPoiInfoState info)
    {
        lua_createtable(state, 0, 20);
        SetNumber(state, "areaPoiID", info.AreaPoiId);

        lua_createtable(state, 0, 2);
        SetNumber(state, "x", info.X);
        SetNumber(state, "y", info.Y);
        ApplyVector2Mixin(state);
        lua_setfield(state, -2, "position");

        SetOptionalString(state, "name", info.Name);
        SetOptionalString(state, "description", info.Description);
        SetOptionalInteger(state, "linkedUiMapID", info.LinkedUiMapId);
        SetOptionalInteger(state, "textureIndex", info.TextureIndex);
        SetOptionalInteger(
            state,
            "tooltipWidgetSet",
            info.TooltipWidgetSet);
        SetOptionalInteger(state, "iconWidgetSet", info.IconWidgetSet);
        SetOptionalString(state, "atlasName", info.AtlasName);
        SetOptionalString(state, "uiTextureKit", info.UiTextureKit);
        SetBoolean(state, "shouldGlow", info.ShouldGlow);
        SetOptionalInteger(state, "factionID", info.FactionId);
        SetBoolean(
            state,
            "isPrimaryMapForPOI",
            info.IsPrimaryMapForPoi);
        SetBoolean(
            state,
            "isAlwaysOnFlightmap",
            info.IsAlwaysOnFlightmap);
        SetOptionalBoolean(
            state,
            "addPaddingAboveTooltipWidgets",
            info.AddPaddingAboveTooltipWidgets);
        SetBoolean(
            state,
            "highlightWorldQuestsOnHover",
            info.HighlightWorldQuestsOnHover);
        SetBoolean(
            state,
            "highlightVignettesOnHover",
            info.HighlightVignettesOnHover);
        SetBoolean(state, "isCurrentEvent", info.IsCurrentEvent);
        SetBoolean(state, "isSuppressible", info.IsSuppressible);
        SetBoolean(state, "isLocked", info.IsLocked);
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

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredInt32(state, index, usage);
    }

    private static int RaiseArgumentError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
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

    private static void SetOptionalBoolean(
        lua_State state,
        string field,
        bool? value)
    {
        if (value.HasValue)
            lua_pushboolean(state, value.Value ? 1 : 0);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        if (value.HasValue)
            lua_pushnumber(state, value.Value);
        else
            lua_pushnil(state);
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
}
