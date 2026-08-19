namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAccountState
{
    private static readonly int[] NativeMaximumLevels =
    [
        30, 30, 30, 35, 35, 40, 45, 50, 60, 70, 80, 90
    ];

    public WowAccountState()
    {
        for (var expansion = 0; expansion < NativeMaximumLevels.Length; expansion++)
            MaximumLevelByExpansion[(uint)expansion] = NativeMaximumLevels[expansion];
    }

    public bool IsTrial { get; set; }
    public bool IsVeteranTrial { get; set; }
    public bool IsRestricted { get; set; }
    public bool IsAccountSecured { get; set; } = true;
    public bool IsExpansionTrial { get; set; }
    public long? ExpansionTrialRemainingSeconds { get; set; }
    public int AccountExpansionLevel { get; set; } = 11;
    public int ExpansionLevel { get; set; } = 11;
    public int ClientDisplayExpansionLevel { get; set; } = 11;
    public int MaximumExpansionLevel { get; set; } = 11;
    public int MinimumExpansionLevel { get; set; }
    public int NumberOfExpansions { get; set; } = 12;
    public int ServerExpansionLevel { get; set; } = 11;
    public int MaximumLevelForPlayerExpansion { get; set; } = 90;
    public int MaximumPlayerLevel { get; set; } = 90;
    public int RestrictedMaximumLevel { get; set; }
    public ulong RestrictedMaximumMoney { get; set; }
    public int RestrictedProfessionCap { get; set; }
    public IDictionary<uint, int> MaximumLevelByExpansion { get; } =
        new Dictionary<uint, int>();
    public IDictionary<uint, WowExpansionDisplayInfoState> ExpansionDisplayInfoByLevel { get; } =
        new Dictionary<uint, WowExpansionDisplayInfoState>();
    public IDictionary<(uint ExpansionLevel, int ReleaseType), WowExpansionDisplayInfoState>
        ExpansionDisplayInfoByLevelAndReleaseType { get; } =
            new Dictionary<(uint ExpansionLevel, int ReleaseType), WowExpansionDisplayInfoState>();
    public ISet<string> RelatedGuids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, uint> BattleNetAccountIdsByGuid { get; } =
        new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
}
