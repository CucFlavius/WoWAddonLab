namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGarrisonInfoState(
    int GarrisonLevel,
    string? GarrisonName = null,
    float MapX = 0,
    float MapY = 0);
