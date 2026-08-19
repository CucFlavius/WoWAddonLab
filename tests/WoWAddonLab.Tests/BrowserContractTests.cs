namespace WoWAddonLab.Tests;

public sealed class BrowserContractTests
{
    [Fact]
    public void ConstructorEnablesNativeInputWhileFrameResetPreservesBrowserState()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local browser=CreateFrame('Browser','BrowserResetBoundary',UIParent); " +
            "browser:NavigateHome('PhotoSharing'); browser:OpenTicket(7); " +
            "browser:NavigateHome('KnowledgeBase'); browser:SetZoom(1.75)");

        Assert.Equal(
            "true:true:true:true:true",
            session.Lua.Evaluate(
                "local browser=BrowserResetBoundary; " +
                "return table.concat({tostring(browser:IsMouseClickEnabled())," +
                "tostring(browser:IsMouseMotionEnabled())," +
                "tostring(browser:IsMouseWheelEnabled())," +
                "tostring(browser:IsKeyboardEnabled())," +
                "tostring(browser:IsResizable())},':')"));

        session.Lua.Evaluate("BrowserResetBoundary:SetToDefaults()");

        Assert.Equal(
            "false:false:false:false:false",
            session.Lua.Evaluate(
                "local browser=BrowserResetBoundary; " +
                "return table.concat({tostring(browser:IsMouseClickEnabled())," +
                "tostring(browser:IsMouseMotionEnabled())," +
                "tostring(browser:IsMouseWheelEnabled())," +
                "tostring(browser:IsKeyboardEnabled())," +
                "tostring(browser:IsResizable())},':')"));

        var browser = session.Ui.Find("BrowserResetBoundary")!;
        Assert.Equal("KnowledgeBase", browser.BrowserPage);
        Assert.Equal((uint)7, browser.BrowserTicketIndex);
        Assert.Equal(1.75, browser.BrowserZoom);
    }
}
