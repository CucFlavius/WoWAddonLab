namespace WoWAddonLab.Emulator.Lua;

public sealed class WowChatWindowState
{
    public string Name { get; set; } = string.Empty;
    public double FontSize { get; set; }
    public double Red { get; set; }
    public double Green { get; set; }
    public double Blue { get; set; }
    public double Alpha { get; set; }
    public bool Shown { get; set; }
    public bool Locked { get; set; }
    public int? DockedOrder { get; set; }
    public bool Uninteractable { get; set; }
    public double SavedWidth { get; set; }
    public double SavedHeight { get; set; }
    public string? SavedPoint { get; set; }
    public double SavedXOffset { get; set; }
    public double SavedYOffset { get; set; }
    public IList<string> MessageGroups { get; } = [];
    public IList<WowChatChannelState> Channels { get; } = [];
}
