namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLootJournalState
{
    public bool DataAvailable { get; set; } = true;

    public IList<WowLootJournalItemSetState> ItemSets { get; } =
        new List<WowLootJournalItemSetState>();

    public IDictionary<int, IReadOnlyList<WowLootJournalItemState>> ItemsBySetId
    {
        get;
    } = new Dictionary<int, IReadOnlyList<WowLootJournalItemState>>();
}
