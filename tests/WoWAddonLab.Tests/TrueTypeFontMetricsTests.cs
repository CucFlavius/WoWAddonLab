using System.Buffers.Binary;
using WoWAddonLab.Rendering;

namespace WoWAddonLab.Tests;

public sealed class TrueTypeFontMetricsTests
{
    [Fact]
    public void ResolvesWowNominalEmRasterSizeAndIndependentBaseline()
    {
        var data = BuildFontMetrics(
            unitsPerEm: 1000,
            ascender: 965,
            descender: -250);

        Assert.True(TrueTypeFontMetrics.TryRead(data, out var metrics));
        Assert.Equal(1000, metrics.UnitsPerEm);
        Assert.Equal(965, metrics.Ascender);
        Assert.Equal(-250, metrics.Descender);
        Assert.Equal(14.58f, metrics.ResolveImGuiGlyphSize(12), 3);
        Assert.Equal(-2, metrics.ResolveImGuiGlyphOffsetY(12));
    }

    private static byte[] BuildFontMetrics(
        ushort unitsPerEm,
        short ascender,
        short descender)
    {
        const int headOffset = 44;
        const int headLength = 54;
        const int hheaOffset = headOffset + headLength;
        const int hheaLength = 36;
        var data = new byte[hheaOffset + hheaLength];
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(4, 2), 2);
        WriteTable(data, 12, 0x68656164, headOffset, headLength);
        WriteTable(data, 28, 0x68686561, hheaOffset, hheaLength);
        BinaryPrimitives.WriteUInt16BigEndian(
            data.AsSpan(headOffset + 18, 2),
            unitsPerEm);
        BinaryPrimitives.WriteInt16BigEndian(
            data.AsSpan(hheaOffset + 4, 2),
            ascender);
        BinaryPrimitives.WriteInt16BigEndian(
            data.AsSpan(hheaOffset + 6, 2),
            descender);
        return data;
    }

    private static void WriteTable(
        Span<byte> data,
        int directoryOffset,
        uint tag,
        int tableOffset,
        int tableLength)
    {
        BinaryPrimitives.WriteUInt32BigEndian(
            data.Slice(directoryOffset, 4),
            tag);
        BinaryPrimitives.WriteUInt32BigEndian(
            data.Slice(directoryOffset + 8, 4),
            (uint)tableOffset);
        BinaryPrimitives.WriteUInt32BigEndian(
            data.Slice(directoryOffset + 12, 4),
            (uint)tableLength);
    }
}
