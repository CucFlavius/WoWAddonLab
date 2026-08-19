using System.Collections;
using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactMapCatalog : TactCatalog, IWowMapProvider
{
    private readonly IReadOnlyDictionary<int, WowMapDetails> _maps;
    private readonly IReadOnlyDictionary<int, WowMapArt> _artByMap;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<WowMapLink>> _linksByParent;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<WowMapAssignment>> _assignmentsByMap;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<int>> _childMapIdsByParent;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<WowMapLink>>
        _assignmentRectanglesByParent;
    private readonly IReadOnlyDictionary<int, string> _areaNames;

    private TactMapCatalog(
        IReadOnlyDictionary<int, WowMapDetails> maps,
        IReadOnlyDictionary<int, WowMapArt> artByMap,
        IReadOnlyDictionary<int, IReadOnlyList<WowMapLink>> linksByParent,
        IReadOnlyDictionary<int, IReadOnlyList<WowMapAssignment>> assignmentsByMap,
        IReadOnlyDictionary<int, string> areaNames)
    {
        _maps = maps;
        _artByMap = artByMap;
        _linksByParent = linksByParent;
        _assignmentsByMap = assignmentsByMap;
        _areaNames = areaNames;
        _childMapIdsByParent = maps.Values
            .Where(map => map.ParentMapId > 0)
            .GroupBy(map => map.ParentMapId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group
                    .Select(map => map.MapId)
                    .Order()
                    .ToArray());
        _assignmentRectanglesByParent = BuildAssignmentRectangles();
    }

    public int MapCount => _maps.Count;
    public int ArtCount => _artByMap.Count;
    public int HighlightMapCount => HighlightParentMapIds()
        .Count(mapId => GetHighlightCount(mapId) > 0);
    public int HighlightCount => HighlightParentMapIds()
        .Sum(GetHighlightCount);

    public int GetHighlightCount(int mapId)
    {
        var linkCount = _linksByParent.TryGetValue(mapId, out var links)
            ? links.Count(link =>
                link.PlayerConditionId == 0 &&
                TryGetHighlightArt(link, out _))
            : 0;
        return linkCount + GetAssignmentHighlightCount(mapId);
    }

    public bool TryGetMapDetails(int mapId, out WowMapDetails details) =>
        _maps.TryGetValue(mapId, out details);

    public bool TryGetAreaName(int areaId, out string name) =>
        _areaNames.TryGetValue(areaId, out name!);

    public bool TryGetMapArt(int mapId, out WowMapArt art) =>
        _artByMap.TryGetValue(mapId, out art!);

    public bool TryGetMapAtPosition(
        int mapId,
        double x,
        double y,
        bool ignoreZoneMapPositionData,
        out WowMapDetails details)
    {
        details = default;
        if (TryGetLinkAtPosition(mapId, x, y, out var link))
            return _maps.TryGetValue(link.ChildMapId, out details);

        return TryGetAssignmentChildAtPosition(
            mapId,
            x,
            y,
            out details,
            out _);
    }

    public IReadOnlyList<WowMapDetails> GetMapChildren(
        int mapId,
        int? mapType = null,
        bool allDescendants = false)
    {
        var result = new List<WowMapDetails>();
        var pending = new Queue<int>();
        pending.Enqueue(mapId);
        while (pending.TryDequeue(out var parentMapId))
        {
            foreach (var child in _maps.Values
                         .Where(value => value.ParentMapId == parentMapId)
                         .OrderBy(value => value.MapId))
            {
                if (mapType is null || child.MapType == mapType)
                    result.Add(child);
                if (allDescendants)
                    pending.Enqueue(child.MapId);
            }
        }
        return result;
    }

    public bool TryGetMapHighlight(
        int mapId,
        double x,
        double y,
        out WowMapHighlight highlight)
    {
        highlight = default;
        if (TryGetLinkAtPosition(mapId, x, y, out var link))
        {
            if (!TryGetHighlightArt(link, out var linkedHighlightArt))
                return false;
            return TryBuildHighlight(
                link,
                linkedHighlightArt,
                link.MinimumX,
                link.MinimumY,
                link.MaximumX,
                link.MaximumY,
                out highlight);
        }

        if (!TryGetAssignmentChildAtPosition(
                mapId,
                x,
                y,
                out var details,
                out var rectangle) ||
            !TryGetHighlightArt(
                new WowMapLink(0, mapId, details.MapId, 0, 0, 0, 0, 0, 0),
                out var assignmentHighlightArt))
        {
            return false;
        }

        return TryBuildHighlight(
            new WowMapLink(
                0,
                mapId,
                details.MapId,
                0,
                rectangle.MinimumX,
                rectangle.MinimumY,
                rectangle.MaximumX,
                rectangle.MaximumY,
                0),
            assignmentHighlightArt,
            rectangle.MinimumX,
            rectangle.MinimumY,
            rectangle.MaximumX,
            rectangle.MaximumY,
            out highlight);
    }

    public bool TryProjectWorldPosition(
        int uiMapId,
        int worldMapId,
        double worldX,
        double worldY,
        out WowMapPosition position)
    {
        position = default;
        if (!_assignmentsByMap.TryGetValue(uiMapId, out var assignments))
            return false;

        foreach (var assignment in assignments)
        {
            if (assignment.MapId != worldMapId ||
                !WowMapHighlightGeometry.TryProjectWorldPositionToUi(
                    assignment,
                    worldX,
                    worldY,
                    out var x,
                    out var y))
            {
                continue;
            }

            position = new WowMapPosition(x, y);
            return true;
        }

        return false;
    }

    public bool TryMapPositionToWorld(
        int uiMapId,
        double x,
        double y,
        out int worldMapId,
        out WowMapPosition position)
    {
        worldMapId = 0;
        position = default;
        if (!_assignmentsByMap.TryGetValue(uiMapId, out var assignments))
            return false;

        foreach (var assignment in assignments)
        {
            var minimumX = Math.Min(assignment.UiMinimumX, assignment.UiMaximumX);
            var maximumX = Math.Max(assignment.UiMinimumX, assignment.UiMaximumX);
            var minimumY = Math.Min(assignment.UiMinimumY, assignment.UiMaximumY);
            var maximumY = Math.Max(assignment.UiMinimumY, assignment.UiMaximumY);
            if (x < minimumX || x > maximumX || y < minimumY || y > maximumY ||
                !WowMapHighlightGeometry.TryMapUiPositionToWorld(
                    assignment,
                    x,
                    y,
                    out var worldX,
                    out var worldY))
            {
                continue;
            }

            worldMapId = assignment.MapId;
            position = new WowMapPosition(worldX, worldY);
            return true;
        }

        return false;
    }

    public bool TryGetMapWorldSize(int uiMapId, out double width, out double height)
    {
        width = 0;
        height = 0;
        if (!_assignmentsByMap.TryGetValue(uiMapId, out var assignments))
            return false;

        foreach (var assignment in assignments)
        {
            var uiWidth = Math.Abs(assignment.UiMaximumX - assignment.UiMinimumX);
            var uiHeight = Math.Abs(assignment.UiMaximumY - assignment.UiMinimumY);
            if (uiWidth <= double.Epsilon || uiHeight <= double.Epsilon)
                continue;

            width = Math.Max(
                        assignment.RegionMinimumY - assignment.RegionMaximumY,
                        0) /
                    uiWidth;
            height = Math.Max(
                         assignment.RegionMinimumX - assignment.RegionMaximumX,
                         0) /
                     uiHeight;
            if (width > 0 && height > 0)
                return true;
        }

        width = 0;
        height = 0;
        return false;
    }

    public bool TryGetMapRectangle(
        int uiMapId,
        int topUiMapId,
        out double minimumX,
        out double maximumX,
        out double minimumY,
        out double maximumY)
    {
        minimumX = 0;
        maximumX = 0;
        minimumY = 0;
        maximumY = 0;
        if (!_assignmentRectanglesByParent.TryGetValue(topUiMapId, out var rectangles))
            return false;

        var rectangle = rectangles.FirstOrDefault(value => value.ChildMapId == uiMapId);
        if (rectangle.ChildMapId != uiMapId)
            return false;

        minimumX = rectangle.MinimumX;
        maximumX = rectangle.MaximumX;
        minimumY = rectangle.MinimumY;
        maximumY = rectangle.MaximumY;
        return true;
    }

    public bool TryProjectWorldPositionBounds(
        int uiMapId,
        int worldMapId,
        double worldX,
        double worldY,
        double worldDiameter,
        out WowMapPosition first,
        out WowMapPosition second)
    {
        first = default;
        second = default;
        if (!_assignmentsByMap.TryGetValue(uiMapId, out var assignments))
            return false;

        foreach (var assignment in assignments)
        {
            if (assignment.MapId != worldMapId ||
                !WowMapHighlightGeometry.TryProjectWorldPositionToUi(
                    assignment,
                    worldX,
                    worldY,
                    out _,
                    out _))
            {
                continue;
            }

            var halfExtent = worldDiameter * 0.5;
            if (!WowMapHighlightGeometry.TryProjectWorldPositionToUiUnbounded(
                    assignment,
                    worldX - halfExtent,
                    worldY + halfExtent,
                    out var firstX,
                    out var firstY) ||
                !WowMapHighlightGeometry.TryProjectWorldPositionToUiUnbounded(
                    assignment,
                    worldX + halfExtent,
                    worldY - halfExtent,
                    out var secondX,
                    out var secondY))
            {
                return false;
            }

            first = new WowMapPosition(firstX, firstY);
            second = new WowMapPosition(secondX, secondY);
            return true;
        }

        return false;
    }

    private static bool TryBuildHighlight(
        WowMapLink link,
        WowMapArt highlightArt,
        double minimumX,
        double minimumY,
        double maximumX,
        double maximumY,
        out WowMapHighlight highlight)
    {
        highlight = default;
        var left = Math.Min(minimumX, maximumX);
        var right = Math.Max(minimumX, maximumX);
        var top = Math.Min(minimumY, maximumY);
        var bottom = Math.Max(minimumY, maximumY);
        var width = right - left;
        var height = bottom - top;
        var asset = WowMapHighlightGeometry.ResolveHighlightAsset(link, highlightArt);
        var (texturePercentageX, texturePercentageY) =
            WowMapHighlightGeometry.CalculateTexturePercentages(
                width,
                height,
                highlightArt.Layers[0].LayerWidth,
                highlightArt.Layers[0].LayerHeight,
                !string.IsNullOrEmpty(asset.AtlasId));
        highlight = new WowMapHighlight(
            asset.FileDataId,
            asset.AtlasId,
            texturePercentageX,
            texturePercentageY,
            width,
            height,
            left,
            top);
        return true;
    }

    public static TactMapCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var atlasNames = database.Load("UiTextureAtlasElement", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => Text(row, "Name"));
        var maps = database.Load("UiMap", build).Values.ToDictionary(
            row => Integer(row, "ID"),
            row => new WowMapDetails(
                Integer(row, "ID"),
                Text(row, "Name_lang"),
                Integer(row, "Type"),
                Integer(row, "ParentUiMapID"),
                Integer(row, "Flags"),
                atlasNames.GetValueOrDefault(Integer(row, "BkgAtlasID")) ?? string.Empty,
                Integer(row, "HelpTextPosition"),
                Integer(row, "MapArtZoneTextPosition")));
        var areaNames = database.Load("AreaTable", build).Values
            .Select(row => (
                Id: Integer(row, "ID"),
                Name: Text(row, "AreaName_lang", "AreaName", "Name_lang", "Name")))
            .Where(value => value.Id > 0 && !string.IsNullOrWhiteSpace(value.Name))
            .ToDictionary(value => value.Id, value => value.Name);

        var artRows = database.Load("UiMapArt", build).Values.ToDictionary(
            row => Integer(row, "ID"));
        var stylesByArt = artRows.ToDictionary(
            pair => pair.Key,
            pair => Integer(pair.Value, "UiMapArtStyleID"));
        var styleLayers = database.Load("UiMapArtStyleLayer", build).Values
            .GroupBy(row => Integer(row, "UiMapArtStyleID"))
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(row => Integer(row, "LayerIndex")).ToArray());
        var tiles = database.Load("UiMapArtTile", build).Values
            .GroupBy(row => (
                ArtId: Integer(row, "UiMapArtID"),
                LayerIndex: Integer(row, "LayerIndex")))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<uint>)group
                    .OrderBy(row => Integer(row, "RowIndex"))
                    .ThenBy(row => Integer(row, "ColIndex"))
                    .Select(row => unchecked((uint)Integer(row, "FileDataID")))
                    .ToArray());

        var artById = new Dictionary<int, WowMapArt>();
        foreach (var (artId, styleId) in stylesByArt)
        {
            if (!styleLayers.TryGetValue(styleId, out var rows))
                continue;
            var layers = new List<WowMapArtLayer>(rows.Length);
            foreach (var row in rows)
            {
                var layerIndex = Integer(row, "LayerIndex");
                tiles.TryGetValue((artId, layerIndex), out var layerTextures);
                layers.Add(new WowMapArtLayer(
                    Integer(row, "LayerWidth"),
                    Integer(row, "LayerHeight"),
                    Integer(row, "TileWidth"),
                    Integer(row, "TileHeight"),
                    Number(row, "MinScale"),
                    Number(row, "MaxScale"),
                    Integer(row, "AdditionalZoomSteps"),
                    layerTextures ?? []));
            }
            var artRow = artRows[artId];
            artById[artId] = new WowMapArt(
                artId,
                layers,
                unchecked((uint)Integer(artRow, "HighlightFileDataID")),
                atlasNames.GetValueOrDefault(Integer(artRow, "HighlightAtlasID")));
        }

        var artByMap = new Dictionary<int, WowMapArt>();
        foreach (var row in database.Load("UiMapXMapArt", build).Values
                     .Where(row => Integer(row, "PhaseID") == 0)
                     .OrderBy(row => Integer(row, "ID")))
        {
            var mapId = Integer(row, "UiMapID");
            var artId = Integer(row, "UiMapArtID");
            if (artById.TryGetValue(artId, out var art))
                artByMap.TryAdd(mapId, art);
        }

        var linksByParent = database.Load("UiMapLink", build).Values
            .Select(row =>
            {
                var minimum = Vector2(row, "UiMin");
                var maximum = Vector2(row, "UiMax");
                return new WowMapLink(
                    Integer(row, "ID"),
                    Integer(row, "ParentUiMapID"),
                    Integer(row, "ChildUiMapID"),
                    Integer(row, "OrderIndex"),
                    minimum.X,
                    minimum.Y,
                    maximum.X,
                    maximum.Y,
                    Integer(row, "Flags"),
                    unchecked((uint)Integer(row, "OverrideHighlightFdID")),
                    atlasNames.GetValueOrDefault(
                        Integer(row, "OverrideHighlightAtlasID")),
                    Integer(row, "PlayerConditionID"));
            })
            .Where(link => link.ParentMapId > 0 && link.ChildMapId > 0)
            .GroupBy(link => link.ParentMapId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WowMapLink>)group
                    .OrderBy(link => link.Id)
                    .ToArray());

        var assignmentsByMap = database.Load("UiMapAssignment", build).Values
            .Select(row =>
            {
                var uiMinimum = Vector2(row, "UiMin");
                var uiMaximum = Vector2(row, "UiMax");
                var region = Vector(row, "Region", 6);
                return new WowMapAssignment(
                    Integer(row, "ID"),
                    Integer(row, "UiMapID"),
                    Integer(row, "OrderIndex"),
                    Integer(row, "MapID"),
                    Integer(row, "AreaID"),
                    uiMinimum.X,
                    uiMinimum.Y,
                    uiMaximum.X,
                    uiMaximum.Y,
                    region[0],
                    region[1],
                    region[2],
                    region[3],
                    region[4],
                    region[5]);
            })
            .Where(assignment => assignment.UiMapId > 0)
            .GroupBy(assignment => assignment.UiMapId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WowMapAssignment>)group
                    .OrderBy(assignment => assignment.OrderIndex)
                    .ThenBy(assignment => assignment.Id)
                    .ToArray());

        return new TactMapCatalog(maps, artByMap, linksByParent, assignmentsByMap, areaNames);
    }

    private bool TryGetLinkAtPosition(
        int mapId,
        double x,
        double y,
        out WowMapLink selected)
    {
        selected = default;
        if (!_linksByParent.TryGetValue(mapId, out var links))
            return false;

        return WowMapHighlightGeometry.TrySelectLinkAtPosition(
            links,
            x,
            y,
            null,
            out selected);
    }

    private bool TryGetHighlightArt(WowMapLink link, out WowMapArt art)
    {
        if (_artByMap.TryGetValue(link.ChildMapId, out art!) &&
            _maps.TryGetValue(link.ChildMapId, out var childMap) &&
            (childMap.Flags & 0x21) == 0 &&
            art.Layers.Count > 0 &&
            art.Layers[0].LayerWidth > 0 &&
            art.Layers[0].LayerHeight > 0 &&
            WowMapHighlightGeometry.ResolveHighlightAsset(link, art) is var asset &&
            (asset.FileDataId != 0 || !string.IsNullOrEmpty(asset.AtlasId)))
        {
            return true;
        }

        art = null!;
        return false;
    }

    private bool TryGetAssignmentChildAtPosition(
        int mapId,
        double x,
        double y,
        out WowMapDetails details,
        out WowMapLink rectangle)
    {
        details = default;
        rectangle = default;
        if (!_assignmentRectanglesByParent.TryGetValue(mapId, out var rectangles))
        {
            return false;
        }

        if (!WowMapHighlightGeometry.TrySelectLinkAtPosition(
                rectangles,
                x,
                y,
                null,
                out rectangle))
        {
            return false;
        }

        return _maps.TryGetValue(rectangle.ChildMapId, out details);
    }

    private static bool IsMapLinkTarget(WowMapDetails details) =>
        (details.Flags & 0x40000) != 0 ||
        details.MapType is not (4 or 5 or 6);

    private IEnumerable<int> HighlightParentMapIds() =>
        _linksByParent.Keys
            .Concat(_childMapIdsByParent.Keys)
            .Distinct();

    private int GetAssignmentHighlightCount(int mapId)
    {
        if (!_assignmentRectanglesByParent.TryGetValue(mapId, out var rectangles))
        {
            return 0;
        }

        return rectangles.Count(rectangle =>
            TryGetHighlightArt(
                new WowMapLink(
                    0,
                    mapId,
                    rectangle.ChildMapId,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0),
                out _));
    }

    private IReadOnlyDictionary<int, IReadOnlyList<WowMapLink>>
        BuildAssignmentRectangles()
    {
        var result = new Dictionary<int, IReadOnlyList<WowMapLink>>();
        foreach (var (parentMapId, childMapIds) in _childMapIdsByParent)
        {
            if (!_assignmentsByMap.TryGetValue(parentMapId, out var parentAssignments))
                continue;

            _linksByParent.TryGetValue(parentMapId, out var parentLinks);
            var rectangles = new List<WowMapLink>();
            foreach (var childMapId in childMapIds)
            {
                if (!_maps.TryGetValue(childMapId, out var childDetails) ||
                    !IsMapLinkTarget(childDetails) ||
                    !_assignmentsByMap.TryGetValue(childMapId, out var childAssignments) ||
                    !WowMapHighlightGeometry.TryResolveDirectMapRectangle(
                        parentMapId,
                        childMapId,
                        parentLinks ?? [],
                        parentAssignments,
                        childAssignments,
                        out var rectangle))
                {
                    continue;
                }

                rectangles.Add(rectangle);
            }

            if (rectangles.Count > 0)
                result[parentMapId] = rectangles;
        }
        return result;
    }

    private static (double X, double Y) Vector2(dynamic row, string name)
    {
        var value = Field(row, name);
        if (value is IEnumerable sequence and not string)
        {
            var numbers = sequence.Cast<object?>()
                .Select(item => Convert.ToDouble(item ?? 0))
                .Take(2)
                .ToArray();
            if (numbers.Length == 2)
                return (numbers[0], numbers[1]);
        }

        return (IndexedNumber(row, name, 0), IndexedNumber(row, name, 1));
    }

    private static double[] Vector(dynamic row, string name, int count)
    {
        var value = Field(row, name);
        if (value is IEnumerable sequence and not string)
        {
            var numbers = sequence.Cast<object?>()
                .Select(item => Convert.ToDouble(item ?? 0))
                .Take(count)
                .ToArray();
            if (numbers.Length == count)
                return numbers;
        }

        return Enumerable.Range(0, count)
            .Select(index => (double)IndexedNumber(row, name, index))
            .ToArray();
    }

    private static double IndexedNumber(dynamic row, string name, int index)
    {
        foreach (var candidate in new[]
                 {
                     $"{name}[{index}]",
                     $"{name}_{index}",
                     $"{name}{index}"
                 })
        {
            var value = Field(row, candidate);
            if (value is not null)
                return Convert.ToDouble(value);
        }
        return 0;
    }



    private static double Number(dynamic row, string name) =>
        Convert.ToDouble(Field(row, name) ?? 0);


}
