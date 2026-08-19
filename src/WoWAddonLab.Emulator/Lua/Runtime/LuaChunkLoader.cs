using System.Runtime.InteropServices;
using System.Text;
using LuaNET.Lua51;

namespace WoWAddonLab.Emulator.Lua;

internal static unsafe class LuaChunkLoader
{
    private static readonly NativeWriter Writer = WriteChunk;

    public static int Load(lua_State state, string source, string chunkName)
    {
        var bytes = Encoding.UTF8.GetBytes(source);
        return Load(state, bytes, 0, bytes.Length, chunkName);
    }

    public static unsafe int Load(
        lua_State state,
        byte[] source,
        int offset,
        int count,
        string chunkName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (offset > source.Length - count)
            throw new ArgumentOutOfRangeException(nameof(count));

        var name = Encoding.UTF8.GetBytes(chunkName + '\0');
        fixed (byte* sourcePointer = source)
        fixed (byte* namePointer = name)
        {
            return NativeLoadBuffer(
                state,
                sourcePointer + offset,
                (nuint)count,
                namePointer);
        }
    }

    public static byte[]? Dump(lua_State state)
    {
        using var stream = new MemoryStream();
        var handle = GCHandle.Alloc(stream);
        try
        {
            return NativeDump(state, Writer, GCHandle.ToIntPtr(handle)) == 0
                ? stream.ToArray()
                : null;
        }
        finally
        {
            handle.Free();
            GC.KeepAlive(Writer);
        }
    }

    private static unsafe int WriteChunk(
        lua_State state,
        byte* buffer,
        nuint size,
        nint userData)
    {
        try
        {
            var stream = (MemoryStream)GCHandle.FromIntPtr(userData).Target!;
            stream.Write(new ReadOnlySpan<byte>(buffer, checked((int)size)));
            return 0;
        }
        catch (Exception)
        {
            return 1;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate int NativeWriter(
        lua_State state,
        byte* buffer,
        nuint size,
        nint userData);

    [DllImport("lua515", EntryPoint = "luaL_loadbuffer", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe int NativeLoadBuffer(
        lua_State state,
        byte* buffer,
        nuint size,
        byte* name);

    [DllImport("lua515", EntryPoint = "lua_dump", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeDump(
        lua_State state,
        NativeWriter writer,
        nint userData);
}
