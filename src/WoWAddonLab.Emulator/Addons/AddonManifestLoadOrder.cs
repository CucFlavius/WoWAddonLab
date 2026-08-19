namespace WoWAddonLab.Emulator.Addons;

public static class AddonManifestLoadOrder
{
    private static readonly string[] RequiredDependencyKeys =
    [
        "Dependencies", "Dep", "RequiredDep", "RequiredDeps", "RequiredDependencies"
    ];

    private static readonly string[] OptionalDependencyKeys =
    [
        "OptionalDep", "OptionalDeps", "OptionalDependencies"
    ];

    public static IReadOnlyList<AddonManifest> Order(IEnumerable<AddonManifest> manifests)
    {
        var source = manifests.ToArray();
        var byName = source.ToDictionary(value => value.Name, StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<AddonManifest>(source.Length);

        void Visit(AddonManifest manifest)
        {
            if (!visited.Add(manifest.Name))
                return;

            foreach (var dependencyName in Dependencies(manifest.Metadata))
            {
                if (byName.TryGetValue(dependencyName, out var dependency))
                    Visit(dependency);
            }
            result.Add(manifest);
        }

        foreach (var manifest in source)
            Visit(manifest);
        return result;
    }

    public static IEnumerable<string> Dependencies(
        IReadOnlyDictionary<string, string> metadata) =>
        RequiredDependencies(metadata)
            .Concat(OptionalDependencies(metadata))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> RequiredDependencies(
        IReadOnlyDictionary<string, string> metadata) =>
        RequiredDependencyKeys
            .Where(metadata.ContainsKey)
            .SelectMany(key => SplitNames(metadata[key]))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> OptionalDependencies(
        IReadOnlyDictionary<string, string> metadata) =>
        OptionalDependencyKeys
            .Where(metadata.ContainsKey)
            .SelectMany(key => SplitNames(metadata[key]))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> SplitNames(string value) =>
        value.Split(
            [',', ' ', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
