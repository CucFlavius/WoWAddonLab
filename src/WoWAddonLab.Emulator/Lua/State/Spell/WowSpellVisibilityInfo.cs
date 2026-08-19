namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSpellVisibilityInfo(
    bool HasCustom,
    bool AlwaysShowMine,
    bool ShowForMySpec);
