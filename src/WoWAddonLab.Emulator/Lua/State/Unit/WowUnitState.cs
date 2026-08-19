namespace WoWAddonLab.Emulator.Lua;

public sealed class WowUnitState
{
    public required string Token { get; init; }
    public required string Guid { get; init; }
    public string Name { get; set; } = "Player";
    public string Realm { get; set; } = "Emulator";
    public string ClassName { get; set; } = "Warrior";
    public string ClassFile { get; set; } = "WARRIOR";
    public int ClassId { get; set; } = 1;
    public int Sex { get; set; } = 2;
    public string? FactionGroupTag { get; set; } = "Alliance";
    public string? LocalizedFactionGroup { get; set; } = "Alliance";
    public string? DisplayFactionGroupTag { get; set; }
    public string? LocalizedDisplayFactionGroup { get; set; }
    public int Level { get; set; } = 1;
    public double HealthPerStamina { get; set; }
    public double MaximumHealthModifier { get; set; }
    public int BaselineArmor { get; set; }
    public int EffectiveArmor { get; set; }
    public int Armor { get; set; }
    public int BonusArmor { get; set; }
    public WowDamageState Damage { get; set; } = new();
    public double MainHandAttackSpeed { get; set; }
    public double? OffHandAttackSpeed { get; set; }
    public WowAttackPowerState AttackPower { get; set; } = new();
    public WowMovementSpeedState MovementSpeed { get; set; } = new();
    public WowRangedDamageState RangedDamage { get; set; } = new();
    public WowAttackPowerState RangedAttackPower { get; set; } = new();
    public int? EffectiveLevel { get; set; }
    public long Experience { get; set; }
    public long MaximumExperience { get; set; } = 1000;
    public long TrialExperience { get; set; }
    public int TrialBankedLevels { get; set; }
    public long TrialXpIntoCurrentLevel { get; set; }
    public long TrialXpForNextLevel { get; set; }
    public long Honor { get; set; }
    public long MaximumHonor { get; set; } = 1000;
    public int HonorLevel { get; set; }
    public long Health { get; set; } = 100;
    public long? PredictedHealth { get; set; }
    public long MaximumHealth { get; set; } = 100;
    public double TotalModifiedMaximumHealthPercent { get; set; }
    public long Power { get; set; } = 100;
    public long MaximumPower { get; set; } = 100;
    public int CurrentPowerTypeId { get; set; } = 1;
    public Dictionary<int, WowUnitPowerTypeState> PowerTypes { get; } = [];
    public Dictionary<int, WowUnitPowerValueState> PowerValues { get; } = [];
    public Dictionary<int, WowUnitStatState> Stats { get; } = [];
    public WowUnitCastState? Cast { get; set; }
    public WowUnitChannelState? Channel { get; set; }
    public WowUnitAvailableRolesState? AvailableRoles { get; set; }
    public bool IsModelReadyForUi { get; set; }
    public long IncomingHeals { get; set; }
    public Dictionary<string, long> IncomingHealsByHealerGuid { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public long TotalAbsorbs { get; set; }
    public long TotalHealAbsorbs { get; set; }
    public long Stagger { get; set; }
    public string? PortraitAsset { get; set; }
    public uint? PortraitFileDataId { get; set; }
    public bool IsPlayer { get; set; }
    public bool IsHumanPlayer { get; set; }
    public bool IsPlayerControlled { get; set; }
    public bool IsBattlePet { get; set; }
    public bool IsBattlePetCompanion { get; set; }
    public bool IsWildBattlePet { get; set; }
    public int? BattlePetLevel { get; set; }
    public int? BattlePetType { get; set; }
    public bool IsBossMob { get; set; }
    public bool IsQuestBoss { get; set; }
    public bool IsTapDenied { get; set; }
    public bool IsUnconscious { get; set; }
    public string Classification { get; set; } = "normal";
    public float SelectionRed { get; set; }
    public float SelectionGreen { get; set; } = 1;
    public float SelectionBlue { get; set; }
    public float SelectionAlpha { get; set; } = 1;
    public bool IsGameObject { get; set; }
    public bool IsCorpse { get; set; }
    public bool HasWorldLootObject { get; set; }
    public bool IsDead { get; set; }
    public bool IsGhost { get; set; }
    public bool IsPvp { get; set; }
    public bool IsPvpInactive { get; set; }
    public bool IsPvpFreeForAll { get; set; }
    public bool IsFalling { get; set; }
    public bool IsFlying { get; set; }
    public bool IsOnTaxi { get; set; }
    public bool IsMounted { get; set; }
    public bool IsOutdoors { get; set; } = true;
    public bool IsAffectingCombat { get; set; }
    public bool IsConnected { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool? IsInRange { get; set; }
    public bool IsInVehicle { get; set; }
    public bool HasVehicleUi { get; set; }
    public bool IsPartyAi { get; set; }
    public bool IsInParty { get; set; }
    public Dictionary<int, bool> InPartyByPartyCategory { get; } = [];
    public bool IsInOtherParty { get; set; }
    public int? RaidIndex { get; set; }
    public Dictionary<int, int?> RaidIndexByPartyCategory { get; } = [];
    public int RaidRank { get; set; }
    public int RaidSubgroup { get; set; } = 1;
    public string Zone { get; set; } = string.Empty;
    public string? RaidRole { get; set; }
    public bool IsMasterLooter { get; set; }
    public int? PhaseReason { get; set; }
    public string? ReadyCheckStatus { get; set; }
    public bool CanBeRaidTarget { get; set; } = true;
    public int? RaidTargetIndex { get; set; }
    public bool IsPossessed { get; set; }
    public bool IsGroupLeader { get; set; }
    public bool IsGroupAssistant { get; set; }
    public bool HasVehiclePlayerFrameUi { get; set; }
    public bool HasIncomingResurrection { get; set; }
    public byte IncomingSummonStatus { get; set; }
    public string GroupRole { get; set; } = "NONE";
    public string? PvpName { get; set; }
    public Dictionary<int, bool> GroupLeaderByPartyCategory { get; } = [];
    public int? ThreatSituation { get; set; }
    public Dictionary<string, int?> ThreatSituationByMobGuid { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public int RealmRelationship { get; set; } = 1;
    public uint DistanceSquared { get; set; }
    public bool HasCheckedDistance { get; set; }
    public WowUnitPositionState? Position { get; set; }
    public WowUnitPowerBarInfoState? AlternatePowerBar { get; set; }
    public IDictionary<int, WowUnitPowerBarTimerState> PowerBarTimers { get; } =
        new Dictionary<int, WowUnitPowerBarTimerState>();
}
