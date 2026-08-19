namespace WoWAddonLab.Tests;

public sealed class PlayerHasToyContractTests
{
    [Fact]
    public void PlayerHasToyRequiresANumberAndReturnsOneBoolean()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:1:false",
            session.Lua.Evaluate(
                "local missing=pcall(PlayerHasToy);" +
                "local nilValue=pcall(PlayerHasToy,nil);" +
                "local boolean=pcall(PlayerHasToy,false);" +
                "return table.concat({tostring(missing),tostring(nilValue)," +
                "tostring(boolean),tostring(PlayerHasToy(0))," +
                "select('#',PlayerHasToy(42,'ignored'))," +
                "tostring(PlayerHasToy(42,'ignored'))},':')"));
    }

    [Fact]
    public void PlayerHasToyUsesSignedInt32ConversionAndRepresentedOwnedItems()
    {
        using var session = new EmulatorSession();
        session.Lua.ToyBox.OwnedItemIds.Add(123);
        session.Lua.ToyBox.OwnedItemIds.Add(-7);
        session.Lua.ToyBox.OwnedItemIds.Add(0);

        Assert.Equal(
            "true:true:true:false:false",
            session.Lua.Evaluate(
                "return table.concat({tostring(PlayerHasToy(123.9))," +
                "tostring(PlayerHasToy('123')),tostring(PlayerHasToy(-7))," +
                "tostring(PlayerHasToy(124)),tostring(PlayerHasToy(0))},':')"));
    }

    [Fact]
    public void PlayerHasToyReturnsFalseWithoutAPlayer()
    {
        using var session = new EmulatorSession();
        session.Lua.ToyBox.OwnedItemIds.Add(123);
        session.Lua.Units.PlayerAvailable = false;

        Assert.Equal("false", session.Lua.Evaluate("return tostring(PlayerHasToy(123))"));
    }
}
