using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelSequenceMetadata(
    ushort AnimationId,
    ushort VariationIndex,
    uint DurationMilliseconds,
    uint Flags,
    uint VariationWeight,
    short VariationNext,
    short AliasNext,
    int MinimumRepetitions = 0,
    int MaximumRepetitions = 0,
    uint BlendTimeMilliseconds = 0)
{
    public Vector3? BoundingBoxMinimum { get; init; }

    public Vector3? BoundingBoxMaximum { get; init; }

    public uint BlendInMilliseconds =>
        (Flags & 0x200) != 0
            ? unchecked((ushort)BlendTimeMilliseconds)
            : BlendTimeMilliseconds;

    public uint BlendOutMilliseconds =>
        (Flags & 0x200) != 0
            ? unchecked((ushort)(BlendTimeMilliseconds >> 16))
            : BlendInMilliseconds;
}
