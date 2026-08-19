namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCommentatorState
{
    public bool IsSpectating { get; set; }
    public bool TeamsSwapped { get; set; }
    public bool CanUseCommentatorCheats { get; set; }
    public bool IsUsingSmartCamera { get; set; }
    public bool IsSmartCameraLocked { get; set; }
    public bool IsMouseDisabled { get; set; }
    public bool ExitInstanceRequested { get; set; }
    public bool CameraLookAtPointSnapped { get; set; }
    public int DampeningPercent { get; set; }
    public double MatchDuration { get; set; }
    public double? TimeLeftInMatch { get; set; }
    public float FieldOfViewTarget { get; set; } = MathF.PI / 2;
    public float MoveSpeed { get; set; }
    public float SpeedFactor { get; set; }
    public float FollowCameraElasticSpeed { get; set; }
    public float FollowCameraMinimumSpeed { get; set; }
    public WowCommentatorVector3 CameraPosition { get; set; }
    public WowCommentatorVector3 CameraTargetPosition { get; set; }
    public WowCommentatorFollowRequest? FollowRequest { get; set; }
    public WowCommentatorLookAtRequest? LookAtRequest { get; set; }
    public uint[] TeamPlayerCounts { get; } = new uint[2];
    public WowCommentatorColor[] TeamColors { get; } =
    [
        new(0, 0, 0),
        new(0, 0, 0)
    ];
    public IDictionary<string, string> PlayerOverrideNames { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<WowCommentatorPlayerKey, WowCommentatorPlayerDataState>
        PlayersByPosition { get; } =
            new Dictionary<WowCommentatorPlayerKey, WowCommentatorPlayerDataState>();
    public IDictionary<string, uint> TeamIndexByUnit { get; } =
        new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
    public ISet<string> FlaggedUnits { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<WowCommentatorUnitSpellKey, WowCommentatorTimedEffectState>
        AurasByUnitAndSpell { get; } =
            new Dictionary<WowCommentatorUnitSpellKey, WowCommentatorTimedEffectState>();
    public IDictionary<WowCommentatorUnitSpellKey, WowCommentatorTimedEffectState>
        CooldownsByUnitAndSpell { get; } =
            new Dictionary<WowCommentatorUnitSpellKey, WowCommentatorTimedEffectState>();
    public IDictionary<string, WowCommentatorCrowdControlState> CrowdControlByUnit { get; } =
        new Dictionary<string, WowCommentatorCrowdControlState>(
            StringComparer.OrdinalIgnoreCase);
    public IDictionary<WowCommentatorUnitSpellKey, WowCommentatorSpellChargesState>
        SpellChargesByUnitAndSpell { get; } =
            new Dictionary<WowCommentatorUnitSpellKey, WowCommentatorSpellChargesState>();
    public IDictionary<string, WowCommentatorUnitDataState> UnitDataByUnit { get; } =
        new Dictionary<string, WowCommentatorUnitDataState>(
            StringComparer.OrdinalIgnoreCase);
    public IDictionary<int, WowCommentatorVector3> StartLocationsByMapId { get; } =
        new Dictionary<int, WowCommentatorVector3>();
    public IList<WowCommentatorTeamDirectoryEntryState> TeamDirectory { get; } =
        new List<WowCommentatorTeamDirectoryEntryState>();
    public IDictionary<int, int> IndirectSpellIdsByTrackedSpellId { get; } =
        new Dictionary<int, int>();
    public IDictionary<string, WowCommentatorTrackedAuraState> TrackedAurasByUnit { get; } =
        new Dictionary<string, WowCommentatorTrackedAuraState>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<WowCommentatorTrackedSpellKey, WowCommentatorTrackedSpellsState>
        TrackedSpellsByUnit { get; } =
            new Dictionary<WowCommentatorTrackedSpellKey, WowCommentatorTrackedSpellsState>();
    public IDictionary<string, float> AdditionalCameraWeightsByUnit { get; } =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    public IList<WowCommentatorSeriesState> Series { get; } =
        new List<WowCommentatorSeriesState>();

    public WowCommentatorSeriesState GetOrCreateSeries(
        string firstTeamName,
        string secondTeamName)
    {
        var existing = Series.FirstOrDefault(
            value =>
                (value.Teams[0].Name.Equals(
                     firstTeamName,
                     StringComparison.OrdinalIgnoreCase) &&
                 value.Teams[1].Name.Equals(
                     secondTeamName,
                     StringComparison.OrdinalIgnoreCase)) ||
                (value.Teams[0].Name.Equals(
                     secondTeamName,
                     StringComparison.OrdinalIgnoreCase) &&
                 value.Teams[1].Name.Equals(
                     firstTeamName,
                     StringComparison.OrdinalIgnoreCase)));
        if (existing is not null)
            return existing;

        var created = new WowCommentatorSeriesState(
            [
                new WowCommentatorSeriesTeamState(firstTeamName),
                new WowCommentatorSeriesTeamState(secondTeamName)
            ]);
        Series.Add(created);
        return created;
    }
}
