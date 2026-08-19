using System.Numerics;
using WoWAddonLab.Emulator.Lua;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class BlobFrameRenderingContractTests
{
    [Fact]
    public void UnsmoothBoundaryBuildsTheNativeFillFanAndBorderStrip()
    {
        using var session = CreateSession();
        session.Lua.Evaluate(
            "blob=CreateFrame('QuestPOIFrame','BlobGeometry',UIParent); " +
            "blob:SetSize(400,200); " +
            "blob:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
            "blob:SetMapID(84); blob:EnableSmoothing(false); " +
            "blob:SetFillTexture('fill'); blob:SetBorderTexture('border'); " +
            "blob:DrawBlob(7,true)");

        var owner = session.Ui.Find("BlobGeometry")!;
        owner.Blob!.Areas.Add(new UiBlobArea(
            7,
            3,
            1,
            [
                new Vector2(100, 100),
                new Vector2(900, 100),
                new Vector2(900, 900),
                new Vector2(100, 900)
            ]));

        var mesh = Assert.Single(
            UiBlobGeometry.Build(session.Ui, owner, session.MapProvider));

        Assert.Collection(
            mesh.Boundary,
            point => AssertPoint(point, 40, 180),
            point => AssertPoint(point, 360, 180),
            point => AssertPoint(point, 360, 20),
            point => AssertPoint(point, 40, 20));
        Assert.Equal(5, mesh.FillVertices.Count);
        Assert.Equal(new Vector2(200, 100), mesh.FillVertices[^1]);
        Assert.Equal(Vector2.Zero, mesh.FillUvs[^1]);
        Assert.Equal<ushort>(
            [0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4],
            mesh.FillIndices);
        Assert.Equal(8, mesh.BorderVertices.Count);
        Assert.Equal<ushort>(
            [0, 1, 2, 3, 4, 5, 6, 7, 0, 1],
            mesh.BorderIndices);
    }

    [Fact]
    public void SmoothingUsesTwentyChordLengthCatmullRomSamplesByDefault()
    {
        using var session = CreateSession();
        session.Lua.Evaluate(
            "blob=CreateFrame('QuestPOIFrame','SmoothedBlob',UIParent); " +
            "blob:SetSize(100,100); blob:SetMapID(84); " +
            "blob:DrawBlob(11,true)");

        var owner = session.Ui.Find("SmoothedBlob")!;
        owner.Blob!.Areas.Add(new UiBlobArea(
            11,
            0,
            1,
            [
                new Vector2(0, 0),
                new Vector2(1000, 0),
                new Vector2(1000, 1000),
                new Vector2(0, 1000)
            ]));

        var mesh = Assert.Single(
            UiBlobGeometry.Build(session.Ui, owner, session.MapProvider));

        Assert.Equal(20, mesh.Boundary.Count);
        Assert.Equal(new Vector2(0, 100), mesh.Boundary[0]);
        Assert.Contains(mesh.Boundary, point => point.X > 100);
    }

    [Fact]
    public void MergeThresholdSuppressesTheContainedSmallerObjective()
    {
        using var session = CreateSession();
        session.Lua.Evaluate(
            "blob=CreateFrame('QuestPOIFrame','MergedBlob',UIParent); " +
            "blob:SetSize(100,100); blob:SetMapID(84); " +
            "blob:EnableSmoothing(false); " +
            "blob:DrawBlob(1,true); blob:DrawBlob(2,true)");

        var owner = session.Ui.Find("MergedBlob")!;
        owner.Blob!.Areas.Add(new UiBlobArea(
            1,
            0,
            1,
            Square(0, 1000),
            MergeGroupId: 9));
        owner.Blob.Areas.Add(new UiBlobArea(
            2,
            1,
            1,
            Square(300, 700),
            MergeGroupId: 9));

        var meshes = UiBlobGeometry.Build(
            session.Ui,
            owner,
            session.MapProvider);

        Assert.Equal(2, meshes.Count);
        Assert.True(meshes.Single(mesh => mesh.BlobId == 1).IsVisible);
        Assert.False(meshes.Single(mesh => mesh.BlobId == 2).IsVisible);
    }

    [Fact]
    public void MouseOverUsesTheRenderedPolygonAndReturnsObjectiveIndices()
    {
        using var session = CreateSession();
        session.Lua.Evaluate(
            "blob=CreateFrame('QuestPOIFrame','HitBlob',UIParent); " +
            "blob:SetSize(100,100); blob:SetMapID(84); " +
            "blob:EnableSmoothing(false); blob:DrawBlob(77,true)");
        var owner = session.Ui.Find("HitBlob")!;
        owner.Blob!.Areas.Add(new UiBlobArea(
            77,
            6,
            1,
            Square(100, 900)));

        Assert.Equal(
            "77:1:6",
            session.Lua.Evaluate(
                "local id,count=blob:UpdateMouseOverTooltip(.5,.5); " +
                "return id..':'..count..':'..blob:GetTooltipIndex(1)"));
        Assert.Equal(
            "true:true",
            session.Lua.Evaluate(
                "local id,count=blob:UpdateMouseOverTooltip(1.5,.5); " +
                "return tostring(id==nil)..':'..tostring(count==nil)"));
    }

    [Fact]
    public void BlobDerivedTypesExposeTheNativeBaseIdentityAndSurface()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:function:function",
            session.Lua.Evaluate(
                "local quest=CreateFrame('QuestPOIFrame',nil,UIParent); " +
                "local scenario=CreateFrame('ScenarioPOIFrame',nil,UIParent); " +
                "local archaeology=CreateFrame('ArchaeologyDigSiteFrame',nil,UIParent); " +
                "return table.concat({" +
                "tostring(quest:IsObjectType('BlobFrame'))," +
                "tostring(scenario:IsObjectType('BlobFrame'))," +
                "tostring(archaeology:IsObjectType('BlobFrame'))," +
                "type(archaeology.SetMapID),type(archaeology.EnableSmoothing)},':')"));
    }

    private static EmulatorSession CreateSession() =>
        new()
        {
            MapProvider = new LinearMapProvider()
        };

    private static IReadOnlyList<Vector2> Square(float minimum, float maximum) =>
    [
        new Vector2(minimum, minimum),
        new Vector2(maximum, minimum),
        new Vector2(maximum, maximum),
        new Vector2(minimum, maximum)
    ];

    private static void AssertPoint(Vector2 actual, float x, float y)
    {
        Assert.Equal(x, actual.X, 3);
        Assert.Equal(y, actual.Y, 3);
    }

    private sealed class LinearMapProvider : IWowMapProvider
    {
        public bool TryGetMapDetails(int mapId, out WowMapDetails details)
        {
            details = default;
            return false;
        }

        public bool TryGetMapArt(int mapId, out WowMapArt art)
        {
            art = null!;
            return false;
        }

        public bool TryProjectWorldPosition(
            int uiMapId,
            int worldMapId,
            double worldX,
            double worldY,
            out WowMapPosition position)
        {
            if (uiMapId != 84 || worldMapId != 1)
            {
                position = default;
                return false;
            }

            position = new WowMapPosition(
                worldX / 1000,
                worldY / 1000);
            return true;
        }
    }
}
