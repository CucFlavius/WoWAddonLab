namespace WoWAddonLab.Tests;

public sealed class NewItemsContractTests
{
    [Fact]
    public void NewItemQueriesAndMutationsUseSimulationState()
    {
        using var session = new EmulatorSession();
        session.Lua.NewItems.MarkNewItem(0, 3);
        session.Lua.NewItems.MarkNewItem(2, 7);

        Assert.Equal(
            "true:true:0:false:0:false:false",
            session.Lua.Evaluate(
                "local first=C_NewItems.IsNewItem(0,3); " +
                "local second=C_NewItems.IsNewItem(2,7); " +
                "local removed=select('#',C_NewItems.RemoveNewItem(0,3)); " +
                "local afterRemove=C_NewItems.IsNewItem(0,3); " +
                "local cleared=select('#',C_NewItems.ClearAll()); " +
                "local afterClear=C_NewItems.IsNewItem(2,7); " +
                "return table.concat({tostring(first),tostring(second),removed," +
                "tostring(afterRemove),cleared,tostring(afterClear)," +
                "tostring(C_NewItems.IsNewItem(1,1))},':')"));
    }

    [Fact]
    public void NewItemFunctionsValidateNativeRequiredArguments()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_NewItems.IsNewItem))," +
                "tostring(pcall(C_NewItems.IsNewItem,0,0))," +
                "tostring(pcall(C_NewItems.IsNewItem,'bag',1))," +
                "tostring(pcall(C_NewItems.RemoveNewItem,0))," +
                "tostring(pcall(C_NewItems.RemoveNewItem,0,4294967295))},':')"));
    }
}
