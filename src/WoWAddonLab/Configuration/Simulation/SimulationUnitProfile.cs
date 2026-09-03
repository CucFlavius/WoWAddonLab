namespace WoWAddonLab.Configuration;

public sealed class SimulationUnitProfile
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Realm { get; set; }
    public string? ClassFile { get; set; }
    public int? ClassId { get; set; }
    public string? RaceFile { get; set; }
    public int? Sex { get; set; }
    public string? FactionGroupTag { get; set; }
    public int? Level { get; set; }
    public long? Health { get; set; }
    public long? MaximumHealth { get; set; }
    public long? Power { get; set; }
    public long? MaximumPower { get; set; }
    public int? PowerTypeId { get; set; }
    public long? Experience { get; set; }
    public long? MaximumExperience { get; set; }
    public string? Classification { get; set; }
    public bool? IsDead { get; set; }
    public bool? IsGhost { get; set; }
    public bool? IsMounted { get; set; }
    public bool? IsAffectingCombat { get; set; }
    public bool? IsPvp { get; set; }
    public bool? IsBossMob { get; set; }
    public bool? IsPlayerControlled { get; set; }

    public SimulationUnitProfile Clone() => (SimulationUnitProfile)MemberwiseClone();
}
