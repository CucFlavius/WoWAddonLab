namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGameRulesState
{
    public IDictionary<int, int> RuleValueOverrides { get; } =
        new Dictionary<int, int>();
    public IList<int> DisplayedGameModeRecordIds { get; } = [];
    public IDictionary<int, WowGameModeRecordState> GameModeRecords { get; } =
        new Dictionary<int, WowGameModeRecordState>();
    public ISet<int> EnabledGameModeRecordIds { get; } = new HashSet<int>();
    public ISet<int> DisabledGameModeRecordIds { get; } = new HashSet<int>();
    public ISet<int> PromotionalGameModeRecordIds { get; } = new HashSet<int>();
    public IDictionary<int, string> FrameStrataOverrides { get; } =
        new Dictionary<int, string>();
    public bool UseProviderDefaults { get; set; }
    public int ActiveGameMode { get; set; } = 1;
    public int CurrentEventRealmQueues { get; set; }
    public int? CurrentGameModeRecordId { get; set; }
    public bool NameplateShowSelf { get; set; }
}
