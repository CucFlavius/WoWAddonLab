namespace WoWAddonLab.Emulator.Lua;

public sealed record WowInvasionInfo(
    int InvasionId,
    string Name,
    float X,
    float Y,
    string? AtlasName = null,
    int? RewardQuestId = null,
    int? TimeLeftMinutes = null,
    bool IsAvailable = true);
