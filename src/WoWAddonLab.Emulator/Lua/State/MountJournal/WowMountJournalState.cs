namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMountJournalState
{
    public int MountsNeedingFanfare { get; set; }

    public int? SummonedMountId { get; set; }

    public int? AppliedMountEquipmentId { get; set; }

    public int MountEquipmentUnlockLevel { get; set; } = 20;

    public bool AreMountEquipmentEffectsSuppressed { get; set; }

    public bool IsDragonridingUnlocked { get; set; }

    public bool IsUsingDefaultFilters { get; set; } = true;

    public string SearchText { get; set; } = string.Empty;

    public int TotalMountCount { get; set; }

    public IList<int> MountIds { get; } = new List<int>();

    public IList<WowDisplayedMountInfoState> DisplayedMounts { get; } =
        new List<WowDisplayedMountInfoState>();

    public IDictionary<int, WowMountInfoExtraState> ExtraInfoByMountId { get; } =
        new Dictionary<int, WowMountInfoExtraState>();

    public IDictionary<int, bool> CollectedFilterSettings { get; } =
        new Dictionary<int, bool>
        {
            [1] = true,
            [2] = true,
            [3] = false,
            [4] = false
        };

    public ISet<int> ValidSourceFilters { get; } =
        new HashSet<int>(Enumerable.Range(1, 12));

    public IDictionary<int, bool> SourceFilterSettings { get; } =
        Enumerable.Range(1, 12).ToDictionary(index => index, _ => true);

    public IDictionary<int, bool> TypeFilterSettings { get; } =
        Enumerable.Range(1, 32).ToDictionary(index => index, _ => true);

    public ISet<WowItemLocation> MountEquipmentItemLocations { get; } =
        new HashSet<WowItemLocation>();
}
