using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelSceneActorDefinition(
    int Id,
    string ScriptTag,
    WowVector3 Position,
    double Yaw,
    double Pitch,
    double Roll,
    double? NormalizeScaleAggressiveness,
    bool UseCenterForOriginX,
    bool UseCenterForOriginY,
    bool UseCenterForOriginZ,
    int? DisplayId);
