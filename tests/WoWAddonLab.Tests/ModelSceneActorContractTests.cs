using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class ModelSceneActorContractTests
{
    [Fact]
    public void SetToDefaultsUsesScriptsOnlyResetAndPreservesActorState()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('ActorResetBoundary'); " +
            "actor:SetParentKey('ResetActor'); " +
            "actor:SetAlpha(.375); actor:SetScale(2.5); " +
            "actor:SetPosition(1.25,-2.5,3.75); actor:SetYaw(.625); " +
            "actor:SetDesaturation(.5); actor:SetAnimation(17,3,1.5,.25); " +
            "actor:SetShown(false)");

        var actor = session.Ui.Find("ActorResetBoundary")!;

        actor.Scale = 3.25f;
        actor.Alpha = .625f;
        actor.MouseClickEnabled = true;
        actor.KeyboardEnabled = true;

        session.Lua.Evaluate("ActorResetBoundary:SetToDefaults()");

        Assert.Equal(
            "ResetActor:0.375:2.500:1.250:-2.500:3.750:0.625:0.500:" +
            "17:3:1:false",
            session.Lua.Evaluate(
                "local actor=ActorResetBoundary; local x,y,z=actor:GetPosition(); " +
                "return string.format(" +
                "'%s:%.3f:%.3f:%.3f:%.3f:%.3f:%.3f:%.3f:" +
                "%d:%d:%d:%s',tostring(actor:GetParentKey())," +
                "actor:GetAlpha(),actor:GetScale(),x,y,z,actor:GetYaw()," +
                "actor:GetDesaturation(),actor:GetAnimation()," +
                "actor:GetAnimationVariation(),actor:GetAnimationBlendOperation()," +
                "tostring(actor:IsShown()))"));
        Assert.Equal(3.25f, actor.Scale);
        Assert.Equal(159 / 255f, actor.Alpha);
        Assert.True(actor.MouseClickEnabled);
        Assert.True(actor.KeyboardEnabled);
        Assert.Equal(1.5f, actor.ModelAnimationSpeed);
        Assert.Equal(250, actor.ModelAnimationTimeOffsetMilliseconds);
    }

    [Fact]
    public void NativeEightyMethodOwnedSurfaceIsAdvertised()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "80:0",
            session.Lua.Evaluate(
                "local scene=CreateFrame('ModelScene',nil,UIParent); " +
                "local actor=scene:CreateActor(); local methods={" +
                "'AttachToMount','CalculateMountScale','DetachFromMount','Dress'," +
                "'DressPlayerSlot','GetAutoDress','GetItemTransmogInfo'," +
                "'GetItemTransmogInfoList','GetObeyHideInTransmogFlag','GetPaused'," +
                "'GetSheathed','GetUseTransmogChoices','GetUseTransmogSkin'," +
                "'IsGeoReady','IsSlotAllowed','IsSlotVisible'," +
                "'ReleaseFrontEndCharacterDisplays','ResetNextHandSlot'," +
                "'SetAutoDress','SetFrontEndLobbyModelFromDefaultCharacterDisplay'," +
                "'SetItemTransmogInfo','SetModelByHyperlink'," +
                "'SetObeyHideInTransmogFlag','SetPaused','SetSheathed'," +
                "'SetSheathedCategory','SetUseTransmogChoices','SetUseTransmogSkin'," +
                "'Undress','UndressSlot','UseUnitSheatheCategory','ClearModel'," +
                "'GetActiveBoundingBox','GetAlpha','GetAnimation'," +
                "'GetAnimationBlendOperation','GetAnimationVariation'," +
                "'GetDesaturation','GetMaxBoundingBox','GetModelFileID'," +
                "'GetModelPath','GetModelUnitGUID','GetParticleOverrideScale'," +
                "'GetPitch','GetPosition','GetRoll','GetScale','GetSpellVisualKit'," +
                "'GetYaw','Hide','IsLoaded','IsPreferringModelCollisionBounds'," +
                "'IsShown','IsUsingCenterForOrigin','IsVisible','PlayAnimationKit'," +
                "'SetAlpha','SetAnimation','SetAnimationBlendOperation'," +
                "'SetDesaturation','SetGradientMask','SetGradientMaskWithDyes'," +
                "'SetModelByCreatureDisplayID','SetModelByFileID','SetModelByPath'," +
                "'SetModelByUnit','SetParticleOverrideScale','SetPitch'," +
                "'SetPlayerModelFromGlues','SetPosition'," +
                "'SetPreferModelCollisionBounds','SetRoll','SetScale','SetShown'," +
                "'SetSpellVisualKit','SetUseCenterForOrigin','SetYaw','Show'," +
                "'StopAnimationKit','TryOn'}; local missing=0; " +
                "for _,name in ipairs(methods) do " +
                "if type(actor[name])~='function' then missing=missing+1 end end; " +
                "return #methods..':'..missing"));
    }

    [Fact]
    public void ActorAlphaScaleVisibilityAndFlagsUseActorSpecificNativeState()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0.1234:0.010:0.125:false:true:false:true:false:" +
            "true:false:true:nil:0.010:nil:false:true",
            session.Lua.Evaluate(
                "local scene=CreateFrame('ModelScene',nil,UIParent); " +
                "local actor=scene:CreateActor(); actor:SetAlpha(.1234); " +
                "local alpha=actor:GetAlpha(); actor:SetScale(-4); " +
                "local minimumScale=actor:GetScale(); actor:SetScale(.125); " +
                "local scale=actor:GetScale(); actor:SetShown(nil); " +
                "local explicitNil=actor:IsShown(); actor:SetShown(); " +
                "local omitted=actor:IsShown(); actor:Hide(); " +
                "local hidden=actor:IsShown(); actor:Show(); " +
                "local shown=actor:IsShown(); scene:Hide(); " +
                "local ownerHidden=actor:IsVisible(); scene:Show(); " +
                "actor:SetUseCenterForOrigin(true,false,true); " +
                "local x,y,z=actor:IsUsingCenterForOrigin(); " +
                "local absent=actor:GetParticleOverrideScale(); " +
                "actor:SetParticleOverrideScale(-3); " +
                "local particle=actor:GetParticleOverrideScale(); " +
                "actor:SetParticleOverrideScale(nil); " +
                "local cleared=actor:GetParticleOverrideScale(); " +
                "actor:SetPreferModelCollisionBounds(nil); " +
                "local preferredNil=actor:IsPreferringModelCollisionBounds(); " +
                "actor:SetPreferModelCollisionBounds(true); " +
                "return string.format(" +
                "'%.4f:%.3f:%.3f:%s:%s:%s:%s:%s:%s:%s:%s:%s:%.3f:%s:%s:%s'," +
                "alpha,minimumScale,scale,tostring(explicitNil),tostring(omitted)," +
                "tostring(hidden),tostring(shown),tostring(ownerHidden)," +
                "tostring(x),tostring(y),tostring(z),tostring(absent)," +
                "particle,tostring(cleared),tostring(preferredNil)," +
                "tostring(actor:IsPreferringModelCollisionBounds()))"));
    }

    [Fact]
    public void AutomaticTransformUsesIndependentCenterOriginAxes()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(0, 0, 1000, 1, 1, -1, -1)
                {
                    BoundingBoxMinimum = new Vector3(2, 4, 6),
                    BoundingBoxMaximum = new Vector3(6, 10, 14)
                }
            ],
            0)
        {
            BoundingBoxMinimum = new Vector3(2, 4, 6),
            BoundingBoxMaximum = new Vector3(6, 10, 14)
        };
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene','ActorTransformScene',UIParent); " +
            "scene:SetScale(1.25); " +
            "local actor=scene:CreateActor('ActorIndependentOrigin'); " +
            "actor:SetScale(2); actor:SetPosition(1,2,3); " +
            "actor:SetUseCenterForOrigin(true,false,true); " +
            "actor:SetModelByFileID(700)");
        var actor = session.Ui.Find("ActorIndependentOrigin")!;
        var factor =
            2 *
            session.Ui.EffectiveScale(actor) *
            session.Ui.NormalizedScreenHeight *
            1.6666666f;

        session.Tick(0);

        Assert.Equal(new Vector3(4, 7, 10), actor.ModelCenter);
        Assert.Equal(factor, actor.ModelTransformMatrix.M11, 5);
        Assert.Equal(-3 * factor, actor.ModelTransformMatrix.M41, 5);
        Assert.Equal(2 * factor, actor.ModelTransformMatrix.M42, 5);
        Assert.Equal(-7 * factor, actor.ModelTransformMatrix.M43, 5);

        session.Lua.Evaluate(
            "ActorIndependentOrigin:SetUseCenterForOrigin(false,true,false)");
        session.Tick(0);

        Assert.Equal(factor, actor.ModelTransformMatrix.M41, 5);
        Assert.Equal(-5 * factor, actor.ModelTransformMatrix.M42, 5);
        Assert.Equal(3 * factor, actor.ModelTransformMatrix.M43, 5);
    }

    [Fact]
    public void LoadedActorSelectsCollisionOrAnimationBoundsLikeNativeClient()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(701);
        resources.Metadata[701] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(0, 0, 1000, 1, 1, -1, -1)
                {
                    BoundingBoxMinimum = new Vector3(-2, -4, -6),
                    BoundingBoxMaximum = new Vector3(4, 8, 12)
                },
                new WowModelSequenceMetadata(7, 0, 1000, 1, 1, -1, -1)
                {
                    BoundingBoxMinimum = new Vector3(-9, -8, -7),
                    BoundingBoxMaximum = new Vector3(6, 5, 4)
                }
            ],
            0)
        {
            BoundingBoxMinimum = new Vector3(-10, -20, -30),
            BoundingBoxMaximum = new Vector3(20, 30, 40),
            CollisionBoundingBoxMinimum = new Vector3(1, 3, 5),
            CollisionBoundingBoxMaximum = new Vector3(7, 11, 15),
            HasCollisionGeometry = true
        };
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('ActorSelectedBounds'); " +
            "actor:SetPreferModelCollisionBounds(true); " +
            "actor:SetUseCenterForOrigin(true,true,true); " +
            "actor:SetModelByFileID(701)");
        var actor = session.Ui.Find("ActorSelectedBounds")!;
        session.Tick(0);

        Assert.Equal(new Vector3(4, 7, 10), actor.ModelCenter);
        Assert.Equal(
            "1:3:5:7:11:15|-10:-20:-30:20:30:40",
            session.Lua.Evaluate(
                "local a,b,c,d,e,f=ActorSelectedBounds:GetActiveBoundingBox(); " +
                "local g,h,i,j,k,l=ActorSelectedBounds:GetMaxBoundingBox(); " +
                "return table.concat({a,b,c,d,e,f},':')..'|'.." +
                "table.concat({g,h,i,j,k,l},':')"));

        var collisionOriginTransform = actor.ModelTransformMatrix;
        session.Lua.Evaluate(
            "ActorSelectedBounds:SetPreferModelCollisionBounds(false)");
        session.Tick(0);

        Assert.Equal(
            "-2:-4:-6:4:8:12",
            session.Lua.Evaluate(
                "return table.concat(" +
                "{ActorSelectedBounds:GetActiveBoundingBox()},':')"));
        Assert.Equal(new Vector3(4, 7, 10), actor.ModelCenter);
        Assert.Equal(collisionOriginTransform, actor.ModelTransformMatrix);

        session.Lua.Evaluate("ActorSelectedBounds:SetAnimation(7)");
        Assert.Equal(
            "-9:-8:-7:6:5:4",
            session.Lua.Evaluate(
                "return table.concat(" +
                "{ActorSelectedBounds:GetActiveBoundingBox()},':')"));

        session.Lua.Evaluate(
            "ActorSelectedBounds:SetPreferModelCollisionBounds(true); " +
            "ActorSelectedBounds:SetAnimation(0)");
        Assert.Equal(
            "1:3:5:7:11:15",
            session.Lua.Evaluate(
                "return table.concat(" +
                "{ActorSelectedBounds:GetActiveBoundingBox()},':')"));
    }

    [Fact]
    public void ActorModelIdentityBoundsAndGlueFallbackUseNativeResultShapes()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "scene:CreateActor('ActorIdentityBounds')");

        Assert.Equal(
            "nil:6:true:Player-0-00000001:0:false:false",
            session.Lua.Evaluate(
                "local actor=ActorIdentityBounds; " +
                "local initialGuid=actor:GetModelUnitGUID(); " +
                "local a,b,c,d,e,f=actor:GetMaxBoundingBox(); " +
                "local unitResult=actor:SetModelByUnit('player'); " +
                "local guid=actor:GetModelUnitGUID(); actor:ClearModel(); " +
                "local file=actor:GetModelFileID(); " +
                "local glue=actor:SetPlayerModelFromGlues(); " +
                "local invalid=pcall(actor.SetPlayerModelFromGlues,actor,{}); " +
                "return table.concat({tostring(initialGuid)," +
                "select('#',actor:GetMaxBoundingBox()),tostring(a==nil and b==nil " +
                "and c==nil and d==nil and e==nil and f==nil)," +
                "tostring(unitResult and guid or nil),file,tostring(glue)," +
                "tostring(invalid)},':')"));

        var actor = session.Ui.Find("ActorIdentityBounds")!;
        actor.ModelMaxBoundingBoxMinimum = new Vector3(-7, -8, -9);
        actor.ModelMaxBoundingBoxMaximum = new Vector3(10, 11, 12);

        Assert.Equal(
            "-7.0:-8.0:-9.0:10.0:11.0:12.0",
            session.Lua.Evaluate(
                "local a,b,c,d,e,f=ActorIdentityBounds:GetMaxBoundingBox(); " +
                "return string.format('%.1f:%.1f:%.1f:%.1f:%.1f:%.1f'," +
                "a,b,c,d,e,f)"));
    }

    [Fact]
    public void ReadyFileResourceReturnsSuccessAndInvalidReplacementIsIgnored()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        session.ModelResourceProvider = resources;

        Assert.Equal(
            "true:true:false:true:700",
            session.Lua.Evaluate(
                "local scene=CreateFrame('ModelScene',nil,UIParent); " +
                "local actor=scene:CreateActor('ActorLifecycleCallbacks'); " +
                "local success=actor:SetModelByFileID(700); local loaded=actor:IsLoaded(); " +
                "local invalid=actor:SetModelByFileID(701); local preserved=actor:GetModelFileID(); " +
                "actor:ClearModel(); return table.concat({tostring(success),tostring(loaded)," +
                "tostring(invalid),tostring(not actor:IsLoaded()),preserved},':')"));
    }

    [Fact]
    public void FileResourceUseMipsArgumentControlsNativeTextureResidencyMode()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.UnionWith([700, 701]);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local full=scene:CreateActor('FullResolutionActor'); " +
            "local mipped=scene:CreateActor('MippedActor'); " +
            "full:SetModelByFileID(700); mipped:SetModelByFileID(701,true)");

        Assert.True(session.Ui.Find("FullResolutionActor")!.ModelNoMip);
        Assert.False(session.Ui.Find("MippedActor")!.ModelNoMip);
    }

    [Fact]
    public void ClearModelStopsOnlyResourceOwnedStateAndPreservesActorConfiguration()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        session.ModelResourceProvider = resources;

        Assert.Equal(
            "42:3:1:789:0.75:false:false:0",
            session.Lua.Evaluate(
                "local scene=CreateFrame('ModelScene',nil,UIParent); " +
                "local actor=scene:CreateActor('ActorClearContract'); " +
                "actor:SetModelByFileID(700); " +
                "actor:SetAnimation(42,3,2,1.25); " +
                "actor:SetAnimationBlendOperation(1); " +
                "actor:SetSpellVisualKit(789,true); " +
                "actor:SetDesaturation(.75); actor:PlayAnimationKit(456,true); " +
                "actor:SetPaused(true,false); " +
                "local clearResults=select('#',actor:ClearModel('ignored')); " +
                "local paused,globalPaused=actor:GetPaused(); " +
                "return table.concat({actor:GetAnimation(),actor:GetAnimationVariation()," +
                "actor:GetAnimationBlendOperation(),actor:GetSpellVisualKit()," +
                "string.format('%.2f',actor:GetDesaturation())," +
                "tostring(paused),tostring(globalPaused),clearResults},':')"));

        var actor = session.Ui.Find("ActorClearContract")!;
        Assert.Null(actor.ModelAnimationKitId);
        Assert.False(actor.ModelAnimationKitLooping);
        Assert.Equal((uint)789, actor.ModelSpellVisualKitId);
        Assert.True(actor.ModelSpellVisualOneShot);
        Assert.Equal(2, actor.ModelAnimationSpeed);
        Assert.Equal(1250, actor.ModelAnimationTimeOffsetMilliseconds);
    }

    [Fact]
    public void LoadedActorAppliesExplicitVariationAndNativeRepeatClock()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(42, 0, 100, 0x21, 1, 1, -1),
                new WowModelSequenceMetadata(
                    42,
                    1,
                    200,
                    0x21,
                    1,
                    -1,
                    -1,
                    MinimumRepetitions: 3,
                    MaximumRepetitions: 3)
            ],
            0);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('AnimatedActor'); " +
            "actor:SetAnimation(42,1,2,.025); " +
            "actor:SetModelByFileID(700)");

        var actor = session.Ui.Find("AnimatedActor")!;
        Assert.Equal(1, actor.ModelSelectedSequenceIndex);
        Assert.Equal(1, actor.ModelResolvedSequenceIndex);
        Assert.Equal((ushort)1, actor.ModelResolvedSequenceVariation);
        Assert.Equal(200u, actor.ModelResolvedSequenceDurationMilliseconds);
        Assert.Equal(3u, actor.ModelSequenceRepeatCount);
        Assert.Equal(2f, actor.ModelSequencePlaybackSpeed);
        Assert.Equal(22, actor.ModelSequenceElapsedMilliseconds, 3);
        Assert.True(actor.ModelSequencePlaying);

        session.Tick(.05);
        Assert.Equal(122, actor.ModelSequenceElapsedMilliseconds, 3);
        Assert.Equal(
            122,
            WowModelSequencePlayback.ResolveSampleTimeMilliseconds(actor),
            3);

        session.Tick(.25);
        Assert.False(actor.ModelSequencePlaying);
        Assert.Equal(600, actor.ModelSequenceElapsedMilliseconds, 3);
        Assert.Equal(
            200,
            WowModelSequencePlayback.ResolveSampleTimeMilliseconds(actor),
            3);
    }

    [Fact]
    public void AnimationKitRequestReplaysWhenActorResourceBecomesReady()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(0, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(40, 0, 1000, 0x21, 1, -1, -1)
            ],
            0);
        resources.AnimationKits[300] = new WowAnimationKitDefinition(
            300,
            0,
            0,
            0,
            [
                new WowAnimationKitSegmentDefinition(
                    3000,
                    0,
                    40,
                    50,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    1.25f,
                    0,
                    0,
                    0,
                    -1,
                    0,
                    0,
                    0,
                    [])
            ]);
        session.ModelResourceProvider = resources;

        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('AnimationKitActor'); " +
            "actor:SetAnimation(0); actor:PlayAnimationKit(300,true); " +
            "actor:SetModelByFileID(700)");

        var actor = session.Ui.Find("AnimationKitActor")!;
        Assert.Equal(300, actor.ModelAnimationKitId);
        Assert.True(actor.ModelAnimationKitLooping);
        Assert.Equal(3000, actor.ModelAnimationKitSegmentId);
        Assert.Equal((ushort)40, actor.ModelResolvedSequenceId);
        Assert.Equal(1.25f, actor.ModelSequencePlaybackSpeed);
        Assert.Equal(50, actor.ModelSequenceTimeOffset);

        session.Lua.Evaluate("AnimationKitActor:StopAnimationKit()");
        Assert.Null(actor.ModelAnimationKitId);
        Assert.Null(actor.ModelAnimationKitSegmentId);
        Assert.Equal((ushort)0, actor.ModelResolvedSequenceId);
    }

    [Fact]
    public void ReverseActorPlaybackUsesNativeReverseDurationAndStopsAtZero()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [new WowModelSequenceMetadata(42, 0, 200, 0x21, 1, -1, -1)],
            0);
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('ReverseActor'); " +
            "actor:SetModelByFileID(700); actor:SetAnimation(42,0,-1,.025)");

        var actor = session.Ui.Find("ReverseActor")!;
        Assert.Equal(25, actor.ModelSequenceElapsedMilliseconds, 3);
        Assert.True(actor.ModelSequencePlaying);

        session.Tick(.024);
        Assert.Equal(1, actor.ModelSequenceElapsedMilliseconds, 3);
        Assert.True(actor.ModelSequencePlaying);
        session.Tick(.002);
        Assert.Equal(0, actor.ModelSequenceElapsedMilliseconds, 3);
        Assert.False(actor.ModelSequencePlaying);
    }

    [Fact]
    public void NormalBlendOperationPreservesAndAdvancesTheSecondaryPose()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(
                    40,
                    0,
                    1000,
                    0x221,
                    1,
                    -1,
                    -1,
                    BlendTimeMilliseconds: (150u << 16) | 25u),
                new WowModelSequenceMetadata(
                    41,
                    0,
                    1000,
                    0x21,
                    1,
                    -1,
                    -1,
                    BlendTimeMilliseconds: 100)
            ],
            0);
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('BlendedActor'); " +
            "actor:SetModelByFileID(700); " +
            "actor:SetAnimation(40,0,1,.1)");

        var actor = session.Ui.Find("BlendedActor")!;
        Assert.Equal(99, actor.ModelSequenceElapsedMilliseconds, 3);
        session.Lua.Evaluate("BlendedActor:SetAnimation(41)");
        var blend = Assert.IsType<WowModelSequenceBlendState>(
            actor.ModelSequenceBlendState);
        Assert.Equal(0, blend.SequenceIndex);
        Assert.Equal(99, blend.SequenceElapsedMilliseconds, 3);
        Assert.Equal(150u, blend.TransitionDurationMilliseconds);
        Assert.Equal(1, WowModelSequencePlayback.ResolveSecondaryPoseWeight(blend), 5);

        session.Tick(.075);

        blend = Assert.IsType<WowModelSequenceBlendState>(
            actor.ModelSequenceBlendState);
        Assert.Equal(174, blend.SequenceElapsedMilliseconds, 3);
        var normalizedRemaining = 76f / 150f;
        var expectedWeight = normalizedRemaining * normalizedRemaining *
                             (3 - 2 * normalizedRemaining);
        Assert.Equal(
            expectedWeight,
            WowModelSequencePlayback.ResolveSecondaryPoseWeight(blend),
            5);
        Assert.Equal(74, actor.ModelSequenceElapsedMilliseconds, 3);

        session.Tick(.075);
        Assert.NotNull(actor.ModelSequenceBlendState);
        session.Tick(.001);
        Assert.Null(actor.ModelSequenceBlendState);
    }

    [Fact]
    public void CutBlendOperationDoesNotCreateASecondaryPose()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(40, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(
                    41,
                    0,
                    1000,
                    0x21,
                    1,
                    -1,
                    -1,
                    BlendTimeMilliseconds: 100)
            ],
            0);
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('CutActor'); " +
            "actor:SetModelByFileID(700); actor:SetAnimation(40); " +
            "actor:SetAnimationBlendOperation(0); actor:SetAnimation(41)");

        Assert.Null(session.Ui.Find("CutActor")!.ModelSequenceBlendState);
    }

    [Fact]
    public void ExternalAnimationRequestWaitsForPayloadAndRetainsLatestSnapshot()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(40, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(41, 0, 1000, 0x1, 1, -1, -1)
            ],
            0)
        {
            AnimationFiles = [new WowModelAnimationFileMetadata(41, 0, 701)]
        };
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('QueuedActor'); " +
            "actor:SetModelByFileID(700); actor:SetAnimation(40); " +
            "actor:SetAnimation(41,0,1,.1); actor:SetAnimation(41,0,1,.2)");

        var actor = session.Ui.Find("QueuedActor")!;
        Assert.Equal((ushort)41, actor.ModelAnimationId);
        Assert.Equal((ushort)40, actor.ModelResolvedSequenceId);
        var pending = Assert.Single(actor.ModelPendingAnimationRequests);
        Assert.Equal(200, pending.TimeOffsetMilliseconds);

        session.Tick(.01);
        Assert.Equal((ushort)40, actor.ModelResolvedSequenceId);
        Assert.Single(actor.ModelPendingAnimationRequests);

        resources.ExistingFileDataIds.Add(701);
        session.Tick(.01);

        Assert.Empty(actor.ModelPendingAnimationRequests);
        Assert.Equal((ushort)41, actor.ModelResolvedSequenceId);
        Assert.Equal(209, actor.ModelSequenceElapsedMilliseconds, 3);
    }

    [Fact]
    public void FailedExternalAnimationPayloadDropsPendingRequestWithoutInterruptingTrack()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [
                new WowModelSequenceMetadata(40, 0, 1000, 0x21, 1, -1, -1),
                new WowModelSequenceMetadata(41, 0, 1000, 0x1, 1, -1, -1)
            ],
            0)
        {
            AnimationFiles = [new WowModelAnimationFileMetadata(41, 0, 701)]
        };
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('FailedActor'); " +
            "actor:SetModelByFileID(700); actor:SetAnimation(40); " +
            "actor:SetAnimation(41)");

        var actor = session.Ui.Find("FailedActor")!;
        Assert.Single(actor.ModelPendingAnimationRequests);
        Assert.Equal((ushort)40, actor.ModelResolvedSequenceId);

        resources.FailedAnimationFileDataIds.Add(701);
        session.Tick(.01);

        Assert.Empty(actor.ModelPendingAnimationRequests);
        Assert.Equal((ushort)41, actor.ModelAnimationId);
        Assert.Equal((ushort)40, actor.ModelResolvedSequenceId);

        session.Lua.Evaluate("FailedActor:SetAnimation(41)");
        Assert.Empty(actor.ModelPendingAnimationRequests);
        Assert.Equal((ushort)40, actor.ModelResolvedSequenceId);
    }

    [Fact]
    public void NonResidentAnimationWithoutAfidMappingRemainsPending()
    {
        using var session = new EmulatorSession();
        var resources = new TestModelResourceProvider();
        resources.ExistingFileDataIds.Add(700);
        resources.Metadata[700] = new WowModelResourceMetadata(
            [new WowModelSequenceMetadata(41, 0, 1000, 0x1, 1, -1, -1)],
            0);
        session.ModelResourceProvider = resources;
        session.Lua.Evaluate(
            "local scene=CreateFrame('ModelScene',nil,UIParent); " +
            "local actor=scene:CreateActor('MissingAfidActor'); " +
            "actor:SetModelByFileID(700); actor:SetAnimation(41)");

        var actor = session.Ui.Find("MissingAfidActor")!;
        Assert.Single(actor.ModelPendingAnimationRequests);
        Assert.Equal(-1, actor.ModelResolvedSequenceIndex);

        session.Tick(.01);

        Assert.Single(actor.ModelPendingAnimationRequests);
        Assert.Equal(-1, actor.ModelResolvedSequenceIndex);
    }
}
