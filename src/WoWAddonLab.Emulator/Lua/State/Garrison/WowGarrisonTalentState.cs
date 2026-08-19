namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonTalentState
{
    public int Id { get; init; }
    public WowGarrisonTalentAbilityState Ability { get; init; } = new();
    public string Name { get; init; } = string.Empty;
    public int Icon { get; init; }
    public int Tier { get; init; }
    public int UiOrder { get; init; }
    public int Type { get; init; }
    public int? PrerequisiteTalentId { get; init; }
    public bool Selected { get; init; }
    public bool Researched { get; init; }
    public bool IgnoreTalent { get; init; }
    public int ResearchDuration { get; init; }
    public int StartTime { get; init; }
    public int TimeRemaining { get; init; }
    public int ResearchGoldCost { get; init; }
    public IReadOnlyList<WowGarrisonTalentCurrencyCostState> ResearchCurrencyCosts
        { get; init; } = [];
    public uint TalentAvailability { get; init; }
    public int TalentRank { get; init; }
    public int TalentMaxRank { get; init; }
    public bool IsBeingResearched { get; init; }
    public string Description { get; init; } = string.Empty;
    public int PerkSpellId { get; init; }
    public string? ResearchDescription { get; init; }
    public string? PlayerConditionReason { get; init; }
    public WowGarrisonTalentSocketState SocketInfo { get; init; } =
        new(0, 0, 0, 0);
    public int TreeId { get; init; }
}
