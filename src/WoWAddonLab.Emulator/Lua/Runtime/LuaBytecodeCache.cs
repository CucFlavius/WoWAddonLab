using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class LuaBytecodeCache
{
    private const uint Magic = 0x43424C57;
    private const int MaximumBytecodeLength = 512 * 1024 * 1024;
    private readonly string _directory;

    public LuaBytecodeCache(string directory)
    {
        var root = Path.GetFullPath(directory);
        _directory = Path.Combine(root, "lua51-v4");
        TryDeleteLegacyCache(Path.Combine(root, "lua51-v1"));
        TryDeleteLegacyCache(Path.Combine(root, "lua51-v2"));
        TryDeleteLegacyCache(Path.Combine(root, "lua51-v3"));
    }

    public CacheKey KeyFor(string sourcePath, byte[] source)
        => KeyForIdentity(Path.GetFullPath(sourcePath).ToUpperInvariant(), source);

    public CacheKey KeyForIdentity(string identity, byte[] source)
    {
        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        var sourceHash = SHA256.HashData(source);
        var name = Convert.ToHexString(pathHash);
        return new CacheKey(
            Path.Combine(_directory, name[..2], $"{name}.luac"),
            sourceHash);
    }

    public bool TryRead(CacheKey key, out byte[] bytecode)
    {
        try
        {
            if (File.Exists(key.Path))
            {
                using var stream = new FileStream(
                    key.Path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    1024 * 1024,
                    FileOptions.SequentialScan);
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                if (reader.ReadUInt32() != Magic)
                {
                    bytecode = [];
                    return false;
                }
                if (!reader.ReadBytes(key.SourceHash.Length).AsSpan().SequenceEqual(key.SourceHash))
                {
                    bytecode = [];
                    return false;
                }

                var compressed = reader.ReadBoolean();
                var length = reader.ReadInt32();
                if (length is <= 0 or > MaximumBytecodeLength)
                    throw new InvalidDataException("Invalid Lua bytecode cache length.");
                bytecode = new byte[length];
                if (compressed)
                {
                    using var compressedStream = new BrotliStream(
                        stream,
                        CompressionMode.Decompress,
                        leaveOpen: false);
                    compressedStream.ReadExactly(bytecode);
                }
                else
                    stream.ReadExactly(bytecode);
                return true;
            }
        }
        catch (Exception)
        {
        }

        bytecode = [];
        return false;
    }

    public void Write(CacheKey key, byte[] bytecode)
    {
        var directory = Path.GetDirectoryName(key.Path)!;
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(key.Path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(directory);
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       1024 * 1024,
                       FileOptions.SequentialScan))
            {
                var compressed = bytecode.Length >= 1024;
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(Magic);
                    writer.Write(key.SourceHash);
                    writer.Write(compressed);
                    writer.Write(bytecode.Length);
                }
                if (compressed)
                {
                    using var compressedStream = new BrotliStream(
                        stream,
                        CompressionLevel.Fastest,
                        leaveOpen: true);
                    compressedStream.Write(bytecode);
                }
                else
                    stream.Write(bytecode);
            }
            File.Move(temporaryPath, key.Path, overwrite: true);
        }
        catch (Exception)
        {
            TryDelete(temporaryPath);
        }
    }

    private static void TryDeleteLegacyCache(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception)
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception)
        {
        }
    }

    internal readonly record struct CacheKey(string Path, byte[] SourceHash);
}
