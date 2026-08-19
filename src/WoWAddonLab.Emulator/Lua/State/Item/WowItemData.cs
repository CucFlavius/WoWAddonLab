using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowItemData
{
    public int ItemId { get; set; }
    public string? Name { get; set; }
    public string? Link { get; set; }
    public byte Quality { get; set; }
    public int ItemLevel { get; set; }
    public int? PreviewItemLevel { get; set; }
    public int? SparseItemLevel { get; set; }
    public int MinimumLevel { get; set; }
    public string? ItemType { get; set; }
    public string? ItemSubType { get; set; }
    public int StackCount { get; set; } = 1;
    public uint Family { get; set; }
    public string? EquipLocation { get; set; }
    public int TextureFileId { get; set; }
    public int SellPrice { get; set; }
    public int ClassId { get; set; }
    public int SubClassId { get; set; }
    public int BindType { get; set; }
    public int ExpansionId { get; set; }
    public int? SetId { get; set; }
    public bool IsCraftingReagent { get; set; }
    public string? Description { get; set; }
    public WowItemCooldownData Cooldown { get; set; } =
        new(0, 0, false);
}
