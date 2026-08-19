using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal sealed record WowM2TextureTransform(
    WowModelAnimationTrack<Vector3>? Translation,
    WowModelAnimationTrack<Quaternion>? Rotation,
    WowModelAnimationTrack<Vector3>? Scale);
