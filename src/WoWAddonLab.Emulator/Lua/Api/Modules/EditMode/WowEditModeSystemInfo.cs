namespace WoWAddonLab.Emulator.Lua;

internal sealed record WowEditModeSystemInfo(
    int System,
    int? SystemIndex,
    WowEditModeAnchorInfo AnchorInfo,
    WowEditModeAnchorInfo? AnchorInfo2,
    IReadOnlyList<WowEditModeSettingInfo> Settings,
    bool IsInDefaultPosition);
