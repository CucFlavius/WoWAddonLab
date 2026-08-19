using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class ModelInfoContractTests
{
    [Fact]
    public void RegistersExactSurfaceAndUsesNativeArgumentContracts()
    {
        using var session = new EmulatorSession
        {
            ModelInfoProvider = new BinaryModelInfoProvider()
        };

        Assert.Equal(
            "8:0:0:0:0:0:0:0:0:" +
            "true:true:true:true:true:" +
            "false:false:false:false:false:false:false:false:false:false",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_ModelInfo) do count=count+1 end;" +
                "local scene=CreateFrame('ModelScene');" +
                "local actor=scene:CreateActor();" +
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "count," +
                "select('#',C_ModelInfo.GetModelSceneInfoByID(404))," +
                "select('#',C_ModelInfo.GetModelSceneActorInfoByID(404))," +
                "select('#',C_ModelInfo.GetModelSceneActorDisplayInfoByID(404))," +
                "select('#',C_ModelInfo.GetModelSceneCameraInfoByID(404))," +
                "select('#',C_ModelInfo.AddActiveModelScene(scene,1))," +
                "select('#',C_ModelInfo.AddActiveModelSceneActor(actor,1))," +
                "select('#',C_ModelInfo.ClearActiveModelScene(scene))," +
                "select('#',C_ModelInfo.ClearActiveModelSceneActor(actor))," +
                "tostring(ok(C_ModelInfo.AddActiveModelScene,scene,'1.9'))," +
                "tostring(ok(C_ModelInfo.AddActiveModelSceneActor,actor,1))," +
                "tostring(ok(C_ModelInfo.ClearActiveModelScene,scene))," +
                "tostring(ok(C_ModelInfo.ClearActiveModelSceneActor,actor))," +
                "tostring(select('#',C_ModelInfo.GetModelSceneInfoByID(" +
                "4294967295))==4)," +
                "tostring(ok(C_ModelInfo.GetModelSceneInfoByID))," +
                "tostring(ok(C_ModelInfo.GetModelSceneActorInfoByID,-1))," +
                "tostring(ok(C_ModelInfo.GetModelSceneActorDisplayInfoByID," +
                "4294967296))," +
                "tostring(ok(C_ModelInfo.GetModelSceneCameraInfoByID,{}))," +
                "tostring(ok(C_ModelInfo.GetModelSceneInfoByID,0/0))," +
                "tostring(ok(C_ModelInfo.GetModelSceneInfoByID,1/0))," +
                "tostring(ok(C_ModelInfo.AddActiveModelScene," +
                "CreateFrame('Frame'),1))," +
                "tostring(ok(C_ModelInfo.AddActiveModelSceneActor,scene,1))," +
                "tostring(ok(C_ModelInfo.ClearActiveModelScene,nil))," +
                "tostring(ok(C_ModelInfo.ClearActiveModelSceneActor," +
                "'actor'))},':')"));
    }

    [Fact]
    public void ProjectsDbRecordsUsingNativeWidthsOptionalsAndCameraTypeRules()
    {
        using var session = new EmulatorSession
        {
            ModelInfoProvider = new BinaryModelInfoProvider()
        };

        Assert.Equal(
            "44:-1:77:88:88:actor:16777216:true:true:nil:nil:" +
            "123:7:16777216:nil:nil:77:OrbitCamera:16777216:255:true:" +
            "78:unsupported::0:0:0:true",
            session.Lua.Evaluate(
                "Vector3DMixin={mixed=true};" +
                "CreateVector3D=function() error('must not be called') end;" +
                "local sceneType,cameras,actors,flags=" +
                "C_ModelInfo.GetModelSceneInfoByID('641.9');" +
                "local actor=C_ModelInfo.GetModelSceneActorInfoByID('88.9');" +
                "local display=C_ModelInfo." +
                "GetModelSceneActorDisplayInfoByID(99);" +
                "local camera=C_ModelInfo.GetModelSceneCameraInfoByID(77);" +
                "local unsupported=C_ModelInfo." +
                "GetModelSceneCameraInfoByID(78);" +
                "return table.concat({" +
                "sceneType,flags,cameras[1],actors[1]," +
                "actor.modelActorID,actor.scriptTag,actor.position.x," +
                "tostring(actor.position.mixed)," +
                "tostring(actor.useCenterForOriginZ)," +
                "tostring(actor.normalizeScaleAggressiveness)," +
                "tostring(actor.modelActorDisplayID)," +
                "display.animation,display.animationVariation," +
                "display.animSpeed,tostring(display.animationKitID)," +
                "tostring(display.spellVisualKitID)," +
                "camera.modelSceneCameraID,camera.cameraType," +
                "camera.target.x,camera.flags,tostring(camera.target.mixed)," +
                "unsupported.modelSceneCameraID,unsupported.scriptTag," +
                "unsupported.cameraType,unsupported.target.x," +
                "unsupported.zoomDistance,unsupported.flags," +
                "tostring(unsupported.target.mixed)},':')"));
    }

    private sealed class BinaryModelInfoProvider : IWowModelInfoProvider
    {
        public bool TryGetScene(int id, out WowModelSceneDefinition scene)
        {
            scene = new WowModelSceneDefinition(
                999,
                300,
                255,
                [77],
                [88]);
            return id is 641 or -1;
        }

        public bool TryGetActor(int id, out WowModelSceneActorDefinition actor)
        {
            actor = new WowModelSceneActorDefinition(
                999,
                "actor",
                new WowVector3(16_777_217, 2, 3),
                16_777_217,
                2,
                3,
                -1,
                true,
                false,
                true,
                0);
            return id == 88;
        }

        public bool TryGetActorDisplay(
            int id,
            out WowModelSceneActorDisplayDefinition display)
        {
            display = new WowModelSceneActorDisplayDefinition(
                999,
                123,
                7,
                16_777_217,
                0,
                0,
                0.75,
                1.5);
            return id == 99;
        }

        public bool TryGetCamera(
            int id,
            out WowModelSceneCameraDefinition camera)
        {
            camera = id == 78
                ? new WowModelSceneCameraDefinition(
                    999,
                    "unsupported",
                    new WowVector3(9, 8, 7),
                    6,
                    5,
                    4,
                    3,
                    2,
                    1,
                    new WowVector3(9, 8, 7),
                    6,
                    5,
                    4,
                    511,
                    1)
                : new WowModelSceneCameraDefinition(
                    999,
                    "orbit",
                    new WowVector3(16_777_217, 2, 3),
                    6,
                    5,
                    4,
                    3,
                    2,
                    1,
                    new WowVector3(9, 8, 7),
                    6,
                    5,
                    4,
                    511);
            return id is 77 or 78;
        }
    }
}
