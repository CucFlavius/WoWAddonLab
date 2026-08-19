namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLegacyProgressionState
{
    public bool IsAzeriteItemAtMaxLevel { get; set; }
    public WowItemLocation? ActiveAzeriteItemLocation { get; set; }
    public ISet<WowItemLocation> EnabledAzeriteItemLocations { get; } =
        new HashSet<WowItemLocation>();
    public double ChallengeLeaverPenaltyWarningTimeLeft { get; set; }
    public int? ActiveChallengeMapId { get; set; }
    public int ActiveKeystoneLevel { get; set; }
    public IList<int> ActiveKeystoneAffixIds { get; } = new List<int>();
    public bool ActiveKeystoneWasEnergized { get; set; }
    public WowItemLocation? SlottedKeystoneLocation { get; set; }
    public bool IsKeystoneFrameOpen { get; set; }
    public IDictionary<int, WowChallengeMapState> ChallengeMaps { get; } =
        new Dictionary<int, WowChallengeMapState>();
}
