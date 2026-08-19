using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiLineState
{
    public UiAnchor? Start { get; set; }
    public UiAnchor? End { get; set; }
    public UiTextureState Texture { get; } = new();
    public string? TextureAsset
    {
        get => Texture.Asset;
        set
        {
            Texture.Asset = value;
            Texture.FileDataId = null;
            Texture.IsColor = false;
        }
    }
    public float Thickness { get; set; } = 1;
    public float HitRectThickness { get; set; }
    public Vector4 Color
    {
        get => Texture.VertexColor;
        set
        {
            Texture.VertexColor = value;
            if (Texture.Asset is null && Texture.FileDataId is null)
            {
                Texture.IsColor = true;
                Texture.Color = Vector4.One;
            }
        }
    }
}
