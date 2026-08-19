using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowHousingApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AcceptNeighborhoodOwnership",
        "CanEditCharter",
        "CanTakeReportScreenshot",
        "CreateGuildNeighborhood",
        "CreateNeighborhoodCharter",
        "DeclineNeighborhoodOwnership",
        "DoesFactionMatchNeighborhood",
        "EditNeighborhoodCharter",
        "GetCurrentHouseInfo",
        "GetCurrentHouseLevelFavor",
        "GetCurrentHouseRefundAmount",
        "GetCurrentNeighborhoodGUID",
        "GetHouseLevelFavorForLevel",
        "GetHouseLevelRewardsForLevel",
        "GetHousingAccessFlags",
        "GetMaxHouseLevel",
        "GetNeighborhoodTextureSuffix",
        "GetOthersOwnedHouses",
        "GetPlayerOwnedHouses",
        "GetTrackedHouseGuid",
        "GetUIMapIDForNeighborhood",
        "GetVisitCooldownInfo",
        "HasHousingExpansionAccess",
        "HouseFinderDeclineNeighborhoodInvitation",
        "HouseFinderRequestNeighborhoods",
        "HouseFinderRequestReservationAndPort",
        "IsHousingMarketCartFullRemoveEnabled",
        "IsHousingMarketEnabled",
        "IsHousingMarketShopEnabled",
        "IsHousingServiceEnabled",
        "IsInsideHouse",
        "IsInsideHouseOrPlot",
        "IsInsideOwnHouse",
        "IsInsidePlot",
        "IsOnNeighborhoodMap",
        "LeaveHouse",
        "OnCharterConfirmationAccepted",
        "OnCharterConfirmationClosed",
        "OnCreateCharterNeighborhoodClosed",
        "OnCreateGuildNeighborhoodClosed",
        "OnHouseFinderClickPlot",
        "OnRequestSignatureClicked",
        "OnSignCharterClicked",
        "RelinquishHouse",
        "RequestCurrentHouseInfo",
        "RequestHouseFinderNeighborhoodData",
        "RequestPlayerCharacterList",
        "ReturnAfterVisitingHouse",
        "SaveHouseSettings",
        "SearchBNetFriendNeighborhoods",
        "SearchBNetFriendNeighborhoodsByID",
        "SetTrackedHouseGuid",
        "StartTutorial",
        "TeleportHome",
        "TryRenameNeighborhood",
        "ValidateCreateGuildNeighborhoodSize",
        "ValidateNeighborhoodName",
        "VisitHouse"
    ];

    public override void Register(lua_State state)
    {
        lua_createtable(state, 0, Functions.Length);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Housing");
    }

    internal static void RegisterEnums(lua_State state)
    {
        SetEnum(
            state,
            "CreateNeighborhoodErrorType",
            "None",
            "Profanity",
            "UndersizedGuild",
            "OversizedGuild");
        SetEnum(
            state,
            "HousingItemToastType",
            "Room",
            "Fixture",
            "Customization",
            "Decor",
            "HouseType");
    }

    private static int Dispatch(lua_State state)
    {
        var housing = LuaBindings.GetRuntime(state).Housing;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CanEditCharter":
                return PushBoolean(state, housing.CanEditCharter);
            case "HasHousingExpansionAccess":
                return PushBoolean(state, housing.HasHousingExpansionAccess);
            case "IsHousingMarketCartFullRemoveEnabled":
                return PushBoolean(state, housing.IsHousingMarketCartFullRemoveEnabled);
            case "IsHousingMarketEnabled":
                return PushBoolean(state, housing.IsHousingMarketEnabled);
            case "IsHousingMarketShopEnabled":
                return PushBoolean(state, housing.IsHousingMarketShopEnabled);
            case "IsHousingServiceEnabled":
                return PushBoolean(state, housing.IsHousingServiceEnabled);
            case "IsInsideHouse":
                return PushBoolean(state, housing.IsInsideHouse);
            case "IsInsideHouseOrPlot":
                return PushBoolean(state, housing.IsInsideHouseOrPlot);
            case "IsInsideOwnHouse":
                return PushBoolean(state, housing.IsInsideOwnHouse);
            case "IsInsidePlot":
                return PushBoolean(state, housing.IsInsidePlot);
            case "IsOnNeighborhoodMap":
                return PushBoolean(state, housing.IsOnNeighborhoodMap);
            case "GetCurrentHouseRefundAmount":
                return PushNumber(state, housing.CurrentHouseRefundAmount);
            case "GetHousingAccessFlags":
                return PushNumber(state, housing.HousingAccessFlags);
            case "GetMaxHouseLevel":
                return PushNumber(state, housing.MaxHouseLevel);
            case "GetCurrentNeighborhoodGUID":
                return PushOptionalString(state, housing.CurrentNeighborhoodGuid);
            case "GetTrackedHouseGuid":
                return PushOptionalString(state, housing.TrackedHouseGuid);
            case "GetHouseLevelFavorForLevel":
            {
                var level = RequiredInt32(state, 1, Usage(operation, "houseLevel"));
                housing.HouseFavorByLevel.TryGetValue(level, out var favor);
                return PushNumber(state, favor);
            }
            case "CanTakeReportScreenshot":
            {
                var plotIndex = RequiredUInt8(state, 1, Usage(operation, "plotIndex"));
                housing.ReportScreenshotReasonByPlotIndex.TryGetValue(plotIndex, out var reason);
                return PushNumber(state, reason);
            }
            case "DoesFactionMatchNeighborhood":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "neighborhoodGUID"));
                return PushBoolean(
                    state,
                    housing.FactionMatchesNeighborhoodByGuid.GetValueOrDefault(guid));
            }
            case "GetNeighborhoodTextureSuffix":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "neighborhoodGUID"));
                housing.NeighborhoodTextureSuffixByGuid.TryGetValue(guid, out var suffix);
                return PushOptionalString(state, suffix);
            }
            case "GetUIMapIDForNeighborhood":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "neighborhoodGUID"));
                housing.UiMapIdByNeighborhoodGuid.TryGetValue(guid, out var mapId);
                if (mapId is { } value)
                    return PushNumber(state, value);
                lua_pushnil(state);
                return 1;
            }
            case "GetCurrentHouseInfo":
                return PushHouseInfo(state, housing.CurrentHouseInfo);
            case "GetVisitCooldownInfo":
                return PushCooldownInfo(state, housing.VisitCooldownInfo);
            case "SetTrackedHouseGuid":
            {
                var guid = OptionalGuid(state, 1, Usage(operation, "[houseGUID]"));
                if (!string.Equals(housing.TrackedHouseGuid, guid, StringComparison.Ordinal))
                    housing.TrackedHouseGuid = guid;
                return 0;
            }
            case "SearchBNetFriendNeighborhoods":
            {
                var name = RequiredString(state, 1, Usage(operation, "bnetName"));
                Record(housing, operation, name);
                return PushBoolean(
                    state,
                    housing.BNetFriendSearchResultByName.GetValueOrDefault(name));
            }
            case "SearchBNetFriendNeighborhoodsByID":
            {
                var id = RequiredInt32(state, 1, Usage(operation, "bnetID"));
                Record(housing, operation, id);
                return PushBoolean(
                    state,
                    housing.BNetFriendSearchResultById.GetValueOrDefault(id));
            }
            case "CreateGuildNeighborhood":
            case "CreateNeighborhoodCharter":
            case "EditNeighborhoodCharter":
            case "TryRenameNeighborhood":
            case "ValidateNeighborhoodName":
                Record(housing, operation, RequiredString(state, 1, Usage(operation, "name")));
                return 0;
            case "GetCurrentHouseLevelFavor":
            case "OnSignCharterClicked":
            case "RelinquishHouse":
                Record(housing, operation, RequiredGuid(state, 1, Usage(operation, "houseGUID")));
                return 0;
            case "HouseFinderRequestReservationAndPort":
                Record(
                    housing,
                    operation,
                    RequiredGuid(state, 1, Usage(operation, "neighborhoodGUID, plotID")),
                    RequiredUInt8(state, 2, Usage(operation, "neighborhoodGUID, plotID")));
                return 0;
            case "OnHouseFinderClickPlot":
                Record(
                    housing,
                    operation,
                    RequiredInt32(state, 1, Usage(operation, "plotID")));
                return 0;
            case "RequestHouseFinderNeighborhoodData":
                Record(
                    housing,
                    operation,
                    RequiredGuid(state, 1, Usage(operation, "neighborhoodGUID, name")),
                    RequiredString(state, 2, Usage(operation, "neighborhoodGUID, name")));
                return 0;
            case "SaveHouseSettings":
                Record(
                    housing,
                    operation,
                    RequiredGuid(state, 1, Usage(operation, "playerGUID, accessFlags")),
                    RequiredUInt32(state, 2, Usage(operation, "playerGUID, accessFlags")));
                return 0;
            case "TeleportHome":
            case "VisitHouse":
            {
                var usage = Usage(operation, "neighborhoodGUID, houseGUID, plotID");
                Record(
                    housing,
                    operation,
                    RequiredGuid(state, 1, usage),
                    RequiredGuid(state, 2, usage),
                    RequiredInt32(state, 3, usage));
                return 0;
            }
            case "GetOthersOwnedHouses":
            {
                var playerGuid = OptionalGuid(
                    state,
                    1,
                    Usage(operation, "[playerGUID, bnetID], isInPlayersGuild"));
                int? bnetId = null;
                if (lua_type(state, 2) is not (LUA_TNONE or LUA_TNIL))
                    bnetId = RequiredInt32(
                        state,
                        2,
                        Usage(operation, "[playerGUID, bnetID], isInPlayersGuild"));
                RequirePresent(state, 3, Usage(operation, "[playerGUID, bnetID], isInPlayersGuild"));
                Record(housing, operation, playerGuid, bnetId, lua_toboolean(state, 3) != 0);
                return 0;
            }
            case "GetHouseLevelRewardsForLevel":
                Record(
                    housing,
                    operation,
                    RequiredInt32(state, 1, Usage(operation, "houseLevel")));
                return 0;
        }

        Record(housing, operation);
        return 0;
    }

    private static int PushHouseInfo(
        lua_State state,
        WowHousingHouseInfoState? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return 1;
        }

        lua_createtable(state, 0, 9);
        SetNumber(state, "plotID", info.PlotId);
        SetString(state, "houseName", info.HouseName);
        SetString(state, "ownerName", info.OwnerName);
        SetOptionalNumber(state, "plotCost", info.PlotCost);
        SetString(state, "neighborhoodName", info.NeighborhoodName);
        SetOptionalNumber(state, "moveOutTime", info.MoveOutTime);
        SetOptionalBoolean(state, "plotReserved", info.PlotReserved);
        SetOptionalString(state, "neighborhoodGUID", info.NeighborhoodGuid);
        SetOptionalString(state, "houseGUID", info.HouseGuid);
        return 1;
    }

    private static int PushCooldownInfo(
        lua_State state,
        WowHousingActionCooldownInfoState? info)
    {
        if (info is null)
            return 0;

        lua_createtable(state, 0, 8);
        SetNumber(state, "startTime", info.StartTime);
        SetNumber(state, "duration", info.Duration);
        SetBoolean(state, "isEnabled", info.IsEnabled);
        SetBoolean(state, "isActive", info.IsActive);
        SetNumber(state, "modRate", info.ModRate);
        SetOptionalNumber(state, "activeCategory", info.ActiveCategory);
        SetOptionalNumber(
            state,
            "timeUntilEndOfStartRecovery",
            info.TimeUntilEndOfStartRecovery);
        SetBoolean(state, "isOnGCD", info.IsOnGcd);
        return 1;
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static uint RequiredUInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((uint)luaL_error(state, usage));
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            return unchecked((uint)luaL_error(state, usage));
        return unchecked((uint)value);
    }

    private static byte RequiredUInt8(lua_State state, int index, string usage)
    {
        var value = RequiredUInt32(state, index, usage);
        if (value > byte.MaxValue)
            return unchecked((byte)luaL_error(state, usage));
        return (byte)value;
    }

    private static string RequiredString(lua_State state, int index, string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string RequiredGuid(lua_State state, int index, string usage) =>
        RequiredString(state, index, usage);

    private static string? OptionalGuid(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredGuid(state, index, usage);
    }

    private static void RequirePresent(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) == LUA_TNONE)
            luaL_error(state, usage);
    }

    private static void Record(
        WowHousingState housing,
        string operation,
        params object?[] arguments) =>
        housing.Requests.Add(new WowHousingRequestState(operation, arguments));

    private static string Usage(string operation, string arguments) =>
        $"Usage: C_Housing.{operation}({arguments})";

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushNumber(lua_State state, double value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static int PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        return 1;
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetString(lua_State state, string key, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetBoolean(lua_State state, string key, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string key,
        double? value)
    {
        if (value is { } number)
            lua_pushnumber(state, number);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string key,
        bool? value)
    {
        if (value is { } boolean)
            lua_pushboolean(state, boolean ? 1 : 0);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalString(
        lua_State state,
        string key,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params string[] memberNames)
    {
        lua_createtable(state, 0, memberNames.Length);
        for (var value = 0; value < memberNames.Length; value++)
            SetNumber(state, memberNames[value], value);
        lua_setfield(state, -2, name);

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", memberNames.Length);
        SetNumber(state, "MinValue", 0);
        SetNumber(state, "MaxValue", memberNames.Length - 1);
        lua_setfield(state, -2, $"{name}Meta");
    }
}
