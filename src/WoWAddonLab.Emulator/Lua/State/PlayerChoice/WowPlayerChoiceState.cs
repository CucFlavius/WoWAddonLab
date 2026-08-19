namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerChoiceState
{
    public WowPlayerChoiceInfo? CurrentChoice { get; set; }
    public int NumRerolls { get; set; }
    public int? RemainingTime { get; set; }
    public bool WaitingForResponse { get; set; }
    public int UiClosedRequestCount { get; internal set; }
    public bool WasWaitingOnLastUiClose { get; internal set; }
    public int RerollRequestCount { get; internal set; }
    public int ResponseRequestCount { get; internal set; }
    public int? LastResponseId { get; internal set; }
}
