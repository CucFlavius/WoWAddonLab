namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonMissionState
{
    public int MissionId { get; init; }
    public int FollowerTypeId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Location { get; init; }
    public string? LocationTextureKit { get; init; }
    public int Level { get; init; }
    public int Xp { get; init; }
    public bool IsMaxLevel { get; init; }
    public int ItemLevel { get; init; }
    public int NumFollowers { get; init; }
    public int RequiredChampionCount { get; init; }
    public int RequiredChampions { get; init; }
    public int RequiredSuccessChance { get; init; }
    public string? Duration { get; init; }
    public int DurationSeconds { get; init; }
    public bool IsRare { get; init; }
    public bool IsZoneSupport { get; init; }
    public int AreaId { get; init; }
    public int Cost { get; init; }
    public int BaseCost { get; init; }
    public int CostCurrencyTypesId { get; init; }
    public string? OfferTimeRemaining { get; init; }
    public double? OfferEndTime { get; init; }
    public IReadOnlyList<string> Followers { get; init; } = [];
    public bool InProgress { get; init; }
    public bool Completed { get; init; }
    public IReadOnlyList<WowGarrisonMissionRewardState> Rewards { get; init; } = [];
    public IReadOnlyList<WowGarrisonMissionRewardState> OvermaxRewards
        { get; init; } = [];
    public bool OvermaxSucceeded { get; init; }
    public float MapPosX { get; init; }
    public float MapPosY { get; init; }
    public bool CanStart { get; init; }
    public int OfferedGarrMissionTextureId { get; init; }
    public string? TimeLeft { get; init; }
    public int? TimeLeftSeconds { get; init; }
    public double? MissionEndTime { get; init; }
    public string? Type { get; init; }
    public string? TypeAtlas { get; init; }
    public string? TypeTextureKit { get; init; }
    public bool HasBonusEffect { get; init; }
    public int MissionScalar { get; init; }
    public bool? IsTutorialMission { get; init; }
}
