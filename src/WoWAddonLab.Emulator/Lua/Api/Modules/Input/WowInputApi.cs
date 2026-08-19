using LuaNET.Lua51;
using WoWAddonLab.Emulator.UI;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowInputApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetCursorPosition",
        "GetMouseFoci",
        "IsAltKeyDown",
        "IsControlKeyDown",
        "IsModifierKeyDown",
        "IsMouseButtonDown",
        "IsShiftKeyDown",
        "MakeModifiers"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        RegisterNamespace(state, "C_Input", Functions);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetCursorPosition":
                var nativeScale = runtime.Ui.NativeCoordinateUnitsPerLogicalUnit;
                lua_pushnumber(state, runtime.Ui.CursorPosition.X * nativeScale);
                lua_pushnumber(state, runtime.Ui.CursorPosition.Y * nativeScale);
                return 2;
            case "IsAltKeyDown":
                lua_pushboolean(state, runtime.Input.AltDown ? 1 : 0);
                return 1;
            case "IsControlKeyDown":
                lua_pushboolean(state, runtime.Input.ControlDown ? 1 : 0);
                return 1;
            case "IsModifierKeyDown":
                lua_pushboolean(
                    state,
                    runtime.Input.ShiftDown ||
                    runtime.Input.ControlDown ||
                    runtime.Input.AltDown
                        ? 1
                        : 0);
                return 1;
            case "IsMouseButtonDown":
                lua_pushboolean(
                    state,
                    IsMouseButtonDown(state, runtime.Input) ? 1 : 0);
                return 1;
            case "IsShiftKeyDown":
                lua_pushboolean(state, runtime.Input.ShiftDown ? 1 : 0);
                return 1;
            case "MakeModifiers":
            {
                var modifiers = 0;
                if (runtime.ShiftDown)
                    modifiers |= 0x03;
                if (runtime.ControlDown)
                    modifiers |= 0x0c;
                if (runtime.AltDown)
                    modifiers |= 0x30;
                lua_pushinteger(state, modifiers);
                return 1;
            }
            case "GetMouseFoci":
            default:
            {
                var foci = runtime.Ui.MouseFoci()
                    .Where(value => !value.Forbidden)
                    .ToArray();
                lua_createtable(state, foci.Length, 0);
                for (var index = 0; index < foci.Length; index++)
                {
                    runtime.PushObject(foci[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
        }
    }

    private static bool IsMouseButtonDown(lua_State state, WowInputState input)
    {
        var type = lua_type(state, 1);
        if (type is LUA_TNONE or LUA_TNIL)
            return input.MouseButtonsDown.Count != 0;

        string? button = null;
        if (type == LUA_TSTRING)
        {
            button = UiObject.NormalizeMouseButtonName(lua_tostring(state, 1));
        }
        else if (type == LUA_TNUMBER)
        {
            var value = lua_tonumber(state, 1);
            if (double.IsFinite(value) && value >= int.MinValue && value <= int.MaxValue)
                button = UiObject.NormalizeMouseButtonName($"Button{(int)value}");
        }

        return button is not null && input.MouseButtonsDown.Contains(button);
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
}
