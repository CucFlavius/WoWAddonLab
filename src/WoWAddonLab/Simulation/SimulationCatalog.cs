namespace WoWAddonLab.Simulation;

internal sealed record SimulationRace(string File, string Name, int Id, string Faction);

internal sealed record SimulationPowerType(int Id, string Token, float Red, float Green, float Blue);

internal static class SimulationCatalog
{
    public static readonly string[] ClassFiles =
    [
        "WARRIOR", "PALADIN", "HUNTER", "ROGUE", "PRIEST", "DEATHKNIGHT", "SHAMAN",
        "MAGE", "WARLOCK", "MONK", "DRUID", "DEMONHUNTER", "EVOKER"
    ];

    public static readonly string[] ClassNames =
    [
        "Warrior", "Paladin", "Hunter", "Rogue", "Priest", "Death Knight", "Shaman",
        "Mage", "Warlock", "Monk", "Druid", "Demon Hunter", "Evoker"
    ];

    public static readonly string[] Factions = ["Alliance", "Horde", "Neutral"];

    public static readonly string[] SexNames = ["Unknown", "Male", "Female"];

    public static readonly string[] Classifications =
    [
        "normal", "elite", "rareelite", "rare", "worldboss", "minus", "trivial"
    ];

    public static readonly SimulationRace[] Races =
    [
        new("Human", "Human", 1, "Alliance"),
        new("Dwarf", "Dwarf", 3, "Alliance"),
        new("NightElf", "Night Elf", 4, "Alliance"),
        new("Gnome", "Gnome", 7, "Alliance"),
        new("Draenei", "Draenei", 11, "Alliance"),
        new("Worgen", "Worgen", 22, "Alliance"),
        new("VoidElf", "Void Elf", 29, "Alliance"),
        new("LightforgedDraenei", "Lightforged Draenei", 30, "Alliance"),
        new("DarkIronDwarf", "Dark Iron Dwarf", 34, "Alliance"),
        new("KulTiran", "Kul Tiran", 32, "Alliance"),
        new("Mechagnome", "Mechagnome", 37, "Alliance"),
        new("Orc", "Orc", 2, "Horde"),
        new("Scourge", "Undead", 5, "Horde"),
        new("Tauren", "Tauren", 6, "Horde"),
        new("Troll", "Troll", 8, "Horde"),
        new("BloodElf", "Blood Elf", 10, "Horde"),
        new("Goblin", "Goblin", 9, "Horde"),
        new("Nightborne", "Nightborne", 27, "Horde"),
        new("HighmountainTauren", "Highmountain Tauren", 28, "Horde"),
        new("MagharOrc", "Mag'har Orc", 36, "Horde"),
        new("ZandalariTroll", "Zandalari Troll", 31, "Horde"),
        new("Vulpera", "Vulpera", 35, "Horde"),
        new("Pandaren", "Pandaren", 24, "Neutral"),
        new("Dracthyr", "Dracthyr", 52, "Neutral"),
        new("Earthen", "Earthen", 85, "Neutral")
    ];

    private static readonly SimulationPowerType Mana = new(0, "MANA", 0f, 0f, 1f);
    private static readonly SimulationPowerType Rage = new(1, "RAGE", 1f, 0f, 0f);
    private static readonly SimulationPowerType Focus = new(2, "FOCUS", 1f, 0.5f, 0.25f);
    private static readonly SimulationPowerType Energy = new(3, "ENERGY", 1f, 1f, 0f);
    private static readonly SimulationPowerType RunicPower = new(6, "RUNIC_POWER", 0f, 0.82f, 1f);
    private static readonly SimulationPowerType Fury = new(17, "FURY", 0.788f, 0.259f, 0.992f);

    public static SimulationPowerType PowerTypeForClass(string classFile) => classFile switch
    {
        "WARRIOR" => Rage,
        "ROGUE" => Energy,
        "MONK" => Energy,
        "HUNTER" => Focus,
        "DEATHKNIGHT" => RunicPower,
        "DEMONHUNTER" => Fury,
        _ => Mana
    };

    public static string ClassDisplayName(string classFile)
    {
        var index = Array.IndexOf(ClassFiles, classFile);
        return index < 0 ? classFile : ClassNames[index];
    }

    public static SimulationRace? FindRace(string? raceFile) =>
        raceFile is null
            ? null
            : Races.FirstOrDefault(race =>
                race.File.Equals(raceFile, StringComparison.OrdinalIgnoreCase));
}
