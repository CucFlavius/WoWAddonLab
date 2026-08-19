using System.Text;
using System.Collections.Concurrent;

namespace WoWAddonLab.Assets;

internal static class TactCatalogCache
{
    private const uint Magic = 0x434C4157;
    private static readonly ConcurrentDictionary<string, byte> PrunedIdentities =
        new(StringComparer.OrdinalIgnoreCase);

    public static bool TryRead<T>(
        string? cacheDirectory,
        string identity,
        string catalog,
        int version,
        Func<BinaryReader, T> read,
        out T? value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(cacheDirectory))
            return false;

        PruneSupersededIdentities(cacheDirectory, identity);
        var path = CachePath(cacheDirectory, identity, catalog, version);
        if (!File.Exists(path))
            return false;

        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                FileOptions.SequentialScan);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != version)
                return false;
            value = read(reader);
            return stream.Position == stream.Length;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }
    }

    public static void Write(
        string? cacheDirectory,
        string identity,
        string catalog,
        int version,
        Action<BinaryWriter> write)
    {
        if (string.IsNullOrWhiteSpace(cacheDirectory))
            return;

        var path = CachePath(cacheDirectory, identity, catalog, version);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       1024 * 1024,
                       FileOptions.SequentialScan))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Magic);
                writer.Write(version);
                write(writer);
            }
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception)
        {
            TryDelete(temporaryPath);
        }
    }

    public static string? ReadNullableString(BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadString() : null;

    public static void WriteNullableString(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
            writer.Write(value);
    }

    public static int? ReadNullableInt32(BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadInt32() : null;

    public static void WriteNullableInt32(BinaryWriter writer, int? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
            writer.Write(value.Value);
    }

    public static bool? ReadNullableBoolean(BinaryReader reader) =>
        reader.ReadByte() switch
        {
            0 => null,
            1 => false,
            2 => true,
            _ => throw new InvalidDataException("Invalid nullable Boolean value.")
        };

    public static void WriteNullableBoolean(BinaryWriter writer, bool? value) =>
        writer.Write(value is null ? (byte)0 : value.Value ? (byte)2 : (byte)1);

    public static int ReadCount(BinaryReader reader, int maximum)
    {
        var count = reader.ReadInt32();
        if (count is < 0 || count > maximum)
            throw new InvalidDataException("Invalid catalog entry count.");
        return count;
    }

    private static string CachePath(
        string cacheDirectory,
        string identity,
        string catalog,
        int version)
    {
        var safeIdentity = string.Concat(identity.Select(value =>
            char.IsLetterOrDigit(value) || value is '.' or '-' or '_'
                ? value
                : '_'));
        return Path.Combine(
            cacheDirectory,
            "catalogs",
            safeIdentity,
            $"{catalog}-v{version}.bin");
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

    private static void PruneSupersededIdentities(string cacheDirectory, string identity)
    {
        var marker = identity.LastIndexOf("-dbd", StringComparison.OrdinalIgnoreCase);
        if (marker <= 0 || !PrunedIdentities.TryAdd(identity, 0))
            return;

        var root = Path.Combine(cacheDirectory, "catalogs");
        if (!Directory.Exists(root))
            return;
        var productBuild = identity[..marker];
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(directory);
            if (name.Equals(identity, StringComparison.OrdinalIgnoreCase) ||
                !name.Equals(productBuild, StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith(productBuild + "-dbd", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (Exception)
            {
            }
        }
    }
}
