using System.Numerics;

namespace WoWAddonLab.Tests;

public sealed class ColorSelectContractTests
{
    private static readonly string[] NativeMethods =
    [
        "ClearColorWheelTexture", "GetColorAlpha", "GetColorAlphaTexture",
        "GetColorAlphaThumbTexture", "GetColorHSV", "GetColorRGB",
        "GetColorValueTexture", "GetColorValueThumbTexture", "GetColorWheelTexture",
        "GetColorWheelThumbTexture", "SetColorAlpha", "SetColorAlphaTexture",
        "SetColorAlphaThumbTexture", "SetColorHSV", "SetColorRGB",
        "SetColorValueTexture", "SetColorValueThumbTexture", "SetColorWheelTexture",
        "SetColorWheelThumbTexture"
    ];

    [Fact]
    public void ColorSelectRegistersItsExactOwnedSurfaceAndConstructorDefaults()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeMethods.Length + ":" +
            string.Join(',', Enumerable.Repeat("function", NativeMethods.Length)),
            session.Lua.Evaluate(
                "local frame=CreateFrame('ColorSelect'); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(frame[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
        Assert.Equal(
            "0:0:1:1:1:1:1",
            session.Lua.Evaluate(
                "local frame=CreateFrame('ColorSelect'); " +
                "local h,s,v=frame:GetColorHSV(); local r,g,b=frame:GetColorRGB(); " +
                "return table.concat({h,s,v,frame:GetColorAlpha(),r,g,b},':')"));
    }

    [Fact]
    public void SetToDefaultsUsesFrameResetWhileOnHideClearsPointerSelection()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "picker=CreateFrame('ColorSelect','ResetColorSelect',UIParent); " +
            "wheel=picker:CreateTexture('ResetColorWheel'); " +
            "picker:SetColorWheelTexture(wheel); picker:SetColorHSV(120,.5,.25); " +
            "picker:SetColorAlpha(.75); picker:EnableKeyboard(true)");
        var picker = session.Ui.Find("ResetColorSelect")!;
        picker.ColorSelect!.SelectingWheel = true;
        picker.ColorSelect.SelectingValue = true;
        picker.ColorSelect.SelectingAlpha = true;
        picker.ColorSelect.Dirty = true;

        Assert.Equal(
            "120:0.5:0.25:0.75:true:false",
            session.Lua.Evaluate(
                "picker:SetToDefaults(); local h,s,v=picker:GetColorHSV(); " +
                "return table.concat({h,s,v,picker:GetColorAlpha()," +
                "tostring(picker:GetColorWheelTexture()==wheel)," +
                "tostring(picker:IsKeyboardEnabled())},':')"));
        Assert.True(picker.ColorSelect.SelectingWheel);
        Assert.True(picker.ColorSelect.SelectingValue);
        Assert.True(picker.ColorSelect.SelectingAlpha);
        Assert.True(picker.ColorSelect.Dirty);

        session.Lua.Evaluate("picker:Hide()");

        Assert.False(picker.ColorSelect.SelectingWheel);
        Assert.False(picker.ColorSelect.SelectingValue);
        Assert.False(picker.ColorSelect.SelectingAlpha);
        Assert.True(picker.ColorSelect.Dirty);
    }

    [Fact]
    public void ScalarSettersClampQuantizeConvertAndDispatchImmediately()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "picker=CreateFrame('ColorSelect','picker',UIParent); calls=0; selected=''; " +
            "picker:SetScript('OnColorSelect',function(_,r,g,b) " +
            "calls=calls+1; selected=table.concat({r,g,b},':') end); " +
            "picker:SetColorHSV(120,1,0.5)");

        var picker = session.Ui.Find("picker")!;
        Assert.Equal(120, picker.ColorSelect!.Hue);
        Assert.Equal(1, picker.ColorSelect.Saturation);
        Assert.Equal(0.5f, picker.ColorSelect.Value);
        Assert.Equal("1", session.Lua.Evaluate("return calls"));
        Assert.Equal(new Vector3(0, 127 / 255f, 0), ReadRgb(session));

        session.Lua.Evaluate("picker:SetColorRGB(2,0.5,-1)");
        Assert.Equal(2, int.Parse(session.Lua.Evaluate("return calls")));
        Assert.Equal(new Vector3(1, 128 / 255f, 0), picker.ColorSelect is { } state
            ? ReadRgb(session)
            : default);

        session.Lua.Evaluate("picker:SetColorRGB(1,1,1)");
        Assert.Equal(-1, picker.ColorSelect.Hue);
        Assert.Equal(0, picker.ColorSelect.Saturation);

        session.Lua.Evaluate("picker:SetColorAlpha(-2)");
        Assert.Equal(0, picker.ColorSelect.Alpha);
        Assert.Equal(4, int.Parse(session.Lua.Evaluate("return calls")));
    }

    [Fact]
    public void TextureMethodsOwnTheSixNativeRegionRoles()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "picker=CreateFrame('ColorSelect','TexturePicker',UIParent); " +
            "wheel=picker:CreateTexture('wheel'); value=picker:CreateTexture('value'); " +
            "alpha=picker:CreateTexture('alpha'); " +
            "picker:SetColorWheelTexture(wheel); picker:SetColorValueTexture(value); " +
            "picker:SetColorAlphaTexture(alpha); " +
            "picker:SetColorWheelThumbTexture('Interface\\\\Buttons\\\\UI-ColorPicker-Buttons'); " +
            "picker:SetColorValueThumbTexture(130840); " +
            "picker:SetColorAlphaThumbTexture('Interface\\\\Buttons\\\\UI-ColorPicker-Buttons')");

        var picker = session.Ui.Find("TexturePicker")!;
        var state = picker.ColorSelect!;
        Assert.Equal(session.Ui.Find("wheel")?.Id, state.WheelTextureId);
        Assert.Equal(session.Ui.Find("value")?.Id, state.ValueTextureId);
        Assert.Equal(session.Ui.Find("alpha")?.Id, state.AlphaTextureId);
        Assert.True(session.Ui.Find(state.WheelTextureId!.Value)!.Texture!.IsColorSelectWheel);
        Assert.Equal("OVERLAY", session.Ui.Find(state.WheelThumbTextureId!.Value)!.DrawLayer);
        Assert.Equal((uint)130840, session.Ui.Find(state.ValueThumbTextureId!.Value)!.Texture!.FileDataId);
        Assert.Equal(
            "table:table:table:table:table:table",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(picker:GetColorWheelTexture()),type(picker:GetColorWheelThumbTexture())," +
                "type(picker:GetColorValueTexture()),type(picker:GetColorValueThumbTexture())," +
                "type(picker:GetColorAlphaTexture()),type(picker:GetColorAlphaThumbTexture())},':')"));

        session.Lua.Evaluate("picker:ClearColorWheelTexture()");
        Assert.Null(state.WheelTextureId);
        Assert.False(session.Ui.Find("wheel")!.Shown);
        Assert.Equal("nil", session.Lua.Evaluate(
            "return type(picker:GetColorWheelTexture())"));
    }

    [Fact]
    public void XmlSpecializedChildrenBecomeOwnedTexturesAndNativeGradients()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-colorselect-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "layout.xml"),
                "<Ui><ColorSelect name=\"XmlColorSelect\">" +
                "<ColorWheelTexture parentKey=\"Wheel\"><Size x=\"128\" y=\"128\"/></ColorWheelTexture>" +
                "<ColorWheelThumbTexture parentKey=\"WheelThumb\" file=\"thumb\"><Size x=\"10\" y=\"10\"/></ColorWheelThumbTexture>" +
                "<ColorValueTexture parentKey=\"Value\"><Size x=\"32\" y=\"128\"/></ColorValueTexture>" +
                "<ColorValueThumbTexture parentKey=\"ValueThumb\" file=\"thumb\"><Size x=\"48\" y=\"14\"/></ColorValueThumbTexture>" +
                "<ColorAlphaTexture parentKey=\"Alpha\"><Size x=\"32\" y=\"128\"/></ColorAlphaTexture>" +
                "<ColorAlphaThumbTexture parentKey=\"AlphaThumb\" file=\"thumb\"><Size x=\"48\" y=\"14\"/></ColorAlphaThumbTexture>" +
                "</ColorSelect></Ui>");
            File.WriteAllText(
                Path.Combine(root, "ColorSelectXml.toc"),
                "## Interface: 1\n## Title: ColorSelect XML\nlayout.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);
            var picker = session.Ui.Find("XmlColorSelect")!;
            var state = picker.ColorSelect!;

            Assert.Equal(6, picker.Children.Count);
            Assert.True(session.Ui.Find(state.WheelTextureId!.Value)!
                .Texture!.IsColorSelectWheel);
            Assert.Equal("OVERLAY", session.Ui.Find(state.WheelThumbTextureId!.Value)!.DrawLayer);
            Assert.Equal(
                ("VERTICAL", Vector4.UnitW, Vector4.One),
                session.Ui.Find(state.ValueTextureId!.Value)!.Texture!.Gradient);
            Assert.Equal(
                ("VERTICAL", new Vector4(1, 1, 1, 0), Vector4.One),
                session.Ui.Find(state.AlphaTextureId!.Value)!.Texture!.Gradient);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void PointerSelectionUsesOwnedTextureBoundsAndDefersCommitUntilTick()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "picker=CreateFrame('ColorSelect','PointerPicker',UIParent); " +
            "picker:SetSize(180,100); picker:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
            "wheel=picker:CreateTexture('PointerWheel'); wheel:SetSize(100,100); " +
            "wheel:SetPoint('BOTTOMLEFT',picker,'BOTTOMLEFT',0,0); " +
            "picker:SetColorWheelTexture(wheel); calls=0; " +
            "picker:SetScript('OnColorSelect',function() calls=calls+1 end)");

        session.MouseMove(100, 50);
        session.MouseButton("LeftButton", true);

        var picker = session.Ui.Find("PointerPicker")!;
        Assert.Equal(180, picker.ColorSelect!.Hue, 3);
        Assert.Equal(1, picker.ColorSelect.Saturation, 3);
        Assert.True(picker.ColorSelect.Dirty);
        Assert.Equal("0", session.Lua.Evaluate("return calls"));

        session.Tick(0);
        Assert.False(picker.ColorSelect.Dirty);
        Assert.Equal("1", session.Lua.Evaluate("return calls"));

        session.MouseButton("LeftButton", false);
        Assert.False(picker.ColorSelect.SelectingWheel);
    }

    private static Vector3 ReadRgb(EmulatorSession session)
    {
        var values = session.Lua.Evaluate(
            "local r,g,b=picker:GetColorRGB(); return table.concat({r,g,b},',')")
            .Split(',')
            .Select(value => float.Parse(
                value,
                System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
        return new Vector3(values[0], values[1], values[2]);
    }
}
