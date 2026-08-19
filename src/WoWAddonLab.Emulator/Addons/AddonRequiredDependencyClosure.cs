namespace WoWAddonLab.Emulator.Addons;

public static class AddonRequiredDependencyClosure
{
    public static IReadOnlyList<AddonManifest> Resolve(
        IEnumerable<AddonManifest> bootstrap,
        IEnumerable<AddonManifest> users,
        IEnumerable<AddonManifest> available)
    {
        var bootstrapManifests = bootstrap.ToArray();
        var userManifests = users.ToArray();
        var loaded = bootstrapManifests
            .Concat(userManifests)
            .ToDictionary(manifest => manifest.Name, StringComparer.OrdinalIgnoreCase);
        var availableByName = available
            .GroupBy(manifest => manifest.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var resolved = new List<AddonManifest>(bootstrapManifests);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(AddonManifest manifest)
        {
            if (!visited.Add(manifest.Name))
                return;

            foreach (var dependencyName in AddonManifestLoadOrder.RequiredDependencies(
                         manifest.Metadata))
            {
                if (loaded.TryGetValue(dependencyName, out var loadedDependency))
                {
                    Visit(loadedDependency);
                    continue;
                }
                if (!availableByName.TryGetValue(dependencyName, out var dependency))
                    continue;

                Visit(dependency);
                loaded[dependency.Name] = dependency;
                resolved.Add(dependency);
            }
        }

        foreach (var manifest in userManifests)
            Visit(manifest);
        return resolved
            .GroupBy(manifest => manifest.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }
}
