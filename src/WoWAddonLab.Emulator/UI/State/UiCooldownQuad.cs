using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiCooldownQuad(
    Vector2 UpperLeft,
    Vector2 LowerLeft,
    Vector2 UpperRight,
    Vector2 LowerRight);
