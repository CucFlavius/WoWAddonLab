using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal static class LuaStringInterop
{
    public static unsafe void PushBytes(lua_State state, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* pointer = bytes)
            LuaPushLString(state, pointer, (ulong)bytes.Length);
    }

    public static byte[] RequiredBytes(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return [];
        }

        ulong length = 0;
        var pointer = LuaToString(state, index, ref length);
        if (pointer == 0 || length == 0)
            return [];
        if (length > int.MaxValue)
        {
            luaL_error(state, usage);
            return [];
        }

        var bytes = new byte[(int)length];
        Marshal.Copy(pointer, bytes, 0, bytes.Length);
        return bytes;
    }

    public static byte[] RequiredCStringBytes(
        lua_State state,
        int index,
        string usage)
    {
        var bytes = RequiredBytes(state, index, usage);
        var terminatorIndex = Array.IndexOf(bytes, (byte)0);
        return terminatorIndex < 0 ? bytes : bytes[..terminatorIndex];
    }

    [DllImport(
        "lua515",
        EntryPoint = "lua_tolstring",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern nint LuaToString(
        lua_State state,
        int index,
        ref ulong length);

    [DllImport(
        "lua515",
        EntryPoint = "lua_pushlstring",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe nint LuaPushLString(
        lua_State state,
        byte* value,
        ulong length);
}
