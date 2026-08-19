namespace WoWAddonLab.Emulator.Lua;

public sealed class WowContainerState
{
    public IDictionary<string, IDictionary<int, bool>> ProfessionBagSlotsByUnit {
        get;
    } = new Dictionary<string, IDictionary<int, bool>>(
        StringComparer.OrdinalIgnoreCase);
    public IDictionary<int, ISet<int>> BagSlotFlags { get; } =
        new Dictionary<int, ISet<int>>();
    public IDictionary<int, int> ContainerSlotCounts { get; } =
        new Dictionary<int, int>();
    public ISet<int> FilteredContainerIds { get; } = new HashSet<int>();
    public int TotalNumberOfFreeBagSlots { get; set; }
    public bool BackpackAutosortDisabled { get; set; }
    public bool BackpackSellJunkDisabled { get; set; }
    public string ItemSearch { get; set; } = string.Empty;
}
