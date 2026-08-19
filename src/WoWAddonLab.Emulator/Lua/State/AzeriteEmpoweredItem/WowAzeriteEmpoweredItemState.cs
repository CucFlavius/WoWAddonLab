namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAzeriteEmpoweredItemState
{
    public double RespecCost { get; set; }

    public bool IsHeartOfAzerothEquipped { get; set; }

    public IDictionary<WowItemLocation, WowAzeriteEmpoweredItemData>
        ItemsByLocation { get; } =
            new Dictionary<WowItemLocation, WowAzeriteEmpoweredItemData>();

    public ISet<int> EmpoweredItemIds { get; } = new HashSet<int>();

    public IDictionary<(int ItemId, int? ClassId),
        IReadOnlyList<WowAzeriteEmpoweredItemTierInfo>> TierInfoByItem
        { get; } =
            new Dictionary<(int, int?),
                IReadOnlyList<WowAzeriteEmpoweredItemTierInfo>>();

    public IDictionary<int, WowAzeriteEmpoweredItemPowerInfo> Powers
        { get; } =
            new Dictionary<int, WowAzeriteEmpoweredItemPowerInfo>();

    public IDictionary<(WowItemLocation Location, int PowerId, int Level),
        WowAzeriteEmpoweredItemPowerText> PowerText { get; } =
            new Dictionary<(WowItemLocation, int, int),
                WowAzeriteEmpoweredItemPowerText>();

    public IDictionary<int, IReadOnlyList<WowAzeriteEmpoweredItemSpecInfo>>
        SpecsByPowerId { get; } =
            new Dictionary<int,
                IReadOnlyList<WowAzeriteEmpoweredItemSpecInfo>>();

    public ISet<(int PowerId, int SpecId)> AvailablePowersBySpec { get; } =
        new HashSet<(int, int)>();

    public ISet<(int ItemId, int? ClassId)> DisplayablePreviewSources
        { get; } = new HashSet<(int, int?)>();

    public IList<WowItemLocation> ConfirmedRespecLocations { get; } =
        new List<WowItemLocation>();

    public IList<WowAzeriteEmpoweredItemSelectRequest> SelectRequests
        { get; } = new List<WowAzeriteEmpoweredItemSelectRequest>();

    public IList<WowItemLocation> SetHasBeenViewedRequests { get; } =
        new List<WowItemLocation>();
}
