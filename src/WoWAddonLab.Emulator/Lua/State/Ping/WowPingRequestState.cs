namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPingRequestState(
    string Operation,
    IReadOnlyList<object?> Arguments);
