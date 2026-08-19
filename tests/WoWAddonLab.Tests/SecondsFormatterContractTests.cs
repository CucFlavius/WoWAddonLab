namespace WoWAddonLab.Tests;

public sealed class SecondsFormatterContractTests
{
    private static readonly string[] NativeMethods =
    [
        "FormatNumber", "CanApproximate", "CanRoundUpIntervals",
        "CanRoundUpLastUnit", "EvaluateDesiredUnitCount", "EvaluateMaxInterval",
        "EvaluateMinInterval", "Format", "FormatZero",
        "GetApproximationSeconds", "GetConvertToLower",
        "GetDefaultAbbreviation", "GetDesiredUnitCount",
        "GetDesiredUnitCountCurve", "GetMaxInterval", "GetMaxIntervalCurve",
        "GetMillisecondsThreshold", "GetMinInterval", "GetMinIntervalCurve",
        "GetStripIntervalWhitespace", "Reset", "SetApproximationSeconds",
        "SetCanRoundUpIntervals", "SetCanRoundUpLastUnit", "SetConvertToLower",
        "SetDefaultAbbreviation", "SetDesiredUnitCount",
        "SetDesiredUnitCountCurve", "SetMaxInterval", "SetMaxIntervalCurve",
        "SetMillisecondsThreshold", "SetMinInterval", "SetMinIntervalCurve",
        "SetStripIntervalWhitespace"
    ];

    [Fact]
    public void RegistersNativeNamespaceEnumsUserdataAndMethodSurface()
    {
        using var session = new EmulatorSession();
        var names = string.Join(",", NativeMethods.Select(name => $"'{name}'"));

        Assert.Equal(
            "userdata:false:34:true:true:7:false:true:0:1:2:3:4:3:3:2",
            session.Lua.Evaluate(
                $"local f=C_StringUtil.CreateSecondsFormatter(); local names={{{names}}}; " +
                "local count=0; local all=true; for _,name in ipairs(names) do " +
                "count=count+1; all=all and type(f[name])=='function' end; " +
                "f.custom=7; local writable=pcall(function() f.Format=3 end); " +
                "return table.concat({type(f),tostring(getmetatable(f)),count,tostring(all)," +
                "tostring(debug.getupvalue(f.Format,1)==nil),f.custom,tostring(writable)," +
                "tostring(string.find(tostring(f),'SecondsFormatter:',1,true)==1)," +
                "Enum.SecondsFormatterAbbreviation.None," +
                "Enum.SecondsFormatterAbbreviation.Truncate," +
                "Enum.SecondsFormatterAbbreviation.OneLetter," +
                "Enum.SecondsFormatterInterval.Days," +
                "Enum.SecondsFormatterIntervalMeta.NumValues," +
                "Enum.SecondsFormatterIntervalMeta.MaxValue," +
                "Enum.SecondsFormatterIntervalWhitespaceMeta.NumValues," +
                "Enum.SecondsFormatterIntervalWhitespaceMeta.MaxValue},':')"));
    }

    [Fact]
    public void UsesRecoveredDefaultsFormattingModesAndResetState()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:0:false:false:false:0:3:2:0:1 minute 33 seconds:1m 33s:" +
            "2 minutes:0minutes:true:0:3:2:0:9",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateSecondsFormatter(); f.custom=9; " +
                "local defaults=table.concat({f:GetApproximationSeconds()," +
                "f:GetDefaultAbbreviation(),tostring(f:CanRoundUpIntervals())," +
                "tostring(f:CanRoundUpLastUnit()),tostring(f:GetConvertToLower())," +
                "f:GetMinInterval(),f:GetMaxInterval(),f:GetDesiredUnitCount()," +
                "f:GetMillisecondsThreshold()},':'); local normal=f:Format(93); " +
                "local short=f:Format(93,Enum.SecondsFormatterAbbreviation.OneLetter); " +
                "f:SetDesiredUnitCount(1); f:SetCanRoundUpLastUnit(true); " +
                "local rounded=f:Format(61); f:SetMinInterval(Enum.SecondsFormatterInterval.Minutes); " +
                "f:SetStripIntervalWhitespace(Enum.SecondsFormatterIntervalWhitespace.StripIgnoreLocale); " +
                "local zero=f:FormatZero(); f:Reset(); " +
                "return table.concat({defaults,normal,short,rounded,zero," +
                "tostring(f:GetDesiredUnitCountCurve()==nil),f:GetMinInterval()," +
                "f:GetMaxInterval(),f:GetDesiredUnitCount(),f:GetStripIntervalWhitespace()," +
                "f.custom},':')"));
    }

    [Fact]
    public void AppliesApproximationMillisecondThresholdAndCurveBackedValues()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:false:< 2 minutes:3.4:nil:true:1:nil:true:1:nil:true:1:60 minutes",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateSecondsFormatter(); " +
                "f:SetApproximationSeconds(300); local approx=f:Format(61); " +
                "f:SetMillisecondsThreshold(10); local millis=f:Format(3.44); " +
                "local c=C_CurveUtil.CreateCurve(); c:AddPoint(0,1); c:AddPoint(1000,1); " +
                "local can1=f:CanApproximate(1); local can2=f:CanApproximate(0); " +
                "f:SetDesiredUnitCountCurve(c); local staticCount=f:GetDesiredUnitCount(); " +
                "local sameCount=f:GetDesiredUnitCountCurve()==c; " +
                "f:SetMinIntervalCurve(c); local staticMin=f:GetMinInterval(); " +
                "local sameMin=f:GetMinIntervalCurve()==c; " +
                "f:SetMaxIntervalCurve(c); local staticMax=f:GetMaxInterval(); " +
                "local sameMax=f:GetMaxIntervalCurve()==c; " +
                "f:SetMillisecondsThreshold(0); f:SetApproximationSeconds(0); " +
                "return table.concat({tostring(can1),tostring(can2),approx,millis," +
                "tostring(staticCount),tostring(sameCount),f:EvaluateDesiredUnitCount(90)," +
                "tostring(staticMin),tostring(sameMin),f:EvaluateMinInterval(90)," +
                "tostring(staticMax),tostring(sameMax),f:EvaluateMaxInterval(90)," +
                "f:Format(3600)},':')"));
    }

    [Fact]
    public void DurationFormatMethodsRequireAndApplyNativeFormatterUserdata()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "3 seconds:1 second:4 seconds:2 seconds:0 seconds:2 seconds:" +
            "false:false:false:false",
            session.Lua.Evaluate(
                "local clock=C_DurationUtil.CreateManualClock(); clock:SetTime(4); " +
                "local d=C_DurationUtil.CreateDuration(); d:SetTimeFromStart(1,4,2); " +
                "d:SetClock(clock); local f=C_StringUtil.CreateSecondsFormatter(); " +
                "return table.concat({d:FormatElapsedDuration(f)," +
                "d:FormatRemainingDuration(f),d:FormatTotalDuration(f)," +
                "d:FormatElapsedDuration(f,1),d:FormatRemainingDuration(f,1)," +
                "d:FormatTotalDuration(f,1)," +
                "tostring(pcall(d.FormatElapsedDuration,d))," +
                "tostring(pcall(d.FormatElapsedDuration,d,{}))," +
                "tostring(pcall(d.FormatElapsedDuration,d,f,2))," +
                "tostring(pcall(d.FormatElapsedDuration,d,f,0,1))},':')"));
    }

    [Fact]
    public void RejectsInvalidNativeSetterAndFormatterArguments()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:false:false:false:false:false:false:false:false:true:true",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateSecondsFormatter(); " +
                "return table.concat({" +
                "tostring(pcall(C_StringUtil.CreateSecondsFormatter,1))," +
                "tostring(pcall(f.Format,f,nil))," +
                "tostring(pcall(f.Format,f,1,3))," +
                "tostring(pcall(f.SetDefaultAbbreviation,f,nil))," +
                "tostring(pcall(f.SetMinInterval,f,4))," +
                "tostring(pcall(f.SetDesiredUnitCount,f,256))," +
                "tostring(pcall(f.SetDesiredUnitCountCurve,f,{}))," +
                "tostring(pcall(f.SetCanRoundUpLastUnit,f,nil))," +
                "tostring(pcall(f.GetMinInterval,f,1))," +
                "tostring(pcall(f.SetCanRoundUpLastUnit,f,0))," +
                "tostring(f:CanRoundUpLastUnit())},':')"));
    }
}
