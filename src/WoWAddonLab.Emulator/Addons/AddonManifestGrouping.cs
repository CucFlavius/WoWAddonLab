namespace WoWAddonLab.Emulator.Addons;

internal static class AddonManifestGrouping
{
    private static readonly string[] DependencyKeys =
    [
        "Dependencies", "Dep", "RequiredDep", "RequiredDeps",
        "RequiredDependencies"
    ];

    public static IReadOnlyList<AddonManifest> Apply(
        IReadOnlyList<AddonManifest> manifests)
    {
        var byName = manifests.ToDictionary(
            manifest => manifest.Name,
            StringComparer.OrdinalIgnoreCase);
        return manifests.Select(manifest => Apply(manifest, byName)).ToArray();
    }

    private static AddonManifest Apply(
        AddonManifest manifest,
        IReadOnlyDictionary<string, AddonManifest> byName)
    {
        if (manifest.Metadata.ContainsKey("Group"))
            return manifest;

        var group = Dependencies(manifest)
            .Where(byName.ContainsKey)
            .Where(dependency =>
                !dependency.Equals(manifest.Name, StringComparison.OrdinalIgnoreCase) &&
                manifest.Name.StartsWith(dependency, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(dependency => dependency.Length)
            .FirstOrDefault() ?? manifest.Name;
        var metadata = new Dictionary<string, string>(
            manifest.Metadata,
            StringComparer.OrdinalIgnoreCase)
        {
            ["Group"] = group
        };
        return manifest with { Metadata = metadata };
    }

    private static IEnumerable<string> Dependencies(AddonManifest manifest) =>
        DependencyKeys
            .Where(manifest.Metadata.ContainsKey)
            .SelectMany(key => manifest.Metadata[key].Split(
                [',', ' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
