using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowAchievementProvider
{
    IReadOnlyList<WowAchievementDefinition> Achievements { get; }
    IReadOnlyList<WowAchievementCategoryDefinition> Categories { get; }
    IReadOnlyList<WowAchievementCriteriaDefinition> Criteria => [];
}
