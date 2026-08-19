namespace WoWAddonLab.Tests;

public sealed class ModelSceneContractTests
{
    [Fact]
    public void ConstructorUsesNativeModelSceneLightingDefaults()
    {
        using var session = new EmulatorSession();

        var scene = session.Ui.Create("ModelScene", "NativeLightDefaults", null)
            .ModelScene!;

        Assert.Equal(new System.Numerics.Vector3(0.7f), scene.AmbientLight);
        Assert.Equal(new System.Numerics.Vector3(0.7f), scene.AmbientLightSecondary);
        Assert.Equal(new System.Numerics.Vector3(0.7f), scene.AmbientLightTertiary);
        Assert.Equal(new System.Numerics.Vector3(0.8f, 0.8f, 0.64f), scene.DiffuseLight);
        Assert.Equal(System.Numerics.Vector3.UnitY, scene.LightDirection);
        Assert.Equal(0, scene.LightType);
        Assert.True(scene.LightVisible);
        Assert.Equal(MathF.PI * .3f, scene.FieldOfView);
        Assert.Equal(.2f, scene.NearClip);
        Assert.Equal(100, scene.FarClip);
    }

    [Fact]
    public void NativeFortySevenMethodSurfaceIsAdvertised()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "47:0",
            session.Lua.Evaluate(
                "local scene=CreateFrame('ModelScene',nil,UIParent); " +
                "local methods={" +
                "'ClearFog','CreateActor','GetActorAtIndex','GetAllowOverlappedModels'," +
                "'GetCameraFarClip','GetCameraFieldOfView','GetCameraForward'," +
                "'GetCameraNearClip','GetCameraPosition','GetCameraRight','GetCameraUp'," +
                "'GetDrawLayer','GetFogColor','GetFogFar','GetFogNear'," +
                "'GetLightAmbientColor','GetLightDiffuseColor','GetLightDirection'," +
                "'GetLightPosition','GetLightType','GetNumActors','GetViewInsets'," +
                "'GetViewTranslation','IsLightVisible','Project3DPointTo2D'," +
                "'SetAllowOverlappedModels','SetCameraFarClip','SetCameraFieldOfView'," +
                "'SetCameraNearClip','SetCameraOrientationByAxisVectors'," +
                "'SetCameraOrientationByYawPitchRoll','SetCameraPosition'," +
                "'SetDesaturation','SetDrawLayer','SetFogColor','SetFogFar','SetFogNear'," +
                "'SetLightAmbientColor','SetLightDiffuseColor','SetLightDirection'," +
                "'SetLightPosition','SetLightType','SetLightVisible','SetPaused'," +
                "'SetViewInsets','SetViewTranslation','TakeActor'}; " +
                "local missing=0; for _,name in ipairs(methods) do " +
                "if type(scene[name])~='function' then missing=missing+1 end end; " +
                "return #methods..':'..missing"));
    }

    [Fact]
    public void ActorEnumerationAndTakeActorUseTheNativeOwnedActorVectorContract()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "2:true:true:0:true:1:1:true:true",
            session.Lua.Evaluate(
                "local firstScene=CreateFrame('ModelScene',nil,UIParent); " +
                "local secondScene=CreateFrame('ModelScene',nil,UIParent); " +
                "local first=firstScene:CreateActor(); " +
                "local second=firstScene:CreateActor(); " +
                "local initial=firstScene:GetNumActors(); " +
                "local firstMatches=firstScene:GetActorAtIndex(1)==first; " +
                "local secondMatches=firstScene:GetActorAtIndex(2)==second; " +
                "local absentCount=select('#',firstScene:GetActorAtIndex(3)); " +
                "local invalidCount=select('#',firstScene:GetActorAtIndex('bad')); " +
                "secondScene:TakeActor(first); " +
                "return table.concat({initial,tostring(firstMatches),tostring(secondMatches)," +
                "absentCount,tostring(invalidCount==0),firstScene:GetNumActors()," +
                "secondScene:GetNumActors(),tostring(firstScene:GetActorAtIndex(1)==second)," +
                "tostring(secondScene:GetActorAtIndex(1)==first)},':')"));
    }

    [Fact]
    public void CameraDrawLayerAndLightAccessorsFollowTheNativeModelSceneMethods()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1.0,2.0,3.0|1.0,0.0,0.0|0.0,-1.0,0.0|0.0,0.0,1.0|" +
            "-0.0,1.0,-0.0|OVERLAY:0|0.102,0.200,0.302|" +
            "1.000,0.000,0.502|0.5,0.6,0.7|7.0,8.0,9.0|1:false",
            session.Lua.Evaluate(
                "local scene=CreateFrame('ModelScene',nil,UIParent); " +
                "scene:SetCameraPosition(1,2,3); " +
                "local cx,cy,cz=scene:GetCameraPosition(); " +
                "scene:SetCameraOrientationByAxisVectors(" +
                "1,0,0, 0,-1,0, 0,0,1); " +
                "local fx,fy,fz=scene:GetCameraForward(); " +
                "local rx,ry,rz=scene:GetCameraRight(); " +
                "local ux,uy,uz=scene:GetCameraUp(); " +
                "scene:SetCameraOrientationByAxisVectors(" +
                "1,0,0, 1,0,0, 0,0,1); " +
                "scene:SetCameraOrientationByYawPitchRoll(0,0,0); " +
                "local yfx,yfy,yfz=scene:GetCameraForward(); " +
                "scene:SetDrawLayer('OVERLAY',7); " +
                "local layer,sublevel=scene:GetDrawLayer(); " +
                "scene:SetLightAmbientColor(.1,.2,.3); " +
                "scene:SetLightDiffuseColor(1,0,.5); " +
                "scene:SetLightDirection(4,5,6); scene:SetLightPosition(7,8,9); " +
                "scene:SetLightType(1); scene:SetLightVisible(nil); " +
                "local ar,ag,ab=scene:GetLightAmbientColor(); " +
                "local dr,dg,db=scene:GetLightDiffuseColor(); " +
                "local dx,dy,dz=scene:GetLightDirection(); " +
                "local px,py,pz=scene:GetLightPosition(); " +
                "return string.format(" +
                "'%.1f,%.1f,%.1f|%.1f,%.1f,%.1f|%.1f,%.1f,%.1f|" +
                "%.1f,%.1f,%.1f|%.1f,%.1f,%.1f|%s:%d|" +
                "%.3f,%.3f,%.3f|%.3f,%.3f,%.3f|" +
                "%.1f,%.1f,%.1f|%.1f,%.1f,%.1f|%d:%s'," +
                "cx,cy,cz,fx,fy,fz,rx,ry,rz,ux,uy,uz,yfx,yfy,yfz,layer,sublevel," +
                "ar,ag,ab,dr,dg,db,dx,dy,dz,px,py,pz," +
                "scene:GetLightType(),tostring(scene:IsLightVisible()))"));
    }

    [Fact]
    public void AmbientSetterPreservesNativeSecondaryGroupsAndDirectionUsesNativeThreshold()
    {
        using var session = new EmulatorSession();

        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene','NativeLightMutation',UIParent); " +
            "scene:SetLightAmbientColor(.1,.2,.3); " +
            "scene:SetLightDirection(1,2,3)");

        var scene = session.Ui.Find("NativeLightMutation")!.ModelScene!;
        Assert.Equal(
            new System.Numerics.Vector3(26 / 255f, 51 / 255f, 77 / 255f),
            scene.AmbientLight);
        Assert.Equal(new System.Numerics.Vector3(0.7f), scene.AmbientLightSecondary);
        Assert.Equal(new System.Numerics.Vector3(0.7f), scene.AmbientLightTertiary);
        Assert.Equal(
            System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(1, 2, 3)),
            scene.LightDirection);

        session.Lua.Evaluate(
            "local scene=_G.NativeLightMutation; " +
            "scene:SetLightDirection(.0000001,0,0)");
        Assert.Equal(new System.Numerics.Vector3(0.0000001f, 0, 0), scene.LightDirection);
    }

    [Fact]
    public void ProjectionRequiresResolvedLayoutAndUsesNativeEqualClipDepth()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:3:0.0",
            session.Lua.Evaluate(
                "local unresolved=CreateFrame('ModelScene',nil,UIParent); " +
                "unresolved:SetSize(100,100); " +
                "local unresolvedCount=select('#',unresolved:Project3DPointTo2D(0,1,0)); " +
                "local resolved=CreateFrame('ModelScene',nil,UIParent); " +
                "resolved:SetSize(100,100); resolved:SetPoint('CENTER'); " +
                "resolved:SetCameraNearClip(0); resolved:SetCameraFarClip(0); " +
                "local count=select('#',resolved:Project3DPointTo2D(0,1,0)); " +
                "local _,_,depth=resolved:Project3DPointTo2D(0,1,0); " +
                "return string.format('%d:%d:%.1f',unresolvedCount,count,depth)"));
    }

    [Fact]
    public void ProjectionDepthUsesNativeFrameAndScreenCoordinateScale()
    {
        using var session = new EmulatorSession();

        var depth = float.Parse(
            session.Lua.Evaluate(
                "local scene=CreateFrame('ModelScene','ScaledProjectionScene',UIParent); " +
                "scene:SetSize(100,100); scene:SetPoint('CENTER'); scene:SetScale(2); " +
                "scene:SetCameraNearClip(.1); scene:SetCameraFarClip(100); " +
                "local _,_,depth=scene:Project3DPointTo2D(0,1,0); " +
                "return tostring(depth)"),
            System.Globalization.CultureInfo.InvariantCulture);
        var scene = session.Ui.Find("ScaledProjectionScene")!;
        var coordinateScale =
            session.Ui.EffectiveScale(scene) *
            scene.Scale *
            session.Ui.NormalizedScreenHeight *
            1.6666666f;
        var expected = (100 - coordinateScale) / (100 - .1f);

        Assert.Equal(expected, depth, 5);
    }
}
