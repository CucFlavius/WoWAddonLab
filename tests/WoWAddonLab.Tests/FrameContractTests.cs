namespace WoWAddonLab.Tests;

public sealed class FrameContractTests
{
    private static readonly string[] NativeFrameMethods =
    [
        "AbortDrag", "CanChangeAttribute", "ClearAlphaGradient", "ClearAttribute",
        "ClearAttributes", "CreateFontString", "CreateLine", "CreateMaskTexture",
        "CreateTexture", "DesaturateHierarchy", "DisableDrawLayer", "DoesClipChildren",
        "DoesHyperlinkPropagateToParent", "EnableDrawLayer", "EnableGamePadButton",
        "EnableGamePadStick", "EnableKeyboard", "ExecuteAttribute", "GetAlpha",
        "GetAttribute", "GetBoundsRect", "GetChildren", "GetClampRectInsets",
        "GetDontSavePosition", "GetEffectiveAlpha", "GetEffectiveScale",
        "GetEffectivelyFlattensRenderLayers", "GetFlattensRenderLayers",
        "GetFrameLevel", "GetFrameStrata", "GetHighestFrameLevel",
        "GetHitRectInsets", "GetHyperlinksEnabled", "GetID", "GetNumChildren",
        "GetNumRegions", "GetPropagateKeyboardInput", "GetRaisedFrameLevel",
        "GetRegions", "GetResizeBounds", "GetScale", "GetWindow",
        "HasAlphaGradient", "HasFixedFrameLevel", "HasFixedFrameStrata", "Hide",
        "InterceptStartDrag", "IsClampedToScreen", "IsDrawLayerEnabled",
        "IsEventRegistered", "IsFrameBuffer", "IsGamePadButtonEnabled",
        "IsGamePadStickEnabled", "IsHighlightLocked", "IsIgnoringChildrenForBounds",
        "IsIgnoringParentAlpha", "IsIgnoringParentScale", "IsKeyboardEnabled",
        "IsMovable", "IsObjectLoaded", "IsResizable", "IsShown", "IsToplevel",
        "IsUserPlaced", "IsUsingParentLevel", "IsVisible", "LockHighlight", "Lower",
        "Raise", "RegisterAllEvents", "RegisterEvent", "RegisterEventCallback",
        "RegisterForDrag", "RegisterUnitEvent", "RegisterUnitEventCallback",
        "RotateTextures", "SetAlpha", "SetAlphaFromBoolean", "SetAlphaGradient",
        "SetAttribute", "SetAttributeNoHandler", "SetClampRectInsets",
        "SetClampedToScreen", "SetClipsChildren", "SetDontSavePosition",
        "SetDrawLayerEnabled", "SetFixedFrameLevel", "SetFixedFrameStrata",
        "SetFlattensRenderLayers", "SetFrameLevel", "SetFrameStrata",
        "SetHighlightLocked", "SetHitRectInsets", "SetHyperlinkPropagateToParent",
        "SetHyperlinksEnabled", "SetID", "SetIgnoreParentAlpha",
        "SetIgnoreParentScale", "SetIgnoringChildrenForBounds", "SetIsFrameBuffer",
        "SetMovable", "SetPropagateKeyboardInput", "SetResizable", "SetResizeBounds",
        "SetScale", "SetShown", "SetToplevel", "SetUserPlaced",
        "SetUsingParentLevel", "SetWindow", "Show", "StartMoving", "StartSizing",
        "StopMovingOrSizing", "UnlockHighlight", "UnregisterAllEvents",
        "UnregisterEvent"
    ];

    [Fact]
    public void FrameExposesEveryMethodInTheRecoveredNativeRegistrar()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(',', NativeFrameMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeFrameMethods.Length + ":" +
            string.Join(',', Enumerable.Repeat("function", NativeFrameMethods.Length)),
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','FrameBinarySurface',UIParent); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(frame[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void FrameAttributesClearAndExecuteWithNativeReturnShape()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "4:two:true:false:true:5:6:false:1:nil:nil:0",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','FrameAttributeAudit',UIParent); " +
                "local changed=0; local lastName; " +
                "frame:SetScript('OnAttributeChanged',function(_,name) changed=changed+1; lastName=name end); " +
                "frame:SetAttribute('MixedCase',12); " +
                "local first=frame:ClearAttribute('MixedCase'); " +
                "local second=frame:ClearAttribute('MixedCase'); " +
                "frame:SetAttributeNoHandler('silent',9); " +
                "frame:SetAttribute('call',function(x,y) return x+y,x*y end); " +
                "local ok,sum,product=frame:ExecuteAttribute('call',2,3); " +
                "local missingCount=select('#',frame:ExecuteAttribute('missing')); " +
                "local missing=frame:ExecuteAttribute('missing'); " +
                "frame:SetAttribute('one',1); frame:SetAttribute('two',2); " +
                "frame:ClearAttributes(); " +
                "return table.concat({changed,lastName,tostring(first),tostring(second)," +
                "tostring(ok),sum,product,tostring(missing),missingCount," +
                "tostring(frame:GetAttribute('one')),tostring(frame:GetAttribute('two'))," +
                "select('#',frame:ClearAttributes())},':')"));
    }

    [Fact]
    public void NativeIdentitySlotAllowsUnhookedFrameApiProxyTables()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "userdata:proxy-value",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','FrameIdentityProxy',UIParent); " +
                "frame:SetAttribute('proxy-key','proxy-value'); " +
                "local proxy={[0]=rawget(frame,0)}; " +
                "local frameApi=GetFrameMetatable().__index; " +
                "return type(rawget(frame,0))..':'.." +
                "frameApi.GetAttribute(proxy,'proxy-key')"));
    }

    [Fact]
    public void FrameFlagsAlphaAndGradientRoundTripThroughRecoveredState()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:false:false:false:false:true:false:false:true:true:true:true:" +
            "true:true:true:true:true:false:false:false:false:false:false:false",
            session.Lua.Evaluate(
                "local parent=CreateFrame('Frame','FrameFlagParent',UIParent); " +
                "local child=CreateFrame('Frame','FrameFlagChild',parent); " +
                "parent:SetAlpha(.5); child:SetAlpha(.5); " +
                "parent:SetAlphaGradient(0,{x=1,y=2}); " +
                "local had=parent:HasAlphaGradient(); parent:ClearAlphaGradient(); " +
                "parent:SetFlattensRenderLayers(true); " +
                "local inherited=child:GetEffectivelyFlattensRenderLayers(); " +
                "parent:SetFlattensRenderLayers(false); parent:SetIsFrameBuffer(true); " +
                "local buffered=child:GetEffectivelyFlattensRenderLayers(); " +
                "child:SetClipsChildren(true); child:SetDontSavePosition(true); " +
                "child:SetHyperlinkPropagateToParent(true); " +
                "child:SetIgnoringChildrenForBounds(true); child:SetToplevel(true); " +
                "child:SetFixedFrameLevel(true); child:SetFixedFrameStrata(true); " +
                "child:SetHighlightLocked(true); child:EnableGamePadButton(true); " +
                "child:EnableGamePadStick(true); child:EnableGamePadButton(); " +
                "child:EnableGamePadStick(); " +
                "local requiredDont=pcall(function() child:SetDontSavePosition() end); " +
                "local requiredFlatten=pcall(function() child:SetFlattensRenderLayers() end); " +
                "local requiredBuffer=pcall(function() child:SetIsFrameBuffer() end); " +
                "local requiredBounds=pcall(function() child:SetIgnoringChildrenForBounds() end); " +
                "local requiredHyperlink=pcall(function() child:SetHyperlinkPropagateToParent() end); " +
                "local result={had,parent:HasAlphaGradient(),inherited," +
                "child:GetFlattensRenderLayers(),buffered,parent:IsFrameBuffer()," +
                "child:IsFrameBuffer(),child:IsGamePadButtonEnabled()," +
                "child:DoesClipChildren(),child:GetDontSavePosition()," +
                "child:DoesHyperlinkPropagateToParent()," +
                "child:IsIgnoringChildrenForBounds(),child:IsToplevel()," +
                "child:HasFixedFrameLevel(),child:HasFixedFrameStrata()," +
                "child:IsHighlightLocked()," +
                "math.abs(child:GetEffectiveAlpha()-(128/255))<.00001," +
                "child:IsGamePadButtonEnabled(),child:IsGamePadStickEnabled()," +
                "requiredDont,requiredFlatten,requiredBuffer,requiredBounds,requiredHyperlink}; " +
                "for i,value in ipairs(result) do result[i]=tostring(value) end; " +
                "return table.concat(result,':')"));
    }

    [Fact]
    public void EffectiveAlphaUsesByteArithmeticAndFrameBufferBoundaries()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "64:32:128",
            session.Lua.Evaluate(
                "local parent=CreateFrame('Frame',nil,UIParent); parent:SetAlpha(.5); " +
                "local child=CreateFrame('Frame',nil,parent); child:SetAlpha(.5); " +
                "local grandchild=CreateFrame('Frame',nil,child); grandchild:SetAlpha(.5); " +
                "local childAlpha=math.floor(child:GetEffectiveAlpha()*255+.5); " +
                "local grandchildAlpha=math.floor(grandchild:GetEffectiveAlpha()*255+.5); " +
                "child:SetIsFrameBuffer(true); " +
                "local bufferedAlpha=math.floor(grandchild:GetEffectiveAlpha()*255+.5); " +
                "return table.concat({childAlpha,grandchildAlpha,bufferedAlpha},':')"));
    }

    [Fact]
    public void FrameIdIsIndependentFromAttributesAndStrataUsesNativeEnum()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "42:99:MEDIUM:BLIZZARD:true:false",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','FrameIdentity',UIParent); " +
                "frame:SetID(42); frame:SetAttribute('Id',99); " +
                "local id=frame:GetID(); local attribute=frame:GetAttribute('Id'); " +
                "frame:SetFrameStrata('WORLD'); local world=frame:GetFrameStrata(); " +
                "frame:SetFrameStrata('BLIZZARD'); local blizzard=frame:GetFrameStrata(); " +
                "local acceptsWorld=pcall(function() frame:SetFrameStrata('WORLD') end); " +
                "local rejectsInvalid=pcall(function() frame:SetFrameStrata('INVALID') end); " +
                "return table.concat({id,attribute,world,blizzard," +
                "tostring(acceptsWorld),tostring(rejectsInvalid)},':')"));
    }

    [Fact]
    public void FrameEventCallbacksUseTheNativeCallbackRegistries()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:0:0:false:nil:true:false:false",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','FrameEventRegistry',UIParent); " +
                "local callback=frame:RegisterEventCallback(" +
                "'TOOLTIP_SHOW_ITEM_COMPARISON',function() end); " +
                "local unitCallback=frame:RegisterUnitEventCallback(" +
                "'MINIMAP_PING',function() end,'player'); " +
                "local bad=select('#',frame:RegisterEventCallback(" +
                "'NOT_A_CALLBACK_EVENT',function() end)); " +
                "local badUnit=select('#',frame:RegisterUnitEventCallback(" +
                "'TOOLTIP_SHOW_ITEM_COMPARISON',function() end)); " +
                "local registered,unit=frame:IsEventRegistered('MINIMAP_PING'); " +
                "local removed=frame:UnregisterEvent('MINIMAP_PING'); " +
                "local after=frame:IsEventRegistered('MINIMAP_PING'); " +
                "frame:UnregisterAllEvents(); " +
                "local cleared=frame:IsEventRegistered(" +
                "'TOOLTIP_SHOW_ITEM_COMPARISON'); " +
                "return table.concat({tostring(callback),tostring(unitCallback)," +
                "bad,badUnit,tostring(registered),tostring(unit)," +
                "tostring(removed),tostring(after),tostring(cleared)},':')"));
    }

    [Fact]
    public void FrameSetToDefaultsRestoresRecoveredNativeFrameState()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:MEDIUM:8:false:false:false:false:false:false:false:" +
            "0:0:0:0:0:0:0:0:false:false:false:false:true:true:true:true:false",
            session.Lua.Evaluate(
                "local parent=CreateFrame('Frame','FrameDefaultsParent',UIParent); " +
                "parent:SetFrameLevel(7); " +
                "local frame=CreateFrame('Frame','FrameDefaultsChild',parent); " +
                "frame:SetID(55); frame:SetFrameStrata('HIGH'); frame:SetFrameLevel(42); " +
                "frame:SetUsingParentLevel(true); frame:SetFixedFrameLevel(true); " +
                "frame:SetFixedFrameStrata(true); frame:SetClampedToScreen(true); " +
                "frame:SetClampRectInsets(1,2,3,4); frame:SetHitRectInsets(5,6,7,8); " +
                "frame:SetClipsChildren(true); frame:EnableKeyboard(true); " +
                "frame:EnableGamePadButton(true); frame:EnableGamePadStick(true); " +
                "frame:EnableDrawLayer('HIGHLIGHT'); frame:SetToDefaults(); " +
                "local cl,cr,ct,cb=frame:GetClampRectInsets(); " +
                "local hl,hr,ht,hb=frame:GetHitRectInsets(); " +
                "return table.concat({frame:GetID(),frame:GetFrameStrata()," +
                "frame:GetFrameLevel(),tostring(frame:IsUsingParentLevel())," +
                "tostring(frame:HasFixedFrameLevel())," +
                "tostring(frame:HasFixedFrameStrata())," +
                "tostring(frame:IsClampedToScreen())," +
                "tostring(frame:DoesClipChildren())," +
                "tostring(frame:IsKeyboardEnabled())," +
                "tostring(frame:IsGamePadButtonEnabled()),cl,cr,ct,cb,hl,hr,ht,hb," +
                "tostring(frame:IsGamePadStickEnabled())," +
                "tostring(frame:IsDrawLayerEnabled('HIGHLIGHT'))," +
                "tostring(frame:IsFrameBuffer())," +
                "tostring(frame:GetFlattensRenderLayers())," +
                "tostring(frame:IsDrawLayerEnabled('BACKGROUND'))," +
                "tostring(frame:IsDrawLayerEnabled('BORDER'))," +
                "tostring(frame:IsDrawLayerEnabled('ARTWORK'))," +
                "tostring(frame:IsDrawLayerEnabled('OVERLAY'))," +
                "tostring(frame:HasAlphaGradient())},':')"));
    }

    [Fact]
    public void FrameDrawLayerControlsAffectQueriesAndRenderOrder()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "DrawLayerFrame=CreateFrame('Frame','DrawLayerFrame',UIParent); " +
            "DrawLayerFrame:SetSize(100,100); DrawLayerFrame:SetPoint('CENTER'); " +
            "DrawLayerTexture=DrawLayerFrame:CreateTexture('DrawLayerTexture','ARTWORK'); " +
            "DrawLayerTexture:SetAllPoints(DrawLayerFrame); " +
            "DrawLayerFrame:DisableDrawLayer('ARTWORK')");

        Assert.Equal(
            "false:true:false:true:false:false:false:true:false",
            session.Lua.Evaluate(
                "local f=DrawLayerFrame; " +
                "local disabled=f:IsDrawLayerEnabled('ARTWORK'); " +
                "f:EnableDrawLayer('ARTWORK'); local enabled=f:IsDrawLayerEnabled('ARTWORK'); " +
                "f:SetDrawLayerEnabled('ARTWORK'); local omitted=f:IsDrawLayerEnabled('ARTWORK'); " +
                "f:SetDrawLayerEnabled('ARTWORK',true); local explicit=f:IsDrawLayerEnabled('ARTWORK'); " +
                "local missing=pcall(function() f:IsDrawLayerEnabled() end); " +
                "local invalid=pcall(function() f:EnableDrawLayer('INVALID') end); " +
                "local defaultHighlight=f:IsDrawLayerEnabled('HIGHLIGHT'); " +
                "f:LockHighlight(); local lockedHighlight=f:IsDrawLayerEnabled('HIGHLIGHT'); " +
                "f:UnlockHighlight(); local unlockedHighlight=f:IsDrawLayerEnabled('HIGHLIGHT'); " +
                "local result={disabled,enabled,omitted,explicit,missing,invalid," +
                "defaultHighlight,lockedHighlight,unlockedHighlight}; " +
                "for i,value in ipairs(result) do result[i]=tostring(value) end; " +
                "return table.concat(result,':')"));

        session.Lua.Evaluate("DrawLayerFrame:DisableDrawLayer('ARTWORK')");
        Assert.DoesNotContain(
            session.Ui.RenderOrder(),
            value => value.Name == "DrawLayerTexture");
        session.Lua.Evaluate("DrawLayerFrame:EnableDrawLayer('ARTWORK')");
        Assert.Contains(
            session.Ui.RenderOrder(),
            value => value.Name == "DrawLayerTexture");
    }

    [Fact]
    public void FrameBoundsResizeBoundsAndHighestLevelFollowNativeShape()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "10:20:140:50:10:20:100:50:10:20:300:200:3:10:20",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','BoundsFrame',UIParent); " +
                "frame:SetSize(100,50); frame:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',10,20); " +
                "local child=CreateFrame('Frame','BoundsChild',frame); " +
                "child:SetSize(20,20); child:SetPoint('BOTTOMLEFT',frame,'BOTTOMLEFT',120,0); " +
                "local grand=CreateFrame('Frame','BoundsGrandchild',child); " +
                "grand:SetSize(5,5); grand:SetPoint('CENTER'); " +
                "frame:SetFrameLevel(3); child:SetFrameLevel(10); grand:SetFrameLevel(20); " +
                "frame:SetResizeBounds(10,20,300,200); " +
                "local left,bottom,width,height=frame:GetBoundsRect(); " +
                "frame:SetIgnoringChildrenForBounds(true); " +
                "local ownLeft,ownBottom,ownWidth,ownHeight=frame:GetBoundsRect(); " +
                "local minWidth,minHeight,maxWidth,maxHeight=frame:GetResizeBounds(); " +
                "return table.concat({left,bottom,width,height,ownLeft,ownBottom,ownWidth," +
                "ownHeight,minWidth,minHeight,maxWidth,maxHeight,frame:GetFrameLevel()," +
                "frame:GetHighestFrameLevel(false),frame:GetHighestFrameLevel(true)},':')"));
    }

    [Fact]
    public void FrameDragAbortAndInterceptionUseTheActiveNativeStyleCapture()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "DragSource=CreateFrame('Frame','DragSource',UIParent); " +
            "DragSource:SetSize(100,100); " +
            "DragSource:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
            "DragSource:EnableMouse(true); DragSource:RegisterForDrag('LeftButton'); " +
            "DragDelegate=CreateFrame('Frame','DragDelegate',UIParent); " +
            "DragDelegate:SetSize(10,10); DragDelegate:SetPoint('TOPRIGHT'); " +
            "DragIntercepted=false; DragDelegateStops=0; " +
            "DragSource:SetScript('OnDragStart',function(self) " +
            " DragIntercepted=self:InterceptStartDrag(DragDelegate) end); " +
            "DragDelegate:SetScript('OnDragStop',function() DragDelegateStops=DragDelegateStops+1 end)");

        session.MouseMove(20, 20);
        session.MouseButton("LeftButton", true);
        session.MouseMove(30, 30);
        session.MouseButton("LeftButton", false);

        Assert.Equal(
            "true:1",
            session.Lua.Evaluate("return tostring(DragIntercepted)..':'..DragDelegateStops"));

        session.Lua.Evaluate(
            "AbortSource=CreateFrame('Frame','AbortSource',UIParent); " +
            "AbortSource:SetSize(100,100); " +
            "AbortSource:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',200,0); " +
            "AbortSource:EnableMouse(true); AbortSource:RegisterForDrag('LeftButton'); " +
            "AbortStops=0; " +
            "AbortSource:SetScript('OnDragStart',function(self) self:AbortDrag() end); " +
            "AbortSource:SetScript('OnDragStop',function() AbortStops=AbortStops+1 end)");

        session.MouseMove(220, 20);
        session.MouseButton("LeftButton", true);
        session.MouseMove(230, 30);
        session.MouseButton("LeftButton", false);

        Assert.Equal("1", session.Lua.Evaluate("return AbortStops"));
    }

    [Fact]
    public void LowerResetsTheContainingToplevelRaiseState()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "LowerRoot=CreateFrame('Frame','LowerRoot',UIParent); LowerRoot:SetToplevel(true); " +
            "LowerChild=CreateFrame('Frame','LowerChild',LowerRoot); LowerRoot:Raise()");
        var root = session.Ui.Find("LowerRoot")!;
        var child = session.Ui.Find("LowerChild")!;

        Assert.True(root.RaisedFrameLevel > 0);
        Assert.Equal(root.RaisedFrameLevel, child.RaisedFrameLevel);
        session.Lua.Evaluate("LowerChild:Lower()");
        Assert.Equal(0, root.RaisedFrameLevel);
        Assert.Equal(0, child.RaisedFrameLevel);
    }
}
