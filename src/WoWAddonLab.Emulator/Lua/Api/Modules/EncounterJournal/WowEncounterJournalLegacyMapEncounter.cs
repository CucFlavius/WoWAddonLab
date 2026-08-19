using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalLegacyMapEncounter(
    float MapX,
    float MapY,
    int JournalInstanceId,
    int EncounterId);
