using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClassState
{
    public IList<WowClassInfoState> Classes { get; } =
    [
        new(1, "Warrior", "WARRIOR"),
        new(2, "Paladin", "PALADIN"),
        new(3, "Hunter", "HUNTER"),
        new(4, "Rogue", "ROGUE"),
        new(5, "Priest", "PRIEST"),
        new(6, "Death Knight", "DEATHKNIGHT"),
        new(7, "Shaman", "SHAMAN"),
        new(8, "Mage", "MAGE"),
        new(9, "Warlock", "WARLOCK"),
        new(10, "Monk", "MONK"),
        new(11, "Druid", "DRUID"),
        new(12, "Demon Hunter", "DEMONHUNTER"),
        new(13, "Evoker", "EVOKER")
    ];

    public IDictionary<int, WowCreatureFamilyInfoState> CreatureFamilies { get; } =
        new Dictionary<int, WowCreatureFamilyInfoState>();

    public IDictionary<int, WowCreatureTypeInfoState> CreatureTypes { get; } =
        new Dictionary<int, WowCreatureTypeInfoState>();

    public IDictionary<int, WowFactionInfoState> FactionsByRaceId { get; } =
        new Dictionary<int, WowFactionInfoState>();

    public IDictionary<int, WowRaceInfoState> Races { get; } =
        new Dictionary<int, WowRaceInfoState>();
}
