using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowBitApi : LuaApiModule
{
    private const double Int64ExclusiveUpperBound = 9223372036854775808.0;
    private const double Int64InclusiveLowerBound = -9223372036854775808.0;

    private static readonly lua_CFunction BNotCallback = BNot;
    private static readonly lua_CFunction BAndCallback = BAnd;
    private static readonly lua_CFunction BOrCallback = BOr;
    private static readonly lua_CFunction BXorCallback = BXor;
    private static readonly lua_CFunction LShiftCallback = LShift;
    private static readonly lua_CFunction RShiftCallback = RShift;
    private static readonly lua_CFunction ARShiftCallback = ARShift;
    private static readonly lua_CFunction ModCallback = Mod;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        SetFunction(state, "bnot", BNotCallback);
        SetFunction(state, "band", BAndCallback);
        SetFunction(state, "bor", BOrCallback);
        SetFunction(state, "bxor", BXorCallback);
        SetFunction(state, "lshift", LShiftCallback);
        SetFunction(state, "rshift", RShiftCallback);
        SetFunction(state, "arshift", ARShiftCallback);
        SetFunction(state, "mod", ModCallback);

        lua_pushvalue(state, -1);
        lua_setglobal(state, "bit");
        lua_getglobal(state, "package");
        lua_getfield(state, -1, "loaded");
        lua_pushvalue(state, -3);
        lua_setfield(state, -2, "bit");
        lua_pop(state, 3);
    }

    private static void SetFunction(lua_State state, string name, lua_CFunction callback)
    {
        lua_pushcfunction(state, callback);
        lua_setfield(state, -2, name);
    }

    private static int BNot(lua_State state)
    {
        var value = ReadWord(state, 1);
        lua_pushnumber(state, ~value);
        return 1;
    }

    private static int BAnd(lua_State state) => Fold(state, static (left, right) => left & right);

    private static int BOr(lua_State state) => Fold(state, static (left, right) => left | right);

    private static int BXor(lua_State state) => Fold(state, static (left, right) => left ^ right);

    private static int Fold(lua_State state, Func<uint, uint, uint> operation)
    {
        var result = ReadWord(state, 1);

        var count = lua_gettop(state);
        for (var index = 2; index <= count; index++)
            result = operation(result, ReadWord(state, index));

        lua_pushnumber(state, result);
        return 1;
    }

    private static int LShift(lua_State state)
    {
        var shift = (int)(ReadWord(state, 2) & 31);
        var value = ReadWord(state, 1);
        lua_pushnumber(state, value << shift);
        return 1;
    }

    private static int RShift(lua_State state)
    {
        var shift = (int)(ReadWord(state, 2) & 31);
        var value = ReadWord(state, 1);
        lua_pushnumber(state, value >> shift);
        return 1;
    }

    private static int ARShift(lua_State state)
    {
        var shift = (int)(ReadWord(state, 2) & 31);
        var value = ReadWord(state, 1);
        lua_pushnumber(state, unchecked((int)value) >> shift);
        return 1;
    }

    private static int Mod(lua_State state)
    {
        var divisorNumber = luaL_checknumber(state, 2);
        var divisorWord = ToNativeWord(divisorNumber);
        if (divisorWord == 0)
        {
            lua_pushnumber(state, 1.0 / divisorNumber);
            return 1;
        }

        divisorWord = ReadWord(state, 2);
        var dividendWord = ReadWord(state, 1);
        var dividend = unchecked((int)dividendWord);
        var divisor = unchecked((int)divisorWord);
        lua_pushnumber(state, dividend % divisor);
        return 1;
    }

    private static uint ReadWord(lua_State state, int index) =>
        ToNativeWord(luaL_checknumber(state, index));

    private static uint ToNativeWord(double value)
    {
        var truncated = double.IsNaN(value) ||
                        value >= Int64ExclusiveUpperBound ||
                        value < Int64InclusiveLowerBound
            ? long.MinValue
            : (long)value;
        return unchecked((uint)truncated);
    }
}
