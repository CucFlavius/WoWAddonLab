namespace WoWAddonLab.Tests;

public sealed class CheckoutContractTests
{
    private static readonly string[] NativeCheckoutMethods =
    [
        "CancelOpenCheckout", "ClearFocus", "CloseCheckout", "CopyExternalLink",
        "OpenCheckout", "OpenExternalLink", "SetFocus", "SetZoom"
    ];

    [Fact]
    public void CheckoutRegistersItsExactOwnedSurfaceWithoutBrowserOnlyMethods()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeCheckoutMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            "8:function,function,function,function,function,function,function,function:" +
            "nil:nil:nil:nil:nil",
            session.Lua.Evaluate(
                "local checkout=CreateFrame('Checkout','CheckoutSurfaceTarget',UIParent); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(checkout[name]) end; " +
                "return #methods..':'..table.concat(result,',')..':'.." +
                "table.concat({type(checkout.DeleteCookies),type(checkout.NavigateBack)," +
                "type(checkout.NavigateHome),type(checkout.NavigateTo)," +
                "type(checkout.OpenTicket)},':')"));
    }

    [Fact]
    public void CheckoutUsesNativeArgumentsNoProviderFallbackAndFocusBehavior()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:0:0:0",
            session.Lua.Evaluate(
                "local checkout=CreateFrame('Checkout','CheckoutContractTarget',UIParent); " +
                "local missing=pcall(function() checkout:OpenCheckout() end); " +
                "local bad=pcall(function() checkout:OpenCheckout('bad') end); " +
                "local opened=checkout:OpenCheckout('173'); " +
                "local zoomMissing=pcall(function() checkout:SetZoom() end); " +
                "checkout:SetZoom('1.25'); checkout:SetFocus(); " +
                "local focused=select('#',checkout:ClearFocus()); " +
                "local canceled=select('#',checkout:CancelOpenCheckout()); " +
                "local closed=select('#',checkout:CloseCheckout()); " +
                "return table.concat({tostring(missing),tostring(bad),tostring(opened)," +
                "tostring(zoomMissing),focused,canceled,closed},':')"));

        var checkout = session.Ui.Find("CheckoutContractTarget")!;
        Assert.Equal(173, checkout.CheckoutLastRequestedId);
        Assert.False(checkout.CheckoutOpen);
        Assert.Equal(1.25f, checkout.BrowserZoom);
        Assert.Null(session.Ui.FocusedObjectId);
    }

    [Fact]
    public void BrowserConstructorDefaultsResetAsFrameWhileCheckoutStateSurvives()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local checkout=CreateFrame('Checkout','CheckoutResetTarget',UIParent); " +
            "checkout:OpenCheckout(41); checkout:SetZoom(1.5)");

        Assert.Equal(
            "true:true:true:true:true",
            session.Lua.Evaluate(
                "local checkout=CheckoutResetTarget; " +
                "return table.concat({tostring(checkout:IsMouseClickEnabled())," +
                "tostring(checkout:IsMouseMotionEnabled())," +
                "tostring(checkout:IsMouseWheelEnabled())," +
                "tostring(checkout:IsKeyboardEnabled())," +
                "tostring(checkout:IsResizable())},':')"));

        session.Lua.Evaluate("CheckoutResetTarget:SetToDefaults()");

        Assert.Equal(
            "false:false:false:false:false",
            session.Lua.Evaluate(
                "local checkout=CheckoutResetTarget; " +
                "return table.concat({tostring(checkout:IsMouseClickEnabled())," +
                "tostring(checkout:IsMouseMotionEnabled())," +
                "tostring(checkout:IsMouseWheelEnabled())," +
                "tostring(checkout:IsKeyboardEnabled())," +
                "tostring(checkout:IsResizable())},':')"));

        var checkout = session.Ui.Find("CheckoutResetTarget")!;
        Assert.Equal(41, checkout.CheckoutLastRequestedId);
        Assert.Equal(1.5f, checkout.BrowserZoom);
    }
}
