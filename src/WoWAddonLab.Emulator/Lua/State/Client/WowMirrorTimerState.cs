namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMirrorTimerState(
    string Name,
    int StartValue,
    int MaximumValue,
    int Scale,
    int Paused,
    string? Label,
    int SpellId);
