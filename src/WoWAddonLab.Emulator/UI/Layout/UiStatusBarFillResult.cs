using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiStatusBarFillResult(
    UiRect Bounds,
    Vector2[]? NormalizedUv);
