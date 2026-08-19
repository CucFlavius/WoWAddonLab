namespace WoWAddonLab.Emulator.Lua;

public sealed record WowFriendInfoState(
    string Name,
    bool Connected,
    string? ClassName,
    string? Area,
    string? Notes,
    string Guid,
    int Level,
    bool IsDnd,
    bool IsAfk,
    uint RafLinkType);
