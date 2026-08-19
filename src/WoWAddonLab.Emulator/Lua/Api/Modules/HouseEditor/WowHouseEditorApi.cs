using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowHouseEditorApi : LuaApiModule
{
    private const byte NoActiveMode = 0;
    private const byte NotInsideHouseResult = 62;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] EditorFunctions =
    [
        "ActivateHouseEditorMode",
        "EnterHouseEditor",
        "GetActiveHouseEditorMode",
        "GetHouseEditorAvailability",
        "GetHouseEditorModeAvailability",
        "IsHouseEditorActive",
        "IsHouseEditorModeActive",
        "IsHouseEditorStatusAvailable",
        "LeaveHouseEditor"
    ];

    private static readonly string[] InspectFunctions =
    [
        "EnterInspectMode",
        "ExitInspectMode",
        "GetHoveredDecorGUID",
        "IsHoveringDecor",
        "IsInInspectMode"
    ];

    public override void Register(lua_State state)
    {
        RegisterNamespace(state, "C_HouseEditor", EditorFunctions);
        RegisterNamespace(state, "C_HousingInspectMode", InspectFunctions);
    }

    internal static void RegisterEnums(lua_State state)
    {
        SetEnum(
            state,
            "HouseEditorMode",
            "None",
            "BasicDecor",
            "ExpertDecor",
            "Layout",
            "Customize",
            "Cleanup",
            "ExteriorCustomization");
    }

    private static int Dispatch(lua_State state)
    {
        var housing = LuaBindings.GetRuntime(state).Housing;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ActivateHouseEditorMode":
            {
                var mode = RequiredEditorMode(state, operation);
                Record(housing, operation, mode);
                return PushEnum(state, housing.ActivateHouseEditorModeResult);
            }
            case "EnterHouseEditor":
                Record(housing, operation);
                return PushEnum(state, housing.EnterHouseEditorResult);
            case "GetActiveHouseEditorMode":
                return PushEnum(
                    state,
                    housing.IsHouseEditorActive
                        ? housing.ActiveHouseEditorMode
                        : NoActiveMode);
            case "GetHouseEditorAvailability":
                return PushEnum(state, housing.HouseEditorAvailability);
            case "GetHouseEditorModeAvailability":
            {
                var mode = RequiredEditorMode(state, operation);
                return PushEnum(
                    state,
                    housing.HouseEditorModeAvailability.GetValueOrDefault(
                        mode,
                        NotInsideHouseResult));
            }
            case "IsHouseEditorActive":
                return PushBoolean(state, housing.IsHouseEditorActive);
            case "IsHouseEditorModeActive":
                return PushBoolean(
                    state,
                    housing.ActiveHouseEditorModes.Contains(
                        RequiredEditorMode(state, operation)));
            case "IsHouseEditorStatusAvailable":
                return PushBoolean(state, housing.IsHouseEditorStatusAvailable);
            case "LeaveHouseEditor":
                Record(housing, operation);
                housing.IsHouseEditorActive = false;
                housing.ActiveHouseEditorMode = NoActiveMode;
                housing.ActiveHouseEditorModes.Clear();
                return 0;
            case "EnterInspectMode":
                Record(housing, operation);
                housing.IsInHousingInspectMode = true;
                return 0;
            case "ExitInspectMode":
                Record(housing, operation);
                housing.IsInHousingInspectMode = false;
                housing.HoveredDecorGuid = null;
                return 0;
            case "GetHoveredDecorGUID":
                if (housing.HoveredDecorGuid is { } guid)
                    lua_pushstring(state, guid);
                else
                    lua_pushnil(state);
                return 1;
            case "IsHoveringDecor":
                return PushBoolean(state, housing.HoveredDecorGuid is not null);
            case "IsInInspectMode":
                return PushBoolean(state, housing.IsInHousingInspectMode);
            default:
                return 0;
        }
    }

    private static byte RequiredEditorMode(lua_State state, string operation)
    {
        const byte maximumMode = 6;
        if (lua_isnumber(state, 1) == 0)
            return unchecked((byte)luaL_error(state, Usage(operation)));
        var number = lua_tonumber(state, 1);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
            return unchecked((byte)luaL_error(state, Usage(operation)));
        var value = unchecked((byte)(int)number);
        if (value > maximumMode)
            return unchecked((byte)luaL_error(state, Usage(operation)));
        return value;
    }

    private static string Usage(string operation) =>
        $"Usage: C_HouseEditor.{operation}(editMode)";

    private static void Record(
        WowHousingState housing,
        string operation,
        params object?[] arguments) =>
        housing.Requests.Add(new WowHousingRequestState(operation, arguments));

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushEnum(lua_State state, byte value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static void RegisterNamespace(
        lua_State state,
        string namespaceName,
        IEnumerable<string> functions)
    {
        lua_newtable(state);
        foreach (var function in functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, namespaceName);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params string[] memberNames)
    {
        lua_createtable(state, 0, memberNames.Length);
        for (var value = 0; value < memberNames.Length; value++)
        {
            lua_pushnumber(state, value);
            lua_setfield(state, -2, memberNames[value]);
        }
        lua_setfield(state, -2, name);

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", memberNames.Length);
        SetNumber(state, "MinValue", 0);
        SetNumber(state, "MaxValue", memberNames.Length - 1);
        lua_setfield(state, -2, $"{name}Meta");
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }
}
