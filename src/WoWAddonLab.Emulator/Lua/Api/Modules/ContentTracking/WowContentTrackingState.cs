using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowContentTrackingState
{
    public bool CollectableSourceTrackingEnabled { get; set; } = true;
    public ISet<int> CollectableSourceTypes { get; } = new HashSet<int> { 0, 3 };
    public ISet<(int Type, int Id)> TrackableEntries { get; } =
        new HashSet<(int Type, int Id)>();
    public IList<(int Type, int Id)> TrackedEntries { get; } =
        new List<(int Type, int Id)>();
    public IDictionary<(int Type, int Id, bool IgnoreWaypoint), WowContentTrackingBestMapResult>
        BestMaps { get; } =
        new Dictionary<(int Type, int Id, bool IgnoreWaypoint), WowContentTrackingBestMapResult>();
    public IDictionary<(int Type, int Id), WowContentTrackingTarget> CurrentTargets { get; } =
        new Dictionary<(int Type, int Id), WowContentTrackingTarget>();
    public IDictionary<int, WowContentTrackingEncounterInfo> EncounterInfo { get; } =
        new Dictionary<int, WowContentTrackingEncounterInfo>();
    public IDictionary<(int Type, int Id, int MapId), WowContentTrackingWaypointResult>
        NextWaypoints { get; } =
        new Dictionary<(int Type, int Id, int MapId), WowContentTrackingWaypointResult>();
    public IDictionary<(int TargetType, int TargetId, bool IncludeHyperlinks), string>
        ObjectiveTexts { get; } =
        new Dictionary<(int TargetType, int TargetId, bool IncludeHyperlinks), string>();
    public IDictionary<(int Type, int Id), string> Titles { get; } =
        new Dictionary<(int Type, int Id), string>();
    public IDictionary<(int Type, int MapId), WowContentTrackingMapResult> TrackablesOnMaps
        { get; } =
        new Dictionary<(int Type, int MapId), WowContentTrackingMapResult>();
    public IDictionary<int, WowContentTrackingVendorInfo> VendorInfo { get; } =
        new Dictionary<int, WowContentTrackingVendorInfo>();
    public IDictionary<(int Type, int Id), string> WaypointTexts { get; } =
        new Dictionary<(int Type, int Id), string>();
    public IDictionary<(int Type, int Id), WowContentTrackingNavigableResult> Navigability
        { get; } =
        new Dictionary<(int Type, int Id), WowContentTrackingNavigableResult>();
    public int? LastStopType { get; internal set; }
}
