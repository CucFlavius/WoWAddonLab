namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMapArtLayer(
    int LayerWidth,
    int LayerHeight,
    int TileWidth,
    int TileHeight,
    double MinScale,
    double MaxScale,
    int AdditionalZoomSteps,
    IReadOnlyList<uint> Textures);
