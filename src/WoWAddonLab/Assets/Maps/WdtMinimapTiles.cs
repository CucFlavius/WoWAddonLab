using System.Buffers.Binary;

namespace WoWAddonLab.Assets;

public sealed class WdtMinimapTiles
{
    public const int Side = 64;
    private const int EntrySize = 32;
    private const int MinimapOffset = 28;

    private readonly uint[] _tiles;

    private WdtMinimapTiles(uint[] tiles, int minimumX, int minimumY, int maximumX, int maximumY)
    {
        _tiles = tiles;
        MinimumX = minimumX;
        MinimumY = minimumY;
        MaximumX = maximumX;
        MaximumY = maximumY;
    }

    public int MinimumX { get; }
    public int MinimumY { get; }
    public int MaximumX { get; }
    public int MaximumY { get; }
    public int Count => _tiles.Count(tile => tile != 0);
    public bool IsEmpty => MaximumX < MinimumX;

    public uint this[int x, int y] =>
        x < 0 || y < 0 || x >= Side || y >= Side ? 0 : _tiles[y * Side + x];

    public static bool TryRead(byte[] wdt, out WdtMinimapTiles tiles)
    {
        tiles = null!;
        var offset = 0;
        while (offset + 8 <= wdt.Length)
        {
            var magic = ReadMagic(wdt, offset);
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(wdt.AsSpan(offset + 4, 4));
            var payload = offset + 8;
            if (size < 0 || payload + size > wdt.Length)
                return false;
            if (magic == "MAID")
            {
                if (size < Side * Side * EntrySize)
                    return false;
                tiles = ReadTiles(wdt.AsSpan(payload, Side * Side * EntrySize));
                return true;
            }
            offset = payload + size;
        }
        return false;
    }

    private static WdtMinimapTiles ReadTiles(ReadOnlySpan<byte> payload)
    {
        var tiles = new uint[Side * Side];
        var minimumX = Side;
        var minimumY = Side;
        var maximumX = -1;
        var maximumY = -1;
        for (var index = 0; index < tiles.Length; index++)
        {
            var fileDataId = BinaryPrimitives.ReadUInt32LittleEndian(
                payload[(index * EntrySize + MinimapOffset)..]);
            tiles[index] = fileDataId;
            if (fileDataId == 0)
                continue;
            var x = index % Side;
            var y = index / Side;
            minimumX = Math.Min(minimumX, x);
            minimumY = Math.Min(minimumY, y);
            maximumX = Math.Max(maximumX, x);
            maximumY = Math.Max(maximumY, y);
        }
        return new WdtMinimapTiles(tiles, minimumX, minimumY, maximumX, maximumY);
    }

    private static string ReadMagic(byte[] bytes, int offset) =>
        new([
            (char)bytes[offset + 3],
            (char)bytes[offset + 2],
            (char)bytes[offset + 1],
            (char)bytes[offset]
        ]);
}
