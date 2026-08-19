namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRandomBattlegroundInfoState(
    bool CanQueue,
    int BattlegroundId,
    int BattlegroundIndex,
    bool HasRandomWinToday,
    int MinLevel,
    int MaxLevel,
    string Name);
