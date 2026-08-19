namespace WoWAddonLab.Emulator.Lua;

public sealed class WowEquipmentState
{
    private IWowInventorySlotProvider? _inventorySlotProvider;

    public WowWeaponEnchantState MainHandEnchant { get; set; } = new(false, 0, 0, 0);
    public WowWeaponEnchantState OffHandEnchant { get; set; } = new(false, 0, 0, 0);
    public double Corruption { get; set; }
    public int EquipmentSetCount { get; set; }
    public int? LastPickedInventorySlot { get; set; }
    public IDictionary<(string UnitToken, int SlotId), WowInventoryItemState>
        InventoryItems { get; } =
            new Dictionary<(string UnitToken, int SlotId), WowInventoryItemState>();
    public IDictionary<int, int?> InventorySlotTextureFileIds { get; } =
        new Dictionary<int, int?>();
    public int[] InventoryAlertStatuses { get; } = new int[10];
    public bool CanUseEquipmentSets { get; set; } = true;
    public IList<WowEquipmentSetState> EquipmentSets { get; } =
        new List<WowEquipmentSetState>();
    public bool[] IgnoredSlotsForSave { get; } = new bool[19];
    public int NextEquipmentSetId { get; set; } = 1;

    public void SetInventorySlotProvider(
        IWowInventorySlotProvider? provider) =>
        _inventorySlotProvider = provider;

    public bool TryGetInventorySlot(
        string name,
        out WowInventorySlotInfo info)
    {
        if (_inventorySlotProvider is not null &&
            _inventorySlotProvider.InventorySlots.TryGetValue(
                name,
                out info!))
        {
            return true;
        }

        info = null!;
        return false;
    }
}
