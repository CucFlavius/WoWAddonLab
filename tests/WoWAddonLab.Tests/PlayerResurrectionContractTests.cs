namespace WoWAddonLab.Tests;

public sealed class PlayerResurrectionContractTests
{
    [Fact]
    public void ResurrectionGlobalsReturnNativeDefaultShapesAndIgnoreArguments()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1:nil:1:false:1:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "select('#',ResurrectGetOfferer('ignored'))," +
                "tostring(ResurrectGetOfferer('ignored'))," +
                "select('#',ResurrectHasSickness('ignored'))," +
                "tostring(ResurrectHasSickness('ignored'))," +
                "select('#',ResurrectHasTimer('ignored'))," +
                "tostring(ResurrectHasTimer('ignored'))},':')"));
    }

    [Fact]
    public void ResurrectionGlobalsExposeRepresentedOfferState()
    {
        using var session = new EmulatorSession();
        session.Lua.PlayerScript.ResurrectOffererName = "Spirit Healer";
        session.Lua.PlayerScript.ResurrectHasSickness = true;
        session.Lua.PlayerScript.ResurrectHasTimer = true;

        Assert.Equal(
            "Spirit Healer:true:true",
            session.Lua.Evaluate(
                "return table.concat({ResurrectGetOfferer()," +
                "tostring(ResurrectHasSickness())," +
                "tostring(ResurrectHasTimer())},':')"));
    }

    [Fact]
    public void ActiveBattlefieldArenaSuppressesOnlyTheResurrectionTimerResult()
    {
        using var session = new EmulatorSession();
        session.Lua.PlayerScript.ResurrectOffererName = string.Empty;
        session.Lua.PlayerScript.ResurrectHasSickness = true;
        session.Lua.PlayerScript.ResurrectHasTimer = true;
        session.Lua.Pvp.IsActiveBattlefieldArena = true;

        Assert.Equal(
            ":true:false",
            session.Lua.Evaluate(
                "return table.concat({ResurrectGetOfferer()," +
                "tostring(ResurrectHasSickness())," +
                "tostring(ResurrectHasTimer())},':')"));
        Assert.True(session.Lua.PlayerScript.ResurrectHasTimer);
    }
}
