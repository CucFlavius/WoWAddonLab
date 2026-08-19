namespace WoWAddonLab.Emulator.Lua;

public sealed class WowBattleNetGameAccountInfoState
{
    public uint GameAccountId { get; set; }
    public string ClientProgram { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsGameBusy { get; set; }
    public bool IsGameAfk { get; set; }
    public int? WowProjectId { get; set; }
    public string? CharacterName { get; set; }
    public string? RealmName { get; set; }
    public string? RealmDisplayName { get; set; }
    public int? RealmId { get; set; }
    public string? FactionName { get; set; }
    public string? RaceName { get; set; }
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? AreaName { get; set; }
    public int? CharacterLevel { get; set; }
    public string? RichPresence { get; set; }
    public string? PlayerGuid { get; set; }
    public bool CanSummon { get; set; }
    public bool HasFocus { get; set; }
    public int RegionId { get; set; }
    public bool IsInCurrentRegion { get; set; }
    public int? TimerunningSeasonId { get; set; }
}
