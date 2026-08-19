namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGuildInfoRequest(
    string Operation,
    IReadOnlyList<object?> Arguments);
