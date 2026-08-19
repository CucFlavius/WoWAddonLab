namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonTalentAbilityState
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public int Icon { get; init; }
    public bool IsTrait { get; init; }
    public bool IsSpecialization { get; init; }
    public bool Temporary { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<WowGarrisonTalentCounterState> Counters { get; init; } = [];
    public bool IsEmptySlot { get; init; }
}
