using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class FontStringRenderingContractTests
{
    [Fact]
    public void LuaStringWidthUsesTheSelectedTrueTypeGlyphAdvances()
    {
        var fontPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            "arial.ttf");
        Assert.True(File.Exists(fontPath));
        var bytes = File.ReadAllBytes(fontPath);
        Assert.True(TrueTypeAdvanceMetrics.TryRead(bytes, out var metrics));

        using var session = new EmulatorSession();
        session.FontAssetReader = _ => bytes;
        var result = session.Lua.Evaluate(
            "local text=UIParent:CreateFontString(); " +
            "text:SetFont('Fonts\\\\TEST.TTF', 12, ''); " +
            "text:SetText('iii'); local narrow=text:GetStringWidth(); " +
            "text:SetText('WWW'); local wide=text:GetStringWidth(); " +
            "text:SetWidth(wide); " +
            "return narrow..'|'..wide..'|'..tostring(text:IsTruncated())");
        var values = result.Split('|');

        var narrow = float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture);
        var wide = float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(wide > narrow * 2);
        Assert.Equal(
            metrics!.MeasureAdvance("WWW", 12),
            wide,
            3);
        Assert.Equal("false", values[2]);
    }

    [Fact]
    public void WidthDerivedFromStringWidthDoesNotFalselyWrapProportionalText()
    {
        var fontPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            "arial.ttf");
        Assert.True(File.Exists(fontPath));
        var bytes = File.ReadAllBytes(fontPath);

        using var session = new EmulatorSession();
        session.FontAssetReader = _ => bytes;
        var result = session.Lua.Evaluate(
            "local text=UIParent:CreateFontString(); " +
            "text:SetFont('Fonts\\\\TEST.TTF', 14, ''); " +
            "text:SetHeight(20); text:SetText('Adventurer of the Dawn'); " +
            "local unbounded=text:GetUnboundedStringWidth(); " +
            "text:SetWidth(unbounded + 4); " +
            "return text:GetNumLines()..'|'..tostring(text:IsTruncated())..'|'.." +
            "text:GetStringWidth()..'|'..unbounded");
        var values = result.Split('|');

        Assert.Equal("1", values[0]);
        Assert.Equal("false", values[1]);
        Assert.Equal(
            float.Parse(values[3], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture),
            3);
    }

    [Fact]
    public void RotationUsesNativeTextBlockOriginToleranceAndScreenTransform()
    {
        Assert.False(UiTextBlockRotation.IsActive(0.000999f));
        Assert.True(UiTextBlockRotation.IsActive(0.001f));
        Assert.True(UiTextBlockRotation.IsActive(-0.001f));

        var origin = new Vector2(100, 200);
        var rotated = UiTextBlockRotation.RotateScreenPoint(
            new Vector2(110, 200),
            origin,
            MathF.PI / 2);

        Assert.Equal(100, rotated.X, 4);
        Assert.Equal(190, rotated.Y, 4);
        Assert.Equal(
            origin,
            UiTextBlockRotation.RotateScreenPoint(origin, origin, 1.25f));
    }

    [Fact]
    public void AlphaGradientUsesDrawableQuadIndicesAndByteSteps()
    {
        const ushort start = 1;
        const ushort length = 2;

        Assert.Equal(
            new UiTextQuadAlpha(255, 255),
            UiTextAlphaGradient.ResolveQuadAlpha(start, length, 0, 255));
        Assert.Equal(
            new UiTextQuadAlpha(255, 128),
            UiTextAlphaGradient.ResolveQuadAlpha(start, length, 1, 255));
        Assert.Equal(
            new UiTextQuadAlpha(128, 1),
            UiTextAlphaGradient.ResolveQuadAlpha(start, length, 2, 255));
        Assert.Equal(
            new UiTextQuadAlpha(1, 1),
            UiTextAlphaGradient.ResolveQuadAlpha(start, length, 3, 255));
        Assert.True(UiTextAlphaGradient.ContainsQuad(start, length, 2));
        Assert.False(UiTextAlphaGradient.ContainsQuad(2, length, 2));
        Assert.False(UiTextAlphaGradient.IsActive(ushort.MaxValue, length));
    }

    [Fact]
    public void ShadowRequiresANonzeroStoredOffset()
    {
        var opaque = Vector4.One;

        Assert.False(UiTextShadow.IsVisible(Vector2.Zero, opaque));
        Assert.True(UiTextShadow.IsVisible(new Vector2(0, -1), opaque));
        Assert.False(UiTextShadow.IsVisible(
            new Vector2(1, 1),
            new Vector4(1, 1, 1, 0)));
        Assert.Equal(0.25f, UiTextShadow.BoundAlpha(0.75f, 0.25f));
        Assert.Equal(0.25f, UiTextShadow.BoundAlpha(0.25f, 0.75f));
    }

    [Fact]
    public void LineSpacingUsesNativePositiveCeilTolerance()
    {
        Assert.Equal(0, UiTextLineMetrics.QuantizePhysicalSpacing(-1));
        Assert.Equal(0, UiTextLineMetrics.QuantizePhysicalSpacing(0));
        Assert.Equal(0, UiTextLineMetrics.QuantizePhysicalSpacing(0.00004f));
        Assert.Equal(1, UiTextLineMetrics.QuantizePhysicalSpacing(0.00006f));
        Assert.Equal(1, UiTextLineMetrics.QuantizePhysicalSpacing(1));
        Assert.Equal(1, UiTextLineMetrics.QuantizePhysicalSpacing(1.00004f));
        Assert.Equal(2, UiTextLineMetrics.QuantizePhysicalSpacing(1.00006f));
    }

    [Fact]
    public void LogicalLineSpacingUsesRegionScaleButNotTextScale()
    {
        Assert.Equal(
            3.2f,
            UiTextLineMetrics.ResolveLogicalSpacing(
                3,
                900,
                768f / 900 * 1.25f),
            4);
        Assert.Equal(
            0,
            UiTextLineMetrics.ResolveLogicalSpacing(0.00004f, 768, 1));
        Assert.Equal(
            1,
            UiTextLineMetrics.ResolveLogicalSpacing(0.00006f, 768, 1));
        Assert.Equal(
            0,
            UiTextLineMetrics.ResolveLogicalSpacing(3, 768, 0));
    }

    [Fact]
    public void PositiveShadowWidthUsesNativePixelCeilAndRegionScale()
    {
        Assert.Equal(
            2.4f,
            UiTextLineMetrics.ResolveLogicalPositiveShadowWidth(
                2.1f,
                900,
                768f / 900 * 1.25f),
            4);
        Assert.Equal(
            0,
            UiTextLineMetrics.ResolveLogicalPositiveShadowWidth(
                0,
                768,
                1));
        Assert.Equal(
            0,
            UiTextLineMetrics.ResolveLogicalPositiveShadowWidth(
                -2,
                768,
                1));
        Assert.Equal(
            0,
            UiTextLineMetrics.ResolveLogicalPositiveShadowWidth(
                2,
                768,
            0));
    }

    [Fact]
    public void IndentedWordWrapUsesAFixedFifteenPhysicalPixels()
    {
        Assert.Equal(15, UiTextLineMetrics.IndentedWordWrapPixels);
        Assert.Equal(
            12,
            UiTextLineMetrics.ResolveLogicalIndentedWordWrapWidth(
                900,
                768f / 900 * 1.25f),
            4);
        Assert.Equal(
            15,
            UiTextLineMetrics.ResolveLogicalIndentedWordWrapWidth(
                768,
                1));
        Assert.Equal(
            0,
            UiTextLineMetrics.ResolveLogicalIndentedWordWrapWidth(
                768,
            0));
    }

    [Fact]
    public void TextBlockOriginUsesNativeBottomUpFloorInTopDownCoordinates()
    {
        Assert.Equal(
            new Vector2(10, 21),
            UiTextLineMetrics.SnapTopDownPhysicalOrigin(
                new Vector2(10.8f, 20.2f),
                false));
        Assert.Equal(
            new Vector2(10.8f, 20.2f),
            UiTextLineMetrics.SnapTopDownPhysicalOrigin(
                new Vector2(10.8f, 20.2f),
                true));
    }

    [Fact]
    public void TextBlockOriginSnapsInFramebufferPixelsAtDesktopDpiScale()
    {
        Assert.Equal(
            new Vector2(12, 34.4f),
            UiTextLineMetrics.SnapTopDownPhysicalOrigin(
                new Vector2(12.63f, 34.01f),
                false,
                new Vector2(1.25f)));
        Assert.Equal(
            new Vector2(12.63f, 34.01f),
            UiTextLineMetrics.SnapTopDownPhysicalOrigin(
                new Vector2(12.63f, 34.01f),
                true,
                new Vector2(1.25f)));
    }

    [Fact]
    public void TextBlockMetricsUseSignedNearestQuantizationUnlessSmooth()
    {
        Assert.Equal(4, UiTextLineMetrics.QuantizeSignedPhysicalMetric(4.49f, false));
        Assert.Equal(5, UiTextLineMetrics.QuantizeSignedPhysicalMetric(4.5f, false));
        Assert.Equal(-4, UiTextLineMetrics.QuantizeSignedPhysicalMetric(-4.49f, false));
        Assert.Equal(-5, UiTextLineMetrics.QuantizeSignedPhysicalMetric(-4.5f, false));
        Assert.Equal(-4.5f, UiTextLineMetrics.QuantizeSignedPhysicalMetric(-4.5f, true));
    }

    [Fact]
    public void JustificationUsesBlockAnchorAndIndependentlyQuantizedLineOffsets()
    {
        Assert.Equal(
            0,
            UiTextLineMetrics.ResolvePhysicalHorizontalAnchorOffset("LEFT", 101));
        Assert.Equal(
            50.5f,
            UiTextLineMetrics.ResolvePhysicalHorizontalAnchorOffset("CENTER", 101));
        Assert.Equal(
            101,
            UiTextLineMetrics.ResolvePhysicalHorizontalAnchorOffset("RIGHT", 101));

        Assert.Equal(
            0,
            UiTextLineMetrics.ResolvePhysicalLineOffset("LEFT", 11, false, false));
        Assert.Equal(
            -6,
            UiTextLineMetrics.ResolvePhysicalLineOffset("CENTER", 11, false, false));
        Assert.Equal(
            -11,
            UiTextLineMetrics.ResolvePhysicalLineOffset("RIGHT", 10.5f, false, false));
        Assert.Equal(
            15,
            UiTextLineMetrics.ResolvePhysicalLineOffset("LEFT", 11, false, true));
        Assert.Equal(
            -27,
            UiTextLineMetrics.ResolvePhysicalLineOffset("RIGHT", 12.4f, false, true));
        Assert.Equal(
            -5.25f,
            UiTextLineMetrics.ResolvePhysicalLineOffset("CENTER", 10.5f, true, true));
    }

    [Fact]
    public void IndentedWordWrapReducesEveryContinuationLineCapacity()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "2:3",
            session.Lua.Evaluate(
                "local text=UIParent:CreateFontString(); " +
                "text:SetFont('Fonts\\\\FRIZQT__.TTF', 12, ''); " +
                "text:SetWidth(65); text:SetNonSpaceWrap(true); " +
                "text:SetText(string.rep('x', 20)); " +
                "local regular=text:GetNumLines(); " +
                "text:SetIndentedWordWrap(true); " +
                "return regular..':'..text:GetNumLines()"));
    }

    [Fact]
    public void DisplayTextFittingUsesNativeBufferAndSafeEllipsisBoundaries()
    {
        var shortResult = UiDisplayTextFitter.Resolve(
            "abcd",
            candidate =>
                candidate.EndsWith("...", StringComparison.Ordinal) &&
                WowTextMarkup.PlainText(candidate).Length <= 5);
        Assert.Equal("ab...", shortResult.Text);
        Assert.True(shortResult.WasTruncated);

        var utf8Result = UiDisplayTextFitter.Resolve(
            "a😀bc",
            candidate =>
                candidate.EndsWith("...", StringComparison.Ordinal) &&
                WowTextMarkup.PlainText(candidate).Length <= 5);
        Assert.Equal("a...", utf8Result.Text);
        Assert.True(utf8Result.WasTruncated);

        var inlineResult = UiDisplayTextFitter.Resolve(
            "a|TInterface\\Icons\\INV_Misc_QuestionMark:16bc",
            candidate =>
                candidate.EndsWith("...", StringComparison.Ordinal) &&
                WowTextMarkup.PlainText(candidate).Length <= 5);
        Assert.Equal("a...", inlineResult.Text);
        Assert.True(inlineResult.WasTruncated);

        var fitting = UiDisplayTextFitter.Resolve("fits", _ => true);
        Assert.Equal("fits", fitting.Text);
        Assert.False(fitting.WasTruncated);

        var oversized = UiDisplayTextFitter.Resolve(
            new string('x', UiDisplayTextFitter.NativeTextByteCapacity + 100),
            candidate => Encoding.UTF8.GetByteCount(candidate) <=
                         UiDisplayTextFitter.NativeBufferCapacity);
        Assert.True(oversized.WasTruncated);
        Assert.Equal(
            UiDisplayTextFitter.NativeBufferCapacity,
            Encoding.UTF8.GetByteCount(oversized.Text));
    }

    [Fact]
    public void LineHeightUsesNativeFontPixelSelectionAndScaleConversion()
    {
        Assert.Equal(
            10,
            UiTextLineMetrics.ResolveLogicalLineHeight(20, 0.5f, 900, 768f / 900));
        Assert.Equal(
            10.4f,
            UiTextLineMetrics.ResolveLogicalLineHeight(
                20,
                0.5f,
                900,
                768f / 900 * 1.25f),
            4);
        Assert.Equal(
            2,
            UiTextLineMetrics.ResolveLogicalLineHeight(0.1f, 1, 768, 1));
        Assert.Equal(
            256,
            UiTextLineMetrics.ResolveLogicalLineHeight(300, 1, 768, 1));
        Assert.Equal(
            0,
            UiTextLineMetrics.ResolveLogicalLineHeight(20, 1, 768, 0));
    }

    [Fact]
    public void SmoothScalingSeparatesRasterSelectionFromDisplayHeight()
    {
        Assert.Equal(
            13,
            UiTextLineMetrics.ResolvePhysicalRasterHeight(20, 0.5f, 1.25f));
        Assert.Equal(
            13,
            UiTextLineMetrics.ResolvePhysicalRenderHeight(
                20,
                0.5f,
                1.25f,
                false));
        Assert.Equal(
            25,
            UiTextLineMetrics.ResolvePhysicalRenderHeight(
                20,
                0.5f,
                1.25f,
                true));
        Assert.Equal(
            20,
            UiTextLineMetrics.ResolveLogicalLineHeight(
                20,
                0.5f,
                900,
                768f / 900 * 1.25f,
                true));
        Assert.Equal(
            2,
            UiTextLineMetrics.ResolvePhysicalRenderHeight(
                0.1f,
                1,
                1,
                true));
        Assert.Equal(
            300,
            UiTextLineMetrics.ResolvePhysicalRenderHeight(
                300,
                1,
                1,
                true));
        Assert.Equal(
            256,
            UiTextLineMetrics.ResolvePhysicalRasterHeight(300, 1, 1));
    }
}
