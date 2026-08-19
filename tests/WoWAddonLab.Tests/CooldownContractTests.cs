using System.Numerics;

namespace WoWAddonLab.Tests;

public sealed class CooldownContractTests
{
    private static readonly string[] NativeMethods =
    [
        "Clear", "GetCooldownDisplayDuration", "GetCooldownDuration",
        "GetCooldownTimes", "GetCountdownAbbrevThreshold",
        "GetCountdownFontString", "GetCountdownFormatter",
        "GetCountdownMillisecondsThreshold", "GetDrawBling", "GetDrawEdge",
        "GetDrawSwipe", "GetEdgeScale", "GetHideCountdownNumbers",
        "GetMinimumCountdownDuration", "GetReverse", "GetRotation",
        "GetUseAuraDisplayTime", "IsPaused", "Pause", "Resume",
        "SetBlingTexture", "SetCooldown", "SetCooldownDuration",
        "SetCooldownFromDurationObject", "SetCooldownFromExpirationTime",
        "SetCooldownUNIX", "SetCountdownAbbrevThreshold", "SetCountdownFont",
        "SetCountdownFormatter", "SetCountdownMillisecondsThreshold",
        "SetDrawBling", "SetDrawEdge", "SetDrawSwipe", "SetEdgeColor",
        "SetEdgeScale", "SetEdgeTexture", "SetHideCountdownNumbers",
        "SetMinimumCountdownDuration", "SetPaused", "SetReverse", "SetRotation",
        "SetSwipeColor", "SetSwipeTexture", "SetTexCoordRange",
        "SetUseAuraDisplayTime", "SetUseCircularEdge"
    ];

    [Fact]
    public void CooldownRegistersItsExactOwnedSurface()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeMethods.Length + ":" +
            string.Join(',', Enumerable.Repeat("function", NativeMethods.Length)),
            session.Lua.Evaluate(
                "local cooldown=CreateFrame('Cooldown'); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(cooldown[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void CooldownSwipeUsesTheNativeSquareRayAndReverseTopology()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local cooldown=CreateFrame('Cooldown','CooldownGeometry',UIParent); " +
            "cooldown:SetSize(100,100); " +
            "cooldown:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
            "cooldown:SetCooldown(1,8); cooldown:SetSwipeTexture(1,1,1,1,1)");

        var value = session.Ui.Find("CooldownGeometry")!;
        value.Cooldown!.ElapsedDisplayMilliseconds = 2_000;
        var remaining = session.Ui.ResolveCooldownSwipeVertices(value);

        Assert.Equal(100, remaining[1].Position.X, 3);
        Assert.Equal(50, remaining[1].Position.Y, 3);
        Assert.Equal(50, remaining[^1].Position.X, 3);
        Assert.Equal(100, remaining[^1].Position.Y, 3);
        Assert.Equal(1, remaining[1].Uv.X, 3);
        Assert.Equal(0.5f, remaining[1].Uv.Y, 3);
        Assert.Equal(0.5f, remaining[^1].Uv.X, 3);
        Assert.Equal(0, remaining[^1].Uv.Y, 3);

        value.Cooldown.Reverse = true;
        var elapsed = session.Ui.ResolveCooldownSwipeVertices(value);
        Assert.Equal(50, elapsed[1].Position.X, 3);
        Assert.Equal(100, elapsed[1].Position.Y, 3);
        Assert.Equal(100, elapsed[^1].Position.X, 3);
        Assert.Equal(50, elapsed[^1].Position.Y, 3);
    }

    [Fact]
    public void CooldownCompletionDispatchesOnceAndClearsWithoutBlingResource()
    {
        using var session = new EmulatorSession();
        session.Tick(0.25);
        session.Lua.Evaluate(
            "CooldownDoneCount=0; " +
            "local cooldown=CreateFrame('Cooldown','CompletingCooldown',UIParent); " +
            "cooldown:SetScript('OnCooldownDone',function() CooldownDoneCount=CooldownDoneCount+1 end); " +
            "cooldown:SetCooldownDuration(0.5)");

        session.Tick(0.25);
        session.Tick(0.25);
        session.Tick(0.25);

        Assert.Equal(
            "1:0:0",
            session.Lua.Evaluate(
                "local start,duration=CompletingCooldown:GetCooldownTimes(); " +
                "return CooldownDoneCount..':'..start..':'..duration"));
    }

    [Fact]
    public void CooldownEdgeAndBlingUseTheNativeRotatingQuadKeyframes()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local cooldown=CreateFrame('Cooldown','CooldownQuads',UIParent); " +
            "cooldown:SetSize(100,100); " +
            "cooldown:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
            "cooldown:SetCooldown(1,8)");

        var value = session.Ui.Find("CooldownQuads")!;
        value.Cooldown!.ElapsedDisplayMilliseconds = 0;
        var edge = session.Ui.ResolveCooldownEdgeQuad(value)!.Value;
        Assert.Equal(14.645f, edge.UpperLeft.X, 3);
        Assert.Equal(85.355f, edge.UpperLeft.Y, 3);
        Assert.Equal(85.355f, edge.LowerRight.X, 3);
        Assert.Equal(14.645f, edge.LowerRight.Y, 3);

        value.Cooldown.CompletionBlingActive = true;
        value.Cooldown.ElapsedDisplayMilliseconds = 500;
        var bling = session.Ui.ResolveCooldownBlingQuad(value)!.Value;
        Assert.Equal(0.75f, session.Ui.ResolveCooldownBlingAlpha(value), 3);
        Assert.Equal(
            62.893f,
            Vector2.Distance(bling.UpperLeft, bling.UpperRight),
            3);
    }

    [Fact]
    public void CooldownXmlResourcesPopulateNativeStateInsteadOfChildTextures()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-cooldown-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "layout.xml"),
                "<Ui><Cooldown name=\"XmlCooldown\" drawEdge=\"false\" " +
                "reverse=\"true\" rotation=\"180\" minimumCountdownDuration=\"7\">" +
                "<SwipeTexture><Color r=\"0\" g=\"0.5\" b=\"1\" a=\"0.8\"/></SwipeTexture>" +
                "<EdgeTexture file=\"Interface\\Cooldown\\Edge\"/>" +
                "<BlingTexture file=\"Interface\\Cooldown\\Bling\">" +
                "<Color r=\"0.3\" g=\"0.6\" b=\"1\" a=\"0.8\"/>" +
                "</BlingTexture></Cooldown></Ui>");
            File.WriteAllText(
                Path.Combine(root, "CooldownXml.toc"),
                "## Interface: 1\n## Title: Cooldown XML\nlayout.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);

            var value = session.Ui.Find("XmlCooldown")!;
            var cooldown = value.Cooldown!;
            Assert.False(cooldown.DrawEdge);
            Assert.True(cooldown.Reverse);
            Assert.Equal(MathF.PI, cooldown.Rotation, 3);
            Assert.Equal(7, cooldown.MinimumCountdownDurationMilliseconds);
            Assert.Equal((uint)0, cooldown.SwipeTextureFileDataId);
            Assert.Equal(0.50196f, cooldown.SwipeColor.Y, 4);
            Assert.Equal("Interface\\Cooldown\\Edge", cooldown.EdgeTextureAsset);
            Assert.Equal(Vector4.One, cooldown.EdgeColor);
            Assert.Equal("Interface\\Cooldown\\Bling", cooldown.BlingTextureAsset);
            Assert.Single(value.Children);
            Assert.NotNull(
                session.Ui.Find(value.Children[0])?.Font);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CooldownCountdownTextUsesNativeVisibilityAndThresholdBranches()
    {
        using var session = new EmulatorSession();
        session.Tick(0.25);
        session.Lua.Evaluate(
            "local cooldown=CreateFrame('Cooldown','CountdownText',UIParent); " +
            "cooldown:SetCooldownDuration(90)");
        session.Tick(0.1);

        var value = session.Ui.Find("CountdownText")!;
        var cooldown = value.Cooldown!;
        var fontString = session.Ui.Find(cooldown.CountdownFontStringId!.Value)!;
        Assert.True(fontString.Shown);
        Assert.Equal("1:30", fontString.Font!.Text);

        session.Lua.Evaluate(
            "CountdownText:SetCountdownMillisecondsThreshold(5); " +
            "CountdownText:SetCooldownDuration(3)");
        session.Tick(0.1);
        Assert.Equal("2.9", fontString.Font.Text);

        session.Lua.Evaluate("CountdownText:SetHideCountdownNumbers(true)");
        session.Tick(0.01);
        Assert.False(fontString.Shown);
    }
}
