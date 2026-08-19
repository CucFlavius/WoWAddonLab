namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClientState
{
    private int? _timerunningSeasonId;
    private string _realmName = "Emulator";

    public int RealmHour { get; set; } = DateTime.Now.Hour;
    public int RealmMinute { get; set; } = DateTime.Now.Minute;
    public int FileStreamingStatus { get; set; }
    public int BackgroundLoadingStatus { get; set; }
    public double IncomingBandwidthKilobytesPerSecond { get; set; }
    public double OutgoingBandwidthKilobytesPerSecond { get; set; }
    public int HomeLatencyMilliseconds { get; set; }
    public int WorldLatencyMilliseconds { get; set; }
    public int HomeProtocolType { get; set; }
    public int WorldProtocolType { get; set; }
    public int RealmId { get; set; } = 1;
    public int NativeRealmId { get; set; } = 1;
    public string RealmName
    {
        get => _realmName;
        set => _realmName = value;
    }
    public string SelectedRealmName
    {
        get => _realmName;
        set => _realmName = value;
    }
    public string? NormalizedRealmName { get; set; }
    public bool IsTournamentRealm { get; set; }
    public double AvailableBandwidth { get; set; }
    public double DownloadedPercentage { get; set; } = 1;
    public Dictionary<int, (bool InProgress, double Downloaded, double Total)>
        MovieDownloadProgress { get; } = [];
    public Dictionary<int, WowMirrorTimerState> MirrorTimers { get; } = [];
    public string ArchaeologyRaceName { get; set; } = "UNKNOWN";
    public double DefaultUiScale { get; set; } = 1;
    public string DefaultLanguage { get; set; } = "Common";
    public int DefaultLanguageId { get; set; } = 7;
    public string? AlternativeDefaultLanguage { get; set; }
    public int? AlternativeDefaultLanguageId { get; set; }
    public bool IsResting { get; set; }
    public bool IsPlayerInWorld { get; set; } = true;
    public bool IsPlayerMoving { get; set; }
    public bool HasPartialPlayTime { get; set; }
    public bool HasNoPlayTime { get; set; }
    public bool ThreatWarningEnabled { get; set; }
    public double? ExperienceExhaustion { get; set; }
    public WowRestState? RestState { get; set; }
    public bool IsPlayerAtEffectiveMaxLevel { get; set; }
    public bool IsXpUserDisabled { get; set; }
    public int? TimerunningSeasonId
    {
        get => _timerunningSeasonId;
        set => _timerunningSeasonId = value is > 0 ? value : null;
    }

    public bool IsTimerunning
    {
        get => _timerunningSeasonId is > 0;
        set
        {
            if (value)
                _timerunningSeasonId ??= 1;
            else
                _timerunningSeasonId = null;
        }
    }
    public bool IsTestBuild { get; set; }
    public bool IsInJailersTower { get; set; }
    public int? SpecializationIndex { get; set; } = 1;
    public int SpecializationCount { get; set; } = 3;
    public int LootSpecializationId { get; set; }
    public string? MinimapZoneText { get; set; } = "Stormwind City";
    public string? ZoneText { get; set; } = "Stormwind City";
    public string? SubZoneText { get; set; }
    public long Money { get; set; }
    public ulong GuildBankMoney { get; set; }
    public ulong PlayerTradeMoney { get; set; }
    public ulong TargetTradeMoney { get; set; }
    public int SendMailAttachmentCount { get; set; }
    public ulong SendMailMoney { get; set; }
    public ulong SendMailCod { get; set; }
    public bool CanAutoSetGamePadCursorControl { get; set; }
    public bool GamePadCursorControlEnabled { get; set; }
    public bool ClientIconFlashRequested { get; set; }
    public bool ClientIconFlashBriefly { get; set; }
    public bool InCombatLockdown { get; set; }
    public bool UiVisible { get; set; } = true;
    public bool VehicleExitRequested { get; set; }
    public string? SessionActionRequested { get; set; }
    public ISet<int> TutorialFlags { get; } = new HashSet<int>();

    public string ResolveNormalizedRealmName() =>
        NormalizedRealmName ?? string.Concat(RealmName.Where(character => !char.IsWhiteSpace(character)));
}
