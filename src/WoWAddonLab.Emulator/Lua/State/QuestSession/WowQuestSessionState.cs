namespace WoWAddonLab.Emulator.Lua;

public sealed class WowQuestSessionState
{
    public bool IsAvailable { get; set; } = true;
    public bool CanStart { get; set; }
    public bool CanStop { get; set; }
    public bool Exists { get; set; }
    public bool HasJoined { get; set; }
    public uint PendingCommand { get; set; }
    public int ProposedMaximumLevel { get; set; } = 90;
    public int? SuperTrackedQuestId { get; set; }
    public WowQuestSessionPlayerDetailsState? BeginDetails { get; set; }
    public int StartRequestCount { get; set; }
    public int StopRequestCount { get; set; }
    public bool? LastBeginResponse { get; set; }
}
