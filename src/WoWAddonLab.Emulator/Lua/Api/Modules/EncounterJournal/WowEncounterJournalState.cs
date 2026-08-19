using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowEncounterJournalState
{
    private static readonly byte[] NativeInventoryTypeSortOrder =
    [
        0, 10, 11, 12, 0, 14, 18, 19, 20, 16, 17, 21, 22, 4, 6, 5, 13,
        1, 0, 15, 14, 2, 3, 7, 0, 8, 5, 0, 9, 0, 0, 0, 0, 0, 0
    ];

    public int CurrentTierIndex { get; set; } = 1;
    public int? SelectedInstanceId { get; set; }
    public bool SelectedInstanceIsRaid { get; set; }
    public int? SelectedEncounterId { get; set; }
    public int DifficultyId { get; set; }
    public int ContentTuningId { get; set; }
    public int LootClassId { get; set; }
    public int LootSpecId { get; set; }
    public bool LootListOutOfDate { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public int SearchProgress { get; set; }
    public int SearchSize { get; set; }
    public bool SearchHasPendingWork { get; set; }
    public bool SearchIsEnding { get; set; }
    public byte SlotFilter { get; set; } = 15;
    public int PreviewMythicPlusLevel { get; set; } = 2;
    public int PreviewPvpTier { get; set; } = -1;
    public int SelectedTab { get; set; }
    public int OpenRequestCount { get; set; }
    public int CloseRequestCount { get; set; }
    public int StartArathiRpeRequestCount { get; set; }
    public IDictionary<int, IList<WowEncounterJournalDungeonEntrance>>
        DungeonEntrancesByUiMapId { get; } =
            new Dictionary<int, IList<WowEncounterJournalDungeonEntrance>>();
    public IDictionary<int, IList<WowEncounterJournalMapEncounter>>
        EncountersByUiMapId { get; } =
            new Dictionary<int, IList<WowEncounterJournalMapEncounter>>();
    public IDictionary<int, WowEncounterJournalEncounter> Encounters { get; } =
        new Dictionary<int, WowEncounterJournalEncounter>();
    public IDictionary<int, IList<int>> EncounterIdsByInstanceId { get; } =
        new Dictionary<int, IList<int>>();
    public IDictionary<int, IList<WowEncounterJournalCreature>>
        CreaturesByEncounterId { get; } =
            new Dictionary<int, IList<WowEncounterJournalCreature>>();
    public IDictionary<int, IList<WowEncounterJournalLegacyMapEncounter>>
        LegacyMapEncountersByMapId { get; } =
            new Dictionary<int, IList<WowEncounterJournalLegacyMapEncounter>>();
    public IDictionary<int, int> InstanceIdsByGameMapId { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, WowEncounterJournalLootInfo> LootById { get; } =
        new Dictionary<int, WowEncounterJournalLootInfo>();
    public IDictionary<(int Index, int? EncounterIndex),
        WowEncounterJournalLootInfo> LootByIndex { get; } =
            new Dictionary<(int Index, int? EncounterIndex),
                WowEncounterJournalLootInfo>();
    public IDictionary<int, IList<int>> SectionIconFlags { get; } =
        new Dictionary<int, IList<int>>();
    public IDictionary<int, WowEncounterJournalSectionInfo> Sections { get; } =
        new Dictionary<int, WowEncounterJournalSectionInfo>();
    public IDictionary<int, int> ParentSectionIds { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, int> EncounterIdsBySectionId { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, ISet<int>> ValidDifficultyIdsByInstanceId { get; } =
        new Dictionary<int, ISet<int>>();
    public IList<int> LegacyLootEncounterCounts { get; } = [];
    public IList<WowEncounterJournalSearchResult> SearchResults { get; } = [];
    public IReadOnlyList<byte> InventoryTypeSortOrder =>
        NativeInventoryTypeSortOrder;
    public ISet<int> InstanceIdsWithLoot { get; } = new HashSet<int>();
    public ISet<int> CompletedEncounterIds { get; } = new HashSet<int>();
}
