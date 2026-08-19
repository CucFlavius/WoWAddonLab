using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowVignetteMapPinButton(
    string? Normal,
    string? Pressed,
    string? Highlight,
    string? Icon,
    bool UseNormalAsHiglight);
