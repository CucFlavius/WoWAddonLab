namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTotemSlotState
{
    public required int Slot { get; init; }
    public string Name { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double Duration { get; set; }
    public uint IconFileId { get; set; }
    public double ModRate { get; set; } = 1;
    public int SpellId { get; set; }
    public bool CannotDismiss { get; set; }
}
