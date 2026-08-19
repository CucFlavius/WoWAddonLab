namespace WoWAddonLab.Emulator.Lua;

public sealed class WowEquipmentSetState
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public int IconFileId { get; set; }
    public string? IconAsset { get; set; }
    public int? AssignedSpecIndex { get; set; }
    public bool IsEquipped { get; set; }
    public bool ContainsLockedItems { get; set; }
    public bool CanEquip { get; set; } = true;
    public int NumItems { get; set; }
    public int NumEquipped { get; set; }
    public int NumInInventory { get; set; }
    public int NumLost { get; set; }
    public int NumIgnored { get; set; }
    public IList<bool?> IgnoredSlots { get; } =
        Enumerable.Repeat<bool?>(null, 19).ToList();
    public IList<int?> ItemIds { get; } =
        Enumerable.Repeat<int?>(null, 19).ToList();
    public IList<int?> ItemLocations { get; } =
        Enumerable.Repeat<int?>(null, 19).ToList();
    public int PickupCount { get; internal set; }
    public int SaveCount { get; internal set; }
}
