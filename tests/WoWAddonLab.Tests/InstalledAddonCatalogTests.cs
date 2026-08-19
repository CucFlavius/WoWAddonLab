using WoWAddonLab.Addons;
using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Tests;

public sealed class InstalledAddonCatalogTests
{
    [Fact]
    public void CurrentProductSkipsEnabledOutdatedAddons()
    {
        var current = Addon("Current", "120007");
        var multiProduct = Addon("MultiProduct", "120007, 11509");
        var outdated = Addon("Outdated", "110100");
        var warnings = new List<string>();

        var resolved = InstalledAddonCatalog.ResolveLoadOrder(
            [current, multiProduct, outdated],
            new HashSet<string>(
                ["Current", "MultiProduct", "Outdated"],
                StringComparer.OrdinalIgnoreCase),
            warnings.Add,
            120007);

        Assert.Equal(
            ["Current", "MultiProduct"],
            resolved.Select(value => value.DirectoryName));
        Assert.Contains(
            warnings,
            value => value.Contains("Skipped Outdated", StringComparison.Ordinal));
    }

    private static InstalledAddon Addon(string name, string interfaceVersion)
    {
        var manifest = new AddonManifest(
            name,
            $"{name}.toc",
            name,
            new Dictionary<string, string>
            {
                ["Interface"] = interfaceVersion
            },
            [],
            [],
            [],
            []);
        return new InstalledAddon(
            name,
            name,
            manifest,
            name,
            name,
            null,
            interfaceVersion,
            [],
            false,
            null);
    }
}
