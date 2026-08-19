namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSkirmishInfoState(
    string Name,
    int MatchmakingType,
    int MinPlayers,
    int MaxPlayers,
    int Icon,
    string LongDescription,
    string ShortDescription);
