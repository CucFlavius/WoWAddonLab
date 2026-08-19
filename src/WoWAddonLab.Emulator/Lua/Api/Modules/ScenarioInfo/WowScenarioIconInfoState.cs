using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowScenarioIconInfoState(
    float X,
    float Y,
    string? Atlas,
    string? Description);
