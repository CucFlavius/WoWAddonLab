using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class FontStringContractTests
{
    [Fact]
    public void CreateFontStringAcceptsARuntimeFontObjectNameAsItsTemplate()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:Fonts\\ARIALN.TTF:17:BACKGROUND:0",
            session.Lua.Evaluate(
                "local font=CreateFont('RuntimeFontStringTemplate'); " +
                "font:SetFont('Fonts\\\\ARIALN.TTF',17,'OUTLINE'); " +
                "local text=UIParent:CreateFontString(nil,'BACKGROUND'," +
                "'RuntimeFontStringTemplate',0); " +
                "local file,height=text:GetFont(); local layer,sublevel=text:GetDrawLayer(); " +
                "return table.concat({tostring(text:GetFontObject()==font),file,height," +
                "layer,sublevel},':')"));
    }

    [Fact]
    public void SetToDefaultsPreservesRegionOwnershipForFrameStrataAndRenderOrder()
    {
        using var session = new EmulatorSession();

        session.Lua.Evaluate(
            "local chat=CreateFrame('Frame','RegionResetLowOwner',UIParent); " +
            "chat:SetSize(200,100); chat:SetPoint('CENTER'); " +
            "chat:SetFrameStrata('LOW'); " +
            "local chatText=chat:CreateFontString('RegionResetLowText'); " +
            "chatText:SetToDefaults(); " +
            "chatText:SetFont('Fonts\\\\FRIZQT__.TTF',12,''); " +
            "chatText:SetText('chat'); chatText:SetPoint('CENTER'); " +
            "local menu=CreateFrame('Frame','RegionResetDialogOwner',UIParent); " +
            "menu:SetSize(200,100); menu:SetPoint('CENTER'); " +
            "menu:SetFrameStrata('FULLSCREEN_DIALOG'); " +
            "local menuText=menu:CreateFontString('RegionResetDialogText'); " +
            "menuText:SetToDefaults(); " +
            "menuText:SetFont('Fonts\\\\FRIZQT__.TTF',12,''); " +
            "menuText:SetText('menu'); menuText:SetPoint('CENTER')");

        var chatText = session.Ui.Find("RegionResetLowText")!;
        var menuText = session.Ui.Find("RegionResetDialogText")!;
        Assert.Equal(string.Empty, chatText.FrameStrata);
        Assert.Equal(string.Empty, menuText.FrameStrata);
        Assert.Equal("LOW", session.Ui.EffectiveFrameStrata(chatText));
        Assert.Equal("FULLSCREEN_DIALOG", session.Ui.EffectiveFrameStrata(menuText));

        var order = session.Ui.VisualRenderOrder().Select(value => value.Id).ToArray();
        Assert.True(
            Array.IndexOf(order, chatText.Id) < Array.IndexOf(order, menuText.Id));
    }

    private static readonly string[] NativeFontStringMethods =
    [
        "CalculateScreenAreaFromCharacterSpan",
        "CanNonSpaceWrap", "CanWordWrap", "ClearAlphaGradient", "ClearText",
        "FindCharacterIndexAtCoordinate", "GetAlphaGradient", "GetFieldSize",
        "GetFont", "GetFontHeight", "GetFontObject", "GetIndentedWordWrap",
        "GetJustifyH", "GetJustifyV", "GetLineHeight", "GetMaxLines",
        "GetNumLines", "GetRotation", "GetScaleAnimationMode", "GetShadowColor",
        "GetShadowOffset", "GetSmoothScaling", "GetSpacing", "GetStringHeight",
        "GetStringWidth", "GetText", "GetTextColor", "GetTextScale",
        "GetUnboundedStringWidth", "GetUnboundedStringWidthForText",
        "GetWrappedWidth", "IsTruncated", "OnColorsUpdated", "SetAlphaGradient",
        "SetFixedColor", "SetFont", "SetFontHeight", "SetFontObject",
        "SetFormattedText", "SetIndentedWordWrap", "SetJustifyH", "SetJustifyV",
        "SetMaxLines", "SetNonSpaceWrap", "SetRotation", "SetScaleAnimationMode",
        "SetShadowColor", "SetShadowOffset", "SetSmoothScaling", "SetSpacing",
        "SetText", "SetTextColor", "SetTextHeight", "SetTextScale",
        "SetTextToFit", "SetWordWrap"
    ];

    [Fact]
    public void FontStringExposesEveryMethodInItsRecoveredNativeRegistrar()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeFontStringMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeFontStringMethods.Length + ":" +
            string.Join(
                ',',
                Enumerable.Repeat("function", NativeFontStringMethods.Length)),
            session.Lua.Evaluate(
                "local text=UIParent:CreateFontString('FontStringBinarySurface'); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(text[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void FontStringDerivedResetRestoresOwnedConstructorDefaults()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "-1:0:0:0:0:1",
            session.Lua.Evaluate(
                "local text=UIParent:CreateFontString('FontStringResetDefaults'); " +
                "local start,length=text:GetAlphaGradient(); " +
                "local r,g,b,a=text:GetShadowColor(); " +
                "return string.format('%d:%d:%.0f:%.0f:%.0f:%.0f'," +
                "start,length,r,g,b,a)"));

        session.Lua.Evaluate(
            "local source=CreateFont('FontStringResetSource'); " +
            "source:SetFont('Fonts\\ARIALN.TTF',17,'OUTLINE'); " +
            "local text=FontStringResetDefaults; text:SetFontObject(source); " +
            "text:SetText('reset state'); text:SetAlphaGradient(3,4); " +
            "text:SetTextColor(.1,.2,.3,.4); " +
            "text:SetShadowColor(.2,.3,.4,.5); text:SetShadowOffset(2,-3); " +
            "text:SetJustifyH('LEFT'); text:SetJustifyV('TOP'); " +
            "text:SetSpacing(4); text:SetMaxLines(7); " +
            "text:SetIndentedWordWrap(true); text:SetWordWrap(false); " +
            "text:SetNonSpaceWrap(true); text:SetTextScale(1.75); " +
            "text:SetRotation(.75); text:SetScaleAnimationMode(1); " +
            "text:SetSmoothScaling(true); text:SetFixedColor(true); " +
            "text:SetToDefaults()");

        Assert.Equal(
            "-1:0:true:true:0:0:0:1:0:0:0:1:CENTER:MIDDLE:0:true:false:false",
            session.Lua.Evaluate(
                "local text=FontStringResetDefaults; " +
                "local start,length=text:GetAlphaGradient(); " +
                "local r,g,b,a=text:GetShadowColor(); " +
                "local x,y=text:GetShadowOffset(); " +
                "return table.concat({start,length," +
                "tostring(text:GetFontObject()==nil),tostring(text:GetText()==nil)," +
                "r,g,b,a,x,y,text:GetRotation(),text:GetTextScale()," +
                "text:GetJustifyH(),text:GetJustifyV(),text:GetMaxLines()," +
                "tostring(text:CanWordWrap()),tostring(text:CanNonSpaceWrap())," +
                "tostring(text:GetIndentedWordWrap())},':')"));

        var text = session.Ui.Find("FontStringResetDefaults")!;
        Assert.Null(text.FontObjectId);
        Assert.False(text.Font!.IsConfigured);
        Assert.Equal(UiFontOverrides.None, text.Font.LocalOverrides);
        Assert.Equal(0f, text.Font.Spacing);
        Assert.False(text.FontSmoothScaling);
        Assert.False(text.FontFixedColor);
        Assert.Equal((byte)0, text.FontScaleAnimationMode);
        Assert.Equal(1f, text.FontAnimationFontSizeScale);
        Assert.Equal(1f, text.FontAnimationVertexScale);
    }

    [Fact]
    public void FontStringRotationUsesFloatValidationAndSurvivesOtherVisualStateChanges()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1.570796:false:false:1.570796:0",
            session.Lua.Evaluate(
                "local text=UIParent:CreateFontString('FontStringRotationState'); " +
                "text:SetFont('Fonts\\\\FRIZQT__.TTF',12,'OUTLINE'); " +
                "text:SetText('rotation'); text:SetRotation(math.pi/2); " +
                "local finite=text:GetRotation(); " +
                "local infinity=pcall(text.SetRotation,text,math.huge); " +
                "local missing=pcall(text.SetRotation,text); " +
                "text:SetShadowOffset(2,-3); text:SetSmoothScaling(true); " +
                "local retained=text:GetRotation(); text:SetToDefaults(); " +
                "return string.format('%.6f:%s:%s:%.6f:%.0f'," +
                "finite,tostring(infinity),tostring(missing),retained,text:GetRotation())"));
    }

    [Fact]
    public void FontStringFontHeightDefaultsToTheCalculatedRasterMetric()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "-1.0:-1.0:20.0:10.4:10.4:10.4:10.4:20.0:20.0",
            session.Lua.Evaluate(
                "local empty=UIParent:CreateFontString(); " +
                "local owner=CreateFrame('Frame',nil,UIParent); owner:SetScale(1.25); " +
                "local text=owner:CreateFontString(); " +
                "text:SetFont('Fonts\\\\FRIZQT__.TTF',20,''); text:SetTextScale(0.5); " +
                "local font=CreateFont('FontHeightBinaryObject'); " +
                "font:SetFont('Fonts\\\\FRIZQT__.TTF',20,''); " +
                "local result=string.format('%.1f:%.1f:%.1f:%.1f:%.1f:%.1f'," +
                "empty:GetFontHeight(),empty:GetFontHeight(false)," +
                "text:GetFontHeight(false),text:GetFontHeight()," +
                "text:GetFontHeight(true),text:GetFontHeight(nil)); " +
                "local truthy=text:GetFontHeight(0); text:SetSmoothScaling(true); " +
                "return result..string.format(':%.1f:%.1f:%.1f',truthy," +
                "text:GetFontHeight(),font:GetFontHeight(false))"));
    }

    [Fact]
    public void FontStringCharacterSpanUsesTheNativeOneBasedIndexParser()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:true:false:false:false",
            session.Lua.Evaluate(
                "local text=UIParent:CreateFontString('FontStringCharacterSpanIndices'); " +
                "text:SetSize(100,20); text:SetFont('Fonts\\\\FRIZQT__.TTF',12,''); " +
                "text:SetText('abcd'); " +
                "local zeroOk,zeroResult=pcall(text.CalculateScreenAreaFromCharacterSpan,text,0,0); " +
                "local fractionOk,fractionResult=pcall(" +
                "text.CalculateScreenAreaFromCharacterSpan,text,1.9,3.9); " +
                "local maxOk,maxResult=pcall(" +
                "text.CalculateScreenAreaFromCharacterSpan,text,4294967295,4294967295); " +
                "local negativeOk=pcall(" +
                "text.CalculateScreenAreaFromCharacterSpan,text,-1,1); " +
                "local tooLargeOk=pcall(" +
                "text.CalculateScreenAreaFromCharacterSpan,text,4294967296,1); " +
                "local infiniteOk=pcall(" +
                "text.CalculateScreenAreaFromCharacterSpan,text,math.huge,1); " +
                "return table.concat({tostring(zeroOk),tostring(zeroResult==nil)," +
                "tostring(fractionOk and fractionResult~=nil),tostring(maxOk and maxResult==nil)," +
                "tostring(negativeOk),tostring(tooLargeOk),tostring(infiniteOk)},':')"));
    }

    [Fact]
    public void FontStringCharacterSpanIndexesDisplayTextUtf8Bytes()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1:10.8:21.6:true",
            session.Lua.Evaluate(
                "local text=UIParent:CreateFontString('FontStringUtf8Span'); " +
                "text:SetSize(100,20); text:SetFont('Fonts\\\\FRIZQT__.TTF',20,''); " +
                "text:SetText('éa'); " +
                "local first=text:CalculateScreenAreaFromCharacterSpan(1,3); " +
                "local all=text:CalculateScreenAreaFromCharacterSpan(1,4); " +
                "local invalid=text:CalculateScreenAreaFromCharacterSpan(1,5); " +
                "return string.format('%d:%.1f:%.1f:%s'," +
                "#first,first[1].width,all[1].width,tostring(invalid==nil))"));
    }

    [Fact]
    public void FontStringStoresProcessedPluralGrammarAndPreservesRichMarkup()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1 Slot |cffff0000red|r:2 Slots:3 things",
            session.Lua.Evaluate(
                "local text=UIParent:CreateFontString('FontStringStoredGrammar'); " +
                "text:SetFont('Fonts\\\\FRIZQT__.TTF',12,''); " +
                "text:SetText('1 |4Slot:Slots; |cffff0000red|r'); " +
                "local singular=text:GetText(); " +
                "text:SetFormattedText('%d |4Slot:Slots;',2); " +
                "local plural=text:GetText(); " +
                "text:SetTextToFit('3 |7thing:things:things;'); " +
                "return singular..':'..plural..':'..text:GetText()"));
    }
}
