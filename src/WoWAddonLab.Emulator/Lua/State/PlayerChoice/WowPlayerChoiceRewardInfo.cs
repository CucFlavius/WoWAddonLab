namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerChoiceRewardInfo
{
    public IList<WowPlayerChoiceCurrencyReward> CurrencyRewards { get; } = [];

    public IList<WowPlayerChoiceItemReward> ItemRewards { get; } = [];

    public IList<WowPlayerChoiceReputationReward> ReputationRewards { get; } = [];
}
