namespace WoWAddonLab.Tests;

public sealed class TimeGlobalsContractTests
{
    [Fact]
    public void TimeReturnsTheCurrentEpochAndAcceptsCalendarTables()
    {
        using var session = new EmulatorSession();
        session.Lua.DateAndTime.CurrentTimeOverride =
            DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

        Assert.Equal(
            "1700000000:0",
            session.Lua.Evaluate(
                "local t={year=2020,month=1,day=1,hour=0,min=0,sec=0}; " +
                "return time()..':'..os.difftime(time(t),os.time(t))"));
    }

    [Fact]
    public void CompleteNativeTimeFamilyUsesOneGlobalRegistrarAndIgnoresArguments()
    {
        using var session = new EmulatorSession();
        session.Lua.Client.RealmHour = 17;
        session.Lua.Client.RealmMinute = 42;
        session.Lua.DateAndTime.CurrentTimeOverride =
            new DateTimeOffset(2026, 7, 24, 14, 35, 0, TimeSpan.FromHours(3));
        session.Lua.FrameTime.FixedTimeStepSeconds = 1f / 60f;

        Assert.Equal(
            "17:42:14:35:2:2:1:1:1:1:1:true",
            session.Lua.Evaluate(
                "local gameHour,gameMinute=GetGameTime('ignored'); " +
                "local localHour,localMinute=GetLocalGameTime(false,17); " +
                "return table.concat({gameHour,gameMinute,localHour,localMinute," +
                "select('#',GetGameTime()),select('#',GetLocalGameTime())," +
                "select('#',GetServerTime({})),select('#',GetSessionTime({}))," +
                "select('#',GetTickTime({})),select('#',GetTime({}))," +
                "select('#',IsUsingFixedTimeStep({}))," +
                "tostring(IsUsingFixedTimeStep())},':')"));
    }

    [Fact]
    public void GetTimeUsesUnsignedMillisecondTicksWhileGetTickTimeRetainsFloatDelta()
    {
        using var session = new EmulatorSession();

        session.Tick(1d / 60d);

        Assert.Equal(
            "0.016:0.016:0.016666668",
            session.Lua.Evaluate(
                "return string.format('%.3f:%.3f:%.9f'," +
                "GetTime('ignored'),GetSessionTime(false),GetTickTime({}))"));

        session.Tick(1d / 60d);

        Assert.Equal(
            "0.033:0.033",
            session.Lua.Evaluate(
                "return string.format('%.3f:%.3f',GetTime(),GetSessionTime())"));
    }

    [Fact]
    public void DebugProfilerMeasuresMillisecondsFromItsLastReset()
    {
        using var session = new EmulatorSession();

        session.Tick(0.125);
        Assert.Equal(
            "0",
            session.Lua.Evaluate("return select('#',debugprofilestart())"));
        session.Tick(0.375);

        Assert.Equal(
            "250:1",
            session.Lua.Evaluate(
                "return debugprofilestop()..':'.." +
                "select('#',debugprofilestop())"));
    }

    [Fact]
    public void GetFramerateUsesTheZeroFilledSixtySampleIntegerMillisecondRing()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1000.000",
            session.Lua.Evaluate("return string.format('%.3f',GetFramerate())"));

        session.Tick(1d / 60d);
        Assert.Equal(
            "3750.000",
            session.Lua.Evaluate("return string.format('%.3f',GetFramerate(false))"));

        for (var frame = 1; frame < 60; frame++)
            session.Tick(1d / 60d);

        Assert.Equal(
            "62.500:1",
            session.Lua.Evaluate(
                "return string.format('%.3f',GetFramerate())..':'.." +
                "select('#',GetFramerate({},17))"));
    }
}
