using WoWAddonLab.Addons;

namespace WoWAddonLab.Tests;

public sealed class BlizzardUiBootstrapTests
{
    [Fact]
    public void StartupClassificationUsesTheManifestLoadOnDemandFlag()
    {
        Assert.True(
            BlizzardUiBootstrap.ShouldLoadAtStartup(
                new Dictionary<string, string>()));
        Assert.True(
            BlizzardUiBootstrap.ShouldLoadAtStartup(
                new Dictionary<string, string>
                {
                    ["Title"] = "Blizzard_TokenUI"
                }));
        Assert.False(
            BlizzardUiBootstrap.ShouldLoadAtStartup(
                new Dictionary<string, string>
                {
                    ["LoadOnDemand"] = "1"
                }));
        Assert.False(
            BlizzardUiBootstrap.ShouldLoadAtStartup(
                new Dictionary<string, string>
                {
                    ["LoadOnDemand"] = "true"
                }));
    }

    [Theory]
    [InlineData("wowt")]
    [InlineData("wowxptr")]
    [InlineData("wow_beta")]
    [InlineData("wow_classic_ptr")]
    [InlineData("wow_classic_era_ptr")]
    [InlineData("wow_classic_beta")]
    public void OnlyBetaAndPtrAddonsLoadOnPublicTestProducts(string productCode)
    {
        Assert.True(
            BlizzardUiBootstrap.IsOnlyBetaAndPtrCompatible(
                new Dictionary<string, string> { ["OnlyBetaAndPTR"] = "1" },
                productCode));
    }

    [Theory]
    [InlineData("wow")]
    [InlineData("wow_classic")]
    [InlineData("wowdev")]
    [InlineData("wow_classic_alpha")]
    [InlineData("wowz")]
    public void OnlyBetaAndPtrAddonsDoNotLoadOnLiveAlphaOrSubmissionProducts(string productCode)
    {
        Assert.False(
            BlizzardUiBootstrap.IsOnlyBetaAndPtrCompatible(
                new Dictionary<string, string> { ["OnlyBetaAndPTR"] = "true" },
                productCode));
    }

    [Fact]
    public void OrdinaryAddonsRemainCompatibleWithEveryProduct()
    {
        Assert.True(
            BlizzardUiBootstrap.IsOnlyBetaAndPtrCompatible(
                new Dictionary<string, string>(),
                "wow"));
    }
}
