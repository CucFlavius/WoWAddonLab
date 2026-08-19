using System.Formats.Cbor;
using System.IO.Compression;
using System.Text;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowEncodingUtilApi : LuaApiModule
{
    private const string DecodeBase64Usage =
        "Usage: local output = C_EncodingUtil.DecodeBase64(source [, variant])";
    private const string DecompressStringUsage =
        "Usage: local output = C_EncodingUtil.DecompressString(source [, method])";
    private const int MaximumDecompressedSize = 100 * 1024 * 1024;
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "CompressString", "DecodeBase64", "DecodeHex", "DecompressString",
        "DeserializeCBOR", "DeserializeJSON", "EncodeBase64", "EncodeHex",
        "SerializeCBOR", "SerializeJSON"
    ];

    public override void Register(lua_State state)
    {
        SetEnum(state, "Base64Variant", ("Standard", 0), ("StandardUrlSafe", 1));
        SetEnumMeta(state, "Base64VariantMeta", 0, 1, 2);
        SetEnum(state, "CompressionMethod", ("Deflate", 0), ("Zlib", 1), ("Gzip", 2));
        SetEnumMeta(state, "CompressionMethodMeta", 0, 2, 3);
        SetEnum(
            state,
            "CompressionLevel",
            ("Default", 0),
            ("OptimizeForSpeed", 1),
            ("OptimizeForSize", 2));
        SetEnumMeta(state, "CompressionLevelMeta", 0, 2, 3);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_EncodingUtil");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "EncodeBase64":
            {
                var source = LuaStringInterop.RequiredBytes(state, 1, "Usage: local output = C_EncodingUtil.EncodeBase64(source [, variant])");
                var variant = OptionalEnum(state, 2, 0, 1, 0, "Usage: local output = C_EncodingUtil.EncodeBase64(source [, variant])");
                var output = Convert.ToBase64String(source);
                if (variant == 1)
                    output = output.Replace('+', '-').Replace('/', '_');
                lua_pushstring(state, output);
                return 1;
            }
            case "DecodeBase64":
                try
                {
                    var source = Encoding.ASCII.GetString(
                        LuaStringInterop.RequiredBytes(state, 1, DecodeBase64Usage));
                    var variant = OptionalEnum(state, 2, 0, 1, 0, DecodeBase64Usage);
                    if (variant == 0 && (source.Contains('-') || source.Contains('_')))
                        return luaL_error(state, DecodeBase64Usage);
                    var normalized = source.Replace('-', '+').Replace('_', '/');
                    normalized = normalized.PadRight((normalized.Length + 3) / 4 * 4, '=');
                    LuaStringInterop.PushBytes(state, Convert.FromBase64String(normalized));
                    return 1;
                }
                catch (FormatException)
                {
                    return 0;
                }
            case "EncodeHex":
                lua_pushstring(
                    state,
                    Convert.ToHexStringLower(LuaStringInterop.RequiredBytes(
                        state,
                        1,
                        "Usage: local output = C_EncodingUtil.EncodeHex(source)")));
                return 1;
            case "DecodeHex":
                try
                {
                    var source = Encoding.ASCII.GetString(LuaStringInterop.RequiredBytes(
                        state,
                        1,
                        "Usage: local output = C_EncodingUtil.DecodeHex(source)"));
                    LuaStringInterop.PushBytes(state, Convert.FromHexString(source));
                    return 1;
                }
                catch (FormatException)
                {
                    return 0;
                }
            case "DecompressString":
                return DecompressString(state);
            case "DeserializeCBOR":
                return DeserializeCbor(state);
            case "DeserializeJSON":
                lua_pushnil(state);
                return 1;
            case "SerializeCBOR":
            case "SerializeJSON":
                lua_pushstring(state, string.Empty);
                return 1;
            default:
                return 0;
        }
    }

    private static int DecompressString(lua_State state)
    {
        var source = LuaStringInterop.RequiredBytes(state, 1, DecompressStringUsage);
        var method = OptionalEnum(state, 2, 0, 2, 0, DecompressStringUsage);
        try
        {
            using var input = new MemoryStream(source, false);
            using Stream decompressor = method switch
            {
                0 => new DeflateStream(input, CompressionMode.Decompress),
                1 => new ZLibStream(input, CompressionMode.Decompress),
                2 => new GZipStream(input, CompressionMode.Decompress),
                _ => throw new InvalidOperationException()
            };
            using var output = new MemoryStream();
            var buffer = new byte[81920];
            while (true)
            {
                var read = decompressor.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;
                if (output.Length + read > MaximumDecompressedSize)
                    return luaL_error(
                        state,
                        $"DecompressString: internal decompression size limit reached (cannot exceed {MaximumDecompressedSize} bytes)");
                output.Write(buffer, 0, read);
            }
            LuaStringInterop.PushBytes(state, output.GetBuffer().AsSpan(0, checked((int)output.Length)));
            return 1;
        }
        catch (InvalidDataException)
        {
            return luaL_error(state, "DecompressString: internal decompression error");
        }
    }

    private static int DeserializeCbor(lua_State state)
    {
        var source = LuaStringInterop.RequiredBytes(
            state,
            1,
            "Usage: local ... = C_EncodingUtil.DeserializeCBOR(source)");
        try
        {
            var reader = new CborReader(
                source,
                CborConformanceMode.Lax,
                allowMultipleRootLevelValues: true);
            var count = 0;
            while (reader.PeekState() != CborReaderState.Finished)
            {
                if (count == 100)
                    return luaL_error(
                        state,
                        "DeserializeCBOR: attempted to deserialize too many values (cannot exceed 100 values)");
                PushCborValue(state, reader);
                count++;
            }
            return count;
        }
        catch (CborContentException exception)
        {
            return luaL_error(state, $"DeserializeCBOR: {exception.Message}");
        }
    }

    private static void PushCborValue(lua_State state, CborReader reader)
    {
        switch (reader.PeekState())
        {
            case CborReaderState.UnsignedInteger:
                lua_pushnumber(state, reader.ReadUInt64());
                return;
            case CborReaderState.NegativeInteger:
                lua_pushnumber(state, reader.ReadInt64());
                return;
            case CborReaderState.ByteString:
            case CborReaderState.StartIndefiniteLengthByteString:
                LuaStringInterop.PushBytes(state, reader.ReadByteString());
                return;
            case CborReaderState.TextString:
            case CborReaderState.StartIndefiniteLengthTextString:
                lua_pushstring(state, reader.ReadTextString());
                return;
            case CborReaderState.StartArray:
                PushCborArray(state, reader);
                return;
            case CborReaderState.StartMap:
                PushCborMap(state, reader);
                return;
            case CborReaderState.Boolean:
                lua_pushboolean(state, reader.ReadBoolean() ? 1 : 0);
                return;
            case CborReaderState.Null:
                reader.ReadNull();
                lua_pushnil(state);
                return;
            case CborReaderState.Undefined:
                reader.SkipValue();
                lua_pushnil(state);
                return;
            case CborReaderState.HalfPrecisionFloat:
                lua_pushnumber(state, (double)reader.ReadHalf());
                return;
            case CborReaderState.SinglePrecisionFloat:
                lua_pushnumber(state, reader.ReadSingle());
                return;
            case CborReaderState.DoublePrecisionFloat:
                lua_pushnumber(state, reader.ReadDouble());
                return;
            case CborReaderState.Tag:
                reader.ReadTag();
                PushCborValue(state, reader);
                return;
            case CborReaderState.SimpleValue:
                lua_pushnumber(state, (byte)reader.ReadSimpleValue());
                return;
            default:
                throw new CborContentException($"Unsupported CBOR state {reader.PeekState()}.");
        }
    }

    private static void PushCborArray(lua_State state, CborReader reader)
    {
        reader.ReadStartArray();
        lua_newtable(state);
        var index = 1;
        while (reader.PeekState() != CborReaderState.EndArray)
        {
            PushCborValue(state, reader);
            lua_rawseti(state, -2, index++);
        }
        reader.ReadEndArray();
    }

    private static void PushCborMap(lua_State state, CborReader reader)
    {
        reader.ReadStartMap();
        lua_newtable(state);
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            PushCborValue(state, reader);
            PushCborValue(state, reader);
            lua_settable(state, -3);
        }
        reader.ReadEndMap();
    }

    private static int OptionalEnum(
        lua_State state,
        int index,
        int minimum,
        int maximum,
        int defaultValue,
        string usage)
    {
        if (lua_type(state, index) == LUA_TNONE || lua_type(state, index) == LUA_TNIL)
            return defaultValue;
        if (lua_type(state, index) != LUA_TNUMBER)
        {
            luaL_error(state, usage);
            return defaultValue;
        }
        var value = (int)lua_tonumber(state, index);
        if (value < minimum || value > maximum)
        {
            luaL_error(state, usage);
            return defaultValue;
        }
        return value;
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] fields)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }
        lua_newtable(state);
        foreach (var field in fields)
        {
            lua_pushinteger(state, field.Value);
            lua_setfield(state, -2, field.Name);
        }
        lua_setfield(state, -2, name);
        lua_pop(state, 1);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int minimum,
        int maximum,
        int count) =>
        SetEnum(
            state,
            name,
            ("NumValues", count),
            ("MinValue", minimum),
            ("MaxValue", maximum));
}
