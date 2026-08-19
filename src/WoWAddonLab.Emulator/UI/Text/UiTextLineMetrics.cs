using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public static class UiTextLineMetrics
{
    private const float NativeFontReferenceHeight = 768;
    private const float MinimumFontPixelHeight = 2;
    private const float MaximumFontPixelHeight = 256;
    private const float PositiveCeilTolerance = 0.99995f;
    public const float IndentedWordWrapPixels = 15;

    public static float ResolveLogicalLineHeight(
        float fontHeight,
        float textScale,
        float physicalScreenHeight,
        float effectiveScale,
        bool smoothScaling = false)
    {
        var physicalPixelsPerUiUnit =
            physicalScreenHeight * effectiveScale / NativeFontReferenceHeight;
        if (!(fontHeight > 0) ||
            !(textScale > 0) ||
            !(physicalPixelsPerUiUnit > 0))
        {
            return 0;
        }

        if (smoothScaling)
            return fontHeight;

        var selectedPixelHeight = ResolvePhysicalRasterHeight(
            fontHeight,
            textScale,
            physicalPixelsPerUiUnit);
        return selectedPixelHeight / physicalPixelsPerUiUnit;
    }

    public static float ResolvePhysicalRasterHeight(
        float fontHeight,
        float textScale,
        float physicalPixelsPerUiUnit)
    {
        if (!(fontHeight > 0) ||
            !(textScale > 0) ||
            !(physicalPixelsPerUiUnit > 0))
        {
            return 0;
        }

        var requestedPixelHeight =
            fontHeight * textScale * physicalPixelsPerUiUnit;
        return Math.Clamp(
            MathF.Truncate(
                MathF.Max(requestedPixelHeight, MinimumFontPixelHeight) + 0.5f),
            MinimumFontPixelHeight,
            MaximumFontPixelHeight);
    }

    public static float ResolvePhysicalRenderHeight(
        float fontHeight,
        float textScale,
        float physicalPixelsPerUiUnit,
        bool smoothScaling)
    {
        var rasterHeight = ResolvePhysicalRasterHeight(
            fontHeight,
            textScale,
            physicalPixelsPerUiUnit);
        if (!(rasterHeight > 0))
            return 0;

        return smoothScaling
            ? MathF.Max(
                fontHeight * physicalPixelsPerUiUnit,
                MinimumFontPixelHeight)
            : rasterHeight;
    }

    public static float ResolveLogicalSpacing(
        float spacing,
        float physicalScreenHeight,
        float effectiveScale)
    {
        var physicalPixelsPerUiUnit =
            physicalScreenHeight * effectiveScale / NativeFontReferenceHeight;
        if (!(spacing > 0) || !(physicalPixelsPerUiUnit > 0))
            return 0;

        return QuantizePhysicalSpacing(spacing * physicalPixelsPerUiUnit) /
               physicalPixelsPerUiUnit;
    }

    public static float ResolveLogicalPositiveShadowWidth(
        float shadowOffsetX,
        float physicalScreenHeight,
        float effectiveScale)
    {
        var physicalPixelsPerUiUnit =
            physicalScreenHeight * effectiveScale / NativeFontReferenceHeight;
        if (!(shadowOffsetX > 0) || !(physicalPixelsPerUiUnit > 0))
            return 0;

        return MathF.Ceiling(shadowOffsetX) / physicalPixelsPerUiUnit;
    }

    public static float ResolveLogicalIndentedWordWrapWidth(
        float physicalScreenHeight,
        float effectiveScale)
    {
        var physicalPixelsPerUiUnit =
            physicalScreenHeight * effectiveScale / NativeFontReferenceHeight;
        return physicalPixelsPerUiUnit > 0
            ? IndentedWordWrapPixels / physicalPixelsPerUiUnit
            : 0;
    }

    public static float QuantizePhysicalSpacing(float spacing)
    {
        if (!(spacing > 0))
            return 0;

        return MathF.Truncate(spacing + PositiveCeilTolerance);
    }

    public static Vector2 SnapTopDownPhysicalOrigin(
        Vector2 origin,
        bool smoothScaling) =>
        SnapTopDownPhysicalOrigin(origin, smoothScaling, Vector2.One);

    public static Vector2 SnapTopDownPhysicalOrigin(
        Vector2 origin,
        bool smoothScaling,
        Vector2 framebufferScale)
    {
        if (smoothScaling)
            return origin;

        var scaleX = framebufferScale.X > 0 ? framebufferScale.X : 1;
        var scaleY = framebufferScale.Y > 0 ? framebufferScale.Y : 1;
        return new Vector2(
            MathF.Floor(origin.X * scaleX) / scaleX,
            MathF.Ceiling(origin.Y * scaleY) / scaleY);
    }

    public static float QuantizeSignedPhysicalMetric(
        float metric,
        bool smoothScaling)
    {
        if (smoothScaling)
            return metric;

        return metric <= 0
            ? -MathF.Truncate(-metric + 0.5f)
            : MathF.Truncate(metric + 0.5f);
    }

    public static float ResolvePhysicalHorizontalAnchorOffset(
        string justification,
        float layoutWidth) =>
        justification.ToUpperInvariant() switch
        {
            "RIGHT" => layoutWidth,
            "CENTER" => layoutWidth / 2,
            _ => 0
        };

    public static float ResolvePhysicalLineOffset(
        string justification,
        float lineWidth,
        bool smoothScaling,
        bool indentedContinuation)
    {
        var normalizedJustification = justification.ToUpperInvariant();
        var offset = normalizedJustification switch
        {
            "RIGHT" => -lineWidth,
            "CENTER" => -lineWidth / 2,
            _ => 0
        };
        offset = QuantizeSignedPhysicalMetric(offset, smoothScaling);

        if (!indentedContinuation)
            return offset;

        return normalizedJustification switch
        {
            "RIGHT" => offset - IndentedWordWrapPixels,
            "CENTER" => offset,
            _ => offset + IndentedWordWrapPixels
        };
    }
}
