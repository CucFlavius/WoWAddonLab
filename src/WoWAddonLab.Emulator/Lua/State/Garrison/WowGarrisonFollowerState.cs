namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonFollowerState
{
    public bool IsCollected { get; init; }
    public object? FollowerId { get; init; }
    public int GarrFollowerId { get; init; }
    public string? Name { get; init; }
    public int Level { get; init; }
    public bool IsMaxLevel { get; init; }
    public int ItemLevel { get; init; }
    public IReadOnlyList<WowGarrisonLegacyFollowerDisplayState> DisplayIds
        { get; init; } = [];
    public int PortraitIconId { get; init; }
    public int? ZoneSupportSpellId { get; init; }
    public float? Scale { get; init; }
    public float? Height { get; init; }
    public float? DisplayScale { get; init; }
    public float? DisplayHeight { get; init; }
    public int Quality { get; init; }
    public int Xp { get; init; }
    public int LevelXp { get; init; }
    public string? Status { get; init; }
    public int? ClassSpec { get; init; }
    public string? ClassName { get; init; }
    public string? ClassAtlas { get; init; }
    public bool IsFavorite { get; init; }
    public string? TextureKit { get; init; }
    public int SlotSoundKitId { get; init; }
    public bool IsTroop { get; init; }
    public int Durability { get; init; }
    public int MaxDurability { get; init; }
    public bool IsAutoTroop { get; init; }
    public bool IsSoulbind { get; init; }
    public string? FlavorText { get; init; }
    public int FollowerTypeId { get; init; }
    public int Health { get; init; }
    public int MaxHealth { get; init; }
    public int Role { get; init; }
}
