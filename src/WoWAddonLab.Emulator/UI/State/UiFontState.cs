using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiFontState
{
    public string Text { get; set; } = string.Empty;
    public string FontPath { get; set; } = "Fonts\\FRIZQT__.TTF";
    public float FontSize { get; set; } = 12;
    public string FontFlags { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public float TextScale { get; set; } = 1;
    public Vector4 Color { get; set; } = Vector4.One;
    public Vector4 ShadowColor { get; set; } = new(0, 0, 0, 1);
    public Vector2 ShadowOffset { get; set; }
    public string JustifyHorizontal { get; set; } = "CENTER";
    public string JustifyVertical { get; set; } = "MIDDLE";
    public bool HasLocalJustifyHorizontal { get; set; }
    public bool HasLocalJustifyVertical { get; set; }
    public float Spacing { get; set; }
    public int MaximumLines { get; set; }
    public bool IndentedWordWrap { get; set; }
    public bool WordWrap { get; set; } = true;
    public bool NonSpaceWrap { get; set; }
    public bool CanBeUserScaled { get; set; }
    public UiFontOverrides LocalOverrides { get; set; }
}
