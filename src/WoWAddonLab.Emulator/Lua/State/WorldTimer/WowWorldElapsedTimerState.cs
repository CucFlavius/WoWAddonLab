namespace WoWAddonLab.Emulator.Lua;

public sealed record WowWorldElapsedTimerState(
    int Id,
    string Name,
    double ElapsedTime,
    byte Type);
