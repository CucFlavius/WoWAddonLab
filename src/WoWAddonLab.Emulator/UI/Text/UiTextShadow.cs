using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public static class UiTextShadow
{
    public static float BoundAlpha(float shadowAlpha, float baseTextAlpha) =>
        Math.Min(shadowAlpha, baseTextAlpha);

    public static bool IsVisible(Vector2 offset, Vector4 color) =>
        (offset.X != 0 || offset.Y != 0) && color.W > 0;
}
