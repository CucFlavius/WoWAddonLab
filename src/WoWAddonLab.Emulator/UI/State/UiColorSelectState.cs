using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiColorSelectState
{
    public float Hue { get; set; }
    public float Saturation { get; set; }
    public float Value { get; set; } = 1;
    public float Alpha { get; set; } = 1;
    public int? WheelTextureId { get; set; }
    public int? WheelThumbTextureId { get; set; }
    public int? ValueTextureId { get; set; }
    public int? ValueThumbTextureId { get; set; }
    public int? AlphaTextureId { get; set; }
    public int? AlphaThumbTextureId { get; set; }
    public bool SelectingWheel { get; set; }
    public bool SelectingValue { get; set; }
    public bool SelectingAlpha { get; set; }
    public bool Dirty { get; set; }
}
