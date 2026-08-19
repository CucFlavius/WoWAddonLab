namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowMapBannerInfo(
    int AreaPoiId,
    string? Name,
    string AtlasName,
    string? UiTextureKit = null);
