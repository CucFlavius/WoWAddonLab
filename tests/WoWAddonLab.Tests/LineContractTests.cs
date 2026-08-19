namespace WoWAddonLab.Tests;

public sealed class LineContractTests
{
    private static readonly string[] NativeLineAndTextureMethods =
    [
        "ClearAllPoints", "GetEndPoint", "GetHitRectThickness", "GetStartPoint",
        "GetThickness", "SetEndPoint", "SetHitRectThickness", "SetStartPoint",
        "SetThickness",
        "AddMaskTexture", "GetMaskTexture", "GetNumMaskTextures", "RemoveMaskTexture",
        "ClearTextureSlice", "ClearVertexOffsets", "GetAtlas", "GetBlendMode",
        "GetDesaturation", "GetHorizTile", "GetRotation", "GetTexCoord", "GetTexture",
        "GetTextureFileID", "GetTextureFilePath", "GetTextureSliceMargins",
        "GetTextureSliceMode", "GetTexelSnappingBias", "GetVertTile",
        "GetVertexColor", "GetVertexOffset", "IsBlockingLoadRequested",
        "IsDesaturated", "IsSnappingToPixelGrid", "SetAtlas", "SetBlendMode",
        "SetBlockingLoadRequested", "SetColorTexture", "SetDesaturated",
        "SetDesaturation", "SetGradient", "SetHorizTile", "ResetTexCoord", "SetMask",
        "SetRotation", "SetSnapToPixelGrid", "SetSpriteSheetCell", "SetTexCoord",
        "SetTexelSnappingBias", "SetTexture", "SetTextureSliceMargins",
        "SetTextureSliceMode", "SetVertTile", "SetVertexColor", "SetVertexOffset"
    ];

    [Fact]
    public void LineRegistersItsExactOwnedMethodsAndTheFullTextureBaseSurface()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeLineAndTextureMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeLineAndTextureMethods.Length + ":" +
            string.Join(
                ',',
                Enumerable.Repeat("function", NativeLineAndTextureMethods.Length)),
            session.Lua.Evaluate(
                "local line=UIParent:CreateLine('LineBinarySurface'); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(line[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void LineInheritsTextureResetWhilePreservingLineOwnedGeometry()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local owner=CreateFrame('Frame','LineResetOwner',UIParent); " +
            "local line=owner:CreateLine('LineResetTarget'); " +
            "line:SetStartPoint('TOPLEFT',owner,3,4); " +
            "line:SetEndPoint('BOTTOMRIGHT',owner,-5,-6); " +
            "line:SetThickness(7); line:SetHitRectThickness(11); " +
            "line:SetTexture(123,true,true,'NEAREST'); " +
            "line:SetBlendMode('ADD'); line:SetDesaturation(.75); " +
            "line:SetRotation(.5); line:SetSnapToPixelGrid(false); " +
            "line:SetTexelSnappingBias(-.25); line:SetBlockingLoadRequested(true); " +
            "line:SetTexCoord(.1,.8,.2,.9); line:SetVertexOffset(1,8,9); " +
            "line:SetToDefaults()");

        Assert.Equal(
            "TOPLEFT:true:3:4:BOTTOMRIGHT:true:-5:-6:7:11",
            session.Lua.Evaluate(
                "local line=LineResetTarget; " +
                "local sp,sr,sx,sy=line:GetStartPoint(); " +
                "local ep,er,ex,ey=line:GetEndPoint(); " +
                "return table.concat({sp,tostring(sr==LineResetOwner),sx,sy," +
                "ep,tostring(er==LineResetOwner),ex,ey," +
                "line:GetThickness(),line:GetHitRectThickness()},':')"));

        var texture = session.Ui.Find("LineResetTarget")!.Line!.Texture;
        Assert.Null(texture.Asset);
        Assert.Null(texture.FileDataId);
        Assert.Null(texture.AtlasName);
        Assert.False(texture.IsColor);
        Assert.Equal(System.Numerics.Vector4.One, texture.Color);
        Assert.Equal(System.Numerics.Vector4.One, texture.VertexColor);
        Assert.Equal("BLEND", texture.BlendMode);
        Assert.Equal("CLAMP", texture.WrapHorizontal);
        Assert.Equal("CLAMP", texture.WrapVertical);
        Assert.Equal("LINEAR", texture.FilterMode);
        Assert.False(texture.BlockingLoadRequested);
        Assert.True(texture.SnapToPixelGrid);
        Assert.Equal(.3f, texture.TexelSnappingBias);
        Assert.False(texture.HorizontallyTiled);
        Assert.False(texture.VerticallyTiled);
        Assert.Equal(0f, texture.Desaturation);
        Assert.Equal(0f, texture.Rotation);
        Assert.Equal(new System.Numerics.Vector2(.5f), texture.RotationPoint);
        Assert.All(texture.VertexOffsets, offset => Assert.Equal(default, offset));
        Assert.Equal(new System.Numerics.Vector2(0, 0), texture.LocalUv[0]);
        Assert.Equal(new System.Numerics.Vector2(0, 1), texture.LocalUv[1]);
        Assert.Equal(new System.Numerics.Vector2(1, 0), texture.LocalUv[2]);
        Assert.Equal(new System.Numerics.Vector2(1, 1), texture.LocalUv[3]);
    }

    [Fact]
    public void LineEndpointsUseTheNativeZeroOrFourResultShape()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:TOPLEFT:true:3.0:4.0:BOTTOMRIGHT:true:5.0:6.0:TOPLEFT:3.0:4.0",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame','LineEndpointOwner',UIParent); " +
                "local line=owner:CreateLine(); " +
                "local empty=select('#',line:GetStartPoint()); " +
                "line:SetStartPoint('TOPLEFT',owner,3,4); " +
                "line:SetEndPoint('BOTTOMRIGHT',nil,5,6); " +
                "local sp,sr,sx,sy=line:GetStartPoint(); " +
                "local ep,er,ex,ey=line:GetEndPoint(); " +
                "line:SetStartPoint('NOT_A_POINT',owner,9,9); " +
                "local retainedPoint,_,retainedX,retainedY=line:GetStartPoint(); " +
                "return string.format('%d:%s:%s:%.1f:%.1f:%s:%s:%.1f:%.1f:%s:%.1f:%.1f'," +
                "empty,sp,tostring(sr==owner),sx,sy,ep,tostring(er==nil),ex,ey," +
                "retainedPoint,retainedX,retainedY)"));
    }

    [Fact]
    public void LineVertexOffsetsRotateWithTheLineDirection()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local owner=CreateFrame('Frame','LineOffsetOwner',UIParent); " +
            "owner:SetSize(200,200); owner:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
            "local line=owner:CreateLine('LineOffsetTarget'); " +
            "line:SetStartPoint('BOTTOMLEFT',owner,0,0); " +
            "line:SetEndPoint('TOPRIGHT',owner,0,0); line:SetThickness(10)");

        var line = session.Ui.Find("LineOffsetTarget")!;
        var baseline = session.Ui.ResolveLineQuad(line);
        var unit = MathF.Sqrt(0.5f);

        line.Line!.Texture.VertexOffsets[0] = new System.Numerics.Vector2(10, 0);
        line.Line.Texture.VertexOffsets[1] = new System.Numerics.Vector2(0, 10);
        session.Ui.InvalidateLayout();
        var adjusted = session.Ui.ResolveLineQuad(line);

        Assert.Equal(10 * unit, adjusted[0].X - baseline[0].X, 4);
        Assert.Equal(10 * unit, adjusted[0].Y - baseline[0].Y, 4);
        Assert.Equal(-10 * unit, adjusted[1].X - baseline[1].X, 4);
        Assert.Equal(10 * unit, adjusted[1].Y - baseline[1].Y, 4);
    }
}
