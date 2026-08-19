using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAnimationKitDefinition(
    int AnimationKitId,
    uint OneShotDurationMilliseconds,
    ushort OneShotStopAnimationKitId,
    ushort LowDefinitionAnimationKitId,
    IReadOnlyList<WowAnimationKitSegmentDefinition> Segments);
