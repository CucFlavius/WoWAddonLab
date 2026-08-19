namespace WoWAddonLab.Emulator.Lua;

public sealed class WowChatState
{
    private readonly Dictionary<int, WowChatWindowState> _windows = new()
    {
        [1] = new WowChatWindowState
        {
            Alpha = 63d / 255d,
            Shown = true,
            DockedOrder = 1,
            SavedWidth = 430,
            SavedHeight = 120
        },
        [2] = new WowChatWindowState
        {
            Alpha = 63d / 255d,
            Shown = true,
            DockedOrder = 2
        }
    };

    public IList<WowLanguageState> Languages { get; } = [];
    public int NumGroupChannels { get; set; }
    public bool OutgoingAddonChatMessagesRestricted { get; set; }
    public bool InChatMessagingLockdown { get; set; }
    public bool CanPerformEmotes { get; set; } = true;
    public string? LastEmoteName { get; set; }
    public string? LastEmoteTarget { get; set; }
    public bool LastEmoteSuppressMoveError { get; set; }
    public ISet<string> RegisteredAddonMessagePrefixes { get; } =
        new HashSet<string>(StringComparer.Ordinal);
    public ISet<ulong> ValidChatLineIds { get; } = new HashSet<ulong>();
    public WowAddonMessageState? LastAddonMessage { get; set; }
    public WowSentChatMessageState? LastSentChatMessage { get; set; }
    public IReadOnlyDictionary<int, WowChatWindowState> Windows => _windows;

    public WowChatWindowState GetWindow(int index)
    {
        if (!_windows.TryGetValue(index, out var window))
        {
            window = new WowChatWindowState();
            _windows[index] = window;
        }
        return window;
    }
}
