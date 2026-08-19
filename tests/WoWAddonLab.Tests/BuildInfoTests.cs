namespace WoWAddonLab.Tests;

public sealed class BuildInfoTests
{
    [Fact]
    public void GetBuildInfoUsesTheSelectedInstallationMetadata()
    {
        using var session = new EmulatorSession();
        session.BuildInfo = new WowBuildInfo(
            "product-version",
            "product-build",
            "product-date",
            42);

        Assert.Equal(
            "product-version:product-build:product-date:42",
            session.Lua.Evaluate(
                "local version,build,date,interface=GetBuildInfo(); " +
                "return table.concat({version,build,date,interface},':')"));
    }
}
