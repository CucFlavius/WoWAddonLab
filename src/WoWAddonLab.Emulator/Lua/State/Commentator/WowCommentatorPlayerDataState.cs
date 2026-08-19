namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCommentatorPlayerDataState(
    string UnitToken,
    string Name,
    int Faction,
    int Specialization,
    int DamageDone,
    int DamageTaken,
    int HealingDone,
    int HealingTaken,
    int Kills,
    int Deaths,
    int SoloShuffleRoundWins,
    int SoloShuffleRoundLosses);
