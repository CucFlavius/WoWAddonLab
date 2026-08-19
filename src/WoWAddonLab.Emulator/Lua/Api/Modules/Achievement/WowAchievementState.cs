using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAchievementState
{
    public bool HasCompletedAnyAchievement { get; set; } = true;
    public bool CanShowAchievementUi { get; set; } = true;
    public int SearchTabIndex { get; set; } = 1;
    public string SearchString { get; set; } = string.Empty;
    public IList<int> FilteredAchievementIds { get; } = new List<int>();
    public string? ComparisonUnitToken { get; set; }
    public int? FocusedAchievementId { get; set; }
}
