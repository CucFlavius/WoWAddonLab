using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTaskQuestState
{
    public ISet<int> MapsShowingTaskQuestObjectives { get; } =
        new HashSet<int>();

    public IDictionary<int, WowTaskQuestInfoState>
        QuestInfoByQuestId { get; } =
        new Dictionary<int, WowTaskQuestInfoState>();

    public IDictionary<(int QuestId, int UiMapId),
        WowTaskQuestLocationState> QuestLocations { get; } =
        new Dictionary<(int QuestId, int UiMapId),
            WowTaskQuestLocationState>();

    public IDictionary<int, double> ProgressByQuestId { get; } =
        new Dictionary<int, double>();

    public IDictionary<int, int> SecondsLeftByQuestId { get; } =
        new Dictionary<int, int>();

    public IDictionary<(int QuestId, byte Type), int>
        WidgetSets { get; } =
        new Dictionary<(int QuestId, byte Type), int>();

    public IDictionary<int, int> ZoneByQuestId { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, IList<WowTaskQuestPoiMapInfoState>>
        QuestsByUiMapId { get; } =
        new Dictionary<int, IList<WowTaskQuestPoiMapInfoState>>();

    public ISet<int> UnavailableQuestMaps { get; } =
        new HashSet<int>();

    public List<int> ThreatQuestIds { get; } = [];
    public ISet<int> ActiveQuestIds { get; } = new HashSet<int>();
    public List<int> PreloadRewardDataRequests { get; } = [];
}
