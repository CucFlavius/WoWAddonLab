using System.Buffers.Binary;

namespace WoWAddonLab.Rendering;

internal readonly record struct TrueTypeFontMetrics(
    ushort UnitsPerEm,
    short Ascender,
    short Descender)
{
    private const uint HeadTag = 0x68656164;
    private const uint HheaTag = 0x68686561;

    public float LineMetricUnits => Ascender + MathF.Abs(Descender);

    public float ResolveImGuiGlyphSize(float nativePixelHeight) =>
        nativePixelHeight * LineMetricUnits / UnitsPerEm;

    public float ResolveImGuiGlyphOffsetY(float nativePixelHeight)
    {
        var nativeBaseline = MathF.Truncate(
            nativePixelHeight * Ascender / LineMetricUnits + 0.5f);
        var imguiBaseline = MathF.Ceiling(
            nativePixelHeight * Ascender / UnitsPerEm);
        return nativeBaseline - imguiBaseline;
    }

    public static bool TryRead(ReadOnlySpan<byte> data, out TrueTypeFontMetrics metrics)
    {
        metrics = default;
        if (data.Length < 12)
            return false;

        var tableCount = BinaryPrimitives.ReadUInt16BigEndian(data[4..6]);
        var directoryLength = 12L + tableCount * 16L;
        if (directoryLength > data.Length)
            return false;

        var headOffset = -1;
        var hheaOffset = -1;
        for (var index = 0; index < tableCount; index++)
        {
            var entryOffset = 12 + index * 16;
            var tag = BinaryPrimitives.ReadUInt32BigEndian(
                data.Slice(entryOffset, 4));
            var tableOffset = BinaryPrimitives.ReadUInt32BigEndian(
                data.Slice(entryOffset + 8, 4));
            var tableLength = BinaryPrimitives.ReadUInt32BigEndian(
                data.Slice(entryOffset + 12, 4));
            if (tableOffset > int.MaxValue ||
                tableLength > int.MaxValue ||
                (long)tableOffset + tableLength > data.Length)
            {
                continue;
            }

            if (tag == HeadTag && tableLength >= 20)
                headOffset = (int)tableOffset;
            else if (tag == HheaTag && tableLength >= 8)
                hheaOffset = (int)tableOffset;
        }

        if (headOffset < 0 || hheaOffset < 0)
            return false;

        var unitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(
            data.Slice(headOffset + 18, 2));
        var ascender = BinaryPrimitives.ReadInt16BigEndian(
            data.Slice(hheaOffset + 4, 2));
        var descender = BinaryPrimitives.ReadInt16BigEndian(
            data.Slice(hheaOffset + 6, 2));
        var lineMetricUnits = ascender + Math.Abs((int)descender);
        if (unitsPerEm == 0 || ascender <= 0 || lineMetricUnits <= 0)
            return false;

        metrics = new TrueTypeFontMetrics(unitsPerEm, ascender, descender);
        return true;
    }
}
