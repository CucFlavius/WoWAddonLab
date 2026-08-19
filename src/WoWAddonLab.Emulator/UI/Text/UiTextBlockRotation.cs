using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public static class UiTextBlockRotation
{
    public const float MinimumRadians = 0.001f;

    public static bool IsActive(float radians) =>
        MathF.Abs(radians) >= MinimumRadians;

    public static Vector2 RotateScreenPoint(
        Vector2 point,
        Vector2 textBlockOrigin,
        float radians)
    {
        var offset = point - textBlockOrigin;
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);

        return textBlockOrigin + new Vector2(
            offset.X * cosine + offset.Y * sine,
            -offset.X * sine + offset.Y * cosine);
    }
}
