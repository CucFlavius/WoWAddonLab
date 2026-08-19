using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal readonly record struct WowM2VertexInfluences(
    byte Weight0,
    byte Weight1,
    byte Weight2,
    byte Weight3,
    byte PaletteIndex0,
    byte PaletteIndex1,
    byte PaletteIndex2,
    byte PaletteIndex3)
{
    private const float NormalizedByteScale = 1f / byte.MaxValue;

    public Vector4 NormalizedWeights => new(
        Weight0 * NormalizedByteScale,
        Weight1 * NormalizedByteScale,
        Weight2 * NormalizedByteScale,
        Weight3 * NormalizedByteScale);

    public static WowM2VertexInfluences Decode(
        uint packedWeights,
        uint packedPaletteIndices) =>
        new(
            (byte)packedWeights,
            (byte)(packedWeights >> 8),
            (byte)(packedWeights >> 16),
            (byte)(packedWeights >> 24),
            (byte)packedPaletteIndices,
            (byte)(packedPaletteIndices >> 8),
            (byte)(packedPaletteIndices >> 16),
            (byte)(packedPaletteIndices >> 24));
}
