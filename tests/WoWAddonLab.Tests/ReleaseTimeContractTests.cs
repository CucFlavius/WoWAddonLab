namespace WoWAddonLab.Tests;

public sealed class ReleaseTimeContractTests
{
    [Fact]
    public void MissingReleaseDeadlineReturnsOneZeroAndIgnoresArguments()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:1",
            session.Lua.Evaluate(
                "return GetReleaseTimeRemaining(false,17)..':'.." +
                "select('#',GetReleaseTimeRemaining({},'ignored'))"));
    }

    [Fact]
    public void SuppressedReleaseTimerReturnsNegativeOneBeforeConsideringDeadline()
    {
        using var session = new EmulatorSession();
        session.Lua.PlayerScript.ReleaseTimerSuppressed = true;
        session.Lua.PlayerScript.ReleaseDeadlineTickMilliseconds = 600_000;

        Assert.Equal("-1", session.Lua.Evaluate("return GetReleaseTimeRemaining()"));
    }

    [Fact]
    public void FutureReleaseDeadlineUsesSignedTickDeltaTruncatedToWholeSeconds()
    {
        using var session = new EmulatorSession();
        session.Tick(0.1);
        session.Lua.PlayerScript.ReleaseDeadlineTickMilliseconds =
            session.Lua.FrameTime.TickMilliseconds + 3_999;

        Assert.Equal("3", session.Lua.Evaluate("return GetReleaseTimeRemaining()"));

        session.Tick(0.25);
        Assert.Equal("3", session.Lua.Evaluate("return GetReleaseTimeRemaining()"));

        session.Lua.PlayerScript.ReleaseDeadlineTickMilliseconds =
            session.Lua.FrameTime.TickMilliseconds;
        Assert.Equal("0", session.Lua.Evaluate("return GetReleaseTimeRemaining()"));
    }
}
