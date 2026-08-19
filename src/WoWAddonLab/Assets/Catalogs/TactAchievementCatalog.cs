using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactAchievementCatalog : TactCatalog, IWowAchievementProvider
{
    private TactAchievementCatalog(
        IReadOnlyList<WowAchievementDefinition> achievements,
        IReadOnlyList<WowAchievementCategoryDefinition> categories)
    {
        Achievements = achievements;
        Categories = categories;
    }

    public IReadOnlyList<WowAchievementDefinition> Achievements { get; }
    public IReadOnlyList<WowAchievementCategoryDefinition> Categories { get; }

    public static TactAchievementCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var achievements = database.Load("Achievement", build).Values
            .Select(row => new WowAchievementDefinition(
                Integer(row, "ID"),
                Integer(row, "Category"),
                Text(row, "Title_lang", "Title"),
                Text(row, "Description_lang", "Description"),
                Text(row, "Reward_lang", "Reward"),
                Integer(row, "Points"),
                Integer(row, "Flags"),
                Unsigned(row, "IconFileID")))
            .OrderBy(value => value.Id)
            .ToArray();
        var categories = database.Load("Achievement_Category", build).Values
            .Select(row => new WowAchievementCategoryDefinition(
                Integer(row, "ID"),
                Integer(row, "Parent"),
                Text(row, "Name_lang", "Name"),
                Integer(row, "Flags")))
            .OrderBy(value => value.Id)
            .ToArray();
        return new TactAchievementCatalog(achievements, categories);
    }




}
