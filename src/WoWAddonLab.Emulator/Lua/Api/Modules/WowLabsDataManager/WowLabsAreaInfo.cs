using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowLabsAreaInfo(
    int WowLabsAreaId,
    uint AreaType,
    float X,
    float Y,
    float Z);
