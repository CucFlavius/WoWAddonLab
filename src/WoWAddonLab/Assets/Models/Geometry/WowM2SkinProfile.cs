using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal sealed record WowM2SkinProfile(
    uint FileDataId,
    IReadOnlyList<ushort> VertexIndices,
    IReadOnlyList<ushort> TriangleIndices,
    IReadOnlyList<uint> BoneIndices,
    IReadOnlyList<WowM2SkinSection> SkinSections,
    IReadOnlyList<WowM2Batch> Batches,
    uint BoneCountMax)
{
    public bool TryResolveTriangleVertexIndices(out ushort[] indices)
    {
        indices = new ushort[TriangleIndices.Count];
        for (var index = 0; index < TriangleIndices.Count; index++)
        {
            var remapIndex = TriangleIndices[index];
            if (remapIndex >= VertexIndices.Count)
            {
                indices = [];
                return false;
            }
            indices[index] = VertexIndices[remapIndex];
        }
        return true;
    }
}
