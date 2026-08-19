using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed record UiAnchor(
    string Point,
    int? RelativeToId,
    string RelativePoint,
    float X,
    float Y);
