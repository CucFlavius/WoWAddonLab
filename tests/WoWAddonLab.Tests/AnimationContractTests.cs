using System.Reflection;

namespace WoWAddonLab.Tests;

public sealed class AnimationContractTests
{
    [Fact]
    public void AnimatableObjectsEnumerateAndStopOwnedGroups()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "2:false:false:false:false",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local first=owner:CreateAnimationGroup(); " +
                "first:CreateAnimation('Alpha'):SetDuration(10); " +
                "local second=owner:CreateAnimationGroup(); " +
                "second:CreateAnimation('Alpha'):SetDuration(10); " +
                "first:Play(); second:Play(); local groups={owner:GetAnimationGroups()}; " +
                "owner:StopAnimating(); return table.concat({#groups," +
                "tostring(first:IsPlaying()),tostring(first:IsPaused())," +
                "tostring(second:IsPlaying()),tostring(second:IsPaused())},':')"));
    }

    [Fact]
    public void GroupPlaybackDispatchesChildLifecycleBeforeGroupCompletion()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "group-play,first-play,first-update:1.0,first-finished:nil," +
            "second-play,second-update:1.0,second-finished:nil," +
            "group-update:2.0,group-finished:false",
            session.Lua.Evaluate(
                "local events={}; local function add(value) events[#events+1]=value end; " +
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local first=group:CreateAnimation('Alpha'); first:SetDuration(1); " +
                "local second=group:CreateAnimation('Alpha'); second:SetDuration(1); " +
                "second:SetOrder(2); " +
                "group:SetScript('OnPlay',function() add('group-play') end); " +
                "group:SetScript('OnUpdate',function(_,elapsed) " +
                "add(string.format('group-update:%.1f',elapsed)) end); " +
                "group:SetScript('OnFinished',function(_,requested) " +
                "add('group-finished:'..tostring(requested)) end); " +
                "first:SetScript('OnPlay',function() add('first-play') end); " +
                "first:SetScript('OnUpdate',function(_,elapsed) " +
                "add(string.format('first-update:%.1f',elapsed)) end); " +
                "first:SetScript('OnFinished',function(_,requested) " +
                "add('first-finished:'..tostring(requested)) end); " +
                "second:SetScript('OnPlay',function() add('second-play') end); " +
                "second:SetScript('OnUpdate',function(_,elapsed) " +
                "add(string.format('second-update:%.1f',elapsed)) end); " +
                "second:SetScript('OnFinished',function(_,requested) " +
                "add('second-finished:'..tostring(requested)) end); " +
                "group:Play(false,2); return table.concat(events,',')"));
    }

    [Fact]
    public void PauseResumeAndStopUseNativeChildBeforeGroupOrdering()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "group-play,child-play,child-update:0.0,group-update:0.0," +
            "child-pause,group-pause,group-play,child-play," +
            "child-update:0.0,group-update:0.0,child-stop:true,group-stop:true",
            session.Lua.Evaluate(
                "local events={}; local function add(value) events[#events+1]=value end; " +
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local child=group:CreateAnimation('Alpha'); child:SetDuration(10); " +
                "group:SetScript('OnPlay',function() add('group-play') end); " +
                "group:SetScript('OnUpdate',function(_,elapsed) " +
                "add(string.format('group-update:%.1f',elapsed)) end); " +
                "group:SetScript('OnPause',function() add('group-pause') end); " +
                "group:SetScript('OnStop',function(_,requested) " +
                "add('group-stop:'..tostring(requested)) end); " +
                "child:SetScript('OnPlay',function() add('child-play') end); " +
                "child:SetScript('OnUpdate',function(_,elapsed) " +
                "add(string.format('child-update:%.1f',elapsed)) end); " +
                "child:SetScript('OnPause',function() add('child-pause') end); " +
                "child:SetScript('OnStop',function(_,requested) " +
                "add('child-stop:'..tostring(requested)) end); " +
                "group:Play(); group:Pause(); group:Play(); group:Stop(); " +
                "return table.concat(events,',')"));
    }

    [Fact]
    public void LoopCallbackReportsDirectionBeforeTheNextChildPlay()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "group-play,child-play,child-finished:nil,loop:REVERSE," +
            "child-play,group-update:1.0",
            session.Lua.Evaluate(
                "local events={}; local function add(value) events[#events+1]=value end; " +
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); group:SetLooping('BOUNCE'); " +
                "local child=group:CreateAnimation('Alpha'); child:SetDuration(1); " +
                "group:SetScript('OnPlay',function() add('group-play') end); " +
                "group:SetScript('OnLoop',function(_,direction) " +
                "add('loop:'..direction) end); " +
                "group:SetScript('OnUpdate',function(_,elapsed) " +
                "add(string.format('group-update:%.1f',elapsed)) end); " +
                "child:SetScript('OnPlay',function() add('child-play') end); " +
                "child:SetScript('OnFinished',function(_,requested) " +
                "add('child-finished:'..tostring(requested)) end); " +
                "group:Play(false,1); return table.concat(events,',')"));
    }

    [Fact]
    public void EmptyGroupsDoNotEnterPlaybackOrDispatchScripts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:0",
            session.Lua.Evaluate(
                "local count=0; local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "group:SetScript('OnPlay',function() count=count+1 end); " +
                "group:SetScript('OnFinished',function() count=count+1 end); " +
                "group:Play(); return tostring(group:IsPlaying())..':'..count"));
    }

    [Fact]
    public void StoppedAnimationsDoNotReceiveGenericFrameUpdates()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "AnimationUpdateCount=0; " +
            "local owner=CreateFrame('Frame',nil,UIParent); " +
            "local group=owner:CreateAnimationGroup(); " +
            "local child=group:CreateAnimation('Alpha'); " +
            "child:SetScript('OnUpdate',function() AnimationUpdateCount=AnimationUpdateCount+1 end)");

        session.Tick(0.1);

        Assert.Equal("0", session.Lua.Evaluate("return AnimationUpdateCount"));
    }

    [Fact]
    public void NaturalCompletionRestoresBaseAlphaUnlessFinalAlphaIsRequested()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0.800000:0.200000",
            session.Lua.Evaluate(
                "local restored=CreateFrame('Frame',nil,UIParent); restored:SetAlpha(.8); " +
                "local restoredGroup=restored:CreateAnimationGroup(); " +
                "local restoredAlpha=restoredGroup:CreateAnimation('Alpha'); " +
                "restoredAlpha:SetDuration(1); restoredAlpha:SetFromAlpha(0); " +
                "restoredAlpha:SetToAlpha(.2); restoredGroup:Play(false,1); " +
                "local committed=CreateFrame('Frame',nil,UIParent); committed:SetAlpha(.8); " +
                "local committedGroup=committed:CreateAnimationGroup(); " +
                "committedGroup:SetToFinalAlpha(true); " +
                "local committedAlpha=committedGroup:CreateAnimation('Alpha'); " +
                "committedAlpha:SetDuration(1); committedAlpha:SetFromAlpha(0); " +
                "committedAlpha:SetToAlpha(.2); committedGroup:Play(false,1); " +
                "return string.format('%.6f:%.6f',restored:GetAlpha(),committed:GetAlpha())"));
    }

    [Fact]
    public void RequestedStopRestoresAlphaAndTransformOverlays()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local owner=CreateFrame('Frame','StoppedAnimationOwner',UIParent); " +
            "owner:SetAlpha(.8); local group=owner:CreateAnimationGroup(); " +
            "group:SetToFinalAlpha(true); " +
            "local alpha=group:CreateAnimation('Alpha'); alpha:SetDuration(10); " +
            "alpha:SetFromAlpha(0); alpha:SetToAlpha(.2); " +
            "local move=group:CreateAnimation('Translation'); move:SetDuration(10); " +
            "move:SetOffset(100,50); group:Play(false,5); group:Stop()");

        var owner = session.Ui.Find("StoppedAnimationOwner")!;
        Assert.Equal(204 / 255f, owner.Alpha, 6);
        Assert.Equal(System.Numerics.Vector2.Zero, owner.AnimationOffset);
        Assert.Equal(System.Numerics.Vector2.One, owner.AnimationScale);
        Assert.Equal(0, owner.AnimationRotation);
    }

    [Fact]
    public void VertexColorRemainsAppliedWhenAnimationTargetsDetach()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0.498039:0.494118:0.498039:0.498039:" +
            "0.901961:0.800000:0.701961:0.600000",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local texture=owner:CreateTexture(); " +
                "texture:SetColorTexture(.2,.3,.4,.5); " +
                "local group=texture:CreateAnimationGroup(); " +
                "local color=group:CreateAnimation('VertexColor'); " +
                "color:SetDuration(60); " +
                "color:SetStartColor(CreateColor(.1,.2,.3,.4)); " +
                "color:SetEndColor(CreateColor(.9,.8,.7,.6)); " +
                "group:Play(false,30); group:Stop(); " +
                "local sr,sg,sb,sa=texture:GetVertexColor(); " +
                "group:Play(false,60); " +
                "local er,eg,eb,ea=texture:GetVertexColor(); " +
                "return string.format(" +
                "'%.6f:%.6f:%.6f:%.6f:%.6f:%.6f:%.6f:%.6f'," +
                "sr,sg,sb,sa,er,eg,eb,ea)"));
    }

    [Fact]
    public void LaterOrdersDoNotApplyTheirStartValuesBeforeActivation()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0.200000:0.301961:0.400000:0.501961",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local texture=owner:CreateTexture(); " +
                "texture:SetVertexColor(.2,.3,.4,.5); " +
                "local group=texture:CreateAnimationGroup(); " +
                "local first=group:CreateAnimation('Translation'); " +
                "first:SetDuration(10); first:SetOffset(10,0); " +
                "local later=group:CreateAnimation('VertexColor'); " +
                "later:SetOrder(2); later:SetDuration(10); " +
                "later:SetStartColor(CreateColor(1,0,0,1)); " +
                "later:SetEndColor(CreateColor(0,0,1,1)); " +
                "group:Play(false,5); " +
                "local r,g,b,a=texture:GetVertexColor(); " +
                "return string.format('%.6f:%.6f:%.6f:%.6f',r,g,b,a)"));
    }

    [Fact]
    public void StoppingTheCurrentChildAdvancesToTheNextOrder()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "AnimationOrderEvents={}; " +
            "local function add(value) " +
            "AnimationOrderEvents[#AnimationOrderEvents+1]=value end; " +
            "local owner=CreateFrame('Frame',nil,UIParent); " +
            "AnimationOrderGroup=owner:CreateAnimationGroup(); " +
            "AnimationOrderFirst=AnimationOrderGroup:CreateAnimation('Alpha'); " +
            "AnimationOrderFirst:SetDuration(10); " +
            "AnimationOrderSecond=AnimationOrderGroup:CreateAnimation('Translation'); " +
            "AnimationOrderSecond:SetOrder(2); AnimationOrderSecond:SetDuration(10); " +
            "AnimationOrderFirst:SetScript('OnStop',function() add('first-stop') end); " +
            "AnimationOrderSecond:SetScript('OnPlay',function() add('second-play') end); " +
            "AnimationOrderSecond:SetScript('OnUpdate',function(_,elapsed) " +
            "add(string.format('second-update:%.1f',elapsed)) end); " +
            "AnimationOrderGroup:Play(false,5); AnimationOrderEvents={}; " +
            "AnimationOrderFirst:Stop()");

        session.Tick(.2);

        Assert.Equal(
            "true:true:0.2:first-stop,second-play,second-update:0.2",
            session.Lua.Evaluate(
                "return string.format('%s:%s:%.1f:%s'," +
                "tostring(AnimationOrderFirst:IsStopped())," +
                "tostring(AnimationOrderSecond:IsPlaying())," +
                "AnimationOrderSecond:GetElapsed()," +
                "table.concat(AnimationOrderEvents,','))"));
    }

    [Fact]
    public void PlayScriptsRunBeforeTheInitialAnimationValueIsApplied()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "group:0.200000,child:0.200000,after:1.000000",
            session.Lua.Evaluate(
                "local events={}; " +
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local texture=owner:CreateTexture(); " +
                "texture:SetVertexColor(.2,.3,.4,.5); " +
                "local group=texture:CreateAnimationGroup(); " +
                "local color=group:CreateAnimation('VertexColor'); " +
                "color:SetDuration(10); " +
                "color:SetStartColor(CreateColor(1,0,0,1)); " +
                "color:SetEndColor(CreateColor(0,0,1,1)); " +
                "local function red() local r=texture:GetVertexColor(); return r end; " +
                "group:SetScript('OnPlay',function() " +
                "events[#events+1]=string.format('group:%.6f',red()) end); " +
                "color:SetScript('OnPlay',function() " +
                "events[#events+1]=string.format('child:%.6f',red()) end); " +
                "group:Play(false,0); " +
                "events[#events+1]=string.format('after:%.6f',red()); " +
                "return table.concat(events,',')"));
    }

    [Fact]
    public void AnimationGroupExposesTheCompleteCurrentBinaryMethodSurface()
    {
        using var session = new EmulatorSession();

        var coreAssembly = typeof(EmulatorSession).Assembly;
        var apiType = coreAssembly.GetType(
            "WoWAddonLab.Emulator.Lua.WowWidgetApi",
            throwOnError: true)!;
        var ownedMethods = Assert.IsType<string[]>(
            apiType.GetField(
                "AnimationGroup",
                BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null));
        Assert.Equal(
            [
                "CreateAnimation", "Finish", "GetAnimationSpeedMultiplier",
                "GetAnimations", "GetDuration", "GetElapsed", "GetLoopState",
                "GetLooping", "GetProgress", "GetScript", "HasScript", "HookScript",
                "IsDone", "IsPaused", "IsPendingFinish", "IsPlaying", "IsReverse",
                "IsSetToFinalAlpha", "Pause", "Play", "RemoveAnimations", "Restart",
                "SetAnimationSpeedMultiplier", "SetLooping", "SetPlaying",
                "SetScript", "SetToFinalAlpha", "Stop"
            ],
            ownedMethods);

        Assert.Equal(
            string.Join(':', Enumerable.Repeat("function", 15)),
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "return table.concat({" +
                "type(group.Finish),type(group.GetAnimationSpeedMultiplier)," +
                "type(group.GetElapsed),type(group.GetLoopState),type(group.GetProgress)," +
                "type(group.IsPendingFinish),type(group.IsReverse)," +
                "type(group.IsSetToFinalAlpha),type(group.RemoveAnimations)," +
                "type(group.SetAnimationSpeedMultiplier),type(group.GetName)," +
                "type(group.GetObjectType),type(group.GetParent)," +
                "type(group.GetParentKey),type(group.SetToDefaults)},':')"));
    }

    [Fact]
    public void AnimationOwnsThirtySixMethodsAndInheritsScriptRegionSurface()
    {
        var coreAssembly = typeof(EmulatorSession).Assembly;
        var apiType = coreAssembly.GetType(
            "WoWAddonLab.Emulator.Lua.WowWidgetApi",
            throwOnError: true)!;
        var ownedMethods = Assert.IsType<string[]>(
            apiType.GetField(
                "Animation",
                BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null));
        Assert.Equal(
            [
                "GetDuration", "GetElapsed", "GetEndDelay", "GetOrder",
                "GetProgress", "GetRegionParent", "GetScript", "GetSmoothProgress",
                "GetSmoothing", "GetStartDelay", "GetTarget", "HasScript",
                "HookScript", "IsDelaying", "IsDone", "IsPaused", "IsPlaying",
                "IsStopped", "Pause", "Play", "Restart", "SetChildKey",
                "SetDuration", "SetEndDelay", "SetOrder", "SetParent",
                "SetPlaying", "SetScript", "SetSmoothProgress", "SetSmoothing",
                "SetStartDelay", "SetTarget", "SetTargetKey", "SetTargetName",
                "SetTargetParent", "Stop"
            ],
            ownedMethods);

        using var session = new EmulatorSession();
        Assert.Equal(
            "function:function:function:function:true",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local animation=group:CreateAnimation('Alpha'); " +
                "return table.concat({type(animation.GetName)," +
                "type(animation.GetObjectType),type(animation.GetParent)," +
                "type(animation.SetToDefaults)," +
                "tostring(animation:GetParent()==group)},':')"));
    }

    [Fact]
    public void DerivedAnimationsAndControlPointsRetainInheritedScriptRegionMethods()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "function:function:function:function:function:function:true",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local alpha=group:CreateAnimation('Alpha'); " +
                "local path=group:CreateAnimation('Path'); " +
                "local point=path:CreateControlPoint(nil,nil,1); " +
                "return table.concat({type(alpha.GetName),type(alpha.GetParent)," +
                "type(path.GetObjectType),type(path.SetToDefaults)," +
                "type(point.GetName),type(point.GetParent)," +
                "tostring(point:GetParent()==path)},':')"));
    }

    [Fact]
    public void SpeedMultiplierScalesChildAndGroupClocksButNotUpdateArgument()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "2.000:2.000:0.200:2.0:1.0",
            session.Lua.Evaluate(
                "local childUpdate,groupUpdate; " +
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local child=group:CreateAnimation('Alpha'); child:SetDuration(10); " +
                "group:SetAnimationSpeedMultiplier(2); " +
                "child:SetScript('OnUpdate',function(_,elapsed) childUpdate=elapsed end); " +
                "group:SetScript('OnUpdate',function(_,elapsed) groupUpdate=elapsed end); " +
                "group:Play(false,1); " +
                "return string.format('%.3f:%.3f:%.3f:%.1f:%.1f'," +
                "group:GetAnimationSpeedMultiplier(),group:GetElapsed()," +
                "group:GetProgress(),childUpdate,groupUpdate)"));
    }

    [Fact]
    public void SpeedMultiplierIsAppliedPerOrderBeforeConsumingInputRemainder()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:1.000:0.500:1.000:0.000",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local first=group:CreateAnimation('Alpha'); first:SetOrder(1); first:SetDuration(1); " +
                "local second=group:CreateAnimation('Alpha'); second:SetOrder(2); second:SetDuration(1); " +
                "group:SetAnimationSpeedMultiplier(2); group:Play(false,1); " +
                "return string.format('%s:%.3f:%.3f:%.3f:%.3f'," +
                "tostring(group:IsPlaying()),group:GetElapsed(),group:GetProgress()," +
                "first:GetElapsed(),second:GetElapsed())"));

        Assert.Equal(
            "true:1.500:0.750:1.000:0.500",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local first=group:CreateAnimation('Alpha'); first:SetOrder(1); first:SetDuration(1); " +
                "local second=group:CreateAnimation('Alpha'); second:SetOrder(2); second:SetDuration(1); " +
                "group:SetAnimationSpeedMultiplier(.5); group:Play(false,2); " +
                "return string.format('%s:%.3f:%.3f:%.3f:%.3f'," +
                "tostring(group:IsPlaying()),group:GetElapsed(),group:GetProgress()," +
                "first:GetElapsed(),second:GetElapsed())"));
    }

    [Fact]
    public void FinishStopsAtTheTraversalBoundaryAndReportsRequested()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "AnimationFinishRequested='unset'; " +
            "local owner=CreateFrame('Frame',nil,UIParent); " +
            "AnimationFinishGroup=owner:CreateAnimationGroup(); " +
            "AnimationFinishGroup:SetLooping('REPEAT'); " +
            "local child=AnimationFinishGroup:CreateAnimation('Alpha'); " +
            "child:SetDuration(1); " +
            "AnimationFinishGroup:SetScript('OnFinished',function(_,requested) " +
            "AnimationFinishRequested=tostring(requested) end); " +
            "AnimationFinishGroup:Play(false,.25); AnimationFinishGroup:Finish()");

        Assert.Equal(
            "true:true:FORWARD:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(AnimationFinishGroup:IsPlaying())," +
                "tostring(AnimationFinishGroup:IsPendingFinish())," +
                "AnimationFinishGroup:GetLoopState()," +
                "tostring(AnimationFinishGroup:IsReverse())},':')"));

        session.Tick(.25);
        Assert.Equal(
            "true:true:0.500",
            session.Lua.Evaluate(
                "return string.format('%s:%s:%.3f'," +
                "tostring(AnimationFinishGroup:IsPlaying())," +
                "tostring(AnimationFinishGroup:IsPendingFinish())," +
                "AnimationFinishGroup:GetElapsed())"));

        session.Tick(.25);
        session.Tick(.25);
        Assert.Equal(
            "false:false:NONE:false:true:0.000",
            session.Lua.Evaluate(
                "return string.format('%s:%s:%s:%s:%s:%.3f'," +
                "tostring(AnimationFinishGroup:IsPlaying())," +
                "tostring(AnimationFinishGroup:IsPendingFinish())," +
                "AnimationFinishGroup:GetLoopState()," +
                "tostring(AnimationFinishGroup:IsReverse())," +
                "AnimationFinishRequested,AnimationFinishGroup:GetElapsed())"));
    }

    [Fact]
    public void RemoveAnimationsStopsWithoutARequestAndDetachesEveryChild()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:0:false:false:nil",
            session.Lua.Evaluate(
                "local childStop,groupStop; " +
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local child=group:CreateAnimation('Alpha'); child:SetDuration(10); " +
                "child:SetScript('OnStop',function(_,requested) " +
                "childStop=tostring(requested) end); " +
                "group:SetScript('OnStop',function(_,requested) " +
                "groupStop=tostring(requested) end); " +
                "group:Play(); group:RemoveAnimations(); " +
                "return select('#',group:GetAnimations())..':'..group:GetDuration()..':'.." +
                "childStop..':'..groupStop..':'..tostring(child:GetParent())"));
    }

    [Fact]
    public void AnimationSetParentMovesBetweenGroupsAndUpdatesItsOrder()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:0:1:0:3:true:true:99:0:0",
            session.Lua.Evaluate(
                "local firstOwner=CreateFrame('Frame',nil,UIParent); " +
                "local secondOwner=CreateFrame('Frame',nil,UIParent); " +
                "local first=firstOwner:CreateAnimationGroup('FirstAnimationGroup'); " +
                "local second=secondOwner:CreateAnimationGroup('SecondAnimationGroup'); " +
                "local child=first:CreateAnimation('Alpha'); child:SetDuration(3); " +
                "child:SetParent(second,7); " +
                "local moved=child:GetParent()==second and child:GetTarget()==secondOwner; " +
                "local firstCount=select('#',first:GetAnimations()); " +
                "local secondCount=select('#',second:GetAnimations()); " +
                "local firstDuration=first:GetDuration(); " +
                "local secondDuration=second:GetDuration(); " +
                "child:SetParent('FirstAnimationGroup'); " +
                "local named=child:GetParent()==first and child:GetOrder()==7; " +
                "child:SetParent(first,200); local high=child:GetOrder(); " +
                "child:SetParent(first,-4); local low=child:GetOrder(); " +
                "child:SetParent(nil,5); " +
                "return table.concat({tostring(moved),firstCount,secondCount," +
                "firstDuration,secondDuration,tostring(named)," +
                "tostring(type(child.SetParent)=='function'),high,low,child:GetOrder()},':')"));
    }

    [Fact]
    public void ScaleOriginLuaMethodsDriveTheRenderedTransformPivot()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "BOTTOMLEFT:10.0:5.0:false:false",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame','LuaScaleOriginOwner',UIParent); " +
                "owner:SetSize(100,50); owner:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',100,100); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local scale=group:CreateAnimation('Scale'); scale:SetDuration(1); " +
                "scale:SetScaleFrom(1,1); scale:SetScaleTo(2,2); " +
                "scale:SetOrigin('BOTTOMLEFT',10,5); " +
                "local point,x,y=scale:GetOrigin(); " +
                "local missing=pcall(function() scale:SetOrigin() end); " +
                "local invalid=pcall(function() scale:SetOrigin('SIDE',1,2) end); " +
                "group:Play(false,.5); " +
                "return string.format('%s:%.1f:%.1f:%s:%s'," +
                "point,x,y,tostring(missing),tostring(invalid))"));

        var bounds = session.Ui.ResolveBounds(session.Ui.Find("LuaScaleOriginOwner")!.Id);
        Assert.Equal(95, bounds.Left, 3);
        Assert.Equal(97.5, bounds.Bottom, 3);
        Assert.Equal(150, bounds.Width, 3);
        Assert.Equal(75, bounds.Height, 3);
    }

    [Fact]
    public void RotationExposesNativeRadianStorageAndOriginMethods()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1.570796:90.000:CENTER:3.0:4.0:false",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local rotation=group:CreateAnimation('Rotation'); " +
                "rotation:SetRadians(1.57079632679); " +
                "rotation:SetOrigin('CENTER',3,4); " +
                "local point,x,y=rotation:GetOrigin(); " +
                "local invalid=pcall(function() rotation:SetRadians('bad') end); " +
                "return string.format('%.6f:%.3f:%s:%.1f:%.1f:%s'," +
                "rotation:GetRadians(),rotation:GetDegrees(),point,x,y,tostring(invalid))"));

        Assert.Equal(
            "3.141593:180.000",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local rotation=group:CreateAnimation('Rotation'); " +
                "rotation:SetDegrees(180); " +
                "return string.format('%.6f:%.3f'," +
                "rotation:GetRadians(),rotation:GetDegrees())"));
    }

    [Fact]
    public void AnimationSetToDefaultsOnlyClearsScripts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:REPEAT:2.500:true:3:7.000:1.000:2.000:IN_OUT",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "group:SetLooping('REPEAT'); " +
                "group:SetAnimationSpeedMultiplier(2.5); " +
                "group:SetToFinalAlpha(true); " +
                "group:SetScript('OnPlay',function() end); " +
                "local animation=group:CreateAnimation('Alpha'); " +
                "animation:SetOrder(3); animation:SetDuration(7); " +
                "animation:SetStartDelay(1); animation:SetEndDelay(2); " +
                "animation:SetSmoothing('IN_OUT'); " +
                "animation:SetScript('OnPlay',function() end); " +
                "group:SetToDefaults(); animation:SetToDefaults(); " +
                "return string.format('%s:%s:%s:%.3f:%s:%d:%.3f:%.3f:%.3f:%s'," +
                "tostring(group:GetScript('OnPlay')~=nil)," +
                "tostring(animation:GetScript('OnPlay')~=nil),group:GetLooping()," +
                "group:GetAnimationSpeedMultiplier()," +
                "tostring(group:IsSetToFinalAlpha()),animation:GetOrder()," +
                "animation:GetDuration(),animation:GetStartDelay()," +
                "animation:GetEndDelay(),animation:GetSmoothing())"));
    }

    [Fact]
    public void DerivedAnimationObjectTypeChainsMatchTheirVtablePredicates()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:false:true:true:true:true:true:false:false",
            session.Lua.Evaluate(
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local group=owner:CreateAnimationGroup(); " +
                "local alpha=group:CreateAnimation('Alpha'); " +
                "local lineScale=group:CreateAnimation('LineScale'); " +
                "local lineTranslation=group:CreateAnimation('LineTranslation'); " +
                "local textureCoord=group:CreateAnimation('TextureCoord'); " +
                "local path=group:CreateAnimation('Path'); " +
                "local point=path:CreateControlPoint(); " +
                "return table.concat({" +
                "tostring(alpha:IsObjectType('Animation'))," +
                "tostring(alpha:IsObjectType('Scale'))," +
                "tostring(lineScale:IsObjectType('Scale'))," +
                "tostring(lineScale:IsObjectType('Animation'))," +
                "tostring(lineTranslation:IsObjectType('Translation'))," +
                "tostring(lineTranslation:IsObjectType('Animation'))," +
                "tostring(textureCoord:IsObjectType('Animation'))," +
                "tostring(textureCoord:IsObjectType('Translation'))," +
                "tostring(point:IsObjectType('Animation'))},':')"));
    }
}
