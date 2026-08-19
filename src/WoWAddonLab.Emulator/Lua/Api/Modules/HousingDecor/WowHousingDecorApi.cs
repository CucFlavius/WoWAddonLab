using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowHousingDecorApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CancelActiveEditing", "CommitDecorMovement", "EnterPreviewState",
        "ExitPreviewState", "GetAllPlacedDecor", "GetDecorDebugInfoForGUID",
        "GetDecorHyperlink", "GetDecorIcon", "GetDecorInstanceInfoForGUID",
        "GetDecorName", "GetHoveredDecorDebugInfo", "GetHoveredDecorInfo",
        "GetMaxPlacementBudget", "GetNumDecorPlaced", "GetNumPreviewDecor",
        "GetSelectedDecorDebugInfo", "GetSelectedDecorInfo",
        "GetSpentPlacementBudget", "HasMaxPlacementBudget", "IsDecorSelected",
        "IsGridVisible", "IsHouseExteriorDoorHovered", "IsHouseExteriorHovered",
        "IsHoveringDecor", "IsModeDisabledForPreviewState", "IsPreviewState",
        "RemovePlacedDecorEntry", "RemoveSelectedDecor", "SetGridVisible",
        "SetPlacedDecorEntryHovered", "SetPlacedDecorEntrySelected"
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
        lua_setglobal(state, "C_HousingDecor");
    }

    internal static void RegisterEnums(lua_State state)
    {
        SetFlagsEnum(
            state,
            "HousingDecorActionFlags",
            [
                ("None", 0),
                ("Add", 1),
                ("Remove", 2),
                ("DragMove", 4),
                ("PrecisionMove", 8),
                ("ClickTarget", 16),
                ("HoverTarget", 32),
                ("TargetRoomComponents", 64),
                ("TargetHouseExterior", 128),
                ("MaintainLastTarget", 256),
                ("IncludeTargetChildren", 512),
                ("UsePlacedDecorList", 1024),
                ("PreviewDecor", 2048)
            ]);
        SetEnum(
            state,
            "LightRadiusIndicatorType",
            "Always",
            "Overlap",
            "Never");
    }

    private static int Dispatch(lua_State state)
    {
        var housing = LuaBindings.GetRuntime(state).Housing;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CancelActiveEditing":
            case "CommitDecorMovement":
                Record(housing, operation);
                return 0;
            case "EnterPreviewState":
                Record(housing, operation);
                housing.IsDecorPreviewState = true;
                return 0;
            case "ExitPreviewState":
                Record(housing, operation);
                housing.IsDecorPreviewState = false;
                return 0;
            case "SetGridVisible":
            {
                var visible = RequiredBoolean(
                    state,
                    1,
                    Usage(operation, "gridVisible"));
                Record(housing, operation, visible);
                housing.IsDecorGridVisible = visible;
                return 0;
            }
            case "GetAllPlacedDecor":
                return PushPlacedDecor(state, housing.PlacedDecor);
            case "GetDecorDebugInfoForGUID":
            {
                var guid = RequiredGuid(
                    state,
                    1,
                    Usage(operation, "decorGUID"));
                housing.DecorDebugInfoByGuid.TryGetValue(guid, out var info);
                return PushDebugInfoOrNil(state, info);
            }
            case "GetDecorHyperlink":
            {
                var decorId = RequiredInt32(
                    state,
                    1,
                    Usage(operation, "decorID"));
                return PushDictionaryString(
                    state,
                    housing.DecorHyperlinks,
                    decorId);
            }
            case "GetDecorIcon":
            {
                var decorId = RequiredInt32(
                    state,
                    1,
                    Usage(operation, "decorID"));
                if (!housing.DecorIcons.TryGetValue(decorId, out var icon))
                    return 0;
                lua_pushnumber(state, icon);
                return 1;
            }
            case "GetDecorInstanceInfoForGUID":
            {
                var guid = RequiredGuid(
                    state,
                    1,
                    Usage(operation, "decorGUID"));
                housing.DecorInfoByGuid.TryGetValue(guid, out var info);
                return PushInstanceInfoOrNil(state, info);
            }
            case "GetDecorName":
            {
                var decorId = RequiredInt32(
                    state,
                    1,
                    Usage(operation, "decorID"));
                return PushDictionaryString(state, housing.DecorNames, decorId);
            }
            case "GetHoveredDecorDebugInfo":
                return PushDebugInfoOrNil(
                    state,
                    housing.HoveredDecorDebugInfo);
            case "GetHoveredDecorInfo":
                return PushInstanceInfoOrNil(state, housing.HoveredDecorInfo);
            case "GetMaxPlacementBudget":
                return PushNumber(state, housing.MaxPlacementBudget);
            case "GetNumDecorPlaced":
                return PushNumber(state, housing.PlacedDecorCount);
            case "GetNumPreviewDecor":
                return PushNumber(state, housing.PreviewDecorCount);
            case "GetSelectedDecorDebugInfo":
                return PushDebugInfoOrNil(
                    state,
                    housing.SelectedDecorDebugInfo);
            case "GetSelectedDecorInfo":
                return PushInstanceInfoOrNil(state, housing.SelectedDecorInfo);
            case "GetSpentPlacementBudget":
                return PushNumber(state, housing.SpentPlacementBudget);
            case "HasMaxPlacementBudget":
                return PushBoolean(state, housing.HasMaxPlacementBudget);
            case "IsDecorSelected":
                return PushBoolean(state, housing.IsDecorSelected);
            case "IsGridVisible":
                return PushBoolean(state, housing.IsDecorGridVisible);
            case "IsHouseExteriorDoorHovered":
                return PushBoolean(
                    state,
                    housing.IsHouseExteriorDoorHovered);
            case "IsHouseExteriorHovered":
                return PushBoolean(state, housing.IsHouseExteriorHovered);
            case "IsHoveringDecor":
                return PushBoolean(state, housing.IsHoveringDecor);
            case "IsModeDisabledForPreviewState":
            {
                var mode = RequiredEditorMode(
                    state,
                    Usage(operation, "mode"));
                return PushBoolean(
                    state,
                    housing.IsDecorPreviewState && mode != 1);
            }
            case "IsPreviewState":
                return PushBoolean(state, housing.IsDecorPreviewState);
            case "RemovePlacedDecorEntry":
            {
                var guid = RequiredGuid(
                    state,
                    1,
                    Usage(operation, "decorGUID"));
                Record(housing, operation, guid);
                housing.PlacedDecor.RemoveAll(
                    entry => string.Equals(
                        entry.DecorGuid,
                        guid,
                        StringComparison.Ordinal));
                housing.HoveredPlacedDecorGuids.Remove(guid);
                housing.SelectedPlacedDecorGuids.Remove(guid);
                return 0;
            }
            case "RemoveSelectedDecor":
                Record(housing, operation);
                housing.SelectedDecorInfo = null;
                housing.SelectedDecorDebugInfo = null;
                housing.SelectedPlacedDecorGuids.Clear();
                housing.IsDecorSelected = false;
                return 0;
            case "SetPlacedDecorEntryHovered":
            case "SetPlacedDecorEntrySelected":
            {
                var guid = RequiredGuid(
                    state,
                    1,
                    Usage(
                        operation,
                        operation.EndsWith("Hovered", StringComparison.Ordinal)
                            ? "decorGUID, hovered"
                            : "decorGUID, selected"));
                var enabled = RequiredBoolean(
                    state,
                    2,
                    Usage(
                        operation,
                        operation.EndsWith("Hovered", StringComparison.Ordinal)
                            ? "decorGUID, hovered"
                            : "decorGUID, selected"));
                Record(housing, operation, guid, enabled);
                var set = operation.EndsWith("Hovered", StringComparison.Ordinal)
                    ? housing.HoveredPlacedDecorGuids
                    : housing.SelectedPlacedDecorGuids;
                if (enabled)
                    set.Add(guid);
                else
                    set.Remove(guid);
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int PushPlacedDecor(
        lua_State state,
        IReadOnlyList<WowHousingPlacedDecorState> entries)
    {
        lua_createtable(state, entries.Count, 0);
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            lua_createtable(state, 0, 2);
            SetString(state, "decorGUID", entry.DecorGuid);
            SetOptionalString(state, "name", entry.Name);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushInstanceInfoOrNil(
        lua_State state,
        WowHousingDecorInstanceInfoState? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return 1;
        }

        lua_createtable(state, 0, 12);
        SetString(state, "decorGUID", info.DecorGuid);
        SetNumber(state, "decorID", info.DecorId);
        SetOptionalString(state, "name", info.Name);
        SetBoolean(state, "isLocked", info.IsLocked);
        SetBoolean(state, "canBeCustomized", info.CanBeCustomized);
        SetBoolean(state, "canBeRemoved", info.CanBeRemoved);
        SetBoolean(state, "isAllowedOutdoors", info.IsAllowedOutdoors);
        SetBoolean(state, "isAllowedIndoors", info.IsAllowedIndoors);
        SetBoolean(state, "isRefundable", info.IsRefundable);
        PushDyeSlots(state, info.DyeSlots);
        lua_setfield(state, -2, "dyeSlots");
        PushDataTags(state, info.DataTagsById);
        lua_setfield(state, -2, "dataTagsByID");
        SetNumber(state, "size", info.Size);
        return 1;
    }

    private static int PushDebugInfoOrNil(
        lua_State state,
        WowHousingDecorDebugInfoState? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return 1;
        }

        lua_createtable(state, 0, 9);
        PushInstanceInfoOrNil(state, info.BaseInfo);
        lua_setfield(state, -2, "baseInfo");
        SetString(state, "assetName", info.AssetName);
        SetNumber(state, "fileDataID", info.FileDataId);
        PushVector3(state, info.WorldPosition);
        lua_setfield(state, -2, "worldPosition");
        PushVector3(state, info.RotationYawPitchRoll);
        lua_setfield(state, -2, "rotationYawPitchRoll");
        SetNumber(state, "scale", info.Scale);
        SetOptionalString(state, "roomGUID", info.RoomGuid);
        SetOptionalString(state, "parentGUID", info.ParentGuid);
        PushStringArray(state, info.ChildDecorGuids);
        lua_setfield(state, -2, "childDecorGUIDs");
        return 1;
    }

    private static void PushDyeSlots(
        lua_State state,
        IReadOnlyList<WowHousingDecorDyeSlotState> dyeSlots)
    {
        lua_createtable(state, dyeSlots.Count, 0);
        for (var index = 0; index < dyeSlots.Count; index++)
        {
            var dyeSlot = dyeSlots[index];
            lua_createtable(state, 0, 6);
            SetNumber(state, "ID", dyeSlot.Id);
            SetNumber(
                state,
                "dyeColorCategoryID",
                dyeSlot.DyeColorCategoryId);
            SetNumber(state, "orderIndex", dyeSlot.OrderIndex);
            SetNumber(state, "channel", dyeSlot.Channel);
            SetOptionalNumber(state, "dyeColorID", dyeSlot.DyeColorId);
            SetOptionalString(state, "dyeColorName", dyeSlot.DyeColorName);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushDataTags(
        lua_State state,
        IReadOnlyDictionary<int, object?> dataTags)
    {
        lua_createtable(state, 0, dataTags.Count);
        foreach (var (id, value) in dataTags)
        {
            lua_pushnumber(state, id);
            PushLuaValue(state, value);
            lua_settable(state, -3);
        }
    }

    private static void PushLuaValue(lua_State state, object? value)
    {
        switch (value)
        {
            case null:
                lua_pushnil(state);
                break;
            case bool boolean:
                lua_pushboolean(state, boolean ? 1 : 0);
                break;
            case string text:
                lua_pushstring(state, text);
                break;
            case byte number:
                lua_pushnumber(state, number);
                break;
            case sbyte number:
                lua_pushnumber(state, number);
                break;
            case short number:
                lua_pushnumber(state, number);
                break;
            case ushort number:
                lua_pushnumber(state, number);
                break;
            case int number:
                lua_pushnumber(state, number);
                break;
            case uint number:
                lua_pushnumber(state, number);
                break;
            case long number:
                lua_pushnumber(state, number);
                break;
            case ulong number:
                lua_pushnumber(state, number);
                break;
            case float number:
                lua_pushnumber(state, number);
                break;
            case double number:
                lua_pushnumber(state, number);
                break;
            default:
                lua_pushnil(state);
                break;
        }
    }

    private static void PushVector3(
        lua_State state,
        WowHousingVector3State vector)
    {
        lua_createtable(state, 0, 3);
        SetNumber(state, "x", vector.X);
        SetNumber(state, "y", vector.Y);
        SetNumber(state, "z", vector.Z);
    }

    private static void PushStringArray(
        lua_State state,
        IReadOnlyList<string> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushstring(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static int PushDictionaryString(
        lua_State state,
        IReadOnlyDictionary<int, string> values,
        int key)
    {
        if (!values.TryGetValue(key, out var value))
            return 0;
        lua_pushstring(state, value);
        return 1;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)number);
    }

    private static byte RequiredEditorMode(lua_State state, string usage)
    {
        const byte maximumMode = 6;
        var value = unchecked((byte)RequiredInt32(state, 1, usage));
        if (value > maximumMode)
            return unchecked((byte)luaL_error(state, usage));
        return value;
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) == LUA_TNONE)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static string RequiredGuid(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static void Record(
        WowHousingState housing,
        string operation,
        params object?[] arguments) =>
        housing.Requests.Add(new WowHousingRequestState(operation, arguments));

    private static string Usage(string operation, string arguments) =>
        $"Usage: C_HousingDecor.{operation}({arguments})";

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

    private static void SetBoolean(lua_State state, string key, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, key);
    }

    private static void SetFlagsEnum(
        lua_State state,
        string name,
        IReadOnlyList<(string Name, double Value)> members)
    {
        lua_createtable(state, 0, members.Count);
        foreach (var member in members)
            SetNumber(state, member.Name, member.Value);
        lua_setfield(state, -2, name);

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", members.Count);
        SetNumber(state, "MinValue", members.Min(member => member.Value));
        SetNumber(state, "MaxValue", members.Max(member => member.Value));
        lua_setfield(state, -2, $"{name}Meta");
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params string[] members)
    {
        lua_createtable(state, 0, members.Length);
        for (var value = 0; value < members.Length; value++)
            SetNumber(state, members[value], value);
        lua_setfield(state, -2, name);

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", members.Length);
        SetNumber(state, "MinValue", 0);
        SetNumber(state, "MaxValue", members.Length - 1);
        lua_setfield(state, -2, $"{name}Meta");
    }
}
