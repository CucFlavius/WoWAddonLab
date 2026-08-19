namespace WoWAddonLab.Tests;

public sealed class NumericRuleFormatterContractTests
{
    private static readonly string[] NativeMethods =
    [
        "FormatNumber", "AddBreakpoint", "ClearBreakpoints", "Copy",
        "GetBreakpoints", "SetBreakpoints"
    ];

    [Fact]
    public void RegistersRecoveredUserdataSurfaceEnumsAndEmptyFactoryState()
    {
        using var session = new EmulatorSession();
        var names = string.Join(",", NativeMethods.Select(name => $"'{name}'"));

        Assert.Equal(
            "userdata:false:6:true:11:false:true:0:0:1:2:3:0:2",
            session.Lua.Evaluate(
                $"local f=C_StringUtil.CreateNumericRuleFormatter('ignored'); " +
                $"local names={{{names}}}; local count=0; local all=true; " +
                "for _,name in ipairs(names) do count=count+1; " +
                "all=all and type(f[name])=='function' end; f.custom=11; " +
                "local writable=pcall(function() f.Copy=3 end); " +
                "local e=Enum.NumericRuleFormatRounding; " +
                "local m=Enum.NumericRuleFormatRoundingMeta; " +
                "return table.concat({type(f),tostring(getmetatable(f)),count," +
                "tostring(all),f.custom,tostring(writable)," +
                "tostring(string.find(tostring(f),'NumericRuleFormatter:',1,true)==1)," +
                "#f:GetBreakpoints(),e.Nearest,e.Up,e.Down,m.NumValues," +
                "m.MinValue,m.MaxValue},':')"));
    }

    [Fact]
    public void SelectsDescendingThresholdAndFallsBackToRawNumberFormatting()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1234.5:low 12:high 123:2:100:0",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateNumericRuleFormatter(); " +
                "local raw=f:FormatNumber(1234.5); " +
                "f:AddBreakpoint({threshold=0,step=0,rounding=0," +
                "format='low %.0f'}); " +
                "f:AddBreakpoint({threshold=100,step=0,rounding=0," +
                "format='high %.0f'}); local b=f:GetBreakpoints(); " +
                "return table.concat({raw,f:FormatNumber(12)," +
                "f:FormatNumber(123),#b,b[1].threshold,b[2].threshold},':')"));
    }

    [Fact]
    public void AppliesRecoveredRoundingClampAndComponentPipeline()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "10/1:20:10:20:10:-10",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateNumericRuleFormatter(); " +
                "f:SetBreakpoints({{threshold=0,step=10,rounding=0,min=0,max=100," +
                "format='%.0f/%.0f',components={{div=1,mod=60,step=1,rounding=2}," +
                "{div=60,mod=0,step=1,rounding=2}}}}); " +
                "local pipeline=f:FormatNumber(67); " +
                "local function round(mode,value) f:SetBreakpoints({{threshold=-100," +
                "step=10,rounding=mode,format='%.0f'}}); return f:FormatNumber(value) end; " +
                "return table.concat({pipeline,round(0,15),round(0,14)," +
                "round(1,11),round(2,19),round(2,-14)},':')"));
    }

    [Fact]
    public void PreservesOptionalRecordShapeAndCopiesStateIndependently()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:1:1:-3:1:9:2:5",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateNumericRuleFormatter(); " +
                "f:SetBreakpoints({{threshold=1,step=-2,rounding=0,components=" +
                "{{div=-3,mod=0,step=-4,rounding=1}}}}); local b=f:GetBreakpoints()[1]; " +
                "local copy=f:Copy(); copy:SetBreakpoints({{threshold=9,step=0," +
                "rounding=2,min=2,max=5,format='%.0f'}}); local c=copy:GetBreakpoints()[1]; " +
                "return table.concat({tostring(b.min==nil),tostring(b.max==nil)," +
                "tostring(b.format==''),#b.components,b.threshold,b.components[1].div," +
                "#f:GetBreakpoints(),c.threshold,c.min,c.max},':')"));
    }

    [Fact]
    public void SuppliesSharedFormatterBoundaryAndRejectsInvalidRecords()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "3u:3u:true:false:false:false:false:false:false:true:0:false",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateNumericRuleFormatter(); " +
                "f:SetBreakpoints({{threshold=0,step=0,rounding=0,format='%.0fu'}}); " +
                "local c=C_DurationUtil.CreateManualClock(); c:SetTime(2); " +
                "local d=C_DurationUtil.CreateDuration(); d:SetTimeFromStart(1,4); d:SetClock(c); " +
                "local b=C_DurationUtil.CreateDurationTextBinding(); " +
                "b:SetDuration(d); b:SetFormatter(f); " +
                "local function add(v) return pcall(f.AddBreakpoint,f,v) end; " +
                "local direct=f:FormatNumber(3); local bound=b:GetFormattedText(); " +
                "f:ClearBreakpoints(); " +
                "local empty=pcall(f.SetBreakpoints,f,{}); " +
                "local badFormat=C_StringUtil.CreateNumericRuleFormatter(); " +
                "badFormat:AddBreakpoint({threshold=0,step=0,rounding=0,format='%Q'}); " +
                "return table.concat({direct,bound,tostring(b:CanFormatText())," +
                "tostring(add({step=0,rounding=0}))," +
                "tostring(add({threshold=0,rounding=0}))," +
                "tostring(add({threshold=0,step=0,rounding=3}))," +
                "tostring(add({threshold=0/0,step=0,rounding=0}))," +
                "tostring(add({threshold=0,step=0,rounding=0,min='x'}))," +
                "tostring(add({threshold=0,step=0,rounding=0,components={{div=1}}}))," +
                "tostring(empty),#f:GetBreakpoints()," +
                "tostring(pcall(badFormat.FormatNumber,badFormat,1))},':')"));
    }
}
