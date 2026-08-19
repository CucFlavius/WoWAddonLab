namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAdventureJournalState
{
    public bool CanBeShown { get; set; } = true;
    public uint AvailableSuggestionCount { get; set; }
    public int PrimaryOffset { get; set; }
    public IList<WowAdventureJournalSuggestion?> Suggestions { get; } = [];
    public IDictionary<int, WowAdventureJournalReward> Rewards { get; } =
        new Dictionary<int, WowAdventureJournalReward>();
    public int? ActivatedEntryIndex { get; set; }
    public int ActivationCount { get; set; }
    public int PrimaryOffsetChangeCount { get; set; }
    public int UpdateSuggestionsRequestCount { get; set; }
    public bool LastUpdateSuggestionsForce { get; set; }
}
