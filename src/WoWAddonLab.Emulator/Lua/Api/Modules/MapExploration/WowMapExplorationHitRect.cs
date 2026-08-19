using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMapExplorationHitRect(
    int Top,
    int Bottom,
    int Left,
    int Right);
