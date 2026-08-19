namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAzeriteEmpoweredItemSelectRequest(
    WowItemLocation Location,
    byte TierIndex,
    int PowerId);
