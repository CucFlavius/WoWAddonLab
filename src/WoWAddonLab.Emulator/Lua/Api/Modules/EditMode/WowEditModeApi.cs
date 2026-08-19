using System.Globalization;
using System.Text;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowEditModeApi : LuaApiModule
{
    private const int CurrentSerializationVersion = 2;
    private const int MinimumCodecCharacter = 35;
    private const int MaximumCodecCharacter = 127;
    private const int SettingValueRadix = 90;

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ConvertLayoutInfoToString",
        "ConvertStringToLayoutInfo",
        "GetAccountSettings",
        "GetLayouts",
        "IsValidLayoutName",
        "OnEditModeExit",
        "OnLayoutAdded",
        "OnLayoutDeleted",
        "SaveLayouts",
        "SetAccountSetting",
        "SetActiveLayout"
    ];

    private static readonly string[] FramePoints =
    [
        "TOPLEFT",
        "TOP",
        "TOPRIGHT",
        "LEFT",
        "CENTER",
        "RIGHT",
        "BOTTOMLEFT",
        "BOTTOM",
        "BOTTOMRIGHT"
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
        lua_setglobal(state, "C_EditMode");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var editMode = runtime.EditMode;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAccountSettings":
                PushAccountSettings(state, editMode);
                return 1;
            case "GetLayouts":
                PushLayouts(state, editMode);
                return 1;
            case "SaveLayouts":
                SaveLayouts(state, runtime, editMode);
                return 0;
            case "OnEditModeExit":
                editMode.ExitRequestCount++;
                editMode.Persist();
                return 0;
            case "OnLayoutAdded":
            {
                const string usage =
                    "Usage: C_EditMode.OnLayoutAdded(addedLayoutIndex, activateNewLayout, isLayoutImported)";
                var addedLayoutIndex = RequiredOneBasedIndex(state, 1, usage);
                var activate = RequiredBoolean(state, 2, usage);
                var imported = RequiredBoolean(state, 3, usage);
                editMode.AddedLayoutRequests.Add((addedLayoutIndex, activate, imported));
                if (addedLayoutIndex <= editMode.ActiveLayout)
                    editMode.ActiveLayout++;
                if (activate)
                    editMode.ActiveLayout = addedLayoutIndex;
                editMode.Persist();
                runtime.TriggerEditModeLayoutsUpdated();
                return 0;
            }
            case "OnLayoutDeleted":
            {
                const string usage =
                    "Usage: C_EditMode.OnLayoutDeleted(deletedLayoutIndex)";
                var deletedLayoutIndex = RequiredOneBasedIndex(state, 1, usage);
                editMode.DeletedLayoutRequests.Add(deletedLayoutIndex);
                if (deletedLayoutIndex == editMode.ActiveLayout)
                    editMode.ActiveLayout = 1;
                else if (deletedLayoutIndex < editMode.ActiveLayout)
                    editMode.ActiveLayout--;
                editMode.Persist();
                runtime.TriggerEditModeLayoutsUpdated();
                return 0;
            }
            case "SetAccountSetting":
            {
                const string usage =
                    "Usage: C_EditMode.SetAccountSetting(setting, value)";
                var setting = RequiredInt32(state, 1, usage);
                if (setting is < 0 or > 33)
                    return luaL_error(state, usage);
                var value = RequiredInt32(state, 2, usage);
                editMode.AccountSettingRequests.Add((setting, value));
                editMode.AccountSettings[setting] = value;
                editMode.Persist();
                return 0;
            }
            case "SetActiveLayout":
            {
                const string usage =
                    "Usage: C_EditMode.SetActiveLayout(activeLayout)";
                var activeLayout = RequiredOneBasedIndex(state, 1, usage);
                editMode.ActiveLayoutRequests.Add(activeLayout);
                editMode.ActiveLayout = activeLayout;
                editMode.Persist();
                runtime.TriggerEditModeLayoutsUpdated();
                return 0;
            }
            case "IsValidLayoutName":
                RequiredString(
                    state,
                    1,
                    "Usage: local isValid = C_EditMode.IsValidLayoutName(name)");
                lua_pushboolean(state, 1);
                return 1;
            case "ConvertLayoutInfoToString":
            {
                const string usage =
                    "Usage: local layoutInfoAsString = C_EditMode.ConvertLayoutInfoToString(layoutInfo)";
                var layout = ReadLayoutInfo(state, 1, usage);
                lua_pushstring(state, SerializeLayout(layout));
                return 1;
            }
            case "ConvertStringToLayoutInfo":
            {
                const string usage =
                    "Usage: local layoutInfo = C_EditMode.ConvertStringToLayoutInfo(layoutInfoAsString)";
                var serialized = RequiredString(state, 1, usage);
                if (!TryDeserializeLayout(serialized, out var layout))
                    return 0;
                PushLayoutInfo(state, layout);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static void PushAccountSettings(lua_State state, WowEditModeState editMode)
    {
        lua_createtable(state, editMode.AccountSettings.Count, 0);
        var index = 1;
        foreach (var (setting, value) in editMode.AccountSettings.OrderBy(entry => entry.Key))
        {
            lua_createtable(state, 0, 2);
            SetNumber(state, "setting", setting);
            SetNumber(state, "value", value);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void PushLayouts(lua_State state, WowEditModeState editMode)
    {
        lua_createtable(state, 0, 2);
        if (editMode.SavedLayoutsReference > 0)
            lua_rawgeti(state, LUA_REGISTRYINDEX, editMode.SavedLayoutsReference);
        else
        {
            lua_createtable(state, editMode.SavedLayouts.Count, 0);
            for (var index = 0; index < editMode.SavedLayouts.Count; index++)
            {
                PushLayoutInfo(state, editMode.SavedLayouts[index]);
                lua_rawseti(state, -2, index + 1);
            }
        }
        lua_setfield(state, -2, "layouts");
        SetNumber(state, "activeLayout", editMode.ActiveLayout);
    }

    private static void SaveLayouts(
        lua_State state,
        LuaRuntime runtime,
        WowEditModeState editMode)
    {
        const string usage = "Usage: C_EditMode.SaveLayouts(saveInfo)";
        var saveInfoIndex = RequiredTable(state, 1, usage);

        lua_getfield(state, saveInfoIndex, "layouts");
        var sourceIndex = RequiredTable(state, -1, usage);
        lua_newtable(state);
        var savedLayoutsIndex = lua_gettop(state);
        var savedLayouts = new List<WowEditModeLayoutInfo>();
        var savedIndex = 1;
        var count = (int)lua_objlen(state, sourceIndex);
        for (var sourceLayoutIndex = 1; sourceLayoutIndex <= count; sourceLayoutIndex++)
        {
            lua_rawgeti(state, sourceIndex, sourceLayoutIndex);
            var layoutIndex = RequiredTable(state, -1, usage);
            var layout = ReadLayoutInfo(state, layoutIndex, usage);
            if (layout.LayoutType != 0)
            {
                savedLayouts.Add(layout);
                lua_pushvalue(state, layoutIndex);
                lua_rawseti(state, savedLayoutsIndex, savedIndex++);
            }
            lua_pop(state, 1);
        }

        lua_getfield(state, saveInfoIndex, "activeLayout");
        editMode.ActiveLayout = RequiredOneBasedIndex(state, -1, usage);
        lua_pop(state, 1);

        var newReference = LuaRuntime.CaptureValue(state, savedLayoutsIndex);
        runtime.ReleaseReference(editMode.SavedLayoutsReference);
        editMode.SavedLayoutsReference = newReference;
        editMode.SavedLayouts.Clear();
        editMode.SavedLayouts.AddRange(savedLayouts);
        editMode.Persist();
    }

    private static WowEditModeLayoutInfo ReadLayoutInfo(
        lua_State state,
        int index,
        string usage)
    {
        var absolute = RequiredTable(state, index, usage);
        var layoutName = RequiredStringField(state, absolute, "layoutName", usage);
        var layoutType = RequiredInt32Field(state, absolute, "layoutType", usage);
        if (layoutType is < 0 or > 3)
            luaL_error(state, usage);

        lua_getfield(state, absolute, "systems");
        var systemsIndex = RequiredTable(state, -1, usage);
        var systemCount = (int)lua_objlen(state, systemsIndex);
        var systems = new List<WowEditModeSystemInfo>(systemCount);
        for (var systemIndex = 1; systemIndex <= systemCount; systemIndex++)
        {
            lua_rawgeti(state, systemsIndex, systemIndex);
            systems.Add(ReadSystemInfo(state, -1, usage));
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
        return new WowEditModeLayoutInfo(layoutName, layoutType, systems);
    }

    private static WowEditModeSystemInfo ReadSystemInfo(
        lua_State state,
        int index,
        string usage)
    {
        var absolute = RequiredTable(state, index, usage);
        var system = RequiredInt32Field(state, absolute, "system", usage);
        if (system is < 0 or > byte.MaxValue)
            luaL_error(state, usage);

        int? systemIndex;
        lua_getfield(state, absolute, "systemIndex");
        if (lua_type(state, -1) is LUA_TNONE or LUA_TNIL)
            systemIndex = null;
        else
            systemIndex = RequiredOneBasedIndex(state, -1, usage);
        lua_pop(state, 1);

        lua_getfield(state, absolute, "anchorInfo");
        var anchor = ReadAnchorInfo(state, -1, usage);
        lua_pop(state, 1);

        WowEditModeAnchorInfo? anchor2 = null;
        lua_getfield(state, absolute, "anchorInfo2");
        if (lua_type(state, -1) is not (LUA_TNONE or LUA_TNIL))
            anchor2 = ReadAnchorInfo(state, -1, usage);
        lua_pop(state, 1);

        lua_getfield(state, absolute, "settings");
        var settingsIndex = RequiredTable(state, -1, usage);
        var settingCount = (int)lua_objlen(state, settingsIndex);
        var settings = new List<WowEditModeSettingInfo>(settingCount);
        for (var settingIndex = 1; settingIndex <= settingCount; settingIndex++)
        {
            lua_rawgeti(state, settingsIndex, settingIndex);
            var settingTable = RequiredTable(state, -1, usage);
            settings.Add(
                new WowEditModeSettingInfo(
                    RequiredInt32Field(state, settingTable, "setting", usage),
                    RequiredInt32Field(state, settingTable, "value", usage)));
            lua_pop(state, 1);
        }
        lua_pop(state, 1);

        var isInDefaultPosition =
            RequiredBooleanField(state, absolute, "isInDefaultPosition", usage);
        return new WowEditModeSystemInfo(
            system,
            systemIndex,
            anchor,
            anchor2,
            settings,
            isInDefaultPosition);
    }

    private static WowEditModeAnchorInfo ReadAnchorInfo(
        lua_State state,
        int index,
        string usage)
    {
        var absolute = RequiredTable(state, index, usage);
        return new WowEditModeAnchorInfo(
            RequiredFramePointField(state, absolute, "point", usage),
            RequiredStringField(state, absolute, "relativeTo", usage),
            RequiredFramePointField(state, absolute, "relativePoint", usage),
            RequiredFloatField(state, absolute, "offsetX", usage),
            RequiredFloatField(state, absolute, "offsetY", usage));
    }

    private static string SerializeLayout(WowEditModeLayoutInfo layout)
    {
        var tokens = new List<string>
        {
            CurrentSerializationVersion.ToString(CultureInfo.InvariantCulture)
        };

        var systems = layout.Systems
            .OrderBy(system => system.System)
            .ThenBy(system => system.SystemIndex ?? 0)
            .ToArray();
        tokens.Add(systems.Length.ToString(CultureInfo.InvariantCulture));
        foreach (var system in systems)
        {
            tokens.Add(system.System.ToString(CultureInfo.InvariantCulture));
            tokens.Add((system.SystemIndex ?? -1).ToString(CultureInfo.InvariantCulture));
            tokens.Add(system.IsInDefaultPosition ? "1" : "0");
            AppendAnchorTokens(tokens, system.AnchorInfo);
            if (system.AnchorInfo2 is { } secondAnchor)
                AppendAnchorTokens(tokens, secondAnchor);
            else
                tokens.Add("-1");
            tokens.Add(EncodeSettings(system.Settings));
        }
        return string.Join(' ', tokens);
    }

    private static void AppendAnchorTokens(
        ICollection<string> tokens,
        WowEditModeAnchorInfo anchor)
    {
        tokens.Add(anchor.Point.ToString(CultureInfo.InvariantCulture));
        tokens.Add(anchor.RelativePoint.ToString(CultureInfo.InvariantCulture));
        tokens.Add(anchor.RelativeTo);
        tokens.Add(anchor.OffsetX.ToString("F1", CultureInfo.InvariantCulture));
        tokens.Add(anchor.OffsetY.ToString("F1", CultureInfo.InvariantCulture));
    }

    private static string EncodeSettings(IReadOnlyList<WowEditModeSettingInfo> settings)
    {
        if (settings.Count == 0)
            return ((char)MinimumCodecCharacter).ToString();

        var encoded = new StringBuilder();
        foreach (var setting in settings)
        {
            if (setting.Setting is < 0 or > MaximumCodecCharacter - MinimumCodecCharacter ||
                setting.Value < 0)
                return ((char)MinimumCodecCharacter).ToString();

            var value = setting.Value;
            do
            {
                encoded.Append((char)(MinimumCodecCharacter + setting.Setting));
                encoded.Append(
                    (char)(MinimumCodecCharacter + (value % SettingValueRadix)));
                value /= SettingValueRadix;
            }
            while (value > 0);
        }
        return encoded.ToString();
    }

    private static bool TryDeserializeLayout(
        string serialized,
        out WowEditModeLayoutInfo layout)
    {
        layout = default!;
        var tokens = serialized.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cursor = 0;
        if (!TryReadInt(tokens, ref cursor, out var version) ||
            version is < 0 or > CurrentSerializationVersion ||
            !TryReadInt(tokens, ref cursor, out var systemCount) ||
            systemCount <= 0)
            return false;

        var systems = new List<WowEditModeSystemInfo>(systemCount);
        for (var index = 0; index < systemCount; index++)
        {
            if (!TryReadInt(tokens, ref cursor, out var system) ||
                system is < 0 or > byte.MaxValue ||
                !TryReadInt(tokens, ref cursor, out var serializedSystemIndex) ||
                serializedSystemIndex < -1 ||
                !TryReadInt(tokens, ref cursor, out var defaultPositionValue) ||
                !TryReadAnchor(tokens, ref cursor, out var anchor))
                return false;

            WowEditModeAnchorInfo? anchor2 = null;
            if (!TryPeekInt(tokens, cursor, out var secondAnchorPoint))
                return false;
            if (secondAnchorPoint == -1)
            {
                cursor++;
            }
            else if (!TryReadAnchor(tokens, ref cursor, out anchor2))
            {
                return false;
            }

            if (cursor >= tokens.Length ||
                !TryDecodeSettings(tokens[cursor++], out var settings))
                return false;

            systems.Add(
                new WowEditModeSystemInfo(
                    system,
                    serializedSystemIndex == -1 ? null : serializedSystemIndex,
                    anchor,
                    anchor2,
                    settings,
                    defaultPositionValue != 0));
        }

        if (cursor != tokens.Length)
            return false;

        layout = new WowEditModeLayoutInfo(string.Empty, 0, systems);
        return true;
    }

    private static bool TryReadAnchor(
        IReadOnlyList<string> tokens,
        ref int cursor,
        out WowEditModeAnchorInfo anchor)
    {
        anchor = default!;
        if (!TryReadInt(tokens, ref cursor, out var point) ||
            point is < 0 or >= 9 ||
            !TryReadInt(tokens, ref cursor, out var relativePoint) ||
            relativePoint is < 0 or >= 9 ||
            cursor >= tokens.Count)
            return false;

        var relativeTo = tokens[cursor++];
        if (!TryReadFloat(tokens, ref cursor, out var offsetX) ||
            !TryReadFloat(tokens, ref cursor, out var offsetY))
            return false;
        anchor = new WowEditModeAnchorInfo(point, relativeTo, relativePoint, offsetX, offsetY);
        return true;
    }

    private static bool TryDecodeSettings(
        string encoded,
        out IReadOnlyList<WowEditModeSettingInfo> settings)
    {
        settings = [];
        var digits = new int[encoded.Length];
        for (var index = 0; index < encoded.Length; index++)
        {
            var digit = encoded[index] - MinimumCodecCharacter;
            if (digit < 0 || encoded[index] > MaximumCodecCharacter)
                return false;
            digits[index] = digit;
        }

        if (digits.Length == 1)
            return digits[0] == 0;
        if ((digits.Length & 1) != 0)
            return false;

        var decoded = new List<WowEditModeSettingInfo>();
        for (var index = 0; index < digits.Length; index += 2)
        {
            var setting = digits[index];
            var value = digits[index + 1];
            var multiplier = SettingValueRadix;
            while (index + 2 < digits.Length && digits[index + 2] == setting)
            {
                index += 2;
                value += digits[index + 1] * multiplier;
                if (multiplier > int.MaxValue / SettingValueRadix)
                    return false;
                multiplier *= SettingValueRadix;
            }
            decoded.Add(new WowEditModeSettingInfo(setting, value));
        }
        settings = decoded;
        return true;
    }

    private static void PushLayoutInfo(lua_State state, WowEditModeLayoutInfo layout)
    {
        lua_createtable(state, 0, 3);
        SetString(state, "layoutName", layout.LayoutName);
        SetNumber(state, "layoutType", layout.LayoutType);
        lua_createtable(state, layout.Systems.Count, 0);
        for (var index = 0; index < layout.Systems.Count; index++)
        {
            PushSystemInfo(state, layout.Systems[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "systems");
    }

    private static void PushSystemInfo(lua_State state, WowEditModeSystemInfo system)
    {
        lua_createtable(state, 0, 6);
        SetNumber(state, "system", system.System);
        if (system.SystemIndex is { } systemIndex)
            SetNumber(state, "systemIndex", systemIndex);
        PushAnchorInfo(state, system.AnchorInfo);
        lua_setfield(state, -2, "anchorInfo");
        if (system.AnchorInfo2 is { } anchor2)
        {
            PushAnchorInfo(state, anchor2);
            lua_setfield(state, -2, "anchorInfo2");
        }
        lua_createtable(state, system.Settings.Count, 0);
        for (var index = 0; index < system.Settings.Count; index++)
        {
            lua_createtable(state, 0, 2);
            SetNumber(state, "setting", system.Settings[index].Setting);
            SetNumber(state, "value", system.Settings[index].Value);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "settings");
        SetBoolean(state, "isInDefaultPosition", system.IsInDefaultPosition);
    }

    private static void PushAnchorInfo(lua_State state, WowEditModeAnchorInfo anchor)
    {
        lua_createtable(state, 0, 5);
        SetString(state, "point", FramePoints[anchor.Point]);
        SetString(state, "relativeTo", anchor.RelativeTo);
        SetString(state, "relativePoint", FramePoints[anchor.RelativePoint]);
        SetNumber(state, "offsetX", anchor.OffsetX);
        SetNumber(state, "offsetY", anchor.OffsetY);
    }

    private static bool TryReadInt(
        IReadOnlyList<string> tokens,
        ref int cursor,
        out int value)
    {
        value = 0;
        return cursor < tokens.Count &&
               int.TryParse(
                   tokens[cursor++],
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out value);
    }

    private static bool TryPeekInt(
        IReadOnlyList<string> tokens,
        int cursor,
        out int value)
    {
        value = 0;
        return cursor < tokens.Count &&
               int.TryParse(
                   tokens[cursor],
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out value);
    }

    private static bool TryReadFloat(
        IReadOnlyList<string> tokens,
        ref int cursor,
        out float value)
    {
        value = 0;
        return cursor < tokens.Count &&
               float.TryParse(
                   tokens[cursor++],
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out value) &&
               float.IsFinite(value);
    }

    private static int RequiredTable(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return 0;
        }
        return AbsoluteIndex(state, index);
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return (int)value;
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value <= 0)
            return luaL_error(state, usage);
        return value;
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static string RequiredStringField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredString(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static int RequiredInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static float RequiredFloatField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_isnumber(state, -1) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, -1);
        if (!double.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        lua_pop(state, 1);
        return (float)value;
    }

    private static bool RequiredBooleanField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredBoolean(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static int RequiredFramePointField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        var point = RequiredStringField(state, tableIndex, field, usage);
        for (var index = 0; index < FramePoints.Length; index++)
        {
            if (string.Equals(point, FramePoints[index], StringComparison.OrdinalIgnoreCase))
                return index;
        }
        luaL_error(state, usage);
        return 0;
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

}
