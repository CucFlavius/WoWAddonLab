using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class MinimapContractTests
{
    [Fact]
    public void TrackingNamespaceUsesNativeTablesMasksAndArgumentContracts()
    {
        using var session = new EmulatorSession();
        session.Lua.Minimap.CanTrackBattlePets = true;
        session.Lua.Minimap.ShouldUseHybridMinimap = true;
        session.Lua.Minimap.Tracking.Add(new WowMinimapTrackingState
        {
            Name = "Fixed A",
            Texture = 101,
            Active = true,
            DefaultActive = true,
            Type = "other",
            SubType = 7,
            Filter = 1
        });
        session.Lua.Minimap.Tracking.Add(new WowMinimapTrackingState
        {
            Name = "Fixed B",
            Texture = 102,
            Active = false,
            DefaultActive = true,
            Type = "other",
            SubType = 8,
            Filter = 2
        });
        session.Lua.Minimap.Tracking.Add(new WowMinimapTrackingState
        {
            Name = "Spell",
            Texture = 103,
            Active = false,
            DefaultActive = false,
            Type = "spell",
            SubType = 9,
            SpellId = 2383,
            Filter = 4
        });

        Assert.Equal(
            "9:true:true:3:true:true:true:false:false:nil:1:2383:nil:" +
            "Fixed A:101:true:other:7:nil:true:true:false:0:true:false:" +
            "false:false:false:false:false:false",
            session.Lua.Evaluate(
                "(function() local count=0; for _ in pairs(C_Minimap) do count=count+1 end; " +
                "local fixed=C_Minimap.GetTrackingFilter('1.9'); " +
                "local spell=C_Minimap.GetTrackingFilter(3); " +
                "local empty=C_Minimap.GetTrackingFilter(0); " +
                "local info=C_Minimap.GetTrackingInfo(1); " +
                "local missing=C_Minimap.GetTrackingInfo(99); " +
                "local filteredTwo=C_Minimap.IsFilteredOut(2); " +
                "local filteredThree=C_Minimap.IsFilteredOut(3); " +
                "local filteredZero=C_Minimap.IsFilteredOut(0); " +
                "local setReturns=select('#',C_Minimap.SetTracking('2.9',0)); " +
                "local second=C_Minimap.GetTrackingInfo(2); " +
                "local badFilter=pcall(C_Minimap.IsFilteredOut); " +
                "local negativeFilter=pcall(C_Minimap.IsFilteredOut,-1); " +
                "local largeFilter=pcall(C_Minimap.GetDefaultTrackingValue,8388608); " +
                "local badIndex=pcall(C_Minimap.GetTrackingInfo,{}); " +
                "local negativeIndex=pcall(C_Minimap.GetTrackingInfo,-1); " +
                "local missingOn=pcall(C_Minimap.SetTracking,1); " +
                "C_Minimap.ClearAllTracking(); " +
                "local cleared=C_Minimap.GetTrackingInfo(1); " +
                "return table.concat({count," +
                "tostring(C_Minimap.CanTrackBattlePets())," +
                "tostring(C_Minimap.ShouldUseHybridMinimap())," +
                "C_Minimap.GetNumTrackingTypes()," +
                "tostring(C_Minimap.GetDefaultTrackingValue(0))," +
                "tostring(C_Minimap.GetDefaultTrackingValue(3))," +
                "tostring(filteredTwo),tostring(filteredThree)," +
                "tostring(filteredZero)," +
                "tostring(fixed.spellID),fixed.filterID," +
                "spell.spellID,tostring(spell.filterID)," +
                "info.name,info.texture,tostring(info.active),info.type," +
                "info.subType,tostring(info.spellID)," +
                "tostring(missing==nil),tostring(next(empty)==nil)," +
                "tostring(second==nil),setReturns,tostring(second.active)," +
                "tostring(cleared.active),tostring(badFilter)," +
                "tostring(negativeFilter),tostring(largeFilter)," +
                "tostring(badIndex),tostring(negativeIndex)," +
                "tostring(missingOn)},':') end)()"));
    }

    [Fact]
    public void PingLocationStoresAWorldPositionAndGetPingTracksPlayerMovement()
    {
        using var session = new EmulatorSession();
        session.Lua.Units.Player.Position =
            new WowUnitPositionState(1000, 2000, 0, 84);

        Assert.Equal(
            "0.500:-0.250",
            session.Lua.Evaluate(
                "Minimap:PingLocation(70,-35); " +
                "local x,y=Minimap:GetPingPosition(); " +
                "return string.format('%.3f:%.3f',x,y)"));

        var minimap = session.Ui.Find("Minimap")!.Minimap!;
        Assert.True(minimap.HasPingWorldPosition);
        Assert.True(minimap.PingActive);
        Assert.Equal(5, minimap.PingDuration);
        Assert.Equal(84, minimap.PingWorldMapId);
        Assert.Equal(833.3333f, minimap.PingWorldX, 3);
        Assert.Equal(1666.6666f, minimap.PingWorldY, 3);

        session.Lua.Units.Player.Position =
            new WowUnitPositionState(1100, 1900, 0, 84);
        Assert.Equal(
            "0.350:-0.400",
            session.Lua.Evaluate(
                "local x,y=Minimap:GetPingPosition(); " +
                "return string.format('%.3f:%.3f',x,y)"));
    }

    [Fact]
    public void PingLocationEmitsNormalizedCoordinatesAndExpiresAfterFiveSeconds()
    {
        using var session = new EmulatorSession();
        session.Lua.Units.Player.Position =
            new WowUnitPositionState(1000, 2000, 0, 84);
        session.Lua.Evaluate(
            "listener=CreateFrame('Frame'); listener:RegisterEvent('MINIMAP_PING'); " +
            "listener:SetScript('OnEvent',function(self,event,unit,x,y) " +
            "self.unit=unit; self.x=x; self.y=y end); " +
            "Minimap:PingLocation(-35,28)");

        Assert.Equal(
            "player:-0.250:0.200",
            session.Lua.Evaluate(
                "return table.concat({listener.unit,string.format('%.3f',listener.x)," +
                "string.format('%.3f',listener.y)},':')"));

        var minimap = session.Ui.Find("Minimap")!.Minimap!;
        for (var index = 0; index < 20; index++)
            session.Tick(0.25);
        Assert.True(minimap.PingActive);
        Assert.Equal(5, minimap.PingElapsed, 3);

        session.Tick(0.25);
        Assert.False(minimap.PingActive);
        Assert.Equal(5.25f, minimap.PingElapsed, 3);
    }

    [Fact]
    public void PingLocationDoesNothingUntilPlayerWorldPositionIsAvailable()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "pingCount=0; local listener=CreateFrame('Frame'); " +
            "listener:RegisterEvent('MINIMAP_PING'); " +
            "listener:SetScript('OnEvent',function() pingCount=pingCount+1 end); " +
            "Minimap:PingLocation(10,20)");

        var minimap = session.Ui.Find("Minimap")!.Minimap!;
        Assert.False(minimap.HasPingWorldPosition);
        Assert.False(minimap.PingActive);
        Assert.Equal("0:0:0", session.Lua.Evaluate(
            "local x,y=Minimap:GetPingPosition(); " +
            "return table.concat({pingCount,x,y},':')"));
    }

    [Fact]
    public void ZoomIsSharedClientStateAndUsesTheRecoveredStandardRadiusTable()
    {
        using var session = new EmulatorSession();
        session.Lua.Units.Player.Position =
            new WowUnitPositionState(1000, 2000, 0, 84);

        session.Lua.Evaluate("Minimap:SetZoom(5); Minimap:PingLocation(70,0)");
        var minimap = session.Ui.Find("Minimap")!.Minimap!;
        Assert.Equal(1900, minimap.PingWorldY, 3);
        Assert.Equal(5, session.Lua.Minimap.Zoom);

        minimap.Zoom = 0;
        Assert.Equal("5", session.Lua.Evaluate("return Minimap:GetZoom()"));
    }
}
