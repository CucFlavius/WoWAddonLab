using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal readonly record struct WowM2Vertex(
    Vector3 Position,
    uint PackedBoneWeights,
    uint PackedBoneIndices,
    Vector3 Normal,
    Vector2 TextureCoordinate0,
    Vector2 TextureCoordinate1)
{
    public WowM2VertexInfluences Influences =>
        WowM2VertexInfluences.Decode(PackedBoneWeights, PackedBoneIndices);
}
