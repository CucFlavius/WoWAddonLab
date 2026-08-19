using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowTextureBlendSetDefinition(
    uint Id,
    IReadOnlyList<uint> TextureFileDataIds,
    byte SwizzleRed,
    byte SwizzleGreen,
    byte SwizzleBlue,
    byte SwizzleAlpha,
    uint Flags,
    Vector3 TextureScrollRateU,
    Vector3 TextureScrollRateV,
    Vector3 TextureScaleU,
    Vector3 TextureScaleV,
    Vector4 Mod);
