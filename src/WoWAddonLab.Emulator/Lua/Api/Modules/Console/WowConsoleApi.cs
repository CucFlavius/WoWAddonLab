using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowConsoleApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterEnums(state);
        foreach (var function in new[]
                 {
                     "ConsoleEcho",
                     "ConsoleExec",
                     "ConsoleGetAllCommands",
                     "ConsoleGetColorFromType",
                     "ConsoleGetFontHeight",
                     "ConsoleIsActive",
                     "ConsolePrintAllMatchingCommands",
                     "ConsoleSetFontHeight",
                     "SetConsoleKey"
                 })
        {
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var runtime = LuaBindings.GetRuntime(state);
        switch (operation)
        {
            case "ConsoleGetAllCommands":
                lua_newtable(state);
                return 1;
            case "ConsoleExec":
                if (lua_type(state, 1) != LUA_TSTRING)
                {
                    return luaL_error(
                        state,
                        "Usage: local result = ConsoleExec(command [, addToHistory])");
                }
                lua_pushboolean(state, 0);
                return 1;
            case "ConsoleGetColorFromType":
                if (lua_type(state, 1) != LUA_TNUMBER)
                {
                    return luaL_error(
                        state,
                        "Usage: local color = ConsoleGetColorFromType(colorType)");
                }
                return PushColor(state, (int)lua_tonumber(state, 1));
            case "ConsoleGetFontHeight":
                lua_pushnumber(state, 14);
                return 1;
            case "ConsoleIsActive":
                lua_pushboolean(state, 0);
                return 1;
            case "ConsoleEcho":
                if (lua_type(state, 1) != LUA_TSTRING)
                    return luaL_error(state, "Usage: ConsoleEcho(message)");
                runtime.Log.Info("console", lua_tostring(state, 1) ?? string.Empty);
                return 0;
            case "ConsolePrintAllMatchingCommands":
                if (lua_type(state, 1) != LUA_TSTRING)
                {
                    return luaL_error(
                        state,
                        "Usage: ConsolePrintAllMatchingCommands(partialCommandText)");
                }
                return 0;
            case "ConsoleSetFontHeight":
                if (lua_type(state, 1) != LUA_TNUMBER)
                {
                    return luaL_error(
                        state,
                        "Usage: ConsoleSetFontHeight(fontHeightInPixels)");
                }
                return 0;
            case "SetConsoleKey":
                if (lua_type(state, 1) != LUA_TSTRING)
                    return luaL_error(state, "Usage: SetConsoleKey(keystring)");
                return 0;
            default:
                return 0;
        }
    }

    private static int PushColor(lua_State state, int colorType)
    {
        var (red, green, blue) = colorType switch
        {
            3 => (1f, .2f, .2f),
            4 => (1f, .82f, 0f),
            11 => (.2f, 1f, .2f),
            _ => (1f, 1f, 1f)
        };
        lua_getglobal(state, "CreateColor");
        lua_pushnumber(state, red);
        lua_pushnumber(state, green);
        lua_pushnumber(state, blue);
        lua_pushnumber(state, 1);
        lua_call(state, 4, 1);
        return 1;
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        SetEnum(
            state,
            "ConsoleCategory",
            ("Debug", 0), ("Graphics", 1), ("Console", 2), ("Combat", 3),
            ("Game", 4), ("Default", 5), ("Net", 6), ("Sound", 7),
            ("Gm", 8), ("Reveal", 9), ("None", 10));
        SetEnum(
            state,
            "ConsoleColorType",
            ("DefaultColor", 0), ("InputColor", 1), ("EchoColor", 2),
            ("ErrorColor", 3), ("WarningColor", 4), ("GlobalColor", 5),
            ("AdminColor", 6), ("HighlightColor", 7), ("BackgroundColor", 8),
            ("ClickbufferColor", 9), ("PrivateColor", 10), ("DefaultGreen", 11));
        SetEnum(
            state,
            "ConsoleCommandType",
            ("Cvar", 0), ("Command", 1), ("Macro", 2), ("Script", 3));
        lua_setglobal(state, "Enum");
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] entries)
    {
        lua_newtable(state);
        foreach (var entry in entries)
        {
            lua_pushinteger(state, entry.Value);
            lua_setfield(state, -2, entry.Name);
        }
        lua_setfield(state, -2, name);
    }
}
