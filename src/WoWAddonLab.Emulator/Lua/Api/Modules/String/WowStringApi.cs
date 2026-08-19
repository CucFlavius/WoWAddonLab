using System.Text;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowStringApi : LuaApiModule
{
    private static readonly lua_CFunction StrtrimCallback = Strtrim;
    private static readonly lua_CFunction StrlenUtf8Callback = StrlenUtf8;
    private static readonly lua_CFunction StrcmpUtf8IgnoreCaseCallback =
        StrcmpUtf8IgnoreCase;

    public override void Register(lua_State state)
    {
        lua_pushcclosure(state, StrtrimCallback, 0);
        lua_setglobal(state, "strtrim");
        lua_pushcclosure(state, StrlenUtf8Callback, 0);
        lua_setglobal(state, "strlenutf8");
        lua_pushcclosure(state, StrcmpUtf8IgnoreCaseCallback, 0);
        lua_setglobal(state, "strcmputf8i");

        lua_getglobal(state, "string");
        lua_pushcclosure(state, StrtrimCallback, 0);
        lua_setfield(state, -2, "trim");
        lua_pop(state, 1);
    }

    private static int Strtrim(lua_State state)
    {
        const string usage =
            "Usage: local trimmed = string.trim(str [, characters])";
        if (lua_isstring(state, 1) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = lua_tostring(state, 1) ?? string.Empty;
        var characters = " \r\n\t";
        if (lua_gettop(state) >= 2 && lua_type(state, 2) != LUA_TNIL)
        {
            if (lua_isstring(state, 2) == 0)
            {
                luaL_error(state, usage);
                return 0;
            }

            characters = lua_tostring(state, 2) ?? string.Empty;
        }

        var result = characters.Length == 0
            ? value
            : value.Trim(characters.ToCharArray());
        lua_pushstring(state, result);
        return 1;
    }

    private static int StrlenUtf8(lua_State state)
    {
        if (lua_isstring(state, 1) == 0)
            return luaL_error(state, "Usage: local length = strlenutf8(text)");

        var bytes = LuaStringInterop.RequiredBytes(
            state,
            1,
            "Usage: local length = strlenutf8(text)");
        var value = Encoding.UTF8.GetString(bytes);
        lua_pushinteger(state, value.EnumerateRunes().Count());
        return 1;
    }

    private static int StrcmpUtf8IgnoreCase(lua_State state)
    {
        const string usage = "Usage: strcmputf8i(string1, string2)";
        if (lua_isstring(state, 1) == 0 || lua_isstring(state, 2) == 0)
            return luaL_error(state, usage);

        var left = Encoding.UTF8
            .GetString(LuaStringInterop.RequiredCStringBytes(state, 1, usage))
            .EnumerateRunes()
            .Select(Rune.ToUpperInvariant)
            .ToArray();
        var right = Encoding.UTF8
            .GetString(LuaStringInterop.RequiredCStringBytes(state, 2, usage))
            .EnumerateRunes()
            .Select(Rune.ToUpperInvariant)
            .ToArray();
        var sharedLength = Math.Min(left.Length, right.Length);
        for (var index = 0; index < sharedLength; index++)
        {
            if (left[index].Value == right[index].Value)
                continue;
            lua_pushinteger(state, left[index].Value - right[index].Value);
            return 1;
        }

        var result = left.Length == right.Length
            ? 0
            : left.Length > right.Length
                ? left[sharedLength].Value
                : -right[sharedLength].Value;
        lua_pushinteger(state, result);
        return 1;
    }
}
