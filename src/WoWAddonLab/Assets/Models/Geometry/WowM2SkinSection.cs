using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal readonly record struct WowM2SkinSection(
    ushort SkinSectionId,
    ushort Level,
    ushort VertexStart,
    ushort VertexCount,
    ushort IndexStart,
    ushort IndexCount,
    ushort BoneCount,
    ushort BoneComboIndex,
    ushort BoneInfluences,
    ushort CenterBoneIndex,
    Vector3 CenterPosition,
    Vector3 SortCenterPosition,
    float SortRadius);
