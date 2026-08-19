using LuaNET.Lua51;
using WoWAddonLab.Emulator.UI;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowColorApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly IReadOnlyDictionary<string, string> ClientGlobalAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MANAPREDICTIONBLUE"] = "POWERBAR_PREDICTION_COLOR_MANA"
        };

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushstring(state, "GetColors");
        lua_pushcclosure(state, Callback, 0);
        lua_settable(state, -3);
        lua_setglobal(state, "C_UIColor");
    }

    public static void ApplyClientGlobals(LuaRuntime runtime)
    {
        var colors = runtime.GlobalColorProvider?.Colors;
        if (colors is null)
            return;

        foreach (var (globalName, colorName) in ClientGlobalAliases)
        {
            WowGlobalColor? match = null;
            foreach (var color in colors)
            {
                if (color.BaseTag.Equals(colorName, StringComparison.Ordinal))
                {
                    match = color;
                    break;
                }
            }

            if (match is not { } value)
                continue;

            lua_getglobal(runtime.State, "CreateColor");
            if (lua_isfunction(runtime.State, -1) == 0)
            {
                lua_pop(runtime.State, 1);
                continue;
            }
            lua_pushnumber(runtime.State, value.Red);
            lua_pushnumber(runtime.State, value.Green);
            lua_pushnumber(runtime.State, value.Blue);
            lua_pushnumber(runtime.State, value.Alpha);
            if (lua_pcall(runtime.State, 4, 1, 0) != 0)
            {
                lua_pop(runtime.State, 1);
                continue;
            }
            lua_setglobal(runtime.State, globalName);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var colors = LuaBindings.GetRuntime(state).GlobalColorProvider?.Colors ??
                     Array.Empty<WowGlobalColor>();
        lua_newtable(state);
        for (var index = 0; index < colors.Count; index++)
        {
            var color = colors[index];
            lua_newtable(state);
            lua_pushstring(state, color.BaseTag);
            lua_setfield(state, -2, "baseTag");

            PushColor(state, color);
            lua_setfield(state, -2, "color");

            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushColor(lua_State state, WowGlobalColor color)
    {
        lua_getglobal(state, "CreateColor");
        if (lua_isfunction(state, -1) != 0)
        {
            lua_pushnumber(state, color.Red);
            lua_pushnumber(state, color.Green);
            lua_pushnumber(state, color.Blue);
            lua_pushnumber(state, color.Alpha);
            if (lua_pcall(state, 4, 1, 0) == 0 &&
                lua_type(state, -1) == LUA_TTABLE)
                return;
            lua_pop(state, 1);
        }
        else
        {
            lua_pop(state, 1);
        }

        lua_newtable(state);
        lua_pushnumber(state, color.Red);
        lua_setfield(state, -2, "r");
        lua_pushnumber(state, color.Green);
        lua_setfield(state, -2, "g");
        lua_pushnumber(state, color.Blue);
        lua_setfield(state, -2, "b");
        lua_pushnumber(state, color.Alpha);
        lua_setfield(state, -2, "a");
    }
}
