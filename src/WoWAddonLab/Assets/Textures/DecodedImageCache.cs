using System.Buffers.Binary;
using System.Security.Cryptography;

namespace WoWAddonLab.Assets;

internal sealed class DecodedImageCache
{
    private const uint Magic = 0x4D494541; // AEIM
    private const int SchemaVersion = 3;
    private const int FileHeaderSize = 12;
    private const int LevelHeaderSize = 12;
    private const long MaximumCacheBytes = 2L * 1024 * 1024 * 1024;
    private readonly string _directory;
    private readonly object _sync = new();
    private int _writesSincePrune;

    public DecodedImageCache(string directory)
    {
        _directory = Path.GetFullPath(directory);
    }

    public bool TryRead(
        ReadOnlySpan<byte> encodedImage,
        out DecodedTextureImage image)
    {
        var path = CachePath(encodedImage);
        lock (_sync)
        {
            try
            {
                if (!File.Exists(path))
                {
                    image = null!;
                    return false;
                }

                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024,
                    FileOptions.SequentialScan);
                Span<byte> header = stackalloc byte[FileHeaderSize];
                stream.ReadExactly(header);
                if (BinaryPrimitives.ReadUInt32LittleEndian(header) != Magic ||
                    BinaryPrimitives.ReadInt32LittleEndian(header[4..]) != SchemaVersion)
                {
                    DeleteInvalid(path);
                    image = null!;
                    return false;
                }

                var mipCount = BinaryPrimitives.ReadInt32LittleEndian(header[8..]);
                if (mipCount is <= 0 or > 16)
                {
                    DeleteInvalid(path);
                    image = null!;
                    return false;
                }

                var levels = new DecodedTextureMipLevel[mipCount];
                Span<byte> levelHeader = stackalloc byte[LevelHeaderSize];
                for (var level = 0; level < mipCount; level++)
                {
                    stream.ReadExactly(levelHeader);
                    var width = BinaryPrimitives.ReadInt32LittleEndian(levelHeader);
                    var height = BinaryPrimitives.ReadInt32LittleEndian(levelHeader[4..]);
                    var byteCount = BinaryPrimitives.ReadInt32LittleEndian(levelHeader[8..]);
                    if (width <= 0 ||
                        height <= 0 ||
                        byteCount <= 0 ||
                        (long)width * height * 4 != byteCount ||
                        byteCount > stream.Length - stream.Position)
                    {
                        DeleteInvalid(path);
                        image = null!;
                        return false;
                    }

                    var pixels = new byte[byteCount];
                    stream.ReadExactly(pixels);
                    levels[level] = new DecodedTextureMipLevel(pixels, width, height);
                }
                if (stream.Position != stream.Length)
                {
                    DeleteInvalid(path);
                    image = null!;
                    return false;
                }

                image = new DecodedTextureImage(levels);
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                return true;
            }
            catch
            {
                DeleteInvalid(path);
                image = null!;
                return false;
            }
        }
    }

    public void Write(
        ReadOnlySpan<byte> encodedImage,
        DecodedTextureImage image)
    {
        if (!IsValid(image))
            return;

        var path = CachePath(encodedImage);
        lock (_sync)
        {
            try
            {
                if (File.Exists(path))
                    return;

                var parent = Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(parent);
                var temporary = path + $".{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
                try
                {
                    using (var stream = new FileStream(
                               temporary,
                               FileMode.CreateNew,
                               FileAccess.Write,
                               FileShare.None,
                               64 * 1024,
                               FileOptions.SequentialScan))
                    {
                        Span<byte> header = stackalloc byte[FileHeaderSize];
                        BinaryPrimitives.WriteUInt32LittleEndian(header, Magic);
                        BinaryPrimitives.WriteInt32LittleEndian(header[4..], SchemaVersion);
                        BinaryPrimitives.WriteInt32LittleEndian(header[8..], image.MipLevels.Count);
                        stream.Write(header);
                        Span<byte> levelHeader = stackalloc byte[LevelHeaderSize];
                        foreach (var level in image.MipLevels)
                        {
                            BinaryPrimitives.WriteInt32LittleEndian(levelHeader, level.Width);
                            BinaryPrimitives.WriteInt32LittleEndian(levelHeader[4..], level.Height);
                            BinaryPrimitives.WriteInt32LittleEndian(levelHeader[8..], level.Pixels.Length);
                            stream.Write(levelHeader);
                            stream.Write(level.Pixels);
                        }
                    }
                    File.Move(temporary, path, false);
                }
                finally
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }

                if (++_writesSincePrune >= 128)
                {
                    _writesSincePrune = 0;
                    PruneIfNeeded();
                }
            }
            catch
            {
            }
        }
    }

    private string CachePath(ReadOnlySpan<byte> encodedImage)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(encodedImage));
        return Path.Combine(_directory, hash[..2], hash + ".rgba");
    }

    private static bool IsValid(DecodedTextureImage image) =>
        image.MipLevels.Count is > 0 and <= 16 &&
        image.MipLevels.All(level =>
            level.Width > 0 &&
            level.Height > 0 &&
            level.Pixels.LongLength == (long)level.Width * level.Height * 4);

    private void PruneIfNeeded()
    {
        var root = new DirectoryInfo(_directory);
        if (!root.Exists)
            return;

        var files = root.EnumerateFiles("*.rgba", SearchOption.AllDirectories)
            .OrderBy(value => value.LastWriteTimeUtc)
            .ToArray();
        var total = files.Sum(value => value.Length);
        foreach (var file in files)
        {
            if (total <= MaximumCacheBytes)
                break;
            total -= file.Length;
            file.Delete();
        }
    }

    private static void DeleteInvalid(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
