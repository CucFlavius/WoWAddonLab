namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerChoiceOptionInfo
{
    public int Id { get; init; }
    public string Description { get; init; } = "";
    public string Header { get; init; } = "";
    public int ChoiceArtId { get; init; }
    public bool DesaturatedArt { get; init; }
    public bool DisabledOption { get; init; }
    public bool HasRewards { get; init; }
    public WowPlayerChoiceRewardInfo RewardInfo { get; } = new();
    public string? UiTextureKit { get; init; }
    public int MaxStacks { get; init; }
    public IList<WowPlayerChoiceButtonInfo> Buttons { get; } = [];
    public int? WidgetSetId { get; init; }
    public int? SpellId { get; init; }
    public int? Rarity { get; init; }
    public int? TypeArtId { get; init; }
    public string? HeaderIconAtlasElement { get; init; }
    public string? SubHeader { get; init; }
    public bool ConsolidateWidgets { get; init; }
}
