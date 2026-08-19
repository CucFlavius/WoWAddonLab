namespace WoWAddonLab.Emulator.Lua;

public sealed class WowBuybackItemData
{
    public int ItemId { get; set; }
    public string? Name { get; set; }
    public int? TextureFileId { get; set; }
    public double Price { get; set; }
    public int StackCount { get; set; } = 1;
    public bool IsUsable { get; set; } = true;
    public bool? AdditionalFlag { get; set; }
    public string? Link { get; set; }
}
