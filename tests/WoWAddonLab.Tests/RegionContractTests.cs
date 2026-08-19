namespace WoWAddonLab.Tests;

public sealed class RegionContractTests
{
    private const string RecoveredMethods =
        "'CanChangeProtectedState','ClearPoint','ClearScripts','CollapsesLayout'," +
        "'GetSourceLocation','Intersects','IsAnchoringRestricted','IsAnchoringSecret'," +
        "'IsCollapsed','IsDragging','IsProtected','IsRectValid','SetCollapsesLayout'," +
        "'IsIgnoringParentAlpha','IsIgnoringParentScale','SetAlphaFromBoolean'," +
        "'SetIgnoreParentAlpha','SetVertexColorFromBoolean'";

    [Fact]
    public void RegionDerivedObjectsExposeTheRecoveredNativeSurface()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            string.Join(',', Enumerable.Repeat("function", 36)),
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','RegionSurfaceFrame',UIParent); " +
                "local texture=frame:CreateTexture('RegionSurfaceTexture'); " +
                $"local methods={{{RecoveredMethods}}}; local result={{}}; " +
                "for _,object in ipairs({frame,texture}) do " +
                " for _,name in ipairs(methods) do table.insert(result,type(object[name])) end " +
                "end; return table.concat(result,',')"));
    }

    [Fact]
    public void ClearPointRemovesOnlyTheNamedMaterializedAnchor()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1:BOTTOMRIGHT:0:false:false",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','ClearPointFrame',UIParent); " +
                "frame:SetAllPoints(UIParent); frame:ClearPoint('topleft'); " +
                "local point=frame:GetPoint(1); " +
                "local namedCount=select('#',frame:GetPointByName('TOPLEFT')); " +
                "local missing=pcall(function() frame:ClearPoint() end); " +
                "local invalid=pcall(function() frame:ClearPoint('MIDDLE') end); " +
                "return table.concat({frame:GetNumPoints(),point,namedCount," +
                "tostring(missing),tostring(invalid)},':')"));
    }

    [Fact]
    public void PointOffsetMethodsRewriteEachExistingAnchorSlot()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "2:2:3:2:3:3:1:3:1:0:0:0:0",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame','PointOffsetFrame',UIParent); " +
                "frame:SetPoint('TOPLEFT',UIParent,'TOPLEFT',20,-30); " +
                "frame:SetPoint('BOTTOMRIGHT',UIParent,'BOTTOMRIGHT',-5,6); " +
                "frame:SetPointsOffset(2,3); " +
                "local _,_,_,setX1,setY1=frame:GetPointByName('TOPLEFT'); " +
                "local _,_,_,setX2,setY2=frame:GetPointByName('BOTTOMRIGHT'); " +
                "frame:AdjustPointsOffset(1,-2); " +
                "local _,_,_,adjustX1,adjustY1=frame:GetPointByName('TOPLEFT'); " +
                "local _,_,_,adjustX2,adjustY2=frame:GetPointByName('BOTTOMRIGHT'); " +
                "frame:ClearPointsOffset(); " +
                "local _,_,_,clearX1,clearY1=frame:GetPointByName('TOPLEFT'); " +
                "local empty=CreateFrame('Frame','EmptyPointOffsetFrame',UIParent); " +
                "empty:SetPointsOffset(9,9); " +
                "return table.concat({frame:GetNumPoints(),setX1,setY1,setX2,setY2," +
                "adjustX1,adjustY1,adjustX2,adjustY2,clearX1,clearY1," +
                "empty:GetNumPoints(),select('#',empty:GetPoint())},':')"));
    }

    [Fact]
    public void GetPointCanResolveThroughCollapsedRelativeRegions()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "CollapsedRelative:1:2:UIParent:10:20:UIParent:10:20",
            session.Lua.Evaluate(
                "local relative=CreateFrame('Frame','CollapsedRelative',UIParent); " +
                "relative:SetPoint('TOPLEFT',UIParent,'TOPLEFT',10,20); " +
                "relative:SetCollapsesLayout(true); relative:Hide(); " +
                "local child=CreateFrame('Frame','CollapsedChild',UIParent); " +
                "child:SetPoint('TOPLEFT',relative,'TOPLEFT',1,2); " +
                "local _,directTo,_,directX,directY=child:GetPoint(1,false); " +
                "local _,resolvedTo,_,resolvedX,resolvedY=child:GetPoint(1,true); " +
                "local _,namedTo,_,namedX,namedY=child:GetPointByName('TOPLEFT',true); " +
                "return table.concat({directTo:GetName(),directX,directY," +
                "resolvedTo:GetName(),resolvedX,resolvedY,namedTo:GetName(),namedX,namedY},':')"));
    }

    [Fact]
    public void RectValidityAndIntersectionUseStrictResolvedAabbOverlap()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:false:false:false:false",
            session.Lua.Evaluate(
                "local a=CreateFrame('Frame','IntersectA',UIParent); " +
                "a:SetSize(100,100); a:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0); " +
                "local b=CreateFrame('Frame','IntersectB',UIParent); " +
                "b:SetSize(50,50); b:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',75,75); " +
                "local c=CreateFrame('Frame','IntersectC',UIParent); " +
                "c:SetSize(50,50); c:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',100,0); " +
                "local invalid=CreateFrame('Frame','IntersectInvalid',UIParent); " +
                "return table.concat({tostring(a:IsRectValid()),tostring(a:Intersects(b))," +
                "tostring(a:Intersects(c)),tostring(invalid:IsRectValid())," +
                "tostring(a:Intersects(invalid))," +
                "tostring(pcall(function() a:Intersects('IntersectB') end))},':')"));
    }

    [Fact]
    public void CollapseProtectionAndDragQueriesReflectLiveRegionState()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "RegionStateFrame=CreateFrame('Frame','RegionStateFrame',UIParent); " +
            "RegionStateFrame:SetCollapsesLayout(true); RegionStateFrame:Hide()");
        var frame = session.Ui.Find("RegionStateFrame")!;
        frame.Protected = true;
        frame.ProtectedExplicitly = false;
        frame.AnchoringRestricted = true;
        frame.AnchoringSecret = true;
        session.Ui.MovingObjectId = frame.Id;

        Assert.Equal(
            "true:true:true:true:true:false:true:true",
            session.Lua.Evaluate(
                "local f=RegionStateFrame; " +
                "local effective,explicit=f:IsProtected(); " +
                "local before=f:CanChangeProtectedState(); " +
                "local result={tostring(f:CollapsesLayout()),tostring(f:IsCollapsed())," +
                "tostring(f:IsDragging()),tostring(f:IsAnchoringRestricted())," +
                "tostring(f:IsAnchoringSecret()),tostring(explicit),tostring(effective)," +
                "tostring(before)}; return table.concat(result,':')"));

        session.Lua.Client.InCombatLockdown = true;
        Assert.Equal("false", session.Lua.Evaluate("return tostring(RegionStateFrame:CanChangeProtectedState())"));
        session.Lua.Evaluate("RegionStateFrame:Show()");
        Assert.Equal("false", session.Lua.Evaluate("return tostring(RegionStateFrame:IsCollapsed())"));

        session.Lua.Evaluate(
            "RegionCollapseParent=CreateFrame('Frame','RegionCollapseParent',UIParent); " +
            "RegionCollapseChild=CreateFrame('Frame','RegionCollapseChild',RegionCollapseParent); " +
            "RegionCollapseChild:SetCollapsesLayout(true); RegionCollapseParent:Hide()");
        Assert.Equal(
            "true:true",
            session.Lua.Evaluate(
                "return tostring(RegionCollapseChild:IsShown())..':'.." +
                "tostring(RegionCollapseChild:IsCollapsed())"));
    }

    [Fact]
    public void SourceLocationAndClearScriptsFollowTheNativeContracts()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "RegionScriptFrame=CreateFrame('Frame','RegionScriptFrame',UIParent); " +
            "RegionScriptFrame:SetScript('OnShow',function() end)");
        var frame = session.Ui.Find("RegionScriptFrame")!;
        frame.SourceLocation = "Interface/AddOns/Test/Frame.xml";

        Assert.Single(frame.ScriptReferences);
        Assert.Equal(
            "Interface/AddOns/Test/Frame.xml:nil:0",
            session.Lua.Evaluate(
                "RegionScriptFrame:ClearScripts(); " +
                "return RegionScriptFrame:GetSourceLocation()..':'.." +
                "tostring(RegionScriptFrame:GetScript('OnShow'))..':'.." +
                "select('#',RegionScriptFrame:ClearScripts())"));
        Assert.Empty(frame.ScriptReferences);
    }

    [Fact]
    public void ParentAlphaAndBooleanAlphaSettersUseNativeQuantizationAndDefaults()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "AlphaParent=CreateFrame('Frame','AlphaParent',UIParent); AlphaParent:SetAlpha(.5); " +
            "AlphaChild=AlphaParent:CreateTexture('AlphaChild'); AlphaChild:SetAlpha(.5)");
        var parent = session.Ui.Find("AlphaParent")!;
        var child = session.Ui.Find("AlphaChild")!;
        var quantizedHalf = 128f / 255f;

        Assert.Equal(64f / 255f, session.Ui.EffectiveAlpha(child), 5);
        Assert.Equal(
            "true:false:false",
            session.Lua.Evaluate(
                "AlphaChild:SetIgnoreParentAlpha(true); " +
                "local missing=pcall(function() AlphaChild:SetIgnoreParentAlpha() end); " +
                "AlphaChild:SetIgnoreParentScale(nil); " +
                "return table.concat({tostring(AlphaChild:IsIgnoringParentAlpha())," +
                "tostring(AlphaChild:IsIgnoringParentScale()),tostring(missing)},':')"));
        Assert.Equal(quantizedHalf, session.Ui.EffectiveAlpha(child), 5);
        Assert.Equal(
            "true:false",
            session.Lua.Evaluate(
                "local accepted=pcall(function() AlphaChild:SetIgnoreParentAlpha(nil) end); " +
                "return tostring(accepted)..':'..tostring(AlphaChild:IsIgnoringParentAlpha())"));

        session.Lua.Evaluate("AlphaChild:SetAlphaFromBoolean(true)");
        Assert.Equal(1f, child.Alpha);
        session.Lua.Evaluate("AlphaChild:SetAlphaFromBoolean(false)");
        Assert.Equal(0f, child.Alpha);
        session.Lua.Evaluate("AlphaChild:SetAlphaFromBoolean(true,.25,.75)");
        Assert.Equal(64f / 255f, child.Alpha, 5);
        session.Lua.Evaluate("AlphaChild:SetAlphaFromBoolean(false,.25,.75)");
        Assert.Equal(191f / 255f, child.Alpha, 5);
        Assert.Equal(
            "false:false",
            session.Lua.Evaluate(
                "return tostring(pcall(function() AlphaChild:SetAlpha() end))..':'.." +
                "tostring(pcall(function() AlphaChild:SetScale() end))"));
    }

    [Fact]
    public void XmlAlphaTruncatesWhileLuaAlphaRoundsIntoByteBackedStorage()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wow-addon-lab-{Guid.NewGuid():N}");
        var addon = Path.Combine(root, "XmlAlpha");
        Directory.CreateDirectory(addon);
        File.WriteAllText(
            Path.Combine(addon, "XmlAlpha.toc"),
            "## Interface: 120000\nlayout.xml");
        File.WriteAllText(
            Path.Combine(addon, "layout.xml"),
            "<Ui><Frame name=\"XmlAlphaFrame\" parent=\"UIParent\" alpha=\".5\"/></Ui>");
        try
        {
            using var session = new EmulatorSession();
            session.Load(addon);

            Assert.Equal(
                "127:128",
                session.Lua.Evaluate(
                    "local xml=math.floor(XmlAlphaFrame:GetAlpha()*255+.5); " +
                    "XmlAlphaFrame:SetAlpha(.5); " +
                    "local lua=math.floor(XmlAlphaFrame:GetAlpha()*255+.5); " +
                    "return xml..':'..lua"));

            var frame = session.Ui.Find("XmlAlphaFrame")!;
            frame.Alpha = .1f;
            Assert.Equal(26f / 255f, frame.Alpha, 5);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ReparentingPropagatesEffectiveVisibilityAndFocusBeforeCallbacks()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:false:leaf,child:true:true:leaf,child:1:nil",
            session.Lua.Evaluate(
                "local visible=CreateFrame('Frame',nil,UIParent); " +
                "local hidden=CreateFrame('Frame',nil,UIParent); hidden:Hide(); " +
                "local child=CreateFrame('EditBox',nil,visible); child:SetAutoFocus(false); " +
                "local leaf=CreateFrame('Frame',nil,child); local trace={}; local lost=0; " +
                "leaf:SetScript('OnHide',function() trace[#trace+1]='leaf' end); " +
                "child:SetScript('OnHide',function() trace[#trace+1]='child' end); " +
                "child:SetScript('OnEditFocusLost',function() lost=lost+1 end); " +
                "child:SetFocus(); child:SetParent(hidden); " +
                "local hiddenState=tostring(child:IsShown())..':'..tostring(child:IsVisible()).." +
                "':'..table.concat(trace,','); trace={}; " +
                "leaf:SetScript('OnShow',function() trace[#trace+1]='leaf' end); " +
                "child:SetScript('OnShow',function() trace[#trace+1]='child' end); " +
                "child:SetParent(visible); " +
                "return hiddenState..':'..tostring(child:IsShown())..':'.." +
                "tostring(child:IsVisible())..':'..table.concat(trace,',')..':'.." +
                "lost..':'..tostring(GetCurrentKeyBoardFocus())"));
    }

    [Fact]
    public void BooleanVertexColorSelectsTheRequiredColorMixin()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "ColorFrame=CreateFrame('Frame','ColorFrame',UIParent); " +
            "ColorTexture=ColorFrame:CreateTexture('ColorTexture'); " +
            "ColorTexture:SetVertexColorFromBoolean(true," +
            "{r=.1,g=.2,b=.3,a=.4},{r=.6,g=.7,b=.8,a=.9})");
        var texture = session.Ui.Find("ColorTexture")!.Texture!;

        Assert.Equal(26f / 255f, texture.VertexColor.X, 5);
        Assert.Equal(51f / 255f, texture.VertexColor.Y, 5);
        Assert.Equal(77f / 255f, texture.VertexColor.Z, 5);
        Assert.Equal(102f / 255f, texture.VertexColor.W, 5);

        session.Lua.Evaluate(
            "ColorTexture:SetVertexColorFromBoolean(false," +
            "{r=.1,g=.2,b=.3,a=.4},{r=.6,g=.7,b=.8,a=.9})");
        Assert.Equal(153f / 255f, texture.VertexColor.X, 5);
        Assert.Equal(179f / 255f, texture.VertexColor.Y, 5);
        Assert.Equal(204f / 255f, texture.VertexColor.Z, 5);
        Assert.Equal(230f / 255f, texture.VertexColor.W, 5);
    }
}
