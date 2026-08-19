namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowMapDetails(
    int MapId,
    string Name,
    int MapType,
    int ParentMapId,
    int Flags,
    string BackgroundAtlas = "",
    int HelpTextPosition = 0,
    int MapArtZoneTextPosition = 0,
    int PlayerConditionId = 0);
