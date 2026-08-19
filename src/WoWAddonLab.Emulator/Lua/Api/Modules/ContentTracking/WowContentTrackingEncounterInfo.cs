using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowContentTrackingEncounterInfo(
    string? EncounterName = null,
    int? JournalEncounterId = null,
    int? JournalInstanceId = null,
    string? InstanceName = null,
    string? SubText = null,
    int? DifficultyId = null,
    int? LfgDungeonId = null,
    int? GroupFinderActivityId = null);
