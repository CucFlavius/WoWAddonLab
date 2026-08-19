using WoWAddonLab.Assets;
using WoWAddonLab.Emulator.UI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WoWAddonLab.Tests;

public sealed class TextureMipChainTests
{
    [Fact]
    public void BlpDecoderPreservesEveryStoredMipPayload()
    {
        var decoded = TextureImageDecoder.Decode(CreateArgbBlp2());

        Assert.Equal(3, decoded.MipLevels.Count);
        AssertLevel(decoded.MipLevels[0], 4, 4, [10, 20, 30, 40]);
        AssertLevel(decoded.MipLevels[1], 2, 2, [50, 60, 70, 80]);
        AssertLevel(decoded.MipLevels[2], 1, 1, [90, 100, 110, 120]);
    }

    [Fact]
    public void BlpDecoderPreservesStoredOnePixelAxisMipTail()
    {
        var decoded = TextureImageDecoder.Decode(CreateThinArgbBlp2());

        Assert.Equal(3, decoded.MipLevels.Count);
        AssertLevel(decoded.MipLevels[0], 4, 1, [10, 20, 30, 40]);
        AssertLevel(decoded.MipLevels[1], 2, 1, [50, 60, 70, 80]);
        AssertLevel(decoded.MipLevels[2], 1, 1, [90, 100, 110, 120]);
    }

    [Fact]
    public void BlpDecoderDecodesBc1Bc2AndBc3BlocksToRgba()
    {
        var bc1 = new byte[8];
        WriteUInt16(bc1, 0, 0xF800);
        WriteUInt16(bc1, 2, 0x07E0);
        AssertLevel(
            TextureImageDecoder.Decode(
                CreateBlp2(2, 0, 0, 4, 4, bc1)).BaseLevel,
            4,
            4,
            [255, 0, 0, 255]);

        var bc2 = new byte[16];
        Array.Fill(bc2, (byte)0xFF, 0, 8);
        WriteUInt16(bc2, 8, 0x001F);
        WriteUInt16(bc2, 10, 0x07E0);
        AssertLevel(
            TextureImageDecoder.Decode(
                CreateBlp2(2, 8, 1, 4, 4, bc2)).BaseLevel,
            4,
            4,
            [0, 0, 255, 255]);

        var bc3 = new byte[16];
        bc3[0] = 128;
        bc3[1] = 64;
        WriteUInt16(bc3, 8, 0xFFFF);
        WriteUInt16(bc3, 10, 0x0000);
        AssertLevel(
            TextureImageDecoder.Decode(
                CreateBlp2(2, 8, 7, 4, 4, bc3)).BaseLevel,
            4,
            4,
            [255, 255, 255, 128]);
    }

    [Fact]
    public void BlpDecoderDecodesBc5ChannelsToRgba()
    {
        var bc5 = new byte[16];
        bc5[0] = 200;
        bc5[1] = 0;
        bc5[8] = 100;
        bc5[9] = 0;

        AssertLevel(
            TextureImageDecoder.Decode(
                CreateBlp2(2, 8, 11, 4, 4, bc5)).BaseLevel,
            4,
            4,
            [200, 100, 0, 255]);
    }

    [Fact]
    public void BlpDecoderAcceptsUndersizedCompressedMipPayloads()
    {
        var payload = new byte[8];
        WriteUInt16(payload, 0, 0xF800);
        WriteUInt16(payload, 2, 0x07E0);

        var decoded = TextureImageDecoder.Decode(
            CreateBlp2(2, 0, 0, 8, 8, payload)).BaseLevel;

        Assert.Equal(new byte[] { 255, 0, 0, 255 }, decoded.Pixels[..4]);
        Assert.Equal(8 * 8 * 4, decoded.Pixels.Length);
    }

    [Fact]
    public void BlpDecoderSupportsBc7()
    {
        var decoded = TextureImageDecoder.Decode(
            CreateBlp2(2, 8, 12, 4, 4, new byte[16]));

        Assert.Equal(4, decoded.BaseLevel.Width);
        Assert.Equal(4, decoded.BaseLevel.Height);
        Assert.All(decoded.BaseLevel.Pixels, value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(3, new byte[] { 0x00, 0xFC }, new byte[] { 255, 0, 0, 255 })]
    [InlineData(4, new byte[] { 0xF0, 0xF0 }, new byte[] { 0, 255, 0, 255 })]
    [InlineData(5, new byte[] { 0x1F, 0x00 }, new byte[] { 0, 0, 255, 255 })]
    [InlineData(6, new byte[] { 0x80 }, new byte[] { 255, 255, 255, 128 })]
    [InlineData(9, new byte[] { 0x00, 0xF8, 0x02 }, new byte[] { 255, 0, 0, 170 })]
    public void BlpDecoderSupportsPackedRawFormats(
        byte format,
        byte[] payload,
        byte[] expected)
    {
        var decoded = TextureImageDecoder.Decode(
            CreateBlp2(3, 8, format, 1, 1, payload));

        Assert.Equal(expected, decoded.BaseLevel.Pixels);
    }

    [Fact]
    public void BlpDecoderUsesNativeFourBitPaletteAlphaExpansion()
    {
        var palette = new byte[256 * 4];
        palette[0] = 30;
        palette[1] = 20;
        palette[2] = 10;
        palette[3] = 99;
        var decoded = TextureImageDecoder.Decode(
            CreateBlp2(1, 4, 8, 2, 1, [0, 0, 0xF0], palette));

        Assert.Equal(2, decoded.BaseLevel.Width);
        Assert.Equal(1, decoded.BaseLevel.Height);
        Assert.Equal(
            new byte[] { 10, 20, 30, 0, 10, 20, 30, 255 },
            decoded.BaseLevel.Pixels);
    }

    [Fact]
    public void BlpDecoderRejectsMipPayloadOutsideFile()
    {
        var encoded = CreateArgbBlp2();
        BitConverter.GetBytes((uint)(encoded.Length + 1)).CopyTo(encoded, 20);

        var exception = Assert.Throws<InvalidDataException>(
            () => TextureImageDecoder.Decode(encoded));
        Assert.Contains("mip 0 payload is outside the file", exception.Message);
    }

    [Fact]
    public void BlpDecoderDecodesLegacyJpegPayload()
    {
        using var source = new Image<Rgba32>(2, 1);
        source[0, 0] = new Rgba32(255, 0, 0, 255);
        source[1, 0] = new Rgba32(0, 255, 0, 255);
        using var jpeg = new MemoryStream();
        source.SaveAsJpeg(jpeg);

        var decoded = TextureImageDecoder.Decode(CreateJpegBlp1(2, 1, jpeg.ToArray()));

        Assert.Equal(2, decoded.BaseLevel.Width);
        Assert.Equal(1, decoded.BaseLevel.Height);
        Assert.Equal(8, decoded.BaseLevel.Pixels.Length);
        Assert.Equal(255, decoded.BaseLevel.Pixels[3]);
        Assert.Equal(255, decoded.BaseLevel.Pixels[7]);
    }

    [Fact]
    public void DecodedImageCacheRoundTripsEveryMipPayload()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"WoWAddonLabMipCache-{Guid.NewGuid():N}");
        try
        {
            var encoded = CreateArgbBlp2();
            var expected = TextureImageDecoder.Decode(encoded);
            var cache = new DecodedImageCache(directory);

            cache.Write(encoded, expected);

            Assert.True(cache.TryRead(encoded, out var actual));
            Assert.Equal(expected.MipLevels.Count, actual.MipLevels.Count);
            for (var index = 0; index < expected.MipLevels.Count; index++)
            {
                Assert.Equal(expected.MipLevels[index].Width, actual.MipLevels[index].Width);
                Assert.Equal(expected.MipLevels[index].Height, actual.MipLevels[index].Height);
                Assert.Equal(expected.MipLevels[index].Pixels, actual.MipLevels[index].Pixels);
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SingleLevelTextureDowngradesNativeMipFiltersWithoutGeneratingPixels()
    {
        var anisotropic = UiTextureSamplerState.Resolve(
            UiTextureFilterMode.Anisotropic,
            UiTextureWrapMode.Repeat,
            UiTextureWrapMode.Clamp) with
        {
            MaxAnisotropy = 16
        };

        var singleLevel = anisotropic.ResolveForAvailableMipLevels(1);
        Assert.Equal(UiTextureFilterMode.Linear, singleLevel.Filter);
        Assert.Equal(1, singleLevel.MaxAnisotropy);
        Assert.Equal(anisotropic.AddressU, singleLevel.AddressU);
        Assert.Equal(anisotropic.AddressV, singleLevel.AddressV);
        Assert.Equal(anisotropic, anisotropic.ResolveForAvailableMipLevels(2));

        Assert.Equal(
            UiTextureFilterMode.Linear,
            (anisotropic with { Filter = UiTextureFilterMode.Bilinear })
                .ResolveForAvailableMipLevels(1)
                .Filter);
        Assert.Equal(
            UiTextureFilterMode.Linear,
            (anisotropic with { Filter = UiTextureFilterMode.Trilinear })
                .ResolveForAvailableMipLevels(1)
                .Filter);
        Assert.Equal(
            UiTextureFilterMode.Nearest,
            (anisotropic with { Filter = UiTextureFilterMode.Nearest })
                .ResolveForAvailableMipLevels(1)
                .Filter);
    }

    [Theory]
    [InlineData(8192, 8192, 14, 0, 0, false, 0)]
    [InlineData(8192, 8192, 14, 12, 0, false, 1)]
    [InlineData(16384, 8192, 15, 15, 0, false, 2)]
    [InlineData(1024, 512, 11, 12, 1, false, 1)]
    [InlineData(1024, 512, 11, 12, 2, false, 2)]
    [InlineData(1024, 32, 11, 12, 2, false, 0)]
    [InlineData(1024, 512, 11, 12, 2, true, 0)]
    public void LeadingMipSelectionMatchesNativePriorityAndWorldBaseMipRules(
        int width,
        int height,
        int mipCount,
        byte loadPriority,
        uint worldBaseMip,
        bool bypassWorldBaseMip,
        int expected)
    {
        Assert.Equal(
            expected,
            WowTextureMipSelector.ResolveLeadingMipLevel(
                width,
                height,
                mipCount,
                new WowTextureMipResidency(
                    loadPriority,
                    worldBaseMip,
                    bypassWorldBaseMip)));
    }

    [Fact]
    public void ModelResidencyReadsNativeClampedWorldBaseMipCVar()
    {
        var cvars = new WoWAddonLab.Emulator.Lua.WowCVarState();
        Assert.Equal(
            new WowTextureMipResidency(0, 0),
            WowTextureMipResidency.ForModel(noMip: true, cvars));
        Assert.Equal(
            new WowTextureMipResidency(12, 0),
            WowTextureMipResidency.ForModel(noMip: false, cvars));

        cvars.TryGet("worldBaseMip", out var entry);
        entry.Value = "0x1";
        Assert.Equal(1u, WowTextureMipResidency.ReadWorldBaseMip(cvars));
        entry.Value = "7";
        Assert.Equal(2u, WowTextureMipResidency.ReadWorldBaseMip(cvars));
        entry.Value = "-1";
        Assert.Equal(2u, WowTextureMipResidency.ReadWorldBaseMip(cvars));
        entry.Value = "invalid";
        Assert.Equal(0u, WowTextureMipResidency.ReadWorldBaseMip(cvars));
        entry.Value = "1junk";
        Assert.Equal(1u, WowTextureMipResidency.ReadWorldBaseMip(cvars));
        entry.Value = "0x1junk";
        Assert.Equal(1u, WowTextureMipResidency.ReadWorldBaseMip(cvars));
        entry.Value = " 0x1";
        Assert.Equal(0u, WowTextureMipResidency.ReadWorldBaseMip(cvars));
    }

    [Theory]
    [InlineData("0", "2")]
    [InlineData("1", "1")]
    [InlineData("1.9", "1")]
    [InlineData("2", "0")]
    [InlineData(" 1.9suffix", "1")]
    [InlineData("invalid", "2")]
    [InlineData("-1", "2")]
    [InlineData("0x1p0", "1")]
    public void GraphicsTextureResolutionAppliesNativeDistinctPresetMapping(
        string graphicsLevel,
        string expectedWorldBaseMip)
    {
        var cvars = new WoWAddonLab.Emulator.Lua.WowCVarState();

        cvars.SetValue("graphicsTextureResolution", graphicsLevel);

        Assert.True(cvars.TryGet("worldBaseMip", out var worldBaseMip));
        Assert.Equal(expectedWorldBaseMip, worldBaseMip.Value);
    }

    [Fact]
    public void RejectedGraphicsTextureResolutionPreservesPriorValueAndDependents()
    {
        var cvars = new WoWAddonLab.Emulator.Lua.WowCVarState();
        cvars.SetValue("graphicsTextureResolution", "1");

        cvars.SetValue("graphicsTextureResolution", "3");

        Assert.True(cvars.TryGet("graphicsTextureResolution", out var resolution));
        Assert.Equal("1", resolution.Value);
        Assert.True(cvars.TryGet("worldBaseMip", out var worldBaseMip));
        Assert.Equal("1", worldBaseMip.Value);
    }

    [Fact]
    public void ConfigCVarCallbacksRunInImportedAssignmentOrder()
    {
        var cvars = new WoWAddonLab.Emulator.Lua.WowCVarState();

        cvars.ImportConfigLines(
        [
            "SET worldBaseMip \"2\"",
            "SET graphicsTextureResolution \"2\""
        ]);
        Assert.True(cvars.TryGet("worldBaseMip", out var worldBaseMip));
        Assert.Equal("0", worldBaseMip.Value);

        cvars.ImportConfigLines(
        [
            "SET graphicsTextureResolution \"0\"",
            "SET worldBaseMip \"1\""
        ]);
        Assert.Equal("1", worldBaseMip.Value);
    }

    private static void AssertLevel(
        DecodedTextureMipLevel level,
        int width,
        int height,
        byte[] rgba)
    {
        Assert.Equal(width, level.Width);
        Assert.Equal(height, level.Height);
        Assert.Equal(width * height * 4, level.Pixels.Length);
        for (var offset = 0; offset < level.Pixels.Length; offset += 4)
            Assert.Equal(rgba, level.Pixels[offset..(offset + 4)]);
    }

    private static byte[] CreateArgbBlp2()
    {
        var levels = new[]
        {
            CreateBgraLevel(4, 4, 10, 20, 30, 40),
            CreateBgraLevel(2, 2, 50, 60, 70, 80),
            CreateBgraLevel(1, 1, 90, 100, 110, 120)
        };
        return CreateArgbBlp2(4, 4, levels);
    }

    private static byte[] CreateThinArgbBlp2()
    {
        var levels = new[]
        {
            CreateBgraLevel(4, 1, 10, 20, 30, 40),
            CreateBgraLevel(2, 1, 50, 60, 70, 80),
            CreateBgraLevel(1, 1, 90, 100, 110, 120)
        };
        return CreateArgbBlp2(4, 1, levels);
    }

    private static byte[] CreateArgbBlp2(
        int width,
        int height,
        IReadOnlyList<byte[]> levels)
    {
        const int headerSize = 148;
        var offsets = new uint[16];
        var sizes = new uint[16];
        var nextOffset = headerSize;
        for (var index = 0; index < levels.Count; index++)
        {
            offsets[index] = (uint)nextOffset;
            sizes[index] = (uint)levels[index].Length;
            nextOffset += levels[index].Length;
        }

        using var stream = new MemoryStream(nextOffset);
        using var writer = new BinaryWriter(stream);
        writer.Write(0x32504C42u);
        writer.Write(1u);
        writer.Write((byte)3);
        writer.Write((byte)8);
        writer.Write((byte)2);
        writer.Write((byte)1);
        writer.Write(width);
        writer.Write(height);
        foreach (var offset in offsets)
            writer.Write(offset);
        foreach (var size in sizes)
            writer.Write(size);
        foreach (var level in levels)
            writer.Write(level);
        return stream.ToArray();
    }

    private static byte[] CreateBlp2(
        byte encoding,
        byte alphaDepth,
        byte preferredFormat,
        int width,
        int height,
        byte[] payload,
        byte[]? palette = null)
    {
        const int headerSize = 148;
        var payloadOffset = checked(headerSize + (palette?.Length ?? 0));
        using var stream = new MemoryStream(payloadOffset + payload.Length);
        using var writer = new BinaryWriter(stream);
        writer.Write(0x32504C42u);
        writer.Write(1u);
        writer.Write(encoding);
        writer.Write(alphaDepth);
        writer.Write(preferredFormat);
        writer.Write((byte)1);
        writer.Write(width);
        writer.Write(height);
        writer.Write((uint)payloadOffset);
        for (var index = 1; index < 16; index++)
            writer.Write(0u);
        writer.Write((uint)payload.Length);
        for (var index = 1; index < 16; index++)
            writer.Write(0u);
        if (palette is not null)
            writer.Write(palette);
        writer.Write(payload);
        return stream.ToArray();
    }

    private static byte[] CreateJpegBlp1(int width, int height, byte[] jpeg)
    {
        const int headerSize = 156;
        const int sharedHeaderSizeField = sizeof(uint);
        var payloadOffset = headerSize + sharedHeaderSizeField;
        using var stream = new MemoryStream(payloadOffset + jpeg.Length);
        using var writer = new BinaryWriter(stream);
        writer.Write(0x31504C42u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(width);
        writer.Write(height);
        writer.Write(0u);
        writer.Write(1u);
        writer.Write((uint)payloadOffset);
        for (var index = 1; index < 16; index++)
            writer.Write(0u);
        writer.Write((uint)jpeg.Length);
        for (var index = 1; index < 16; index++)
            writer.Write(0u);
        writer.Write(0u);
        writer.Write(jpeg);
        return stream.ToArray();
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)value;
        destination[offset + 1] = (byte)(value >> 8);
    }

    private static byte[] CreateBgraLevel(
        int width,
        int height,
        byte red,
        byte green,
        byte blue,
        byte alpha)
    {
        var pixels = new byte[width * height * 4];
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = blue;
            pixels[offset + 1] = green;
            pixels[offset + 2] = red;
            pixels[offset + 3] = alpha;
        }
        return pixels;
    }
}
