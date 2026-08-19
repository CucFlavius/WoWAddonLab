using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowContentTrackingApi : LuaApiModule
{
    private const uint Success = 0;
    private const uint Failure = 2;
    private const int Untrackable = 0;
    private const int CapacityExceeded = 1;
    private const int AlreadyTracked = 2;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetBestMapForTrackable", "GetCollectableSourceTrackingEnabled",
        "GetCollectableSourceTypes", "GetCurrentTrackingTarget",
        "GetEncounterTrackingInfo", "GetNextWaypointForTrackable", "GetObjectiveText",
        "GetTitle", "GetTrackablesOnMap", "GetTrackedIDs", "GetVendorTrackingInfo",
        "GetWaypointText", "IsNavigable", "IsTrackable", "IsTracking", "StartTracking",
        "StopTracking", "ToggleTracking"
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
        lua_setglobal(state, "C_ContentTracking");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var tracking = runtime.ContentTracking;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetBestMapForTrackable":
            {
                const string usage =
                    "Usage: local result, mapID = C_ContentTracking.GetBestMapForTrackable(trackableType, trackableID [, ignoreWaypoint])";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                var ignoreWaypoint = OptionalBoolean(state, 3, usage);
                var result = tracking.BestMaps.TryGetValue(
                    (type, id, ignoreWaypoint),
                    out var configured)
                    ? configured
                    : new WowContentTrackingBestMapResult(Failure, null);
                lua_pushinteger(state, result.Result);
                PushOptionalInt32(state, result.MapId);
                return 2;
            }
            case "GetCollectableSourceTrackingEnabled":
                lua_pushboolean(state, tracking.CollectableSourceTrackingEnabled ? 1 : 0);
                return 1;
            case "GetCollectableSourceTypes":
                PushIntegers(state, tracking.CollectableSourceTypes.Order());
                return 1;
            case "GetCurrentTrackingTarget":
            {
                const string usage =
                    "Usage: local targetType, targetID = C_ContentTracking.GetCurrentTrackingTarget(type, id)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                if (!tracking.CurrentTargets.TryGetValue((type, id), out var target))
                    return 0;
                lua_pushinteger(state, target.TargetType);
                lua_pushinteger(state, target.TargetId);
                return 2;
            }
            case "GetEncounterTrackingInfo":
            {
                const string usage =
                    "Usage: local trackingInfo = C_ContentTracking.GetEncounterTrackingInfo(journalEncounterID)";
                var id = RequiredInt32(state, 1, usage);
                if (!tracking.EncounterInfo.TryGetValue(id, out var info))
                    return 0;
                PushEncounterInfo(state, info);
                return 1;
            }
            case "GetNextWaypointForTrackable":
            {
                const string usage =
                    "Usage: local result, mapInfo = C_ContentTracking.GetNextWaypointForTrackable(trackableType, trackableID, uiMapID)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                var mapId = RequiredInt32(state, 3, usage);
                var result = tracking.NextWaypoints.TryGetValue(
                    (type, id, mapId),
                    out var configured)
                    ? configured
                    : new WowContentTrackingWaypointResult(Failure, null);
                lua_pushinteger(state, result.Result);
                if (result.MapInfo is null)
                    lua_pushnil(state);
                else
                    PushMapInfo(state, result.MapInfo);
                return 2;
            }
            case "GetObjectiveText":
            {
                const string usage =
                    "Usage: local objectiveText = C_ContentTracking.GetObjectiveText(targetType, targetID [, includeHyperlinks])";
                var type = RequiredTargetType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                var includeHyperlinks = OptionalBoolean(state, 3, usage);
                if (!tracking.ObjectiveTexts.TryGetValue(
                        (type, id, includeHyperlinks),
                        out var text))
                {
                    return 0;
                }
                lua_pushstring(state, text);
                return 1;
            }
            case "GetTitle":
            {
                const string usage =
                    "Usage: local title = C_ContentTracking.GetTitle(trackableType, trackableID)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                if (!tracking.Titles.TryGetValue((type, id), out var title))
                    return 0;
                lua_pushstring(state, title);
                return 1;
            }
            case "GetTrackablesOnMap":
            {
                const string usage =
                    "Usage: local result, trackableMapInfos = C_ContentTracking.GetTrackablesOnMap(trackableType, uiMapID)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var mapId = RequiredInt32(state, 2, usage);
                var result = tracking.TrackablesOnMaps.TryGetValue(
                    (type, mapId),
                    out var configured)
                    ? configured
                    : new WowContentTrackingMapResult(
                        Failure,
                        Array.Empty<WowContentTrackingMapInfo>());
                lua_pushinteger(state, result.Result);
                PushMapInfos(state, result.Trackables);
                return 2;
            }
            case "GetTrackedIDs":
            {
                const string usage =
                    "Usage: local entryIDs = C_ContentTracking.GetTrackedIDs(trackableType)";
                var type = RequiredContentTrackingType(state, 1, usage);
                PushIntegers(
                    state,
                    tracking.TrackedEntries
                        .Where(entry => entry.Type == type)
                        .Select(entry => entry.Id));
                return 1;
            }
            case "GetVendorTrackingInfo":
            {
                const string usage =
                    "Usage: local vendorTrackingInfo = C_ContentTracking.GetVendorTrackingInfo(collectableEntryID)";
                var id = RequiredInt32(state, 1, usage);
                if (!tracking.VendorInfo.TryGetValue(id, out var info))
                    return 0;
                PushVendorInfo(state, info);
                return 1;
            }
            case "GetWaypointText":
            {
                const string usage =
                    "Usage: local waypointText = C_ContentTracking.GetWaypointText(trackableType, trackableID)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                if (!tracking.WaypointTexts.TryGetValue((type, id), out var text))
                    return 0;
                lua_pushstring(state, text);
                return 1;
            }
            case "IsNavigable":
            {
                const string usage =
                    "Usage: local result, isNavigable = C_ContentTracking.IsNavigable(trackableType, trackableID)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                var result = tracking.Navigability.TryGetValue(
                    (type, id),
                    out var configured)
                    ? configured
                    : new WowContentTrackingNavigableResult(Failure, false);
                lua_pushinteger(state, result.Result);
                lua_pushboolean(state, result.IsNavigable ? 1 : 0);
                return 2;
            }
            case "IsTrackable":
            {
                const string usage =
                    "Usage: local isTrackable = C_ContentTracking.IsTrackable(type, id)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                lua_pushboolean(state, tracking.TrackableEntries.Contains((type, id)) ? 1 : 0);
                return 1;
            }
            case "IsTracking":
            {
                const string usage =
                    "Usage: local isTracking = C_ContentTracking.IsTracking(type, id)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                lua_pushboolean(state, tracking.TrackedEntries.Contains((type, id)) ? 1 : 0);
                return 1;
            }
            case "StartTracking":
            {
                const string usage =
                    "Usage: local error = C_ContentTracking.StartTracking(type, id)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                return StartTracking(runtime, state, type, id);
            }
            case "StopTracking":
            {
                const string usage =
                    "Usage: C_ContentTracking.StopTracking(type, id, stopType)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                var stopType = RequiredStopType(state, 3, usage);
                tracking.LastStopType = stopType;
                StopTracking(runtime, type, id);
                return 0;
            }
            case "ToggleTracking":
            {
                const string usage =
                    "Usage: local error = C_ContentTracking.ToggleTracking(type, id, stopType)";
                var type = RequiredContentTrackingType(state, 1, usage);
                var id = RequiredInt32(state, 2, usage);
                var stopType = RequiredStopType(state, 3, usage);
                if (tracking.TrackedEntries.Contains((type, id)))
                {
                    tracking.LastStopType = stopType;
                    StopTracking(runtime, type, id);
                    lua_pushnil(state);
                    return 1;
                }
                return StartTracking(runtime, state, type, id);
            }
            default:
                return 0;
        }
    }

    private static int StartTracking(
        LuaRuntime runtime,
        lua_State state,
        int type,
        int id)
    {
        var tracking = runtime.ContentTracking;
        if (tracking.TrackedEntries.Contains((type, id)))
        {
            lua_pushinteger(state, AlreadyTracked);
            return 1;
        }
        var capacity = type == 2 ? 10 : 15;
        if (tracking.TrackedEntries.Count(entry => entry.Type == type) >= capacity)
        {
            lua_pushinteger(state, CapacityExceeded);
            return 1;
        }
        if (!tracking.TrackableEntries.Contains((type, id)))
        {
            lua_pushinteger(state, Untrackable);
            return 1;
        }
        tracking.TrackedEntries.Add((type, id));
        runtime.TriggerEvent("CONTENT_TRACKING_UPDATE", type, id, true);
        lua_pushnil(state);
        return 1;
    }

    private static void StopTracking(
        LuaRuntime runtime,
        int type,
        int id)
    {
        var tracking = runtime.ContentTracking;
        if (tracking.TrackedEntries.Remove((type, id)))
            runtime.TriggerEvent("CONTENT_TRACKING_UPDATE", type, id, false);
    }

    private static void PushMapInfos(
        lua_State state,
        IReadOnlyList<WowContentTrackingMapInfo> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            PushMapInfo(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushMapInfo(
        lua_State state,
        WowContentTrackingMapInfo info)
    {
        lua_createtable(state, 0, 7);
        SetNumber(state, "x", info.X);
        SetNumber(state, "y", info.Y);
        SetInteger(state, "trackableType", info.TrackableType);
        SetInteger(state, "trackableID", info.TrackableId);
        SetInteger(state, "targetType", info.TargetType);
        SetInteger(state, "targetID", info.TargetId);
        SetString(state, "waypointText", info.WaypointText);
    }

    private static void PushEncounterInfo(
        lua_State state,
        WowContentTrackingEncounterInfo info)
    {
        lua_createtable(state, 0, 8);
        SetOptionalString(state, "encounterName", info.EncounterName);
        SetOptionalInteger(state, "journalEncounterID", info.JournalEncounterId);
        SetOptionalInteger(state, "journalInstanceID", info.JournalInstanceId);
        SetOptionalString(state, "instanceName", info.InstanceName);
        SetOptionalString(state, "subText", info.SubText);
        SetOptionalInteger(state, "difficultyID", info.DifficultyId);
        SetOptionalInteger(state, "lfgDungeonID", info.LfgDungeonId);
        SetOptionalInteger(
            state,
            "groupFinderActivityID",
            info.GroupFinderActivityId);
    }

    private static void PushVendorInfo(
        lua_State state,
        WowContentTrackingVendorInfo info)
    {
        lua_createtable(state, 0, 4);
        SetOptionalString(state, "creatureName", info.CreatureName);
        SetOptionalString(state, "zoneName", info.ZoneName);
        SetOptionalInteger(state, "currencyType", info.CurrencyType);
        if (info.Cost is { } cost)
        {
            lua_pushnumber(state, cost);
            lua_setfield(state, -2, "cost");
        }
    }

    private static void PushIntegers(lua_State state, IEnumerable<int> values)
    {
        var entries = values.ToArray();
        lua_createtable(state, entries.Length, 0);
        for (var index = 0; index < entries.Length; index++)
        {
            lua_pushinteger(state, entries[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOptionalInt32(lua_State state, int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string name,
        int? value)
    {
        if (value is not { } integer)
            return;
        lua_pushinteger(state, integer);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(
        lua_State state,
        string name,
        string? value)
    {
        if (value is null)
            return;
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static int RequiredContentTrackingType(
        lua_State state,
        int index,
        string usage) =>
        RequiredEnum(state, index, 3, usage);

    private static int RequiredTargetType(
        lua_State state,
        int index,
        string usage) =>
        RequiredEnum(state, index, 4, usage);

    private static int RequiredStopType(
        lua_State state,
        int index,
        string usage) =>
        RequiredEnum(state, index, 2, usage);

    private static int RequiredEnum(
        lua_State state,
        int index,
        int maximum,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        return unchecked((uint)value) <= maximum
            ? value
            : luaL_error(state, usage);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return false;
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }
}
