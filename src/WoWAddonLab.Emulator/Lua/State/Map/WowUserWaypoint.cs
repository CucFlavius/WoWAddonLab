namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowUserWaypoint(
    int MapId,
    double X,
    double Y,
    double? Z = null);
