namespace WoWAddonLab.Emulator.Lua;

public sealed class WowReputationState
{
    public IList<WowFactionDataState> Factions { get; } = new List<WowFactionDataState>();
    public WowFactionDataState? WatchedFaction { get; set; }
    public WowFactionDataState? GuildFaction { get; set; }
    public IDictionary<int, WowFactionParagonInfoState> ParagonInfoByFactionId
        { get; } = new Dictionary<int, WowFactionParagonInfoState>();
    public IList<int> ParagonPreloadRequests { get; } = new List<int>();
    public int? GuildRepExpirationTime { get; set; }
    public int SelectedFactionIndex { get; set; }
    public int SortType { get; set; }
    public bool LegacyReputationsShown { get; set; }
}
