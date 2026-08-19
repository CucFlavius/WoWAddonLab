namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonAutoTroopState
{
    public string? Name { get; init; }
    public object? FollowerId { get; init; }
    public object? GarrFollowerId { get; init; }
    public int FollowerTypeId { get; init; }
    public IReadOnlyList<WowGarrisonFollowerDisplayState> DisplayIds { get; init; } = [];
    public int Level { get; init; }
    public int Quality { get; init; }
    public int LevelXp { get; init; }
    public int MaxXp { get; init; }
    public float Height { get; init; }
    public float Scale { get; init; }
    public float? DisplayScale { get; init; }
    public float? DisplayHeight { get; init; }
    public int? ClassSpec { get; init; }
    public string? ClassName { get; init; }
    public string? FlavorText { get; init; }
    public string ClassAtlas { get; init; } = string.Empty;
    public int PortraitIconId { get; init; }
    public string TextureKit { get; init; } = string.Empty;
    public bool IsTroop { get; init; }
    public int RaceId { get; init; }
    public int Health { get; init; }
    public int MaxHealth { get; init; }
    public int Role { get; init; }
    public bool IsAutoTroop { get; init; }
    public bool IsSoulbind { get; init; }
    public bool IsCollected { get; init; }
    public WowGarrisonAutoCombatStatsState AutoCombatStats { get; init; } =
        new(0, 0, 0, 0, 0, 0);
}
