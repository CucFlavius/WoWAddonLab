using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalInstance(
    int Id,
    string Name,
    string Description,
    int MapId,
    uint BackgroundFileDataId,
    uint ButtonFileDataId,
    uint ButtonSmallFileDataId,
    uint LoreFileDataId,
    int AreaId,
    int CovenantId,
    bool IsDungeon,
    bool IsRaid,
    bool ShouldDisplayDifficulty = true);
