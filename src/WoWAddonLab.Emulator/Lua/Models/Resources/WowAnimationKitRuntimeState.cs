using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAnimationKitRuntimeState(
    WowAnimationKitDefinition definition,
    IReadOnlyList<WowAnimationKitSegmentRuntimeState> segments)
{
    public WowAnimationKitDefinition Definition { get; } = definition;
    public IReadOnlyList<WowAnimationKitSegmentRuntimeState> Segments { get; } =
        segments;
}
