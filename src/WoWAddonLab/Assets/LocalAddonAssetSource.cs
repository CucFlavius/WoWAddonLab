namespace WoWAddonLab.Assets;

internal sealed class LocalAddonAssetSource
{
    private const string AddonPrefix = "Interface\\AddOns\\";
    private readonly string[] _roots;
    private readonly IReadOnlyDictionary<string, string> _namedRoots;

    public LocalAddonAssetSource(IEnumerable<string> addonRoots)
    {
        _roots = addonRoots
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _namedRoots = _roots
            .GroupBy(
                path => Path.GetFileName(path.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last(),
                StringComparer.OrdinalIgnoreCase);
    }

    public byte[]? Read(string? asset, bool defaultToBlp = false)
    {
        if (string.IsNullOrWhiteSpace(asset))
            return null;

        foreach (var candidate in Candidates(asset, defaultToBlp))
        {
            var path = ResolveExactPath(candidate);
            if (path is not null)
                return File.ReadAllBytes(path);
        }
        return null;
    }

    internal string? ResolvePath(string? asset, bool defaultToBlp = false)
    {
        if (string.IsNullOrWhiteSpace(asset))
            return null;
        return Candidates(asset, defaultToBlp)
            .Select(ResolveExactPath)
            .FirstOrDefault(path => path is not null);
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
        if (normalized.StartsWith(AddonPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relative = normalized[AddonPrefix.Length..];
            var separator = relative.IndexOf('\\');
            if (separator < 0 ||
                !_namedRoots.TryGetValue(relative[..separator], out var root))
            {
                return null;
            }

            return ExistingSafePath(root, relative[(separator + 1)..]);
        }

        foreach (var root in _roots)
        {
            var path = ExistingSafePath(root, normalized);
            if (path is not null)
                return path;
        }
        return null;
    }

    private static string? ExistingSafePath(string root, string relative)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var fromRoot = Path.GetRelativePath(root, path);
        if (fromRoot.Equals("..", StringComparison.Ordinal) ||
            fromRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(fromRoot))
        {
            return null;
        }
        return File.Exists(path) ? path : null;
    }
}
