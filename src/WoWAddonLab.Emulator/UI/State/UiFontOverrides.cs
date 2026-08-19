using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

[Flags]
public enum UiFontOverrides
{
    None = 0,
    FontPath = 1 << 0,
    FontSize = 1 << 1,
    FontFlags = 1 << 2,
    TextScale = 1 << 3,
    Color = 1 << 4,
    ShadowColor = 1 << 5,
    ShadowOffset = 1 << 6,
    JustifyHorizontal = 1 << 7,
    JustifyVertical = 1 << 8,
    Spacing = 1 << 9,
    MaximumLines = 1 << 10,
    IndentedWordWrap = 1 << 11,
    WordWrap = 1 << 12,
    NonSpaceWrap = 1 << 13,
    CanBeUserScaled = 1 << 14
}
