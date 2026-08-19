using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalMapEncounter(
    int EncounterId,
    float MapX,
    float MapY);
