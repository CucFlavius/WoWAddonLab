using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCombatLogEventFilter(
    string? Events,
    object? Source,
    object? Destination,
    object? Spell);
