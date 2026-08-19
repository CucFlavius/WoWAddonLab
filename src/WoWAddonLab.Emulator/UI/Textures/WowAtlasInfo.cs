using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed record WowAtlasInfo(
    string Name,
    uint FileDataId,
    float Width,
    float Height,
    float RawWidth,
    float RawHeight,
    float Left,
    float Right,
    float Top,
    float Bottom,
    bool TilesHorizontally = false,
    bool TilesVertically = false,
    UiTextureSliceData? SliceData = null,
    string? Filename = null,
    int AtlasId = 0,
    int ElementId = 0)
{
    public Vector2[] Uv =>
    [
        new(Left, Top),
        new(Left, Bottom),
        new(Right, Top),
        new(Right, Bottom)
    ];
}
