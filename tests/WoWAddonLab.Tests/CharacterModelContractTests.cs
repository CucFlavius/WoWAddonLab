using System.Reflection;
using System.Numerics;
using WoWAddonLab.Emulator.Lua;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class CharacterModelContractTests
{
    private static readonly string[] NativeOwnedMethods =
    [
        "ApplySpellVisualKit", "CanSetUnit", "FreezeAnimation", "GetDisplayInfo",
        "GetDoBlend", "GetKeepModelOnHide", "HasAnimation", "PlayAnimKit",
        "RefreshCamera", "RefreshUnit", "SetAnimation",
        "SetBarberShopAlternateForm", "SetCamDistanceScale", "SetCreature",
        "SetDisplayInfo", "SetDoBlend", "SetItem", "SetItemAppearance",
        "SetKeepModelOnHide", "SetPortraitZoom", "SetRotation", "SetUnit",
        "StopAnimKit", "ZeroCachedCenterXY"
    ];

    [Fact]
    public void NativeTwentyFourMethodOwnedSurfaceMatchesRegistrationTable()
    {
        Assert.Equal(NativeOwnedMethods, GetOwnedMethods("CharacterModel"));

        using var session = new EmulatorSession();
        Assert.Equal(
            "function:function",
            session.Lua.Evaluate(
                "local model=CreateFrame('PlayerModel',nil,UIParent); " +
                "return type(model.SetLight)..':'..type(model.ZeroCachedCenterXY)"));
    }

    [Fact]
    public void NativeDressUpAndCinematicOwnedSurfacesMatchRegistrationTables()
    {
        Assert.Equal(
            [
                "Dress", "GetAutoDress", "GetItemTransmogInfo",
                "GetItemTransmogInfoList", "GetObeyHideInTransmogFlag",
                "GetSheathed", "GetUseTransmogChoices", "GetUseTransmogSkin",
                "IsGeoReady", "IsSlotAllowed", "IsSlotVisible", "SetAutoDress",
                "SetItemTransmogInfo", "SetObeyHideInTransmogFlag", "SetSheathed",
                "SetUseTransmogChoices", "SetUseTransmogSkin", "TryOn", "Undress",
                "UndressSlot"
            ],
            GetOwnedMethods("DressUpModel"));

        Assert.Equal(
            [
                "EquipItem", "InitializeCamera", "InitializePanCamera",
                "RefreshCamera", "SetAnimOffset", "SetCameraPosition",
                "SetCameraTarget", "SetCreatureData", "SetFacingLeft",
                "SetFadeTimes", "SetHeightFactor", "SetJumpInfo", "SetPanDistance",
                "SetSpellVisualKit", "SetTargetDistance", "StartPan", "StopPan",
                "UnequipItems"
            ],
            GetOwnedMethods("CinematicModel"));
    }

    [Fact]
    public void FrameModelBranchesDoNotBorrowModelSceneActorMethods()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "nil:nil:nil:nil:function:function:function",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model',nil,UIParent); " +
                "local character=CreateFrame('PlayerModel',nil,UIParent); " +
                "local dress=CreateFrame('DressUpModel',nil,UIParent); " +
                "local cinematic=CreateFrame('CinematicModel',nil,UIParent); " +
                "return table.concat({type(model.SetModelByFileID)," +
                "type(character.SetUseCenterForOrigin),type(dress.CreateActor)," +
                "type(cinematic.GetYaw),type(model.SetModel)," +
                "type(dress.TryOn),type(cinematic.StartPan)},':')"));
    }

    [Fact]
    public void PlainModelStopsAtTheSimpleModelSurface()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "nil:nil:function:function",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model',nil,UIParent); " +
                "local player=CreateFrame('PlayerModel',nil,UIParent); " +
                "return table.concat({type(model.SetUnit),type(model.SetCreature)," +
                "type(model.SetModel),type(player.SetUnit)},':')"));
    }

    [Fact]
    public void PublicPlayerModelTypeChainDoesNotExposeInternalCharacterName()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:true:true:true:false:true:true:true:true",
            session.Lua.Evaluate(
                "local model=CreateFrame('Model',nil,UIParent); " +
                "local player=CreateFrame('PlayerModel',nil,UIParent); " +
                "local dress=CreateFrame('DressUpModel',nil,UIParent); " +
                "local cinematic=CreateFrame('CinematicModel',nil,UIParent); " +
                "local tabard=CreateFrame('TabardModel',nil,UIParent); " +
                "return table.concat({" +
                "tostring(model:IsObjectType('PlayerModel'))," +
                "tostring(player:IsObjectType('PlayerModel'))," +
                "tostring(player:IsObjectType('Model'))," +
                "tostring(dress:IsObjectType('DressUpModel'))," +
                "tostring(dress:IsObjectType('PlayerModel'))," +
                "tostring(dress:IsObjectType('CharacterModel'))," +
                "tostring(dress:IsObjectType('Model'))," +
                "tostring(cinematic:IsObjectType('PlayerModel'))," +
                "tostring(tabard:IsObjectType('PlayerModel'))," +
                "tostring(tabard:IsObjectType('Model'))},':')"));
    }

    [Fact]
    public void NativeCameraLookupsDrivePortraitBlendAndDistanceScale()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata([], 0)
        {
            BoundingBoxMinimum = Vector3.Zero,
            BoundingBoxMaximum = new Vector3(2, 4, 6),
            Cameras =
            [
                new WowModelCameraMetadata(
                    new Vector3(11, 22, 33),
                    new Vector3(4, 5, 6),
                    FieldOfViewTrack: ConstantTrack(1)),
                new WowModelCameraMetadata(
                    new Vector3(101, 202, 303),
                    new Vector3(11, 22, 33),
                    FieldOfViewTrack: ConstantTrack(.8f),
                    FarClip: 500,
                    NearClip: .25f)
            ],
            CameraLookupIndices = [0, 1]
        };
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','CharacterCameraModel',UIParent); " +
            "model:SetPosition(1,2,3); model:SetPortraitZoom(.25); " +
            "model:SetCamDistanceScale(.5); model:SetModel(700)");

        var model = session.Ui.Find("CharacterCameraModel")!;
        Assert.True(model.ModelCharacterCameraActive);
        Assert.True(model.ModelHasCurrentCamera);
        var normalPosition =
            Vector3.Transform(new Vector3(101, 202, 303), model.ModelTransformMatrix) -
            model.ModelPosition;
        var normalTarget =
            Vector3.Transform(new Vector3(11, 22, 33), model.ModelTransformMatrix) -
            model.ModelPosition;
        var portraitPosition =
            Vector3.Transform(new Vector3(11, 22, 33), model.ModelTransformMatrix) -
            model.ModelPosition;
        var portraitTarget =
            Vector3.Transform(new Vector3(4, 5, 6), model.ModelTransformMatrix) -
            model.ModelPosition;
        var blendedPosition = Vector3.Lerp(normalPosition, portraitPosition, .25f);
        var blendedTarget = Vector3.Lerp(normalTarget, portraitTarget, .25f);
        Assert.Equal(
            blendedTarget + (blendedPosition - blendedTarget) * .5f,
            model.ModelCameraPosition);
        Assert.Equal(blendedTarget, model.ModelCameraTarget);
        Assert.Equal(.85f, model.ModelCameraFieldOfView, 5);
        Assert.Equal(.25f, model.ModelCameraNearClip);
        Assert.Equal(500, model.ModelCameraFarClip);

        session.Tick(0);
        Assert.NotNull(model.ModelRenderCameraState);
    }

    [Fact]
    public void MissingNormalCameraLookupUsesTheNativeBoundsFallback()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(701);
        resources.Metadata[701] = new WowModelResourceMetadata([], 0)
        {
            BoundingBoxMinimum = new Vector3(-2, -4, -6),
            BoundingBoxMaximum = new Vector3(6, 8, 10)
        };
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','CharacterFallbackCamera',UIParent); " +
            "model:SetModel(701)");

        var model = session.Ui.Find("CharacterFallbackCamera")!;
        var factor =
            session.Ui.EffectiveScale(model) *
            session.Ui.NormalizedScreenHeight *
            1.6666666f;
        Assert.Equal(new Vector3(5.5555558f, 0, 2.4166667f) * factor,
            model.ModelCameraPosition);
        Assert.Equal(new Vector3(2, 2, 2) * factor, model.ModelCameraTarget);
        Assert.Equal(.5f, model.ModelCameraFieldOfView);
        Assert.Equal(1 / 36f, model.ModelCameraNearClip);
    }

    [Fact]
    public void RotationIsImmediateAndUsesNativeTurnAnimationGracePeriod()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(702);
        resources.Metadata[702] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(0, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(11, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(12, 0, 1000, 0x21, 1, -1, -1)
            ],
            0);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','TurningCharacter',UIParent); " +
            "model:SetModel(702); model:SetRotation(1)");

        var model = session.Ui.Find("TurningCharacter")!;
        Assert.Equal(1, model.ModelYaw);
        Assert.True(model.ModelRotationAnimating);
        Assert.Equal((ushort)11, model.ModelRotationTurnAnimationId);
        Assert.Equal((ushort)11, model.ModelResolvedSequenceId);

        session.Tick(.016);
        Assert.False(model.ModelRotationResumeSkipFrame);
        Assert.Equal((ushort)11, model.ModelResolvedSequenceId);
        session.Tick(.084);
        Assert.Equal((ushort)11, model.ModelResolvedSequenceId);
        session.Tick(.001);
        Assert.False(model.ModelRotationAnimating);
        Assert.Equal((ushort)0, model.ModelResolvedSequenceId);

        session.Lua.Evaluate("TurningCharacter:SetRotation(-1)");
        Assert.Equal(-1, model.ModelYaw);
        Assert.Equal((ushort)12, model.ModelResolvedSequenceId);
        Assert.Equal((ushort)12, model.ModelRotationTurnAnimationId);

        session.Lua.Evaluate("TurningCharacter:SetAnimation(12)");
        Assert.False(model.ModelRotationAnimating);
        Assert.Equal((ushort)12, model.ModelAnimationId);
        Assert.Equal((ushort)12, model.ModelResolvedSequenceId);
        session.Lua.Evaluate("TurningCharacter:SetRotation(0,true)");
        Assert.Equal(0, model.ModelYaw);
        Assert.False(model.ModelRotationAnimating);
        Assert.Equal((ushort)12, model.ModelResolvedSequenceId);

        session.Lua.Evaluate(
            "TurningCharacter:SetAnimation(0); " +
            "TurningCharacter:SetRotation(.5,true); " +
            "TurningCharacter:SetRotation(.75,false)");
        Assert.Equal(.75f, model.ModelYaw);
        Assert.True(model.ModelRotationAnimating);
        Assert.Equal((ushort)11, model.ModelResolvedSequenceId);
    }

    [Fact]
    public void FreezeAnimationUsesTheLowSixteenBitFrameAsSequenceTime()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(703);
        resources.Metadata[703] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(0, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(12, 2, 1000, 0x21, 1, -1, -1)
            ],
            0);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','FrozenCharacter',UIParent); " +
            "model:SetModel(703); model:FreezeAnimation(12,2,65786)");

        var model = session.Ui.Find("FrozenCharacter")!;
        Assert.Equal((ushort)12, model.ModelAnimationId);
        Assert.Equal(2, model.ModelAnimationVariation);
        Assert.Equal(250, model.ModelAnimationFrozenFrame);
        Assert.Equal(250, model.ModelAnimationTimeOffsetMilliseconds);
        Assert.Equal((ushort)12, model.ModelResolvedSequenceId);
        Assert.Equal((ushort)2, model.ModelResolvedSequenceVariation);
        Assert.Equal(250, model.ModelSequenceElapsedMilliseconds, 3);
        Assert.Equal(0, model.ModelSequencePlaybackSpeed);
        Assert.False(model.ModelSequencePlaying);

        session.Tick(.25);
        Assert.Equal(250, model.ModelSequenceElapsedMilliseconds, 3);

        session.Lua.Evaluate("FrozenCharacter:SetAnimation(0)");
        Assert.Equal(-1, model.ModelAnimationFrozenFrame);
        Assert.Equal(0, model.ModelAnimationTimeOffsetMilliseconds);
        Assert.Equal((ushort)0, model.ModelResolvedSequenceId);
        Assert.Equal(1, model.ModelSequencePlaybackSpeed);
        Assert.True(model.ModelSequencePlaying);
    }

    [Fact]
    public void RawResourceReplacementPreservesTheCharacterController()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        foreach (var fileDataId in new uint[] { 704, 705 })
        {
            resources.ExistingFileDataIds.Add(fileDataId);
            resources.Metadata[fileDataId] = new WowModelResourceMetadata(
                [
                    new WowModelSequenceMetadata(0, 0, 1000, 0x21, 1, -1, -1),
                    new WowModelSequenceMetadata(
                        40,
                        2,
                        1000,
                        0x21,
                        1,
                        -1,
                        -1,
                        BlendTimeMilliseconds: 100),
                    new WowModelSequenceMetadata(
                        41,
                        2,
                        1000,
                        0x21,
                        1,
                        -1,
                        -1,
                        BlendTimeMilliseconds: 100)
                ],
                0);
        }
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','PersistentCharacter',UIParent); " +
            "model:SetModel(704); model:SetDoBlend(false); " +
            "model:SetAnimation(40,2); model:SetModel(705)");

        var model = session.Ui.Find("PersistentCharacter")!;
        Assert.False(model.ModelDoBlend);
        Assert.Equal((ushort)40, model.ModelAnimationId);
        Assert.Equal(2, model.ModelAnimationVariation);
        Assert.Equal((ushort)0, model.ModelResolvedSequenceId);

        session.Lua.Evaluate(
            "PersistentCharacter:FreezeAnimation(40,2,250); " +
            "PersistentCharacter:ClearModel()");
        Assert.False(model.ModelResourceLoaded);
        Assert.False(model.ModelDoBlend);
        Assert.Equal((ushort)40, model.ModelAnimationId);
        Assert.Equal(2, model.ModelAnimationVariation);
        Assert.Equal(250, model.ModelAnimationFrozenFrame);
        Assert.Equal(0, model.ModelAnimationSpeed);

        session.Lua.Evaluate(
            "PersistentCharacter:SetModel(704); " +
            "PersistentCharacter:SetRotation(1,true)");
        Assert.Equal(1, model.ModelYaw);
        Assert.False(model.ModelRotationAnimating);
        Assert.Equal((ushort)0, model.ModelResolvedSequenceId);

        session.Lua.Evaluate("PersistentCharacter:SetAnimation(40,2)");
        Assert.Equal(-1, model.ModelAnimationFrozenFrame);
        Assert.Equal((ushort)40, model.ModelResolvedSequenceId);
        Assert.Null(model.ModelSequenceBlendState);

        session.Lua.Evaluate(
            "PersistentCharacter:SetDoBlend(true); " +
            "PersistentCharacter:SetAnimation(41,2)");
        Assert.Equal((ushort)41, model.ModelResolvedSequenceId);
        Assert.NotNull(model.ModelSequenceBlendState);

        session.Lua.Evaluate(
            "local cinematic=CreateFrame('CinematicModel','CinematicController',UIParent); " +
            "cinematic:SetModel(704); cinematic:SetDoBlend(false); " +
            "cinematic:SetAnimation(40,2); cinematic:SetAnimation(41,2)");
        var cinematic = session.Ui.Find("CinematicController")!;
        Assert.Equal((ushort)41, cinematic.ModelResolvedSequenceId);
        Assert.Null(cinematic.ModelSequenceBlendState);
    }

    [Fact]
    public void AnimationKitUsesDb2SegmentsAndNativeLoopHandleLifetime()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(706);
        resources.Metadata[706] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(0, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(40, 2, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(41, 0, 1000, 0x21, 1, -1, -1)
            ],
            0);
        resources.AnimationKits[100] = new WowAnimationKitDefinition(
            100,
            750,
            101,
            0,
            [
                AnimationKitSegment(
                    segmentId: 1000,
                    orderIndex: 0,
                    animationId: 41,
                    boneSets: [new WowAnimationKitBoneSetDefinition(3, 7)]),
                AnimationKitSegment(
                    segmentId: 1001,
                    orderIndex: 1,
                    animationId: 40,
                    animationStartTimeMilliseconds: 125,
                    forcedVariation: 2,
                    speed: 1.5f)
            ]);
        resources.AnimationKits[101] = new WowAnimationKitDefinition(
            101,
            0,
            0,
            0,
            [AnimationKitSegment(1002, 0, 41)]);
        resources.AnimationKits[102] = new WowAnimationKitDefinition(
            102,
            0,
            0,
            0,
            [AnimationKitSegment(
                1003,
                0,
                40,
                startConditionDelayMilliseconds: 100)]);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','AnimationKitCharacter',UIParent); " +
            "model:SetModel(706); model:PlayAnimKit(100,true)");

        var model = session.Ui.Find("AnimationKitCharacter")!;
        Assert.Equal(100, model.ModelAnimationKitId);
        Assert.True(model.ModelAnimationKitLooping);
        Assert.Equal(1001, model.ModelAnimationKitSegmentId);
        Assert.Equal((byte)1, model.ModelAnimationKitSegmentOrderIndex);
        Assert.False(model.ModelAnimationKitSegmentUsesBoneSet);
        Assert.Equal((uint)750, model.ModelAnimationKitOneShotDurationMilliseconds);
        Assert.Equal((ushort)101, model.ModelAnimationKitStopId);
        Assert.Equal((ushort)40, model.ModelResolvedSequenceId);
        Assert.Equal((ushort)2, model.ModelResolvedSequenceVariation);
        Assert.Equal(1.5f, model.ModelSequencePlaybackSpeed);
        Assert.Equal(125, model.ModelSequenceTimeOffset);

        session.Lua.Evaluate("AnimationKitCharacter:SetAnimation(41)");
        Assert.Equal((ushort)41, model.ModelAnimationId);
        Assert.Equal((ushort)40, model.ModelResolvedSequenceId);

        session.Tick(.25);
        session.Tick(.25);
        session.Tick(.249);
        Assert.Equal(100, model.ModelAnimationKitId);
        session.Tick(.002);
        Assert.Equal(101, model.ModelAnimationKitId);
        Assert.False(model.ModelAnimationKitLooping);
        Assert.Equal((ushort)41, model.ModelResolvedSequenceId);

        session.Lua.Evaluate("AnimationKitCharacter:PlayAnimKit(100,true)");

        session.Lua.Evaluate("AnimationKitCharacter:PlayAnimKit(999,true)");
        Assert.Null(model.ModelAnimationKitId);
        Assert.Equal((ushort)41, model.ModelResolvedSequenceId);

        session.Lua.Evaluate(
            "AnimationKitCharacter:PlayAnimKit(100,false); " +
            "AnimationKitCharacter:PlayAnimKit(999,false)");
        Assert.Equal(100, model.ModelAnimationKitId);
        Assert.False(model.ModelAnimationKitLooping);
        Assert.Equal((ushort)40, model.ModelResolvedSequenceId);

        session.Lua.Evaluate(
            "AnimationKitCharacter:PlayAnimKit(100,false); " +
            "AnimationKitCharacter:StopAnimKit()");
        session.Tick(.75);
        Assert.Null(model.ModelAnimationKitId);
        Assert.Null(model.ModelAnimationKitSegmentId);
        Assert.Equal((ushort)41, model.ModelResolvedSequenceId);

        session.Lua.Evaluate("AnimationKitCharacter:PlayAnimKit(102,false)");
        Assert.Equal(102, model.ModelAnimationKitId);
        Assert.Null(model.ModelAnimationKitSegmentId);
        Assert.Equal((ushort)41, model.ModelResolvedSequenceId);
        session.Tick(.099);
        Assert.Null(model.ModelAnimationKitSegmentId);
        session.Tick(.002);
        Assert.Equal(1003, model.ModelAnimationKitSegmentId);
        Assert.Equal((ushort)40, model.ModelResolvedSequenceId);
    }

    [Fact]
    public void AnimationKitRetainsSimultaneousBoneSetTrackRuntimeState()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(709);
        resources.Metadata[709] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(40, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(41, 0, 1000, 0x21, 1, -1, -1)
            ],
            0);
        resources.AnimationKits[103] = new WowAnimationKitDefinition(
            103,
            0,
            0,
            0,
            [
                AnimationKitSegment(
                    1004,
                    0,
                    40,
                    boneSets:
                    [
                        new WowAnimationKitBoneSetDefinition(
                            3,
                            7,
                            BoneDataId: 1,
                            Priority: 4)
                    ]),
                AnimationKitSegment(1005, 1, 41)
            ]);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','LayeredKitCharacter',UIParent); " +
            "model:SetModel(709); model:PlayAnimKit(103,true)");

        var model = session.Ui.Find("LayeredKitCharacter")!;
        var segments = model.ModelAnimationKitRuntimeState!.Segments;
        var boneSetSegment = Assert.Single(
            segments,
            value => value.Definition.SegmentId == 1004);
        var wholeModelSegment = Assert.Single(
            segments,
            value => value.Definition.SegmentId == 1005);
        Assert.Equal(
            WowAnimationKitSegmentPlaybackState.Playing,
            boneSetSegment.PlaybackState);
        Assert.Equal(
            WowAnimationKitSegmentPlaybackState.Playing,
            wholeModelSegment.PlaybackState);
        Assert.Equal(0, boneSetSegment.ResolvedSequenceIndex);
        Assert.Equal(1, wholeModelSegment.ResolvedSequenceIndex);
        Assert.Equal(0, boneSetSegment.StartElapsedMilliseconds);
        Assert.Equal(0, wholeModelSegment.StartElapsedMilliseconds);
        Assert.Equal(1, boneSetSegment.TransformWeight);
        Assert.Equal(1, wholeModelSegment.TransformWeight);
        Assert.Equal((ushort)41, model.ModelResolvedSequenceId);
        Assert.Equal(1005, model.ModelAnimationKitSegmentId);
    }

    [Fact]
    public void SpellVisualKitUsesDb2LookupAccumulationAndZeroClear()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(708);
        resources.Metadata[708] = new WowModelResourceMetadata([], 0);
        resources.SpellVisualKits[500] = new WowSpellVisualKitDefinition(
            500,
            [
                new WowSpellVisualKitEffectDefinition(700, 800, 7),
                new WowSpellVisualKitEffectDefinition(701, 801, 11)
            ]);
        resources.SpellVisualKits[501] = new WowSpellVisualKitDefinition(
            501,
            [new WowSpellVisualKitEffectDefinition(702, 802, 7)]);
        resources.SpellVisualKits[502] = new WowSpellVisualKitDefinition(
            502,
            [new WowSpellVisualKitEffectDefinition(703, 999, 7)]);
        resources.ShadowyEffects[800] = new WowShadowyEffectDefinition(
            800,
            0x00112233,
            0x00445566,
            .4f,
            1,
            .25f,
            .75f);
        resources.ShadowyEffects[802] = new WowShadowyEffectDefinition(
            802,
            0x00204060,
            0x00FFFFFF,
            .6f,
            0,
            .5f,
            .9f);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','SpellVisualCharacter',UIParent); " +
            "model:SetModel(708); model:ApplySpellVisualKit(500); " +
            "model:ApplySpellVisualKit(999,true)");

        var model = session.Ui.Find("SpellVisualCharacter")!;
        Assert.Equal((uint)500, model.ModelSpellVisualKitId);
        Assert.False(model.ModelSpellVisualOneShot);
        var directApplication = Assert.Single(model.ModelAppliedSpellVisualKits);
        Assert.Equal((uint)500, directApplication.Definition.Id);
        Assert.False(directApplication.OneShot);
        Assert.Equal(2, directApplication.Definition.Effects.Count);
        Assert.Equal(UiModelRenderEffectKind.Shadow, model.ModelRenderEffectKind);
        Assert.Equal(.4f, model.ModelShadowEffectStrength);
        var directEffect = Assert.IsType<UiModelShadowEffectState>(
            model.ModelShadowEffectState);
        Assert.Equal(
            new Vector4(0x11 / 255f, 0x22 / 255f, 0x33 / 255f, .25f),
            directEffect.PrimaryColor);
        Assert.Equal(
            new Vector4(0x44 / 255f, 0x55 / 255f, 0x66 / 255f, .75f),
            directEffect.SecondaryColor);

        session.Lua.Evaluate("SpellVisualCharacter:ApplySpellVisualKit(501,true)");
        Assert.Equal((uint)501, model.ModelSpellVisualKitId);
        Assert.True(model.ModelSpellVisualOneShot);
        Assert.Equal(2, model.ModelAppliedSpellVisualKits.Count);
        Assert.Equal(.6f, model.ModelShadowEffectStrength);
        var derivedEffect = Assert.IsType<UiModelShadowEffectState>(
            model.ModelShadowEffectState);
        Assert.Equal(.5f, derivedEffect.PrimaryColor.W);
        Assert.Equal(.9f, derivedEffect.SecondaryColor.W);
        Assert.Equal(derivedEffect.PrimaryColor.X, derivedEffect.SecondaryColor.X, 6);
        Assert.Equal(derivedEffect.PrimaryColor.Y, derivedEffect.SecondaryColor.Y, 6);
        Assert.Equal(derivedEffect.PrimaryColor.Z, derivedEffect.SecondaryColor.Z, 6);

        session.Lua.Evaluate("SpellVisualCharacter:ApplySpellVisualKit(502)");
        Assert.Equal((uint)502, model.ModelSpellVisualKitId);
        Assert.Equal(3, model.ModelAppliedSpellVisualKits.Count);
        Assert.Equal(derivedEffect, model.ModelShadowEffectState);

        session.Lua.Evaluate("SpellVisualCharacter:SetShadowEffect(.2)");
        Assert.Equal(.2f, model.ModelShadowEffectStrength);
        Assert.Null(model.ModelShadowEffectState);

        session.Lua.Evaluate("SpellVisualCharacter:ApplySpellVisualKit(0)");
        Assert.Empty(model.ModelAppliedSpellVisualKits);
        Assert.Null(model.ModelSpellVisualKitId);
        Assert.False(model.ModelSpellVisualOneShot);
        Assert.Equal(UiModelRenderEffectKind.None, model.ModelRenderEffectKind);
        Assert.Equal(0, model.ModelShadowEffectStrength);
        Assert.Null(model.ModelShadowEffectState);
    }

    [Fact]
    public void SpellVisualKitRenderEffectsUseRelationOrderAndSharedSlot()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(709);
        resources.Metadata[709] = new WowModelResourceMetadata([], 0);
        resources.SpellVisualKits[600] = new WowSpellVisualKitDefinition(
            600,
            [
                new WowSpellVisualKitEffectDefinition(710, 900, 7),
                new WowSpellVisualKitEffectDefinition(711, 999, 7),
                new WowSpellVisualKitEffectDefinition(712, 901, 7)
            ]);
        resources.SpellVisualKits[601] = new WowSpellVisualKitDefinition(
            601,
            [new WowSpellVisualKitEffectDefinition(713, 902, 12)]);
        resources.ShadowyEffects[900] = new WowShadowyEffectDefinition(
            900,
            0x00102030,
            0x00405060,
            .2f,
            1,
            .3f,
            .4f);
        resources.ShadowyEffects[901] = new WowShadowyEffectDefinition(
            901,
            0x00708090,
            0x00A0B0C0,
            .7f,
            1,
            .8f,
            .9f);
        resources.EdgeGlowEffects[902] = new WowEdgeGlowEffectDefinition(
            902,
            2.5f,
            new Vector4(.1f, .2f, .3f, .4f),
            1.5f,
            1);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','SpellVisualOrder',UIParent); " +
            "model:SetModel(709); model:ApplySpellVisualKit(600)");

        var model = session.Ui.Find("SpellVisualOrder")!;
        Assert.Equal(UiModelRenderEffectKind.Shadow, model.ModelRenderEffectKind);
        Assert.Equal(.7f, model.ModelShadowEffectStrength);
        var effect = Assert.IsType<UiModelShadowEffectState>(
            model.ModelShadowEffectState);
        Assert.Equal(
            new Vector4(0x70 / 255f, 0x80 / 255f, 0x90 / 255f, .8f),
            effect.PrimaryColor);
        Assert.Equal(
            new Vector4(0xA0 / 255f, 0xB0 / 255f, 0xC0 / 255f, .9f),
            effect.SecondaryColor);

        session.Lua.Evaluate("SpellVisualOrder:ApplySpellVisualKit(601)");
        Assert.Equal(UiModelRenderEffectKind.EdgeGlow, model.ModelRenderEffectKind);
        Assert.Equal(0, model.ModelShadowEffectStrength);
        Assert.Null(model.ModelShadowEffectState);
        var edgeGlow = Assert.IsType<UiModelEdgeGlowEffectState>(
            model.ModelEdgeGlowEffectState);
        Assert.Equal(new Vector4(.1f, .2f, .3f, .4f), edgeGlow.GlowColor);
        Assert.Equal(1.5f, edgeGlow.GlowMultiplier);
        Assert.Equal(2.5f, edgeGlow.FresnelCoefficient);
        Assert.True(edgeGlow.InvertFresnel);

        session.Lua.Evaluate("SpellVisualOrder:SetShadowEffect(.5)");
        Assert.Equal(UiModelRenderEffectKind.Shadow, model.ModelRenderEffectKind);
        Assert.Null(model.ModelEdgeGlowEffectState);
    }

    [Fact]
    public void SpellVisualKitDissolveUsesResolvedTextureBlendSetAndEndValue()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(710);
        resources.Metadata[710] = new WowModelResourceMetadata([], 0);
        resources.SpellVisualKits[602] = new WowSpellVisualKitDefinition(
            602,
            [new WowSpellVisualKitEffectDefinition(714, 903, 11)]);
        var textureBlendSet = new WowTextureBlendSetDefinition(
            100,
            [101u, 102u, 103u],
            2,
            1,
            0,
            3,
            0x44,
            new Vector3(.1f, .2f, .3f),
            new Vector3(.4f, .5f, .6f),
            new Vector3(1.1f, 1.2f, 1.3f),
            new Vector3(1.4f, 1.5f, 1.6f),
            new Vector4(2, 3, 4, 5));
        resources.DissolveEffects[903] = new WowDissolveEffectDefinition(
            903,
            1.75f,
            .1f,
            .65f,
            .2f,
            .3f,
            4,
            9,
            6,
            textureBlendSet,
            2,
            0x2468,
            300,
            7,
            1.25f,
            new Vector4(.7f, .8f, .9f, 1),
            1.1f,
            12,
            2);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','SpellVisualDissolve',UIParent); " +
            "model:SetModel(710); model:ApplySpellVisualKit(602)");

        var model = session.Ui.Find("SpellVisualDissolve")!;
        Assert.Equal(UiModelRenderEffectKind.Dissolve, model.ModelRenderEffectKind);
        Assert.Equal(.65f, model.ModelShadowEffectStrength);
        var dissolve = Assert.IsType<UiModelDissolveEffectState>(
            model.ModelDissolveEffectState);
        Assert.Equal(.65f, dissolve.Strength);
        Assert.Equal(resources.DissolveEffects[903], dissolve.Definition);
        Assert.Equal([101u, 102u, 103u], dissolve.Definition.TextureBlendSet.TextureFileDataIds);
        Assert.Null(model.ModelShadowEffectState);
        Assert.Null(model.ModelEdgeGlowEffectState);

        session.Lua.Evaluate("SpellVisualDissolve:SetShadowEffect(.4)");
        Assert.Equal(UiModelRenderEffectKind.Shadow, model.ModelRenderEffectKind);
        Assert.Null(model.ModelDissolveEffectState);
    }

    [Fact]
    public void AnimationKitSchedulesNativeDependentSegmentTransitions()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(707);
        resources.Metadata[707] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(0, 0, 100, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(40, 0, 100, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(41, 0, 100, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(42, 0, 100, 0x21, 1, -1, -1)
            ],
            0);
        resources.AnimationKits[200] = new WowAnimationKitDefinition(
            200,
            0,
            0,
            0,
            [
                AnimationKitSegment(
                    2000,
                    0,
                    40,
                    endConditionParameter: 1,
                    endConditionDelayMilliseconds: 10),
                AnimationKitSegment(
                    2001,
                    1,
                    41,
                    startCondition: 2,
                    startConditionParameter: 0,
                    startConditionDelayMilliseconds: 20),
                AnimationKitSegment(
                    2002,
                    2,
                    42,
                    startCondition: 1,
                    startConditionParameter: 1,
                    startConditionDelayMilliseconds: 5),
                AnimationKitSegment(
                    2003,
                    3,
                    42,
                    endCondition: 4,
                    endConditionParameter: 1,
                    endConditionDelayMilliseconds: 7)
            ]);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local model=CreateFrame('PlayerModel','DependentKitCharacter',UIParent); " +
            "model:SetModel(707); model:SetAnimation(0); " +
            "model:PlayAnimKit(200,true)");

        var model = session.Ui.Find("DependentKitCharacter")!;
        var segments = model.ModelAnimationKitRuntimeState!.Segments;
        Assert.Equal(2000, model.ModelAnimationKitSegmentId);
        Assert.Equal(
            WowAnimationKitSegmentPlaybackState.Playing,
            segments.Single(value => value.Definition.SegmentId == 2003)
                .PlaybackState);

        session.Tick(.109);
        Assert.Equal(2000, model.ModelAnimationKitSegmentId);
        session.Tick(.002);
        Assert.Equal(2003, model.ModelAnimationKitSegmentId);
        Assert.Equal(
            WowAnimationKitSegmentPlaybackState.Stopped,
            segments.Single(value => value.Definition.SegmentId == 2000)
                .PlaybackState);

        session.Tick(.019);
        Assert.Equal(2003, model.ModelAnimationKitSegmentId);
        session.Tick(.002);
        Assert.Equal(2001, model.ModelAnimationKitSegmentId);
        Assert.Equal(
            WowAnimationKitSegmentPlaybackState.Playing,
            segments.Single(value => value.Definition.SegmentId == 2001)
                .PlaybackState);

        session.Tick(.004);
        Assert.Equal(
            WowAnimationKitSegmentPlaybackState.Inactive,
            segments.Single(value => value.Definition.SegmentId == 2002)
                .PlaybackState);
        session.Tick(.002);
        Assert.Equal(
            WowAnimationKitSegmentPlaybackState.Playing,
            segments.Single(value => value.Definition.SegmentId == 2002)
                .PlaybackState);
        session.Tick(.002);
        Assert.Equal(
            WowAnimationKitSegmentPlaybackState.Stopped,
            segments.Single(value => value.Definition.SegmentId == 2003)
                .PlaybackState);
    }

    private static WowAnimationKitSegmentDefinition AnimationKitSegment(
        int segmentId,
        byte orderIndex,
        ushort animationId,
        uint animationStartTimeMilliseconds = 0,
        byte forcedVariation = byte.MaxValue,
        float speed = 1,
        byte startCondition = 0,
        byte startConditionParameter = 0,
        uint startConditionDelayMilliseconds = 0,
        byte endCondition = 0,
        uint endConditionParameter = 0,
        uint endConditionDelayMilliseconds = 0,
        IReadOnlyList<WowAnimationKitBoneSetDefinition>? boneSets = null) =>
        new(
            segmentId,
            orderIndex,
            animationId,
            animationStartTimeMilliseconds,
            0,
            startCondition,
            startConditionParameter,
            startConditionDelayMilliseconds,
            endCondition,
            endConditionParameter,
            endConditionDelayMilliseconds,
            speed,
            forcedVariation == byte.MaxValue ? 0u : 2u,
            forcedVariation,
            0,
            -1,
            0,
            0,
            0,
            boneSets ?? []);

    private static WowModelAnimationTrack<float> ConstantTrack(float value) =>
        new(
            0,
            -1,
            [new WowModelAnimationTrackSequence<float>(
                [0],
                [new WowModelAnimationTrackKey<float>(value, value, value)])]);

    private static string[] GetOwnedMethods(string fieldName)
    {
        var coreAssembly = typeof(EmulatorSession).Assembly;
        var apiType = coreAssembly.GetType(
            "WoWAddonLab.Emulator.Lua.WowWidgetApi",
            throwOnError: true)!;
        return Assert.IsType<string[]>(
            apiType.GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null));
    }
}
