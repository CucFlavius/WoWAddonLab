namespace WoWAddonLab.Emulator.Lua;

public sealed class WowBattleNetAccountInfoState
{
    public uint AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string BattleTag { get; set; } = string.Empty;
    public bool IsFriend { get; set; }
    public bool IsBattleTagFriend { get; set; }
    public double LastOnlineTime { get; set; }
    public bool IsAfk { get; set; }
    public bool IsDnd { get; set; }
    public bool IsFavorite { get; set; }
    public bool AppearOffline { get; set; }
    public string CustomMessage { get; set; } = string.Empty;
    public double CustomMessageTime { get; set; }
    public string Note { get; set; } = string.Empty;
    public int RafLinkType { get; set; }
    public WowBattleNetGameAccountInfoState GameAccountInfo { get; set; } = new();
}
