using WoWAddonLab.Assets;

namespace WoWAddonLab.Tests;

public sealed class TactAssetSourceTests
{
    [Fact]
    public void SortedListfileFilenameLookupHandlesVariableLengthRows()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-listfile-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(
                path,
                "1;a.blp\n" +
                "77;interface/really/long/path/to/test.blp\n" +
                "902;z.blp\n");

            Assert.Equal("a.blp", TactAssetSource.FindFilenameInSortedListfile(path, 1));
            Assert.Equal(
                "interface/really/long/path/to/test.blp",
                TactAssetSource.FindFilenameInSortedListfile(path, 77));
            Assert.Equal("z.blp", TactAssetSource.FindFilenameInSortedListfile(path, 902));
            Assert.Null(TactAssetSource.FindFilenameInSortedListfile(path, 78));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
