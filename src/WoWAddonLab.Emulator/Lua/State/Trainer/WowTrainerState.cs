namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTrainerState
{
    public int ServiceCount { get; set; }
    public bool IsTradeSkillTrainer { get; set; }
    public int? ServiceStepIndex { get; set; }
    public int? TradeSkillRank { get; set; }
    public int TradeSkillMaxRank { get; set; }
    public int TradeSkillRankModifier { get; set; }
    public ISet<string> EnabledServiceTypeFilters { get; } =
        new HashSet<string>(
            ["available", "unavailable", "used"],
            StringComparer.OrdinalIgnoreCase);
}
