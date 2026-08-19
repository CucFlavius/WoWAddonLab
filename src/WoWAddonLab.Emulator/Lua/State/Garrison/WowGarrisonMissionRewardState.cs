namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonMissionRewardState
{
    public int? ItemId { get; init; }
    public int? Quantity { get; init; }
    public string? ItemLink { get; init; }
    public int? FollowerXp { get; init; }
    public object? Icon { get; init; }
    public string? Title { get; init; }
    public string? Tooltip { get; init; }
    public string? Name { get; init; }
    public int? CurrencyId { get; init; }
    public int? BonusAbilityId { get; init; }
    public string? TextureAtlas { get; init; }
    public float? PosX { get; init; }
    public float? PosY { get; init; }
    public string? Description { get; init; }
    public int? Duration { get; init; }
}
