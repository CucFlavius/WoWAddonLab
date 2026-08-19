using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal readonly record struct WowM2Batch(
    byte Flags,
    sbyte PriorityPlane,
    short ShaderId,
    ushort SkinSectionIndex,
    ushort GeosetIndex,
    short ColorIndex,
    ushort MaterialIndex,
    ushort MaterialLayer,
    ushort TextureCount,
    ushort TextureComboIndex,
    ushort TextureCoordinateComboIndex,
    ushort TextureWeightComboIndex,
    ushort TextureTransformComboIndex);
