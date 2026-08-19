using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiTextureSliceData(
    float MarginLeft,
    float MarginTop,
    float MarginRight,
    float MarginBottom,
    UiTextureSliceMode Mode);
