using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Tests;

public sealed class AddonManifestLoadOrderTests
{
    [Fact]
    public void PresentOptionalDependenciesLoadBeforeTheirConsumer()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-load-order-{Guid.NewGuid():N}");
        var restricted = Path.Combine(root, "Restricted");
        var cleanup = Path.Combine(root, "Cleanup");
        Directory.CreateDirectory(restricted);
        Directory.CreateDirectory(cleanup);
        try
        {
            File.WriteAllText(
                Path.Combine(restricted, "Restricted.toc"),
                "## Interface: 1\n## Title: Restricted\n");
            File.WriteAllText(
                Path.Combine(cleanup, "Cleanup.toc"),
                "## Interface: 1\n## Title: Cleanup\n## OptionalDeps: Restricted\n");

            var ordered = AddonManifestLoadOrder.Order(
                [AddonManifest.Load(cleanup), AddonManifest.Load(restricted)]);

            Assert.Equal(["Restricted", "Cleanup"], ordered.Select(value => value.Name));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
