namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPrivateAuraAnchorState(
    long Id,
    string UnitToken,
    uint AuraIndex,
    int ParentId,
    bool ShowCountdownFrame,
    bool ShowCountdownNumbers,
    bool IsContainer,
    WowAuraAnchorPointState? IconAnchor,
    double? IconWidth,
    double? IconHeight,
    double? BorderScale,
    WowAuraAnchorPointState? DurationAnchor);
