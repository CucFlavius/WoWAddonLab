namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMythicPlusAffixScoreState(
    string Name,
    int Score,
    int Level,
    int DurationSec,
    bool OverTime);
