namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowLfgRoles(
    bool Leader,
    bool Tank,
    bool Healer,
    bool Damage);
