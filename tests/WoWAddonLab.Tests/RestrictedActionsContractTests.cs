namespace WoWAddonLab.Tests;

public sealed class RestrictedActionsContractTests
{
    [Fact]
    public void RestrictionStateApisExposeInactiveTransitionAndActiveStates()
    {
        using var session = new EmulatorSession();
        session.Lua.RestrictedActions.RestrictionStates[1] = 1;
        session.Lua.RestrictedActions.RestrictionStates[3] = 2;

        Assert.Equal(
            "0:false:1:false:2:true:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "C_RestrictedActions.GetAddOnRestrictionState(4)," +
                "tostring(C_RestrictedActions.IsAddOnRestrictionActive(4))," +
                "C_RestrictedActions.GetAddOnRestrictionState(1)," +
                "tostring(C_RestrictedActions.IsAddOnRestrictionActive(1))," +
                "C_RestrictedActions.GetAddOnRestrictionState(3)," +
                "tostring(C_RestrictedActions.IsAddOnRestrictionActive(3))," +
                "tostring(pcall(C_RestrictedActions.GetAddOnRestrictionState,{}))," +
                "tostring(pcall(C_RestrictedActions.IsAddOnRestrictionActive,6))},':')"));
    }

    [Fact]
    public void CombatRestrictionFollowsCombatLockdownWithoutAnOverride()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:false",
            session.Lua.Evaluate(
                "return C_RestrictedActions.GetAddOnRestrictionState(0)..':'.." +
                "tostring(C_RestrictedActions.IsAddOnRestrictionActive(0))"));

        session.Lua.Client.InCombatLockdown = true;

        Assert.Equal(
            "2:true",
            session.Lua.Evaluate(
                "return C_RestrictedActions.GetAddOnRestrictionState(0)..':'.." +
                "tostring(C_RestrictedActions.IsAddOnRestrictionActive(0))"));
    }
}
