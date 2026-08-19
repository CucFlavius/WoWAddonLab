namespace WoWAddonLab.Emulator.Lua;

public interface IWowMapProvider
{
    int HighlightMapCount => 0;
    int HighlightCount => 0;
    int GetHighlightCount(int mapId) => 0;
    bool TryGetAreaName(int areaId, out string name)
    {
        name = string.Empty;
        return false;
    }
    bool TryGetMapDetails(int mapId, out WowMapDetails details);
    bool TryGetMapArt(int mapId, out WowMapArt art);
    bool TryGetMapAtPosition(
        int mapId,
        double x,
        double y,
        bool ignoreZoneMapPositionData,
        out WowMapDetails details)
    {
        details = default;
        return false;
    }
    IReadOnlyList<WowMapDetails> GetMapChildren(
        int mapId,
        int? mapType = null,
        bool allDescendants = false) => [];
    bool TryGetMapHighlight(
        int mapId,
        double x,
        double y,
        out WowMapHighlight highlight)
    {
        highlight = default;
        return false;
    }

    bool TryProjectWorldPosition(
        int uiMapId,
        int worldMapId,
        double worldX,
        double worldY,
        out WowMapPosition position)
    {
        position = default;
        return false;
    }

    bool TryMapPositionToWorld(
        int uiMapId,
        double x,
        double y,
        out int worldMapId,
        out WowMapPosition position)
    {
        worldMapId = 0;
        position = default;
        return false;
    }

    bool TryGetMapWorldSize(int uiMapId, out double width, out double height)
    {
        width = 0;
        height = 0;
        return false;
    }

    bool TryGetMapRectangle(
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
        return false;
    }

    bool TryProjectWorldPositionBounds(
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
        var halfExtent = worldDiameter * 0.5;
        return TryProjectWorldPosition(
                   uiMapId,
                   worldMapId,
                   worldX - halfExtent,
                   worldY + halfExtent,
                   out first) &&
               TryProjectWorldPosition(
                   uiMapId,
                   worldMapId,
                   worldX + halfExtent,
                   worldY - halfExtent,
                   out second);
    }
}
