using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiMessageFrameLine
{
    public required int FontStringId { get; init; }
    public bool Active { get; set; }
    public uint MessageId { get; set; }
    public float TimeVisible { get; set; }
    public float FadeDuration { get; set; }
}
