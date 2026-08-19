using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed record UiBlobArea(
    int BlobId,
    int ObjectiveIndex,
    int WorldMapId,
    IReadOnlyList<Vector2> WorldBoundary,
    int MergeGroupId = 0,
    bool IsVisible = true,
    string? TooltipText = null);
