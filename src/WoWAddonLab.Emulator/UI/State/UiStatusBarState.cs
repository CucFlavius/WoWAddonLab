using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiStatusBarState
{
    public double Minimum { get; set; }
    public double Maximum { get; set; } = 1;
    public double Value { get; set; }
    public bool RangeInitialized { get; set; }
    public bool ValueInitialized { get; set; }
    public UiDurationState? TimerDuration { get; set; }
    public int TimerDirection { get; set; }
    public bool InterpolationActive { get; set; }
    public double DisplayNormalizedValue { get; set; }
    public string Orientation { get; set; } = "HORIZONTAL";
    public bool ReverseFill
    {
        get => FillStyle == 3;
        set => FillStyle = value ? 3 : 0;
    }
    public bool RotatesTexture { get; set; }
    public int FillStyle { get; set; }
    public int? TextureId { get; set; }
}
