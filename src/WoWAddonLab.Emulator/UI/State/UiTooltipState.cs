using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiTooltipState
{
    public List<UiTooltipLineState> Lines { get; } = [];
    public List<UiTooltipTextureState> Textures { get; } = [];
    public UiInsets Padding { get; set; }
    public float MinimumWidth { get; set; }
    public bool ForceMinimumWidth { get; set; }
    public float? CustomLineSpacing { get; set; }
    public float? CustomWordWrapMinWidth { get; set; }
    public bool ShrinkToFitWrapped { get; set; } = true;
    public bool AllowShowWithNoLines { get; set; }
    public float FadeRemaining { get; set; }
}
