namespace WoWAddonLab.Tests;

public sealed class ButtonContractTests
{
    private static readonly string[] NativeButtonMethods =
    [
        "ClearDisabledTexture", "ClearHighlightTexture", "ClearNormalTexture",
        "ClearPushedTexture", "Click", "Disable", "Enable", "GetButtonState",
        "GetDisabledFontObject", "GetDisabledTexture", "GetFontString",
        "GetHighlightFontObject", "GetHighlightTexture",
        "GetMotionScriptsWhileDisabled", "GetNormalFontObject", "GetNormalTexture",
        "GetPushedTextOffset", "GetPushedTexture", "GetText", "GetTextHeight",
        "GetTextWidth", "IsEnabled", "RegisterForClicks", "RegisterForMouse",
        "SetButtonState", "SetDisabledAtlas", "SetDisabledFontObject",
        "SetDisabledTexture", "SetEnabled", "SetFontString", "SetFormattedText",
        "SetHighlightAtlas", "SetHighlightFontObject", "SetHighlightTexture",
        "SetMotionScriptsWhileDisabled", "SetNormalAtlas", "SetNormalFontObject",
        "SetNormalTexture", "SetPushedAtlas", "SetPushedTextOffset",
        "SetPushedTexture", "SetText"
    ];

    private static readonly string[] NativeCheckButtonMethods =
    [
        "GetChecked", "GetCheckedTexture", "GetDisabledCheckedTexture",
        "SetChecked", "SetCheckedTexture", "SetDisabledCheckedTexture"
    ];

    [Fact]
    public void ButtonExposesEveryMethodInItsRecoveredNativeRegistrar()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeButtonMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeButtonMethods.Length + ":" +
            string.Join(
                ',',
                Enumerable.Repeat("function", NativeButtonMethods.Length)),
            session.Lua.Evaluate(
                "local button=CreateFrame('Button','ButtonBinarySurface',UIParent); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(button[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void ButtonDoesNotBorrowGenericFontOrEditMethods()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "nil:nil:nil:nil:nil:nil:function:function:function",
            session.Lua.Evaluate(
                "local button=CreateFrame('Button',nil,UIParent); " +
                "return table.concat({type(button.CopyFontObject)," +
                "type(button.GetFont),type(button.SetFont)," +
                "type(button.HighlightText),type(button.SetMultiLine)," +
                "type(button.SetTextInsets),type(button.GetFontString)," +
                "type(button.GetText),type(button.SetText)},':')"));
    }

    [Fact]
    public void CheckButtonExposesEveryMethodInItsRecoveredNativeRegistrar()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeCheckButtonMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeCheckButtonMethods.Length + ":" +
            string.Join(
                ',',
                Enumerable.Repeat("function", NativeCheckButtonMethods.Length)),
            session.Lua.Evaluate(
                "local button=CreateFrame('CheckButton','CheckButtonBinarySurface',UIParent); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(button[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void DisabledCheckedStateFallsBackToTheCheckedTexture()
    {
        using var session = new EmulatorSession();

        session.Lua.Evaluate(
            "local button=CreateFrame('CheckButton','CheckedTextureFallback',UIParent); " +
            "button:SetCheckedTexture(134400); button:SetChecked(true); button:Disable()");

        var button = session.Ui.Find("CheckedTextureFallback")!;
        Assert.True(button.Checked);
        Assert.False(button.Enabled);
        Assert.True(session.Ui.Find(button.CheckedTextureId!.Value)!.Shown);
        Assert.Null(button.DisabledCheckedTextureId);

        session.Lua.Evaluate(
            "CheckedTextureFallback:SetDisabledCheckedTexture(134401); " +
            "CheckedTextureFallback:SetChecked(true)");

        Assert.False(session.Ui.Find(button.CheckedTextureId.Value)!.Shown);
        Assert.True(session.Ui.Find(button.DisabledCheckedTextureId!.Value)!.Shown);
    }
}
