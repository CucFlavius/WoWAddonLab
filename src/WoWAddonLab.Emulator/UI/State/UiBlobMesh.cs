using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiBlobMesh
{
    public required int BlobId { get; init; }
    public required int ObjectiveIndex { get; init; }
    public required int MergeGroupId { get; init; }
    public string? TooltipText { get; init; }
    public required IReadOnlyList<Vector2> Boundary { get; init; }
    public required IReadOnlyList<Vector2> FillVertices { get; init; }
    public required IReadOnlyList<Vector2> FillUvs { get; init; }
    public required IReadOnlyList<ushort> FillIndices { get; init; }
    public required IReadOnlyList<Vector2> BorderVertices { get; init; }
    public required IReadOnlyList<Vector2> BorderUvs { get; init; }
    public required IReadOnlyList<ushort> BorderIndices { get; init; }
    public required UiRect Bounds { get; init; }
    public bool IsVisible { get; internal set; } = true;
}
