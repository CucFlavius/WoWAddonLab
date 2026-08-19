namespace WoWAddonLab.Tests;

public sealed class PlayerStatePredicateContractTests
{
    [Fact]
    public void IsFallingUsesOptionalStringCoercibleUnitAndReturnsOneBoolean()
    {
        using var session = new EmulatorSession();
        session.Lua.Units.Player.IsFalling = true;

        Assert.Equal(
            "true:true:false:false:1",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(IsFalling())," +
                "tostring(IsFalling(nil,'ignored'))," +
                "tostring(IsFalling('missing'))," +
                "tostring(IsFalling(17))," +
                "select('#',IsFalling('player'))},':')"));

        Assert.Equal(
            "false:false",
            session.Lua.Evaluate(
                "return tostring(pcall(IsFalling,false))..':'.." +
                "tostring(pcall(IsFalling,{}))"));

        session.Lua.Units.PlayerAvailable = false;
        Assert.Equal("false", session.Lua.Evaluate("return tostring(IsFalling())"));
    }

    [Fact]
    public void IsFlyingUsesOptionalStringCoercibleUnitAndReturnsOneBoolean()
    {
        using var session = new EmulatorSession();
        session.Lua.Units.Player.IsFlying = true;

        Assert.Equal(
            "true:true:false:false:1",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(IsFlying())," +
                "tostring(IsFlying(nil,'ignored'))," +
                "tostring(IsFlying('missing'))," +
                "tostring(IsFlying(17))," +
                "select('#',IsFlying('player'))},':')"));

        Assert.Equal(
            "false:false",
            session.Lua.Evaluate(
                "return tostring(pcall(IsFlying,false))..':'.." +
                "tostring(pcall(IsFlying,{}))"));

        session.Lua.Units.PlayerAvailable = false;
        Assert.Equal("false", session.Lua.Evaluate("return tostring(IsFlying())"));
    }

    [Fact]
    public void IsLoggedInUsesOnlyCinematicStateBitEightAndIgnoresArguments()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:1",
            session.Lua.Evaluate(
                "return tostring(IsLoggedIn({},17))..':'..select('#',IsLoggedIn())"));

        session.Lua.PlayerScript.CinematicStateFlags = 0x4;
        Assert.Equal("true", session.Lua.Evaluate("return tostring(IsLoggedIn())"));

        session.Lua.PlayerScript.CinematicStateFlags = 0x8;
        Assert.Equal("false", session.Lua.Evaluate("return tostring(IsLoggedIn())"));

        session.Lua.PlayerScript.CinematicStateFlags = 0xC;
        Assert.Equal("false", session.Lua.Evaluate("return tostring(IsLoggedIn())"));
    }

    [Fact]
    public void MountedAndOutdoorsQueriesUseRepresentedPlayerStateAndPlayerBoundary()
    {
        using var session = new EmulatorSession();
        var player = session.Lua.Units.Player;

        Assert.Equal(
            "false:true:1:1",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(IsMounted('ignored'))," +
                "tostring(IsOutdoors({},17))," +
                "select('#',IsMounted()),select('#',IsOutdoors())},':')"));

        player.IsMounted = true;
        player.IsOutdoors = false;
        Assert.Equal(
            "true:false",
            session.Lua.Evaluate(
                "return tostring(IsMounted())..':'..tostring(IsOutdoors())"));

        session.Lua.Units.PlayerAvailable = false;
        Assert.Equal(
            "false:false",
            session.Lua.Evaluate(
                "return tostring(IsMounted())..':'..tostring(IsOutdoors())"));
    }
}
