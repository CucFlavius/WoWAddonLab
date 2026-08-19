using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class CurveObjectContractTests
{
    private static readonly string[] NativeMethods =
    [
        "GetType", "HasSecretValues", "SetType", "AddPoint", "ClearPoints",
        "Copy", "Evaluate", "GetPoint", "GetPointCount", "GetPoints",
        "RemovePoint", "SetPoints", "SetToDefaults"
    ];

    [Fact]
    public void RegistersNativeCurveSurfaceEnumsAndNamespace()
    {
        using var session = new EmulatorSession();
        var names = string.Join(",", NativeMethods.Select(name => $"'{name}'"));

        Assert.Equal(
            "userdata:false:13:true:false:true:0:1:2:3:4:0:3:true",
            session.Lua.Evaluate(
                $"local c=C_CurveUtil.CreateCurve('ignored'); local names={{{names}}}; " +
                "local count=0; local all=true; for _,name in ipairs(names) do " +
                "count=count+1; all=all and type(c[name])=='function' end; c.custom=7; " +
                "local writable=pcall(function() c.Evaluate=3 end); " +
                "local api={'CreateColorCurve','CreateCurve','EvaluateColorFromBoolean'," +
                "'EvaluateColorValueFromBoolean','EvaluateGameCurve'}; local apiOk=true; " +
                "for _,name in ipairs(api) do apiOk=apiOk and type(C_CurveUtil[name])=='function' end; " +
                "return table.concat({type(c),tostring(getmetatable(c)),count,tostring(all)," +
                "tostring(writable),tostring(debug.getupvalue(c.Evaluate,1)==nil)," +
                "Enum.LuaCurveType.Linear,Enum.LuaCurveType.Step,Enum.LuaCurveType.Cosine," +
                "Enum.LuaCurveType.Cubic,Enum.LuaCurveTypeMeta.NumValues," +
                "Enum.LuaCurveTypeMeta.MinValue,Enum.LuaCurveTypeMeta.MaxValue," +
                "tostring(apiOk)},':')"));
    }

    [Fact]
    public void MaintainsNativePointOrderingCopiesAndDefaults()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "3:1:10:2:25:2:20:true:true:2:3:30:4:40:0:0:0:true:9",
            session.Lua.Evaluate(
                "local c=C_CurveUtil.CreateCurve(); c:AddPoint(2,20); c:AddPoint(1,10); " +
                "c:AddPoint(2,25); local p=c:GetPoints(); local zero=c:GetPoint(0); " +
                "local past=c:GetPoint(4); local copy=c:Copy(); copy:RemovePoint(2); " +
                "c:SetPoints({{x=4,y=40},{x=3,y=30}}); local sorted=c:GetPoints(); " +
                "c:SetType(Enum.LuaCurveType.Cubic); c.custom=9; c:SetToDefaults(); " +
                "return table.concat({#p,p[1].x,p[1].y,p[2].x,p[2].y,p[3].x,p[3].y," +
                "tostring(zero==nil),tostring(past==nil),copy:GetPointCount()," +
                "sorted[1].x,sorted[1].y,sorted[2].x,sorted[2].y," +
                "c:GetPointCount(),c:GetType(),c:Evaluate(12)," +
                "tostring(c.custom==9),c.custom},':')"));
    }

    [Fact]
    public void EvaluatesAllRecoveredCurveTypesAtNativeBoundaries()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0.000:5.000:10.000:10.000:0.000:0.000:50.000:100.000:" +
            "5.000:10.000:15.000:20.000:5.000",
            session.Lua.Evaluate(
                "local c=C_CurveUtil.CreateCurve(); c:AddPoint(0,0); c:AddPoint(10,10); " +
                "local linear=string.format('%.3f:%.3f:%.3f:%.3f'," +
                "c:Evaluate(-1),c:Evaluate(5),c:Evaluate(10),c:Evaluate(11)); " +
                "c:SetPoints({{x=0,y=0},{x=5,y=50},{x=10,y=100}}); " +
                "c:SetType(Enum.LuaCurveType.Step); local step=string.format('%.3f:%.3f:%.3f:%.3f'," +
                "c:Evaluate(0),c:Evaluate(5),c:Evaluate(10),c:Evaluate(11)); " +
                "c:SetPoints({{x=0,y=0},{x=10,y=10}}); c:SetType(Enum.LuaCurveType.Cosine); " +
                "local cosine=string.format('%.3f',c:Evaluate(5)); " +
                "c:SetPoints({{x=0,y=0},{x=1,y=10},{x=2,y=20},{x=3,y=30}}); " +
                "c:SetType(Enum.LuaCurveType.Cubic); local cubic=string.format('%.3f:%.3f:%.3f'," +
                "c:Evaluate(0),c:Evaluate(1.5),c:Evaluate(3)); " +
                "c:SetPoints({{x=0,y=0},{x=10,y=10}}); " +
                "local fallback=string.format('%.3f',c:Evaluate(5)); " +
                "return table.concat({linear,step,cosine,cubic,fallback},':')"));
    }

    [Fact]
    public void RejectsInvalidPointMutationsWithoutReplacingExistingPoints()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:1:10",
            session.Lua.Evaluate(
                "local c=C_CurveUtil.CreateCurve(); c:AddPoint(1,10); local tooMany={}; " +
                "for i=1,257 do tooMany[i]={x=i,y=i} end; " +
                "local badShape=pcall(c.SetPoints,c,{{x=1}}); " +
                "local overflow=pcall(c.SetPoints,c,tooMany); " +
                "local removeZero=pcall(c.RemovePoint,c,0); " +
                "local removePast=pcall(c.RemovePoint,c,2); local p=c:GetPoint(1); " +
                "return table.concat({tostring(badShape),tostring(overflow)," +
                "tostring(removeZero),tostring(removePast),c:GetPointCount(),p.y},':')"));
    }

    [Fact]
    public void DurationEvaluateMethodsRequireAndApplyNativeCurveUserdata()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "30:20:10:0:40:20:75:25:100:0:false:false:false",
            session.Lua.Evaluate(
                "local clock=C_DurationUtil.CreateManualClock(); clock:SetTime(4); " +
                "local d=C_DurationUtil.CreateDuration(); d:SetTimeFromStart(1,4,2); d:SetClock(clock); " +
                "local seconds=C_CurveUtil.CreateCurve(); seconds:AddPoint(0,0); seconds:AddPoint(4,40); " +
                "local percent=C_CurveUtil.CreateCurve(); percent:AddPoint(0,0); percent:AddPoint(1,100); " +
                "return table.concat({d:EvaluateElapsedDuration(seconds)," +
                "d:EvaluateElapsedDuration(seconds,1),d:EvaluateRemainingDuration(seconds)," +
                "d:EvaluateRemainingDuration(seconds,1),d:EvaluateTotalDuration(seconds)," +
                "d:EvaluateTotalDuration(seconds,1),d:EvaluateElapsedPercent(percent)," +
                "d:EvaluateRemainingPercent(percent),d:EvaluateElapsedPercent(percent,1)," +
                "d:EvaluateRemainingPercent(percent,1)," +
                "tostring(pcall(d.EvaluateElapsedDuration,d,nil))," +
                "tostring(pcall(d.EvaluateElapsedDuration,d,{}))," +
                "tostring(pcall(d.EvaluateElapsedDuration,d,seconds,2))},':')"));
    }
}
