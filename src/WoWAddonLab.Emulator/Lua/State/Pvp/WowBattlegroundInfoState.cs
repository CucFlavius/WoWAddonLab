namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBattlegroundInfoState(
    string? Name,
    int? Icon,
    string? GameType,
    string? ShortDescription,
    string? LongDescription,
    string? MapDescription,
    int MaxPlayers,
    int? BattlegroundId,
    int? LfgDungeonId,
    int? MapId,
    bool IsHoliday,
    bool IsRandom,
    bool CanEnter,
    bool IsTrainingGround);
