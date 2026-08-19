namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCovenantCallingsState
{
    public bool AreCallingsUnlocked { get; set; }
    public int RequestCount { get; internal set; }
}
