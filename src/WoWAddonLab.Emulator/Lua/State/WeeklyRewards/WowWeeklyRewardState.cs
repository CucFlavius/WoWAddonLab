namespace WoWAddonLab.Emulator.Lua;

public sealed record WowWeeklyRewardState(
    int Type,
    int Id,
    int Quantity,
    long? ItemDbId);
