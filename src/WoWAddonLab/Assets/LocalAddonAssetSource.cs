namespace WoWAddonLab.Assets;

internal sealed class LocalAddonAssetSource
{
    private const string AddonPrefix = "Interface\\AddOns\\";

    private static readonly EnumerationOptions FileEnumeration = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.None,
        MaxRecursionDepth = 32
    };

    private readonly string[] _roots;
    private readonly Lazy<AssetIndex> _index;

    public LocalAddonAssetSource(IEnumerable<string> addonRoots)
    {
        _roots = addonRoots
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _index = new Lazy<AssetIndex>(
            BuildIndex,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public byte[]? Read(string? asset, bool defaultToBlp = false)
    {
        var path = ResolvePath(asset, defaultToBlp);
        return path is null ? null : File.ReadAllBytes(path);
    }

    internal string? ResolvePath(string? asset, bool defaultToBlp = false)
    {
        if (string.IsNullOrWhiteSpace(asset))
            return null;

        foreach (var candidate in Candidates(asset, defaultToBlp))
        {
            var path = ResolveExactPath(candidate);
            if (path is not null)
                return path;
        }
        return null;
    }

    private static IEnumerable<string> Candidates(string asset, bool defaultToBlp)
    {
        yield return asset;
        if (!defaultToBlp)
            yield break;

        var fallback = WowFileAssetPath.WithDefaultBlpExtension(asset);
        if (!fallback.Equals(asset, StringComparison.OrdinalIgnoreCase))
            yield return fallback;

        fallback = WowFileAssetPath.WithDefaultTgaExtension(asset);
        if (!fallback.Equals(asset, StringComparison.OrdinalIgnoreCase))
            yield return fallback;
    }

    private string? ResolveExactPath(string asset)
    {
        if (Path.IsPathRooted(asset))
            return File.Exists(asset) ? Path.GetFullPath(asset) : null;

        var normalized = asset.Replace('/', '\\').TrimStart('\\');
        var index = _index.Value;
        if (normalized.StartsWith(AddonPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relative = normalized[AddonPrefix.Length..];
            var separator = relative.IndexOf('\\');
            if (separator < 0 ||
                !index.NamedRoots.TryGetValue(relative[..separator], out var addonFiles))
            {
                return null;
            }

            return addonFiles.GetValueOrDefault(relative[(separator + 1)..]);
        }

        return index.Files.GetValueOrDefault(normalized);
    }

    private AssetIndex BuildIndex()
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var namedRoots = new Dictionary<string, Dictionary<string, string>>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var root in _roots)
        {
            var rootFiles = IndexRoot(root);
            foreach (var file in rootFiles)
                files.TryAdd(file.Key, file.Value);
            namedRoots[Path.GetFileName(root.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar))] = rootFiles;
        }
        return new AssetIndex(files, namedRoots);
    }

    private static Dictionary<string, string> IndexRoot(string root)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(root))
            return files;

        foreach (var path in Directory.EnumerateFiles(root, "*", FileEnumeration))
            files.TryAdd(Path.GetRelativePath(root, path).Replace('/', '\\'), path);
        return files;
    }

    private sealed record AssetIndex(
        Dictionary<string, string> Files,
        Dictionary<string, Dictionary<string, string>> NamedRoots);
}
