using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactRaceCatalog : TactCatalog, IWowRaceProvider
{
    private TactRaceCatalog(IReadOnlyDictionary<int, WowRaceInfoState> races)
    {
        Races = races;
    }

    public IReadOnlyDictionary<int, WowRaceInfoState> Races { get; }

    public static TactRaceCatalog Load(TactAssetSource tact, string build)
    {
        var races = new Dictionary<int, WowRaceInfoState>();
        foreach (var row in tact.Database.Load("ChrRaces", build).Values)
        {
            var id = Integer(row, "ID");
            var name = Text(row, "Name_lang", "Name");
            var clientFileString = Text(row, "ClientFileString");
            if (id <= 0 || string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(clientFileString))
            {
                continue;
            }

            races[id] = new WowRaceInfoState(id, name, clientFileString);
        }

        return new TactRaceCatalog(races);
    }
}
