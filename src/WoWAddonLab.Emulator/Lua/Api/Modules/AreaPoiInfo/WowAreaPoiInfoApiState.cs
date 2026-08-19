using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAreaPoiInfoApiState
{
    public IDictionary<int, IList<int>> AreaPoiIdsByMapId { get; } =
        new Dictionary<int, IList<int>>();
    public IDictionary<int, IList<int>> DelveIdsByMapId { get; } =
        new Dictionary<int, IList<int>>();
    public IDictionary<int, IList<int>>
        DragonridingRaceIdsByMapId { get; } =
        new Dictionary<int, IList<int>>();
    public IDictionary<int, IList<int>> EventIdsByMapId { get; } =
        new Dictionary<int, IList<int>>();
    public IDictionary<int, IList<int>> QuestHubIdsByMapId { get; } =
        new Dictionary<int, IList<int>>();

    public IDictionary<int, WowAreaPoiInfoState> PoiInfoById { get; } =
        new Dictionary<int, WowAreaPoiInfoState>();
    public IDictionary<(int UiMapId, int AreaPoiId), WowAreaPoiInfoState>
        PoiInfoByMapAndId { get; } =
        new Dictionary<
            (int UiMapId, int AreaPoiId),
            WowAreaPoiInfoState>();
    public IDictionary<int, int> SecondsLeftByAreaPoiId { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, bool>
        HideTimerInTooltipByTimedAreaPoiId { get; } =
        new Dictionary<int, bool>();
}
