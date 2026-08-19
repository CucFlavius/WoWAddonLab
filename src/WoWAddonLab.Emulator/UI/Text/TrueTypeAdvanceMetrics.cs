using System.Buffers.Binary;
using System.Text;

namespace WoWAddonLab.Emulator.UI;

public sealed class TrueTypeAdvanceMetrics
{
    private const uint CmapTag = 0x636D6170;
    private const uint HeadTag = 0x68656164;
    private const uint HheaTag = 0x68686561;
    private const uint HmtxTag = 0x686D7478;
    private const uint MaxpTag = 0x6D617870;

    private readonly byte[] _data;
    private readonly int _cmapOffset;
    private readonly ushort _cmapFormat;
    private readonly ushort[] _advances;

    private TrueTypeAdvanceMetrics(
        byte[] data,
        ushort unitsPerEm,
        int cmapOffset,
        ushort cmapFormat,
        ushort[] advances)
    {
        _data = data;
        UnitsPerEm = unitsPerEm;
        _cmapOffset = cmapOffset;
        _cmapFormat = cmapFormat;
        _advances = advances;
    }

    public ushort UnitsPerEm { get; }

    public float MeasureAdvance(string text, float emPixelHeight)
    {
        if (string.IsNullOrEmpty(text) || !(emPixelHeight > 0))
            return 0;

        long units = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var glyph = MapGlyph(rune.Value);
            if (glyph == 0 && rune.Value != 0x3F)
                glyph = MapGlyph(0x3F);
            if ((uint)glyph < (uint)_advances.Length)
                units += _advances[glyph];
        }
        return units * emPixelHeight / UnitsPerEm;
    }

    public static bool TryRead(ReadOnlySpan<byte> data, out TrueTypeAdvanceMetrics? metrics)
    {
        metrics = null;
        if (data.Length < 12)
            return false;

        var tableCount = ReadU16(data, 4);
        if (12L + tableCount * 16L > data.Length)
            return false;

        if (!TryFindTable(data, tableCount, HeadTag, out var head, out var headLength) ||
            headLength < 20 ||
            !TryFindTable(data, tableCount, HheaTag, out var hhea, out var hheaLength) ||
            hheaLength < 36 ||
            !TryFindTable(data, tableCount, MaxpTag, out var maxp, out var maxpLength) ||
            maxpLength < 6 ||
            !TryFindTable(data, tableCount, HmtxTag, out var hmtx, out var hmtxLength) ||
            !TryFindTable(data, tableCount, CmapTag, out var cmap, out var cmapLength) ||
            cmapLength < 4)
        {
            return false;
        }

        var unitsPerEm = ReadU16(data, head + 18);
        var longMetricCount = ReadU16(data, hhea + 34);
        var glyphCount = ReadU16(data, maxp + 4);
        if (unitsPerEm == 0 || glyphCount == 0 || longMetricCount == 0 ||
            longMetricCount > glyphCount || hmtxLength < longMetricCount * 4L)
        {
            return false;
        }

        var advances = new ushort[glyphCount];
        ushort lastAdvance = 0;
        for (var glyph = 0; glyph < glyphCount; glyph++)
        {
            if (glyph < longMetricCount)
                lastAdvance = ReadU16(data, hmtx + glyph * 4);
            advances[glyph] = lastAdvance;
        }

        if (!TrySelectCmap(data, cmap, cmapLength, out var subtable, out var format))
            return false;

        metrics = new TrueTypeAdvanceMetrics(
            data.ToArray(),
            unitsPerEm,
            subtable,
            format,
            advances);
        return true;
    }

    private int MapGlyph(int codePoint) => _cmapFormat switch
    {
        12 => MapFormat12(codePoint),
        4 when codePoint <= ushort.MaxValue => MapFormat4(codePoint),
        _ => 0
    };

    private int MapFormat12(int codePoint)
    {
        var groups = (int)ReadU32(_data, _cmapOffset + 12);
        var low = 0;
        var high = groups - 1;
        while (low <= high)
        {
            var middle = low + (high - low) / 2;
            var entry = _cmapOffset + 16 + middle * 12;
            var start = ReadU32(_data, entry);
            var end = ReadU32(_data, entry + 4);
            if ((uint)codePoint < start)
                high = middle - 1;
            else if ((uint)codePoint > end)
                low = middle + 1;
            else
            {
                var glyph = ReadU32(_data, entry + 8) + (uint)codePoint - start;
                return glyph <= int.MaxValue ? (int)glyph : 0;
            }
        }
        return 0;
    }

    private int MapFormat4(int codePoint)
    {
        var segmentCount = ReadU16(_data, _cmapOffset + 6) / 2;
        var endCodes = _cmapOffset + 14;
        var startCodes = endCodes + segmentCount * 2 + 2;
        var idDeltas = startCodes + segmentCount * 2;
        var idRangeOffsets = idDeltas + segmentCount * 2;
        for (var index = 0; index < segmentCount; index++)
        {
            var end = ReadU16(_data, endCodes + index * 2);
            if (codePoint > end)
                continue;
            var start = ReadU16(_data, startCodes + index * 2);
            if (codePoint < start)
                return 0;
            var delta = ReadI16(_data, idDeltas + index * 2);
            var rangeOffsetAddress = idRangeOffsets + index * 2;
            var rangeOffset = ReadU16(_data, rangeOffsetAddress);
            if (rangeOffset == 0)
                return (codePoint + delta) & 0xFFFF;
            var glyphAddress = rangeOffsetAddress + rangeOffset + (codePoint - start) * 2;
            if (glyphAddress < 0 || glyphAddress + 2 > _data.Length)
                return 0;
            var glyph = ReadU16(_data, glyphAddress);
            return glyph == 0 ? 0 : (glyph + delta) & 0xFFFF;
        }
        return 0;
    }

    private static bool TrySelectCmap(
        ReadOnlySpan<byte> data,
        int cmap,
        int cmapLength,
        out int selectedOffset,
        out ushort selectedFormat)
    {
        selectedOffset = 0;
        selectedFormat = 0;
        var count = ReadU16(data, cmap + 2);
        if (4L + count * 8L > cmapLength)
            return false;

        var selectedRank = -1;
        for (var index = 0; index < count; index++)
        {
            var record = cmap + 4 + index * 8;
            var platform = ReadU16(data, record);
            var encoding = ReadU16(data, record + 2);
            var relative = ReadU32(data, record + 4);
            if (relative > int.MaxValue || relative + 2 > cmapLength)
                continue;
            var offset = cmap + (int)relative;
            var format = ReadU16(data, offset);
            if (!IsValidCmapSubtable(data, offset, format, cmap + cmapLength))
                continue;
            var rank = format switch
            {
                12 when platform == 3 && encoding == 10 => 5,
                12 when platform == 0 => 4,
                4 when platform == 3 && (encoding == 1 || encoding == 10) => 3,
                4 when platform == 0 => 2,
                _ => -1
            };
            if (rank <= selectedRank)
                continue;
            selectedRank = rank;
            selectedOffset = offset;
            selectedFormat = format;
        }
        return selectedRank >= 0;
    }

    private static bool IsValidCmapSubtable(
        ReadOnlySpan<byte> data,
        int offset,
        ushort format,
        int cmapEnd)
    {
        if (format == 12)
        {
            if (offset + 16 > cmapEnd || offset + 16 > data.Length)
                return false;
            var length = ReadU32(data, offset + 4);
            var groups = ReadU32(data, offset + 12);
            return length >= 16 &&
                   length <= int.MaxValue &&
                   groups <= int.MaxValue &&
                   16L + groups * 12L <= length &&
                   (long)offset + length <= cmapEnd &&
                   (long)offset + length <= data.Length;
        }

        if (format == 4)
        {
            if (offset + 14 > cmapEnd || offset + 14 > data.Length)
                return false;
            var length = ReadU16(data, offset + 2);
            var segmentBytes = ReadU16(data, offset + 6);
            return length >= 16 &&
                   segmentBytes >= 2 &&
                   (segmentBytes & 1) == 0 &&
                   16L + segmentBytes * 4L <= length &&
                   offset + length <= cmapEnd &&
                   offset + length <= data.Length;
        }

        return false;
    }

    private static bool TryFindTable(
        ReadOnlySpan<byte> data,
        int tableCount,
        uint tag,
        out int offset,
        out int length)
    {
        offset = 0;
        length = 0;
        for (var index = 0; index < tableCount; index++)
        {
            var entry = 12 + index * 16;
            if (ReadU32(data, entry) != tag)
                continue;
            var tableOffset = ReadU32(data, entry + 8);
            var tableLength = ReadU32(data, entry + 12);
            if (tableOffset > int.MaxValue || tableLength > int.MaxValue ||
                (long)tableOffset + tableLength > data.Length)
            {
                return false;
            }
            offset = (int)tableOffset;
            length = (int)tableLength;
            return true;
        }
        return false;
    }

    private static ushort ReadU16(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));

    private static short ReadI16(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadInt16BigEndian(data.Slice(offset, 2));

    private static uint ReadU32(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
}
