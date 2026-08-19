namespace WoWAddonLab.Emulator.Lua;

public sealed class WowInstanceState
{
    public bool IsInInstance { get; set; }
    public string InstanceType { get; set; } = "none";
    public string Name { get; set; } = string.Empty;
    public int DungeonDifficultyId { get; set; } = 1;
    public int? RaidDifficultyId { get; set; } = 14;
    public int? LegacyRaidDifficultyId { get; set; } = 3;
    public string DifficultyName { get; set; } = string.Empty;
    public int MaximumPlayers { get; set; }
    public int DynamicDifficulty { get; set; }
    public bool? IsDynamic { get; set; }
    public int InstanceId { get; set; }
    public int InstanceGroupSize { get; set; }
    public int? LfgDungeonId { get; set; }
    public bool IsRaid { get; set; }
    public int InstanceBootTimeRemainingSeconds { get; set; }
    public int InstanceLockTimeRemainingSeconds { get; set; }
    public bool IsInstanceLockExtending { get; set; }
    public int InstanceLockEncounterCount { get; set; }
    public int InstanceLockCompletedEncounterCount { get; set; }
    public int SavedInstanceCount { get; set; }
    public int SavedWorldBossCount { get; set; }
    public int RaidInfoRequestCount { get; set; }
    public bool CanChangeDifficulty { get; set; } = true;
    public bool DifficultyChangeNotOnCooldown { get; set; } = true;
    public bool CanShowResetInstances { get; set; }
    public IDictionary<int, WowDifficultyInfoState> Difficulties { get; } =
        new Dictionary<int, WowDifficultyInfoState>();
    public IList<WowInstanceLockEncounterState> LockEncounters { get; } =
        new List<WowInstanceLockEncounterState>();
    public IDictionary<int, bool> MapDifficultyChanges { get; } =
        new Dictionary<int, bool>();
    public IDictionary<int, WowModifiedInstanceInfoState> ModifiedInstances { get; } =
        new Dictionary<int, WowModifiedInstanceInfoState>();
}
