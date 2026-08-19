using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowItemState
{
    private IWowItemProvider? _provider;

    public IDictionary<int, WowItemData> Items { get; } =
        new Dictionary<int, WowItemData>();

    public IDictionary<int, string?> InventorySlotNames { get; } =
        new Dictionary<int, string?>();

    public IDictionary<WowItemLocation, int> LocationItemIds { get; } =
        new Dictionary<WowItemLocation, int>();

    public IDictionary<string, WowItemLocation> LocationsByGuid { get; } =
        new Dictionary<string, WowItemLocation>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<int, WowItemCountData> Counts { get; } =
        new Dictionary<int, WowItemCountData>();

    public IDictionary<int, string> Classes { get; } =
        new Dictionary<int, string>();

    public IDictionary<int, IReadOnlyList<int>> SpecializationIds { get; } =
        new Dictionary<int, IReadOnlyList<int>>();

    public IDictionary<WowItemLocation, UiItemTransmogInfo>
        AppliedTransmogByLocation { get; } =
            new Dictionary<WowItemLocation, UiItemTransmogInfo>();

    public IDictionary<(int ClassId, int SubClassId), WowItemSubClassData>
        SubClasses { get; } =
            new Dictionary<(int ClassId, int SubClassId), WowItemSubClassData>();

    public void SetProvider(IWowItemProvider? provider) => _provider = provider;

    public bool TryGetItem(int itemId, out WowItemData item)
    {
        if (Items.TryGetValue(itemId, out item!))
            return true;
        return _provider?.Items.TryGetValue(itemId, out item!) == true;
    }
}
