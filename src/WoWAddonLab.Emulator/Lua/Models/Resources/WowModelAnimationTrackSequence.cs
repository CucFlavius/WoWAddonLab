using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelAnimationTrackSequence<T>(
    IReadOnlyList<uint> TimestampsMilliseconds,
    IReadOnlyList<WowModelAnimationTrackKey<T>> Keys);
