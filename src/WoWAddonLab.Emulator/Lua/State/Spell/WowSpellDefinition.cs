namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSpellDefinition
{
    public WowSpellDefinition(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Subtext { get; set; }
    public int IconId { get; set; }
    public int OriginalIconId { get; set; }
    public int CastTimeMilliseconds { get; set; }
    public float MinRange { get; set; }
    public float MaxRange { get; set; }
    public int? BaseSpellId { get; set; }
    public int? OverrideSpellId { get; set; }
    public int LevelLearned { get; set; }
    public string? Link { get; set; }
    public string? TradeSkillLink { get; set; }
    public string? MawPowerLink { get; set; }
    public WowSpellMawPowerRarityInfo? MawPowerRarity { get; set; }
    public int CastCount { get; set; }
    public int UseCount { get; set; }
    public int MaxCumulativeAuraApplications { get; set; }
    public int? SkillLineAbilityRank { get; set; }
    public bool IsDataCached { get; set; } = true;
    public bool AutoCastAllowed { get; set; }
    public bool AutoCastEnabled { get; set; }
    public bool IsAutoAttack { get; set; }
    public bool IsAutoRepeat { get; set; }
    public bool IsBigDefensive { get; set; }
    public bool IsClassTalent { get; set; }
    public bool IsConsumable { get; set; }
    public bool IsStackable { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsExternalDefensive { get; set; }
    public bool IsPressHoldRelease { get; set; }
    public bool IsPriorityAura { get; set; }
    public bool IsPvpTalent { get; set; }
    public bool IsSelfBuff { get; set; }
    public bool IsCrowdControl { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsHarmful { get; set; }
    public bool IsHelpful { get; set; }
    public bool IsImportant { get; set; }
    public bool IsPassive { get; set; }
    public bool IsUsable { get; set; }
    public bool HasInsufficientPower { get; set; }
    public bool HasRange { get; set; }
    public bool? IsInRange { get; set; }
    public WowSpellAuraStatChanges AuraStatChanges { get; set; } =
        new(0, []);
    public WowSpellDeadlyDebuffInfo? DeadlyDebuffInfo { get; set; }
    public IReadOnlyList<int> ItemModifiedAppearancesApplied { get; set; } = [];
    public IReadOnlyList<WowSpellPowerCostInfo> PowerCosts { get; set; } = [];
    public WowDurationState? ChargeDuration { get; set; }
    public WowActionChargeInfo? Charges { get; set; }
    public WowActionCooldownInfo? Cooldown { get; set; }
    public WowDurationState? CooldownDuration { get; set; }
    public WowDurationState? LossOfControlCooldownDuration { get; set; }
    public WowActionLossOfControlInfo? LossOfControlCooldownInfo { get; set; }
    public IDictionary<WowSpellAuraVisibilityType, WowSpellVisibilityInfo>
        Visibility { get; } =
        new Dictionary<WowSpellAuraVisibilityType, WowSpellVisibilityInfo>();
}
