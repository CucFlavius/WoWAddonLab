using System.Text.Json;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactMacroIconCatalog : IWowMacroIconProvider
{
    private const int CacheSchemaVersion = 2;
    private readonly IReadOnlyDictionary<string, uint> _fileDataIds;

    private TactMacroIconCatalog(
        IReadOnlyList<string> looseSpellIcons,
        IReadOnlyList<string> looseItemIcons,
        IReadOnlyDictionary<string, uint> fileDataIds)
    {
        LooseSpellIcons = looseSpellIcons;
        LooseItemIcons = looseItemIcons;
        _fileDataIds = new Dictionary<string, uint>(
            fileDataIds,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> LooseSpellIcons { get; }
    public IReadOnlyList<string> LooseItemIcons { get; }
    public IReadOnlyList<uint> SpellIcons { get; } = [];
    public IReadOnlyList<uint> ItemIcons { get; } = [];
    public int Count => LooseSpellIcons.Count + LooseItemIcons.Count;

    public uint? ResolveFileDataId(string icon)
    {
        var normalized = icon.Replace('\\', '/');
        const string prefix = "interface/icons/";
        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[prefix.Length..];
        if (normalized.EndsWith(".blp", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^4];
        return _fileDataIds.TryGetValue(normalized, out var fileDataId)
            ? fileDataId
            : null;
    }

    public static TactMacroIconCatalog Load(
        TactAssetSource tact,
        string build,
        string cacheDirectory)
    {
        var listfilePath = tact.ListfilePath ??
                           throw new InvalidOperationException("TACT listfile is unavailable.");
        var listfile = new FileInfo(listfilePath);
        var catalogDirectory = Path.Combine(cacheDirectory, "macro-icons");
        Directory.CreateDirectory(catalogDirectory);
        var cachePath = Path.Combine(catalogDirectory, $"{SafeFileName(build)}.json");

        if (TryReadCache(cachePath, listfile, out var cached))
        {
            return new TactMacroIconCatalog(
                cached.LooseSpellIcons,
                cached.LooseItemIcons,
                cached.FileDataIds);
        }

        var spells = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fileDataIds = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        using (var reader = new StreamReader(listfilePath))
        {
            while (reader.ReadLine() is { } line)
            {
                var separator = line.IndexOf(';');
                if (separator <= 0 ||
                    !uint.TryParse(line.AsSpan(0, separator), out var fileDataId) ||
                    !tact.FileExists(fileDataId))
                    continue;

                var path = line[(separator + 1)..].Replace('\\', '/');
                const string iconPrefix = "interface/icons/";
                if (!path.StartsWith(iconPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = path[iconPrefix.Length..];
                if (fileName.Contains('/') ||
                    !fileName.EndsWith(".blp", StringComparison.OrdinalIgnoreCase))
                    continue;

                var icon = fileName[..^4];
                if (icon.StartsWith("Ability_", StringComparison.OrdinalIgnoreCase) ||
                    icon.StartsWith("Spell_", StringComparison.OrdinalIgnoreCase))
                {
                    spells.Add(icon);
                    fileDataIds[icon] = fileDataId;
                }
                else if (icon.StartsWith("INV_", StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(icon);
                    fileDataIds[icon] = fileDataId;
                }
            }
        }

        var spellIcons = spells.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var itemIcons = items.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        WriteCache(
            cachePath,
            new CacheEntry(
                CacheSchemaVersion,
                listfile.Length,
                listfile.LastWriteTimeUtc.Ticks,
                spellIcons,
                itemIcons,
                fileDataIds));
        return new TactMacroIconCatalog(spellIcons, itemIcons, fileDataIds);
    }

    private static bool TryReadCache(
        string cachePath,
        FileInfo listfile,
        out CacheEntry entry)
    {
        entry = default!;
        if (!File.Exists(cachePath))
            return false;
        try
        {
            var cached = JsonSerializer.Deserialize<CacheEntry>(File.ReadAllBytes(cachePath));
            if (cached is null ||
                cached.SchemaVersion != CacheSchemaVersion ||
                cached.ListfileLength != listfile.Length ||
                cached.ListfileLastWriteUtcTicks != listfile.LastWriteTimeUtc.Ticks ||
                cached.FileDataIds is null)
                return false;
            entry = cached;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteCache(string cachePath, CacheEntry entry)
    {
        var temporaryPath = cachePath + ".tmp";
        File.WriteAllBytes(temporaryPath, JsonSerializer.SerializeToUtf8Bytes(entry));
        File.Move(temporaryPath, cachePath, overwrite: true);
    }

    private static string SafeFileName(string value) =>
        string.Concat(value.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private sealed record CacheEntry(
        int SchemaVersion,
        long ListfileLength,
        long ListfileLastWriteUtcTicks,
        string[] LooseSpellIcons,
        string[] LooseItemIcons,
        Dictionary<string, uint> FileDataIds);
}
