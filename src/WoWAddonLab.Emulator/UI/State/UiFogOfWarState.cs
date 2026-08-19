using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiFogOfWarState
{
    public int? BackgroundTextureId { get; set; }
    public int?[] MaskTextureIds { get; } = new int?[3];
    public int UiMapId { get; set; }
    public string? BackgroundAtlas { get; set; }
    public string? BackgroundTexture { get; set; }
    public uint? BackgroundTextureFileDataId { get; set; }
    public bool BackgroundTextureTilesHorizontally { get; set; }
    public bool BackgroundTextureTilesVertically { get; set; }
    public string? MaskAtlas { get; set; }
    public string? MaskTexture { get; set; }
    public uint? MaskTextureFileDataId { get; set; }
    public float MaskScalar { get; set; } = 1;
}
