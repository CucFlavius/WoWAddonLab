using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalTier(
    int Id,
    string Name,
    string? Link = null);
