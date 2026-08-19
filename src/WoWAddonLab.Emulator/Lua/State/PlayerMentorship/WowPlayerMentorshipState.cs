namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerMentorshipState
{
    public int? MentorLevelRequirement { get; set; }
    public IList<int> RequiredAchievementIds { get; } = [];
    public IList<int> OptionalAchievementIds { get; } = [];
    public int OptionalCompleteAtLeastCount { get; set; }
    public int Status { get; set; }
    public IDictionary<string, int> StatusByUnitToken { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, int> StatusByGuid { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public int ActivePlayerStatus { get; set; }
    public bool ActivePlayerConsideredNewcomer { get; set; }
    public bool MentorRestricted { get; set; }
}
