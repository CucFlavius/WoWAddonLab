using System.Buffers.Binary;
using System.Numerics;
using WoWAddonLab.Assets;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class WowM2MetadataReaderTests
{
    [Fact]
    public void GeometryReaderReadsNativeEmbeddedLightLayout()
    {
        const int md20Base = 8;
        const int lightOffset = 304;
        const int lightSize = 156;
        const int md21Size = lightOffset + lightSize;
        var data = new byte[md20Base + md21Size];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 16, 0x8000);
        WriteArrayDescriptor(data, md20Base + 52, 3, 296);
        WriteUInt16(data, md20Base + 296, 5);
        WriteUInt16(data, md20Base + 298, ushort.MaxValue);
        WriteUInt16(data, md20Base + 300, 11);
        WriteUInt16(data, md20Base + 264, 1);
        WriteUInt16(data, md20Base + 266, 0xD001);
        WriteUInt32(data, md20Base + 268, lightOffset);

        var light = md20Base + lightOffset;
        WriteUInt16(data, light, 1);
        WriteInt16(data, light + 2, 7);
        WriteVector3(data, light + 4, new Vector3(1, 2, 3));
        foreach (var trackOffset in new[] { 16, 36, 56, 76, 96, 116, 136 })
            WriteInt16(data, light + trackOffset + 2, -1);

        var geometry = WowM2GeometryReader.Read(data);

        Assert.NotNull(geometry);
        Assert.Equal(0x8000u, geometry.Flags);
        Assert.Equal([5, ushort.MaxValue, 11], geometry.AnimationTrackBoneLookup);
        Assert.Equal(1u, geometry.EmbeddedLightCount);
        var parsed = Assert.Single(geometry.EmbeddedLights);
        Assert.Equal((ushort)1, parsed.Type);
        Assert.Equal((short)7, parsed.BoneIndex);
        Assert.Equal(new Vector3(1, 2, 3), parsed.Position);
        Assert.NotNull(parsed.AmbientColor);
        Assert.NotNull(parsed.AmbientIntensity);
        Assert.NotNull(parsed.DiffuseColor);
        Assert.NotNull(parsed.DiffuseIntensity);
        Assert.NotNull(parsed.AttenuationStart);
        Assert.NotNull(parsed.AttenuationEnd);
        Assert.NotNull(parsed.Visibility);
    }

    [Fact]
    public void ReadsAnimationFileDataIdChunk()
    {
        const int md20Base = 8;
        const int md21Size = 304;
        var data = new byte[8 + md21Size + 16];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        var afid = 8 + md21Size;
        WriteUInt32(data, afid, 0x44494641);
        WriteUInt32(data, afid + 4, 8);
        WriteUInt16(data, afid + 8, 42);
        WriteUInt16(data, afid + 10, 3);
        WriteUInt32(data, afid + 12, 987654);

        var metadata = WowM2MetadataReader.Read(data);

        Assert.NotNull(metadata);
        Assert.Equal(
            new WowModelAnimationFileMetadata(42, 3, 987654),
            Assert.Single(metadata.AnimationFiles));
    }

    [Fact]
    public void ReadsPerSequenceTrackPayloadFromExternalAnimationData()
    {
        var model = new byte[36];
        WriteUInt16(model, 0, 1);
        WriteInt16(model, 2, -1);
        WriteUInt32(model, 4, 1);
        WriteUInt32(model, 8, 20);
        WriteUInt32(model, 12, 1);
        WriteUInt32(model, 16, 28);
        WriteUInt32(model, 20, 2);
        WriteUInt32(model, 24, 0);
        WriteUInt32(model, 28, 2);
        WriteUInt32(model, 32, 8);

        var payload = new byte[32];
        WriteUInt32(payload, 0, 0);
        WriteUInt32(payload, 4, 1000);
        WriteVector3(payload, 8, new System.Numerics.Vector3(1, 2, 3));
        WriteVector3(payload, 20, new System.Numerics.Vector3(4, 5, 6));

        Assert.True(WowM2MetadataReader.TryReadAnimationTrack(
            model,
            0,
            0,
            12,
            static (bytes, offset) => new System.Numerics.Vector3(
                BitConverter.ToSingle(bytes, offset),
                BitConverter.ToSingle(bytes, offset + 4),
                BitConverter.ToSingle(bytes, offset + 8)),
            out WowModelAnimationTrack<System.Numerics.Vector3>? track,
            sequencePayloads: new Dictionary<int, byte[]> { [0] = payload }));

        var sequence = Assert.Single(track!.Sequences);
        Assert.Equal([0u, 1000u], sequence.TimestampsMilliseconds);
        Assert.Equal(new System.Numerics.Vector3(1, 2, 3), sequence.Keys[0].Value);
        Assert.Equal(new System.Numerics.Vector3(4, 5, 6), sequence.Keys[1].Value);
    }

    [Fact]
    public void GeometryReaderResolvesAfidAndAfm2BonePayload()
    {
        const int md20Base = 8;
        const int sequenceOffset = 304;
        const int boneOffset = sequenceOffset + 64;
        const int timestampSequencesOffset = boneOffset + 88;
        const int valueSequencesOffset = timestampSequencesOffset + 8;
        const int md21Size = valueSequencesOffset + 8;
        var model = new byte[8 + md21Size + 16];
        WriteUInt32(model, 0, 0x3132444D);
        WriteUInt32(model, 4, md21Size);
        WriteUInt32(model, md20Base, 0x3032444D);
        WriteUInt32(model, md20Base + 28, 1);
        WriteUInt32(model, md20Base + 32, sequenceOffset);
        WriteUInt32(model, md20Base + 44, 1);
        WriteUInt32(model, md20Base + 48, boneOffset);
        WriteUInt16(model, md20Base + 30, 0xD001);
        WriteUInt16(model, md20Base + 46, 0xD002);
        WriteSequence(
            model,
            md20Base + sequenceOffset,
            42,
            0,
            1000,
            0x1,
            1,
            -1,
            -1);

        var bone = md20Base + boneOffset;
        WriteInt16(model, bone + 8, -1);
        WriteUInt16(model, bone + 16, 1);
        WriteInt16(model, bone + 18, -1);
        WriteUInt32(model, bone + 20, 1);
        WriteUInt32(model, bone + 24, timestampSequencesOffset);
        WriteUInt32(model, bone + 28, 1);
        WriteUInt32(model, bone + 32, valueSequencesOffset);
        WriteUInt16(model, bone + 22, 0xD003);
        WriteUInt16(model, bone + 30, 0xD004);
        WriteUInt32(model, md20Base + timestampSequencesOffset, 2);
        WriteUInt32(model, md20Base + timestampSequencesOffset + 4, 0);
        WriteUInt32(model, md20Base + valueSequencesOffset, 2);
        WriteUInt32(model, md20Base + valueSequencesOffset + 4, 8);
        WriteUInt16(model, md20Base + timestampSequencesOffset + 2, 0xD005);
        WriteUInt16(model, md20Base + valueSequencesOffset + 2, 0xD006);

        var afid = 8 + md21Size;
        WriteUInt32(model, afid, 0x44494641);
        WriteUInt32(model, afid + 4, 8);
        WriteUInt16(model, afid + 8, 42);
        WriteUInt16(model, afid + 10, 0);
        WriteUInt32(model, afid + 12, 701);

        var sidecar = new byte[40];
        WriteUInt32(sidecar, 0, 0x324D4641);
        WriteUInt32(sidecar, 4, 32);
        WriteUInt32(sidecar, 8, 0);
        WriteUInt32(sidecar, 12, 1000);
        WriteVector3(sidecar, 16, new System.Numerics.Vector3(1, 2, 3));
        WriteVector3(sidecar, 28, new System.Numerics.Vector3(4, 5, 6));

        var geometry = WowM2GeometryReader.Read(
            model,
            fileDataId => fileDataId == 701 ? sidecar : null);

        Assert.NotNull(geometry);
        var translation = Assert.IsType<
            WowModelAnimationTrack<System.Numerics.Vector3>>(
            Assert.Single(geometry.Bones).Translation);
        var sequence = Assert.Single(translation.Sequences);
        Assert.Equal([0u, 1000u], sequence.TimestampsMilliseconds);
        Assert.Equal(new System.Numerics.Vector3(1, 2, 3), sequence.Keys[0].Value);
        Assert.Equal(new System.Numerics.Vector3(4, 5, 6), sequence.Keys[1].Value);
    }

    [Fact]
    public void GeometryReaderUsesSkb1BonesAndAfsbSequencePayload()
    {
        const uint skeletonFileDataId = 9876;
        const uint animationFileDataId = 701;
        const int md20Base = 8;
        const int textureTransformOffset = 304;
        const int timestampSequencesOffset = 364;
        const int valueSequencesOffset = 372;
        const int md21Size = 380;
        var model = new byte[8 + md21Size + 12];
        WriteUInt32(model, 0, 0x3132444D);
        WriteUInt32(model, 4, md21Size);
        WriteUInt32(model, md20Base, 0x3032444D);
        WriteArrayDescriptor(
            model,
            md20Base + 96,
            1,
            textureTransformOffset);
        WriteUInt16(model, md20Base + 98, 0xE001);
        WriteTrackHeader(
            model,
            md20Base + textureTransformOffset,
            1,
            timestampSequencesOffset,
            valueSequencesOffset);
        WriteArrayDescriptor(
            model,
            md20Base + timestampSequencesOffset,
            2,
            0);
        WriteArrayDescriptor(
            model,
            md20Base + valueSequencesOffset,
            2,
            8);
        WriteUInt16(
            model,
            md20Base + textureTransformOffset + 6,
            0xE002);
        WriteUInt16(
            model,
            md20Base + textureTransformOffset + 14,
            0xE003);
        WriteUInt16(model, md20Base + timestampSequencesOffset + 2, 0xE004);
        WriteUInt16(model, md20Base + valueSequencesOffset + 2, 0xE005);
        var skid = 8 + md21Size;
        WriteUInt32(model, skid, 0x44494B53);
        WriteUInt32(model, skid + 4, 4);
        WriteUInt32(model, skid + 8, skeletonFileDataId);

        const int sks1DataSize = 94;
        const int skb1DataSize = 128;
        var sks1 = 0;
        var skb1 = 8 + sks1DataSize;
        var afid = skb1 + 8 + skb1DataSize;
        var skeleton = new byte[afid + 16];
        WriteUInt32(skeleton, sks1, 0x31534B53);
        WriteUInt32(skeleton, sks1 + 4, sks1DataSize);
        var sks1Data = sks1 + 8;
        WriteArrayDescriptor(skeleton, sks1Data, 1, 24);
        WriteArrayDescriptor(skeleton, sks1Data + 8, 1, 28);
        WriteArrayDescriptor(skeleton, sks1Data + 16, 1, 92);
        WriteUInt16(skeleton, sks1Data + 2, 0xA001);
        WriteUInt16(skeleton, sks1Data + 10, 0xA002);
        WriteUInt16(skeleton, sks1Data + 18, 0xA003);
        WriteUInt32(skeleton, sks1Data + 24, 2400);
        WriteSequence(
            skeleton,
            sks1Data + 28,
            42,
            0,
            1000,
            0x1,
            1,
            -1,
            -1);
        WriteUInt16(skeleton, sks1Data + 92, 0);

        WriteUInt32(skeleton, skb1, 0x31424B53);
        WriteUInt32(skeleton, skb1 + 4, skb1DataSize);
        var skb1Data = skb1 + 8;
        WriteArrayDescriptor(skeleton, skb1Data, 1, 16);
        WriteArrayDescriptor(skeleton, skb1Data + 8, 1, 104);
        WriteUInt16(skeleton, skb1Data + 2, 0xB001);
        WriteUInt16(skeleton, skb1Data + 10, 0xB002);
        var bone = skb1Data + 16;
        WriteInt16(skeleton, bone + 8, -1);
        WriteUInt16(skeleton, bone + 16, 1);
        WriteInt16(skeleton, bone + 18, -1);
        WriteArrayDescriptor(skeleton, bone + 20, 1, 112);
        WriteArrayDescriptor(skeleton, bone + 28, 1, 120);
        WriteUInt16(skeleton, bone + 22, 0xB003);
        WriteUInt16(skeleton, bone + 30, 0xB004);
        WriteUInt16(skeleton, skb1Data + 104, 0);
        WriteArrayDescriptor(skeleton, skb1Data + 112, 2, 0);
        WriteArrayDescriptor(skeleton, skb1Data + 120, 2, 8);
        WriteUInt16(skeleton, skb1Data + 114, 0xB005);
        WriteUInt16(skeleton, skb1Data + 122, 0xB006);

        WriteUInt32(skeleton, afid, 0x44494641);
        WriteUInt32(skeleton, afid + 4, 8);
        WriteUInt16(skeleton, afid + 8, 42);
        WriteUInt16(skeleton, afid + 10, 0);
        WriteUInt32(skeleton, afid + 12, animationFileDataId);

        var animation = new byte[80];
        WriteUInt32(animation, 0, 0x42534641);
        WriteUInt32(animation, 4, 32);
        WriteUInt32(animation, 8, 0);
        WriteUInt32(animation, 12, 1000);
        WriteVector3(animation, 16, new System.Numerics.Vector3(1, 2, 3));
        WriteVector3(animation, 28, new System.Numerics.Vector3(4, 5, 6));
        WriteUInt32(animation, 40, 0x324D4641);
        WriteUInt32(animation, 44, 32);
        WriteUInt32(animation, 48, 0);
        WriteUInt32(animation, 52, 1000);
        WriteVector3(animation, 56, new System.Numerics.Vector3(10, 20, 30));
        WriteVector3(animation, 68, new System.Numerics.Vector3(40, 50, 60));

        var geometry = WowM2GeometryReader.Read(
            model,
            fileDataId => fileDataId switch
            {
                skeletonFileDataId => skeleton,
                animationFileDataId => animation,
                _ => null
            });

        Assert.NotNull(geometry);
        var translation = Assert.IsType<
            WowModelAnimationTrack<System.Numerics.Vector3>>(
            Assert.Single(geometry.Bones).Translation);
        var sequence = Assert.Single(translation.Sequences);
        Assert.Equal([0u, 1000u], sequence.TimestampsMilliseconds);
        Assert.Equal(new System.Numerics.Vector3(1, 2, 3), sequence.Keys[0].Value);
        Assert.Equal(new System.Numerics.Vector3(4, 5, 6), sequence.Keys[1].Value);
        var modelTranslation = Assert.IsType<
            WowModelAnimationTrack<System.Numerics.Vector3>>(
            Assert.Single(geometry.TextureTransforms).Translation);
        Assert.Equal(
            new System.Numerics.Vector3(40, 50, 60),
            Assert.Single(modelTranslation.Sequences).Keys[1].Value);
        Assert.Equal([0], geometry.AnimationTrackBoneLookup);
    }

    [Fact]
    public void GeometryReaderRecursivelyInheritsSkpdParentBoneFlagsUsingNativeMask()
    {
        const uint childFileDataId = 100;
        const uint parentFileDataId = 200;
        const uint grandparentFileDataId = 300;
        var model = BuildModelWithSkeleton(childFileDataId);
        var child = BuildSkeleton([0x00000010], parentFileDataId);
        var parent = BuildSkeleton([0x80070020], grandparentFileDataId);
        var grandparent = BuildSkeleton([0x00000040]);

        var geometry = WowM2GeometryReader.Read(
            model,
            fileDataId => fileDataId switch
            {
                childFileDataId => child,
                parentFileDataId => parent,
                grandparentFileDataId => grandparent,
                _ => null
            });

        Assert.NotNull(geometry);
        Assert.Equal(0x80000070u, Assert.Single(geometry.Bones).Flags);
    }

    [Fact]
    public void GeometryReaderDoesNotInheritSkpdFlagsWhenBoneCountsDiffer()
    {
        const uint childFileDataId = 100;
        const uint parentFileDataId = 200;
        var model = BuildModelWithSkeleton(childFileDataId);
        var child = BuildSkeleton([0x00000011], parentFileDataId);
        var parent = BuildSkeleton([0x80000020, 0x00000040]);

        var geometry = WowM2GeometryReader.Read(
            model,
            fileDataId => fileDataId switch
            {
                childFileDataId => child,
                parentFileDataId => parent,
                _ => null
            });

        Assert.NotNull(geometry);
        Assert.Equal(0x00000011u, Assert.Single(geometry.Bones).Flags);
    }

    [Fact]
    public void GeometryReaderRejectsCyclicSkpdInheritanceWithoutChangingFlags()
    {
        const uint childFileDataId = 100;
        const uint parentFileDataId = 200;
        var model = BuildModelWithSkeleton(childFileDataId);
        var child = BuildSkeleton([0x00000011], parentFileDataId);
        var parent = BuildSkeleton([0x80000020], childFileDataId);

        var geometry = WowM2GeometryReader.Read(
            model,
            fileDataId => fileDataId switch
            {
                childFileDataId => child,
                parentFileDataId => parent,
                _ => null
            });

        Assert.NotNull(geometry);
        Assert.Equal(0x00000011u, Assert.Single(geometry.Bones).Flags);
    }

    [Fact]
    public void GeometryReaderCatalogsBfidBoneFilesUsingBidaAndBomt()
    {
        const uint skeletonFileDataId = 100;
        const uint residentBoneFileDataId = 700;
        const uint missingBoneFileDataId = 701;
        var model = BuildModelWithSkeleton(skeletonFileDataId);
        var skeleton = BuildSkeleton(
            [0, 0],
            boneFileDataIds:
            [
                residentBoneFileDataId,
                missingBoneFileDataId
            ]);
        var localZero = Matrix4x4.CreateTranslation(10, 20, 30);
        var localOne = Matrix4x4.CreateScale(2, 3, 4);
        var boneFile = BuildBoneFile([1, 0], [localZero, localOne]);

        var geometry = WowM2GeometryReader.Read(
            model,
            fileDataId => fileDataId switch
            {
                skeletonFileDataId => skeleton,
                residentBoneFileDataId => boneFile,
                _ => null
            });

        Assert.NotNull(geometry);
        Assert.Equal(2, geometry.BoneFiles.Count);
        var parsed = Assert.IsType<WowM2BoneFile>(geometry.BoneFiles[0]);
        Assert.Null(geometry.BoneFiles[1]);
        Assert.Same(parsed, geometry.FindBoneFile(residentBoneFileDataId));
        Assert.Null(geometry.FindBoneFile(missingBoneFileDataId));
        Assert.True(parsed.TryGetMatrix(0, out var globalZero));
        Assert.True(parsed.TryGetMatrix(1, out var globalOne));
        Assert.Equal(localOne, globalZero);
        Assert.Equal(localZero, globalOne);
    }

    [Fact]
    public void ReadsNativeVertexStreamAndSfidSkinProfileLayout()
    {
        const uint skinFileDataId = 4567;
        const int md20Base = 8;
        const int vertexOffset = 304;
        const int vertexCount = 3;
        const int vertexSize = 48;
        const int md21Size = vertexOffset + vertexCount * vertexSize;
        var data = new byte[8 + md21Size + 12];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteUInt32(data, md20Base + 60, vertexCount);
        WriteUInt32(data, md20Base + 64, vertexOffset);
        WriteUInt32(data, md20Base + 68, 1);
        WriteVertex(
            data,
            md20Base + vertexOffset,
            new System.Numerics.Vector3(1, 2, 3),
            0x04030201,
            0x08070605,
            new System.Numerics.Vector3(4, 5, 6),
            new System.Numerics.Vector2(.25f, .5f),
            new System.Numerics.Vector2(.75f, 1));
        WriteVertex(
            data,
            md20Base + vertexOffset + vertexSize,
            new System.Numerics.Vector3(7, 8, 9),
            0,
            0,
            System.Numerics.Vector3.UnitZ,
            System.Numerics.Vector2.Zero,
            System.Numerics.Vector2.One);
        WriteVertex(
            data,
            md20Base + vertexOffset + vertexSize * 2,
            new System.Numerics.Vector3(10, 11, 12),
            uint.MaxValue,
            0x03020100,
            System.Numerics.Vector3.UnitY,
            System.Numerics.Vector2.One,
            System.Numerics.Vector2.Zero);

        var sfidOffset = 8 + md21Size;
        WriteUInt32(data, sfidOffset, 0x44494653);
        WriteUInt32(data, sfidOffset + 4, 4);
        WriteUInt32(data, sfidOffset + 8, skinFileDataId);

        var skin = new byte[152];
        WriteUInt32(skin, 0, 0x4E494B53);
        WriteArrayDescriptor(skin, 4, 3, 56);
        WriteUInt16(skin, 56, 2);
        WriteUInt16(skin, 58, 0);
        WriteUInt16(skin, 60, 1);
        WriteArrayDescriptor(skin, 12, 3, 62);
        WriteUInt16(skin, 62, 0);
        WriteUInt16(skin, 64, 1);
        WriteUInt16(skin, 66, 2);
        WriteArrayDescriptor(skin, 20, 3, 68);
        WriteUInt32(skin, 68, 0x01020304);
        WriteUInt32(skin, 72, 0x05060708);
        WriteUInt32(skin, 76, 0x090A0B0C);
        WriteArrayDescriptor(skin, 28, 1, 80);
        WriteUInt16(skin, 80, 17);
        WriteUInt16(skin, 82, 2);
        WriteUInt16(skin, 84, 0);
        WriteUInt16(skin, 86, 3);
        WriteUInt16(skin, 88, 0);
        WriteUInt16(skin, 90, 3);
        WriteUInt16(skin, 92, 4);
        WriteUInt16(skin, 94, 6);
        WriteUInt16(skin, 96, 8);
        WriteUInt16(skin, 98, 10);
        WriteVector3(skin, 100, new System.Numerics.Vector3(13, 14, 15));
        WriteVector3(skin, 112, new System.Numerics.Vector3(16, 17, 18));
        WriteSingle(skin, 124, 19);
        WriteArrayDescriptor(skin, 36, 1, 128);
        skin[128] = 0x10;
        skin[129] = unchecked((byte)-2);
        WriteUInt16(skin, 130, 0x9234);
        WriteUInt16(skin, 132, 0);
        WriteUInt16(skin, 134, 17);
        WriteInt16(skin, 136, -1);
        WriteUInt16(skin, 138, 3);
        WriteUInt16(skin, 140, 4);
        WriteUInt16(skin, 142, 2);
        WriteUInt16(skin, 144, 5);
        WriteUInt16(skin, 146, 6);
        WriteUInt16(skin, 148, 7);
        WriteUInt16(skin, 150, 8);
        WriteUInt32(skin, 44, 23);
        WriteArrayDescriptor(skin, 48, 0, 0);

        uint requestedFileDataId = 0;
        var geometry = WowM2GeometryReader.Read(
            data,
            fileDataId =>
            {
                requestedFileDataId = fileDataId;
                return skin;
            });

        Assert.NotNull(geometry);
        Assert.Equal(skinFileDataId, requestedFileDataId);
        Assert.Equal(1u, geometry.ViewCount);
        Assert.Equal(3, geometry.Vertices.Count);
        Assert.Equal(new System.Numerics.Vector3(1, 2, 3), geometry.Vertices[0].Position);
        Assert.Equal(0x04030201u, geometry.Vertices[0].PackedBoneWeights);
        Assert.Equal(0x08070605u, geometry.Vertices[0].PackedBoneIndices);
        Assert.Equal(
            new WowM2VertexInfluences(1, 2, 3, 4, 5, 6, 7, 8),
            geometry.Vertices[0].Influences);
        const float normalizedByteScale = 1f / byte.MaxValue;
        Assert.Equal(
            new Vector4(
                normalizedByteScale,
                2 * normalizedByteScale,
                3 * normalizedByteScale,
                4 * normalizedByteScale),
            geometry.Vertices[0].Influences.NormalizedWeights);
        Assert.Equal(new System.Numerics.Vector3(4, 5, 6), geometry.Vertices[0].Normal);
        Assert.Equal(new System.Numerics.Vector2(.25f, .5f), geometry.Vertices[0].TextureCoordinate0);
        Assert.Equal(new System.Numerics.Vector2(.75f, 1), geometry.Vertices[0].TextureCoordinate1);

        var profile = Assert.IsType<WowM2SkinProfile>(geometry.BaselineSkinProfile);
        Assert.Equal([2, 0, 1], profile.VertexIndices);
        Assert.Equal([0, 1, 2], profile.TriangleIndices);
        Assert.Equal([0x01020304u, 0x05060708u, 0x090A0B0Cu], profile.BoneIndices);
        Assert.Equal(23u, profile.BoneCountMax);
        Assert.True(profile.TryResolveTriangleVertexIndices(out var resolvedIndices));
        Assert.Equal([2, 0, 1], resolvedIndices);

        var section = Assert.Single(profile.SkinSections);
        Assert.Equal((ushort)17, section.SkinSectionId);
        Assert.Equal((ushort)3, section.VertexCount);
        Assert.Equal((ushort)3, section.IndexCount);
        Assert.Equal(new System.Numerics.Vector3(13, 14, 15), section.CenterPosition);
        Assert.Equal(new System.Numerics.Vector3(16, 17, 18), section.SortCenterPosition);
        Assert.Equal(19, section.SortRadius);

        var batch = Assert.Single(profile.Batches);
        Assert.Equal((byte)0x10, batch.Flags);
        Assert.Equal((sbyte)-2, batch.PriorityPlane);
        Assert.Equal(unchecked((short)0x9234), batch.ShaderId);
        Assert.Equal((short)-1, batch.ColorIndex);
        Assert.Equal((ushort)2, batch.TextureCount);
        Assert.Equal((ushort)8, batch.TextureTransformComboIndex);
    }

    [Fact]
    public void RejectsVertexArrayThatExceedsTheNativeFortyEightByteRange()
    {
        const int md20Base = 8;
        const int md21Size = 304;
        var data = new byte[8 + md21Size];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteUInt32(data, md20Base + 60, 1);
        WriteUInt32(data, md20Base + 64, 304);

        Assert.Null(WowM2GeometryReader.Read(data));
    }

    [Fact]
    public void ReadsNativeEightyEightByteBoneAndPackedQuaternionTrack()
    {
        const int md20Base = 8;
        const int boneOffset = 304;
        const int timestampSequencesOffset = 392;
        const int valueSequencesOffset = 400;
        const int timestampsOffset = 408;
        const int valuesOffset = 412;
        const int md21Size = 420;
        var data = new byte[8 + md21Size];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteArrayDescriptor(data, md20Base + 44, 1, boneOffset);

        var bone = md20Base + boneOffset;
        WriteInt32(data, bone, -7);
        WriteUInt32(data, bone + 4, 0x12345678);
        WriteInt16(data, bone + 8, -1);
        WriteUInt16(data, bone + 10, 9);
        WriteUInt16(data, bone + 12, 10);
        WriteUInt16(data, bone + 14, 11);
        WriteUInt16(data, bone + 36, 0);
        WriteInt16(data, bone + 38, -1);
        WriteArrayDescriptor(data, bone + 40, 1, timestampSequencesOffset);
        WriteArrayDescriptor(data, bone + 48, 1, valueSequencesOffset);
        WriteVector3(data, bone + 76, new Vector3(1, 2, 3));

        WriteArrayDescriptor(
            data,
            md20Base + timestampSequencesOffset,
            1,
            timestampsOffset);
        WriteArrayDescriptor(
            data,
            md20Base + valueSequencesOffset,
            1,
            valuesOffset);
        WriteUInt32(data, md20Base + timestampsOffset, 123);
        ushort[] packed = [0, 32768, 65535, 16384];
        for (var index = 0; index < packed.Length; index++)
            WriteUInt16(data, md20Base + valuesOffset + index * 2, packed[index]);

        var geometry = WowM2GeometryReader.Read(data);

        Assert.NotNull(geometry);
        var parsed = Assert.Single(geometry.Bones);
        Assert.Equal(-7, parsed.KeyBoneId);
        Assert.Equal(0x12345678u, parsed.Flags);
        Assert.Equal((short)-1, parsed.ParentBoneIndex);
        Assert.Equal((ushort)9, parsed.SubmeshId);
        Assert.Equal((ushort)10, parsed.Unknown0);
        Assert.Equal((ushort)11, parsed.Unknown1);
        Assert.Equal(new Vector3(1, 2, 3), parsed.Pivot);
        var track = Assert.IsType<WowModelAnimationTrack<Quaternion>>(
            parsed.Rotation);
        var key = Assert.Single(Assert.Single(track.Sequences).Keys).Value;
        var scale = BitConverter.Int32BitsToSingle(0x38000080);
        Assert.Equal(packed[0] * scale - 1f, key.X);
        Assert.Equal(packed[1] * scale - 1f, key.Y);
        Assert.Equal(packed[2] * scale - 1f, key.Z);
        Assert.Equal(packed[3] * scale - 1f, key.W);
    }

    [Fact]
    public void ReadsNativeTextureMaterialComboAndTxidMapping()
    {
        const int md20Base = 8;
        const int textureOffset = 304;
        const int materialOffset = 336;
        const int boneComboOffset = 380;
        const int textureComboOffset = 344;
        const int transformBoneMapOffset = 350;
        const int weightComboOffset = 354;
        const int transformComboOffset = 356;
        const int fileNameOffset = 360;
        const int md21Size = 384;
        const string fileName = "textures/test.blp";
        var data = new byte[8 + md21Size + 16];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);

        WriteArrayDescriptor(data, md20Base + 80, 2, textureOffset);
        var firstTexture = md20Base + textureOffset;
        WriteUInt32(data, firstTexture, 0);
        WriteUInt32(data, firstTexture + 4, 3);
        WriteArrayDescriptor(
            data,
            firstTexture + 8,
            (uint)fileName.Length + 1,
            fileNameOffset);
        var secondTexture = firstTexture + 16;
        WriteUInt32(data, secondTexture, 2);
        WriteUInt32(data, secondTexture + 4, 4);
        WriteArrayDescriptor(data, secondTexture + 8, 0, 0);
        System.Text.Encoding.UTF8.GetBytes(
            fileName,
            data.AsSpan(md20Base + fileNameOffset));

        WriteArrayDescriptor(data, md20Base + 112, 2, materialOffset);
        WriteUInt16(data, md20Base + materialOffset, 0x1234);
        WriteUInt16(data, md20Base + materialOffset + 2, 2);
        WriteUInt16(data, md20Base + materialOffset + 4, 0x8000);
        WriteUInt16(data, md20Base + materialOffset + 6, 7);

        WriteArrayDescriptor(data, md20Base + 120, 1, boneComboOffset);
        WriteUInt16(data, md20Base + boneComboOffset, 23);

        WriteArrayDescriptor(data, md20Base + 128, 3, textureComboOffset);
        WriteUInt16(data, md20Base + textureComboOffset, 1);
        WriteUInt16(data, md20Base + textureComboOffset + 2, 0);
        WriteUInt16(data, md20Base + textureComboOffset + 4, 1);
        WriteArrayDescriptor(
            data,
            md20Base + 136,
            2,
            transformBoneMapOffset);
        WriteUInt16(data, md20Base + transformBoneMapOffset, 4);
        WriteUInt16(data, md20Base + transformBoneMapOffset + 2, 5);
        WriteArrayDescriptor(data, md20Base + 144, 1, weightComboOffset);
        WriteUInt16(data, md20Base + weightComboOffset, 6);
        WriteArrayDescriptor(data, md20Base + 152, 2, transformComboOffset);
        WriteUInt16(data, md20Base + transformComboOffset, 7);
        WriteUInt16(data, md20Base + transformComboOffset + 2, 8);

        var txidOffset = 8 + md21Size;
        WriteUInt32(data, txidOffset, 0x44495854);
        WriteUInt32(data, txidOffset + 4, 8);
        WriteUInt32(data, txidOffset + 8, 1001);
        WriteUInt32(data, txidOffset + 12, 1002);

        var geometry = WowM2GeometryReader.Read(data);

        Assert.NotNull(geometry);
        Assert.Equal(
            new WowM2Texture(0, 3, fileName, 1001),
            geometry.Textures[0]);
        Assert.Equal(
            new WowM2Texture(2, 4, string.Empty, 1002),
            geometry.Textures[1]);
        Assert.Equal(
            [
                new WowM2Material(0x1234, 2),
                new WowM2Material(0x8000, 7)
            ],
            geometry.Materials);
        Assert.Equal([23], geometry.BoneCombos);
        Assert.Equal([1, 0, 1], geometry.TextureCombos);
        Assert.Equal(0u, geometry.TextureTransformCount);
        Assert.Equal([4, 5], geometry.TextureTransformAnimationStateLookup);
        Assert.Equal([6], geometry.TextureWeightCombos);
        Assert.Equal([7, 8], geometry.TextureTransformCombos);
    }

    [Fact]
    public void ReadsNativeSixtyByteTextureTransformTracks()
    {
        const int md20Base = 8;
        const int transformOffset = 304;
        const int translationTimestampSequencesOffset = 364;
        const int translationValueSequencesOffset = 372;
        const int translationTimestampsOffset = 380;
        const int translationValuesOffset = 388;
        const int rotationTimestampSequencesOffset = 412;
        const int rotationValueSequencesOffset = 420;
        const int rotationTimestampsOffset = 428;
        const int rotationValuesOffset = 436;
        const int scaleTimestampSequencesOffset = 468;
        const int scaleValueSequencesOffset = 476;
        const int scaleTimestampsOffset = 484;
        const int scaleValuesOffset = 492;
        const int md21Size = 516;
        var data = new byte[md20Base + md21Size];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteArrayDescriptor(data, md20Base + 96, 1, transformOffset);

        var transform = md20Base + transformOffset;
        WriteTrackHeader(
            data,
            transform,
            interpolationType: 1,
            translationTimestampSequencesOffset,
            translationValueSequencesOffset);
        WriteTrackHeader(
            data,
            transform + 20,
            interpolationType: 2,
            rotationTimestampSequencesOffset,
            rotationValueSequencesOffset);
        WriteTrackHeader(
            data,
            transform + 40,
            interpolationType: 1,
            scaleTimestampSequencesOffset,
            scaleValueSequencesOffset);

        WriteTrackSequence(
            data,
            md20Base,
            translationTimestampSequencesOffset,
            translationValueSequencesOffset,
            translationTimestampsOffset,
            translationValuesOffset);
        WriteVector3(data, md20Base + translationValuesOffset, new Vector3(1, 2, 3));
        WriteVector3(data, md20Base + translationValuesOffset + 12, new Vector3(4, 5, 6));

        WriteTrackSequence(
            data,
            md20Base,
            rotationTimestampSequencesOffset,
            rotationValueSequencesOffset,
            rotationTimestampsOffset,
            rotationValuesOffset);
        WriteQuaternion(data, md20Base + rotationValuesOffset, Quaternion.Identity);
        WriteQuaternion(
            data,
            md20Base + rotationValuesOffset + 16,
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2));

        WriteTrackSequence(
            data,
            md20Base,
            scaleTimestampSequencesOffset,
            scaleValueSequencesOffset,
            scaleTimestampsOffset,
            scaleValuesOffset);
        WriteVector3(data, md20Base + scaleValuesOffset, Vector3.One);
        WriteVector3(data, md20Base + scaleValuesOffset + 12, new Vector3(2, 3, 4));

        var geometry = WowM2GeometryReader.Read(data);

        Assert.NotNull(geometry);
        var parsed = Assert.Single(geometry.TextureTransforms);
        var translation = Assert.IsType<WowModelAnimationTrack<Vector3>>(parsed.Translation);
        var rotation = Assert.IsType<WowModelAnimationTrack<Quaternion>>(parsed.Rotation);
        var scale = Assert.IsType<WowModelAnimationTrack<Vector3>>(parsed.Scale);
        Assert.Equal((ushort)1, translation.InterpolationType);
        Assert.Equal((ushort)2, rotation.InterpolationType);
        Assert.Equal((ushort)1, scale.InterpolationType);
        Assert.Equal((short)-1, rotation.GlobalSequenceIndex);
        Assert.Equal([0u, 1000u], Assert.Single(rotation.Sequences).TimestampsMilliseconds);
        Assert.Equal(2, Assert.Single(rotation.Sequences).Keys.Count);
        Assert.Equal(Quaternion.Identity, Assert.Single(rotation.Sequences).Keys[0].Value);
        Assert.Equal(new Vector3(4, 5, 6), Assert.Single(translation.Sequences).Keys[1].Value);
        Assert.Equal(new Vector3(2, 3, 4), Assert.Single(scale.Sequences).Keys[1].Value);
    }

    [Fact]
    public void ReadsNativeFortyByteColorAndFixed16AlphaTracks()
    {
        const int md20Base = 8;
        const int colorOffset = 304;
        const int rgbTimestampSequencesOffset = 344;
        const int rgbValueSequencesOffset = 352;
        const int rgbTimestampsOffset = 360;
        const int rgbValuesOffset = 368;
        const int alphaTimestampSequencesOffset = 392;
        const int alphaValueSequencesOffset = 400;
        const int alphaTimestampsOffset = 408;
        const int alphaValuesOffset = 416;
        const int md21Size = 428;
        var data = new byte[md20Base + md21Size];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteArrayDescriptor(data, md20Base + 72, 1, colorOffset);

        var color = md20Base + colorOffset;
        WriteTrackHeader(
            data,
            color,
            interpolationType: 1,
            rgbTimestampSequencesOffset,
            rgbValueSequencesOffset);
        WriteTrackHeader(
            data,
            color + 20,
            interpolationType: 3,
            alphaTimestampSequencesOffset,
            alphaValueSequencesOffset);
        WriteTrackSequence(
            data,
            md20Base,
            rgbTimestampSequencesOffset,
            rgbValueSequencesOffset,
            rgbTimestampsOffset,
            rgbValuesOffset);
        WriteVector3(data, md20Base + rgbValuesOffset, new Vector3(1, 2, 3));
        WriteVector3(data, md20Base + rgbValuesOffset + 12, new Vector3(4, 5, 6));
        WriteTrackSequence(
            data,
            md20Base,
            alphaTimestampSequencesOffset,
            alphaValueSequencesOffset,
            alphaTimestampsOffset,
            alphaValuesOffset);
        WriteInt16(data, md20Base + alphaValuesOffset, 16384);
        WriteInt16(data, md20Base + alphaValuesOffset + 2, 32767);

        var geometry = WowM2GeometryReader.Read(data);

        Assert.NotNull(geometry);
        var parsed = Assert.Single(geometry.Colors);
        var rgb = Assert.IsType<WowModelAnimationTrack<Vector3>>(parsed.Rgb);
        var alpha = Assert.IsType<WowModelAnimationTrack<float>>(parsed.Alpha);
        Assert.Equal((ushort)1, rgb.InterpolationType);
        Assert.Equal(new Vector3(4, 5, 6), Assert.Single(rgb.Sequences).Keys[1].Value);
        Assert.Equal((ushort)3, alpha.InterpolationType);
        Assert.Equal(16384 * 0.000030518509f, Assert.Single(alpha.Sequences).Keys[0].Value);
        Assert.Equal(1f, Assert.Single(alpha.Sequences).Keys[1].Value);
        Assert.True(WowModelAnimationTrackSampler.TrySampleFixed16Normalized(
            alpha,
            0,
            500,
            0,
            [],
            out var halfTime));
        Assert.Equal((16384 * 0.000030518509f + 1f) * .5f, halfTime, 6);
    }

    [Fact]
    public void ReadsNativeTwentyByteFixed16TextureWeightTrack()
    {
        const int md20Base = 8;
        const int weightOffset = 304;
        const int timestampSequencesOffset = 324;
        const int valueSequencesOffset = 332;
        const int timestampsOffset = 340;
        const int valuesOffset = 348;
        const int md21Size = 360;
        var data = new byte[md20Base + md21Size];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteArrayDescriptor(data, md20Base + 88, 1, weightOffset);
        WriteTrackHeader(
            data,
            md20Base + weightOffset,
            interpolationType: 2,
            timestampSequencesOffset,
            valueSequencesOffset);
        WriteTrackSequence(
            data,
            md20Base,
            timestampSequencesOffset,
            valueSequencesOffset,
            timestampsOffset,
            valuesOffset);
        WriteInt16(data, md20Base + valuesOffset, -16384);
        WriteInt16(data, md20Base + valuesOffset + 2, 32767);

        var geometry = WowM2GeometryReader.Read(data);

        Assert.NotNull(geometry);
        var weight = Assert.IsType<WowModelAnimationTrack<float>>(
            Assert.Single(geometry.TextureWeights).Value);
        Assert.Equal((ushort)2, weight.InterpolationType);
        Assert.Equal(-16384 * 0.000030518509f, Assert.Single(weight.Sequences).Keys[0].Value);
        Assert.Equal(1f, Assert.Single(weight.Sequences).Keys[1].Value);
        Assert.True(WowModelAnimationTrackSampler.TrySampleFixed16Normalized(
            weight,
            0,
            250,
            0,
            [],
            out var quarterTime));
        Assert.Equal(
            -16384 * 0.000030518509f * .75f + .25f,
            quarterTime,
            6);
    }

    [Fact]
    public void ReadsNativeSequenceRecordsAndAttachmentCountFromMd21()
    {
        const int md20Base = 8;
        const int sequenceOffset = 288;
        var data = new byte[md20Base + sequenceOffset + 128];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, (uint)(data.Length - 8));
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteUInt32(data, md20Base + 28, 2);
        WriteUInt32(data, md20Base + 32, sequenceOffset);
        WriteUInt32(data, md20Base + 240, 3);
        WriteSingle(data, md20Base + 160, -4);
        WriteSingle(data, md20Base + 164, -2);
        WriteSingle(data, md20Base + 168, 1);
        WriteSingle(data, md20Base + 172, 8);
        WriteSingle(data, md20Base + 176, 6);
        WriteSingle(data, md20Base + 180, 9);
        WriteSingle(data, md20Base + 188, -3);
        WriteSingle(data, md20Base + 192, -1);
        WriteSingle(data, md20Base + 196, 2);
        WriteSingle(data, md20Base + 200, 7);
        WriteSingle(data, md20Base + 204, 5);
        WriteSingle(data, md20Base + 208, 8);
        WriteUInt32(data, md20Base + 216, 3);
        WriteUInt32(data, md20Base + 224, 4);

        WriteSequence(
            data,
            md20Base + sequenceOffset,
            animationId: 42,
            variation: 3,
            duration: 750,
            flags: 0x20,
            frequency: 17,
            variationNext: 1,
            aliasNext: -1,
            minimumRepetitions: 2,
            maximumRepetitions: 5,
            blendTimeMilliseconds: 0x00960019);
        WriteSingle(data, md20Base + sequenceOffset + 32, -2);
        WriteSingle(data, md20Base + sequenceOffset + 36, 1);
        WriteSingle(data, md20Base + sequenceOffset + 40, 3);
        WriteSingle(data, md20Base + sequenceOffset + 44, 4);
        WriteSingle(data, md20Base + sequenceOffset + 48, 7);
        WriteSingle(data, md20Base + sequenceOffset + 52, 11);
        WriteSequence(
            data,
            md20Base + sequenceOffset + 64,
            animationId: 147,
            variation: 0,
            duration: 1250,
            flags: 0,
            frequency: 5,
            variationNext: -1,
            aliasNext: 0);

        var metadata = WowM2MetadataReader.Read(data);

        Assert.NotNull(metadata);
        Assert.Equal(3, metadata.AttachmentCount);
        Assert.Equal(new System.Numerics.Vector3(-4, -2, 1), metadata.BoundingBoxMinimum);
        Assert.Equal(new System.Numerics.Vector3(8, 6, 9), metadata.BoundingBoxMaximum);
        Assert.Equal(new Vector3(-3, -1, 2), metadata.CollisionBoundingBoxMinimum);
        Assert.Equal(new Vector3(7, 5, 8), metadata.CollisionBoundingBoxMaximum);
        Assert.True(metadata.HasCollisionGeometry);
        Assert.Equal(2, metadata.Sequences.Count);
        Assert.Equal(
            new WowModelSequenceMetadata(
                42,
                3,
                750,
                0x20,
                17,
                1,
                -1,
                2,
                5,
                0x00960019)
            {
                BoundingBoxMinimum = new Vector3(-2, 1, 3),
                BoundingBoxMaximum = new Vector3(4, 7, 11)
            },
            metadata.Sequences[0]);
        Assert.Equal(0x00960019u, metadata.Sequences[0].BlendInMilliseconds);
        Assert.Equal(
            new WowModelSequenceMetadata(147, 0, 1250, 0, 5, -1, 0)
            {
                BoundingBoxMinimum = Vector3.Zero,
                BoundingBoxMaximum = Vector3.Zero
            },
            metadata.Sequences[1]);
    }

    [Fact]
    public void SkidUsesTheExternalSkeletonSequenceAndAttachmentArrays()
    {
        const uint skeletonFileDataId = 9876;
        const int md20Base = 8;
        const int sequenceOffset = 288;
        const int md21Size = sequenceOffset + 64;
        var data = new byte[8 + md21Size + 12];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteUInt32(data, md20Base + 28, 1);
        WriteUInt32(data, md20Base + 32, sequenceOffset);
        WriteUInt32(data, md20Base + 240, 2);
        WriteSequence(
            data,
            md20Base + sequenceOffset,
            animationId: 1,
            variation: 0,
            duration: 100,
            flags: 0,
            frequency: 1,
            variationNext: -1,
            aliasNext: -1);
        var skidOffset = 8 + md21Size;
        WriteUInt32(data, skidOffset, 0x44494B53);
        WriteUInt32(data, skidOffset + 4, 4);
        WriteUInt32(data, skidOffset + 8, skeletonFileDataId);

        const int sks1DataSize = 28 + 128;
        var skeleton = new byte[8 + sks1DataSize + 16];
        WriteUInt32(skeleton, 0, 0x31534B53);
        WriteUInt32(skeleton, 4, sks1DataSize);
        WriteUInt32(skeleton, 8, 1);
        WriteUInt32(skeleton, 12, 24);
        WriteUInt32(skeleton, 16, 2);
        WriteUInt32(skeleton, 20, 28);
        WriteUInt16(skeleton, 10, 0xC001);
        WriteUInt16(skeleton, 18, 0xC002);
        WriteUInt32(skeleton, 8 + 24, 3600);
        WriteSequence(
            skeleton,
            8 + 28,
            animationId: 42,
            variation: 0,
            duration: 750,
            flags: 0,
            frequency: 5,
            variationNext: 1,
            aliasNext: -1);
        WriteSequence(
            skeleton,
            8 + 28 + 64,
            animationId: 42,
            variation: 1,
            duration: 900,
            flags: 0x40,
            frequency: 7,
            variationNext: -1,
            aliasNext: 0);
        var ska1Offset = 8 + sks1DataSize;
        WriteUInt32(skeleton, ska1Offset, 0x31414B53);
        WriteUInt32(skeleton, ska1Offset + 4, 8);
        WriteUInt32(skeleton, ska1Offset + 8, 6);
        WriteUInt16(skeleton, ska1Offset + 10, 0xC003);

        uint requestedFileDataId = 0;
        var metadata = WowM2MetadataReader.Read(
            data,
            fileDataId =>
            {
                requestedFileDataId = fileDataId;
                return skeleton;
            });

        Assert.NotNull(metadata);
        Assert.Equal(skeletonFileDataId, requestedFileDataId);
        Assert.Equal(6, metadata.AttachmentCount);
        Assert.Equal([3600u], metadata.GlobalSequenceDurationsMilliseconds);
        Assert.Equal(2, metadata.Sequences.Count);
        Assert.DoesNotContain(metadata.Sequences, sequence => sequence.AnimationId == 1);
        Assert.Equal(
            new WowModelSequenceMetadata(42, 0, 750, 0, 5, 1, -1)
            {
                BoundingBoxMinimum = Vector3.Zero,
                BoundingBoxMaximum = Vector3.Zero
            },
            metadata.Sequences[0]);
        Assert.Equal(
            new WowModelSequenceMetadata(42, 1, 900, 0x40, 7, -1, 0)
            {
                BoundingBoxMinimum = Vector3.Zero,
                BoundingBoxMaximum = Vector3.Zero
            },
            metadata.Sequences[1]);
    }

    [Fact]
    public void ReadsTheNativeModelCameraAndLookupArrays()
    {
        const int md20Base = 8;
        const int cameraOffset = 320;
        const int cameraSize = 116;
        var data = new byte[md20Base + cameraOffset + cameraSize];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, (uint)(data.Length - 8));
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteUInt32(data, md20Base + 272, 1);
        WriteUInt32(data, md20Base + 276, cameraOffset);
        WriteUInt32(data, md20Base + 280, 2);
        WriteUInt32(data, md20Base + 284, 300);
        WriteUInt16(data, md20Base + 300, 0);
        WriteUInt16(data, md20Base + 302, ushort.MaxValue);
        WriteSingle(data, md20Base + cameraOffset + 32, 10.5f);
        WriteSingle(data, md20Base + cameraOffset + 36, -2.25f);
        WriteSingle(data, md20Base + cameraOffset + 40, 7.75f);
        WriteSingle(data, md20Base + cameraOffset + 64, 1.5f);
        WriteSingle(data, md20Base + cameraOffset + 68, 2.5f);
        WriteSingle(data, md20Base + cameraOffset + 72, 3.5f);

        var metadata = WowM2MetadataReader.Read(data);

        Assert.NotNull(metadata);
        var camera = Assert.Single(metadata.Cameras);
        Assert.Equal(new System.Numerics.Vector3(10.5f, -2.25f, 7.75f), camera.Position);
        Assert.Equal(new System.Numerics.Vector3(1.5f, 2.5f, 3.5f), camera.Target);
        Assert.Equal([0, ushort.MaxValue], metadata.CameraLookupIndices);
    }

    [Fact]
    public void ReadsNativeCameraTracksClipPlanesAndGlobalSequenceDurations()
    {
        const int md20Base = 8;
        const int globalSequenceOffset = 300;
        const int cameraOffset = 320;
        const int timestampSequencesOffset = 500;
        const int valueSequencesOffset = 508;
        const int timestampsOffset = 516;
        const int valuesOffset = 524;
        var data = new byte[md20Base + valuesOffset + 24];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, (uint)(data.Length - 8));
        WriteUInt32(data, md20Base, 0x3032444D);
        WriteUInt32(data, md20Base + 4, 274);
        WriteUInt32(data, md20Base + 20, 1);
        WriteUInt32(data, md20Base + 24, globalSequenceOffset);
        WriteUInt32(data, md20Base + globalSequenceOffset, 2400);
        WriteUInt32(data, md20Base + 272, 1);
        WriteUInt32(data, md20Base + 276, cameraOffset);

        var camera = md20Base + cameraOffset;
        WriteInt32(data, camera, -1);
        WriteSingle(data, camera + 4, 5000);
        WriteSingle(data, camera + 8, 0.5f);
        WriteUInt16(data, camera + 12, 1);
        WriteInt16(data, camera + 14, -1);
        WriteUInt32(data, camera + 16, 1);
        WriteUInt32(data, camera + 20, timestampSequencesOffset);
        WriteUInt32(data, camera + 24, 1);
        WriteUInt32(data, camera + 28, valueSequencesOffset);
        WriteUInt32(data, md20Base + timestampSequencesOffset, 2);
        WriteUInt32(
            data,
            md20Base + timestampSequencesOffset + 4,
            timestampsOffset);
        WriteUInt32(data, md20Base + valueSequencesOffset, 2);
        WriteUInt32(data, md20Base + valueSequencesOffset + 4, valuesOffset);
        WriteUInt32(data, md20Base + timestampsOffset, 0);
        WriteUInt32(data, md20Base + timestampsOffset + 4, 1000);
        WriteSingle(data, md20Base + valuesOffset, 1);
        WriteSingle(data, md20Base + valuesOffset + 4, 2);
        WriteSingle(data, md20Base + valuesOffset + 8, 3);
        WriteSingle(data, md20Base + valuesOffset + 12, 4);
        WriteSingle(data, md20Base + valuesOffset + 16, 5);
        WriteSingle(data, md20Base + valuesOffset + 20, 6);

        var metadata = WowM2MetadataReader.Read(data);

        Assert.NotNull(metadata);
        Assert.Equal([2400u], metadata.GlobalSequenceDurationsMilliseconds);
        var parsedCamera = Assert.Single(metadata.Cameras);
        Assert.Equal(-1, parsedCamera.Type);
        Assert.Equal(5000, parsedCamera.FarClip);
        Assert.Equal(0.5f, parsedCamera.NearClip);
        var track = Assert.IsType<WowModelAnimationTrack<System.Numerics.Vector3>>(
            parsedCamera.PositionTrack);
        Assert.Equal((ushort)1, track.InterpolationType);
        Assert.Equal((short)-1, track.GlobalSequenceIndex);
        var sequence = Assert.Single(track.Sequences);
        Assert.Equal([0u, 1000u], sequence.TimestampsMilliseconds);
        Assert.Equal(
            new System.Numerics.Vector3(1, 2, 3),
            sequence.Keys[0].Value);
        Assert.Equal(
            new System.Numerics.Vector3(4, 5, 6),
            sequence.Keys[1].Value);
    }

    private static void WriteSequence(
        byte[] data,
        int offset,
        ushort animationId,
        ushort variation,
        uint duration,
        uint flags,
        short frequency,
        short variationNext,
        short aliasNext,
        int minimumRepetitions = 0,
        int maximumRepetitions = 0,
        uint blendTimeMilliseconds = 0)
    {
        WriteUInt16(data, offset, animationId);
        WriteUInt16(data, offset + 2, variation);
        WriteUInt32(data, offset + 4, duration);
        WriteUInt32(data, offset + 12, flags);
        WriteInt16(data, offset + 16, frequency);
        WriteInt32(data, offset + 20, minimumRepetitions);
        WriteInt32(data, offset + 24, maximumRepetitions);
        WriteUInt32(data, offset + 28, blendTimeMilliseconds);
        WriteInt16(data, offset + 60, variationNext);
        WriteInt16(data, offset + 62, aliasNext);
    }

    private static byte[] BuildModelWithSkeleton(uint skeletonFileDataId)
    {
        const int md20Base = 8;
        const int md21Size = 304;
        var data = new byte[8 + md21Size + 12];
        WriteUInt32(data, 0, 0x3132444D);
        WriteUInt32(data, 4, md21Size);
        WriteUInt32(data, md20Base, 0x3032444D);
        var skid = 8 + md21Size;
        WriteUInt32(data, skid, 0x44494B53);
        WriteUInt32(data, skid + 4, 4);
        WriteUInt32(data, skid + 8, skeletonFileDataId);
        return data;
    }

    private static byte[] BuildSkeleton(
        IReadOnlyList<uint> boneFlags,
        uint parentFileDataId = 0,
        IReadOnlyList<uint>? boneFileDataIds = null)
    {
        const int sks1DataSize = 24;
        var skb1DataSize = 16 + boneFlags.Count * 88;
        var skb1 = 8 + sks1DataSize;
        var skpd = skb1 + 8 + skb1DataSize;
        var skpdSize = parentFileDataId == 0 ? 0 : 20;
        var bfid = skpd + skpdSize;
        var bfidSize = boneFileDataIds is { Count: > 0 }
            ? 8 + boneFileDataIds.Count * sizeof(uint)
            : 0;
        var data = new byte[bfid + bfidSize];

        WriteUInt32(data, 0, 0x31534B53);
        WriteUInt32(data, 4, sks1DataSize);
        WriteUInt32(data, skb1, 0x31424B53);
        WriteUInt32(data, skb1 + 4, (uint)skb1DataSize);
        var skb1Data = skb1 + 8;
        WriteArrayDescriptor(data, skb1Data, (uint)boneFlags.Count, 16);
        for (var index = 0; index < boneFlags.Count; index++)
        {
            var bone = skb1Data + 16 + index * 88;
            WriteUInt32(data, bone + 4, boneFlags[index]);
            WriteInt16(data, bone + 8, -1);
        }

        if (parentFileDataId != 0)
        {
            WriteUInt32(data, skpd, 0x44504B53);
            WriteUInt32(data, skpd + 4, 12);
            WriteUInt32(data, skpd + 16, parentFileDataId);
        }

        if (boneFileDataIds is { Count: > 0 })
        {
            WriteUInt32(data, bfid, 0x44494642);
            WriteUInt32(data, bfid + 4, (uint)(boneFileDataIds.Count * sizeof(uint)));
            for (var index = 0; index < boneFileDataIds.Count; index++)
                WriteUInt32(data, bfid + 8 + index * sizeof(uint), boneFileDataIds[index]);
        }

        return data;
    }

    private static byte[] BuildBoneFile(
        IReadOnlyList<ushort> globalBoneIds,
        IReadOnlyList<Matrix4x4> matrices)
    {
        var bida = sizeof(uint);
        var bomt = bida + 8 + globalBoneIds.Count * sizeof(ushort);
        var data = new byte[bomt + 8 + matrices.Count * 64];
        WriteUInt32(data, 0, 0x314E4F42);
        WriteUInt32(data, bida, 0x41444942);
        WriteUInt32(data, bida + 4, (uint)(globalBoneIds.Count * sizeof(ushort)));
        for (var index = 0; index < globalBoneIds.Count; index++)
            WriteUInt16(data, bida + 8 + index * sizeof(ushort), globalBoneIds[index]);
        WriteUInt32(data, bomt, 0x544D4F42);
        WriteUInt32(data, bomt + 4, (uint)(matrices.Count * 64));
        for (var index = 0; index < matrices.Count; index++)
            WriteMatrix4x4(data, bomt + 8 + index * 64, matrices[index]);
        return data;
    }

    private static void WriteVertex(
        byte[] data,
        int offset,
        System.Numerics.Vector3 position,
        uint packedBoneWeights,
        uint packedBoneIndices,
        System.Numerics.Vector3 normal,
        System.Numerics.Vector2 textureCoordinate0,
        System.Numerics.Vector2 textureCoordinate1)
    {
        WriteVector3(data, offset, position);
        WriteUInt32(data, offset + 12, packedBoneWeights);
        WriteUInt32(data, offset + 16, packedBoneIndices);
        WriteVector3(data, offset + 20, normal);
        WriteSingle(data, offset + 32, textureCoordinate0.X);
        WriteSingle(data, offset + 36, textureCoordinate0.Y);
        WriteSingle(data, offset + 40, textureCoordinate1.X);
        WriteSingle(data, offset + 44, textureCoordinate1.Y);
    }

    private static void WriteArrayDescriptor(
        byte[] data,
        int offset,
        uint count,
        uint arrayOffset)
    {
        WriteUInt32(data, offset, count);
        WriteUInt32(data, offset + 4, arrayOffset);
    }

    private static void WriteTrackHeader(
        byte[] data,
        int offset,
        ushort interpolationType,
        uint timestampSequencesOffset,
        uint valueSequencesOffset)
    {
        WriteUInt16(data, offset, interpolationType);
        WriteInt16(data, offset + 2, -1);
        WriteArrayDescriptor(data, offset + 4, 1, timestampSequencesOffset);
        WriteArrayDescriptor(data, offset + 12, 1, valueSequencesOffset);
    }

    private static void WriteTrackSequence(
        byte[] data,
        int md20Base,
        uint timestampSequencesOffset,
        uint valueSequencesOffset,
        uint timestampsOffset,
        uint valuesOffset)
    {
        WriteArrayDescriptor(
            data,
            checked(md20Base + (int)timestampSequencesOffset),
            2,
            timestampsOffset);
        WriteArrayDescriptor(
            data,
            checked(md20Base + (int)valueSequencesOffset),
            2,
            valuesOffset);
        WriteUInt32(data, checked(md20Base + (int)timestampsOffset), 0);
        WriteUInt32(data, checked(md20Base + (int)timestampsOffset + 4), 1000);
    }

    private static void WriteVector3(
        byte[] data,
        int offset,
        System.Numerics.Vector3 value)
    {
        WriteSingle(data, offset, value.X);
        WriteSingle(data, offset + 4, value.Y);
        WriteSingle(data, offset + 8, value.Z);
    }

    private static void WriteQuaternion(
        byte[] data,
        int offset,
        Quaternion value)
    {
        WriteSingle(data, offset, value.X);
        WriteSingle(data, offset + 4, value.Y);
        WriteSingle(data, offset + 8, value.Z);
        WriteSingle(data, offset + 12, value.W);
    }

    private static void WriteMatrix4x4(
        byte[] data,
        int offset,
        Matrix4x4 value)
    {
        WriteSingle(data, offset, value.M11);
        WriteSingle(data, offset + 4, value.M12);
        WriteSingle(data, offset + 8, value.M13);
        WriteSingle(data, offset + 12, value.M14);
        WriteSingle(data, offset + 16, value.M21);
        WriteSingle(data, offset + 20, value.M22);
        WriteSingle(data, offset + 24, value.M23);
        WriteSingle(data, offset + 28, value.M24);
        WriteSingle(data, offset + 32, value.M31);
        WriteSingle(data, offset + 36, value.M32);
        WriteSingle(data, offset + 40, value.M33);
        WriteSingle(data, offset + 44, value.M34);
        WriteSingle(data, offset + 48, value.M41);
        WriteSingle(data, offset + 52, value.M42);
        WriteSingle(data, offset + 56, value.M43);
        WriteSingle(data, offset + 60, value.M44);
    }

    private static void WriteUInt16(byte[] data, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset, 2), value);

    private static void WriteInt16(byte[] data, int offset, short value) =>
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset, 2), value);

    private static void WriteInt32(byte[] data, int offset, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset, 4), value);

    private static void WriteUInt32(byte[] data, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value);

    private static void WriteSingle(byte[] data, int offset, float value) =>
        WriteUInt32(data, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
}
