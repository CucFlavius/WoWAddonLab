using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalSearchResult
{
    public int Type { get; init; }
    public int Id { get; init; }
    public int DifficultyId { get; init; }
    public int? JournalInstanceId { get; init; }
    public int? EncounterId { get; init; }
    public string? DisplayName { get; init; }
}
