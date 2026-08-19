namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSoulbindOperationResult(
    bool Allowed,
    string? ErrorDescription = null);
