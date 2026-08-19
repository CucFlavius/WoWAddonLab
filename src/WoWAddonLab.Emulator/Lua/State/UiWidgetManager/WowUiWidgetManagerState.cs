namespace WoWAddonLab.Emulator.Lua;

public sealed class WowUiWidgetManagerState
{
    public int ObjectiveTrackerWidgetSetId { get; set; } = 240;
    public int BelowMinimapWidgetSetId { get; set; } = 2;
    public int PowerBarWidgetSetId { get; set; } = 283;
    public int TopCenterWidgetSetId { get; set; } = 1;
    public IDictionary<int, WowUiWidgetSetInfoState> WidgetSets { get; } =
        new Dictionary<int, WowUiWidgetSetInfoState>();
    public IDictionary<int, List<WowUiWidgetInfoState>> WidgetsBySetId { get; } =
        new Dictionary<int, List<WowUiWidgetInfoState>>();
    public IDictionary<(string Function, int WidgetId), IReadOnlyDictionary<string, object?>>
        VisualizationInfo { get; } =
        new Dictionary<(string Function, int WidgetId), IReadOnlyDictionary<string, object?>>();
    public ISet<string> RegisteredUnitTokens { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ISet<string> RegisteredUnitGuids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public string? ProcessingUnit { get; set; }
    public bool ProcessingUnitIsGuid { get; set; }
}
