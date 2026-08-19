using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiTextureMaskShaderTransform(
    Vector2 Origin,
    Vector2 PositionX,
    Vector2 PositionY)
{
    public Vector2 Transform(Vector2 position) =>
        Origin + PositionX * position.X + PositionY * position.Y;

    public static bool TryResolve(
        Vector2 subjectUpperLeft,
        Vector2 subjectUpperRight,
        Vector2 subjectLowerLeft,
        Vector2 maskUvAtUpperLeft,
        Vector2 maskUvAtUpperRight,
        Vector2 maskUvAtLowerLeft,
        out UiTextureMaskShaderTransform transform)
    {
        var horizontal = subjectUpperRight - subjectUpperLeft;
        var vertical = subjectLowerLeft - subjectUpperLeft;
        var determinant =
            horizontal.X * vertical.Y - horizontal.Y * vertical.X;
        if (MathF.Abs(determinant) <= 1e-6f)
        {
            transform = default;
            return false;
        }

        var inverseHorizontalX = vertical.Y / determinant;
        var inverseHorizontalY = -vertical.X / determinant;
        var inverseVerticalX = -horizontal.Y / determinant;
        var inverseVerticalY = horizontal.X / determinant;
        var uvHorizontal = maskUvAtUpperRight - maskUvAtUpperLeft;
        var uvVertical = maskUvAtLowerLeft - maskUvAtUpperLeft;
        var positionX =
            uvHorizontal * inverseHorizontalX +
            uvVertical * inverseVerticalX;
        var positionY =
            uvHorizontal * inverseHorizontalY +
            uvVertical * inverseVerticalY;
        transform = new UiTextureMaskShaderTransform(
            maskUvAtUpperLeft -
            positionX * subjectUpperLeft.X -
            positionY * subjectUpperLeft.Y,
            positionX,
            positionY);
        return true;
    }

    public static Vector2 ProjectIntoQuad(
        Vector2 upperLeft,
        Vector2 upperRight,
        Vector2 lowerLeft,
        Vector2 point)
    {
        var horizontal = upperRight - upperLeft;
        var vertical = lowerLeft - upperLeft;
        var relative = point - upperLeft;
        var determinant =
            horizontal.X * vertical.Y - horizontal.Y * vertical.X;
        if (MathF.Abs(determinant) <= 1e-6f)
            return new Vector2(float.PositiveInfinity);

        return new Vector2(
            (relative.X * vertical.Y - relative.Y * vertical.X) /
            determinant,
            (horizontal.X * relative.Y - horizontal.Y * relative.X) /
            determinant);
    }

    public static Vector2 InterpolateUv(
        IReadOnlyList<Vector2> uv,
        Vector2 normalizedPoint)
    {
        var top = Vector2.Lerp(uv[0], uv[2], normalizedPoint.X);
        var bottom = Vector2.Lerp(uv[1], uv[3], normalizedPoint.X);
        return Vector2.Lerp(top, bottom, normalizedPoint.Y);
    }
}
