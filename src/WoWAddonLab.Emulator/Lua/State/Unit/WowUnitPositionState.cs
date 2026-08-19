namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUnitPositionState(
    float X,
    float Y,
    float Z,
    int MapId,
    float Facing = 0);
