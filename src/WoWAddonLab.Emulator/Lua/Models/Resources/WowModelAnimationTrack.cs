using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelAnimationTrack<T>(
    ushort InterpolationType,
    short GlobalSequenceIndex,
    IReadOnlyList<WowModelAnimationTrackSequence<T>> Sequences);
