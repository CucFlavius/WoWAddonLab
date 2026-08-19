namespace WoWAddonLab.Emulator.Lua;

public sealed class WowInventoryItemState
{
    public int ItemId { get; init; }
    public int? TextureFileId { get; set; }
    public int? Quality { get; set; }
    public string? Link { get; set; }
    public bool IsLocked { get; set; }
    public int? CurrentDurability { get; set; }
    public int? MaxDurability { get; set; }
}
