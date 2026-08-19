namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMapArt(
    int MapArtId,
    IReadOnlyList<WowMapArtLayer> Layers,
    uint HighlightFileDataId = 0,
    string? HighlightAtlasId = null);
