using WoWAddonLab.Emulator.Addons;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Addons;

public static class InstalledAddonCatalog
{
    public static IReadOnlyList<InstalledAddon> Discover(
        string productPath,
        AddonManifestContext? context = null)
    {
        var addonsPath = Path.Combine(productPath, "Interface", "AddOns");
        if (!Directory.Exists(addonsPath))
            return [];

        var result = new List<InstalledAddon>();
        foreach (var directory in Directory.EnumerateDirectories(addonsPath).OrderBy(Path.GetFileName))
        {
            var directoryName = Path.GetFileName(directory);
            try
            {
                var manifest = AddonManifest.Load(directory, context);
                var title = Metadata(manifest, "Title") ?? directoryName;
                result.Add(new InstalledAddon(
                    directoryName,
                    directory,
                    manifest,
                    title,
                    WowTextMarkup.PlainText(title),
                    Metadata(manifest, "Version"),
                    Metadata(manifest, "Interface"),
                    Dependencies(manifest),
                    IsTrue(Metadata(manifest, "LoadOnDemand")),
                    null));
            }
            catch (Exception exception)
            {
                result.Add(new InstalledAddon(
                    directoryName,
                    directory,
                    null,
                    directoryName,
                    directoryName,
                    null,
                    null,
                    [],
                    false,
                    exception.Message));
            }
        }
        return result;
    }

    public static IReadOnlyList<InstalledAddon> ResolveLoadOrder(
        IReadOnlyList<InstalledAddon> catalog,
        IReadOnlySet<string> enabled,
        Action<string>? warning = null,
        int minimumInterfaceVersion = 0)
    {
        var byName = new Dictionary<string, InstalledAddon>(StringComparer.OrdinalIgnoreCase);
        foreach (var addon in catalog.Where(value => value.Manifest is not null))
        {
            byName.TryAdd(addon.Manifest!.Name, addon);
            byName.TryAdd(addon.DirectoryName, addon);
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<InstalledAddon>();

        void Visit(InstalledAddon addon)
        {
            var key = addon.Manifest!.Name;
            if (visited.Contains(key))
                return;
            if (!SupportsInterface(addon.InterfaceVersion, minimumInterfaceVersion))
            {
                visited.Add(key);
                warning?.Invoke(
                    $"Skipped {key}: interface {addon.InterfaceVersion ?? "unknown"} " +
                    $"is older than {minimumInterfaceVersion}.");
                return;
            }
            if (!visiting.Add(key))
            {
                warning?.Invoke($"Dependency cycle includes {key}.");
                return;
            }
            foreach (var dependency in addon.RequiredDependencies)
            {
                if (byName.TryGetValue(dependency, out var required))
                    Visit(required);
                else
                    warning?.Invoke($"{key} requires missing addon {dependency}.");
            }
            visiting.Remove(key);
            visited.Add(key);
            result.Add(addon);
        }

        foreach (var addon in catalog.Where(value =>
                     value.Manifest is not null &&
                     (enabled.Contains(value.DirectoryName) || enabled.Contains(value.Manifest.Name))))
            Visit(addon);
        return result;
    }

    private static bool SupportsInterface(
        string? value,
        int minimumInterfaceVersion)
    {
        if (minimumInterfaceVersion <= 0)
            return true;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Split(
                [',', ' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Any(part =>
                int.TryParse(part, out var interfaceVersion) &&
                interfaceVersion >= minimumInterfaceVersion);
    }

    private static string? Metadata(AddonManifest manifest, string key) =>
        manifest.Metadata.TryGetValue(key, out var value) ? value : null;

    private static IReadOnlyList<string> Dependencies(AddonManifest manifest)
    {
        var keys = new[] { "Dependencies", "RequiredDeps", "RequiredDependencies" };
        return keys
            .Where(manifest.Metadata.ContainsKey)
            .SelectMany(key => manifest.Metadata[key].Split(
                [',', ' '],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsTrue(string? value) =>
        value is not null &&
        (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("yes", StringComparison.OrdinalIgnoreCase));
}
