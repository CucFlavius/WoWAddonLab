namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSocialRestrictionsState
{
    public bool ChatDisabled { get; set; }
    public bool IsMuted { get; set; }
    public bool IsSilenced { get; set; }
    public bool IsSquelched { get; set; }
    public bool RegionalChatDisabledAcknowledged { get; set; }
    public bool? PendingChatDisabledRequest { get; internal set; }
    public List<bool> ChatDisabledRequests { get; } = [];
}
