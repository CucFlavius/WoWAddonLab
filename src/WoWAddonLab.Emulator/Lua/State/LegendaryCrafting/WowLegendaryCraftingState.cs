namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLegendaryCraftingState
{
    public List<int> Currencies { get; } = [];
    public List<int> Modifiers { get; } = [];

    public IDictionary<WowItemLocation, IReadOnlyList<WowRuneforgeCurrencyCost>>
        CostsByItem { get; } =
            new Dictionary<
                WowItemLocation,
                IReadOnlyList<WowRuneforgeCurrencyCost>>();

    public IDictionary<WowItemLocation, WowRuneforgeLegendaryComponentInfo>
        ComponentsByItem { get; } =
            new Dictionary<
                WowItemLocation,
                WowRuneforgeLegendaryComponentInfo>();

    public IDictionary<int, WowRuneforgePowerInfo> Powers { get; } =
        new Dictionary<int, WowRuneforgePowerInfo>();

    public IDictionary<(WowItemLocation? BaseItem, int Filter),
        WowRuneforgePowerLists> PowerLists { get; } =
            new Dictionary<
                (WowItemLocation?, int),
                WowRuneforgePowerLists>();

    public IDictionary<(int? ClassId, int? SpecId, int? CovenantId, int Filter),
        IReadOnlyList<int>> PowerListsByClassSpecAndCovenant { get; } =
            new Dictionary<
                (int?, int?, int?, int),
                IReadOnlyList<int>>();

    public IList<WowRuneforgePreviewRule> PreviewRules { get; } =
        new List<WowRuneforgePreviewRule>();

    public IList<WowRuneforgeModifierInfoRule> ModifierInfoRules { get; } =
        new List<WowRuneforgeModifierInfoRule>();

    public ISet<WowItemLocation> RuneforgeLegendaryItems { get; } =
        new HashSet<WowItemLocation>();

    public ISet<WowItemLocation> MaxLevelRuneforgeLegendaryItems { get; } =
        new HashSet<WowItemLocation>();

    public ISet<WowItemLocation> ValidBaseItems { get; } =
        new HashSet<WowItemLocation>();

    public ISet<(WowItemLocation Legendary, WowItemLocation UpgradeItem)>
        ValidUpgradePairs { get; } =
            new HashSet<(WowItemLocation, WowItemLocation)>();

    public IList<WowRuneforgeCraftRequest> CraftRequests { get; } =
        new List<WowRuneforgeCraftRequest>();

    public IList<WowRuneforgeUpgradeRequest> UpgradeRequests { get; } =
        new List<WowRuneforgeUpgradeRequest>();
}
