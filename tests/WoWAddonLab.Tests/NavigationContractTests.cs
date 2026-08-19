using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class NavigationContractTests
{
    [Fact]
    public void RegistersExactSurfaceEnumsAndOptionalDefaultValues()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "6:0:1:nil:1:nil:1:0:1:false:1:false:" +
            "1:0:1:2:3:4:0:3",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_Navigation) do " +
                "count=count+1 end;" +
                "local distanceCount=select('#'," +
                "C_Navigation.GetDistance());" +
                "local frameCount=select('#',C_Navigation.GetFrame());" +
                "local frame=C_Navigation.GetFrame();" +
                "local partyCount=select('#'," +
                "C_Navigation.GetNearestPartyMemberToken());" +
                "local party=C_Navigation." +
                "GetNearestPartyMemberToken();" +
                "local stateCount=select('#'," +
                "C_Navigation.GetTargetState());" +
                "local validCount=select('#'," +
                "C_Navigation.HasValidScreenPosition());" +
                "local clampedCount=select('#'," +
                "C_Navigation.WasClampedToScreen());" +
                "return table.concat({" +
                "count,C_Navigation.GetDistance(),distanceCount," +
                "tostring(frame),frameCount,tostring(party),partyCount," +
                "C_Navigation.GetTargetState(),stateCount," +
                "tostring(C_Navigation.HasValidScreenPosition())," +
                "validCount,tostring(C_Navigation.WasClampedToScreen())," +
                "clampedCount,Enum.NavigationState.Invalid," +
                "Enum.NavigationState.Occluded," +
                "Enum.NavigationState.InRange," +
                "Enum.NavigationState.Disabled," +
                "Enum.NavigationStateMeta.NumValues," +
                "Enum.NavigationStateMeta.MinValue," +
                "Enum.NavigationStateMeta.MaxValue},':')"));
    }

    [Fact]
    public void ProjectsNativeFloatFrameTokenStateAndScreenValidity()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "CreateFrame('Frame','NavigationBinaryFrame',UIParent)");
        var frame = session.Ui.Find("NavigationBinaryFrame")!;
        var navigation = session.Lua.Navigation;

        navigation.Distance = 16_777_217d;
        navigation.FrameId = frame.Id;
        navigation.NearestPartyMemberToken = "party2";
        navigation.TargetState = 2;
        navigation.ScreenPositionX = 0.0004f;
        navigation.ScreenPositionY = 0;
        navigation.UseComputedScreenPositionValidity();
        navigation.WasClampedToScreen = true;

        Assert.Equal(
            "16777216:true:party2:2:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "C_Navigation.GetDistance()," +
                "tostring(C_Navigation.GetFrame()==" +
                "NavigationBinaryFrame)," +
                "C_Navigation.GetNearestPartyMemberToken()," +
                "C_Navigation.GetTargetState()," +
                "tostring(C_Navigation.HasValidScreenPosition())," +
                "tostring(C_Navigation.WasClampedToScreen())},':')"));

        navigation.TargetState = 99;
        navigation.ScreenPositionX = 0.0005f;

        Assert.Equal(
            "0:true",
            session.Lua.Evaluate(
                "return C_Navigation.GetTargetState()..':'.." +
                "tostring(C_Navigation.HasValidScreenPosition())"));
    }

    [Fact]
    public void NavigationEventsPreserveRecoveredPayloadShapes()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "ready",
            session.Lua.Evaluate(
                "NavigationEvents={};" +
                "NavigationEventFrame=CreateFrame('Frame');" +
                "NavigationCreatedFrame=CreateFrame(" +
                "'Frame','NavigationCreatedFrame');" +
                "for _,event in ipairs({" +
                "'NAVIGATION_DESTINATION_REACHED'," +
                "'NAVIGATION_FRAME_CREATED'," +
                "'NAVIGATION_FRAME_DESTROYED'}) do " +
                "NavigationEventFrame:RegisterEvent(event) end;" +
                "NavigationEventFrame:SetScript(" +
                "'OnEvent',function(_,event,value) " +
                "NavigationEvents[#NavigationEvents+1]=" +
                "event..':'..tostring(value==" +
                "NavigationCreatedFrame or value) end);" +
                "return 'ready'"));

        session.Lua.TriggerEvent(
            "NAVIGATION_DESTINATION_REACHED",
            true);
        session.Lua.TriggerEvent(
            "NAVIGATION_FRAME_CREATED",
            session.Ui.Find("NavigationCreatedFrame"));
        session.Lua.TriggerEvent("NAVIGATION_FRAME_DESTROYED");

        Assert.Equal(
            "NAVIGATION_DESTINATION_REACHED:true|" +
            "NAVIGATION_FRAME_CREATED:true|" +
            "NAVIGATION_FRAME_DESTROYED:nil",
            session.Lua.Evaluate(
                "return table.concat(NavigationEvents,'|')"));
    }
}
