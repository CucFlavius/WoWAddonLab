using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelCameraMetadata(
    Vector3 Position,
    Vector3 Target,
    WowModelAnimationTrack<Vector3>? PositionTrack = null,
    WowModelAnimationTrack<Vector3>? TargetTrack = null,
    WowModelAnimationTrack<float>? RollTrack = null,
    WowModelAnimationTrack<float>? FieldOfViewTrack = null,
    int Type = 0,
    float FarClip = 0,
    float NearClip = 0);
