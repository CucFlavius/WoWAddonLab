namespace WoWAddonLab.Configuration;

public sealed class SimulationProfile
{
    public SimulationUnitProfile Player { get; set; } = new() { Id = "player" };
    public List<SimulationUnitProfile> Units { get; set; } = [];
    public string? TargetUnitId { get; set; }
    public string? FocusUnitId { get; set; }
    public string? MouseoverUnitId { get; set; }
    public long? Money { get; set; }
    public string? ZoneText { get; set; }
    public string? SubZoneText { get; set; }
    public bool? IsResting { get; set; }
    public int? MapId { get; set; }
    public int? MapTileX { get; set; }
    public int? MapTileY { get; set; }
}
