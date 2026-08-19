using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiHtmlContentNode
{
    public required int RegionId { get; init; }
    public string? TextType { get; init; }
    public string Align { get; init; } = "LEFT";
}
