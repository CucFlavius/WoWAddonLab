namespace WoWAddonLab.Tests;

public sealed class GmTicketGlobalsContractTests
{
    [Fact]
    public void TicketQueriesSubmitTheirDistinctNativeRequestsAndReturnNothing()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:0:0",
            session.Lua.Evaluate(
                "return select('#',GetWebTicket(false,17))..':'.." +
                "select('#',GetWebTicket())..':'.." +
                "select('#',GetGMStatus({},'ignored'))"));
        Assert.Equal(2, session.Lua.GmTicket.WebTicketRequestCount);
        Assert.Equal(1, session.Lua.GmTicket.GmStatusRequestCount);
    }

    [Fact]
    public void MissingRequestServiceSuppressesMessagesButKeepsZeroValueContract()
    {
        using var session = new EmulatorSession();
        session.Lua.GmTicket.RequestServiceAvailable = false;

        Assert.Equal(
            "0:0",
            session.Lua.Evaluate(
                "return select('#',GetWebTicket())..':'.." +
                "select('#',GetGMStatus())"));
        Assert.Equal(0, session.Lua.GmTicket.WebTicketRequestCount);
        Assert.Equal(0, session.Lua.GmTicket.GmStatusRequestCount);
    }
}
