namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAddonMessageState(
    string Prefix,
    string Message,
    string ChatType,
    string? Target,
    bool IsLogged);
