using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowQuestLogState
{
    public int MaxNumQuests { get; set; } = 175;
    public int NumQuestLogEntries { get; set; }
    public int NumQuests { get; set; }
    public int QuestPoiMapId { get; set; }
    public int QuestMapVisibleQuestCount { get; set; }
    public int? PendingQuestOfferId { get; set; }
    public int? SelectedQuestId { get; set; }

    public List<int> ActiveThreatMaps { get; } = [];
    public Dictionary<int, WowQuestBountySetInfo> BountySetsByMap { get; } = [];
    public Dictionary<int, List<WowQuestBountyInfo>> BountiesByMap { get; } = [];
    public Dictionary<int, WowQuestAdditionalHighlights> AdditionalHighlights { get; } = [];
    public Dictionary<int, List<WowQuestPoiMapInfo>> QuestsByMap { get; } = [];
    public Dictionary<int, (int AchievementId, int StoryMapId)> ZoneStories { get; } = [];
    public Dictionary<int, string> QuestTitles { get; } = [];

    public List<int> QuestWatchIds { get; } = [];
    public List<int> WorldQuestWatchIds { get; } = [];
    public List<int> TaskQuestIds { get; } = [];
    public List<WowAutoQuestPopup> AutoQuestPopups { get; } = [];

    public HashSet<int> ActiveQuestIds { get; } = [];
    public HashSet<int> CompletedQuestIds { get; } = [];
    public HashSet<int> CompletedOnAccountQuestIds { get; } = [];
    public HashSet<int> WorldQuestIds { get; } = [];
    public HashSet<int> ReadyForTurnInQuestIds { get; } = [];
    public List<int> QuestLoadRequests { get; } = [];

    public int CampaignHeaderUpdateCount { get; set; }
    public int QuestMapUpdateCount { get; set; }
    public int QuestPoiUpdateCount { get; set; }
    public int QuestSortCount { get; set; }
    public int QuestSortTypeSortCount { get; set; }
}
