using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiTooltipLineState
{
    public required int LeftId { get; init; }
    public required int RightId { get; init; }
    public bool Wrap { get; set; }
    public float LeftPadding { get; set; }
}
