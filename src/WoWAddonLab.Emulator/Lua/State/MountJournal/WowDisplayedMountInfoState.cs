namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDisplayedMountInfoState(
    string? Name,
    int SpellId,
    int IconFileId,
    bool IsActive,
    bool IsUsable,
    int SourceType,
    bool IsFavorite,
    bool IsFactionSpecific,
    byte? Faction,
    bool ShouldHideOnCharacter,
    bool IsCollected,
    int MountId,
    bool IsSteadyFlight);
