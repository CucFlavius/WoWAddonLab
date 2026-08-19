namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLfgLockState(
    int LfgId,
    int Reason,
    bool HideEntry);
