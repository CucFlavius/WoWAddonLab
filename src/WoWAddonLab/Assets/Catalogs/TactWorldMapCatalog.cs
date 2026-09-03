namespace WoWAddonLab.Assets;

public sealed class TactWorldMapCatalog
{
    private readonly Dictionary<int, WdtMinimapTiles?> _tilesByMap = [];
    private readonly TactAssetSource _tact;

    private TactWorldMapCatalog(TactAssetSource tact, IReadOnlyList<WorldMapEntry> maps)
    {
        _tact = tact;
        Maps = maps;
    }

    public IReadOnlyList<WorldMapEntry> Maps { get; }

    public static TactWorldMapCatalog Load(TactAssetSource tact, string build)
    {
        var maps = tact.Database.Load("Map", build).Values
            .Select(row => new WorldMapEntry(
                Convert.ToInt32(row["ID"]),
                row["Directory"]?.ToString() ?? string.Empty,
                row["MapName_lang"]?.ToString() ?? string.Empty,
                Convert.ToInt32(row["MapType"]),
                Convert.ToInt32(row["InstanceType"]),
                Convert.ToInt32(row["ExpansionID"]),
                Convert.ToUInt32(row["WdtFileDataID"])))
            .Where(map => map.WdtFileDataId != 0)
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new TactWorldMapCatalog(tact, maps);
    }

    public WdtMinimapTiles? TilesFor(WorldMapEntry map)
    {
        if (_tilesByMap.TryGetValue(map.Id, out var cached))
            return cached;

        WdtMinimapTiles? tiles = null;
        if (_tact.Read(map.WdtFileDataId) is { } wdt &&
            WdtMinimapTiles.TryRead(wdt, out var parsed) &&
            !parsed.IsEmpty)
        {
            tiles = parsed;
        }
        _tilesByMap[map.Id] = tiles;
        return tiles;
    }
}
