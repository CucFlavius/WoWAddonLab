namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUnitAvailableRolesState(
    bool Tank,
    bool Healer,
    bool Damage);
