using System.Collections;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactSpellCatalog : TactCatalog, IWowSpellProvider
{
    private const int CacheVersion = 1;
    private readonly IReadOnlyDictionary<int, WowSpellStaticInfo> _spells;
    private readonly IReadOnlyDictionary<string, int> _idsByName;

    private TactSpellCatalog(IReadOnlyDictionary<int, WowSpellStaticInfo> spells)
    {
        _spells = spells;
        _idsByName = spells.Values
            .GroupBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Min(value => value.Id),
                StringComparer.OrdinalIgnoreCase);
    }

    public int Count => _spells.Count;

    public WowSpellStaticInfo? Find(int id) =>
        _spells.GetValueOrDefault(id);

    public int FindIdByName(string name) =>
        _idsByName.GetValueOrDefault(name);

    public static TactSpellCatalog Load(
        TactAssetSource tact,
        string build,
        string? cacheDirectory = null)
    {
        var cacheIdentity = tact.CatalogCacheIdentity(build);
        if (TactCatalogCache.TryRead(
                cacheDirectory,
                cacheIdentity,
                "spells",
                CacheVersion,
                ReadCache,
                out TactSpellCatalog? cached))
        {
            return cached!;
        }

        var database = tact.Database;
        var castTimes = database.Load("SpellCastTimes", build).Values
            .ToDictionary(row => Integer(row, "ID"));
        var ranges = database.Load("SpellRange", build).Values
            .ToDictionary(row => Integer(row, "ID"));
        var miscBySpell = database.Load("SpellMisc", build).Values
            .Where(row => Integer(row, "DifficultyID") == 0)
            .GroupBy(row => Integer(row, "SpellID"))
            .ToDictionary(group => group.Key, group => group.First());

        var spells = new Dictionary<int, WowSpellStaticInfo>();
        foreach (var row in database.Load("SpellName", build).Values)
        {
            var id = Integer(row, "ID");
            var name = Text(row, "Name_lang", "Name");
            if (id <= 0 || string.IsNullOrWhiteSpace(name))
                continue;

            var iconId = 0;
            var castTime = 0;
            var minRange = 0f;
            var maxRange = 0f;
            if (miscBySpell.TryGetValue(id, out var misc))
            {
                iconId = Integer(misc, "SpellIconFileDataID");
                if (castTimes.TryGetValue(Integer(misc, "CastingTimeIndex"), out var cast))
                    castTime = Integer(cast, "Base");
                if (ranges.TryGetValue(Integer(misc, "RangeIndex"), out var range))
                {
                    minRange = FirstNumber(range, "RangeMin");
                    maxRange = FirstNumber(range, "RangeMax");
                }
            }

            spells[id] = new WowSpellStaticInfo(
                id,
                name,
                iconId,
                iconId,
                castTime,
                minRange,
                maxRange);
        }

        var catalog = new TactSpellCatalog(spells);
        TactCatalogCache.Write(
            cacheDirectory,
            cacheIdentity,
            "spells",
            CacheVersion,
            writer => WriteCache(writer, catalog));
        return catalog;
    }

    private static TactSpellCatalog ReadCache(BinaryReader reader)
    {
        var count = TactCatalogCache.ReadCount(reader, 1_000_000);
        var spells = new Dictionary<int, WowSpellStaticInfo>(count);
        for (var index = 0; index < count; index++)
        {
            var id = reader.ReadInt32();
            spells.Add(
                id,
                new WowSpellStaticInfo(
                    id,
                    reader.ReadString(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadSingle(),
                    reader.ReadSingle()));
        }
        return new TactSpellCatalog(spells);
    }

    private static void WriteCache(BinaryWriter writer, TactSpellCatalog catalog)
    {
        writer.Write(catalog._spells.Count);
        foreach (var spell in catalog._spells.Values.OrderBy(value => value.Id))
        {
            writer.Write(spell.Id);
            writer.Write(spell.Name);
            writer.Write(spell.IconId);
            writer.Write(spell.OriginalIconId);
            writer.Write(spell.CastTimeMilliseconds);
            writer.Write(spell.MinRange);
            writer.Write(spell.MaxRange);
        }
    }

    private static float FirstNumber(dynamic row, string name)
    {
        var value = Field(row, name);
        if (value is IEnumerable sequence and not string)
        {
            foreach (var item in sequence)
                return Convert.ToSingle(item ?? 0);
        }

        foreach (var candidate in new[] { $"{name}[0]", $"{name}_0", $"{name}0" })
        {
            value = Field(row, candidate);
            if (value is not null)
                return Convert.ToSingle(value);
        }

        return 0;
    }
}
