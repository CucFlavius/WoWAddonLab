using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class CameraCompatibilityContractTests
{
    [Fact]
    public void CameraMovementGlobalsFollowRecoveredArgumentsAndState()
    {
        using var session = new EmulatorSession();
        session.Lua.Camera.Zoom = 10;

        Assert.Equal(
            "0:7:0:8:0:0:true:true:false:false",
            session.Lua.Evaluate(
                "local zin=select('#',CameraZoomIn(3));" +
                "local afterIn=GetCameraZoom();" +
                "local zout=select('#',CameraZoomOut(false));" +
                "local afterOut=GetCameraZoom();" +
                "local start=select('#',MoveViewRightStart('1.25','0.5',0));" +
                "local stop=select('#',MoveViewRightStop('ignored'));" +
                "return table.concat({zin,afterIn,zout,afterOut,start,stop," +
                "tostring(not pcall(MoveViewRightStart,-1))," +
                "tostring(not pcall(MoveViewRightStart,1,-1))," +
                "tostring(pcall(MoveViewRightStart,{}))," +
                "tostring(pcall(MoveViewRightStart,1,{}))},':')"));

        Assert.DoesNotContain(
            WowCameraMovementDirection.Right,
            session.Lua.Camera.ActiveMovements.Keys);
        Assert.Contains(
            WowCameraMovementDirection.Out,
            session.Lua.Camera.ActiveMovements.Keys);
    }

    [Fact]
    public void EncounterTimelineViewTypeAndShoulderCVarUseClientDefaults()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:1:0:0",
            session.Lua.Evaluate(
                "return table.concat({C_EncounterTimeline.GetViewType()," +
                "select('#',C_EncounterTimeline.GetViewType('ignored'))," +
                "C_CVar.GetCVar('test_cameraOverShoulder')," +
                "C_CVar.GetCVarDefault('test_cameraOverShoulder')},':')"));
    }
}
