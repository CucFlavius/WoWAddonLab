namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGameModeExpBarState
{
    public int CurrentLevel { get; set; } = 1;
    public int CurrentXp { get; set; }
    public int MaxXpForLevel { get; set; } = 1000;
}
