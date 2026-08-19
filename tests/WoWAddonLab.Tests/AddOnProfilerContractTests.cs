using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class AddOnProfilerContractTests
{
    [Fact]
    public void RegistersExactSurfaceEnumsAndDefaultArities()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "10:0:1:1:1:1:1:1:0:0:true:" +
            "0:1:2:3:0:2:" +
            "0:1:2:3:4:5:6:7:8:9:10:11:12:0:11",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_AddOnProfiler) do " +
                "count=count+1 end;" +
                "return table.concat({" +
                "count," +
                "select('#',C_AddOnProfiler.CheckForPerformanceMessage())," +
                "select('#',C_AddOnProfiler.GetAddOnMetric('Missing',0))," +
                "select('#',C_AddOnProfiler.GetApplicationMetric(0))," +
                "select('#',C_AddOnProfiler.GetOverallMetric(0))," +
                "select('#',C_AddOnProfiler.GetTicksPerSecond())," +
                "select('#',C_AddOnProfiler.GetTopKAddOnsForMetric(0,5))," +
                "select('#',C_AddOnProfiler.IsEnabled())," +
                "C_AddOnProfiler.GetOverallMetric(0)," +
                "select('#',C_AddOnProfiler.AddMeasuredCallEvent('idle'))," +
                "tostring(C_AddOnProfiler.IsEnabled())," +
                "Enum.AddOnPerformanceMessageType.SpecificAddOnChatWarning," +
                "Enum.AddOnPerformanceMessageType.SpecificAddOnErrorDialog," +
                "Enum.AddOnPerformanceMessageType.OverallAddOnErrorDialog," +
                "Enum.AddOnPerformanceMessageTypeMeta.NumValues," +
                "Enum.AddOnPerformanceMessageTypeMeta.MinValue," +
                "Enum.AddOnPerformanceMessageTypeMeta.MaxValue," +
                "Enum.AddOnProfilerMetric.SessionAverageTime," +
                "Enum.AddOnProfilerMetric.RecentAverageTime," +
                "Enum.AddOnProfilerMetric.EncounterAverageTime," +
                "Enum.AddOnProfilerMetric.LastTime," +
                "Enum.AddOnProfilerMetric.PeakTime," +
                "Enum.AddOnProfilerMetric.CountTimeOver1Ms," +
                "Enum.AddOnProfilerMetric.CountTimeOver5Ms," +
                "Enum.AddOnProfilerMetric.CountTimeOver10Ms," +
                "Enum.AddOnProfilerMetric.CountTimeOver50Ms," +
                "Enum.AddOnProfilerMetric.CountTimeOver100Ms," +
                "Enum.AddOnProfilerMetric.CountTimeOver500Ms," +
                "Enum.AddOnProfilerMetric.CountTimeOver1000Ms," +
                "Enum.AddOnProfilerMetricMeta.NumValues," +
                "Enum.AddOnProfilerMetricMeta.MinValue," +
                "Enum.AddOnProfilerMetricMeta.MaxValue},':')"));
    }

    [Fact]
    public void UsesStrictNativeArgumentContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:false:false:false:false:false:false:" +
            "false:false:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_AddOnProfiler.AddMeasuredCallEvent))," +
                "tostring(ok(C_AddOnProfiler.AddMeasuredCallEvent,false))," +
                "tostring(ok(C_AddOnProfiler.GetAddOnMetric,'A',-1))," +
                "tostring(ok(C_AddOnProfiler.GetAddOnMetric,'A',12))," +
                "tostring(ok(C_AddOnProfiler.GetApplicationMetric))," +
                "tostring(ok(C_AddOnProfiler.GetOverallMetric,{}))," +
                "tostring(ok(C_AddOnProfiler.GetTopKAddOnsForMetric,0))," +
                "tostring(ok(C_AddOnProfiler.GetTopKAddOnsForMetric,0,{}))," +
                "tostring(ok(C_AddOnProfiler.AddPerformanceMessageShown,{}))," +
                "tostring(ok(C_AddOnProfiler.AddPerformanceMessageShown," +
                "{type=3,metric=0,metricValue=1,thresholdValue=2}))," +
                "tostring(ok(C_AddOnProfiler.AddPerformanceMessageShown," +
                "{type=0,metric=12,metricValue=1,thresholdValue=2}))," +
                "tostring(ok(C_AddOnProfiler.MeasureCall))," +
                "tostring(ok(C_AddOnProfiler.MeasureCall,17))},':')"));
    }

    [Fact]
    public void ExposesStateBackedMetricsAndNativeTopKOrdering()
    {
        using var session = new EmulatorSession();
        var profiler = session.Lua.AddOnProfiler;
        profiler.SetAddOnMetric(
            "Slow",
            WowAddOnProfilerMetric.PeakTime,
            30);
        profiler.SetAddOnMetric(
            "Medium",
            WowAddOnProfilerMetric.PeakTime,
            20);
        profiler.SetAddOnMetric(
            "Zero",
            WowAddOnProfilerMetric.PeakTime,
            0);
        profiler.SetAddOnMetric(
            "Fast",
            WowAddOnProfilerMetric.PeakTime,
            10);
        profiler.SetApplicationMetric(
            WowAddOnProfilerMetric.PeakTime,
            45);
        profiler.SetOverallMetric(
            WowAddOnProfilerMetric.PeakTime,
            60);
        profiler.TicksPerSecond = 12_345;

        Assert.Equal(
            "30:0:45:60:12345:Slow=30,Medium=20:3",
            session.Lua.Evaluate(
                "local top=C_AddOnProfiler.GetTopKAddOnsForMetric(" +
                "Enum.AddOnProfilerMetric.PeakTime,2);" +
                "local all=C_AddOnProfiler.GetTopKAddOnsForMetric(" +
                "Enum.AddOnProfilerMetric.PeakTime,-1);" +
                "return table.concat({" +
                "C_AddOnProfiler.GetAddOnMetric('Slow',4)," +
                "C_AddOnProfiler.GetAddOnMetric('slow',4)," +
                "C_AddOnProfiler.GetApplicationMetric(4)," +
                "C_AddOnProfiler.GetOverallMetric(4)," +
                "C_AddOnProfiler.GetTicksPerSecond()," +
                "top[1].addOnName..'='..top[1].metricValue..','.." +
                "top[2].addOnName..'='..top[2].metricValue,#all},':')"));
    }

    [Fact]
    public void PerformanceMessagesPreserveOptionalFieldAndShownSlots()
    {
        using var session = new EmulatorSession();
        var profiler = session.Lua.AddOnProfiler;
        profiler.PendingPerformanceMessage = new WowAddOnPerformanceMessage(
            WowAddOnPerformanceMessageType.SpecificAddOnErrorDialog,
            WowAddOnProfilerMetric.RecentAverageTime,
            "Dungeonmire",
            0.25,
            0.2);

        Assert.Equal(
            "1:1:Dungeonmire:0.25:0.2:0",
            session.Lua.Evaluate(
                "local msg=C_AddOnProfiler.CheckForPerformanceMessage();" +
                "C_AddOnProfiler.AddPerformanceMessageShown(msg);" +
                "return table.concat({" +
                "msg.type,msg.metric,msg.addOnName,msg.metricValue," +
                "msg.thresholdValue,select('#'," +
                "C_AddOnProfiler.CheckForPerformanceMessage())},':')"));

        Assert.Equal(
            "Dungeonmire",
            profiler.ShownPerformanceMessages[
                WowAddOnPerformanceMessageType.SpecificAddOnErrorDialog]
                .AddOnName);

        profiler.PendingPerformanceMessage = new WowAddOnPerformanceMessage(
            WowAddOnPerformanceMessageType.OverallAddOnErrorDialog,
            WowAddOnProfilerMetric.SessionAverageTime,
            null,
            0.4,
            0.3);

        Assert.Equal(
            "nil",
            session.Lua.Evaluate(
                "return tostring(C_AddOnProfiler." +
                "CheckForPerformanceMessage().addOnName)"));
    }

    [Fact]
    public void MeasureCallReturnsValuesAndCapturesNestedEvents()
    {
        using var session = new EmulatorSession();
        session.Lua.AddOnProfiler.AllocatedBytes = 100;
        session.Lua.AddOnProfiler.DeallocatedBytes = 25;

        Assert.Equal(
            "7:5:1:checkpoint:0:0:true:true:1:2",
            session.Lua.Evaluate(
                "local outer,a,b=C_AddOnProfiler.MeasureCall(function(x,y) " +
                "local inner=C_AddOnProfiler.MeasureCall(function() " +
                "C_AddOnProfiler.AddMeasuredCallEvent('checkpoint') end);" +
                "return x+y,y,#inner.events end,2,5);" +
                "local event=outer.events[1];" +
                "return table.concat({" +
                "a,b,#outer.events,event.name,event.allocatedBytes," +
                "event.deallocatedBytes," +
                "tostring(type(event.elapsedMilliseconds)=='number')," +
                "tostring(type(event.elapsedTicks)=='number')," +
                "select('#',C_AddOnProfiler.MeasureCall(function() end))," +
                "select('#',C_AddOnProfiler.MeasureCall(" +
                "function() return nil end))},':')"));
    }
}
