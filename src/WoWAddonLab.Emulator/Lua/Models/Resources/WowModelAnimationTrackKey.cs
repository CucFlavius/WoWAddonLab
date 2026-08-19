using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowModelAnimationTrackKey<T>(
    T Value,
    T InTangent,
    T OutTangent);
