namespace WoWAddonLab.Tests;

public sealed class TimerContractTests
{
    [Fact]
    public void TimerDurationsRequireFiniteNumbersAndUseNativeBounds()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:false:true:true",
            session.Lua.Evaluate(
                "local callback=function() end;" +
                "local missing=pcall(C_Timer.After);" +
                "local text=pcall(C_Timer.After,'nope',callback);" +
                "local nan=pcall(C_Timer.After,0/0,callback);" +
                "local negative=pcall(C_Timer.After,-1,callback);" +
                "local huge=pcall(C_Timer.NewTicker,4294967.296,callback);" +
                "local numeric=pcall(C_Timer.After,'0',callback);" +
                "local valid=pcall(C_Timer.NewTimer,4294967.295,callback);" +
                "return table.concat({tostring(missing),tostring(text),tostring(nan)," +
                "tostring(negative),tostring(huge),tostring(numeric),tostring(valid)},':')"));
    }

    [Fact]
    public void TimerAndTickerCallbacksReceiveTheirExactHandles()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "afterArgs=-1; timerSame=false; timerArgs=-1; tickerSame=false; tickerCount=0;" +
            "C_Timer.After(0,function(...) afterArgs=select('#',...) end);" +
            "local timer; timer=C_Timer.NewTimer(0,function(value,...) " +
            "timerSame=value==timer; timerArgs=1+select('#',...) end);" +
            "local ticker; ticker=C_Timer.NewTicker(0,function(value) " +
            "tickerSame=value==ticker; tickerCount=tickerCount+1; value:Cancel() end)");

        session.Tick(1.0 / 60.0);

        Assert.Equal(
            "0:true:1:true:1",
            session.Lua.Evaluate(
                "return table.concat({afterArgs,tostring(timerSame),timerArgs," +
                "tostring(tickerSame),tickerCount},':')"));
    }

    [Fact]
    public void TimerHandlesRequireTheirOwnerAndExposeInvoke()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "userdata:false:function:false:false:false:false:0:function:1:false:0:true:true:true",
            session.Lua.Evaluate(
                "local first,count=-1; local timer=C_Timer.NewTimer(10,function(...) " +
                "first=(...); count=select('#',...) end);" +
                "local missingSelf=pcall(timer.Cancel);" +
                "local wrongSelf=pcall(timer.Cancel,{});" +
                "local invalid=pcall(function() timer:Invoke('invalid') end);" +
                "local extra=pcall(function() timer:Invoke(function() end,2) end);" +
                "timer:Invoke(); local emptyCount=count;" +
                "timer:Invoke(function() end); local firstType=type(first);" +
                "local before=timer:IsCancelled();" +
                "local other=C_Timer.NewTimer(10,function() end); timer.Cancel(other);" +
                "local cancelCount=select('#',timer:Cancel());" +
                "return table.concat({type(timer),tostring(getmetatable(timer))," +
                "type(timer.Invoke),tostring(missingSelf),tostring(wrongSelf)," +
                "tostring(invalid),tostring(extra),emptyCount,firstType,count," +
                "tostring(before),cancelCount,tostring(timer:IsCancelled())," +
                "tostring(other:IsCancelled()),tostring(pcall(function() " +
                "timer:Invoke('ignored') end))},':')"));
    }

    [Fact]
    public void TickerIterationsAndDurationsUseMillisecondScheduling()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "finiteCount=0; infiniteCount=0; quantized=false;" +
            "finite=C_Timer.NewTicker(0,function() finiteCount=finiteCount+1 end,3);" +
            "infinite=C_Timer.NewTicker(0,function() infiniteCount=infiniteCount+1 end,0);" +
            "C_Timer.After(0.0019,function() quantized=true end)");

        session.Tick(0.0005);
        Assert.Equal("false", session.Lua.Evaluate("tostring(quantized)"));
        session.Tick(0.0005);
        Assert.Equal("true", session.Lua.Evaluate("tostring(quantized)"));
        session.Tick(0.001);
        session.Tick(0.001);

        Assert.Equal(
            "3:true:3:false",
            session.Lua.Evaluate(
                "return table.concat({finiteCount,tostring(finite:IsCancelled())," +
                "infiniteCount,tostring(infinite:IsCancelled())},':')"));
    }

    [Fact]
    public void ZeroDelayTimersWaitForClockAdvanceAndPreserveInsertionOrder()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "order={};" +
            "C_Timer.After(0,function() order[#order+1]='after' end);" +
            "C_Timer.NewTimer(0,function() order[#order+1]='timer' end);" +
            "C_Timer.NewTicker(0,function(cb) " +
            "order[#order+1]='ticker'; cb:Cancel() end)");

        session.Tick(0);
        session.Tick(0.0005);
        Assert.Equal("", session.Lua.Evaluate("return table.concat(order,':')"));
        session.Tick(0.0005);

        Assert.Equal(
            "after:timer:ticker",
            session.Lua.Evaluate("return table.concat(order,':')"));
    }

    [Fact]
    public void TimerEntryPointsAcceptRepresentedFunctionContainers()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "afterCount=0; tickerCount=0;" +
            "local afterCallback=C_FunctionContainers.CreateCallback(function() " +
            "afterCount=afterCount+1 end);" +
            "local tickerCallback=C_FunctionContainers.CreateCallback(function(cb) " +
            "tickerCount=tickerCount+1; cb:Cancel() end);" +
            "C_Timer.After(0,afterCallback);" +
            "C_Timer.NewTicker(0,tickerCallback)");

        session.Tick(0.001);

        Assert.Equal(
            "1:1",
            session.Lua.Evaluate("return afterCount..':'..tickerCount"));
    }
}
