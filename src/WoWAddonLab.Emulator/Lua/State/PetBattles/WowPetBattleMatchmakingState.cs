namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPetBattleMatchmakingState(
    string Status,
    int EstimatedSeconds,
    double QueuedSeconds);
