namespace WoWAddonLab.Emulator.Lua;

public sealed record WowInspectPvpState(
    int Rating = 0,
    int GamesWon = 0,
    int GamesPlayed = 0,
    int RoundsWon = 0,
    int RoundsPlayed = 0);
