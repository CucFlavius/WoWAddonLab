using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal sealed record WowM2Geometry(
    IReadOnlyList<WowM2Vertex> Vertices,
    IReadOnlyList<WowM2SkinProfile?> SkinProfiles,
    uint ViewCount,
    IReadOnlyList<WowM2Texture> Textures,
    IReadOnlyList<WowM2TextureTransform> TextureTransforms,
    IReadOnlyList<WowM2Bone> Bones,
    IReadOnlyList<WowM2BoneFile?> BoneFiles,
    IReadOnlyList<WowM2Material> Materials,
    IReadOnlyList<ushort> BoneCombos,
    IReadOnlyList<ushort> TextureCombos,
    IReadOnlyList<ushort> TextureTransformAnimationStateLookup,
    IReadOnlyList<ushort> TextureWeightCombos,
    IReadOnlyList<ushort> TextureTransformCombos)
{
    public uint Flags { get; init; }

    public IReadOnlyList<ushort> AnimationTrackBoneLookup { get; init; } = [];

    public IReadOnlyList<WowM2Light> EmbeddedLights { get; init; } = [];

    public uint EmbeddedLightCount => (uint)EmbeddedLights.Count;

    public IReadOnlyList<WowM2Color> Colors { get; init; } = [];

    public IReadOnlyList<WowM2TextureWeight> TextureWeights { get; init; } = [];

    public uint TextureTransformCount => (uint)TextureTransforms.Count;

    public WowM2SkinProfile? BaselineSkinProfile =>
        SkinProfiles.Count == 0 ? null : SkinProfiles[0];

    public WowM2BoneFile? FindBoneFile(uint fileDataId) =>
        fileDataId == 0
            ? null
            : BoneFiles.FirstOrDefault(
                value => value?.FileDataId == fileDataId);
}
