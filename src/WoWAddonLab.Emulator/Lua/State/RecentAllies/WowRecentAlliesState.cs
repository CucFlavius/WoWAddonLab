namespace WoWAddonLab.Emulator.Lua;

public sealed class WowRecentAlliesState
{
    public bool IsSupported { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsDataReady { get; set; }
    public IList<WowRecentAllyState> Allies { get; } = new List<WowRecentAllyState>();
    public IList<WowRecentAllyNoteRequest> NoteRequests { get; } =
        new List<WowRecentAllyNoteRequest>();
    public IList<WowRecentAllyPinnedRequest> PinnedRequests { get; } =
        new List<WowRecentAllyPinnedRequest>();
    public int DataRequests { get; internal set; }
}
