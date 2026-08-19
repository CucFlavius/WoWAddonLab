using System.Numerics;
using WoWAddonLab.Emulator.Lua;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class SimpleModelContractTests
{
    [Fact]
    public void ModelDrawLayerOrdersTheQueuedViewportWithoutChangingInputOrder()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local model=CreateFrame('Model','LayeredModel',UIParent); " +
            "model:SetSize(100,100); model:SetPoint('CENTER'); " +
            "local background=model:CreateTexture('LayeredModelBackground','BACKGROUND'); " +
            "background:SetAllPoints(model); " +
            "local artwork=model:CreateTexture('LayeredModelArtwork','ARTWORK'); " +
            "artwork:SetAllPoints(model); " +
            "model:SetModelDrawLayer('BORDER')");

        var order = session.Ui.VisualRenderOrder().ToList();
        var model = session.Ui.Find("LayeredModel")!;
        var background = session.Ui.Find("LayeredModelBackground")!;
        var artwork = session.Ui.Find("LayeredModelArtwork")!;

        Assert.True(order.IndexOf(background) < order.IndexOf(model));
        Assert.True(order.IndexOf(model) < order.IndexOf(artwork));
        Assert.Equal("BORDER:0", session.Lua.Evaluate(
            "local layer,sublayer=LayeredModel:GetModelDrawLayer(); " +
            "return layer..':'..sublayer"));
    }

    [Fact]
    public void NativeSixtyFiveMethodOwnedSurfaceIsAdvertised()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "65:0",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model',nil,UIParent); local methods={" +
                "'AdvanceTime','ClearFog','ClearModel','ClearTransform'," +
                "'GetCameraDistance','GetCameraFacing','GetCameraPosition'," +
                "'GetCameraRoll','GetCameraTarget','GetDesaturation','GetFacing'," +
                "'GetFogColor','GetFogFar','GetFogNear','GetLight','GetModelAlpha'," +
                "'GetModelDrawLayer','GetModelFileID','GetModelScale','GetPaused'," +
                "'GetPitch','GetPosition','GetRoll','GetShadowEffect','GetViewInsets'," +
                "'GetViewTranslation','GetWorldScale','HasAttachmentPoints'," +
                "'HasCustomCamera','IsUsingModelCenterToTransform'," +
                "'MakeCurrentCameraCustom','ReplaceIconTexture','SetCamera'," +
                "'SetCameraDistance','SetCameraFacing','SetCameraPosition'," +
                "'SetCameraRoll','SetCameraTarget','SetCustomCamera','SetDesaturation'," +
                "'SetFacing','SetFogColor','SetFogFar','SetFogNear','SetGlow'," +
                "'SetGradientMask','SetLight','SetModel','SetModelAlpha'," +
                "'SetModelDrawLayer','SetModelScale','SetParticlesEnabled'," +
                "'SetPaused','SetPitch','SetPosition','SetRoll','SetSequence'," +
                "'SetSequenceTime','SetShadowEffect','SetTransform','SetUseGBuffer'," +
                "'SetViewInsets','SetViewTranslation'," +
                "'TransformCameraSpaceToModelSpace','UseModelCenterToTransform'}; " +
                "local missing=0; for _,name in ipairs(methods) do " +
                "if type(model[name])~='function' then missing=missing+1 end end; " +
                "return #methods..':'..missing"));
    }

    [Fact]
    public void CameraSelectionConsumesOnlySetCameraPendingStateAndClonesValidResources()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata([], 0)
        {
            Cameras =
            [
                new WowModelCameraMetadata(
                    new Vector3(8, 4, 2),
                    new Vector3(2, 1, -1))
            ]
        };
        session.ModelResourceProvider = resources;

        Assert.Equal(
            "100:100:true:false:false:true:false:false",
            session.Lua.Evaluate(
                "local empty=CreateFrame('Model','DefaultCameraModel',UIParent); " +
                "empty:MakeCurrentCameraCustom(); local x=empty:GetCameraPosition(); " +
                "local model=CreateFrame('Model','IndexedCameraModel',UIParent); " +
                "model:SetCamera(0); model:SetCustomCamera(0); " +
                "local customBeforeLoad=model:HasCustomCamera(); " +
                "model:SetModel(777); local customAfterLoad=model:HasCustomCamera(); " +
                "model:SetCustomCamera(0); local validCustom=model:HasCustomCamera(); " +
                "model:SetCamera(9); local invalidSelected=model:HasCustomCamera(); " +
                "model:SetCustomCamera(9); local invalidCustom=model:HasCustomCamera(); " +
                "return table.concat({x,empty:GetCameraDistance()," +
                "tostring(empty:HasCustomCamera()),tostring(customBeforeLoad)," +
                "tostring(customAfterLoad),tostring(validCustom)," +
                "tostring(invalidSelected),tostring(invalidCustom)},':')"));

        var model = session.Ui.Find("IndexedCameraModel")!;
        Assert.Null(model.ModelCameraIndex);
        Assert.False(model.ModelHasCurrentCamera);
        Assert.False(model.ModelHasCustomCamera);
        Assert.Single(model.ModelCameras);

        session.Lua.Evaluate("IndexedCameraModel:SetCustomCamera(0)");
        Assert.True(model.ModelHasCurrentCamera);
        Assert.True(model.ModelHasCustomCamera);
        Assert.Equal(new Vector3(8, 4, 2), model.ModelCameraPosition);
        Assert.Equal(new Vector3(2, 1, -1), model.ModelCameraTarget);
        Assert.Equal(MathF.Sqrt(45), model.ModelCameraDistance, 4);
    }

    [Fact]
    public void SelectedCameraSamplesNativeTracksAndCustomCameraDetachesFromThem()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata(
            [new WowModelSequenceMetadata(42, 0, 1000, 1, 0x8000, -1, -1)],
            0)
        {
            GlobalSequenceDurationsMilliseconds = [200],
            Cameras =
            [
                new WowModelCameraMetadata(
                    new Vector3(1, 2, 3),
                    new Vector3(4, 5, 6),
                    new WowModelAnimationTrack<Vector3>(
                        1,
                        -1,
                        [
                            new WowModelAnimationTrackSequence<Vector3>(
                                [0, 100],
                                [
                                    new WowModelAnimationTrackKey<Vector3>(
                                        Vector3.Zero,
                                        default,
                                        default),
                                    new WowModelAnimationTrackKey<Vector3>(
                                        new Vector3(10, 0, 0),
                                        default,
                                        default)
                                ])
                        ]),
                    new WowModelAnimationTrack<Vector3>(
                        1,
                        0,
                        [
                            new WowModelAnimationTrackSequence<Vector3>(
                                [0, 200],
                                [
                                    new WowModelAnimationTrackKey<Vector3>(
                                        Vector3.Zero,
                                        default,
                                        default),
                                    new WowModelAnimationTrackKey<Vector3>(
                                        new Vector3(0, 10, 0),
                                        default,
                                        default)
                                ])
                        ]),
                    new WowModelAnimationTrack<float>(
                        2,
                        -1,
                        [
                            new WowModelAnimationTrackSequence<float>(
                                [0, 100],
                                [
                                    new WowModelAnimationTrackKey<float>(0, 0, 0),
                                    new WowModelAnimationTrackKey<float>(1, 1, 1)
                                ])
                        ]),
                    new WowModelAnimationTrack<float>(
                        1,
                        -1,
                        [
                            new WowModelAnimationTrackSequence<float>(
                                [0, 100],
                                [
                                    new WowModelAnimationTrackKey<float>(4, 0, 0),
                                    new WowModelAnimationTrackKey<float>(4, 0, 0)
                                ])
                        ]),
                    Type: -1,
                    FarClip: 5000,
                    NearClip: 0.5f)
            ]
        };
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local model=CreateFrame('Model','TrackedCameraModel',UIParent); " +
            "model:SetModel(777); model:SetSequenceTime(42,25); model:SetCamera(0)");
        var model = session.Ui.Find("TrackedCameraModel")!;

        session.Tick(0.051);

        Assert.Equal(new Vector3(8.5f, 2, 3), model.ModelCameraPosition);
        Assert.Equal(new Vector3(4, 7.55f, 6), model.ModelCameraTarget);
        Assert.Equal(0.84375f, model.ModelCameraRoll, 4);
        Assert.Equal(MathF.PI / 2, model.ModelCameraFieldOfView, 4);
        Assert.Equal(0.5f, model.ModelCameraNearClip);
        Assert.Equal(5000, model.ModelCameraFarClip);
        Assert.Equal(0u, model.ModelSelectedCameraIndex);

        session.Lua.Evaluate("TrackedCameraModel:SetCustomCamera(0)");
        var detachedPosition = model.ModelCameraPosition;
        session.Tick(0.05);

        Assert.True(model.ModelHasCustomCamera);
        Assert.Null(model.ModelSelectedCameraIndex);
        Assert.Equal(detachedPosition, model.ModelCameraPosition);
    }

    [Fact]
    public void AutomaticTransformUsesDisplayScaleBoundsCenterAndRunsWhilePaused()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata([], 0)
        {
            BoundingBoxMinimum = new Vector3(2, 4, 6),
            BoundingBoxMaximum = new Vector3(6, 10, 14)
        };
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local model=CreateFrame('Model','AutomaticTransformModel',UIParent); " +
            "model:SetModel(777); model:SetModelScale(2); " +
            "model:SetPosition(1,2,3)");
        var model = session.Ui.Find("AutomaticTransformModel")!;
        var factor =
            2 *
            session.Ui.EffectiveScale(model) *
            session.Ui.NormalizedScreenHeight *
            1.6666666f;

        session.Tick(0);

        Assert.Equal(new Vector3(4, 7, 10), model.ModelCenter);
        Assert.Equal(factor, model.ModelTransformMatrix.M11, 5);
        Assert.Equal(factor, model.ModelTransformMatrix.M22, 5);
        Assert.Equal(factor, model.ModelTransformMatrix.M33, 5);
        Assert.Equal(factor, model.ModelTransformMatrix.M41, 5);
        Assert.Equal(2 * factor, model.ModelTransformMatrix.M42, 5);
        Assert.Equal(3 * factor, model.ModelTransformMatrix.M43, 5);
        Assert.Equal(factor, model.ModelWorldScale, 5);

        session.Lua.Evaluate(
            "AutomaticTransformModel:UseModelCenterToTransform(true); " +
            "AutomaticTransformModel:SetPaused(true); " +
            "AutomaticTransformModel:SetPosition(5,6,7)");
        session.Tick(0);

        Assert.Equal(factor, model.ModelTransformMatrix.M41, 5);
        Assert.Equal(-factor, model.ModelTransformMatrix.M42, 5);
        Assert.Equal(-3 * factor, model.ModelTransformMatrix.M43, 5);

        var acceptedMatrix = model.ModelTransformMatrix;
        session.Lua.Evaluate("AutomaticTransformModel:SetModelScale(0)");
        session.Tick(0);
        Assert.Equal(acceptedMatrix, model.ModelTransformMatrix);

        session.Lua.Evaluate(
            "AutomaticTransformModel:SetModelScale(2); " +
            "AutomaticTransformModel:UseModelCenterToTransform(false); " +
            "AutomaticTransformModel:SetScript('OnUpdate',function(self) " +
            "self:SetPosition(9,8,7); self:SetScript('OnUpdate',nil) end)");
        session.Tick(0);
        Assert.Equal(9 * factor, model.ModelTransformMatrix.M41, 5);
        Assert.Equal(8 * factor, model.ModelTransformMatrix.M42, 5);
        Assert.Equal(7 * factor, model.ModelTransformMatrix.M43, 5);
    }

    [Fact]
    public void ExplicitTransformOverridesAutomaticTransformUntilCleared()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata([], 0)
        {
            BoundingBoxMinimum = new Vector3(-2, -2, -2),
            BoundingBoxMaximum = new Vector3(2, 2, 2)
        };
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local model=CreateFrame('Model','OverrideTransformModel',UIParent); " +
            "model:SetModel(777); model:SetTransform({x=9,y=8,z=7},nil,3); " +
            "model:SetModelScale(2); model:SetPosition(1,2,3)");
        var model = session.Ui.Find("OverrideTransformModel")!;
        var explicitMatrix = model.ModelTransformMatrix;

        session.Tick(0);

        Assert.Equal(explicitMatrix, model.ModelTransformMatrix);
        Assert.Equal(9, model.ModelTransformMatrix.M41);
        Assert.Equal(8, model.ModelTransformMatrix.M42);
        Assert.Equal(7, model.ModelTransformMatrix.M43);

        session.Lua.Evaluate("OverrideTransformModel:ClearTransform()");
        session.Tick(0);

        var factor =
            2 *
            session.Ui.EffectiveScale(model) *
            session.Ui.NormalizedScreenHeight *
            1.6666666f;
        Assert.Equal(factor, model.ModelTransformMatrix.M11, 5);
        Assert.Equal(factor, model.ModelTransformMatrix.M41, 5);
        Assert.Equal(2 * factor, model.ModelTransformMatrix.M42, 5);
        Assert.Equal(3 * factor, model.ModelTransformMatrix.M43, 5);
    }

    [Fact]
    public void CharacterModelVirtualScaleIncludesItsDisplayMultiplier()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local plain=CreateFrame('Model','PlainScaleModel',UIParent); " +
            "plain:SetModel(777); plain:SetModelScale(2); " +
            "local character=CreateFrame('PlayerModel','CharacterScaleModel',UIParent); " +
            "character:SetModel(777); character:SetModelScale(2)");
        var plain = session.Ui.Find("PlainScaleModel")!;
        var character = session.Ui.Find("CharacterScaleModel")!;
        plain.ModelDisplayScaleMultiplier = 0.5f;
        character.ModelDisplayScaleMultiplier = 0.5f;

        session.Tick(0);

        var baseFactor =
            session.Ui.EffectiveScale(plain) *
            session.Ui.NormalizedScreenHeight *
            1.6666666f;
        Assert.Equal(2 * baseFactor, plain.ModelTransformMatrix.M11, 5);
        Assert.Equal(baseFactor, character.ModelTransformMatrix.M11, 5);

        session.Lua.Evaluate("CharacterScaleModel:SetDisplayInfo(123)");
        Assert.Equal(1, character.ModelDisplayScaleMultiplier);
        session.Tick(0);
        Assert.Equal(2 * baseFactor, character.ModelTransformMatrix.M11, 5);
    }

    [Fact]
    public void RenderCameraPacketUsesNativeBasisClipAndFovRules()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata([], 0)
        {
            Cameras =
            [
                new WowModelCameraMetadata(
                    new Vector3(2, 3, 4),
                    new Vector3(7, 3, 4),
                    FarClip: -20,
                    NearClip: 0.2f)
            ]
        };
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local model=CreateFrame('Model','RenderCameraModel',UIParent); " +
            "model:SetModel(777); model:SetCamera(0)");
        var model = session.Ui.Find("RenderCameraModel")!;
        model.ModelCameraFieldOfView = 9;

        session.Tick(0);

        var camera = Assert.IsType<UiModelRenderCameraState>(
            model.ModelRenderCameraState);
        Assert.Equal(Vector3.UnitX, camera.Forward);
        Assert.Equal(Vector3.UnitY, camera.Right);
        Assert.Equal(Vector3.UnitZ, camera.Up);
        Assert.Equal(new Vector3(2, 3, 4), camera.Position);
        Assert.Equal(MathF.Tau, camera.DiagonalFieldOfView);
        Assert.Equal(0.1f, camera.NearClip);
        Assert.Equal(0, camera.FarClip);
        Assert.Equal(1, camera.Scale);
        Assert.Equal(0.1f, model.ModelCameraNearClip);

        model.ModelCameraPosition = Vector3.Zero;
        model.ModelCameraTarget = Vector3.UnitZ;
        model.ModelCameraFieldOfView = float.NaN;
        model.ModelCameraNearClip = float.NaN;
        model.ModelCameraFarClip = float.PositiveInfinity;
        session.Tick(0);

        camera = Assert.IsType<UiModelRenderCameraState>(
            model.ModelRenderCameraState);
        Assert.Equal(Vector3.UnitZ, camera.Forward);
        Assert.Equal(Vector3.UnitX, camera.Right);
        Assert.Equal(Vector3.UnitY, camera.Up);
        Assert.Equal(0, camera.DiagonalFieldOfView);
        Assert.Equal(0, camera.NearClip);
        Assert.Equal(float.PositiveInfinity, camera.FarClip);
    }

    [Fact]
    public void RenderCameraProjectionUsesDiagonalFovAndReverseZDepth()
    {
        const float aspect = 16f / 9;
        var camera = new UiModelRenderCameraState(
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ,
            Vector3.Zero,
            1.2f,
            0.1f,
            1000,
            1);

        var projection = camera.CreateProjectionMatrix(aspect);
        var verticalFieldOfView =
            camera.DiagonalFieldOfView /
            MathF.Sqrt(aspect * aspect + 1);
        var expectedY = 1 / MathF.Tan(verticalFieldOfView * .5f);

        Assert.Equal(expectedY / aspect, projection.M11, 5);
        Assert.Equal(expectedY, projection.M22, 5);
        Assert.Equal(
            camera.NearClip / (camera.NearClip - camera.FarClip),
            projection.M33,
            7);
        Assert.Equal(1, projection.M34);
        Assert.Equal(
            -camera.NearClip * camera.FarClip /
            (camera.NearClip - camera.FarClip),
            projection.M43,
            6);
        Assert.Equal(0, projection.M44);

        var near = Vector4.Transform(
            new Vector4(0, 0, camera.NearClip, 1),
            projection);
        var far = Vector4.Transform(
            new Vector4(0, 0, camera.FarClip, 1),
            projection);
        Assert.Equal(1, near.Z / near.W, 5);
        Assert.Equal(0, far.Z / far.W, 5);
    }

    [Fact]
    public void RenderCameraViewUsesNativeNegativeRightUpForwardBasis()
    {
        var camera = new UiModelRenderCameraState(
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ,
            new Vector3(2, 3, 4),
            1.2f,
            0.1f,
            1000,
            2);

        var orientation = camera.CreateViewOrientationMatrix();
        Assert.Equal(
            new Matrix4x4(
                0, 0, 1, 0,
                -1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 0, 1),
            orientation);

        var cameraPosition = camera.Position * camera.Scale;
        var view = camera.CreateViewMatrix();
        Assert.Equal(
            Vector3.Zero,
            Vector3.Transform(cameraPosition, view));
        Assert.Equal(
            new Vector3(0, 0, 5),
            Vector3.Transform(
                cameraPosition + camera.Forward * 5,
                view));
        Assert.Equal(
            new Vector3(-5, 0, 0),
            Vector3.Transform(
                cameraPosition + camera.Right * 5,
                view));
        Assert.Equal(
            new Vector3(0, 5, 0),
            Vector3.Transform(
                cameraPosition + camera.Up * 5,
                view));
    }

    [Fact]
    public void ModelViewProjectionComposesInNativeRowVectorOrder()
    {
        const float aspect = 4f / 3;
        var camera = new UiModelRenderCameraState(
            Vector3.UnitZ,
            Vector3.UnitX,
            Vector3.UnitY,
            new Vector3(3, 4, 5),
            1.1f,
            0.2f,
            200,
            1);
        var model = Matrix4x4.CreateScale(2) *
            Matrix4x4.CreateTranslation(3, 4, 10);

        var expected = model *
            camera.CreateViewMatrix() *
            camera.CreateProjectionMatrix(aspect);

        Assert.Equal(
            expected,
            camera.CreateModelViewProjectionMatrix(model, aspect));
    }

    [Fact]
    public void DesaturationUsesOnlyTheCurrentLiveModelRenderEffect()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0.00:0.00:1.00:0.00:0.00",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model',nil,UIParent); " +
                "local empty=model:GetDesaturation(); " +
                "model:SetDesaturation(2.5); local notDeferred=model:GetDesaturation(); " +
                "model:SetModel(777); model:SetDesaturation(3.25); " +
                "local active=model:GetDesaturation(); " +
                "model:SetDesaturation(0); local cleared=model:GetDesaturation(); " +
                "model:SetDesaturation(1.5); model:ClearModel(); " +
                "return string.format('%.2f:%.2f:%.2f:%.2f:%.2f'," +
                "empty,notDeferred,active,cleared,model:GetDesaturation())"));
    }

    [Fact]
    public void ShadowAndDesaturationShareOneClampedLiveRenderEffect()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1.00:0.00:1.00:0.00:0.00",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model',nil,UIParent); model:SetModel(777); " +
                "model:SetShadowEffect(2.5); local shadow=model:GetShadowEffect(); " +
                "model:SetDesaturation(3.25); " +
                "local replacedShadow=model:GetShadowEffect(); " +
                "local desaturation=model:GetDesaturation(); " +
                "model:SetShadowEffect(-1); " +
                "return string.format('%.2f:%.2f:%.2f:%.2f:%.2f'," +
                "shadow,replacedShadow,desaturation," +
                "model:GetShadowEffect(),model:GetDesaturation())"));
    }

    [Fact]
    public void AdvanceTimeIgnoresSurplusArgumentsAndDoesNotMutatePlayback()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata(
            [new WowModelSequenceMetadata(42, 0, 100, 1, 0x8000, -1, -1)],
            0);
        session.ModelResourceProvider = resources;

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model','AdvanceTimeModel',UIParent); " +
                "model:SetModel(777); model:SetSequence(42); " +
                "return select('#',model:AdvanceTime(250,'ignored'))"));

        var model = session.Ui.Find("AdvanceTimeModel")!;
        Assert.Equal(-1, model.ModelSequenceElapsedMilliseconds, 3);
        Assert.True(model.ModelSequencePlaying);
    }

    [Fact]
    public void ResourceReplacementInitializesModelAlphaFromFrameAlpha()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.UnionWith([777, 778]);
        session.ModelResourceProvider = resources;

        Assert.Equal(
            "0.400:0.800:0.400:1.000:1.000:0.400",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model','ModelAlphaLifecycle',UIParent); " +
                "model:SetAlpha(.4); model:SetModel(777); " +
                "local initial=model:GetModelAlpha(); " +
                "model:SetModelAlpha(.8); local changed=model:GetModelAlpha(); " +
                "model:SetModel(778); local replaced=model:GetModelAlpha(); " +
                "model:ClearModel(); local cleared=model:GetModelAlpha(); " +
                "model:SetModelAlpha(.2); local unloaded=model:GetModelAlpha(); " +
                "model:SetModel(777); local reloaded=model:GetModelAlpha(); " +
                "return string.format('%.3f:%.3f:%.3f:%.3f:%.3f:%.3f'," +
                "initial,changed,replaced,cleared,unloaded,reloaded)"));
    }

    [Fact]
    public void ResourceReplacementRestoresTheNativeParticleEmitterDefault()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.UnionWith([777, 778]);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('Model','ParticleLifecycleModel',UIParent); " +
            "model:SetParticlesEnabled(false)");
        var model = session.Ui.Find("ParticleLifecycleModel")!;
        Assert.True(model.ModelParticlesEnabled);

        session.Lua.Evaluate("ParticleLifecycleModel:SetModel(777)");
        Assert.True(model.ModelParticlesEnabled);

        session.Lua.Evaluate("ParticleLifecycleModel:SetParticlesEnabled(false)");
        Assert.False(model.ModelParticlesEnabled);

        session.Lua.Evaluate("ParticleLifecycleModel:SetModel(778)");
        Assert.True(model.ModelParticlesEnabled);

        session.Lua.Evaluate(
            "ParticleLifecycleModel:ClearModel(); " +
            "ParticleLifecycleModel:SetParticlesEnabled(false); " +
            "ParticleLifecycleModel:SetModel(777)");
        Assert.True(model.ModelParticlesEnabled);
    }

    [Fact]
    public void SetGlowUsesTheNativeNoOpForLuaCreatableModelTypes()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:0:false:false",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model','GlowModel',UIParent); " +
                "local player=CreateFrame('PlayerModel','GlowPlayerModel',UIParent); " +
                "local modelResult=select('#',model:SetGlow(-2.5)); " +
                "local playerResult=select('#',player:SetGlow(4.25)); " +
                "return table.concat({modelResult,playerResult," +
                "tostring(pcall(model.SetGlow,model))," +
                "tostring(pcall(player.SetGlow,player))},':')"));

        Assert.Equal(0, session.Ui.Find("GlowModel")!.ModelGlow);
        Assert.Equal(0, session.Ui.Find("GlowPlayerModel")!.ModelGlow);
    }

    [Fact]
    public void ResourceLifecycleUsesResolvedLiveResourceAndPlainModelScripts()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.Paths["models/test.m2"] = 9001;
        resources.ExistingFileDataIds.UnionWith([777, 9001]);
        resources.Metadata[9001] = new WowModelResourceMetadata(
            [new WowModelSequenceMetadata(42, 0, 750, 0, 1, -1, -1)],
            2);
        session.ModelResourceProvider = resources;

        Assert.Equal(
            "true:true:true:false:false:9001:loaded",
            session.Lua.Evaluate(
                "local events={}; local model=CreateFrame('Model','LifecycleModel',UIParent); " +
                "model:SetScript('OnModelLoaded',function() table.insert(events,'loaded') end); " +
                "model:SetModel('models/test.m2',true); " +
                "return table.concat({tostring(model:HasScript('OnModelLoaded'))," +
                "tostring(model:HasScript('OnAnimStarted'))," +
                "tostring(model:HasScript('OnAnimFinished'))," +
                "tostring(model:HasScript('OnModelCleared'))," +
                "tostring(model:HasScript('OnModelLoading'))," +
                "model:GetModelFileID(),table.concat(events,',')},':')"));

        var model = session.Ui.Find("LifecycleModel")!;
        Assert.True(model.ModelResourceLoaded);
        Assert.Equal(9001u, model.ModelFileDataId);
        Assert.Equal("models/test.m2", model.ModelPath);
        Assert.True(model.ModelNoMip);
        Assert.Equal([42], model.ModelAnimationIdsInResourceOrder);
        Assert.Equal(
            "true:false:0:false",
            session.Lua.Evaluate(
                "local model=LifecycleModel; local attachments=model:HasAttachmentPoints(); " +
                "local success=pcall(model.SetModel,model,999); " +
                "return table.concat({tostring(attachments),tostring(success)," +
                "model:GetModelFileID(),tostring(model:HasAttachmentPoints())},':')"));
        Assert.False(model.ModelResourceLoaded);
        Assert.Null(model.ModelFileDataId);
        Assert.False(model.ModelNoMip);
    }

    [Fact]
    public void SimulatedModelsKeepPositiveFileIdsWithoutDecodedResources()
    {
        using var session = new EmulatorSession();
        session.ModelResourceProvider = new TestModelResourceProvider
        {
            SimulateUnresolvedModels = true
        };

        Assert.Equal(
            "83906:0",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model',nil,UIParent); " +
                "model:SetModel(83906); " +
                "return model:GetModelFileID()..':'..select('#',model:SetModel(83906))"));
    }

    [Fact]
    public void ReplaceIconTextureResolvesAFileAssetOnlyForALiveResource()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.Paths["icons/test.blp"] = 501;
        resources.ExistingFileDataIds.Add(777);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('Model','IconReplacementModel',UIParent); " +
            "model:ReplaceIconTexture(456)");
        var model = session.Ui.Find("IconReplacementModel")!;
        Assert.Null(model.ModelIconTextureFileDataId);

        session.Lua.Evaluate(
            "IconReplacementModel:SetModel(777); " +
            "IconReplacementModel:ReplaceIconTexture('icons/test.blp')");
        Assert.Equal(501u, model.ModelIconTextureFileDataId);
        Assert.Null(model.ModelIconTexturePath);

        session.Lua.Evaluate(
            "IconReplacementModel:ReplaceIconTexture('icons/missing.blp')");
        Assert.Null(model.ModelIconTextureFileDataId);
    }

    [Fact]
    public void SequenceRangeFailureRaisesLuaErrorAndPreservesLiveSequence()
    {
        using var session = new EmulatorSession();

        session.Lua.Evaluate(
            "local model=CreateFrame('Model','SequenceRangeModel',UIParent); " +
            "model:SetModel(777); model:SetSequenceTime(41,-23)");
        var model = session.Ui.Find("SequenceRangeModel")!;
        Assert.Equal(41u, model.ModelSequenceId);
        Assert.Equal(-23, model.ModelSequenceTimeOffset);

        Assert.Equal(
            "false:true:false:true",
            session.Lua.Evaluate(
                "local ok,errorText=pcall(SequenceRangeModel.SetSequence," +
                "SequenceRangeModel,1858); " +
                "local timeOk,timeError=pcall(SequenceRangeModel.SetSequenceTime," +
                "SequenceRangeModel,4294967295,99); " +
                "return table.concat({tostring(ok),tostring(" +
                "string.find(errorText,'Sequence exceeds valid range of 0 %- 1858')~=nil)," +
                "tostring(timeOk),tostring(string.find(timeError," +
                "'Sequence exceeds valid range of 0 %- 1858')~=nil)},':')"));
        Assert.Equal(41u, model.ModelSequenceId);
        Assert.Equal(-23, model.ModelSequenceTimeOffset);

        Assert.Equal(
            "true",
            session.Lua.Evaluate(
                "return tostring(pcall(SequenceRangeModel.SetSequence," +
                "SequenceRangeModel,1857))"));
        Assert.Equal(1857u, model.ModelSequenceId);
        Assert.Equal(0, model.ModelSequenceTimeOffset);
    }

    [Fact]
    public void ResolvedSequenceDispatchesOnAnimStartedSynchronously()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(42, 0, 750, 0, 1, -1, -1),
                new WowModelSequenceMetadata(7, 0, 400, 0, 1, -1, -1)
            ],
            0);
        resources.AnimationFallbacks[43] =
            new WowAnimationFallback(43, 42, 0);
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local model=CreateFrame('Model','SequenceEventModel',UIParent); " +
            "model:SetModel(777)");
        var model = session.Ui.Find("SequenceEventModel")!;

        Assert.Equal(
            "before,started,after,started,fallback-after,started,default-after",
            session.Lua.Evaluate(
                "local events={'before'}; " +
                "SequenceEventModel:SetScript('OnAnimStarted',function() " +
                "table.insert(events,'started') end); " +
                "SequenceEventModel:SetSequenceTime(42,-17); " +
                "table.insert(events,'after'); " +
                "SequenceEventModel:SetSequence(43); " +
                "table.insert(events,'fallback-after'); " +
                "SequenceEventModel:SetSequence(99); " +
                "table.insert(events,'default-after'); " +
                "return table.concat(events,',')"));
        Assert.Equal(99u, model.ModelSequenceId);
        Assert.Equal(0, model.ModelSequenceTimeOffset);
        Assert.Equal((ushort)42, model.ModelResolvedSequenceId);
    }

    [Fact]
    public void RandomVariationWalkUsesNativeWeightsAndVariationNextLinks()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(42, 0, 500, 1, 0, 1, -1),
                new WowModelSequenceMetadata(42, 1, 900, 1, 0x8000, -1, -1)
            ],
            0);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('Model','VariationModel',UIParent); " +
            "model:SetModel(777); model:SetSequence(42)");

        var model = session.Ui.Find("VariationModel")!;
        Assert.Equal(1, model.ModelResolvedSequenceIndex);
        Assert.Equal((ushort)1, model.ModelResolvedSequenceVariation);
        Assert.Equal(900u, model.ModelResolvedSequenceDurationMilliseconds);
    }

    [Fact]
    public void AliasSequenceUsesTheFirstNonAliasPlaybackRecord()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(42, 0, 100, 0x40, 0x8000, -1, 1),
                new WowModelSequenceMetadata(7, 3, 600, 1, 0x8000, -1, 0)
            ],
            0);
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local model=CreateFrame('Model','AliasSequenceModel',UIParent); " +
            "AliasSequenceFinished=0; " +
            "model:SetScript('OnAnimFinished',function() " +
            "AliasSequenceFinished=AliasSequenceFinished+1 end); " +
            "model:SetModel(777); model:SetSequence(42)");

        var model = session.Ui.Find("AliasSequenceModel")!;
        Assert.Equal(0, model.ModelSelectedSequenceIndex);
        Assert.Equal(1, model.ModelResolvedSequenceIndex);
        Assert.Equal((ushort)7, model.ModelResolvedSequenceId);
        Assert.Equal((ushort)3, model.ModelResolvedSequenceVariation);
        Assert.Equal(600u, model.ModelResolvedSequenceDurationMilliseconds);

        session.Tick(0.25);
        Assert.Equal("0", session.Lua.Evaluate("return AliasSequenceFinished"));
        session.Tick(0.25);
        session.Tick(0.1);
        Assert.Equal("0", session.Lua.Evaluate("return AliasSequenceFinished"));
        session.Tick(0.001);
        Assert.Equal("1", session.Lua.Evaluate("return AliasSequenceFinished"));
    }

    [Fact]
    public void SequenceTimePauseReplacementNaturalFinishAndInclusiveTickFollowControllerLifecycle()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(42, 0, 500, 1, 0x8000, -1, -1),
                new WowModelSequenceMetadata(7, 0, 300, 1, 0x8000, -1, -1)
            ],
            0);
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local events={}; SequenceLifecycleEvents=events; " +
            "local model=CreateFrame('Model','SequenceLifecycleModel',UIParent); " +
            "model:SetScript('OnAnimStarted',function() table.insert(events,'start') end); " +
            "model:SetScript('OnAnimFinished',function() table.insert(events,'finish') end); " +
            "model:SetModel(777); model:SetSequenceTime(42,-100)");
        var model = session.Ui.Find("SequenceLifecycleModel")!;

        session.Tick(0.25);
        Assert.Equal(149, model.ModelSequenceElapsedMilliseconds, 3);
        Assert.Equal("start", session.Lua.Evaluate(
            "return table.concat(SequenceLifecycleEvents,',')"));

        session.Lua.Evaluate("SequenceLifecycleModel:SetPaused(true)");
        session.Tick(0.25);
        Assert.Equal(149, model.ModelSequenceElapsedMilliseconds, 3);

        session.Lua.Evaluate("SequenceLifecycleModel:SetPaused(false)");
        session.Tick(0.25);
        Assert.Equal(399, model.ModelSequenceElapsedMilliseconds, 3);
        session.Lua.Evaluate("SequenceLifecycleModel:SetSequence(7)");
        Assert.Equal("start,start", session.Lua.Evaluate(
            "return table.concat(SequenceLifecycleEvents,',')"));

        session.Tick(0.25);
        Assert.True(model.ModelSequencePlaying);
        session.Tick(0.05);
        Assert.True(model.ModelSequencePlaying);
        session.Tick(0.001);
        Assert.False(model.ModelSequencePlaying);
        Assert.Equal(300, model.ModelSequenceElapsedMilliseconds, 3);
        Assert.Equal("start,start,finish", session.Lua.Evaluate(
            "return table.concat(SequenceLifecycleEvents,',')"));

        session.Lua.Evaluate("SequenceLifecycleModel:SetSequenceTime(42,500)");
        Assert.True(model.ModelSequencePlaying);
        Assert.Equal(499, model.ModelSequenceElapsedMilliseconds, 3);
        session.Tick(0.001);
        Assert.False(model.ModelSequencePlaying);
        Assert.Equal("start,start,finish,start,finish", session.Lua.Evaluate(
            "return table.concat(SequenceLifecycleEvents,',')"));
    }

    [Fact]
    public void LoopingSequenceReportsBoundariesAndReentrantStartSkipsInclusiveTick()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(777);
        resources.Metadata[777] = new WowModelResourceMetadata(
            [new WowModelSequenceMetadata(42, 0, 100, 0, 0x8000, -1, -1)],
            0);
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local model=CreateFrame('Model','LoopingSequenceModel',UIParent); " +
            "LoopingSequenceFinishCount=0; " +
            "model:SetScript('OnAnimFinished',function(self) " +
            "LoopingSequenceFinishCount=LoopingSequenceFinishCount+1; " +
            "if LoopingSequenceFinishCount==1 then self:SetSequence(42) end end); " +
            "model:SetModel(777); model:SetSequence(42)");

        session.Tick(0.25);

        Assert.Equal("1", session.Lua.Evaluate(
            "return LoopingSequenceFinishCount"));
        var model = session.Ui.Find("LoopingSequenceModel")!;
        Assert.Equal(0, model.ModelSequenceElapsedMilliseconds, 3);
        Assert.True(model.ModelSequencePlaying);
    }
}
