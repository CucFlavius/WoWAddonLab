using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowContentTrackingMapInfo(
    float X,
    float Y,
    int TrackableType,
    int TrackableId,
    int TargetType,
    int TargetId,
    string WaypointText);
