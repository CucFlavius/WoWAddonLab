namespace WoWAddonLab.Emulator.Lua;

public sealed class WowUnitAuraInfoState
{
    public required long AuraInstanceId { get; init; }
    public required string Name { get; init; }
    public required int SpellId { get; init; }
    public int Applications { get; set; }
    public bool CanActivePlayerDispel { get; set; }
    public bool CanApplyAura { get; set; }
    public int? Charges { get; set; }
    public string? DispelName { get; set; }
    public double Duration { get; set; }
    public double ExpirationTime { get; set; }
    public uint Icon { get; set; }
    public bool IsBossAura { get; set; }
    public bool IsDpsRoleAura { get; set; }
    public bool IsFromPlayerOrPlayerPet { get; set; }
    public bool IsHarmful { get; set; }
    public bool IsHealerRoleAura { get; set; }
    public bool IsHelpful { get; set; } = true;
    public bool HideOnPartyFrames { get; set; }
    public bool IsNameplateOnly { get; set; }
    public bool IsRaid { get; set; }
    public bool IsStealable { get; set; }
    public bool IsTankRoleAura { get; set; }
    public int? MaximumCharges { get; set; }
    public bool NameplateShowAll { get; set; }
    public bool NameplateShowPersonal { get; set; }
    public IList<double> Points { get; } = new List<double>();
    public string? SourceUnit { get; set; }
    public double TimeMod { get; set; } = 1;
    public bool IsMawAura { get; set; }
    public bool IsCancelable { get; set; }
    public bool IsPrivate { get; set; }
}
