namespace WoWAddonLab.Emulator.Lua;

public static class WowMapHighlightGeometry
{
    public static bool TrySelectLinkAtPosition(
        IReadOnlyList<WowMapLink> links,
        double x,
        double y,
        Func<int, bool>? isPlayerConditionMet,
        out WowMapLink selected)
    {
        selected = default;
        var selectedDistanceSquared = double.MaxValue;
        var found = false;
        foreach (var link in links)
        {
            if (link.PlayerConditionId != 0 &&
                isPlayerConditionMet?.Invoke(link.PlayerConditionId) != true)
            {
                continue;
            }

            var minimumX = Math.Min(link.MinimumX, link.MaximumX);
            var maximumX = Math.Max(link.MinimumX, link.MaximumX);
            var minimumY = Math.Min(link.MinimumY, link.MaximumY);
            var maximumY = Math.Max(link.MinimumY, link.MaximumY);
            if (x < minimumX || x > maximumX ||
                y < minimumY || y > maximumY)
            {
                continue;
            }

            var centerX = (minimumX + maximumX) * .5;
            var centerY = (minimumY + maximumY) * .5;
            var deltaX = centerX - x;
            var deltaY = centerY - y;
            var distanceSquared = deltaX * deltaX + deltaY * deltaY;
            if (distanceSquared >= selectedDistanceSquared)
                continue;

            selected = link;
            selectedDistanceSquared = distanceSquared;
            found = true;
        }
        return found;
    }

    public static (uint FileDataId, string? AtlasId) ResolveHighlightAsset(
        WowMapLink link,
        WowMapArt art)
    {
        var fileDataId = link.OverrideHighlightFileDataId != 0
            ? link.OverrideHighlightFileDataId
            : art.HighlightFileDataId;
        var atlasId = !string.IsNullOrWhiteSpace(link.OverrideHighlightAtlasId)
            ? link.OverrideHighlightAtlasId
            : art.HighlightAtlasId;
        return (fileDataId, atlasId);
    }

    public static (double X, double Y) CalculateTexturePercentages(
        double rectangleWidth,
        double rectangleHeight,
        int layerWidth,
        int layerHeight,
        bool usesAtlas)
    {
        if (usesAtlas)
            return (1, 1);
        if (rectangleWidth <= 0 ||
            rectangleHeight <= 0 ||
            layerWidth <= 0 ||
            layerHeight <= 0)
        {
            return (0, 0);
        }

        var aspectAdjustedWidth =
            (layerHeight / (double)layerWidth) * rectangleHeight;
        if (aspectAdjustedWidth <= 0)
            return (0, 0);
        return rectangleWidth > aspectAdjustedWidth
            ? (1, aspectAdjustedWidth / rectangleWidth)
            : (rectangleWidth / aspectAdjustedWidth, 1);
    }

    public static bool TryMapUiPositionToWorld(
        WowMapAssignment assignment,
        double x,
        double y,
        out double worldX,
        out double worldY)
    {
        worldX = 0;
        worldY = 0;
        var uiWidth = assignment.UiMaximumX - assignment.UiMinimumX;
        var uiHeight = assignment.UiMaximumY - assignment.UiMinimumY;
        if (Math.Abs(uiWidth) < double.Epsilon ||
            Math.Abs(uiHeight) < double.Epsilon)
        {
            return false;
        }

        var normalizedX = (x - assignment.UiMinimumX) / uiWidth;
        var normalizedY = (y - assignment.UiMinimumY) / uiHeight;
        worldX =
            (1 - normalizedY) * assignment.RegionMaximumX +
            normalizedY * assignment.RegionMinimumX;
        worldY =
            (1 - normalizedX) * assignment.RegionMaximumY +
            normalizedX * assignment.RegionMinimumY;
        return true;
    }

    public static bool TryProjectWorldPositionToUi(
        WowMapAssignment assignment,
        double worldX,
        double worldY,
        out double x,
        out double y)
    {
        if (!TryProjectWorldPositionToUiUnbounded(
                assignment,
                worldX,
                worldY,
                out x,
                out y))
            return false;

        var minimumX = Math.Min(assignment.UiMinimumX, assignment.UiMaximumX);
        var maximumX = Math.Max(assignment.UiMinimumX, assignment.UiMaximumX);
        var minimumY = Math.Min(assignment.UiMinimumY, assignment.UiMaximumY);
        var maximumY = Math.Max(assignment.UiMinimumY, assignment.UiMaximumY);
        return x >= minimumX && x <= maximumX &&
               y >= minimumY && y <= maximumY;
    }

    public static bool TryProjectWorldPositionToUiUnbounded(
        WowMapAssignment assignment,
        double worldX,
        double worldY,
        out double x,
        out double y)
    {
        x = 0;
        y = 0;
        var regionWidth = assignment.RegionMinimumY - assignment.RegionMaximumY;
        var regionHeight = assignment.RegionMinimumX - assignment.RegionMaximumX;
        if (Math.Abs(regionWidth) < double.Epsilon ||
            Math.Abs(regionHeight) < double.Epsilon)
        {
            return false;
        }

        var normalizedX = (worldY - assignment.RegionMaximumY) / regionWidth;
        var normalizedY = (worldX - assignment.RegionMaximumX) / regionHeight;
        x = assignment.UiMinimumX +
            normalizedX * (assignment.UiMaximumX - assignment.UiMinimumX);
        y = assignment.UiMinimumY +
            normalizedY * (assignment.UiMaximumY - assignment.UiMinimumY);
        return true;
    }

    public static bool TryProjectWorldRegionToUi(
        WowMapAssignment parent,
        WowMapAssignment child,
        out double minimumX,
        out double minimumY,
        out double maximumX,
        out double maximumY)
    {
        minimumX = 0;
        minimumY = 0;
        maximumX = 0;
        maximumY = 0;
        var regionWidth = parent.RegionMinimumY - parent.RegionMaximumY;
        var regionHeight = parent.RegionMinimumX - parent.RegionMaximumX;
        if (Math.Abs(regionWidth) < double.Epsilon ||
            Math.Abs(regionHeight) < double.Epsilon)
        {
            return false;
        }

        var uiWidth = parent.UiMaximumX - parent.UiMinimumX;
        var uiHeight = parent.UiMaximumY - parent.UiMinimumY;
        var firstX = parent.UiMinimumX +
                     ((child.RegionMaximumY - parent.RegionMaximumY) / regionWidth) * uiWidth;
        var secondX = parent.UiMinimumX +
                      ((child.RegionMinimumY - parent.RegionMaximumY) / regionWidth) * uiWidth;
        var firstY = parent.UiMinimumY +
                     ((child.RegionMaximumX - parent.RegionMaximumX) / regionHeight) * uiHeight;
        var secondY = parent.UiMinimumY +
                      ((child.RegionMinimumX - parent.RegionMaximumX) / regionHeight) * uiHeight;
        minimumX = Math.Min(firstX, secondX);
        maximumX = Math.Max(firstX, secondX);
        minimumY = Math.Min(firstY, secondY);
        maximumY = Math.Max(firstY, secondY);
        return true;
    }

    public static bool TryResolveDirectMapRectangle(
        int parentMapId,
        int childMapId,
        IReadOnlyList<WowMapLink> links,
        IReadOnlyList<WowMapAssignment> parentAssignments,
        IReadOnlyList<WowMapAssignment> childAssignments,
        out WowMapLink rectangle)
    {
        foreach (var link in links)
        {
            if (link.ParentMapId != parentMapId ||
                link.ChildMapId != childMapId)
            {
                continue;
            }

            rectangle = link with
            {
                PlayerConditionId = 0,
                OverrideHighlightFileDataId = 0,
                OverrideHighlightAtlasId = null
            };
            return true;
        }

        foreach (var childAssignment in childAssignments)
        {
            foreach (var parentAssignment in parentAssignments)
            {
                if (parentAssignment.MapId != childAssignment.MapId ||
                    !TryProjectWorldRegionToUi(
                        parentAssignment,
                        childAssignment,
                        out var minimumX,
                        out var minimumY,
                        out var maximumX,
                        out var maximumY))
                {
                    continue;
                }

                rectangle = new WowMapLink(
                    0,
                    parentMapId,
                    childMapId,
                    childAssignment.OrderIndex,
                    minimumX,
                    minimumY,
                    maximumX,
                    maximumY,
                    0);
                return true;
            }
        }

        rectangle = default;
        return false;
    }
}
