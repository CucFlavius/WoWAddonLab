namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemReagentQualityInfo(
    int Quality = 0,
    string? Icon = null,
    string? IconSmall = null,
    string? IconInventory = null,
    string? IconMixed = null,
    string? IconAppear = null,
    string? IconDissolve = null,
    string? BarFill = null,
    string? BarBackground = null,
    string? BarBackgroundCap = null,
    string? BarHighlight = null,
    string? IconChat = null);
