using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelResourceMetadata(
    IReadOnlyList<WowModelSequenceMetadata> Sequences,
    int AttachmentCount)
{
    public Vector3? BoundingBoxMinimum { get; init; }

    public Vector3? BoundingBoxMaximum { get; init; }

    public Vector3? CollisionBoundingBoxMinimum { get; init; }

    public Vector3? CollisionBoundingBoxMaximum { get; init; }

    public bool HasCollisionGeometry { get; init; }

    public IReadOnlyList<uint> GlobalSequenceDurationsMilliseconds { get; init; } = [];

    public IReadOnlyList<WowModelCameraMetadata> Cameras { get; init; } = [];

    public IReadOnlyList<ushort> CameraLookupIndices { get; init; } = [];

    public IReadOnlyList<WowModelAnimationFileMetadata> AnimationFiles { get; init; } = [];
}
