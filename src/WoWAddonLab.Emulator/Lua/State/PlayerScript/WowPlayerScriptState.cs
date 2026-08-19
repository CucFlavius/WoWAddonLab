namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerScriptState
{
    public ushort CinematicStateFlags { get; set; }
    public bool AutoDeclineGuildInvites { get; set; }
    public bool AutoDeclineNeighborhoodInvites { get; set; }
    public bool ReleaseTimerSuppressed { get; set; }
    public uint ReleaseDeadlineTickMilliseconds { get; set; }
    public int? SheathState { get; set; }
    public bool IsCharacterNewlyBoosted { get; set; }
    public string? ResurrectOffererName { get; set; }
    public bool ResurrectHasSickness { get; set; }
    public bool ResurrectHasTimer { get; set; }
    public int TimePlayedRequestCount { get; internal set; }
    public bool TimePlayedResponsePending { get; internal set; }
    public int TotalTimePlayedSeconds { get; set; }
    public int LevelTimePlayedSeconds { get; set; }
    public int LifetimeHonorableKills { get; set; }
    public byte LifetimeMaxPvpRank { get; set; }
}
