using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiMinimapBlobStyle
{
    public UiMinimapBlobStyle(
        byte insideAlpha,
        byte outsideAlpha,
        byte ringAlpha,
        string? insideTexture = null,
        string? outsideTexture = null,
        string? ringTexture = null)
    {
        InsideAlpha = insideAlpha;
        OutsideAlpha = outsideAlpha;
        RingAlpha = ringAlpha;
        InsideTexture = insideTexture;
        OutsideTexture = outsideTexture;
        RingTexture = ringTexture;
    }

    public byte InsideAlpha { get; set; }
    public string? InsideTexture { get; set; }
    public uint? InsideTextureFileDataId { get; set; }
    public byte OutsideAlpha { get; set; }
    public string? OutsideTexture { get; set; }
    public uint? OutsideTextureFileDataId { get; set; }
    public string? OutsideSelectedTexture { get; set; }
    public uint? OutsideSelectedTextureFileDataId { get; set; }
    public byte RingAlpha { get; set; }
    public float RingScalar { get; set; } = 1;
    public string? RingTexture { get; set; }
    public uint? RingTextureFileDataId { get; set; }
}
