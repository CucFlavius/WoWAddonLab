namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowMapLinkInfo(
    int AreaPoiId,
    double X,
    double Y,
    string? Name,
    string AtlasName,
    int LinkedUiMapId);
