namespace WoWAddonLab.Emulator.Lua;

public sealed record WowHousingRequestState(
    string Operation,
    IReadOnlyList<object?> Arguments);
