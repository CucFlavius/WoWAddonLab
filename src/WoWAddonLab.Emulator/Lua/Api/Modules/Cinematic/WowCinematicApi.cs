using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCinematicApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CinematicFinished",
        "CinematicStarted",
        "GetCurrentCinematicSummary",
        "InCinematic",
        "MouseOverrideCinematicDisable",
        "OpeningCinematic",
        "StopCinematic"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var cinematic = LuaBindings.GetRuntime(state).Cinematics;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "CinematicStarted":
            {
                const string usage =
                    "Usage: CinematicStarted(movieType, movieID [, canCancel])";
                var movieType = RequiredMovieType(state, 1, usage);
                var movieId = RequiredInt32(state, 2, usage);
                var canCancel = OptionalBoolean(state, 3, true, usage);
                cinematic.Start(movieType, movieId, canCancel);
                return 0;
            }
            case "CinematicFinished":
            {
                const string usage =
                    "Usage: CinematicFinished(movieType [, userCanceled, didError])";
                var movieType = RequiredMovieType(state, 1, usage);
                var userCanceled = OptionalBoolean(state, 2, false, usage);
                var didError = OptionalBoolean(state, 3, false, usage);
                cinematic.Finish(movieType, userCanceled, didError);
                return 0;
            }
            case "StopCinematic":
                cinematic.Finish(3, true, false);
                return 0;
            case "GetCurrentCinematicSummary":
                if (cinematic.CurrentMovieId != 0 &&
                    cinematic.CurrentSummary is not null)
                {
                    lua_pushstring(state, cinematic.CurrentSummary);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            case "InCinematic":
                lua_pushboolean(state, cinematic.InCinematic ? 1 : 0);
                return 1;
            case "MouseOverrideCinematicDisable":
                cinematic.MouseOverrideDisabled = OptionalBoolean(
                    state,
                    1,
                    false,
                    "Usage: MouseOverrideCinematicDisable([doOverride])");
                return 0;
            case "OpeningCinematic":
                cinematic.OpeningCinematicRequested = true;
                return 0;
            default:
                return 0;
        }
    }

    private static int RequiredMovieType(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInteger(state, index, usage);
        if (value is < 0 or > 3)
            return luaL_error(state, usage);
        return (int)value;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInteger(state, index, usage);
        if (value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return (int)value;
    }

    private static long RequiredInteger(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TNUMBER)
            return luaL_error(state, usage);

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value != Math.Truncate(value) ||
            value < long.MinValue ||
            value > long.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (long)value;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        bool defaultValue,
        string usage)
    {
        var type = lua_type(state, index);
        if (type is LUA_TNONE or LUA_TNIL)
            return defaultValue;
        if (type != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return defaultValue;
        }
        return lua_toboolean(state, index) != 0;
    }
}
