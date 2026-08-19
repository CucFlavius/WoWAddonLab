using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal enum WowM2TextureAddressFlags : uint
{
    Clamp = 0,
    WrapU = 1,
    WrapV = 2,
    WrapUv = 3,
    TransparentBlackBorder = 4,
    OpaqueBlackBorder = 5,
    WhiteBorder = 6,
    Value7 = 7
}
