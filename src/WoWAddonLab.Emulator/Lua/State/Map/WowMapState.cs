namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMapState
{
    public int BestMapForPlayer { get; set; } = 84;
    public WowUserWaypoint? UserWaypoint { get; internal set; }
    public int PreloadRequestCount { get; internal set; }
    public IList<int> PreloadRequests { get; } = new List<int>();
    public IDictionary<string, int?> BestMapByUnit { get; } =
        new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, IDictionary<int, WowMapPosition>> PlayerPositionsByUnit { get; } =
        new Dictionary<string, IDictionary<int, WowMapPosition>>(
            StringComparer.OrdinalIgnoreCase);
    public IDictionary<int, WowMapPosition> UserWaypointProjections { get; } =
        new Dictionary<int, WowMapPosition>();
    public IDictionary<int, WowMapDetails> MapOverrides { get; } =
        new Dictionary<int, WowMapDetails>();
    public IDictionary<int, string> AreaNameOverrides { get; } =
        new Dictionary<int, string>();
    public IDictionary<int, WowMapArt> MapArtOverrides { get; } =
        new Dictionary<int, WowMapArt>();
    public IDictionary<int, WowMapLevels> LevelOverrides { get; } =
        new Dictionary<int, WowMapLevels>();
    public IDictionary<int, int> MapGroupIds { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, IReadOnlyList<WowMapGroupMemberInfo>> MapGroupMembers { get; } =
        new Dictionary<int, IReadOnlyList<WowMapGroupMemberInfo>>();
    public IDictionary<int, IReadOnlyList<WowMapBannerInfo>> MapBanners { get; } =
        new Dictionary<int, IReadOnlyList<WowMapBannerInfo>>();
    public IDictionary<int, IReadOnlyList<WowMapLinkInfo>> MapLinks { get; } =
        new Dictionary<int, IReadOnlyList<WowMapLinkInfo>>();
    public IDictionary<int, WowMapHighlight> MapHighlightPulses { get; } =
        new Dictionary<int, WowMapHighlight>();
    public IDictionary<int, bool> NavBarValidityOverrides { get; } =
        new Dictionary<int, bool>();
}
