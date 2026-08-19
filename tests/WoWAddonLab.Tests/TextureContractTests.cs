using System.Numerics;
using WoWAddonLab.Rendering;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class TextureContractTests
{
    private static readonly string[] NativeTextureMethods =
    [
        "ClearTextureSlice", "ClearVertexOffsets", "GetAtlas", "GetBlendMode",
        "GetDesaturation", "GetHorizTile", "GetRotation", "GetTexCoord",
        "GetTexelSnappingBias", "GetTexture", "GetTextureFileID",
        "GetTextureFilePath", "GetTextureSliceMargins", "GetTextureSliceMode",
        "GetVertTile", "GetVertexOffset", "IsBlockingLoadRequested",
        "IsDesaturated", "IsSnappingToPixelGrid", "ResetTexCoord", "SetAtlas",
        "SetBlendMode", "SetBlockingLoadRequested", "SetColorTexture",
        "SetDesaturated", "SetDesaturation", "SetGradient", "SetHorizTile",
        "SetMask", "SetRotation", "SetSnapToPixelGrid", "SetSpriteSheetCell",
        "SetTexCoord", "SetTexelSnappingBias", "SetTexture",
        "SetTextureSliceMargins", "SetTextureSliceMode", "SetVertTile",
        "SetVertexOffset"
    ];

    private static readonly string[] NativeMaskedTextureMethods =
    [
        "AddMaskTexture", "GetMaskTexture", "GetNumMaskTextures",
        "RemoveMaskTexture"
    ];

    [Fact]
    public void TextureExposesEveryMethodInTheRecoveredNativeRegistrars()
    {
        using var session = new EmulatorSession();
        var methods = NativeTextureMethods.Concat(NativeMaskedTextureMethods).ToArray();
        var literal = string.Join(',', methods.Select(value => $"'{value}'"));

        Assert.Equal(
            methods.Length + ":" +
            string.Join(',', Enumerable.Repeat("function", methods.Length)),
            session.Lua.Evaluate(
                "local texture=UIParent:CreateTexture('TextureBinarySurface'); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(texture[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void TextureBlendModesUseRecoveredNativeRenderTargetStates()
    {
        Assert.Equal(
            new UiTextureBlendState(
                0, false,
                UiBlendFactor.One, UiBlendFactor.Zero,
                UiBlendFactor.One, UiBlendFactor.Zero),
            UiTextureBlendState.Resolve(UiTextureBlendMode.Disable));
        Assert.Equal(
            new UiTextureBlendState(
                1, false,
                UiBlendFactor.One, UiBlendFactor.Zero,
                UiBlendFactor.One, UiBlendFactor.Zero),
            UiTextureBlendState.Resolve(UiTextureBlendMode.AlphaKey));
        Assert.Equal(
            new UiTextureBlendState(
                2, true,
                UiBlendFactor.SourceAlpha, UiBlendFactor.OneMinusSourceAlpha,
                UiBlendFactor.One, UiBlendFactor.OneMinusSourceAlpha),
            UiTextureBlendState.Resolve(UiTextureBlendMode.Blend));
        Assert.Equal(
            new UiTextureBlendState(
                3, true,
                UiBlendFactor.SourceAlpha, UiBlendFactor.One,
                UiBlendFactor.Zero, UiBlendFactor.One),
            UiTextureBlendState.Resolve(UiTextureBlendMode.Add));
        Assert.Equal(
            new UiTextureBlendState(
                4, true,
                UiBlendFactor.DestinationColor, UiBlendFactor.Zero,
                UiBlendFactor.DestinationAlpha, UiBlendFactor.Zero),
            UiTextureBlendState.Resolve(UiTextureBlendMode.Mod));
    }

    [Fact]
    public void FullTextureDesaturationDoesNotOverflowIntoTheBlendModeTag()
    {
        var textureId = WowImGuiController.TextureId(0x1234, "BLEND", 1);
        var raw = unchecked((ulong)(long)textureId);

        Assert.Equal(0x1234u, (uint)raw);
        Assert.Equal(0x8FFFFFFFu, (uint)(raw >> 32));
    }

    [Fact]
    public void TextureSamplerModesUseRecoveredPackedSamplerState()
    {
        Assert.Equal(0, (byte)UiTextureWrapMode.Clamp);
        Assert.Equal(1, (byte)UiTextureWrapMode.Repeat);
        Assert.Equal(2, (byte)UiTextureWrapMode.ClampToBlack);
        Assert.Equal(3, (byte)UiTextureWrapMode.ClampToBlackAdditive);
        Assert.Equal(4, (byte)UiTextureWrapMode.ClampToWhite);
        Assert.Equal(5, (byte)UiTextureWrapMode.Mirror);
        Assert.Equal(0, (byte)UiTextureFilterMode.Nearest);
        Assert.Equal(1, (byte)UiTextureFilterMode.Linear);
        Assert.Equal(4, (byte)UiTextureFilterMode.Trilinear);

        Assert.Equal(
            UiTextureAddressMode.Clamp,
            UiTextureSamplerState.Resolve(
                UiTextureFilterMode.Linear,
                UiTextureWrapMode.Clamp,
                UiTextureWrapMode.Clamp).AddressU);
        Assert.Equal(
            UiTextureAddressMode.Repeat,
            UiTextureSamplerState.Resolve(
                UiTextureFilterMode.Linear,
                UiTextureWrapMode.Repeat,
                UiTextureWrapMode.Clamp).AddressU);
        Assert.Equal(
            UiTextureAddressMode.Mirror,
            UiTextureSamplerState.Resolve(
                UiTextureFilterMode.Linear,
                UiTextureWrapMode.Mirror,
                UiTextureWrapMode.Clamp).AddressU);

        var opaqueBlack = UiTextureSamplerState.Resolve(
            UiTextureFilterMode.Linear,
            UiTextureWrapMode.ClampToBlack,
            UiTextureWrapMode.Clamp);
        Assert.Equal(UiTextureAddressMode.Border, opaqueBlack.AddressU);
        Assert.Equal(UiTextureBorderColor.OpaqueBlack, opaqueBlack.BorderColor);

        var transparentBlack = UiTextureSamplerState.Resolve(
            UiTextureFilterMode.Linear,
            UiTextureWrapMode.ClampToBlackAdditive,
            UiTextureWrapMode.Clamp);
        Assert.Equal(UiTextureAddressMode.Border, transparentBlack.AddressU);
        Assert.Equal(
            UiTextureBorderColor.TransparentBlack,
            transparentBlack.BorderColor);

        var white = UiTextureSamplerState.Resolve(
            UiTextureFilterMode.Linear,
            UiTextureWrapMode.ClampToWhite,
            UiTextureWrapMode.Clamp);
        Assert.Equal(UiTextureAddressMode.Border, white.AddressU);
        Assert.Equal(UiTextureBorderColor.White, white.BorderColor);

        var horizontalWins = UiTextureSamplerState.Resolve(
            UiTextureFilterMode.Linear,
            UiTextureWrapMode.ClampToBlack,
            UiTextureWrapMode.ClampToWhite);
        Assert.Equal(UiTextureAddressMode.Border, horizontalWins.AddressU);
        Assert.Equal(UiTextureAddressMode.Border, horizontalWins.AddressV);
        Assert.Equal(
            UiTextureBorderColor.OpaqueBlack,
            horizontalWins.BorderColor);

        var nearest = UiTextureSamplerState.Resolve(
            UiTextureFilterMode.Nearest,
            UiTextureWrapMode.Clamp,
            UiTextureWrapMode.Clamp);
        Assert.False(nearest.MinLinear);
        Assert.False(nearest.MagLinear);
        Assert.False(nearest.MipLinear);
        Assert.False(nearest.UsesMipmaps);

        var linear = UiTextureSamplerState.Resolve(
            UiTextureFilterMode.Linear,
            UiTextureWrapMode.Clamp,
            UiTextureWrapMode.Clamp);
        Assert.True(linear.MinLinear);
        Assert.True(linear.MagLinear);
        Assert.False(linear.MipLinear);
        Assert.False(linear.UsesMipmaps);

        var trilinear = UiTextureSamplerState.Resolve(
            UiTextureFilterMode.Trilinear,
            UiTextureWrapMode.Clamp,
            UiTextureWrapMode.Clamp);
        Assert.True(trilinear.MinLinear);
        Assert.True(trilinear.MagLinear);
        Assert.True(trilinear.MipLinear);
        Assert.True(trilinear.UsesMipmaps);
    }

    [Fact]
    public void TextureSamplerAddressingMatchesNativeBackendModes()
    {
        AssertAddress(-1, 4, UiTextureAddressMode.Clamp, 0);
        AssertAddress(4, 4, UiTextureAddressMode.Clamp, 3);

        AssertAddress(-1, 4, UiTextureAddressMode.Repeat, 3);
        AssertAddress(4, 4, UiTextureAddressMode.Repeat, 0);
        AssertAddress(5, 4, UiTextureAddressMode.Repeat, 1);

        AssertAddress(-2, 4, UiTextureAddressMode.Mirror, 1);
        AssertAddress(-1, 4, UiTextureAddressMode.Mirror, 0);
        AssertAddress(4, 4, UiTextureAddressMode.Mirror, 3);
        AssertAddress(5, 4, UiTextureAddressMode.Mirror, 2);

        Assert.False(
            UiTextureSamplerState.TryAddressTexel(
                -1,
                4,
                UiTextureAddressMode.Border,
                out _));
        Assert.False(
            UiTextureSamplerState.TryAddressTexel(
                4,
                4,
                UiTextureAddressMode.Border,
                out _));
        AssertAddress(2, 4, UiTextureAddressMode.Border, 2);

        Assert.Equal(
            new Vector4(0, 0, 0, 1),
            UiTextureSamplerState.Resolve(
                UiTextureFilterMode.Linear,
                UiTextureWrapMode.ClampToBlack,
                UiTextureWrapMode.Clamp).BorderRgba);
        Assert.Equal(
            Vector4.Zero,
            UiTextureSamplerState.Resolve(
                UiTextureFilterMode.Linear,
                UiTextureWrapMode.ClampToBlackAdditive,
                UiTextureWrapMode.Clamp).BorderRgba);
    }

    [Fact]
    public void TextureMaskShaderTransformPreservesProjectedCoordinatesAfterSnapping()
    {
        var maskUpperLeft = new Vector2(40, 20);
        var maskUpperRight = new Vector2(120, 60);
        var maskLowerLeft = new Vector2(20, 60);
        var subjectUpperLeft = new Vector2(52.25f, 31.75f);
        var subjectUpperRight = new Vector2(101.5f, 56.25f);
        var subjectLowerLeft = new Vector2(39.75f, 71.5f);
        var subjectUpperLeftScreen = new Vector2(152, 232);
        var subjectUpperRightScreen = new Vector2(251, 281);
        var subjectLowerLeftScreen = new Vector2(127, 311);
        Vector2 MaskUv(Vector2 point) =>
            UiTextureMaskShaderTransform.InterpolateUv(
                [
                    new Vector2(.1f, .2f),
                    new Vector2(.1f, .8f),
                    new Vector2(.9f, .2f),
                    new Vector2(.9f, .8f)
                ],
                UiTextureMaskShaderTransform.ProjectIntoQuad(
                    maskUpperLeft,
                    maskUpperRight,
                    maskLowerLeft,
                    point));

        Assert.True(
            UiTextureMaskShaderTransform.TryResolve(
                subjectUpperLeftScreen,
                subjectUpperRightScreen,
                subjectLowerLeftScreen,
                MaskUv(subjectUpperLeft),
                MaskUv(subjectUpperRight),
                MaskUv(subjectLowerLeft),
                out var transform));

        AssertVectorNear(MaskUv(subjectUpperLeft), transform.Transform(subjectUpperLeftScreen));
        AssertVectorNear(MaskUv(subjectUpperRight), transform.Transform(subjectUpperRightScreen));
        AssertVectorNear(MaskUv(subjectLowerLeft), transform.Transform(subjectLowerLeftScreen));
    }

    [Fact]
    public void TextureSliceParametersUseRecoveredNativeFitAndTilingRules()
    {
        var expanded = UiTextureSliceShaderParameters.Resolve(
            new Vector2(300, 200),
            new Vector2(100, 80),
            new UiInsets(10, 10, 8, 8),
            UiTextureSliceMode.Tiled);

        Assert.Equal(new Vector2(300, 200), expanded.DestinationSize);
        Assert.Equal(new Vector2(3.5f, 2.875f), expanded.CenterRepeat);
        Assert.Equal(10f / 300, expanded.DestinationLeft, 6);
        Assert.Equal(1 - 10f / 300, expanded.DestinationRight, 6);
        Assert.Equal(8f / 200, expanded.DestinationTop, 6);
        Assert.Equal(1 - 8f / 200, expanded.DestinationBottom, 6);
        Assert.Equal(.1f, expanded.SourceLeft, 6);
        Assert.Equal(.9f, expanded.SourceRight, 6);
        Assert.Equal(.1f, expanded.SourceTop, 6);
        Assert.Equal(.9f, expanded.SourceBottom, 6);
        Assert.Equal(.005f, expanded.HalfTexelX, 6);
        Assert.Equal(.00625f, expanded.HalfTexelY, 6);

        var narrow = UiTextureSliceShaderParameters.Resolve(
            new Vector2(80, 200),
            new Vector2(100, 80),
            new UiInsets(10, 10, 8, 8),
            UiTextureSliceMode.Stretched);

        Assert.Equal(0, narrow.Margins.Left);
        Assert.Equal(0, narrow.Margins.Right);
        Assert.Equal(8, narrow.Margins.Top);
        Assert.Equal(8, narrow.Margins.Bottom);
        Assert.Equal(80, narrow.DestinationSize.X);
        Assert.Equal(250, narrow.DestinationSize.Y);
        Assert.Equal(1, narrow.CenterRepeat.X);
        Assert.Equal(3.65625f, narrow.CenterRepeat.Y, 6);
        Assert.Equal(.032f, narrow.DestinationTop, 6);
        Assert.Equal(.968f, narrow.DestinationBottom, 6);

        var smallerOnBothAxes = UiTextureSliceShaderParameters.Resolve(
            new Vector2(80, 70),
            new Vector2(100, 80),
            new UiInsets(10, 10, 8, 8),
            UiTextureSliceMode.Tiled);
        Assert.Equal(new UiInsets(0, 0, 0, 0), smallerOnBothAxes.Margins);
        Assert.Equal(Vector2.One, smallerOnBothAxes.CenterRepeat);
    }

    private static void AssertAddress(
        int coordinate,
        int extent,
        UiTextureAddressMode mode,
        int expected)
    {
        Assert.True(
            UiTextureSamplerState.TryAddressTexel(
                coordinate,
                extent,
                mode,
                out var actual));
        Assert.Equal(expected, actual);
    }

    private static void AssertVectorNear(Vector2 expected, Vector2 actual)
    {
        Assert.InRange(actual.X, expected.X - 0.0001f, expected.X + 0.0001f);
        Assert.InRange(actual.Y, expected.Y - 0.0001f, expected.Y + 0.0001f);
    }

    [Fact]
    public void TextureVertexBlockingAndDesaturationStateMatchesNativeCallbacks()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "3.5:-2.25:0:0:true:false:true:false:0:0",
            session.Lua.Evaluate(
                "local texture=UIParent:CreateTexture('TextureRecoveredState'); " +
                "texture:SetVertexOffset(2,3.5,-2.25); " +
                "local x,y=texture:GetVertexOffset(2); " +
                "local ox,oy=texture:GetVertexOffset(5); " +
                "texture:SetBlockingLoadRequested(true); " +
                "local blocking=texture:IsBlockingLoadRequested(); " +
                "texture:SetBlockingLoadRequested(); " +
                "texture:SetDesaturation(.25); " +
                "local desaturated=texture:IsDesaturated(); " +
                "texture:SetDesaturated(); " +
                "local cleared=texture:IsDesaturated(); " +
                "texture:ClearVertexOffsets(); " +
                "local cx,cy=texture:GetVertexOffset(2); " +
                "return table.concat({x,y,ox,oy,tostring(blocking)," +
                "tostring(texture:IsBlockingLoadRequested()),tostring(desaturated)," +
                "tostring(cleared),cx,cy},':')"));
    }

    [Fact]
    public void SpriteSheetCellUsesZeroBasedGridAndOptionalPixelDimensions()
    {
        using var session = new EmulatorSession();
        var texture = session.Ui.Find(
            session.Lua.Evaluate(
                "SpriteTexture=UIParent:CreateTexture('SpriteTexture'); " +
                "return SpriteTexture:GetName()"))!;
        texture.Texture!.IntrinsicWidth = 80;
        texture.Texture.IntrinsicHeight = 40;

        Assert.Equal(
            "0.625:0.000:0.750:0.250:0.625:0.000:0.750:0.125",
            session.Lua.Evaluate(
                "SpriteTexture:SetSpriteSheetCell(5,4,8); " +
                "local x1,y1,_,_,_,_,x2,y2=SpriteTexture:GetTexCoord(); " +
                "SpriteTexture:SetSpriteSheetCell(5,4,8,10,5); " +
                "local px1,py1,_,_,_,_,px2,py2=SpriteTexture:GetTexCoord(); " +
                "return string.format('%.3f:%.3f:%.3f:%.3f:%.3f:%.3f:%.3f:%.3f'," +
                "x1,y1,x2,y2,px1,py1,px2,py2)"));

        var before = texture.Texture.LocalUv.ToArray();
        session.Lua.Evaluate("SpriteTexture:SetSpriteSheetCell(2,0,8)");
        Assert.Equal(before, texture.Texture.LocalUv);
    }

    [Fact]
    public void TextureSetToDefaultsRestoresNativeTextureStateButRetainsAttachedMasks()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "nil:nil:BLEND:0:false:false:false:false:false:true:0.3:" +
            "0.0:0.0:1.0:1.0:0:0:1:0.0:0.5:1.0:1.0",
            session.Lua.Evaluate(
                "local texture=UIParent:CreateTexture('TextureDefaults'); " +
                "local mask=UIParent:CreateMaskTexture('TextureDefaultsMask'); " +
                "texture:AddMaskTexture(mask); texture:SetTexture(123); " +
                "texture:SetBlendMode('ADD'); texture:SetBlockingLoadRequested(true); " +
                "texture:SetDesaturation(.5); texture:SetHorizTile(true); " +
                "texture:SetVertTile(true); texture:SetSnapToPixelGrid(false); " +
                "texture:SetTexelSnappingBias(.8); texture:SetRotation(1,{x=.2,y=.3}); " +
                "texture:SetTexCoord(.1,.9,.2,.8); texture:SetVertexOffset(1,4,5); " +
                "texture:SetVertexColor(.2,.3,.4,.5); texture:SetMask('legacy-mask'); " +
                "texture:SetTextureSliceMargins(1,2,3,4); texture:SetTextureSliceMode(1); " +
                "texture:SetToDefaults(); " +
                "local ux,uy,_,_,_,_,lx,ly=texture:GetTexCoord(); " +
                "local ox,oy=texture:GetVertexOffset(1); " +
                "local radians,pivot=texture:GetRotation(); " +
                "local r,g,b,a=texture:GetVertexColor(); " +
                "return table.concat({tostring(texture:GetTexture())," +
                "tostring(texture:GetAtlas()),texture:GetBlendMode()," +
                "texture:GetDesaturation(),tostring(texture:IsDesaturated())," +
                "tostring(texture:IsBlockingLoadRequested())," +
                "tostring(texture:GetHorizTile()),tostring(texture:GetVertTile())," +
                "tostring(not texture:IsSnappingToPixelGrid())," +
                "tostring(texture:IsSnappingToPixelGrid())," +
                "string.format('%.1f',texture:GetTexelSnappingBias())," +
                "string.format('%.1f',ux),string.format('%.1f',uy)," +
                "string.format('%.1f',lx),string.format('%.1f',ly),ox,oy," +
                "texture:GetNumMaskTextures(),string.format('%.1f',radians)," +
                "string.format('%.1f',pivot.x),string.format('%.1f',r)," +
                "string.format('%.1f',a)},':')"));
    }
}
