namespace WoWAddonLab.Emulator.Lua;

public sealed class WowProfessionState
{
    public int? PrimaryProfession1 { get; set; }
    public int? PrimaryProfession2 { get; set; }
    public int? Archaeology { get; set; }
    public int? Fishing { get; set; }
    public int? Cooking { get; set; }
    public string? NewSpecReminderProfessionName { get; set; }
    public bool ShouldShowPointsReminder { get; set; }
}
