using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiTooltipTextureState
{
    public required int TextureId { get; init; }
    public int LineIndex { get; init; }
    public float Width { get; init; } = 12;
    public float Height { get; init; } = 12;
    public float VerticalOffset { get; init; }
    public UiInsets Margin { get; init; } = new(0, 8, 0, 0);
    public int Anchor { get; init; }
    public int RelativeRegion { get; init; }
}
