using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMapApi : LuaApiModule
{
    private const string UserWaypointUpdated = "USER_WAYPOINT_UPDATED";
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetBestMapForUnit",
        "GetFallbackWorldMapID",
        "CanSetUserWaypointOnMap",
        "CloseWorldMapInteraction",
        "ClearUserWaypoint",
        "GetMapArtBackgroundAtlas",
        "GetAreaInfo",
        "GetMapArtHelpTextPosition",
        "GetMapArtID",
        "GetMapArtLayers",
        "GetMapArtLayerTextures",
        "GetMapArtZoneTextPosition",
        "GetMapBannersForMap",
        "GetMapChildrenInfo",
        "GetMapDisplayInfo",
        "GetMapGroupID",
        "GetMapGroupMembersInfo",
        "GetMapHighlightInfoAtPosition",
        "GetMapHighlightPulseInfo",
        "GetMapInfo",
        "GetMapInfoAtPosition",
        "GetMapLevels",
        "GetMapLinksForMap",
        "GetMapRectOnMap",
        "GetMapWorldSize",
        "GetPlayerMapPosition",
        "GetUserWaypointPositionForMap",
        "GetWorldPosFromMapPos",
        "HasUserWaypoint",
        "IsMapValidForNavBarDropdown",
        "MapHasArt",
        "RequestPreloadMap",
        "SetUserWaypoint"
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
        lua_setglobal(state, "C_Map");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation switch
        {
            "GetBestMapForUnit" => GetBestMapForUnit(state, runtime),
            "GetFallbackWorldMapID" => PushInteger(state, 947),
            "GetAreaInfo" => GetAreaInfo(state, runtime),
            "CanSetUserWaypointOnMap" => CanSetUserWaypointOnMap(state, runtime),
            "CloseWorldMapInteraction" => CloseWorldMapInteraction(runtime),
            "ClearUserWaypoint" => ClearUserWaypoint(runtime),
            "GetMapArtBackgroundAtlas" => GetMapArtBackgroundAtlas(state, runtime),
            "GetMapArtHelpTextPosition" => GetMapArtHelpTextPosition(state, runtime),
            "GetMapArtID" => GetMapArtId(state, runtime),
            "GetMapArtLayers" => GetMapArtLayers(state, runtime),
            "GetMapArtLayerTextures" => GetMapArtLayerTextures(state, runtime),
            "GetMapArtZoneTextPosition" => GetMapArtZoneTextPosition(state, runtime),
            "GetMapBannersForMap" => GetMapBannersForMap(state, runtime),
            "GetMapChildrenInfo" => GetMapChildrenInfo(state, runtime),
            "GetMapDisplayInfo" => GetMapDisplayInfo(state, runtime),
            "GetMapGroupID" => GetMapGroupId(state, runtime),
            "GetMapGroupMembersInfo" => GetMapGroupMembersInfo(state, runtime),
            "GetMapHighlightInfoAtPosition" => GetMapHighlightInfoAtPosition(state, runtime),
            "GetMapHighlightPulseInfo" => GetMapHighlightPulseInfo(state, runtime),
            "GetMapInfo" => GetMapInfo(state, runtime),
            "GetMapInfoAtPosition" => GetMapInfoAtPosition(state, runtime),
            "GetMapLevels" => GetMapLevels(state, runtime),
            "GetMapLinksForMap" => GetMapLinksForMap(state, runtime),
            "GetMapRectOnMap" => GetMapRectOnMap(state, runtime),
            "GetMapWorldSize" => GetMapWorldSize(state, runtime),
            "GetPlayerMapPosition" => GetPlayerMapPosition(state, runtime),
            "GetUserWaypointPositionForMap" => GetUserWaypointPositionForMap(state, runtime),
            "GetWorldPosFromMapPos" => GetWorldPosFromMapPos(state, runtime),
            "HasUserWaypoint" => PushBoolean(state, HasValidUserWaypoint(runtime.Maps)),
            "IsMapValidForNavBarDropdown" => IsMapValidForNavBarDropdown(state, runtime),
            "MapHasArt" => MapHasArt(state, runtime),
            "RequestPreloadMap" => RequestPreloadMap(state, runtime),
            "SetUserWaypoint" => SetUserWaypoint(state, runtime),
            _ => 0
        };
    }

    private static int PushInteger(lua_State state, int value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static int GetAreaInfo(lua_State state, LuaRuntime runtime)
    {
        const string usage = "Usage: local name = C_Map.GetAreaInfo(areaID)";
        var areaId = ReadInt32(state, 1, usage);
        if (!runtime.Maps.AreaNameOverrides.TryGetValue(areaId, out var name) &&
            !(runtime.MapProvider?.TryGetAreaName(areaId, out name) ?? false))
        {
            lua_pushnil(state);
            return 1;
        }

        lua_pushstring(state, name);
        return 1;
    }

    private static int GetBestMapForUnit(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local uiMapID = C_Map.GetBestMapForUnit(unitToken)";
        var unit = ReadUnitToken(state, 1, usage);
        int? mapId = null;
        if (runtime.Maps.BestMapByUnit.TryGetValue(unit, out var mapped))
            mapId = mapped;
        else if (unit.Equals("player", StringComparison.OrdinalIgnoreCase))
            mapId = runtime.Maps.BestMapForPlayer > 0
                ? runtime.Maps.BestMapForPlayer
                : null;

        if (mapId is { } value)
            lua_pushinteger(state, value);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int CanSetUserWaypointOnMap(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local canSet = C_Map.CanSetUserWaypointOnMap(uiMapID)";
        return PushBoolean(
            state,
            CanSetUserWaypoint(runtime, ReadInt32(state, 1, usage)));
    }

    private static int CloseWorldMapInteraction(LuaRuntime runtime)
    {
        const int worldMapInteractionType = 29;
        var interactions = runtime.PlayerInteractions;
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = worldMapInteractionType;
        if (interactions.HasActiveInteraction &&
            interactions.CurrentInteractionType == worldMapInteractionType)
        {
            interactions.HasActiveInteraction = false;
            interactions.HasPendingInteraction = false;
            interactions.CurrentInteractionType = 0;
            interactions.PendingInteractionType = 0;
            interactions.ValidNpcInteractionTypes.Clear();
        }
        return 0;
    }

    private static int ClearUserWaypoint(LuaRuntime runtime)
    {
        var hadWaypoint = HasValidUserWaypoint(runtime.Maps);
        runtime.Maps.UserWaypoint = null;
        runtime.Maps.UserWaypointProjections.Clear();
        if (hadWaypoint)
            runtime.TriggerEvent(UserWaypointUpdated);
        return 0;
    }

    private static int GetMapArtBackgroundAtlas(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local atlasName = C_Map.GetMapArtBackgroundAtlas(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        if (TryGetDetails(runtime, mapId, out var details) &&
            !string.IsNullOrEmpty(details.BackgroundAtlas))
        {
            lua_pushstring(state, details.BackgroundAtlas);
        }
        else
        {
            lua_pushnil(state);
        }
        return 1;
    }

    private static int GetMapArtHelpTextPosition(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local position = C_Map.GetMapArtHelpTextPosition(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        lua_pushinteger(
            state,
            TryGetDetails(runtime, mapId, out var details)
                ? details.HelpTextPosition
                : 0);
        return 1;
    }

    private static int GetMapArtId(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local uiMapArtID = C_Map.GetMapArtID(uiMapID)";
        if (!TryGetArt(runtime, ReadInt32(state, 1, usage), out var art) ||
            art.MapArtId == 0)
        {
            return 0;
        }
        lua_pushinteger(state, art.MapArtId);
        return 1;
    }

    private static int GetMapArtLayers(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local layerInfo = C_Map.GetMapArtLayers(uiMapID)";
        if (!TryGetArt(runtime, ReadInt32(state, 1, usage), out var art))
            return 0;

        lua_createtable(state, art.Layers.Count, 0);
        for (var index = 0; index < art.Layers.Count; index++)
        {
            var layer = art.Layers[index];
            lua_createtable(state, 0, 7);
            SetInteger(state, "layerWidth", layer.LayerWidth);
            SetInteger(state, "layerHeight", layer.LayerHeight);
            SetInteger(state, "tileWidth", layer.TileWidth);
            SetInteger(state, "tileHeight", layer.TileHeight);
            SetNumber(state, "minScale", layer.MinScale);
            SetNumber(state, "maxScale", layer.MaxScale);
            SetInteger(state, "additionalZoomSteps", layer.AdditionalZoomSteps);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetMapArtLayerTextures(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local textures = C_Map.GetMapArtLayerTextures(uiMapID, layerIndex)";
        var mapId = ReadInt32(state, 1, usage);
        var layerIndex = ReadOneBasedIndex(state, 2, usage) - 1;
        if (!TryGetArt(runtime, mapId, out var art) ||
            layerIndex < 0 ||
            layerIndex >= art.Layers.Count)
        {
            return 0;
        }

        var textures = art.Layers[layerIndex].Textures;
        lua_createtable(state, textures.Count, 0);
        for (var index = 0; index < textures.Count; index++)
        {
            if (textures[index] == 0)
                lua_pushnil(state);
            else
                lua_pushnumber(state, textures[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetMapArtZoneTextPosition(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local position = C_Map.GetMapArtZoneTextPosition(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        lua_pushinteger(
            state,
            TryGetDetails(runtime, mapId, out var details)
                ? details.MapArtZoneTextPosition
                : 0);
        return 1;
    }

    private static int GetMapBannersForMap(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local mapBanners = C_Map.GetMapBannersForMap(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        var banners = runtime.Maps.MapBanners.TryGetValue(mapId, out var values)
            ? values
            : [];
        lua_createtable(state, banners.Count, 0);
        for (var index = 0; index < banners.Count; index++)
        {
            var banner = banners[index];
            lua_createtable(state, 0, 4);
            SetInteger(state, "areaPoiID", banner.AreaPoiId);
            SetOptionalString(state, "name", banner.Name);
            SetString(state, "atlasName", banner.AtlasName);
            SetOptionalString(state, "uiTextureKit", banner.UiTextureKit);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetMapChildrenInfo(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local info = C_Map.GetMapChildrenInfo(uiMapID [, mapType, allDescendants])";
        var mapId = ReadInt32(state, 1, usage);
        int? mapType = null;
        if (lua_type(state, 2) is not (LUA_TNONE or LUA_TNIL))
        {
            var parsedMapType = ReadInt32(state, 2, usage);
            if (parsedMapType is < 0 or > 6)
                return luaL_error(state, usage);
            mapType = parsedMapType;
        }
        var allDescendants = lua_type(state, 3) is not (LUA_TNONE or LUA_TNIL) &&
                             lua_toboolean(state, 3) != 0;
        if (!TryGetDetails(runtime, mapId, out _))
            return 0;

        var children = GetMapChildren(runtime, mapId, mapType, allDescendants);
        lua_createtable(state, children.Count, 0);
        for (var index = 0; index < children.Count; index++)
        {
            PushDetails(state, children[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetMapDisplayInfo(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local hideIcons = C_Map.GetMapDisplayInfo(uiMapID)";
        if (!TryGetDetails(runtime, ReadInt32(state, 1, usage), out var details))
            return 0;
        return PushBoolean(state, (details.Flags & 0x400) != 0);
    }

    private static int GetMapGroupId(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local uiMapGroupID = C_Map.GetMapGroupID(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        if (!runtime.Maps.MapGroupIds.TryGetValue(mapId, out var groupId) ||
            groupId == 0)
        {
            return 0;
        }
        lua_pushinteger(state, groupId);
        return 1;
    }

    private static int GetMapGroupMembersInfo(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local info = C_Map.GetMapGroupMembersInfo(uiMapGroupID)";
        var groupId = ReadInt32(state, 1, usage);
        if (!runtime.Maps.MapGroupMembers.TryGetValue(groupId, out var members) ||
            members.Count == 0)
        {
            return 0;
        }

        lua_createtable(state, members.Count, 0);
        for (var index = 0; index < members.Count; index++)
        {
            var member = members[index];
            lua_createtable(state, 0, 3);
            SetInteger(state, "mapID", member.MapId);
            SetInteger(state, "relativeHeightIndex", member.RelativeHeightIndex);
            SetOptionalString(state, "name", member.Name);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetMapHighlightInfoAtPosition(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local fileDataID, atlasID, texturePercentageX, texturePercentageY, textureX, textureY, scrollChildX, scrollChildY = C_Map.GetMapHighlightInfoAtPosition(uiMapID, x, y)";
        var mapId = ReadInt32(state, 1, usage);
        var x = ReadFloat(state, 2, usage);
        var y = ReadFloat(state, 3, usage);
        if (runtime.MapProvider?.TryGetMapHighlight(mapId, x, y, out var highlight) != true)
            return 0;
        return PushHighlight(state, highlight);
    }

    private static int GetMapHighlightPulseInfo(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local fileDataID, atlasID, texturePercentageX, texturePercentageY, textureX, textureY, scrollChildX, scrollChildY = C_Map.GetMapHighlightPulseInfo(uiMapID)";
        if (!runtime.Maps.MapHighlightPulses.TryGetValue(
                ReadInt32(state, 1, usage),
                out var highlight))
        {
            return 0;
        }
        return PushHighlight(state, highlight);
    }

    private static int GetMapInfo(lua_State state, LuaRuntime runtime)
    {
        const string usage = "Usage: local info = C_Map.GetMapInfo(uiMapID)";
        if (!TryGetDetails(runtime, ReadInt32(state, 1, usage), out var details))
            return 0;
        PushDetails(state, details);
        return 1;
    }

    private static int GetMapInfoAtPosition(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local info = C_Map.GetMapInfoAtPosition(uiMapID, x, y [, ignoreZoneMapPositionData])";
        var mapId = ReadInt32(state, 1, usage);
        var x = ReadFloat(state, 2, usage);
        var y = ReadFloat(state, 3, usage);
        var ignoreZoneMapPositionData =
            lua_type(state, 4) is not (LUA_TNONE or LUA_TNIL) &&
            lua_toboolean(state, 4) != 0;
        if (runtime.MapProvider?.TryGetMapAtPosition(
                mapId,
                x,
                y,
                ignoreZoneMapPositionData,
                out var details) != true)
        {
            return 0;
        }
        PushDetails(state, details);
        return 1;
    }

    private static int GetMapLevels(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local playerMinLevel, playerMaxLevel, petMinLevel, petMaxLevel = C_Map.GetMapLevels(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        if (!TryGetDetails(runtime, mapId, out _))
            return 0;
        var levels = runtime.Maps.LevelOverrides.TryGetValue(mapId, out var value)
            ? value
            : default;
        lua_pushinteger(state, levels.PlayerMinLevel);
        lua_pushinteger(state, levels.PlayerMaxLevel);
        lua_pushinteger(state, levels.PetMinLevel);
        lua_pushinteger(state, levels.PetMaxLevel);
        return 4;
    }

    private static int GetMapLinksForMap(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local mapLinks = C_Map.GetMapLinksForMap(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        var links = runtime.Maps.MapLinks.TryGetValue(mapId, out var values)
            ? values
            : [];
        lua_createtable(state, links.Count, 0);
        for (var index = 0; index < links.Count; index++)
        {
            var link = links[index];
            lua_createtable(state, 0, 5);
            SetInteger(state, "areaPoiID", link.AreaPoiId);
            PushVector2(state, link.X, link.Y);
            lua_setfield(state, -2, "position");
            SetOptionalString(state, "name", link.Name);
            SetString(state, "atlasName", link.AtlasName);
            SetInteger(state, "linkedUiMapID", link.LinkedUiMapId);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetPlayerMapPosition(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local position = C_Map.GetPlayerMapPosition(uiMapID, unitToken)";
        var mapId = ReadInt32(state, 1, usage);
        var unit = ReadUnitToken(state, 2, usage);
        if (!runtime.Maps.PlayerPositionsByUnit.TryGetValue(unit, out var positions) ||
            !positions.TryGetValue(mapId, out var position))
        {
            lua_pushnil(state);
            return 1;
        }
        PushVector2(state, position.X, position.Y);
        return 1;
    }

    private static int GetUserWaypointPositionForMap(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local mapPosition = C_Map.GetUserWaypointPositionForMap(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        if (!HasValidUserWaypoint(runtime.Maps))
            return 0;

        var waypoint = runtime.Maps.UserWaypoint!.Value;
        WowMapPosition position;
        if (waypoint.MapId == mapId)
            position = new WowMapPosition(waypoint.X, waypoint.Y);
        else if (!runtime.Maps.UserWaypointProjections.TryGetValue(mapId, out position))
            return 0;
        PushVector2(state, position.X, position.Y);
        return 1;
    }

    private static int IsMapValidForNavBarDropdown(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local isValid = C_Map.IsMapValidForNavBarDropdown(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        if (runtime.Maps.NavBarValidityOverrides.TryGetValue(mapId, out var value))
            return PushBoolean(state, value);

        const int forceOnNavBar = 0x8000;
        const int doNotShowOnNavBar = 0x80000;
        var isValid = TryGetDetails(runtime, mapId, out var details) &&
                      (details.Flags & doNotShowOnNavBar) == 0 &&
                      details.PlayerConditionId == 0 &&
                      (details.MapType is 1 or 2 or 3 ||
                       (details.Flags & forceOnNavBar) != 0);
        return PushBoolean(state, isValid);
    }

    private static int MapHasArt(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local hasArt = C_Map.MapHasArt(uiMapID)";
        return PushBoolean(
            state,
            TryGetArt(runtime, ReadInt32(state, 1, usage), out _));
    }

    private static int GetWorldPosFromMapPos(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local continentID, worldPosition = C_Map.GetWorldPosFromMapPos(uiMapID, mapPosition)";
        var mapId = ReadInt32(state, 1, usage);
        var position = ReadVector2(state, 2, usage);
        if (runtime.MapProvider?.TryMapPositionToWorld(
                mapId,
                position.X,
                position.Y,
                out var worldMapId,
                out var worldPosition) != true)
        {
            return 0;
        }

        lua_pushnumber(state, worldMapId);
        PushVector2(state, worldPosition.X, worldPosition.Y);
        return 2;
    }

    private static int GetMapWorldSize(lua_State state, LuaRuntime runtime)
    {
        const string usage = "Usage: local width, height = C_Map.GetMapWorldSize(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        var width = 0d;
        var height = 0d;
        runtime.MapProvider?.TryGetMapWorldSize(mapId, out width, out height);
        lua_pushnumber(state, width);
        lua_pushnumber(state, height);
        return 2;
    }

    private static int GetMapRectOnMap(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local minX, maxX, minY, maxY = C_Map.GetMapRectOnMap(uiMapID, topUiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        var topMapId = ReadInt32(state, 2, usage);
        if (runtime.MapProvider?.TryGetMapRectangle(
                mapId,
                topMapId,
                out var minimumX,
                out var maximumX,
                out var minimumY,
                out var maximumY) != true)
        {
            return 0;
        }

        lua_pushnumber(state, minimumX);
        lua_pushnumber(state, maximumX);
        lua_pushnumber(state, minimumY);
        lua_pushnumber(state, maximumY);
        return 4;
    }

    private static int RequestPreloadMap(lua_State state, LuaRuntime runtime)
    {
        const string usage = "Usage: C_Map.RequestPreloadMap(uiMapID)";
        var mapId = ReadInt32(state, 1, usage);
        runtime.Maps.PreloadRequestCount++;
        runtime.Maps.PreloadRequests.Add(mapId);
        return 0;
    }

    private static int SetUserWaypoint(lua_State state, LuaRuntime runtime)
    {
        const string usage = "Usage: C_Map.SetUserWaypoint(point)";
        var waypoint = ReadUserWaypoint(state, 1, usage);
        if (!CanSetUserWaypoint(runtime, waypoint.MapId))
            return 0;

        var prior = runtime.Maps.UserWaypoint;
        if (prior is { } existing &&
            existing.MapId == waypoint.MapId &&
            existing.X == waypoint.X &&
            existing.Y == waypoint.Y &&
            existing.Z.HasValue == waypoint.Z.HasValue &&
            (!existing.Z.HasValue || existing.Z.Value == waypoint.Z!.Value))
        {
            return 0;
        }

        runtime.Maps.UserWaypoint = waypoint;
        runtime.Maps.UserWaypointProjections.Clear();
        runtime.TriggerEvent(UserWaypointUpdated);
        return 0;
    }

    private static bool CanSetUserWaypoint(LuaRuntime runtime, int mapId)
    {
        if (!TryGetDetails(runtime, mapId, out var details))
            return false;
        return details.MapType is not (4 or 6) ||
               (details.Flags & 0x10000) != 0;
    }

    private static bool HasValidUserWaypoint(WowMapState maps) =>
        maps.UserWaypoint is { MapId: not 0 } waypoint &&
        waypoint.X is >= 0 and <= 1 &&
        waypoint.Y is >= 0 and <= 1;

    private static WowUserWaypoint ReadUserWaypoint(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_istable(state, index) == 0)
            return ErrorWaypoint(state, usage);

        var absoluteIndex = AbsoluteIndex(state, index);
        lua_getfield(state, absoluteIndex, "uiMapID");
        var mapId = ReadInt32(state, -1, usage);
        lua_pop(state, 1);

        lua_getfield(state, absoluteIndex, "position");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            return ErrorWaypoint(state, usage);
        }
        var positionIndex = AbsoluteIndex(state, -1);
        lua_getfield(state, positionIndex, "x");
        var x = (double)(float)ReadFloat(state, -1, usage);
        lua_pop(state, 1);
        lua_getfield(state, positionIndex, "y");
        var y = (double)(float)ReadFloat(state, -1, usage);
        lua_pop(state, 2);

        double? z = null;
        lua_getfield(state, absoluteIndex, "z");
        if (lua_type(state, -1) is not (LUA_TNONE or LUA_TNIL))
            z = (double)(float)ReadFloat(state, -1, usage);
        lua_pop(state, 1);

        if (mapId == 0 || x is < 0 or > 1 || y is < 0 or > 1)
            return ErrorWaypoint(state, usage);
        return new WowUserWaypoint(mapId, x, y, z);
    }

    private static WowMapPosition ReadVector2(lua_State state, int index, string usage)
    {
        if (lua_istable(state, index) == 0)
        {
            luaL_error(state, usage);
            return default;
        }

        var tableIndex = AbsoluteIndex(state, index);
        lua_getfield(state, tableIndex, "x");
        var x = ReadFloat(state, -1, usage);
        lua_pop(state, 1);
        lua_getfield(state, tableIndex, "y");
        var y = ReadFloat(state, -1, usage);
        lua_pop(state, 1);
        return new WowMapPosition(x, y);
    }

    private static WowUserWaypoint ErrorWaypoint(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return default;
    }

    private static int PushHighlight(lua_State state, WowMapHighlight highlight)
    {
        if (highlight.FileDataId == 0)
            lua_pushnil(state);
        else
            lua_pushnumber(state, highlight.FileDataId);
        if (string.IsNullOrEmpty(highlight.AtlasId))
            lua_pushnil(state);
        else
            lua_pushstring(state, highlight.AtlasId);
        lua_pushnumber(state, highlight.TexturePercentageX);
        lua_pushnumber(state, highlight.TexturePercentageY);
        lua_pushnumber(state, highlight.TextureWidth);
        lua_pushnumber(state, highlight.TextureHeight);
        lua_pushnumber(state, highlight.OffsetX);
        lua_pushnumber(state, highlight.OffsetY);
        return 8;
    }

    private static void PushVector2(lua_State state, double x, double y)
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

    private static bool TryGetDetails(
        LuaRuntime runtime,
        int mapId,
        out WowMapDetails details)
    {
        if (runtime.Maps.MapOverrides.TryGetValue(mapId, out details))
            return true;
        if (runtime.MapProvider?.TryGetMapDetails(mapId, out details) == true)
            return true;
        if (mapId == runtime.Maps.BestMapForPlayer && mapId > 0)
        {
            details = new WowMapDetails(
                mapId,
                mapId == 84 ? "Stormwind City" : $"Emulator Map {mapId}",
                3,
                mapId == 84 ? 13 : 0,
                0,
                "");
            return true;
        }
        details = default;
        return false;
    }

    private static bool TryGetArt(LuaRuntime runtime, int mapId, out WowMapArt art)
    {
        if (runtime.Maps.MapArtOverrides.TryGetValue(mapId, out art!))
            return true;
        return runtime.MapProvider?.TryGetMapArt(mapId, out art!) == true;
    }

    private static IReadOnlyList<WowMapDetails> GetMapChildren(
        LuaRuntime runtime,
        int mapId,
        int? mapType,
        bool allDescendants)
    {
        var children = new Dictionary<int, WowMapDetails>();
        foreach (var details in runtime.MapProvider?.GetMapChildren(
                     mapId,
                     mapType,
                     allDescendants) ?? [])
        {
            children[details.MapId] = details;
        }

        var pending = new Queue<int>();
        pending.Enqueue(mapId);
        while (pending.TryDequeue(out var parentMapId))
        {
            foreach (var child in runtime.Maps.MapOverrides.Values
                         .Where(value => value.ParentMapId == parentMapId)
                         .OrderBy(value => value.MapId))
            {
                if (mapType is null || child.MapType == mapType)
                    children[child.MapId] = child;
                if (allDescendants)
                    pending.Enqueue(child.MapId);
            }
        }
        return children.Values.OrderBy(value => value.MapId).ToArray();
    }

    private static void PushDetails(lua_State state, WowMapDetails details)
    {
        lua_createtable(state, 0, 5);
        SetInteger(state, "mapID", details.MapId);
        SetString(state, "name", details.Name);
        SetInteger(state, "mapType", details.MapType);
        SetInteger(state, "parentMapID", details.ParentMapId);
        SetInteger(state, "flags", details.Flags);
    }

    private static int ReadInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (double.IsNaN(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return unchecked((int)value);
    }

    private static int ReadOneBasedIndex(lua_State state, int index, string usage)
    {
        var value = ReadInt32(state, index, usage);
        if (value < 1)
            return luaL_error(state, usage);
        return value;
    }

    private static double ReadFloat(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (float)lua_tonumber(state, index);
    }

    private static string ReadUnitToken(lua_State state, int index, string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(lua_State state, string field, double value)
    {
        lua_pushnumber(state, value);
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
        if (value is not null)
            SetString(state, field, value);
    }
}
