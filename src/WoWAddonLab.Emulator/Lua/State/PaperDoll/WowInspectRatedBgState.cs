namespace WoWAddonLab.Emulator.Lua;

public sealed record WowInspectRatedBgState(
    int Rating = 0,
    int Played = 0,
    int Won = 0);
