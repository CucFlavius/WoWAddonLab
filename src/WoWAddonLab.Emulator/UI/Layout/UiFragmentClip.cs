namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiFragmentClip(
    float Left,
    float Bottom,
    float Right,
    float Top)
{
    public bool IsEmpty => Left >= Right || Bottom >= Top;

    public static UiFragmentClip FromTopLeft(
        float left,
        float top,
        float right,
        float bottom,
        int framebufferWidth,
        int framebufferHeight)
    {
        var width = Math.Max(0, framebufferWidth);
        var height = Math.Max(0, framebufferHeight);
        return new UiFragmentClip(
            Math.Clamp(left, 0, width),
            Math.Clamp(height - bottom, 0, height),
            Math.Clamp(right, 0, width),
            Math.Clamp(height - top, 0, height));
    }

    public UiIntegerScissor ConservativeScissor()
    {
        var left = (int)MathF.Floor(Left);
        var bottom = (int)MathF.Floor(Bottom);
        var right = (int)MathF.Ceiling(Right);
        var top = (int)MathF.Ceiling(Top);
        return new UiIntegerScissor(
            left,
            bottom,
            Math.Max(0, right - left),
            Math.Max(0, top - bottom));
    }

    public bool ContainsFragmentCenter(float x, float y) =>
        x >= Left && x < Right && y >= Bottom && y < Top;
}
