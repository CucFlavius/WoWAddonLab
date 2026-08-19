namespace WoWAddonLab.Emulator.Lua;

public interface IWowRaceProvider
{
    IReadOnlyDictionary<int, WowRaceInfoState> Races { get; }
}
