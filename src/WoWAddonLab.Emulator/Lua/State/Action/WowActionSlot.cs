namespace WoWAddonLab.Emulator.Lua;

public sealed record WowActionSlot(string Type, int Id, string? SubType = null)
{
    public object? ActionInfoIdentifier { get; init; }
    public int? TextureId { get; init; }
    public string? Text { get; init; }
    public int UseCount { get; init; }
    public int? OnEquipSpellId { get; init; }
    public int? ProfessionQuality { get; init; }
    public bool IsUsable { get; init; }
    public bool IsLackingResources { get; init; }
    public bool IsAttack { get; init; }
    public bool IsAutoRepeat { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEquipped { get; init; }
    public bool IsHarmful { get; init; }
    public bool IsHelpful { get; init; }
    public bool IsAssistedCombat { get; init; }
    public bool IsAutoCastPetAction { get; init; }
    public bool IsAutoCastAllowed { get; init; }
    public bool IsAutoCastEnabled { get; init; }
    public bool IsConsumable { get; init; }
    public bool IsEquippedGearOutfit { get; init; }
    public bool IsInterrupt { get; init; }
    public bool IsStackable { get; init; }
    public bool HasRangeRequirements { get; init; }
    public bool? IsInRange { get; init; }
    public WowDurationState? ChargeDuration { get; init; }
    public WowDurationState? CooldownDuration { get; init; }
    public WowDurationState? LossOfControlCooldownDuration { get; init; }
    public WowActionCooldownInfo Cooldown { get; init; } = new();
    public WowActionChargeInfo Charges { get; init; } = new();
    public WowActionLossOfControlInfo LossOfControl { get; init; } = new();
}
