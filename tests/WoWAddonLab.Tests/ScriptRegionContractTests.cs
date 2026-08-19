namespace WoWAddonLab.Tests;

public sealed class ScriptRegionContractTests
{
    [Fact]
    public void FontExposesScriptObjectWithoutScriptRegionParentMethods()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            string.Join(',', Enumerable.Repeat("function", 10)) + ":" +
            string.Join(',', Enumerable.Repeat("nil", 18)),
            session.Lua.Evaluate(
                "local value=CreateFont('ScriptRegionSurfaceFont'); " +
                "local present={" +
                "'GetName','GetObjectType','HasAnySecretAspect','HasSecretAspect'," +
                "'HasSecretValues','IsForbidden','IsObjectType'," +
                "'IsPreventingSecretValues','SetForbidden','SetToDefaults'}; " +
                "local absent={" +
                "'ClearParentKey','GetDebugName','GetParent','GetParentKey','SetParentKey'," +
                "'GetScript','HasScript','Hide','HookScript','IsObjectLoaded'," +
                "'IsMouseMotionFocus','IsShown','IsVisible','SetPassThroughButtons'," +
                "'SetScript','SetShown','ShouldButtonPassThrough','Show'}; " +
                "local result={}; " +
                "for _,name in ipairs(present) do table.insert(result,type(value[name])) end; " +
                "local removed={}; " +
                "for _,name in ipairs(absent) do table.insert(removed,type(value[name])) end; " +
                "return table.concat(result,',')..':'..table.concat(removed,',')"));
    }

    [Fact]
    public void SecretAspectQueriesUseBothNativeMasksAndNativeArgumentBounds()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "SecretAspectFont=CreateFont('SecretAspectFont'); return true");
        var value = session.Ui.Find("SecretAspectFont")!;
        value.SecretAspectMask = 0x12;
        value.SecondarySecretAspectMask = 0x400;
        value.ContainsSecretValues = true;
        value.PreventsSecretValues = true;

        Assert.Equal(
            "true:true:true:true:false:true:true:false:false:false:false:true",
            session.Lua.Evaluate(
                "local value=SecretAspectFont; " +
                "local missing=pcall(function() value:HasSecretAspect() end); " +
                "local negative=pcall(function() value:HasSecretAspect(-1) end); " +
                "local tooLarge=pcall(function() value:HasSecretAspect(0x800000) end); " +
                "local wrongType=pcall(function() value:HasSecretAspect({}) end); " +
                "return table.concat({" +
                "tostring(value:HasAnySecretAspect())," +
                "tostring(value:HasSecretAspect(0x2))," +
                "tostring(value:HasSecretAspect('16'))," +
                "tostring(value:HasSecretAspect(1024.9))," +
                "tostring(value:HasSecretAspect(0))," +
                "tostring(value:HasSecretValues())," +
                "tostring(value:IsPreventingSecretValues())," +
                "tostring(missing),tostring(negative),tostring(tooLarge)," +
                "tostring(wrongType),tostring(value:HasSecretAspect(0x7fffff))},':')"));
    }

    [Fact]
    public void ParentKeysAndDebugNamesFollowTheNativeParentPathRules()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "Upper:DebugChild:UIParent, DebugParent.Upper:true:true:nil:" +
            "UIParent, DebugParent, DebugChild:0:1",
            session.Lua.Evaluate(
                "local parent=CreateFrame('Frame','DebugParent',UIParent); " +
                "local child=parent:CreateTexture('DebugChild'); " +
                "parent.lower=child; child:SetParentKey('Upper'); " +
                "local selected=child:GetParentKey(); " +
                "local direct=child:GetDebugName(false); " +
                "local preferred=child:GetDebugName(true); " +
                "local clearCount=select('#',child:ClearParentKey()); " +
                "local parentCount=select('#',child:GetParent()); " +
                "return table.concat({selected,direct,preferred," +
                "tostring(parent.lower==nil),tostring(parent.Upper==nil)," +
                "tostring(child:GetParentKey()),child:GetDebugName(true)," +
                "clearCount,parentCount},':')"));
    }

    [Fact]
    public void RegionDerivedObjectsInheritTheRecoveredScriptObjectMethods()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "function:function:function:function:function:function",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame',nil,UIParent); " +
                "local texture=frame:CreateTexture(); " +
                "return table.concat({" +
                "type(frame.ClearParentKey),type(frame.GetDebugName)," +
                "type(frame.HasAnySecretAspect),type(texture.ClearParentKey)," +
                "type(texture.GetDebugName),type(texture.HasSecretAspect)},':')"));
    }
}
