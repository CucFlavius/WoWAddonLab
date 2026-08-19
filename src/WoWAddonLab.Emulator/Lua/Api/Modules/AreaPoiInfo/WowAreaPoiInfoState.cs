using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAreaPoiInfoState
{
    public WowAreaPoiInfoState(
        int areaPoiId,
        double x = 0,
        double y = 0)
    {
        AreaPoiId = areaPoiId;
        X = x;
        Y = y;
    }

    public int AreaPoiId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? LinkedUiMapId { get; set; }
    public int? TextureIndex { get; set; }
    public int? TooltipWidgetSet { get; set; }
    public int? IconWidgetSet { get; set; }
    public string? AtlasName { get; set; }
    public string? UiTextureKit { get; set; }
    public bool ShouldGlow { get; set; }
    public int? FactionId { get; set; }
    public bool IsPrimaryMapForPoi { get; set; }
    public bool IsAlwaysOnFlightmap { get; set; }
    public bool? AddPaddingAboveTooltipWidgets { get; set; }
    public bool HighlightWorldQuestsOnHover { get; set; }
    public bool HighlightVignettesOnHover { get; set; }
    public bool IsCurrentEvent { get; set; }
    public bool IsSuppressible { get; set; }
    public bool IsLocked { get; set; }
}
