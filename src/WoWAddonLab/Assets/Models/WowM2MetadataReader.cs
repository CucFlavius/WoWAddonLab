using System.Buffers.Binary;
using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal static class WowM2MetadataReader
{
    private const uint Md20 = 0x3032444D;
    private const uint Md21 = 0x3132444D;
    private const uint Skid = 0x44494B53;
    private const uint Sks1 = 0x31534B53;
    private const uint Ska1 = 0x31414B53;
    private const uint Afid = 0x44494641;
    private const int GlobalSequenceArrayOffset = 20;
    private const int SequenceArrayOffset = 28;
    private const int BoundingBoxMinimumOffset = 160;
    private const int BoundingBoxMaximumOffset = 172;
    private const int CollisionBoundingBoxMinimumOffset = 188;
    private const int CollisionBoundingBoxMaximumOffset = 200;
    private const int CollisionTriangleArrayOffset = 216;
    private const int CollisionVertexArrayOffset = 224;
    private const int AttachmentArrayOffset = 240;
    private const int CameraArrayOffset = 272;
    private const int CameraLookupArrayOffset = 280;
    private const int SequenceSize = 64;
    private const int CameraSize = 116;

    public static WowModelResourceMetadata? Read(
        byte[]? data,
        Func<uint, byte[]?>? readSidecar = null)
    {
        if (data is null || data.Length < 8)
            return null;

        var md20Base = FindMd20Base(data);
        if (md20Base < 0 || !HasRange(data, md20Base, CameraArrayOffset + 8))
            return null;

        uint globalSequenceCount = ReadUInt16(
            data,
            md20Base + GlobalSequenceArrayOffset);
        var globalSequenceOffset = ReadUInt32(
            data,
            md20Base + GlobalSequenceArrayOffset + 4);
        uint sequenceCount = ReadUInt16(data, md20Base + SequenceArrayOffset);
        var sequenceOffset = ReadUInt32(data, md20Base + SequenceArrayOffset + 4);
        uint attachmentCount = ReadUInt16(data, md20Base + AttachmentArrayOffset);
        uint cameraCount = ReadUInt16(data, md20Base + CameraArrayOffset);
        var cameraOffset = ReadUInt32(data, md20Base + CameraArrayOffset + 4);
        uint cameraLookupCount = 0;
        uint cameraLookupOffset = 0;
        if (HasRange(data, md20Base, CameraLookupArrayOffset + 8))
        {
            cameraLookupCount = ReadUInt16(
                data,
                md20Base + CameraLookupArrayOffset);
            cameraLookupOffset = ReadUInt32(
                data,
                md20Base + CameraLookupArrayOffset + 4);
        }
        if (!TryResolveArrayRange(
                data,
                md20Base,
                globalSequenceCount,
                globalSequenceOffset,
                sizeof(uint),
                out var globalSequenceStart))
        {
            return null;
        }
        var globalSequenceDurations = new List<uint>((int)globalSequenceCount);
        for (var index = 0; index < globalSequenceCount; index++)
        {
            globalSequenceDurations.Add(
                ReadUInt32(data, checked(globalSequenceStart + (int)index * 4)));
        }

        var sequenceStart = (long)md20Base + sequenceOffset;
        var sequenceBytes = (long)sequenceCount * SequenceSize;
        if (sequenceStart < 0 ||
            sequenceStart + sequenceBytes > data.LongLength)
        {
            return null;
        }

        var sequences = new List<WowModelSequenceMetadata>((int)sequenceCount);
        for (var index = 0; index < sequenceCount; index++)
        {
            var offset = checked((int)(sequenceStart + index * SequenceSize));
            sequences.Add(ReadSequence(data, offset));
        }

        var cameraStart = (long)md20Base + cameraOffset;
        var cameraBytes = (long)cameraCount * CameraSize;
        if (cameraStart < 0 || cameraStart + cameraBytes > data.LongLength)
            return null;
        var cameras = new List<WowModelCameraMetadata>((int)cameraCount);
        for (var index = 0; index < cameraCount; index++)
        {
            var offset = checked((int)(cameraStart + index * CameraSize));
            if (!TryReadAnimationTrack(
                    data,
                    md20Base,
                    offset + 12,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? positionTrack) ||
                !TryReadAnimationTrack(
                    data,
                    md20Base,
                    offset + 44,
                    12,
                    ReadVector3,
                    out WowModelAnimationTrack<Vector3>? targetTrack) ||
                !TryReadAnimationTrack(
                    data,
                    md20Base,
                    offset + 76,
                    4,
                    ReadSingle,
                    out WowModelAnimationTrack<float>? rollTrack) ||
                !TryReadAnimationTrack(
                    data,
                    md20Base,
                    offset + 96,
                    4,
                    ReadSingle,
                    out WowModelAnimationTrack<float>? fieldOfViewTrack))
            {
                return null;
            }
            cameras.Add(new WowModelCameraMetadata(
                ReadVector3(data, offset + 32),
                ReadVector3(data, offset + 64),
                positionTrack,
                targetTrack,
                rollTrack,
                fieldOfViewTrack,
                ReadInt32(data, offset),
                ReadSingle(data, offset + 4),
                ReadSingle(data, offset + 8)));
        }

        if (!TryResolveArrayRange(
                data,
                md20Base,
                cameraLookupCount,
                cameraLookupOffset,
                sizeof(ushort),
                out var cameraLookupStart))
        {
            return null;
        }
        var cameraLookupIndices = new List<ushort>((int)cameraLookupCount);
        for (var index = 0; index < cameraLookupCount; index++)
        {
            cameraLookupIndices.Add(
                ReadUInt16(
                    data,
                    checked(cameraLookupStart + (int)index * sizeof(ushort))));
        }

        var animationFiles = ReadAnimationFiles(data);
        var skeletonFileDataId = FindSkeletonFileDataId(data);
        if (skeletonFileDataId is { } id &&
            readSidecar?.Invoke(id) is { } skeletonData &&
            TryReadSkeletonMetadata(
                skeletonData,
                out var skeletonGlobalSequenceDurations,
                out var skeletonSequences,
                out var skeletonAttachmentCount))
        {
            globalSequenceDurations = skeletonGlobalSequenceDurations;
            sequences = skeletonSequences;
            if (skeletonAttachmentCount is { } count)
                attachmentCount = (uint)count;
            var skeletonAnimationFiles = ReadAnimationFiles(skeletonData);
            if (skeletonAnimationFiles.Count > 0)
                animationFiles = skeletonAnimationFiles;
        }

        return new WowModelResourceMetadata(sequences, (int)attachmentCount)
        {
            BoundingBoxMinimum = ReadVector3(
                data,
                md20Base + BoundingBoxMinimumOffset),
            BoundingBoxMaximum = ReadVector3(
                data,
                md20Base + BoundingBoxMaximumOffset),
            CollisionBoundingBoxMinimum = ReadVector3(
                data,
                md20Base + CollisionBoundingBoxMinimumOffset),
            CollisionBoundingBoxMaximum = ReadVector3(
                data,
                md20Base + CollisionBoundingBoxMaximumOffset),
            HasCollisionGeometry =
                ReadUInt16(data, md20Base + CollisionTriangleArrayOffset) > 0 &&
                ReadUInt16(data, md20Base + CollisionVertexArrayOffset) > 0,
            GlobalSequenceDurationsMilliseconds = globalSequenceDurations,
            Cameras = cameras,
            CameraLookupIndices = cameraLookupIndices,
            AnimationFiles = animationFiles
        };
    }

    internal static IReadOnlyList<WowModelAnimationFileMetadata>
        ReadAnimationFiles(byte[] data)
    {
        var values = new List<WowModelAnimationFileMetadata>();
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
            if (chunkId == Afid)
            {
                if ((chunkSize & 7) != 0)
                    return [];
                for (var entry = chunkData; entry < chunkEnd; entry += 8)
                {
                    var entryOffset = checked((int)entry);
                    values.Add(new WowModelAnimationFileMetadata(
                        ReadUInt16(data, entryOffset),
                        ReadUInt16(data, entryOffset + 2),
                        ReadUInt32(data, entryOffset + 4)));
                }
            }
            offset = chunkEnd;
        }
        return values;
    }

    internal static bool TryReadAnimationTrack<T>(
        byte[] data,
        int md20Base,
        int trackOffset,
        int valueSize,
        Func<byte[], int, T> readValue,
        out WowModelAnimationTrack<T>? track,
        bool hasCubicTangents = true,
        IReadOnlyDictionary<int, byte[]>? sequencePayloads = null)
    {
        track = null;
        if (!HasRange(data, trackOffset, 20))
            return false;

        var interpolationType = ReadUInt16(data, trackOffset);
        var globalSequenceIndex = ReadInt16(data, trackOffset + 2);
        uint timestampSequenceCount = ReadUInt16(data, trackOffset + 4);
        var timestampSequenceOffset = ReadUInt32(data, trackOffset + 8);
        uint valueSequenceCount = ReadUInt16(data, trackOffset + 12);
        var valueSequenceOffset = ReadUInt32(data, trackOffset + 16);
        if (timestampSequenceCount != valueSequenceCount)
        {
            return false;
        }

        if (!TryResolveArrayRange(
                data,
                md20Base,
                timestampSequenceCount,
                timestampSequenceOffset,
                8,
                out var timestampSequencesStart) ||
            !TryResolveArrayRange(
                data,
                md20Base,
                valueSequenceCount,
                valueSequenceOffset,
                8,
                out var valueSequencesStart))
        {
            return false;
        }

        var sequences = new List<WowModelAnimationTrackSequence<T>>(
            (int)timestampSequenceCount);
        for (var sequenceIndex = 0u;
             sequenceIndex < timestampSequenceCount;
             sequenceIndex++)
        {
            var timestampDescriptor = checked(
                timestampSequencesStart + (int)sequenceIndex * 8);
            var valueDescriptor = checked(
                valueSequencesStart + (int)sequenceIndex * 8);
            uint timestampCount = ReadUInt16(data, timestampDescriptor);
            var timestampOffset = ReadUInt32(data, timestampDescriptor + 4);
            uint valueCount = ReadUInt16(data, valueDescriptor);
            var valueOffset = ReadUInt32(data, valueDescriptor + 4);
            var valuesPerKey =
                hasCubicTangents && interpolationType is 2 or 3 ? 3u : 1u;
            if (timestampCount > int.MaxValue ||
                valueCount != (ulong)timestampCount * valuesPerKey ||
                valueCount > int.MaxValue)
            {
                return false;
            }

            var payloadData = data;
            var payloadBase = md20Base;
            if (sequencePayloads?.TryGetValue(
                    checked((int)sequenceIndex),
                    out var externalPayload) == true)
            {
                payloadData = externalPayload;
                payloadBase = 0;
            }

            if (!TryResolveArrayRange(
                    payloadData,
                    payloadBase,
                    timestampCount,
                    timestampOffset,
                    sizeof(uint),
                    out var timestampsStart) ||
                !TryResolveArrayRange(
                    payloadData,
                    payloadBase,
                    valueCount,
                    valueOffset,
                    valueSize,
                    out var valuesStart))
            {
                return false;
            }

            var timestamps = new List<uint>((int)timestampCount);
            var keys = new List<WowModelAnimationTrackKey<T>>((int)timestampCount);
            for (var keyIndex = 0u; keyIndex < timestampCount; keyIndex++)
            {
                timestamps.Add(ReadUInt32(
                    payloadData,
                    checked(timestampsStart + (int)keyIndex * 4)));
                var valueIndex = checked((int)(keyIndex * valuesPerKey));
                var value = readValue(
                    payloadData,
                    checked(valuesStart + valueIndex * valueSize));
                if (valuesPerKey == 1)
                {
                    keys.Add(new WowModelAnimationTrackKey<T>(
                        value,
                        default!,
                        default!));
                    continue;
                }

                keys.Add(new WowModelAnimationTrackKey<T>(
                    value,
                    readValue(
                        payloadData,
                        checked(valuesStart + (valueIndex + 1) * valueSize)),
                    readValue(
                        payloadData,
                        checked(valuesStart + (valueIndex + 2) * valueSize))));
            }
            sequences.Add(new WowModelAnimationTrackSequence<T>(timestamps, keys));
        }

        track = new WowModelAnimationTrack<T>(
            interpolationType,
            globalSequenceIndex,
            sequences);
        return true;
    }

    private static bool TryResolveArrayRange(
        byte[] data,
        int md20Base,
        uint count,
        uint relativeOffset,
        int elementSize,
        out int start)
    {
        start = 0;
        var absoluteStart = (long)md20Base + relativeOffset;
        var byteCount = (long)count * elementSize;
        if (absoluteStart < 0 ||
            absoluteStart > int.MaxValue ||
            byteCount < 0 ||
            absoluteStart + byteCount > data.LongLength)
        {
            return false;
        }

        start = (int)absoluteStart;
        return true;
    }

    internal static uint? FindSkeletonFileDataId(byte[] data)
    {
        if (ReadUInt32(data, 0) == Md20)
            return null;

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
            if (chunkId == Skid && chunkSize >= 4)
                return ReadUInt32(data, checked((int)chunkData));
            offset = chunkEnd;
        }
        return null;
    }

    private static bool TryReadSkeletonMetadata(
        byte[] data,
        out List<uint> globalSequenceDurations,
        out List<WowModelSequenceMetadata> sequences,
        out int? attachmentCount)
    {
        globalSequenceDurations = [];
        sequences = [];
        attachmentCount = null;
        var foundSequences = false;
        long offset = 0;
        while (offset + 8 <= data.LongLength)
        {
            var chunkOffset = checked((int)offset);
            var chunkId = ReadUInt32(data, chunkOffset);
            var chunkSize = ReadUInt32(data, chunkOffset + 4);
            var chunkData = offset + 8;
            var chunkEnd = chunkData + chunkSize;
            if (chunkEnd > data.LongLength)
                return false;

            if (chunkId == Sks1)
            {
                if (chunkSize < 24)
                    return false;
                var globalSequenceCount = ReadUInt16(
                    data,
                    checked((int)chunkData));
                var globalSequenceOffset = ReadUInt32(
                    data,
                    checked((int)chunkData + 4));
                var sequenceCount = ReadUInt16(data, checked((int)chunkData + 8));
                var sequenceOffset = ReadUInt32(data, checked((int)chunkData + 12));
                var sequenceLookupCount = ReadUInt16(
                    data,
                    checked((int)chunkData + 16));
                var sequenceLookupOffset = ReadUInt32(
                    data,
                    checked((int)chunkData + 20));
                var globalSequenceStart = chunkData + globalSequenceOffset;
                var sequenceStart = chunkData + sequenceOffset;
                var sequenceLookupStart = chunkData + sequenceLookupOffset;
                var globalSequenceBytes = (long)globalSequenceCount * sizeof(uint);
                var sequenceBytes = (long)sequenceCount * SequenceSize;
                var sequenceLookupBytes =
                    (long)sequenceLookupCount * sizeof(ushort);
                if (globalSequenceStart < chunkData ||
                    globalSequenceStart + globalSequenceBytes > chunkEnd ||
                    sequenceStart < chunkData ||
                    sequenceStart + sequenceBytes > chunkEnd ||
                    sequenceLookupStart < chunkData ||
                    sequenceLookupStart + sequenceLookupBytes > chunkEnd)
                {
                    return false;
                }

                globalSequenceDurations = new List<uint>(
                    (int)globalSequenceCount);
                for (var index = 0; index < globalSequenceCount; index++)
                {
                    globalSequenceDurations.Add(ReadUInt32(
                        data,
                        checked((int)(globalSequenceStart + index * 4))));
                }
                sequences = new List<WowModelSequenceMetadata>((int)sequenceCount);
                for (var index = 0; index < sequenceCount; index++)
                {
                    var sequenceRecordOffset = checked(
                        (int)(sequenceStart + index * SequenceSize));
                    sequences.Add(ReadSequence(data, sequenceRecordOffset));
                }
                foundSequences = true;
            }
            else if (chunkId == Ska1)
            {
                if (chunkSize < 8)
                    return false;
                attachmentCount = ReadUInt16(data, checked((int)chunkData));
            }

            offset = chunkEnd;
        }
        return foundSequences;
    }

    internal static WowModelSequenceMetadata ReadSequence(byte[] data, int offset) =>
        new(
            ReadUInt16(data, offset),
            ReadUInt16(data, offset + 2),
            ReadUInt32(data, offset + 4),
            ReadUInt32(data, offset + 12),
            ReadUInt32(data, offset + 16),
            ReadInt16(data, offset + 60),
            ReadInt16(data, offset + 62),
            ReadInt32(data, offset + 20),
            ReadInt32(data, offset + 24),
            ReadUInt32(data, offset + 28))
        {
            BoundingBoxMinimum = ReadVector3(data, offset + 32),
            BoundingBoxMaximum = ReadVector3(data, offset + 44)
        };

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
                chunkSize >= AttachmentArrayOffset + 8 &&
                ReadUInt32(data, checked((int)chunkData)) == Md20)
            {
                return checked((int)chunkData);
            }
            offset = chunkEnd;
        }
        return -1;
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

    private static Vector3 ReadVector3(byte[] data, int offset) =>
        new(
            ReadSingle(data, offset),
            ReadSingle(data, offset + 4),
            ReadSingle(data, offset + 8));
}
