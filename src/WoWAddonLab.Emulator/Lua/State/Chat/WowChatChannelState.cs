namespace WoWAddonLab.Emulator.Lua;

public sealed record WowChatChannelState(
    string Name,
    int Id,
    bool IsHeader = false,
    bool IsCollapsed = false,
    int MemberCount = 0,
    bool IsActive = true,
    string Category = "",
    int ChannelType = 0,
    bool IsDisabled = false);
