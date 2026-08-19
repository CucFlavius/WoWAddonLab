using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal sealed record WowM2BoneFile(
    uint FileDataId,
    IReadOnlyList<ushort> MatrixIndexByBone,
    IReadOnlyList<Matrix4x4> Matrices)
{
    public bool TryGetMatrix(int boneIndex, out Matrix4x4 matrix)
    {
        matrix = Matrix4x4.Identity;
        if ((uint)boneIndex >= (uint)MatrixIndexByBone.Count)
            return false;
        var matrixIndex = MatrixIndexByBone[boneIndex];
        if (matrixIndex == ushort.MaxValue || matrixIndex >= Matrices.Count)
            return false;
        matrix = Matrices[matrixIndex];
        return true;
    }
}
