using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal static class WowM2GeometryReader
{
    private const uint Md20 = 0x3032444D;
    private const uint Md21 = 0x3132444D;
    private const uint Sfid = 0x44494653;
    private const uint Txid = 0x44495854;
    private const uint Sks1 = 0x31534B53;
    private const uint Skb1 = 0x31424B53;
    private const uint Skpd = 0x44504B53;
    private const uint Bfid = 0x44494642;
    private const uint Bida = 0x41444942;
    private const uint Bomt = 0x544D4F42;
    private const uint Afm2 = 0x324D4641;
    private const uint Afsb = 0x42534641;
    private const uint Skin = 0x4E494B53;
    private const int BoneArrayOffset = 44;
    private const int AnimationTrackBoneLookupArrayOffset = 52;
    private const int VertexArrayOffset = 60;
    private const int ViewCountOffset = 68;
    private const int ColorArrayOffset = 72;
    private const int TextureArrayOffset = 80;
    private const int TextureWeightArrayOffset = 88;
    private const int TextureTransformArrayOffset = 96;
    private const int MaterialArrayOffset = 112;
    private const int TextureComboArrayOffset = 128;
    private const int TextureTransformAnimationStateLookupArrayOffset = 136;
    private const int BoneComboArrayOffset = 120;
    private const int TextureWeightComboArrayOffset = 144;
    private const int TextureTransformComboArrayOffset = 152;
    private const int LightArrayOffset = 264;
    private const int HeaderSize = 304;
    private const int VertexSize = 48;
    private const int TextureSize = 16;
    private const int TextureTransformSize = 60;
    private const int ColorSize = 40;
    private const int TextureWeightSize = 20;
    private const int LightSize = 156;
    private const int BoneSize = 88;
    private const int BoneFileMatrixSize = 64;
    private const int MaterialSize = 4;
    private const int SkinHeaderSize = 56;
    private const int SkinSectionSize = 48;
    private const int BatchSize = 24;
    private const int ShadowBatchSize = 12;
    private const uint ParentBoneFlagInheritanceMask = 0xFFF8FFFF;

    public static WowM2Geometry? Read(
        byte[]? data,
        Func<uint, byte[]?>? readSidecar = null)
    {
        if (data is null || data.Length < 8)
            return null;

        var md20Base = FindMd20Base(data);
        if (md20Base < 0 || !HasRange(data, md20Base, HeaderSize))
            return null;

        uint vertexCount = ReadUInt16(data, md20Base + VertexArrayOffset);
        var vertexOffset = ReadUInt32(data, md20Base + VertexArrayOffset + 4);
        if (!TryResolveArrayRange(
                data,
                md20Base,
                vertexCount,
                vertexOffset,
                VertexSize,
                out var vertexStart))
        {
            return null;
        }

        var vertices = new List<WowM2Vertex>((int)vertexCount);
        for (var index = 0u; index < vertexCount; index++)
        {
            var offset = checked(vertexStart + (int)index * VertexSize);
            vertices.Add(new WowM2Vertex(
                ReadVector3(data, offset),
                ReadUInt32(data, offset + 12),
                ReadUInt32(data, offset + 16),
                ReadVector3(data, offset + 20),
                ReadVector2(data, offset + 32),
                ReadVector2(data, offset + 40)));
        }

        var sidecars = new Dictionary<uint, byte[]?>();
        byte[]? ReadSidecar(uint fileDataId)
        {
            if (sidecars.TryGetValue(fileDataId, out var cached))
                return cached;
            var loaded = readSidecar?.Invoke(fileDataId);
            sidecars[fileDataId] = loaded;
            return loaded;
        }

        var skinProfileFileDataIds = ReadChunkFileDataIds(data, Sfid);
        var skinProfiles = new List<WowM2SkinProfile?>(skinProfileFileDataIds.Count);
        foreach (var fileDataId in skinProfileFileDataIds)
        {
            skinProfiles.Add(
                ReadSidecar(fileDataId) is { } skinData
                    ? ReadSkinProfile(skinData, fileDataId)
                    : null);
        }

        var textureFileDataIds = ReadChunkFileDataIds(data, Txid);
        var boneData = data;
        var boneDescriptorOffset = md20Base + BoneArrayOffset;
        var animationTrackBoneLookupDescriptorOffset =
            md20Base + AnimationTrackBoneLookupArrayOffset;
        var boneRelativeBase = md20Base;
        uint? skeletonFileDataId = null;
        byte[]? skeletonFileData = null;
        var modelSequencePayloads = ReadExternalAnimationPayloads(
            data,
            md20Base + 28,
            md20Base,
            ReadSidecar,
            Afm2,
            allowRawPayload: true);
        var boneSequencePayloads = modelSequencePayloads;
        if (WowM2MetadataReader.FindSkeletonFileDataId(data) is { } skeletonId &&
            ReadSidecar(skeletonId) is { } skeletonData)
        {
            if (!TryFindChunkData(
                    skeletonData,
                    Sks1,
                    out var sks1Base,
                    out var sks1Size) ||
                !TryFindChunkData(
                    skeletonData,
                    Skb1,
                    out var skb1Base,
                    out var skb1Size) ||
                sks1Size < 24 ||
                skb1Size < 16)
            {
                return null;
            }

            var sks1Data = skeletonData.AsSpan(sks1Base, sks1Size).ToArray();
            var skb1Data = skeletonData.AsSpan(skb1Base, skb1Size).ToArray();
            boneData = skb1Data;
            boneDescriptorOffset = 0;
            animationTrackBoneLookupDescriptorOffset = 8;
            boneRelativeBase = 0;
            skeletonFileDataId = skeletonId;
            skeletonFileData = skeletonData;
            modelSequencePayloads = ReadExternalAnimationPayloads(
                sks1Data,
                8,
                0,
                ReadSidecar,
                Afm2,
                allowRawPayload: false,
                animationFileCatalogData: skeletonData);
            boneSequencePayloads = ReadExternalAnimationPayloads(
                sks1Data,
                8,
                0,
                ReadSidecar,
                Afsb,
                allowRawPayload: false,
                animationFileCatalogData: skeletonData);
        }
        if (!TryReadBones(
                boneData,
                boneDescriptorOffset,
                boneRelativeBase,
                boneSequencePayloads,
                out var bones) ||
            !TryReadUInt16Array(
                boneData,
                animationTrackBoneLookupDescriptorOffset,
                boneRelativeBase,
                out var animationTrackBoneLookup) ||
            !TryReadColors(
                data,
                md20Base + ColorArrayOffset,
                md20Base,
                modelSequencePayloads,
                out var colors) ||
            !TryReadTextureWeights(
                data,
                md20Base + TextureWeightArrayOffset,
                md20Base,
                modelSequencePayloads,
                out var textureWeights) ||
            !TryReadTextures(
                data,
                md20Base + TextureArrayOffset,
                md20Base,
                textureFileDataIds,
                out var textures) ||
            !TryReadTextureTransforms(
                data,
                md20Base + TextureTransformArrayOffset,
                md20Base,
                modelSequencePayloads,
                out var textureTransforms) ||
            !TryReadLights(
                data,
                md20Base + LightArrayOffset,
                md20Base,
                modelSequencePayloads,
                out var embeddedLights) ||
            !TryReadMaterials(
                data,
                md20Base + MaterialArrayOffset,
                md20Base,
                out var materials) ||
            !TryReadUInt16Array(
                data,
                md20Base + BoneComboArrayOffset,
                md20Base,
                out var boneCombos) ||
            !TryReadUInt16Array(
                data,
                md20Base + TextureComboArrayOffset,
                md20Base,
                out var textureCombos) ||
            !TryReadUInt16Array(
                data,
                md20Base + TextureTransformAnimationStateLookupArrayOffset,
                md20Base,
                out var textureTransformAnimationStateLookup) ||
            !TryReadUInt16Array(
                data,
                md20Base + TextureWeightComboArrayOffset,
                md20Base,
                out var textureWeightCombos) ||
            !TryReadUInt16Array(
                data,
                md20Base + TextureTransformComboArrayOffset,
                md20Base,
                out var textureTransformCombos))
        {
            return null;
        }

        if (skeletonFileDataId is { } resolvedSkeletonFileDataId &&
            skeletonFileData is not null &&
            ResolveEffectiveSkeletonBoneFlags(
                resolvedSkeletonFileDataId,
                skeletonFileData,
                ReadSidecar,
                new HashSet<uint>()) is { } effectiveFlags &&
            effectiveFlags.Length == bones.Length)
        {
            for (var index = 0; index < bones.Length; index++)
                bones[index] = bones[index] with { Flags = effectiveFlags[index] };
        }

        var boneFiles = ReadBoneFiles(
            skeletonFileData ?? data,
            bones.Length,
            ReadSidecar);

        return new WowM2Geometry(
            vertices,
            skinProfiles,
            ReadUInt32(data, md20Base + ViewCountOffset),
            textures,
            textureTransforms,
            bones,
            boneFiles,
            materials,
            boneCombos,
            textureCombos,
            textureTransformAnimationStateLookup,
            textureWeightCombos,
            textureTransformCombos)
        {
            Flags = ReadUInt32(data, md20Base + 16),
            AnimationTrackBoneLookup = animationTrackBoneLookup,
            EmbeddedLights = embeddedLights,
            Colors = colors,
            TextureWeights = textureWeights
        };
    }

    private static bool TryReadLights(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        IReadOnlyDictionary<int, byte[]> sequencePayloads,
        out WowM2Light[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                LightSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2Light[count];
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * LightSize;
            if (!WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 16,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? ambientColor,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 36,
                    4,
                    ReadSingle,
                    out WowModelAnimationTrack<float>? ambientIntensity,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 56,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? diffuseColor,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 76,
                    4,
                    ReadSingle,
                    out WowModelAnimationTrack<float>? diffuseIntensity,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 96,
                    4,
                    ReadSingle,
                    out WowModelAnimationTrack<float>? attenuationStart,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 116,
                    4,
                    ReadSingle,
                    out WowModelAnimationTrack<float>? attenuationEnd,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 136,
                    1,
                    static (bytes, valueOffset) => (float)bytes[valueOffset],
                    out WowModelAnimationTrack<float>? visibility,
                    hasCubicTangents: false,
                    sequencePayloads: sequencePayloads))
            {
                values = [];
                return false;
            }

            values[index] = new WowM2Light(
                ReadUInt16(data, offset),
                ReadInt16(data, offset + 2),
                ReadVector3(data, offset + 4),
                ambientColor,
                ambientIntensity,
                diffuseColor,
                diffuseIntensity,
                attenuationStart,
                attenuationEnd,
                visibility);
        }

        return true;
    }

    private static bool TryReadColors(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        IReadOnlyDictionary<int, byte[]> sequencePayloads,
        out WowM2Color[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                ColorSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2Color[count];
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * ColorSize;
            if (!WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? rgb,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 20,
                    2,
                    ReadFixed16Normalized,
                    out WowModelAnimationTrack<float>? alpha,
                    hasCubicTangents: false,
                    sequencePayloads: sequencePayloads))
            {
                values = [];
                return false;
            }

            values[index] = new WowM2Color(rgb, alpha);
        }

        return true;
    }

    private static bool TryReadTextureWeights(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        IReadOnlyDictionary<int, byte[]> sequencePayloads,
        out WowM2TextureWeight[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                TextureWeightSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2TextureWeight[count];
        for (var index = 0; index < count; index++)
        {
            if (!WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    start + index * TextureWeightSize,
                    2,
                    ReadFixed16Normalized,
                    out WowModelAnimationTrack<float>? weight,
                    hasCubicTangents: false,
                    sequencePayloads: sequencePayloads))
            {
                values = [];
                return false;
            }

            values[index] = new WowM2TextureWeight(weight);
        }

        return true;
    }

    private static bool TryReadTextures(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        IReadOnlyList<uint> fileDataIds,
        out WowM2Texture[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                TextureSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2Texture[count];
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * TextureSize;
            if (!TryReadString(
                    data,
                    offset + 8,
                    relativeBase,
                    out var fileName))
            {
                values = [];
                return false;
            }
            values[index] = new WowM2Texture(
                ReadUInt32(data, offset),
                ReadUInt32(data, offset + 4),
                fileName,
                index < fileDataIds.Count ? fileDataIds[index] : 0);
        }
        return true;
    }

    private static bool TryReadMaterials(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        out WowM2Material[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                MaterialSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2Material[count];
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * MaterialSize;
            values[index] = new WowM2Material(
                ReadUInt16(data, offset),
                ReadUInt16(data, offset + 2));
        }
        return true;
    }

    private static bool TryReadTextureTransforms(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        IReadOnlyDictionary<int, byte[]> sequencePayloads,
        out WowM2TextureTransform[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                TextureTransformSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2TextureTransform[count];
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * TextureTransformSize;
            if (!WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? translation,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 20,
                    16,
                    ReadQuaternion,
                    out WowModelAnimationTrack<Quaternion>? rotation,
                    hasCubicTangents: false,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 40,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? scale,
                    sequencePayloads: sequencePayloads))
            {
                values = [];
                return false;
            }

            values[index] = new WowM2TextureTransform(
                translation,
                rotation,
                scale);
        }

        return true;
    }

    private static bool TryReadBones(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        IReadOnlyDictionary<int, byte[]> sequencePayloads,
        out WowM2Bone[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                BoneSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2Bone[count];
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * BoneSize;
            if (!WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 16,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? translation,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 36,
                    8,
                    ReadPackedQuaternion,
                    out WowModelAnimationTrack<Quaternion>? rotation,
                    hasCubicTangents: false,
                    sequencePayloads: sequencePayloads) ||
                !WowM2MetadataReader.TryReadAnimationTrack(
                    data,
                    relativeBase,
                    offset + 56,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? scale,
                    sequencePayloads: sequencePayloads))
            {
                values = [];
                return false;
            }

            values[index] = new WowM2Bone(
                ReadInt32(data, offset),
                ReadUInt32(data, offset + 4),
                ReadInt16(data, offset + 8),
                ReadUInt16(data, offset + 10),
                ReadUInt16(data, offset + 12),
                ReadUInt16(data, offset + 14),
                translation,
                rotation,
                scale,
                ReadVector3(data, offset + 76));
        }

        return true;
    }

    private static uint[]? ResolveEffectiveSkeletonBoneFlags(
        uint skeletonFileDataId,
        byte[] skeletonData,
        Func<uint, byte[]?> readSidecar,
        HashSet<uint> visiting)
    {
        if (!visiting.Add(skeletonFileDataId))
            return null;

        try
        {
            if (!TryReadSkeletonBoneFlags(skeletonData, out var flags))
                return null;
            if (!TryReadParentSkeletonFileDataId(
                    skeletonData,
                    out var parentFileDataId) ||
                parentFileDataId == 0 ||
                readSidecar(parentFileDataId) is not { } parentData)
            {
                return flags;
            }

            var parentFlags = ResolveEffectiveSkeletonBoneFlags(
                parentFileDataId,
                parentData,
                readSidecar,
                visiting);
            if (parentFlags is null)
                return null;
            if (parentFlags.Length != flags.Length)
                return flags;

            for (var index = 0; index < flags.Length; index++)
                flags[index] |= parentFlags[index] & ParentBoneFlagInheritanceMask;
            return flags;
        }
        finally
        {
            visiting.Remove(skeletonFileDataId);
        }
    }

    private static bool TryReadSkeletonBoneFlags(
        byte[] skeletonData,
        out uint[] flags)
    {
        flags = [];
        if (!TryFindChunkData(
                skeletonData,
                Skb1,
                out var skb1Base,
                out var skb1Size) ||
            skb1Size < 16)
        {
            return false;
        }

        var skb1Data = skeletonData.AsSpan(skb1Base, skb1Size).ToArray();
        if (!TryResolveDescriptor(
                skb1Data,
                0,
                0,
                BoneSize,
                out var count,
                out var start))
        {
            return false;
        }

        flags = new uint[count];
        for (var index = 0; index < count; index++)
            flags[index] = ReadUInt32(skb1Data, start + index * BoneSize + 4);
        return true;
    }

    private static bool TryReadParentSkeletonFileDataId(
        byte[] skeletonData,
        out uint parentFileDataId)
    {
        parentFileDataId = 0;
        if (!TryFindChunkData(
                skeletonData,
                Skpd,
                out var skpdBase,
                out var skpdSize))
        {
            return false;
        }
        if (skpdSize < 12)
            return false;

        parentFileDataId = ReadUInt32(skeletonData, skpdBase + 8);
        return true;
    }

    private static IReadOnlyList<WowM2BoneFile?> ReadBoneFiles(
        byte[] ownerData,
        int boneCount,
        Func<uint, byte[]?> readSidecar)
    {
        var fileDataIds = ReadChunkFileDataIds(ownerData, Bfid);
        var files = new WowM2BoneFile?[fileDataIds.Count];
        for (var index = 0; index < fileDataIds.Count; index++)
        {
            var fileDataId = fileDataIds[index];
            files[index] = readSidecar(fileDataId) is { } sidecar
                ? ReadBoneFile(sidecar, fileDataId, boneCount)
                : null;
        }
        return files;
    }

    private static WowM2BoneFile? ReadBoneFile(
        byte[] sidecar,
        uint fileDataId,
        int boneCount)
    {
        if (sidecar.Length < sizeof(uint))
            return null;

        var chunks = sidecar.AsSpan(sizeof(uint)).ToArray();
        if (!TryFindChunkData(chunks, Bida, out var bidaBase, out var bidaSize) ||
            !TryFindChunkData(chunks, Bomt, out var bomtBase, out var bomtSize) ||
            (bidaSize & 1) != 0 ||
            bomtSize % BoneFileMatrixSize != 0)
        {
            return null;
        }

        var localBoneCount = bidaSize / sizeof(ushort);
        if (localBoneCount > ushort.MaxValue)
            return null;
        var matrixIndexByBone = new ushort[boneCount];
        Array.Fill(matrixIndexByBone, ushort.MaxValue);
        for (var localIndex = 0; localIndex < localBoneCount; localIndex++)
        {
            var globalBoneIndex = ReadUInt16(
                chunks,
                bidaBase + localIndex * sizeof(ushort));
            if (globalBoneIndex < boneCount)
                matrixIndexByBone[globalBoneIndex] = (ushort)localIndex;
        }

        var matrixCount = bomtSize / BoneFileMatrixSize;
        var matrices = new Matrix4x4[matrixCount];
        for (var index = 0; index < matrixCount; index++)
        {
            matrices[index] = ReadMatrix4x4(
                chunks,
                bomtBase + index * BoneFileMatrixSize);
        }

        return new WowM2BoneFile(fileDataId, matrixIndexByBone, matrices);
    }

    private static IReadOnlyDictionary<int, byte[]> ReadExternalAnimationPayloads(
        byte[] ownerData,
        int sequenceDescriptorOffset,
        int relativeBase,
        Func<uint, byte[]?>? readSidecar,
        uint payloadChunkId,
        bool allowRawPayload,
        byte[]? animationFileCatalogData = null)
    {
        var result = new Dictionary<int, byte[]>();
        if (readSidecar is null)
            return result;
        if (!TryResolveDescriptor(
                ownerData,
                sequenceDescriptorOffset,
                relativeBase,
                64,
                out var sequenceCount,
                out var sequenceStart))
            return result;

        var files = WowM2MetadataReader.ReadAnimationFiles(
            animationFileCatalogData ?? ownerData);
        if (files.Count == 0)
            return result;
        var filesByAnimation = files
            .GroupBy(value => (value.AnimationId, value.VariationIndex))
            .ToDictionary(group => group.Key, group => group.First().FileDataId);

        for (var sequenceIndex = 0; sequenceIndex < sequenceCount; sequenceIndex++)
        {
            var sequence = WowM2MetadataReader.ReadSequence(
                ownerData,
                sequenceStart + sequenceIndex * 64);
            if ((sequence.Flags & 0x20) != 0 ||
                !filesByAnimation.TryGetValue(
                    (sequence.AnimationId, sequence.VariationIndex),
                    out var fileDataId) ||
                readSidecar(fileDataId) is not { } sidecar ||
                ExtractAnimationPayload(
                    sidecar,
                    payloadChunkId,
                    allowRawPayload) is not { } payload)
            {
                continue;
            }

            result[sequenceIndex] = payload;
        }
        return result;
    }

    private static byte[]? ExtractAnimationPayload(
        byte[] data,
        uint payloadChunkId,
        bool allowRawPayload)
    {
        long offset = 0;
        while (offset + 8 <= data.LongLength)
        {
            var chunkOffset = checked((int)offset);
            var chunkId = ReadUInt32(data, chunkOffset);
            var chunkSize = ReadUInt32(data, chunkOffset + 4);
            var chunkData = offset + 8;
            var chunkEnd = chunkData + chunkSize;
            if (chunkEnd > data.LongLength)
                return null;
            if (chunkId == payloadChunkId)
            {
                return data.AsSpan(
                    checked((int)chunkData),
                    checked((int)chunkSize)).ToArray();
            }
            offset = chunkEnd;
        }
        return allowRawPayload ? data : null;
    }

    private static bool TryFindChunkData(
        byte[] data,
        uint expectedChunkId,
        out int chunkDataOffset,
        out int chunkDataSize)
    {
        chunkDataOffset = 0;
        chunkDataSize = 0;
        long offset = 0;
        while (offset + 8 <= data.LongLength)
        {
            var header = checked((int)offset);
            var chunkId = ReadUInt32(data, header);
            var chunkSize = ReadUInt32(data, header + 4);
            var chunkData = offset + 8;
            var chunkEnd = chunkData + chunkSize;
            if (chunkEnd > data.LongLength)
                return false;
            if (chunkId == expectedChunkId)
            {
                if (chunkData > int.MaxValue || chunkSize > int.MaxValue)
                    return false;
                chunkDataOffset = (int)chunkData;
                chunkDataSize = (int)chunkSize;
                return true;
            }
            offset = chunkEnd;
        }
        return false;
    }

    private static bool TryReadString(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        out string value)
    {
        value = string.Empty;
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                sizeof(byte),
                out var count,
                out var start))
        {
            return false;
        }

        var length = count;
        while (length > 0 && data[start + length - 1] == 0)
            length--;
        value = Encoding.UTF8.GetString(data, start, length);
        return true;
    }

    private static WowM2SkinProfile? ReadSkinProfile(
        byte[] data,
        uint fileDataId)
    {
        if (!HasRange(data, 0, SkinHeaderSize) || ReadUInt32(data, 0) != Skin)
            return null;

        if (!TryReadUInt16Array(data, 4, out var vertexIndices) ||
            !TryReadUInt16Array(data, 12, out var triangleIndices) ||
            !TryReadUInt32Array(data, 20, out var boneIndices) ||
            !TryReadSkinSections(data, 28, out var skinSections) ||
            !TryReadBatches(data, 36, out var batches) ||
            !ValidateArray(data, 48, ShadowBatchSize))
        {
            return null;
        }

        return new WowM2SkinProfile(
            fileDataId,
            vertexIndices,
            triangleIndices,
            boneIndices,
            skinSections,
            batches,
            ReadUInt32(data, 44));
    }

    private static bool TryReadUInt16Array(
        byte[] data,
        int descriptorOffset,
        out ushort[] values)
        => TryReadUInt16Array(data, descriptorOffset, 0, out values);

    private static bool TryReadUInt16Array(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        out ushort[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                relativeBase,
                sizeof(ushort),
                out var count,
                out var start))
        {
            return false;
        }

        values = new ushort[count];
        for (var index = 0; index < count; index++)
            values[index] = ReadUInt16(data, start + index * 2);
        return true;
    }

    private static bool TryReadUInt32Array(
        byte[] data,
        int descriptorOffset,
        out uint[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                0,
                sizeof(uint),
                out var count,
                out var start))
        {
            return false;
        }

        values = new uint[count];
        for (var index = 0; index < count; index++)
            values[index] = ReadUInt32(data, start + index * 4);
        return true;
    }

    private static bool TryReadSkinSections(
        byte[] data,
        int descriptorOffset,
        out WowM2SkinSection[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                0,
                SkinSectionSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2SkinSection[count];
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * SkinSectionSize;
            values[index] = new WowM2SkinSection(
                ReadUInt16(data, offset),
                ReadUInt16(data, offset + 2),
                ReadUInt16(data, offset + 4),
                ReadUInt16(data, offset + 6),
                ReadUInt16(data, offset + 8),
                ReadUInt16(data, offset + 10),
                ReadUInt16(data, offset + 12),
                ReadUInt16(data, offset + 14),
                ReadUInt16(data, offset + 16),
                ReadUInt16(data, offset + 18),
                ReadVector3(data, offset + 20),
                ReadVector3(data, offset + 32),
                ReadSingle(data, offset + 44));
        }
        return true;
    }

    private static bool TryReadBatches(
        byte[] data,
        int descriptorOffset,
        out WowM2Batch[] values)
    {
        values = [];
        if (!TryResolveDescriptor(
                data,
                descriptorOffset,
                0,
                BatchSize,
                out var count,
                out var start))
        {
            return false;
        }

        values = new WowM2Batch[count];
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * BatchSize;
            values[index] = new WowM2Batch(
                data[offset],
                unchecked((sbyte)data[offset + 1]),
                ReadInt16(data, offset + 2),
                ReadUInt16(data, offset + 4),
                ReadUInt16(data, offset + 6),
                ReadInt16(data, offset + 8),
                ReadUInt16(data, offset + 10),
                ReadUInt16(data, offset + 12),
                ReadUInt16(data, offset + 14),
                ReadUInt16(data, offset + 16),
                ReadUInt16(data, offset + 18),
                ReadUInt16(data, offset + 20),
                ReadUInt16(data, offset + 22));
        }
        return true;
    }

    private static bool ValidateArray(
        byte[] data,
        int descriptorOffset,
        int elementSize) =>
        TryResolveDescriptor(
            data,
            descriptorOffset,
            0,
            elementSize,
            out _,
            out _);

    private static bool TryResolveDescriptor(
        byte[] data,
        int descriptorOffset,
        int relativeBase,
        int elementSize,
        out int count,
        out int start)
    {
        count = 0;
        start = 0;
        if (!HasRange(data, descriptorOffset, 8))
            return false;

        var rawCount = ReadUInt16(data, descriptorOffset);
        var rawOffset = ReadUInt32(data, descriptorOffset + 4);
        if (!TryResolveArrayRange(
                data,
                relativeBase,
                rawCount,
                rawOffset,
                elementSize,
                out start))
        {
            return false;
        }
        count = (int)rawCount;
        return true;
    }

    private static IReadOnlyList<uint> ReadChunkFileDataIds(
        byte[] data,
        uint requestedChunkId)
    {
        if (ReadUInt32(data, 0) == Md20)
            return [];

        var result = new List<uint>();
        long offset = 0;
        while (offset + 8 <= data.LongLength)
        {
            var chunkOffset = checked((int)offset);
            var chunkId = ReadUInt32(data, chunkOffset);
            var chunkSize = ReadUInt32(data, chunkOffset + 4);
            var chunkData = offset + 8;
            var chunkEnd = chunkData + chunkSize;
            if (chunkEnd > data.LongLength)
                return [];

            if (chunkId == requestedChunkId)
            {
                if ((chunkSize & 3) != 0)
                    return [];
                for (var entry = chunkData; entry < chunkEnd; entry += 4)
                    result.Add(ReadUInt32(data, checked((int)entry)));
            }
            offset = chunkEnd;
        }
        return result;
    }

    private static int FindMd20Base(byte[] data)
    {
        if (ReadUInt32(data, 0) == Md20)
            return 0;

        long offset = 0;
        while (offset + 8 <= data.LongLength)
        {
            var chunkOffset = checked((int)offset);
            var chunkId = ReadUInt32(data, chunkOffset);
            var chunkSize = ReadUInt32(data, chunkOffset + 4);
            var chunkData = offset + 8;
            var chunkEnd = chunkData + chunkSize;
            if (chunkEnd > data.LongLength)
                return -1;
            if (chunkId == Md21 &&
                chunkSize >= HeaderSize &&
                ReadUInt32(data, checked((int)chunkData)) == Md20)
            {
                return checked((int)chunkData);
            }
            offset = chunkEnd;
        }
        return -1;
    }

    private static bool TryResolveArrayRange(
        byte[] data,
        int relativeBase,
        uint count,
        uint relativeOffset,
        int elementSize,
        out int start)
    {
        start = 0;
        var absoluteStart = (long)relativeBase + relativeOffset;
        var byteCount = (long)count * elementSize;
        if (count > int.MaxValue ||
            absoluteStart < 0 ||
            absoluteStart > int.MaxValue ||
            absoluteStart + byteCount > data.LongLength)
        {
            return false;
        }
        start = (int)absoluteStart;
        return true;
    }

    private static bool HasRange(byte[] data, int offset, int size) =>
        offset >= 0 && size >= 0 && (long)offset + size <= data.LongLength;

    private static ushort ReadUInt16(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));

    private static short ReadInt16(byte[] data, int offset) =>
        BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset, 2));

    private static int ReadInt32(byte[] data, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));

    private static uint ReadUInt32(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));

    private static float ReadSingle(byte[] data, int offset) =>
        BitConverter.Int32BitsToSingle(unchecked((int)ReadUInt32(data, offset)));

    private static float ReadFixed16Normalized(byte[] data, int offset) =>
        ReadInt16(data, offset) * 0.000030518509f;

    private static Vector2 ReadVector2(byte[] data, int offset) =>
        new(ReadSingle(data, offset), ReadSingle(data, offset + 4));

    private static Vector3 ReadVector3(byte[] data, int offset) =>
        new(
            ReadSingle(data, offset),
            ReadSingle(data, offset + 4),
            ReadSingle(data, offset + 8));

    private static Matrix4x4 ReadMatrix4x4(byte[] data, int offset) =>
        new(
            ReadSingle(data, offset),
            ReadSingle(data, offset + 4),
            ReadSingle(data, offset + 8),
            ReadSingle(data, offset + 12),
            ReadSingle(data, offset + 16),
            ReadSingle(data, offset + 20),
            ReadSingle(data, offset + 24),
            ReadSingle(data, offset + 28),
            ReadSingle(data, offset + 32),
            ReadSingle(data, offset + 36),
            ReadSingle(data, offset + 40),
            ReadSingle(data, offset + 44),
            ReadSingle(data, offset + 48),
            ReadSingle(data, offset + 52),
            ReadSingle(data, offset + 56),
            ReadSingle(data, offset + 60));

    private static Quaternion ReadQuaternion(byte[] data, int offset) =>
        new(
            ReadSingle(data, offset),
            ReadSingle(data, offset + 4),
            ReadSingle(data, offset + 8),
            ReadSingle(data, offset + 12));

    private static Quaternion ReadPackedQuaternion(byte[] data, int offset)
    {
        var scale = BitConverter.Int32BitsToSingle(0x38000080);
        return new Quaternion(
            ReadUInt16(data, offset) * scale - 1.0f,
            ReadUInt16(data, offset + 2) * scale - 1.0f,
            ReadUInt16(data, offset + 4) * scale - 1.0f,
            ReadUInt16(data, offset + 6) * scale - 1.0f);
    }
}
