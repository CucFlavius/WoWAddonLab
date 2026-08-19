namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLobbyMatchmakerState
{
    public bool IsInQueue { get; set; }

    public uint CurrentPlaylistEntry { get; set; }

    public uint CurrentQueueState { get; set; }

    public int QueueStartTime { get; set; }
}
