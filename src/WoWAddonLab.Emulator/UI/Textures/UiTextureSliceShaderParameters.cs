using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiTextureSliceShaderParameters(
    Vector2 DestinationSize,
    Vector2 SourcePixelSize,
    Vector2 CenterRepeat,
    UiInsets Margins,
    UiTextureSliceMode Mode)
{
    private const float NativeNearZero = 1.1754944e-35f;

    public float DestinationLeft =>
        Margins.Left / DestinationSize.X;
    public float DestinationRight =>
        1 - Margins.Right / DestinationSize.X;
    public float DestinationTop =>
        Margins.Top / DestinationSize.Y;
    public float DestinationBottom =>
        1 - Margins.Bottom / DestinationSize.Y;

    public float SourceLeft =>
        Margins.Left / SourcePixelSize.X;
    public float SourceRight =>
        1 - Margins.Right / SourcePixelSize.X;
    public float SourceTop =>
        Margins.Top / SourcePixelSize.Y;
    public float SourceBottom =>
        1 - Margins.Bottom / SourcePixelSize.Y;

    public float HalfTexelX =>
        0.5f / SourcePixelSize.X;
    public float HalfTexelY =>
        0.5f / SourcePixelSize.Y;

    public static UiTextureSliceShaderParameters Resolve(
        Vector2 destinationSize,
        Vector2 sourcePixelSize,
        UiInsets pixelMargins,
        UiTextureSliceMode mode)
    {
        var destinationWidth = destinationSize.X;
        var destinationHeight = destinationSize.Y;
        var left = pixelMargins.Left;
        var top = pixelMargins.Top;
        var right = pixelMargins.Right;
        var bottom = pixelMargins.Bottom;

        if (destinationWidth <= sourcePixelSize.X && destinationWidth > 0)
        {
            left = 0;
            right = 0;
        }
        if (destinationHeight <= sourcePixelSize.Y && destinationHeight > 0)
        {
            top = 0;
            bottom = 0;
        }

        if (left == 0 && right == 0 && destinationWidth > 0)
        {
            destinationHeight *= sourcePixelSize.X / destinationWidth;
        }
        if (top == 0 && bottom == 0 && destinationHeight > 0)
        {
            destinationWidth *= sourcePixelSize.Y / destinationHeight;
        }

        var sourceCenterWidth = sourcePixelSize.X - right - left;
        var sourceCenterHeight = sourcePixelSize.Y - bottom - top;
        var repeatX = 1f;
        var repeatY = 1f;
        if (MathF.Abs(sourceCenterWidth) > NativeNearZero &&
            (left > 0 || right > 0))
        {
            repeatX =
                (destinationWidth -
                 (sourcePixelSize.X - sourceCenterWidth)) /
                sourceCenterWidth;
        }
        if (MathF.Abs(sourceCenterHeight) > NativeNearZero &&
            (top > 0 || bottom > 0))
        {
            repeatY =
                (destinationHeight -
                 (sourcePixelSize.Y - sourceCenterHeight)) /
                sourceCenterHeight;
        }

        return new UiTextureSliceShaderParameters(
            new Vector2(destinationWidth, destinationHeight),
            sourcePixelSize,
            new Vector2(repeatX, repeatY),
            new UiInsets(left, right, top, bottom),
            mode);
    }
}
