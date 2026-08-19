using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal sealed record WowM2Light(
    ushort Type,
    short BoneIndex,
    Vector3 Position,
    WowModelAnimationTrack<Vector3>? AmbientColor,
    WowModelAnimationTrack<float>? AmbientIntensity,
    WowModelAnimationTrack<Vector3>? DiffuseColor,
    WowModelAnimationTrack<float>? DiffuseIntensity,
    WowModelAnimationTrack<float>? AttenuationStart,
    WowModelAnimationTrack<float>? AttenuationEnd,
    WowModelAnimationTrack<float>? Visibility);
