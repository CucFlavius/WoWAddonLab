using WoWAddonLab.Assets;

namespace WoWAddonLab.Tests;

public sealed class LocalAddonAssetSourceTests
{
    [Fact]
    public void ExtensionlessTextureUsesAddonBlpFile()
    {
        using var files = new TestAddonFiles();
        files.Write("assets\\logo_32x32.blp", [1, 2, 3]);
        var assets = new LocalAddonAssetSource([files.Root]);

        Assert.Equal(
            [1, 2, 3],
            assets.Read(
                "interface/AddOns/AllTheThings/assets/logo_32x32",
                defaultToBlp: true));
    }

    [Fact]
    public void ExactTextureWinsBeforeBlpFallback()
    {
        using var files = new TestAddonFiles();
        files.Write("assets\\icon.tga", [4]);
        files.Write("assets\\icon.blp", [5]);
        var assets = new LocalAddonAssetSource([files.Root]);

        Assert.Equal([4], assets.Read("assets\\icon.tga", defaultToBlp: true));
    }

    [Fact]
    public void ExtensionlessTextureUsesAddonTgaFile()
    {
        using var files = new TestAddonFiles();
        files.Write("minimap-button.tga", [8, 9]);
        var assets = new LocalAddonAssetSource([files.Root]);

        Assert.Equal(
            [8, 9],
            assets.Read(
                "Interface\\AddOns\\AllTheThings\\minimap-button",
                defaultToBlp: true));
    }

    [Fact]
    public void BlpWinsBeforeTgaFallback()
    {
        using var files = new TestAddonFiles();
        files.Write("assets\\icon.blp", [10]);
        files.Write("assets\\icon.tga", [11]);
        var assets = new LocalAddonAssetSource([files.Root]);

        Assert.Equal([10], assets.Read("assets\\icon", defaultToBlp: true));
    }

    [Fact]
    public void MissingExtensionIsReplacedWithBlp()
    {
        using var files = new TestAddonFiles();
        files.Write("assets\\icon.blp", [6]);
        var assets = new LocalAddonAssetSource([files.Root]);

        Assert.Equal([6], assets.Read("assets\\icon.tga", defaultToBlp: true));
    }

    [Fact]
    public void RelativePathsCannotEscapeAddonRoot()
    {
        using var files = new TestAddonFiles();
        var outside = Path.Combine(Path.GetDirectoryName(files.Root)!, "outside.blp");
        File.WriteAllBytes(outside, [7]);
        try
        {
            var assets = new LocalAddonAssetSource([files.Root]);
            Assert.Null(assets.Read("..\\outside", defaultToBlp: true));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Theory]
    [InlineData("Interface/AddOns/Test/assets/icon", "Interface/AddOns/Test/assets/icon.blp")]
    [InlineData("Interface/AddOns/Test/assets/icon.tga", "Interface/AddOns/Test/assets/icon.blp")]
    [InlineData("Interface/AddOns/Test/.hidden", "Interface/AddOns/Test/.hidden.blp")]
    public void DefaultBlpPathMatchesClientBehavior(string input, string expected) =>
        Assert.Equal(expected, WowFileAssetPath.WithDefaultBlpExtension(input));

    private sealed class TestAddonFiles : IDisposable
    {
        public TestAddonFiles()
        {
            var parent = Path.Combine(
                Path.GetTempPath(),
                $"wow-addon-lab-assets-{Guid.NewGuid():N}");
            Root = Path.Combine(parent, "AllTheThings");
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Write(string relativePath, byte[] bytes)
        {
            var path = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
        }

        public void Dispose() => Directory.Delete(Path.GetDirectoryName(Root)!, recursive: true);
    }
}
