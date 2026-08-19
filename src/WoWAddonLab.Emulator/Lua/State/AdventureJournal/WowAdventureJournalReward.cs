namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAdventureJournalReward
{
    public int? ItemLevel { get; init; }
    public int? MinimumItemLevel { get; init; }
    public int? MaximumItemLevel { get; init; }
    public bool? IsRewardTable { get; init; }
    public int? ItemId { get; init; }
    public int? ItemQuantity { get; init; }
    public int? ItemIcon { get; init; }
    public string? ItemLink { get; init; }
    public int? CurrencyType { get; init; }
    public int? CurrencyQuantity { get; init; }
    public int? CurrencyIcon { get; init; }
    public string? RewardDescription { get; init; }
}
