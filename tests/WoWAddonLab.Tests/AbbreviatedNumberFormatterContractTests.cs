namespace WoWAddonLab.Tests;

public sealed class AbbreviatedNumberFormatterContractTests
{
    private static readonly string[] NativeMethods =
    [
        "FormatNumber", "AddBreakpoint", "ClearBreakpoints", "Copy",
        "GetBreakpoints", "ResetBreakpoints", "SetBreakpoints"
    ];

    [Fact]
    public void RegistersRecoveredUserdataSurfaceAndDefaultBreakpointRecords()
    {
        using var session = new EmulatorSession();
        var names = string.Join(",", NativeMethods.Select(name => $"'{name}'"));

        Assert.Equal(
            "userdata:false:7:true:true:9:false:true:8:10000000000000:" +
            "FOURTH_NUMBER_CAP_NO_SPACE:1000000000000:1:true:1000:100:10:true",
            session.Lua.Evaluate(
                $"local f=C_StringUtil.CreateAbbreviatedNumberFormatter('ignored'); " +
                $"local names={{{names}}}; local count=0; local all=true; " +
                "for _,name in ipairs(names) do count=count+1; " +
                "all=all and type(f[name])=='function' end; f.custom=9; " +
                "local writable=pcall(function() f.Copy=3 end); " +
                "local b=f:GetBreakpoints(); local first,last=b[1],b[#b]; " +
                "return table.concat({type(f),tostring(getmetatable(f)),count," +
                "tostring(all),tostring(debug.getupvalue(f.FormatNumber,1)==nil)," +
                "f.custom,tostring(writable),tostring(string.find(tostring(f)," +
                "'AbbreviatedNumberFormatter:',1,true)==1),#b,first.breakpoint," +
                "first.abbreviation,first.significandDivisor,first.fractionDivisor," +
                "tostring(first.abbreviationIsGlobal),last.breakpoint," +
                "last.significandDivisor,last.fractionDivisor," +
                "tostring(last.abbreviationIsGlobal)},':')"));
    }

    [Fact]
    public void FormatsWithRecoveredDescendingFloorAndDivisorAlgorithm()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "999:9.9K:12K:1.2M:12M:1.2B:12B:1.2T:12T:-1234",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateAbbreviatedNumberFormatter(); " +
                "return table.concat({f:FormatNumber(999),f:FormatNumber(9999)," +
                "f:FormatNumber(12345),f:FormatNumber(1234567)," +
                "f:FormatNumber(12345678),f:FormatNumber(1234567890)," +
                "f:FormatNumber(12345678901),f:FormatNumber(1234567890123)," +
                "f:FormatNumber(12345678901234),f:FormatNumber(-1234)},':')"));
    }

    [Fact]
    public void MutatesSortsCopiesAndResetsBreakpointsIndependently()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1234:1.2X:2:1000:100:2.3Q:1.2X:1.2X:8:9",
            session.Lua.Evaluate(
                "CUSTOM_ABBREVIATION='Q'; local f=" +
                "C_StringUtil.CreateAbbreviatedNumberFormatter(); f.custom=9; " +
                "f:ClearBreakpoints(); local raw=f:FormatNumber(1234); " +
                "f:AddBreakpoint({breakpoint=100,abbreviation='X'," +
                "significandDivisor=10,fractionDivisor=10," +
                "abbreviationIsGlobal=false}); local custom=f:FormatNumber(123); " +
                "f:AddBreakpoint({breakpoint=1000,abbreviation=" +
                "'CUSTOM_ABBREVIATION',significandDivisor=100," +
                "fractionDivisor=10}); local b=f:GetBreakpoints(); " +
                "local global=f:FormatNumber(2345); local copy=f:Copy(); " +
                "f:ClearBreakpoints(); local copied=copy:FormatNumber(123); " +
                "copy:SetBreakpoints({{breakpoint=100,abbreviation='X'," +
                "significandDivisor=10,fractionDivisor=10," +
                "abbreviationIsGlobal=false}}); local set=copy:FormatNumber(123); " +
                "copy:ResetBreakpoints(); return table.concat({raw,custom,#b," +
                "b[1].breakpoint,b[2].breakpoint,global,copied,set," +
                "#copy:GetBreakpoints(),f.custom},':')"));
    }

    [Fact]
    public void SuppliesTheSharedNumericFormatterBoundaryToDurationBindings()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "3u:3u:true",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateAbbreviatedNumberFormatter(); " +
                "f:SetBreakpoints({{breakpoint=1,abbreviation='u'," +
                "significandDivisor=1,fractionDivisor=1," +
                "abbreviationIsGlobal=false}}); local c=C_DurationUtil.CreateManualClock(); " +
                "c:SetTime(2); local d=C_DurationUtil.CreateDuration(); " +
                "d:SetTimeFromStart(1,4); d:SetClock(c); " +
                "local b=C_DurationUtil.CreateDurationTextBinding(); " +
                "b:SetDuration(d); b:SetFormatter(f); " +
                "return table.concat({f:FormatNumber(3),b:GetFormattedText()," +
                "tostring(b:CanFormatText())},':')"));
    }

    [Fact]
    public void RejectsInvalidBreakpointShapesAndPredicates()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:false:false:false:false:false:true:false:true:0",
            session.Lua.Evaluate(
                "local f=C_StringUtil.CreateAbbreviatedNumberFormatter(); " +
                "local function add(v) return pcall(f.AddBreakpoint,f,v) end; " +
                "return table.concat({" +
                "tostring(pcall(C_StringUtil.CreateAbbreviatedNumberFormatter,1))," +
                "tostring(add({breakpoint=0,abbreviation='X'," +
                "significandDivisor=1,fractionDivisor=1}))," +
                "tostring(add({breakpoint=20,abbreviation='X'," +
                "significandDivisor=1,fractionDivisor=1}))," +
                "tostring(add({breakpoint=10,abbreviation='X'," +
                "significandDivisor=0,fractionDivisor=1}))," +
                "tostring(add({breakpoint=10,abbreviation='X'," +
                "significandDivisor=1,fractionDivisor=0}))," +
                "tostring(add({breakpoint=10,significandDivisor=1," +
                "fractionDivisor=1})),tostring(pcall(f.SetBreakpoints,f,{}))," +
                "tostring(pcall(f.SetBreakpoints,f,{{breakpoint=20," +
                "abbreviation='X',significandDivisor=1,fractionDivisor=1}}))," +
                "tostring(pcall(f.SetBreakpoints,f,{})),#f:GetBreakpoints()},':')"));
    }
}
