using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalDungeonEntrance(
    int AreaPoiId,
    float X,
    float Y,
    string? Name,
    string? Description,
    string AtlasName,
    int JournalInstanceId);
