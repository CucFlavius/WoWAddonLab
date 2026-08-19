using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowScenarioState
{
    public int CurrentScenarioId { get; set; }
    public int CurrentStepId { get; set; }
    public WowScenarioInfoState? Info { get; set; }
    public WowScenarioProvingGroundsInfoState ProvingGrounds { get; set; } =
        new(0, 0, 0, 0);
    public bool ShouldShowCriteria { get; set; }
    public IList<int> BonusStepIds { get; } = new List<int>();
    public IList<int> ChoiceOrder { get; } = new List<int>();
    public IList<WowScenarioSupersededObjectiveState> SupersededObjectives { get; } =
        new List<WowScenarioSupersededObjectiveState>();
    public IDictionary<int, WowScenarioStepInfoState> StepsById { get; } =
        new Dictionary<int, WowScenarioStepInfoState>();

    public bool IsInScenario => CurrentScenarioId > 0;
}
