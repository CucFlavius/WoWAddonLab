using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

[Flags]
internal enum WowM2BoneFlags : uint
{
    None = 0,
    UseModelTranslation = 0x00000001,
    NormalizeParentBasis = 0x00000002,
    UseModelBasis = 0x00000004,
    SphericalBillboard = 0x00000008,
    CylindricalBillboardLockX = 0x00000010,
    CylindricalBillboardLockY = 0x00000020,
    CylindricalBillboardLockZ = 0x00000040,
    RuntimeAuxiliaryTransform = 0x00000080,
    AnimatedTransform = 0x00000200,
    RuntimeMatrixOverride = 0x00400400,
    DoNotInheritParentTransformWeightMask = 0x00802000,
    PostBillboardOffset = 0x00080000,
    PostRuntimeTranslation = 0x00200000,
    RuntimeAuxiliaryAfterParent = 0x01000000,
    PositionFacingBillboard = 0x04000000
}
