namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowMapGroupMemberInfo(
    int MapId,
    int RelativeHeightIndex,
    string? Name);
