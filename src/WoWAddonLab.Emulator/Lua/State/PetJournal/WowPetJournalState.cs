namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPetJournalState
{
    public IDictionary<int, WowPetCollectionInfo> CollectionInfoBySpeciesId { get; } =
        new Dictionary<int, WowPetCollectionInfo>();
}
