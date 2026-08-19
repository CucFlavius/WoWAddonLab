namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSummonInfoState
{
    public string AreaName { get; set; } = "";
    public string? Summoner { get; set; }
    public int ConfirmTimeLeft { get; set; }
    public byte Reason { get; set; }
    public bool SkippingStartExperience { get; set; }
    public bool CanConfirm { get; set; } = true;
    public int AcceptRequestCount { get; internal set; }
    public int RejectRequestCount { get; internal set; }
    public bool? LastResponseAccepted { get; internal set; }

    internal void ClearPendingSummon()
    {
        AreaName = "";
        Summoner = null;
        ConfirmTimeLeft = 0;
        Reason = 0;
        SkippingStartExperience = false;
    }
}
