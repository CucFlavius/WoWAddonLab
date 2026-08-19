namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubFinderApplicantInfoState
{
    public string ClubFinderGuid { get; init; } = string.Empty;
    public string PlayerGuid { get; init; } = string.Empty;
    public int Closed { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int Level { get; init; }
    public int ClassId { get; init; }
    public int ItemLevel { get; init; }
    public IReadOnlyList<int> SpecIds { get; init; } = [];
    public int RequestStatus { get; init; }
    public bool LookupSuccess { get; init; }
    public int LastUpdatedTime { get; init; }
    public int Faction { get; init; }
}
