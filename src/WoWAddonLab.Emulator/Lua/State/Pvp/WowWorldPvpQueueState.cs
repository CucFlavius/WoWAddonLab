namespace WoWAddonLab.Emulator.Lua;

public sealed record WowWorldPvpQueueState(
    string Status,
    string? MapName,
    int QueueId,
    double ExpireTime,
    double AverageWaitTime,
    double QueuedTime,
    bool Suspended);
