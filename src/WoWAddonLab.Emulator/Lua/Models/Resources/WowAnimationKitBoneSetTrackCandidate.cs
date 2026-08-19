using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowAnimationKitBoneSetTrackCandidate(
    uint BoneDataId,
    uint? AlternateBoneDataId,
    bool UseTrackZeroWhenUnavailable = false,
    byte AnimationKitBoneSetId = 0);
