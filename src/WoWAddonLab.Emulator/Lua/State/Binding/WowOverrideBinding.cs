namespace WoWAddonLab.Emulator.Lua;

public sealed record WowOverrideBinding(
    int OwnerId,
    string Key,
    string Action,
    bool Priority,
    long Sequence);
