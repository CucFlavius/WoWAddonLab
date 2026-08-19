using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TinyBCSharp;

namespace WoWAddonLab.Assets;

internal sealed class BLP
{
    private const uint Blp0Magic = 0x30504C42;
    private const uint Blp1Magic = 0x31504C42;
    private const uint Blp2Magic = 0x32504C42;
    private const int Blp2HeaderSize = 148;
    private const int LegacyHeaderSize = 156;
    private const int MaximumMipLevels = 16;

    private readonly byte[] _file;
    private readonly BlpColorEncoding _colorEncoding;
    private readonly byte _alphaDepth;
    private readonly BlpPixelFormat _preferredFormat;
    private readonly int _width;
    private readonly int _height;
    private readonly MipPayload[] _mips;
    private readonly int _paletteOffset;
    private readonly byte[] _jpegHeader;

    public BLP(byte[] file)
    {
        ArgumentNullException.ThrowIfNull(file);
        _file = file;

        if (file.Length < sizeof(uint))
            throw Invalid("header is truncated");

        var magic = ReadUInt32(file, 0);
        switch (magic)
        {
            case Blp2Magic:
                ParseBlp2(
                    out _colorEncoding,
                    out _alphaDepth,
                    out _preferredFormat,
                    out _width,
                    out _height,
                    out _paletteOffset,
                    out _jpegHeader,
                    out _mips);
                break;

            case Blp0Magic:
            case Blp1Magic:
                ParseLegacy(
                    out _colorEncoding,
                    out _alphaDepth,
                    out _preferredFormat,
                    out _width,
                    out _height,
                    out _paletteOffset,
                    out _jpegHeader,
                    out _mips);
                break;

            default:
                throw Invalid("magic is not BLP0, BLP1, or BLP2");
        }
    }

    public DecodedTextureImage Decode()
    {
        if (_mips.Length == 0)
            throw Invalid("contains no image levels");

        var levels = new DecodedTextureMipLevel[_mips.Length];
        for (var level = 0; level < _mips.Length; level++)
        {
            var width = Math.Max(1, _width >> level);
            var height = Math.Max(1, _height >> level);
            var mip = _mips[level];
            var payloadLength = ResolvePayloadLength(mip, width, height);
            var storedLength = Math.Min(mip.Length, payloadLength);
            if ((ulong)mip.Offset + (uint)storedLength > (ulong)_file.Length)
            {
                throw Invalid(
                    $"mip {level} payload is outside the file " +
                    $"(offset {mip.Offset}, stored {mip.Length}, " +
                    $"decoded {payloadLength}, dimensions {width}x{height}, " +
                    $"file {_file.Length}, table {MipTableSummary()})");
            }

            byte[]? paddedPayload = null;
            ReadOnlySpan<byte> payload;
            if (_colorEncoding == BlpColorEncoding.BlockCompressed && storedLength < payloadLength)
            {
                paddedPayload = new byte[payloadLength];
                _file.AsSpan(mip.Offset, storedLength).CopyTo(paddedPayload);
                payload = paddedPayload;
            }
            else
            {
                payload = _file.AsSpan(mip.Offset, storedLength);
            }
            try
            {
                levels[level] = DecodeLevel(payload, width, height);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                throw Invalid(
                    $"mip {level} ({width}x{height}, encoding {(byte)_colorEncoding}, " +
                    $"format {(byte)_preferredFormat}, alpha {_alphaDepth}, " +
                    $"stored {mip.Length} bytes) could not be decoded",
                    exception);
            }
        }

        return new DecodedTextureImage(levels);
    }

    private string MipTableSummary() => string.Join(
        ", ",
        _mips.Select((mip, level) => $"{level}:{mip.Offset}+{mip.Length}"));

    private void ParseBlp2(
        out BlpColorEncoding colorEncoding,
        out byte alphaDepth,
        out BlpPixelFormat preferredFormat,
        out int width,
        out int height,
        out int paletteOffset,
        out byte[] jpegHeader,
        out MipPayload[] mips)
    {
        RequireLength(_file, Blp2HeaderSize, "BLP2 header");
        var version = ReadUInt32(_file, 4);
        if (version != 1)
            throw Invalid($"unsupported BLP2 version {version}");

        colorEncoding = (BlpColorEncoding)_file[8];
        alphaDepth = _file[9];
        preferredFormat = (BlpPixelFormat)_file[10];
        if (preferredFormat == BlpPixelFormat.Unspecified)
            preferredFormat = BlpPixelFormat.Argb8888;

        width = ReadDimension(_file, 12, "width");
        height = ReadDimension(_file, 16, "height");
        paletteOffset = colorEncoding == BlpColorEncoding.Palette
            ? Blp2HeaderSize
            : -1;
        jpegHeader = colorEncoding == BlpColorEncoding.Jpeg
            ? ReadJpegHeader(_file, Blp2HeaderSize)
            : [];
        mips = ReadMipTable(_file, 20, 84, width, height);

        if (paletteOffset >= 0)
            RequireLength(_file, checked(paletteOffset + 256 * 4), "BLP2 palette");
    }

    private void ParseLegacy(
        out BlpColorEncoding colorEncoding,
        out byte alphaDepth,
        out BlpPixelFormat preferredFormat,
        out int width,
        out int height,
        out int paletteOffset,
        out byte[] jpegHeader,
        out MipPayload[] mips)
    {
        RequireLength(_file, LegacyHeaderSize, "legacy BLP header");
        var rawEncoding = ReadUInt32(_file, 4);
        if (rawEncoding > byte.MaxValue)
            throw Invalid($"unsupported legacy color encoding {rawEncoding}");

        colorEncoding = (BlpColorEncoding)rawEncoding;
        var rawAlphaDepth = ReadUInt32(_file, 8);
        if (rawAlphaDepth > byte.MaxValue)
            throw Invalid($"invalid legacy alpha depth {rawAlphaDepth}");
        alphaDepth = (byte)rawAlphaDepth;

        width = ReadDimension(_file, 12, "width");
        height = ReadDimension(_file, 16, "height");
        var rawPreferredFormat = ReadUInt32(_file, 20);
        if (rawPreferredFormat > byte.MaxValue)
            throw Invalid($"unsupported legacy pixel format {rawPreferredFormat}");
        preferredFormat = (BlpPixelFormat)rawPreferredFormat;

        paletteOffset = colorEncoding == BlpColorEncoding.Palette
            ? LegacyHeaderSize
            : -1;
        jpegHeader = colorEncoding == BlpColorEncoding.Jpeg
            ? ReadJpegHeader(_file, LegacyHeaderSize)
            : [];
        mips = ReadMipTable(_file, 28, 92, width, height);

        if (paletteOffset >= 0)
            RequireLength(_file, checked(paletteOffset + 256 * 4), "legacy BLP palette");
    }

    private DecodedTextureMipLevel DecodeLevel(
        ReadOnlySpan<byte> payload,
        int width,
        int height) => _colorEncoding switch
        {
            BlpColorEncoding.Jpeg => DecodeJpeg(payload, width, height),
            BlpColorEncoding.Palette => DecodePalette(payload, width, height),
            BlpColorEncoding.BlockCompressed => DecodeBlockCompressed(payload, width, height),
            BlpColorEncoding.Argb8888 or BlpColorEncoding.Argb8888Duplicate =>
                DecodeRaw(payload, width, height),
            _ => throw Invalid($"unsupported color encoding {(byte)_colorEncoding}")
        };

    private DecodedTextureMipLevel DecodeJpeg(
        ReadOnlySpan<byte> payload,
        int width,
        int height)
    {
        var encoded = new byte[checked(_jpegHeader.Length + payload.Length)];
        _jpegHeader.CopyTo(encoded, 0);
        payload.CopyTo(encoded.AsSpan(_jpegHeader.Length));

        using var image = Image.Load<Rgba32>(encoded);
        if (image.Width != width || image.Height != height)
        {
            throw Invalid(
                $"JPEG mip dimensions are {image.Width}x{image.Height}, expected {width}x{height}");
        }

        var pixels = new byte[PixelByteCount(width, height)];
        image.CopyPixelDataTo(pixels);
        return new DecodedTextureMipLevel(pixels, width, height);
    }

    private DecodedTextureMipLevel DecodePalette(
        ReadOnlySpan<byte> payload,
        int width,
        int height)
    {
        var pixelCount = PixelCount(width, height);
        var alphaDepth = _alphaDepth & 0x3F;
        var alphaBytes = alphaDepth switch
        {
            0 => 0,
            1 => checked((pixelCount + 7) / 8),
            4 => checked((pixelCount + 1) / 2),
            8 => pixelCount,
            _ => throw Invalid($"unsupported palette alpha depth {alphaDepth}")
        };
        RequirePayload(payload, checked(pixelCount + alphaBytes), "palette mip");

        var pixels = new byte[checked(pixelCount * 4)];
        var alpha = payload[pixelCount..];
        for (var index = 0; index < pixelCount; index++)
        {
            var paletteEntry = checked(_paletteOffset + payload[index] * 4);
            var target = index * 4;
            pixels[target] = _file[paletteEntry + 2];
            pixels[target + 1] = _file[paletteEntry + 1];
            pixels[target + 2] = _file[paletteEntry];
            pixels[target + 3] = alphaDepth switch
            {
                0 => byte.MaxValue,
                1 => (alpha[index >> 3] & (1 << (index & 7))) == 0
                    ? (byte)0
                    : byte.MaxValue,
                4 => Expand4To8((byte)((alpha[index >> 1] >> ((index & 1) * 4)) & 0xF)),
                8 => alpha[index],
                _ => throw new InvalidOperationException("Validated alpha depth became invalid.")
            };
        }

        return new DecodedTextureMipLevel(pixels, width, height);
    }

    private DecodedTextureMipLevel DecodeRaw(
        ReadOnlySpan<byte> payload,
        int width,
        int height) => _preferredFormat switch
        {
            BlpPixelFormat.Argb8888 or BlpPixelFormat.Unspecified =>
                DecodeBgra8888(payload, width, height),
            BlpPixelFormat.Argb1555 => DecodeArgb1555(payload, width, height),
            BlpPixelFormat.Argb4444 => DecodeArgb4444(payload, width, height),
            BlpPixelFormat.Rgb565 => DecodeRgb565(payload, width, height),
            BlpPixelFormat.A8 => DecodeA8(payload, width, height),
            BlpPixelFormat.Argb2565 => DecodeArgb2565(payload, width, height),
            _ => throw Invalid($"unsupported raw pixel format {(byte)_preferredFormat}")
        };

    private static DecodedTextureMipLevel DecodeBgra8888(
        ReadOnlySpan<byte> payload,
        int width,
        int height)
    {
        var byteCount = PixelByteCount(width, height);
        RequirePayload(payload, byteCount, "BGRA8888 mip");
        var pixels = new byte[byteCount];
        for (var offset = 0; offset < byteCount; offset += 4)
        {
            pixels[offset] = payload[offset + 2];
            pixels[offset + 1] = payload[offset + 1];
            pixels[offset + 2] = payload[offset];
            pixels[offset + 3] = payload[offset + 3];
        }
        return new DecodedTextureMipLevel(pixels, width, height);
    }

    private static DecodedTextureMipLevel DecodeArgb1555(
        ReadOnlySpan<byte> payload,
        int width,
        int height) => DecodePacked16(
            payload,
            width,
            height,
            packed => (
                Expand5To8((byte)((packed >> 10) & 0x1F)),
                Expand5To8((byte)((packed >> 5) & 0x1F)),
                Expand5To8((byte)(packed & 0x1F)),
                (packed & 0x8000) == 0 ? (byte)0 : byte.MaxValue));

    private static DecodedTextureMipLevel DecodeArgb4444(
        ReadOnlySpan<byte> payload,
        int width,
        int height) => DecodePacked16(
            payload,
            width,
            height,
            packed => (
                Expand4To8((byte)((packed >> 8) & 0xF)),
                Expand4To8((byte)((packed >> 4) & 0xF)),
                Expand4To8((byte)(packed & 0xF)),
                Expand4To8((byte)(packed >> 12))));

    private static DecodedTextureMipLevel DecodeRgb565(
        ReadOnlySpan<byte> payload,
        int width,
        int height) => DecodePacked16(
            payload,
            width,
            height,
            packed => (
                Expand5To8((byte)(packed >> 11)),
                Expand6To8((byte)((packed >> 5) & 0x3F)),
                Expand5To8((byte)(packed & 0x1F)),
                byte.MaxValue));

    private static DecodedTextureMipLevel DecodeA8(
        ReadOnlySpan<byte> payload,
        int width,
        int height)
    {
        var pixelCount = PixelCount(width, height);
        RequirePayload(payload, pixelCount, "A8 mip");
        var pixels = new byte[checked(pixelCount * 4)];
        for (var index = 0; index < pixelCount; index++)
        {
            var target = index * 4;
            pixels[target] = byte.MaxValue;
            pixels[target + 1] = byte.MaxValue;
            pixels[target + 2] = byte.MaxValue;
            pixels[target + 3] = payload[index];
        }
        return new DecodedTextureMipLevel(pixels, width, height);
    }

    private static DecodedTextureMipLevel DecodeArgb2565(
        ReadOnlySpan<byte> payload,
        int width,
        int height)
    {
        var pixelCount = PixelCount(width, height);
        var colorBytes = checked(pixelCount * 2);
        var alphaBytes = Math.Max(1, pixelCount / 4);
        RequirePayload(payload, checked(colorBytes + alphaBytes), "ARGB2565 mip");
        var pixels = new byte[checked(pixelCount * 4)];
        var alpha = payload[colorBytes..];
        for (var index = 0; index < pixelCount; index++)
        {
            var packed = BinaryPrimitives.ReadUInt16LittleEndian(payload[(index * 2)..]);
            var target = index * 4;
            pixels[target] = Expand5To8((byte)(packed >> 11));
            pixels[target + 1] = Expand6To8((byte)((packed >> 5) & 0x3F));
            pixels[target + 2] = Expand5To8((byte)(packed & 0x1F));
            pixels[target + 3] = Expand2To8(
                (byte)((alpha[index >> 2] >> ((index & 3) * 2)) & 3));
        }
        return new DecodedTextureMipLevel(pixels, width, height);
    }

    private static DecodedTextureMipLevel DecodePacked16(
        ReadOnlySpan<byte> payload,
        int width,
        int height,
        Func<ushort, (byte Red, byte Green, byte Blue, byte Alpha)> unpack)
    {
        var pixelCount = PixelCount(width, height);
        RequirePayload(payload, checked(pixelCount * 2), "16-bit raw mip");
        var pixels = new byte[checked(pixelCount * 4)];
        for (var index = 0; index < pixelCount; index++)
        {
            var color = unpack(BinaryPrimitives.ReadUInt16LittleEndian(payload[(index * 2)..]));
            var target = index * 4;
            pixels[target] = color.Red;
            pixels[target + 1] = color.Green;
            pixels[target + 2] = color.Blue;
            pixels[target + 3] = color.Alpha;
        }
        return new DecodedTextureMipLevel(pixels, width, height);
    }

    private DecodedTextureMipLevel DecodeBlockCompressed(
        ReadOnlySpan<byte> payload,
        int width,
        int height)
    {
        var format = _preferredFormat switch
        {
            BlpPixelFormat.Dxt1 => BlockFormat.BC1,
            BlpPixelFormat.Dxt3 => BlockFormat.BC2,
            BlpPixelFormat.Dxt5 => BlockFormat.BC3,
            BlpPixelFormat.Bc4 => BlockFormat.BC4U,
            BlpPixelFormat.Bc5 => BlockFormat.BC5U,
            BlpPixelFormat.Bc7 => BlockFormat.BC7,
            _ => throw Invalid(
                $"unsupported block-compressed pixel format {(byte)_preferredFormat}")
        };

        var pixels = BlockDecoder.Create(format).Decode(width, height, payload.ToArray());
        if (pixels.Length != PixelByteCount(width, height))
        {
            throw Invalid(
                $"{format} decoder returned {pixels.Length} bytes, expected " +
                $"{PixelByteCount(width, height)}");
        }
        return new DecodedTextureMipLevel(pixels, width, height);
    }

    private int ResolvePayloadLength(MipPayload mip, int width, int height)
    {
        if (_colorEncoding != BlpColorEncoding.BlockCompressed)
            return mip.Length;

        var bytesPerBlock = _preferredFormat switch
        {
            BlpPixelFormat.Dxt1 or BlpPixelFormat.Bc4 => 8,
            BlpPixelFormat.Dxt3 or
            BlpPixelFormat.Dxt5 or
            BlpPixelFormat.Bc5 or
            BlpPixelFormat.Bc7 => 16,
            _ => throw Invalid(
                $"unsupported block-compressed pixel format {(byte)_preferredFormat}")
        };
        return checked(Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4) * bytesPerBlock);
    }

    private static void DecodeBc1(
        ReadOnlySpan<byte> source,
        int width,
        int height,
        Span<byte> destination) =>
        DecodeColorBlocks(source, width, height, destination, BlockCompression.Bc1);

    private static void DecodeBc2(
        ReadOnlySpan<byte> source,
        int width,
        int height,
        Span<byte> destination) =>
        DecodeColorBlocks(source, width, height, destination, BlockCompression.Bc2);

    private static void DecodeBc3(
        ReadOnlySpan<byte> source,
        int width,
        int height,
        Span<byte> destination) =>
        DecodeColorBlocks(source, width, height, destination, BlockCompression.Bc3);

    private static void DecodeColorBlocks(
        ReadOnlySpan<byte> source,
        int width,
        int height,
        Span<byte> destination,
        BlockCompression compression)
    {
        var bytesPerBlock = compression == BlockCompression.Bc1 ? 8 : 16;
        var blockColumns = checked((width + 3) / 4);
        var blockRows = checked((height + 3) / 4);
        RequirePayload(source, checked(blockColumns * blockRows * bytesPerBlock), $"{compression} mip");

        Span<byte> blockPixels = stackalloc byte[16 * 4];
        var sourceOffset = 0;
        for (var blockY = 0; blockY < blockRows; blockY++)
        {
            for (var blockX = 0; blockX < blockColumns; blockX++)
            {
                var block = source.Slice(sourceOffset, bytesPerBlock);
                sourceOffset += bytesPerBlock;

                var colorOffset = compression == BlockCompression.Bc1 ? 0 : 8;
                DecodeColorBlock(
                    block[colorOffset..],
                    blockPixels,
                    allowTransparentColor: compression == BlockCompression.Bc1);

                if (compression == BlockCompression.Bc2)
                    DecodeBc2Alpha(block, blockPixels);
                else if (compression == BlockCompression.Bc3)
                    DecodeBc3Alpha(block, blockPixels);

                CopyBlock(blockPixels, blockX, blockY, width, height, destination);
            }
        }
    }

    private static void DecodeColorBlock(
        ReadOnlySpan<byte> source,
        Span<byte> pixels,
        bool allowTransparentColor)
    {
        var color0 = BinaryPrimitives.ReadUInt16LittleEndian(source);
        var color1 = BinaryPrimitives.ReadUInt16LittleEndian(source[2..]);
        Span<byte> colors = stackalloc byte[4 * 4];
        UnpackRgb565(color0, colors);
        UnpackRgb565(color1, colors[4..]);

        if (allowTransparentColor && color0 <= color1)
        {
            for (var channel = 0; channel < 3; channel++)
                colors[8 + channel] = (byte)((colors[channel] + colors[4 + channel]) / 2);
            colors[11] = byte.MaxValue;
            colors[12] = 0;
            colors[13] = 0;
            colors[14] = 0;
            colors[15] = 0;
        }
        else
        {
            for (var channel = 0; channel < 3; channel++)
            {
                colors[8 + channel] =
                    (byte)((2 * colors[channel] + colors[4 + channel]) / 3);
                colors[12 + channel] =
                    (byte)((colors[channel] + 2 * colors[4 + channel]) / 3);
            }
            colors[11] = byte.MaxValue;
            colors[15] = byte.MaxValue;
        }

        var indices = BinaryPrimitives.ReadUInt32LittleEndian(source[4..]);
        for (var pixel = 0; pixel < 16; pixel++)
        {
            var color = (int)((indices >> (pixel * 2)) & 3) * 4;
            colors.Slice(color, 4).CopyTo(pixels.Slice(pixel * 4, 4));
        }
    }

    private static void DecodeBc2Alpha(ReadOnlySpan<byte> source, Span<byte> pixels)
    {
        var alphaBits = BinaryPrimitives.ReadUInt64LittleEndian(source);
        for (var pixel = 0; pixel < 16; pixel++)
            pixels[pixel * 4 + 3] = Expand4To8((byte)((alphaBits >> (pixel * 4)) & 0xF));
    }

    private static void DecodeBc3Alpha(ReadOnlySpan<byte> source, Span<byte> pixels)
    {
        Span<byte> alpha = stackalloc byte[16];
        DecodeBc4Block(source, alpha);
        for (var pixel = 0; pixel < 16; pixel++)
            pixels[pixel * 4 + 3] = alpha[pixel];
    }

    private static void DecodeBc4Image(
        ReadOnlySpan<byte> source,
        int width,
        int height,
        Span<byte> destination)
    {
        var blockColumns = checked((width + 3) / 4);
        var blockRows = checked((height + 3) / 4);
        RequirePayload(source, checked(blockColumns * blockRows * 8), "BC4 mip");
        Span<byte> channel = stackalloc byte[16];
        var offset = 0;
        for (var blockY = 0; blockY < blockRows; blockY++)
        {
            for (var blockX = 0; blockX < blockColumns; blockX++)
            {
                DecodeBc4Block(source.Slice(offset, 8), channel);
                offset += 8;
                CopyChannelBlock(
                    channel,
                    ReadOnlySpan<byte>.Empty,
                    blockX,
                    blockY,
                    width,
                    height,
                    destination);
            }
        }
    }

    private static void DecodeBc5Image(
        ReadOnlySpan<byte> source,
        int width,
        int height,
        Span<byte> destination)
    {
        var blockColumns = checked((width + 3) / 4);
        var blockRows = checked((height + 3) / 4);
        RequirePayload(source, checked(blockColumns * blockRows * 16), "BC5 mip");
        Span<byte> red = stackalloc byte[16];
        Span<byte> green = stackalloc byte[16];
        var offset = 0;
        for (var blockY = 0; blockY < blockRows; blockY++)
        {
            for (var blockX = 0; blockX < blockColumns; blockX++)
            {
                DecodeBc4Block(source.Slice(offset, 8), red);
                DecodeBc4Block(source.Slice(offset + 8, 8), green);
                offset += 16;
                CopyChannelBlock(red, green, blockX, blockY, width, height, destination);
            }
        }
    }

    private static void DecodeBc4Block(ReadOnlySpan<byte> source, Span<byte> channel)
    {
        Span<byte> values = stackalloc byte[8];
        values[0] = source[0];
        values[1] = source[1];
        if (values[0] > values[1])
        {
            for (var index = 1; index < 7; index++)
                values[index + 1] = (byte)(((7 - index) * values[0] + index * values[1]) / 7);
        }
        else
        {
            for (var index = 1; index < 5; index++)
                values[index + 1] = (byte)(((5 - index) * values[0] + index * values[1]) / 5);
            values[6] = 0;
            values[7] = byte.MaxValue;
        }

        ulong indices = 0;
        for (var index = 0; index < 6; index++)
            indices |= (ulong)source[index + 2] << (index * 8);
        for (var pixel = 0; pixel < 16; pixel++)
            channel[pixel] = values[(int)((indices >> (pixel * 3)) & 7)];
    }

    private static void CopyBlock(
        ReadOnlySpan<byte> block,
        int blockX,
        int blockY,
        int width,
        int height,
        Span<byte> destination)
    {
        for (var localY = 0; localY < 4; localY++)
        {
            var y = blockY * 4 + localY;
            if (y >= height)
                break;
            for (var localX = 0; localX < 4; localX++)
            {
                var x = blockX * 4 + localX;
                if (x >= width)
                    break;
                block.Slice((localY * 4 + localX) * 4, 4)
                    .CopyTo(destination.Slice((y * width + x) * 4, 4));
            }
        }
    }

    private static void CopyChannelBlock(
        ReadOnlySpan<byte> red,
        ReadOnlySpan<byte> green,
        int blockX,
        int blockY,
        int width,
        int height,
        Span<byte> destination)
    {
        for (var localY = 0; localY < 4; localY++)
        {
            var y = blockY * 4 + localY;
            if (y >= height)
                break;
            for (var localX = 0; localX < 4; localX++)
            {
                var x = blockX * 4 + localX;
                if (x >= width)
                    break;
                var source = localY * 4 + localX;
                var target = (y * width + x) * 4;
                destination[target] = red[source];
                destination[target + 1] = green.IsEmpty ? (byte)0 : green[source];
                destination[target + 2] = 0;
                destination[target + 3] = byte.MaxValue;
            }
        }
    }

    private static void UnpackRgb565(ushort packed, Span<byte> color)
    {
        var red = (packed >> 11) & 0x1F;
        var green = (packed >> 5) & 0x3F;
        var blue = packed & 0x1F;
        color[0] = (byte)((red << 3) | (red >> 2));
        color[1] = (byte)((green << 2) | (green >> 4));
        color[2] = (byte)((blue << 3) | (blue >> 2));
        color[3] = byte.MaxValue;
    }

    private static MipPayload[] ReadMipTable(
        byte[] file,
        int offsetsStart,
        int sizesStart,
        int width,
        int height)
    {
        var maximumLevels = 1;
        var mipWidth = width;
        var mipHeight = height;
        for (;
             (mipWidth > 1 || mipHeight > 1) && maximumLevels < MaximumMipLevels;
             maximumLevels++)
        {
            mipWidth = Math.Max(1, mipWidth >> 1);
            mipHeight = Math.Max(1, mipHeight >> 1);
        }

        var levels = new List<MipPayload>(maximumLevels);
        for (var level = 0; level < maximumLevels; level++)
        {
            var offset = ReadUInt32(file, offsetsStart + level * sizeof(uint));
            var length = ReadUInt32(file, sizesStart + level * sizeof(uint));
            if (offset == 0 || length == 0)
                break;
            if (offset > int.MaxValue || length > int.MaxValue || offset + (ulong)length > (ulong)file.Length)
                throw Invalid($"mip {level} payload is outside the file");
            levels.Add(new MipPayload((int)offset, (int)length));
        }
        return levels.ToArray();
    }

    private static byte[] ReadJpegHeader(byte[] file, int offset)
    {
        RequireLength(file, checked(offset + sizeof(uint)), "BLP JPEG header size");
        var length = ReadUInt32(file, offset);
        if (length > int.MaxValue ||
            (ulong)(offset + sizeof(uint)) + length > (ulong)file.Length)
            throw Invalid("BLP JPEG header is outside the file");
        return file.AsSpan(offset + sizeof(uint), (int)length).ToArray();
    }

    private static int ReadDimension(byte[] file, int offset, string name)
    {
        var value = ReadUInt32(file, offset);
        if (value == 0 || value > int.MaxValue)
            throw Invalid($"invalid {name} {value}");
        return (int)value;
    }

    private static uint ReadUInt32(byte[] file, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(offset, sizeof(uint)));

    private static int PixelCount(int width, int height)
    {
        try
        {
            return checked(width * height);
        }
        catch (OverflowException exception)
        {
            throw Invalid("decoded dimensions overflow", exception);
        }
    }

    private static int PixelByteCount(int width, int height)
    {
        try
        {
            return checked(PixelCount(width, height) * 4);
        }
        catch (OverflowException exception)
        {
            throw Invalid("decoded byte count overflows", exception);
        }
    }

    private static void RequireLength(byte[] file, int length, string description)
    {
        if (file.Length < length)
            throw Invalid($"{description} is truncated");
    }

    private static void RequirePayload(
        ReadOnlySpan<byte> payload,
        int length,
        string description)
    {
        if (payload.Length < length)
            throw Invalid($"{description} is truncated: {payload.Length} bytes, expected {length}");
    }

    private static byte Expand4To8(byte value) => (byte)((value << 4) | value);

    private static byte Expand2To8(byte value) => (byte)(value * 85);

    private static byte Expand5To8(byte value) => (byte)((value << 3) | (value >> 2));

    private static byte Expand6To8(byte value) => (byte)((value << 2) | (value >> 4));

    private static InvalidDataException Invalid(string message, Exception? inner = null) =>
        new($"Invalid BLP: {message}.", inner);

    private readonly record struct MipPayload(int Offset, int Length);

    private enum BlpColorEncoding : byte
    {
        Jpeg = 0,
        Palette = 1,
        BlockCompressed = 2,
        Argb8888 = 3,
        Argb8888Duplicate = 4
    }

    private enum BlpPixelFormat : byte
    {
        Dxt1 = 0,
        Dxt3 = 1,
        Argb8888 = 2,
        Argb1555 = 3,
        Argb4444 = 4,
        Rgb565 = 5,
        A8 = 6,
        Dxt5 = 7,
        Unspecified = 8,
        Argb2565 = 9,
        Bc4 = 10,
        Bc5 = 11,
        Bc7 = 12
    }

    private enum BlockCompression
    {
        Bc1,
        Bc2,
        Bc3
    }
}
