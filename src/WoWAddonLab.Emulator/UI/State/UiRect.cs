using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiRect(float Left, float Bottom, float Width, float Height)
{
    public float Right => Left + Width;
    public float Top => Bottom + Height;
    public Vector2 Center => new(Left + Width / 2, Bottom + Height / 2);

    public bool Contains(Vector2 point) =>
        point.X >= Left && point.X <= Right && point.Y >= Bottom && point.Y <= Top;
}
