using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Tests;

public sealed class AddonRequiredDependencyClosureTests
{
    [Fact]
    public void RequiredAvailableDependenciesJoinTheBootstrapClosure()
    {
        var baseUi = Manifest("Blizzard_UIParent");
        var collections = Manifest(
            "Blizzard_Collections",
            ("Dependencies", "Blizzard_Transmog"));
        var transmog = Manifest("Blizzard_Transmog");
        var addon = Manifest(
            "BetterWardrobe",
            ("Dependencies", "Blizzard_Collections, Blizzard_Transmog"),
            ("OptionalDeps", "OptionalIntegration"));
        var optional = Manifest("OptionalIntegration");

        var result = AddonRequiredDependencyClosure.Resolve(
            [baseUi],
            [addon],
            [baseUi, collections, transmog, optional]);

        Assert.Equal(
            ["Blizzard_UIParent", "Blizzard_Transmog", "Blizzard_Collections"],
            result.Select(manifest => manifest.Name));
    }

    private static AddonManifest Manifest(
        string name,
        params (string Key, string Value)[] metadata) =>
        new(
            name,
            $"{name}.toc",
            name,
            metadata.ToDictionary(
                value => value.Key,
                value => value.Value,
                StringComparer.OrdinalIgnoreCase),
            [],
            [],
            [],
            []);
}
