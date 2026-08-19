namespace WoWAddonLab.Tests;

public sealed class CurrentRegionContractTests
{
    [Fact]
    public void RegionIdAndNameShareTheNativeRegionTable()
    {
        using var session = new EmulatorSession();

        Assert.Equal("3:EU", session.Lua.Evaluate(
            "return GetCurrentRegion()..':'..GetCurrentRegionName()"));

        var expectedNames = new[] { "", "US", "KR", "EU", "TW", "CN" };
        for (var region = 0; region < expectedNames.Length; region++)
        {
            session.Lua.Localization.CurrentRegion = region;
            Assert.Equal(
                $"{region}:{expectedNames[region]}",
                session.Lua.Evaluate(
                    "return GetCurrentRegion()..':'..GetCurrentRegionName()"));
        }
    }

    [Fact]
    public void RegionQueriesIgnoreArgumentsAndReturnOneValueEach()
    {
        using var session = new EmulatorSession();
        session.Lua.Localization.CurrentRegion = 5;

        Assert.Equal(
            "1:5:1:CN",
            session.Lua.Evaluate(
                "local function pack(...) return select('#',...),... end; " +
                "local idCount,id=pack(GetCurrentRegion(false,17)); " +
                "local nameCount,name=pack(GetCurrentRegionName({},'ignored')); " +
                "return table.concat({idCount,id,nameCount,name},':')"));
    }
}
