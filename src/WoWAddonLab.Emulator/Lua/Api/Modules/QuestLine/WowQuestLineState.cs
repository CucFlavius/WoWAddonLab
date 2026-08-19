using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowQuestLineState
{
    public Dictionary<int, List<WowQuestLineInfo>> AvailableQuestLinesByMapId { get; } = [];
    public Dictionary<int, List<int>> ForceVisibleQuestIdsByMapId { get; } = [];
    public Dictionary<int, WowQuestLineInfo> QuestLineInfoByQuestId { get; } = [];
    public Dictionary<(int QuestId, int UiMapId), WowQuestLineInfo>
        QuestLineInfoByQuestAndMapId { get; } = [];
    public Dictionary<int, List<int>> QuestIdsByQuestLineId { get; } = [];
    public HashSet<int> CompletedQuestLineIds { get; } = [];
    public HashSet<(int UiMapId, int QuestLineId)>
        IgnoreAccountCompletedFiltering { get; } = [];

    public List<int> RequestedMapIds { get; } = [];
    internal Dictionary<int, double> LastRequestTimeByMapId { get; } = [];
}
