using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGossipInfoState
{
    public bool IsOpen { get; set; }
    public bool CanForceGossip { get; set; }
    public string? CompletedOptionDescription { get; set; }
    public string? CustomDescription { get; set; }
    public string? Text { get; set; }
    public int CloseRequests { get; set; }
    public int RefreshRequests { get; set; }

    public List<WowGossipQuestInfoState> ActiveQuests { get; } = [];
    public List<WowGossipQuestInfoState> AvailableQuests { get; } = [];
    public List<WowGossipOptionInfoState> Options { get; } = [];

    public IDictionary<int, WowGossipFriendshipReputationState>
        FriendshipReputationByFactionId { get; } =
        new Dictionary<int, WowGossipFriendshipReputationState>();

    public IDictionary<int, WowGossipFriendshipRanksState>
        FriendshipRanksByFactionId { get; } =
        new Dictionary<int, WowGossipFriendshipRanksState>();

    public IDictionary<int, IList<WowGossipWidgetSetState>>
        WidgetSetsByOptionId { get; } =
        new Dictionary<int, IList<WowGossipWidgetSetState>>();

    public IDictionary<int, int> PoiIdByUiMapId { get; } =
        new Dictionary<int, int>();

    public IDictionary<(int UiMapId, int PoiId), WowGossipPoiInfoState>
        PoiInfoByMapAndPoiId { get; } =
        new Dictionary<(int UiMapId, int PoiId),
            WowGossipPoiInfoState>();

    public List<WowGossipSelectionRequest> SelectionRequests { get; } =
        [];
}
