using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowDeathInfoState
{
    public IDictionary<int, WowDeathMapPositionState>
        CorpsePositionsByUiMapId { get; } =
        new Dictionary<int, WowDeathMapPositionState>();

    public IDictionary<int, WowDeathMapPositionState>
        DeathReleasePositionsByUiMapId { get; } =
        new Dictionary<int, WowDeathMapPositionState>();

    public IDictionary<int, IList<WowGraveyardMapInfoState>>
        GraveyardsByUiMapId { get; } =
        new Dictionary<int, IList<WowGraveyardMapInfoState>>();

    public IList<WowSelfResurrectOptionState>
        SelfResurrectOptions { get; } =
        new List<WowSelfResurrectOptionState>();

    public bool SelfResurrectOptionsAvailable { get; set; } = true;
    public int UseSelfResurrectOptionRequests { get; internal set; }
    public byte? LastUsedOptionType { get; internal set; }
    public int? LastUsedOptionId { get; internal set; }
}
