namespace WoWAddonLab.Emulator.Lua;

public sealed class WowDamageMeterState
{
    public bool IsAvailable { get; set; }
    public string AvailabilityReason { get; set; } = string.Empty;

    public IList<WowDamageMeterAvailableCombatSession> AvailableSessions
        { get; } = new List<WowDamageMeterAvailableCombatSession>();

    public IDictionary<(uint SessionId, WowDamageMeterType Type),
        WowDamageMeterCombatSession> SessionsById { get; } =
        new Dictionary<(uint, WowDamageMeterType), WowDamageMeterCombatSession>();

    public IDictionary<(WowDamageMeterSessionType SessionType,
            WowDamageMeterType Type),
        WowDamageMeterCombatSession> SessionsByType { get; } =
        new Dictionary<(WowDamageMeterSessionType, WowDamageMeterType),
            WowDamageMeterCombatSession>();

    public IDictionary<(uint SessionId, WowDamageMeterType Type,
            string? SourceGuid, int? SourceCreatureId),
        WowDamageMeterCombatSessionSource> SourcesById { get; } =
        new Dictionary<(uint, WowDamageMeterType, string?, int?),
            WowDamageMeterCombatSessionSource>();

    public IDictionary<(WowDamageMeterSessionType SessionType,
            WowDamageMeterType Type, string? SourceGuid,
            int? SourceCreatureId),
        WowDamageMeterCombatSessionSource> SourcesByType { get; } =
        new Dictionary<(WowDamageMeterSessionType, WowDamageMeterType,
            string?, int?), WowDamageMeterCombatSessionSource>();

    public IDictionary<WowDamageMeterSessionType, double> SessionDurations
        { get; } =
        new Dictionary<WowDamageMeterSessionType, double>();

    public int ResetCount { get; private set; }

    public void ResetAllCombatSessions()
    {
        AvailableSessions.Clear();
        SessionsById.Clear();
        SessionsByType.Clear();
        SourcesById.Clear();
        SourcesByType.Clear();
        SessionDurations.Clear();
        ResetCount++;
    }
}
