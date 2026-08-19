using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowScenarioStepInfoState(
    string Title,
    string Description,
    int NumCriteria,
    bool StepFailed,
    bool IsBonusStep,
    bool IsForCurrentStepOnly,
    bool ShouldShowBonusObjective,
    IReadOnlyList<WowScenarioStepSpellInfoState> Spells,
    int? WeightedProgress,
    int RewardQuestId,
    int? WidgetSetId);
