using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowAnimationKitBoneSetDefinition(
    byte AnimationKitBoneSetId,
    ushort AnimationKitPriorityId,
    uint? BoneDataId = null,
    byte ParentAnimationKitBoneSetId = 0,
    byte AlternateAnimationKitBoneSetId = 0,
    uint? AlternateBoneDataId = null,
    byte Priority = 0)
{
    public IReadOnlyList<WowAnimationKitBoneSetTrackCandidate>
        AvailabilityCandidates { get; init; } = [];
}
