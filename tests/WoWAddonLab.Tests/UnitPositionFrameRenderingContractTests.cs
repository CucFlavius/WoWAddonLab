using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class UnitPositionFrameRenderingContractTests
{
    [Fact]
    public void FinalizeMaterializesAndProjectsTheNativeUnitTexture()
    {
        using var session = new EmulatorSession
        {
            MapProvider = new LinearMapProvider()
        };
        session.Lua.Units.Player.Position =
            new WowUnitPositionState(250, 750, 0, 1, 0.75f);

        session.Lua.Evaluate(
            "units=CreateFrame('UnitPositionFrame','UnitProjection',UIParent); " +
            "units:SetSize(400,200); " +
            "units:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
            "units:SetUiMapID(84); " +
            "units:AddUnit('player','Interface\\\\Icons\\\\INV_Misc_Map_01'," +
            "20,30,1,.5,.25,1,3,true); " +
            "units:FinalizeUnits()");

        var owner = session.Ui.Find("UnitProjection")!;
        var entry = owner.UnitPosition!.Units["player"];
        var texture = session.Ui.Find(entry.TextureId!.Value)!;

        Assert.Single(owner.Children);
        Assert.Equal("Texture", texture.ObjectType);
        Assert.Equal("ARTWORK", texture.DrawLayer);
        Assert.Equal(3, texture.SubLevel);
        Assert.Equal(20, texture.Width);
        Assert.Equal(30, texture.Height);
        Assert.Equal(100, texture.Anchors.Single().X, 3);
        Assert.Equal(50, texture.Anchors.Single().Y, 3);
        Assert.Equal(0.75f, texture.Texture!.Rotation, 3);
        Assert.Equal(128 / 255f, texture.Texture.VertexColor.Y, 5);
        Assert.Equal(64 / 255f, texture.Texture.VertexColor.Z, 5);
        Assert.True(texture.Shown);

        session.MouseMove(100, 50);
        session.Tick(0);
        Assert.Equal("player", session.Lua.Evaluate(
            "return units:GetMouseOverUnits()"));
    }

    [Fact]
    public void ClearUnitsReturnsTheTextureToTheNativeStylePool()
    {
        using var session = new EmulatorSession
        {
            MapProvider = new LinearMapProvider()
        };
        session.Lua.Units.Player.Position =
            new WowUnitPositionState(500, 500, 0, 1);
        session.Lua.Evaluate(
            "units=CreateFrame('UnitPositionFrame','UnitPooling',UIParent); " +
            "units:SetSize(100,100); units:SetUiMapID(84); " +
            "units:AddUnit('player','first',10,10); units:FinalizeUnits()");

        var owner = session.Ui.Find("UnitPooling")!;
        var firstId = owner.UnitPosition!.Units["player"].TextureId;
        session.Lua.Evaluate(
            "units:ClearUnits(); " +
            "units:AddUnit('player','second',12,13); units:FinalizeUnits()");

        Assert.Single(owner.Children);
        Assert.Equal(firstId, owner.UnitPosition.Units["player"].TextureId);
        var texture = session.Ui.Find(firstId!.Value)!;
        Assert.Equal("second", texture.Texture!.Asset);
        Assert.Equal(12, texture.Width);
        Assert.Equal(13, texture.Height);
        Assert.True(texture.Shown);
    }

    [Fact]
    public void PlayerPingUsesThreeTexturesAndNativeFadeAndOuterRotation()
    {
        using var session = new EmulatorSession
        {
            MapProvider = new LinearMapProvider()
        };
        session.Lua.Units.Player.Position =
            new WowUnitPositionState(500, 500, 0, 1);
        session.Lua.Evaluate(
            "ping=CreateFrame('UnitPositionFrame','UnitPing',UIParent); " +
            "ping:SetSize(400,200); ping:SetUiMapID(84); " +
            "ping:SetPlayerPingScale(.5); " +
            "ping:SetPlayerPingTexture(0,'center',32,32); " +
            "ping:SetPlayerPingTexture(2,'outer',64,64); " +
            "ping:StartPlayerPing(.5,.5)");

        var owner = session.Ui.Find("UnitPing")!;
        var state = owner.UnitPosition!;
        Assert.Equal(3, owner.Children.Count);
        Assert.All(state.PlayerPingTextureIds, id => Assert.NotNull(id));

        session.Tick(0.25);
        session.Tick(0.25);
        session.Tick(0.25);

        var center = session.Ui.Find(state.PlayerPingTextureIds[0]!.Value)!;
        var middle = session.Ui.Find(state.PlayerPingTextureIds[1]!.Value)!;
        var outer = session.Ui.Find(state.PlayerPingTextureIds[2]!.Value)!;
        Assert.True(center.Shown);
        Assert.True(middle.Shown);
        Assert.True(outer.Shown);
        Assert.Equal(0.5f, center.Scale);
        Assert.Equal(128f / 255f, center.Alpha, 5);
        Assert.Equal(400, center.Anchors.Single().X, 3);
        Assert.Equal(200, center.Anchors.Single().Y, 3);
        Assert.Equal(-MathF.PI, outer.Texture!.Rotation, 3);

        session.Tick(0.25);
        Assert.False(state.PlayerPingActive);
        Assert.All(
            state.PlayerPingTextureIds,
            id => Assert.False(session.Ui.Find(id!.Value)!.Shown));
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
