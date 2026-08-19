using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLfgListState
{
    public List<WowLfgApplicationInfo> Applications { get; } = [];
    public List<int> AvailableCategories { get; } = [];
    public bool CanTank { get; set; }
    public bool CanHeal { get; set; }
    public bool CanDamage { get; set; }
    public bool HasActiveEntry { get; set; }
    public bool HasActivityList { get; set; }
    public int? RoleCheckActivityId { get; set; }
    public int PremadeGroupFinderStyle { get; set; } = 1;
    public int AvailableActivitiesRequestCount { get; set; }
}
