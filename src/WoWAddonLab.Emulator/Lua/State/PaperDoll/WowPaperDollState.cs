namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPaperDollState
{
    public Dictionary<uint, byte> AttackPowerPerStatPoint { get; } = new()
    {
        [0] = 1,
        [1] = 1
    };

    public double ArmorEffectivenessCap { get; set; } = 0.85000002;
    public double ArmorEffectivenessFallback { get; set; }
    public Dictionary<int, double> ArmorMitigationConstantsByAttackerLevel { get; } = [];
    public Dictionary<int, double> ArmorMitigationScalesByAttackerLevel { get; } = [];
    public double? ArmorEffectivenessAgainstTarget { get; set; }
    public Dictionary<string, WowStaggerState> StaggerByUnit { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public double DodgeChance { get; set; }
    public double BlockChance { get; set; }
    public int ShieldBlock { get; set; }
    public double ParryChance { get; set; }
    public double MeleeHaste { get; set; }
    public double RangedCriticalStrikeChance { get; set; }
    public double CriticalStrikeChance { get; set; }
    public double SpellCriticalStrikeChance { get; set; }
    public double PowerRegeneration { get; set; }
    public double PowerRegenerationWhileCasting { get; set; }
    public double Haste { get; set; }
    public double ManaRegeneration { get; set; }
    public double ManaRegenerationInCombat { get; set; }
    public double MasteryEffect { get; set; }
    public double MasteryBonusCoefficient { get; set; } = 1;
    public double Lifesteal { get; set; }
    public double Avoidance { get; set; }
    public double Speed { get; set; }
    public double ModifiedResilienceDamageReduction { get; set; }
    public Dictionary<uint, int> CombatRatings { get; } = [];
    public Dictionary<uint, double> CombatRatingBonuses { get; } = [];
    public Dictionary<uint, double> CombatRatingBonusPerPoint { get; } = [];
    public Dictionary<uint, double> VersatilityBonuses { get; } = [];
    public double AverageItemLevel { get; set; }
    public double EquippedItemLevel { get; set; }
    public double PvpItemLevel { get; set; }
    public int? MinimumItemLevel { get; set; }
    public bool HasRangedWeapon { get; set; }
    public Dictionary<uint, int> SpellBonusDamageBySchool { get; } = [];
    public int SpellBonusHealing { get; set; }
    public bool UsesPvpGearStatRules { get; set; }
    public double AttackPowerFromSpellPowerMultiplier { get; set; }
    public double SpellPowerFromAttackPowerMultiplier { get; set; }
    public bool AttackPowerAffectsSpellPower { get; set; }
    public bool SpellPowerAffectsAttackPower { get; set; }
    public int PetSpellBonusDamage { get; set; }
    public double Expertise { get; set; }
    public double OffHandExpertise { get; set; }
    public double RangedExpertise { get; set; }
    public bool CriticalStrikeProvidesParry { get; set; }
    public double DodgeChanceFromAttribute { get; set; }
    public double ParryChanceFromAttribute { get; set; }

    public bool CanAutoEquipCursorItem { get; set; } = true;
    public HashSet<uint> CursorCompatibleSlots { get; } = [];
    public Dictionary<(string Unit, int Slot), IReadOnlyList<int>> InspectAzeritePowerChoices
        { get; } = [];
    public Dictionary<string, WowInspectGuildState> InspectGuildByUnit { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> InspectItemLevels { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public WowInspectPvpState InspectRatedBgBlitz { get; set; } = new();
    public WowInspectRatedBgState InspectRatedBg { get; set; } = new();
    public WowInspectPvpState InspectRatedSoloShuffle { get; set; } = new();
    public HashSet<string> KnownInventorySlots { get; } =
        new(
        [
            "AmmoSlot", "HeadSlot", "NeckSlot", "ShoulderSlot", "ShirtSlot",
            "BodySlot", "ChestSlot", "WaistSlot", "LegsSlot", "FeetSlot",
            "WristSlot", "HandsSlot", "Finger0Slot", "Finger1Slot",
            "Trinket0Slot", "Trinket1Slot", "BackSlot", "MainHandSlot",
            "SecondaryHandSlot", "OffHandSlot", "RangedSlot", "TabardSlot",
            "Bag0Slot", "Bag1Slot", "Bag2Slot", "Bag3Slot"
        ],
        StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DisabledInventorySlots { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public bool OffHandHasShield { get; set; }
    public bool OffHandHasWeapon { get; set; }
}
