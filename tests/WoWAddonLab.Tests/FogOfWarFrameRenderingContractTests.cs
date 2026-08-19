using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class FogOfWarFrameRenderingContractTests
{
    [Fact]
    public void AssetSettersMaterializeTheNativeBackgroundAndThreeMasks()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "fog=CreateFrame('FogOfWarFrame','FogOwnedRegions',UIParent); " +
            "fog:SetFogOfWarBackgroundTexture(123,true,false); " +
            "fog:SetFogOfWarMaskTexture('Interface\\\\Minimap\\\\UI-Minimap-Mask')");

        var owner = session.Ui.Find("FogOwnedRegions")!;
        var fog = owner.FogOfWar!;
        var background = session.Ui.Find(fog.BackgroundTextureId!.Value)!;

        Assert.Equal(4, owner.Children.Count);
        Assert.Equal("Texture", background.ObjectType);
        Assert.Equal("ARTWORK", background.DrawLayer);
        Assert.Equal(owner.Id, background.AllPointsTargetId);
        Assert.Equal((uint)123, background.Texture!.FileDataId);
        Assert.True(background.Texture.HorizontallyTiled);
        Assert.False(background.Texture.VerticallyTiled);
        Assert.Equal(3, background.MaskTextureIds.Count);

        foreach (var maskId in fog.MaskTextureIds)
        {
            var mask = session.Ui.Find(maskId!.Value)!;
            Assert.Equal("MaskTexture", mask.ObjectType);
            Assert.Equal("ARTWORK", mask.DrawLayer);
            Assert.False(mask.Shown);
            Assert.Equal(
                "Interface\\Minimap\\UI-Minimap-Mask",
                mask.Texture!.Asset);
            Assert.Contains(mask.Id, background.MaskTextureIds);
        }
    }

    [Fact]
    public void SoloUpdateProjectsTheNativeTwoHundredWorldUnitRevealSquare()
    {
        using var session = new EmulatorSession
        {
            MapProvider = new LinearMapProvider()
        };
        session.Lua.Units.Player.Position =
            new WowUnitPositionState(500, 500, 0, 1);
        session.Lua.Evaluate(
            "fog=CreateFrame('FogOfWarFrame','FogProjection',UIParent); " +
            "fog:SetSize(400,200); " +
            "fog:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
            "fog:SetUiMapID(84); " +
            "fog:SetFogOfWarBackgroundTexture(123,false,false); " +
            "fog:SetFogOfWarMaskTexture(456)");

        session.Tick(0);

        var owner = session.Ui.Find("FogProjection")!;
        var fog = owner.FogOfWar!;
        var first = session.Ui.Find(fog.MaskTextureIds[0]!.Value)!;
        var second = session.Ui.Find(fog.MaskTextureIds[1]!.Value)!;
        var third = session.Ui.Find(fog.MaskTextureIds[2]!.Value)!;

        Assert.True(first.Shown);
        Assert.False(second.Shown);
        Assert.False(third.Shown);
        Assert.Equal(80, first.Width!.Value, 3);
        Assert.Equal(40, first.Height!.Value, 3);
        Assert.Equal(200, first.Anchors.Single().X, 3);
        Assert.Equal(100, first.Anchors.Single().Y, 3);

        session.Lua.Evaluate("fog:SetMaskScalar(0.5)");
        session.Tick(0);
        Assert.Equal(40, first.Width!.Value, 3);
        Assert.Equal(20, first.Height!.Value, 3);
    }

    [Fact]
    public void UiMapAssignmentWorldProjectionIsTheInverseOfUiToWorld()
    {
        var assignment = new WowMapAssignment(
            1,
            84,
            0,
            1,
            0,
            0.1,
            0.2,
            0.9,
            0.8,
            1000,
            200,
            0,
            800,
            100,
            0);

        Assert.True(WowMapHighlightGeometry.TryMapUiPositionToWorld(
            assignment,
            0.3,
            0.65,
            out var worldX,
            out var worldY));
        Assert.True(WowMapHighlightGeometry.TryProjectWorldPositionToUi(
            assignment,
            worldX,
            worldY,
            out var x,
            out var y));
        Assert.Equal(0.3, x, 8);
        Assert.Equal(0.65, y, 8);
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

            position = new WowMapPosition(worldX / 1000, worldY / 1000);
            return true;
        }
    }
}
