using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAnimationKitSegmentDefinition(
    int SegmentId,
    byte OrderIndex,
    ushort AnimationId,
    uint AnimationStartTimeMilliseconds,
    ushort AnimationKitConfigId,
    byte StartCondition,
    byte StartConditionParameter,
    uint StartConditionDelayMilliseconds,
    byte EndCondition,
    uint EndConditionParameter,
    uint EndConditionDelayMilliseconds,
    float Speed,
    uint SegmentFlags,
    byte ForcedVariation,
    uint OverrideConfigFlags,
    sbyte LoopToSegmentIndex,
    ushort BlendInTimeMilliseconds,
    ushort BlendOutTimeMilliseconds,
    uint ConfigFlags,
    IReadOnlyList<WowAnimationKitBoneSetDefinition> BoneSets);
