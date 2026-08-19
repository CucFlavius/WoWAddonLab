namespace WoWAddonLab.Emulator.Lua;

public sealed class WowHousingBasicModeState
{
    public bool DecorSelected { get; set; }
    public bool FreePlaceEnabled { get; set; }
    public bool GridSnapEnabled { get; set; }
    public bool GridVisible { get; set; }
    public bool HouseExteriorHovered { get; set; }
    public bool HouseExteriorSelected { get; set; }
    public bool HoveringDecor { get; set; }
    public bool PlacingNewDecor { get; set; }
    public double DecorRotationDegrees { get; set; }
    public double HouseRotationDegrees { get; set; }
    public int? PreviewDecorRecordId { get; set; }
}
