using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public static class UiStatusBarFillGeometry
{
    public static UiStatusBarFillResult Resolve(
        UiRect bounds,
        double normalizedValue,
        string orientation,
        int fillStyle,
        bool rotatesTexture,
        bool cropTexture,
        float? nativeCoordinateUnitsPerLogicalUnit = null)
    {
        var progress = (float)Math.Clamp(normalizedValue, 0, 1);
        var vertical = orientation.Equals(
            "VERTICAL",
            StringComparison.OrdinalIgnoreCase);
        if (nativeCoordinateUnitsPerLogicalUnit is > 0)
        {
            progress = SnapProgressToNativeCoordinateGrid(
                bounds,
                progress,
                vertical,
                fillStyle,
                nativeCoordinateUnitsPerLogicalUnit.Value);
        }
        var fillBounds = vertical
            ? ResolveVerticalBounds(bounds, progress, fillStyle)
            : ResolveHorizontalBounds(bounds, progress, fillStyle);
        if (!cropTexture)
            return new UiStatusBarFillResult(fillBounds, null);

        return new UiStatusBarFillResult(
            fillBounds,
            ResolveNormalizedUv(progress, vertical, rotatesTexture));
    }

    private static float SnapProgressToNativeCoordinateGrid(
        UiRect bounds,
        float progress,
        bool vertical,
        int fillStyle,
        float nativeCoordinateUnitsPerLogicalUnit)
    {
        var extent = vertical ? bounds.Height : bounds.Width;
        if (fillStyle == 2)
            extent *= 0.5f;
        if (extent == 0)
            return 1;

        var nativeExtent = extent * nativeCoordinateUnitsPerLogicalUnit;
        var roundedUnfilledExtent = MathF.Round(
            nativeExtent * (1 - progress),
            MidpointRounding.AwayFromZero);
        return 1 - roundedUnfilledExtent / nativeExtent;
    }

    private static UiRect ResolveHorizontalBounds(
        UiRect bounds,
        float progress,
        int fillStyle)
    {
        var width = bounds.Width * progress;
        return fillStyle switch
        {
            2 => new UiRect(
                bounds.Left + (bounds.Width - width) * 0.5f,
                bounds.Bottom,
                width,
                bounds.Height),
            3 => new UiRect(
                bounds.Right - width,
                bounds.Bottom,
                width,
                bounds.Height),
            _ => new UiRect(bounds.Left, bounds.Bottom, width, bounds.Height)
        };
    }

    private static UiRect ResolveVerticalBounds(
        UiRect bounds,
        float progress,
        int fillStyle)
    {
        var height = bounds.Height * progress;
        return fillStyle switch
        {
            2 => new UiRect(
                bounds.Left,
                bounds.Bottom + (bounds.Height - height) * 0.5f,
                bounds.Width,
                height),
            3 => new UiRect(
                bounds.Left,
                bounds.Top - height,
                bounds.Width,
                height),
            _ => new UiRect(bounds.Left, bounds.Bottom, bounds.Width, height)
        };
    }

    private static Vector2[] ResolveNormalizedUv(
        float progress,
        bool vertical,
        bool rotatesTexture)
    {
        if (!rotatesTexture)
        {
            return vertical
                ?
                [
                    new Vector2(0, 1 - progress),
                    new Vector2(0, 1),
                    new Vector2(1, 1 - progress),
                    new Vector2(1, 1)
                ]
                :
                [
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(progress, 0),
                    new Vector2(progress, 1)
                ];
        }

        return vertical
            ?
            [
                new Vector2(progress, 0),
                new Vector2(0, 0),
                new Vector2(progress, 1),
                new Vector2(0, 1)
            ]
            :
            [
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(0, 1 - progress),
                new Vector2(1, 1 - progress)
            ];
    }
}
