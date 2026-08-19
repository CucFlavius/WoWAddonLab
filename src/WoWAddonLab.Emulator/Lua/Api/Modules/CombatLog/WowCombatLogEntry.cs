using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCombatLogEntry(
    IReadOnlyList<object?> Info,
    bool ShouldShow = true,
    bool MatchesEventFilters = true);
