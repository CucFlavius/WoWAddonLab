namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAdventureJournalSuggestion
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ButtonText { get; init; }
    public int? EncounterJournalInstanceId { get; init; }
    public bool? HideDifficulty { get; init; }
    public int? DifficultyId { get; init; }
    public int? ExpansionLevel { get; init; }
    public bool? IsRandomDungeon { get; init; }
    public int? IconPath { get; init; }
}
