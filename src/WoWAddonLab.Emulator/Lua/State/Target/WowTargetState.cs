namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTargetState
{
    public bool IsLoose { get; set; }
    public bool HasTarget { get; set; }
    public bool HasFocus { get; set; }
    public string? TargetGuid { get; set; }
    public string? FocusGuid { get; set; }
}
