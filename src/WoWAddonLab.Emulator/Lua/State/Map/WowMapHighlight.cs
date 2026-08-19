namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowMapHighlight(
    uint FileDataId,
    string? AtlasId,
    double TexturePercentageX,
    double TexturePercentageY,
    double TextureWidth,
    double TextureHeight,
    double OffsetX,
    double OffsetY);
