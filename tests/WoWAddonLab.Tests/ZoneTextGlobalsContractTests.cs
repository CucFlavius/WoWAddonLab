namespace WoWAddonLab.Tests;

public sealed class ZoneTextGlobalsContractTests
{
    [Fact]
    public void MinimapAndSubZoneTextUseIndependentNativeState()
    {
        using var session = new EmulatorSession();
        session.Lua.Client.MinimapZoneText = "Elwynn Forest";
        session.Lua.Client.SubZoneText = "Goldshire";

        Assert.Equal(
            "Elwynn Forest:Goldshire",
            session.Lua.Evaluate(
                "return GetMinimapZoneText()..':'..GetSubZoneText()"));
    }

    [Fact]
    public void ZoneTextQueriesIgnoreArgumentsAndReturnOneString()
    {
        using var session = new EmulatorSession();
        session.Lua.Client.MinimapZoneText = null;
        session.Lua.Client.SubZoneText = null;

        Assert.Equal(
            "1::string:1::string",
            session.Lua.Evaluate(
                "local function pack(...) return select('#',...),... end; " +
                "local minimapCount,minimap=pack(GetMinimapZoneText(false,17)); " +
                "local subCount,sub=pack(GetSubZoneText({},'ignored')); " +
                "return table.concat({minimapCount,minimap,type(minimap)," +
                "subCount,sub,type(sub)},':')"));
    }
}
