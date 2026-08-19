using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Tests;

public sealed class AddonManifestContractTests
{
    [Fact]
    public void TocTokensAndInlineConditionsUseTheSelectedProductContext()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wow-addon-lab-toc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, $"{Path.GetFileName(root)}.toc"),
                "## Title: Retail title [AllowLoadGameType standard]\n" +
                "## Notes: Classic note [AllowLoadGameType classic]\n" +
                "db\\[Game]\\ReferenceDB.lua\n" +
                "family\\[Family].lua\n" +
                "[AllowLoadTextLocale ruRU] locale\\ruRU-prefix.lua\n" +
                "[AllowLoadTextLocale enUS] locale\\enUS-prefix.lua\n" +
                "locale\\[TextLocale].lua [AllowLoadTextLocale deDE, enUS]\n" +
                "locale\\deDE.lua [AllowLoadTextLocale deDE]\n" +
                "retail.lua [AllowLoadGameType standard]\n" +
                "classic.lua [AllowLoadGameType classic]\n" +
                "excluded.lua [ExcludeLoadGameType standard]\n");

            var manifest = AddonManifest.Load(root, AddonManifestContext.Mainline);
            var relativeFiles = manifest.Files
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .ToArray();

            Assert.Equal("Retail title", manifest.Metadata["Title"]);
            Assert.False(manifest.Metadata.ContainsKey("Notes"));
            Assert.Equal(
                [
                    "db/Standard/ReferenceDB.lua",
                    "family/Mainline.lua",
                    "locale/enUS-prefix.lua",
                    "locale/enUS.lua",
                    "retail.lua"
                ],
                relativeFiles);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ProductSpecificTocIsPreferredWhenTheBaseManifestIsAbsent()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wow-addon-lab-toc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var name = Path.GetFileName(root);
            File.WriteAllText(Path.Combine(root, $"{name}_Cata.toc"), "## Title: Cata\ncata.lua\n");
            File.WriteAllText(
                Path.Combine(root, $"{name}_Mainline.toc"),
                "## Title: Mainline\nmainline.lua\n");

            var manifest = AddonManifest.Load(root, AddonManifestContext.Mainline);

            Assert.Equal(name, manifest.Name);
            Assert.Equal("Mainline", manifest.Metadata["Title"]);
            Assert.EndsWith($"{name}_Mainline.toc", manifest.TocPath);
            Assert.EndsWith("mainline.lua", Assert.Single(manifest.Files));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
