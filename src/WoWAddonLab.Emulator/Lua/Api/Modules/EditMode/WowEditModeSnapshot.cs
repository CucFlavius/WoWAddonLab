namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowEditModeSnapshot
{
    public int ActiveLayout { get; set; } = 1;
    public Dictionary<int, int> AccountSettings { get; set; } = [];
    public List<WowEditModeLayoutInfo> Layouts { get; set; } = [];
}
