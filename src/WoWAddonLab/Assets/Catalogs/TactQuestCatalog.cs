using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactQuestCatalog : TactCatalog, IWowQuestProvider
{
    private TactQuestCatalog(IReadOnlyDictionary<int, string> titles)
    {
        Titles = titles;
    }

    public IReadOnlyDictionary<int, string> Titles { get; }
    public int Count => Titles.Count;

    public static TactQuestCatalog Load(TactAssetSource tact, string build)
    {
        var titles = tact.Database.Load("QuestV2CliTask", build).Values
            .Select(row => (Id: Integer(row, "ID"), Title: Text(row, "QuestTitle_lang")))
            .Where(value => value.Id > 0 && !string.IsNullOrEmpty(value.Title))
            .ToDictionary(value => value.Id, value => value.Title);
        return new TactQuestCatalog(titles);
    }

    public bool TryGetTitle(int questId, out string title) =>
        Titles.TryGetValue(questId, out title!);
}
