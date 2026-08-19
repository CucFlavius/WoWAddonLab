namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitConditionInfoState(
    int ConditionId,
    int? RanksGranted,
    bool IsAlwaysMet,
    bool IsMet,
    bool IsGate,
    bool IsSufficient,
    int Type,
    int? QuestId,
    int? AchievementId,
    int? SpecSetId,
    int? PlayerLevel,
    int? TraitCurrencyId,
    int? SpentAmountRequired,
    string? TooltipFormat,
    int? TraitConditionAccountElementId);
