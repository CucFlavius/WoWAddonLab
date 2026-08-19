namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonLandingPageItemState
{
    public bool IsBuilding { get; init; }
    public bool IsComplete { get; init; }
    public int BuildingId { get; init; }
    public string? Name { get; init; }
    public int BuildingLevel { get; init; }
    public string? TimeLeft { get; init; }
    public WowGarrisonMissionState? Mission { get; init; }
}
