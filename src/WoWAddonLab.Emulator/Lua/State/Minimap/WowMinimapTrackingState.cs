namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMinimapTrackingState
{
    public required string Name { get; init; }
    public uint Texture { get; set; }
    public bool Active { get; set; }
    public bool DefaultActive { get; set; }
    public string Type { get; set; } = "other";
    public int SubType { get; set; }
    public int? SpellId { get; set; }
    public int Filter { get; set; }
}
