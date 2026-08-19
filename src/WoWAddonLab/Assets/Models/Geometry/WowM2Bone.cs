using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal sealed record WowM2Bone(
    int KeyBoneId,
    uint Flags,
    short ParentBoneIndex,
    ushort SubmeshId,
    ushort Unknown0,
    ushort Unknown1,
    WowModelAnimationTrack<Vector3>? Translation,
    WowModelAnimationTrack<Quaternion>? Rotation,
    WowModelAnimationTrack<Vector3>? Scale,
    Vector3 Pivot);
