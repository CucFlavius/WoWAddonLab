namespace WoWAddonLab.Emulator.Lua;

internal sealed record WowEditModeAnchorInfo(
    int Point,
    string RelativeTo,
    int RelativePoint,
    float OffsetX,
    float OffsetY);
