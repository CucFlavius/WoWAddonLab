namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonState
{
    public int LandingPageGarrisonType { get; set; }

    public int? CurrentGarrTalentTreeId { get; set; }

    public bool CanGenerateRecruits { get; set; }

    public bool CanSetRecruitmentPreference { get; set; }

    public bool IsVisitGarrisonAvailable { get; set; }

    public int LandingPageShipmentInfoRequestCount { get; internal set; }

    public ISet<int> VisibleLandingPageGarrisonTypes { get; } =
        new HashSet<int>();

    public ISet<int> KnownGarrisonTypes { get; } = new HashSet<int>();

    public IList<WowGarrisonBuildingSizeState> BuildingSizes { get; } = [];

    public IDictionary<int, IList<WowGarrisonBuildingState>>
        BuildingsByGarrisonType { get; } =
            new Dictionary<int, IList<WowGarrisonBuildingState>>();

    public IDictionary<(int GarrisonType, int UiCategoryId),
        IList<WowGarrisonAvailableBuildingState>> BuildingsBySize { get; } =
            new Dictionary<
                (int GarrisonType, int UiCategoryId),
                IList<WowGarrisonAvailableBuildingState>>();

    public IDictionary<int, IList<WowGarrisonPlotInstanceState>>
        PlotInstancesByMapId { get; } =
            new Dictionary<int, IList<WowGarrisonPlotInstanceState>>();

    public IDictionary<int, IList<WowGarrisonAutoTroopState>>
        AutoTroopsByFollowerType { get; } =
            new Dictionary<int, IList<WowGarrisonAutoTroopState>>();

    public IDictionary<int, IList<WowGarrisonEncounterThreatState>>
        EncounterThreatsByFollowerType { get; } =
            new Dictionary<int, IList<WowGarrisonEncounterThreatState>>();

    public IDictionary<int, IDictionary<int, int>> FollowerXpByLevelAndType
        { get; } = new Dictionary<int, IDictionary<int, int>>();

    public IDictionary<int, IDictionary<int, int>> FollowerXpByQualityAndType
        { get; } = new Dictionary<int, IDictionary<int, int>>();

    public IDictionary<int, IDictionary<int, WowGarrisonTalentAbilityState>>
        FollowerAbilityCountersByMechanicAndType { get; } =
            new Dictionary<
                int,
                IDictionary<int, WowGarrisonTalentAbilityState>>();

    public IDictionary<int, (int FirstCurrencyType, int SecondCurrencyType)>
        CurrencyTypesByGarrisonType { get; } =
            new Dictionary<int, (int FirstCurrencyType, int SecondCurrencyType)>();

    public IDictionary<int, int> FollowerCountsByType { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, IList<WowGarrisonFollowerState>> FollowersByType
        { get; } = new Dictionary<int, IList<WowGarrisonFollowerState>>();

    public IList<WowGarrisonFollowerState> AvailableRecruits { get; } = [];

    public IList<string?> RecruiterAbilityCategories { get; } = [];

    public IDictionary<int, IList<WowGarrisonMissionState>> AvailableMissionsByType
        { get; } = new Dictionary<int, IList<WowGarrisonMissionState>>();

    public IDictionary<int, IList<WowGarrisonMissionState>> InProgressMissionsByType
        { get; } = new Dictionary<int, IList<WowGarrisonMissionState>>();

    public IDictionary<int, IList<WowGarrisonMissionState>> CompleteMissionsByType
        { get; } = new Dictionary<int, IList<WowGarrisonMissionState>>();

    public IDictionary<int, WowGarrisonMissionState> CombatAllyMissionsByType
        { get; } = new Dictionary<int, WowGarrisonMissionState>();

    public IDictionary<int, IList<WowGarrisonBonusAbilityEffectState>>
        BonusAbilityEffectsByFollowerType { get; } =
            new Dictionary<int, IList<WowGarrisonBonusAbilityEffectState>>();

    public IDictionary<int, IList<WowGarrisonLandingPageItemState>>
        LandingPageItemsByGarrisonType { get; } =
            new Dictionary<int, IList<WowGarrisonLandingPageItemState>>();

    public IDictionary<int, WowGarrisonInfoState> GarrisonInfoByType { get; } =
        new Dictionary<int, WowGarrisonInfoState>();

    public IDictionary<int, int> GarrisonIdsByFollowerType { get; } =
        new Dictionary<int, int>();

    public IList<int> GarrisonUpgradeableRequests { get; } = [];

    public IDictionary<(int GarrisonType, int ClassId), IList<int>>
        TalentTreeIdsByGarrisonTypeAndClassId { get; } =
            new Dictionary<(int GarrisonType, int ClassId), IList<int>>();

    public IDictionary<int, WowGarrisonTalentTreeState> TalentTreesById
        { get; } = new Dictionary<int, WowGarrisonTalentTreeState>();

    public IDictionary<int, WowGarrisonTalentState> TalentsById { get; } =
        new Dictionary<int, WowGarrisonTalentState>();
}
