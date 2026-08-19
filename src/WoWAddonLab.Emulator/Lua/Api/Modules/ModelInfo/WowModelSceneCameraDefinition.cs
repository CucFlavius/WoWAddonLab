using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelSceneCameraDefinition(
    int Id,
    string ScriptTag,
    WowVector3 Target,
    double Yaw,
    double Pitch,
    double Roll,
    double ZoomDistance,
    double MinZoomDistance,
    double MaxZoomDistance,
    WowVector3 ZoomedTargetOffset,
    double ZoomedYawOffset,
    double ZoomedPitchOffset,
    double ZoomedRollOffset,
    int Flags,
    int CameraType = 0);
