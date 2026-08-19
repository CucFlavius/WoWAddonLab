using DBCD;
using DBCD.Providers;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Assets;

public sealed class TactAtlasCatalog : TactCatalog, IWowAtlasProvider, IWowFileDataNameProvider
{
    private readonly TactAssetSource _tact;
    private readonly Dictionary<string, WowAtlasInfo> _atlases;

    private TactAtlasCatalog(
        TactAssetSource tact,
        Dictionary<string, WowAtlasInfo> atlases)
    {
        _tact = tact;
        _atlases = atlases;
    }

    public int Count => _atlases.Count;

    public bool TryGetAtlas(string name, out WowAtlasInfo info) =>
        _atlases.TryGetValue(name, out info!);

    public IEnumerable<WowAtlasInfo> EnumerateAtlases() => _atlases.Values;

    public bool TryGetFilename(uint fileDataId, out string filename) =>
        _tact.TryGetFilename(fileDataId, out filename);

    public static TactAtlasCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var canvasRows = database.Load("UiCanvas", build).Values
            .ToDictionary(row => Integer(row, "ID"));
        var atlasRows = database.Load("UiTextureAtlas", build).Values
            .ToDictionary(row => Integer(row, "ID"));
        var elementRows = database.Load("UiTextureAtlasElement", build).Values
            .ToDictionary(row => Integer(row, "ID"));
        var sliceRows = database.Load("UiTextureAtlasElementSliceData", build).Values
            .GroupBy(row => Integer(row, "UiTextureAtlasElementID"))
            .ToDictionary(group => group.Key, group => group.First());
        var baseCanvasWidth = canvasRows.Values
            .Select(row => Number(row, "Width"))
            .Where(width => width > 0)
            .DefaultIfEmpty(1024)
            .Min();
        var baseCanvasHeight = canvasRows.Values
            .Select(row => Number(row, "Height"))
            .Where(height => height > 0)
            .DefaultIfEmpty(768)
            .Min();
        var results = new Dictionary<string, AtlasCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in database.Load("UiTextureAtlasMember", build).Values)
        {
            var atlasMemberId = Integer(member, "ID");
            var atlasId = Integer(member, "UiTextureAtlasID");
            var elementId = Integer(member, "UiTextureAtlasElementID");
            if (!atlasRows.TryGetValue(atlasId, out var atlas) ||
                !elementRows.TryGetValue(elementId, out var element))
                continue;

            var name = Text(element, "Name");
            if (string.IsNullOrWhiteSpace(name))
                name = Text(member, "CommittedName");
            var atlasWidth = Number(atlas, "AtlasWidth");
            var atlasHeight = Number(atlas, "AtlasHeight");
            if (string.IsNullOrWhiteSpace(name) || atlasWidth <= 0 || atlasHeight <= 0)
                continue;

            var left = Number(member, "CommittedLeft");
            var right = Number(member, "CommittedRight");
            var top = Number(member, "CommittedTop");
            var bottom = Number(member, "CommittedBottom");
            var overrideWidth = Number(member, "OverrideWidth");
            var overrideHeight = Number(member, "OverrideHeight");
            var rawWidth = Number(member, "Width");
            var rawHeight = Number(member, "Height");
            if (rawWidth <= 0)
                rawWidth = Math.Abs(right - left);
            if (rawHeight <= 0)
                rawHeight = Math.Abs(bottom - top);
            var canvasId = Integer(member, "UiCanvasID");
            if (canvasId <= 0)
                canvasId = Integer(atlas, "UiCanvasID");
            var canvasScaleX = 1f;
            var canvasScaleY = 1f;
            if (canvasRows.TryGetValue(canvasId, out var canvas))
            {
                canvasScaleX = Math.Max(1, Number(canvas, "Width") / baseCanvasWidth);
                canvasScaleY = Math.Max(1, Number(canvas, "Height") / baseCanvasHeight);
            }
            var width = WowAtlasMetrics.ResolveLogicalDimension(
                rawWidth,
                overrideWidth,
                canvasScaleX);
            var height = WowAtlasMetrics.ResolveLogicalDimension(
                rawHeight,
                overrideHeight,
                canvasScaleY);
            var flags = Integer(member, "CommittedFlags");
            UiTextureSliceData? sliceData = null;
            if (sliceRows.TryGetValue(elementId, out var slice))
            {
                sliceData = new UiTextureSliceData(
                    Number(slice, "MarginLeft", "Left"),
                    Number(slice, "MarginTop", "Top"),
                    Number(slice, "MarginRight", "Right"),
                    Number(slice, "MarginBottom", "Bottom"),
                    Integer(slice, "SliceMode") == 1
                        ? UiTextureSliceMode.Tiled
                        : UiTextureSliceMode.Stretched);
            }
            var canTile = (flags & 0x10) == 0;

            var info = new WowAtlasInfo(
                name,
                (uint)Integer(atlas, "FileDataID"),
                width,
                height,
                rawWidth,
                rawHeight,
                left / atlasWidth,
                right / atlasWidth,
                top / atlasHeight,
                bottom / atlasHeight,
                canTile && (flags & 4) != 0,
                canTile && (flags & 2) != 0,
                sliceData,
                AtlasId: atlasMemberId,
                ElementId: elementId);
            var density = Math.Max(canvasScaleX, canvasScaleY);
            if (!results.TryGetValue(name, out var existing) || density >= existing.Density)
                results[name] = new AtlasCandidate(info, density);
        }

        return new TactAtlasCatalog(
            tact,
            results.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Info,
                StringComparer.OrdinalIgnoreCase));
    }



    private static float Number(dynamic row, params string[] names)
    {
        foreach (var name in names)
        {
            if (Field(row, name) is { } value)
                return Convert.ToSingle(value);
        }
        return 0;
    }


    private sealed record AtlasCandidate(WowAtlasInfo Info, float Density);
}
