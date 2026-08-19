using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiUnitPositionEntry
{
    public required string Unit { get; init; }
    public required string UnitGuid { get; init; }
    public int? TextureId { get; set; }
    public string? Asset { get; set; }
    public uint? FileDataId { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Vector4 Color { get; set; } = Vector4.One;
    public int SubLayer { get; set; }
    public bool ShowFacing { get; set; }
}
