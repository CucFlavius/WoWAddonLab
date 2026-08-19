using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLabsDataManagerState
{
    public int? ConfirmedAreaId { get; set; }
    public List<WowLabsAreaInfo> Areas { get; } = [];
    public bool InPrematch { get; set; }
    public bool CircleInfoDirty { get; set; }
    public int CircleInfoPushRequestCount { get; set; }
    public int SelectedAreaQueryCount { get; set; }
    public int AreaInfoQueryCount { get; set; }
    public List<int> SelectedAreaRequests { get; } = [];
}
