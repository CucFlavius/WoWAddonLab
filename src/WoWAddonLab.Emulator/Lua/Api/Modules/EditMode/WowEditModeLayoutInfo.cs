namespace WoWAddonLab.Emulator.Lua;

internal sealed record WowEditModeLayoutInfo(
    string LayoutName,
    int LayoutType,
    IReadOnlyList<WowEditModeSystemInfo> Systems);
