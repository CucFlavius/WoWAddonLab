using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class CombatTextContractTests
{
    [Fact]
    public void UsesNativeSurfaceAritiesAndDynamicEventTuple()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "3:0:0:0:1:player",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_CombatText) do " +
                "count=count+1 end;" +
                "local emptyActive=select('#'," +
                "C_CombatText.GetActiveUnit());" +
                "local emptyEvent=select('#'," +
                "C_CombatText.GetCurrentEventInfo());" +
                "local setCount=select('#'," +
                "C_CombatText.SetActiveUnit('player'));" +
                "local activeCount=select('#'," +
                "C_CombatText.GetActiveUnit());" +
                "return table.concat({count,emptyActive,emptyEvent," +
                "setCount,activeCount,C_CombatText.GetActiveUnit()},':')"));

        session.Lua.CombatText.CurrentEventInfo.Add("DAMAGE");
        session.Lua.CombatText.CurrentEventInfo.Add(null);
        session.Lua.CombatText.CurrentEventInfo.Add(42d);

        Assert.Equal(
            "3:DAMAGE:nil:42",
            session.Lua.Evaluate(
                "local count=select('#'," +
                "C_CombatText.GetCurrentEventInfo());" +
                "local eventType,hiddenValue,amount=" +
                "C_CombatText.GetCurrentEventInfo();" +
                "return table.concat({count,eventType," +
                "tostring(hiddenValue),amount},':')"));
    }

    [Fact]
    public void SetActiveUnitUsesRequiredCoercibleTokenAndResolvedGuidSemantics()
    {
        using var session = new EmulatorSession();
        session.Lua.Units.AssignAlias(
            "target",
            session.Lua.Units.Player);

        Assert.Equal(
            "false:false:false:false:player:player:0:0:player",
            session.Lua.Evaluate(
                "C_CombatText.SetActiveUnit('player');" +
                "local missing=pcall(C_CombatText.SetActiveUnit);" +
                "local nilValue=pcall(C_CombatText.SetActiveUnit,nil);" +
                "local tableValue=pcall(" +
                "C_CombatText.SetActiveUnit,{});" +
                "local booleanValue=pcall(" +
                "C_CombatText.SetActiveUnit,false);" +
                "local afterErrors=C_CombatText.GetActiveUnit();" +
                "C_CombatText.SetActiveUnit('target');" +
                "local canonical=C_CombatText.GetActiveUnit();" +
                "C_CombatText.SetActiveUnit(17);" +
                "local numericCount=select('#'," +
                "C_CombatText.GetActiveUnit());" +
                "C_CombatText.SetActiveUnit('not-a-unit');" +
                "local unresolvedCount=select('#'," +
                "C_CombatText.GetActiveUnit());" +
                "C_CombatText.SetActiveUnit('PLAYER');" +
                "return table.concat({" +
                "tostring(missing),tostring(nilValue)," +
                "tostring(tableValue),tostring(booleanValue)," +
                "afterErrors,canonical,numericCount," +
                "unresolvedCount,C_CombatText.GetActiveUnit()},':')"));
    }
}
