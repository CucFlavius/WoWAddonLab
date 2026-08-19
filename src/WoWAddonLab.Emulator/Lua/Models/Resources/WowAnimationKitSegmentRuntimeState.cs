using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAnimationKitSegmentRuntimeState(
    WowAnimationKitSegmentDefinition definition)
{
    public WowAnimationKitSegmentDefinition Definition { get; } = definition;
    public WowAnimationKitSegmentPlaybackState PlaybackState { get; set; }
    public double? StartDeadlineMilliseconds { get; set; }
    public double? EndDeadlineMilliseconds { get; set; }
    public ushort ResolvedAnimationId { get; set; }
    public ushort ResolvedVariation { get; set; }
    public int ResolvedSequenceIndex { get; set; } = -1;
    public int? InheritedVariation { get; set; }
    public uint SequenceDurationMilliseconds { get; set; }
    public uint RepeatCount { get; set; } = 1;
    public float PlaybackSpeed { get; set; } = 1;
    public double StartElapsedMilliseconds { get; set; }
    public float TransformWeight { get; set; } = 1;
}
