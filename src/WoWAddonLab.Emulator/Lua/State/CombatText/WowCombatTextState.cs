namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCombatTextState
{
    public string? ActiveUnit { get; set; }
    public IList<object?> CurrentEventInfo { get; } = new List<object?>();
}
