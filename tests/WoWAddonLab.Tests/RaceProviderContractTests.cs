using WoWAddonLab.Emulator.Lua;
using WoWAddonLab.Assets;

namespace WoWAddonLab.Tests;

public sealed class RaceProviderContractTests
{
    [Fact]
    public void ChrRacesUsesItsClientFileDataId()
    {
        Assert.True(
            Db2FileDataIds.TryGet("dbfilesclient/chrraces.db2", out var fileDataId));
        Assert.Equal(1305311U, fileDataId);
    }

    [Fact]
    public void RaceProviderPopulatesCreatureInfoState()
    {
        using var session = new EmulatorSession();
        session.RaceProvider = new TestRaceProvider(
            new Dictionary<int, WowRaceInfoState>
            {
                [1] = new(1, "Human", "Human"),
                [22] = new(22, "Worgen", "Worgen")
            });

        Assert.Equal(
            "Human:Human:1:Worgen:1:nil",
            session.Lua.Evaluate(
                "local human=C_CreatureInfo.GetRaceInfo(1); " +
                "local worgen=C_CreatureInfo.GetRaceInfo(22); " +
                "return table.concat({human.raceName,human.clientFileString," +
                "human.raceID,worgen.raceName," +
                "select('#',C_CreatureInfo.GetRaceInfo(999))," +
                "tostring(C_CreatureInfo.GetRaceInfo(999))},':')"));
    }

    private sealed record TestRaceProvider(
        IReadOnlyDictionary<int, WowRaceInfoState> Races) : IWowRaceProvider;
}
