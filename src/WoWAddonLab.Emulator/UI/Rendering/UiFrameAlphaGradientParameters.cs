using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiFrameAlphaGradientParameters(
    Vector4 EdgeWidths,
    Vector4 Rectangle)
{
    public float Evaluate(Vector2 position)
    {
        var distances = new Vector4(
            MathF.Abs(position.X - Rectangle.X),
            MathF.Abs(position.Y - Rectangle.Y),
            MathF.Abs(position.X - Rectangle.Z),
            MathF.Abs(position.Y - Rectangle.W));
        return SaturateDivide(distances.X, EdgeWidths.X) *
               SaturateDivide(distances.Y, EdgeWidths.Y) *
               SaturateDivide(distances.Z, EdgeWidths.Z) *
               SaturateDivide(distances.W, EdgeWidths.W);
    }

    private static float SaturateDivide(float numerator, float denominator)
    {
        var value = numerator / denominator;
        return float.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
    }
}
