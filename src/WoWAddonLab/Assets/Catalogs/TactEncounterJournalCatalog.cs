using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactEncounterJournalCatalog : TactCatalog, IWowEncounterJournalProvider
{
    private readonly IReadOnlyDictionary<int, WowEncounterJournalInstance> _instances;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<OrderedInstance>> _instancesByTier;

    private TactEncounterJournalCatalog(
        IReadOnlyList<WowEncounterJournalTier> tiers,
        IReadOnlyDictionary<int, WowEncounterJournalInstance> instances,
        IReadOnlyDictionary<int, IReadOnlyList<OrderedInstance>> instancesByTier)
    {
        Tiers = tiers;
        _instances = instances;
        _instancesByTier = instancesByTier;
    }

    public IReadOnlyList<WowEncounterJournalTier> Tiers { get; }
    public int InstanceCount => _instances.Count;
    public int TierRelationCount => _instancesByTier.Values.Sum(value => value.Count);
    public int DungeonCount => _instances.Values.Count(value => value.IsDungeon);
    public int RaidCount => _instances.Values.Count(value => value.IsRaid);

    public IReadOnlyList<WowEncounterJournalInstance> GetInstances(int tierId, bool raid) =>
        _instancesByTier.TryGetValue(tierId, out var entries)
            ? entries
                .Select(value => value.Instance)
                .Where(value => raid ? value.IsRaid : value.IsDungeon)
                .ToArray()
            : [];

    public bool TryGetInstance(int instanceId, out WowEncounterJournalInstance instance) =>
        _instances.TryGetValue(instanceId, out instance!);

    public static TactEncounterJournalCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var tiers = database.Load("JournalTier", build).Values
            .Select(row => new WowEncounterJournalTier(
                Integer(row, "ID"),
                Text(row, "Name_lang")))
            .OrderBy(value => value.Id)
            .ToArray();
        var mapInstanceTypes = database.Load("Map", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => Integer(row, "InstanceType"));
        var instanceRows = database.Load("JournalInstance", build).Values.ToArray();
        var instances = instanceRows
            .Select(row =>
            {
                var mapId = Integer(row, "MapID");
                var instanceType = mapInstanceTypes.GetValueOrDefault(mapId);
                return new WowEncounterJournalInstance(
                    Integer(row, "ID"),
                    Text(row, "Name_lang"),
                    Text(row, "Description_lang"),
                    mapId,
                    Unsigned(row, "BackgroundFileDataID"),
                    Unsigned(row, "ButtonFileDataID"),
                    Unsigned(row, "ButtonSmallFileDataID"),
                    Unsigned(row, "LoreFileDataID"),
                    Integer(row, "AreaID"),
                    Integer(row, "CovenantID"),
                    instanceType == 1,
                    instanceType == 2);
            })
            .ToDictionary(value => value.Id);
        var instancesByTier = database.Load("JournalTierXInstance", build).Values
            .Select(row => new
            {
                TierId = Integer(row, "JournalTierID"),
                InstanceId = Integer(row, "JournalInstanceID"),
                Order = Integer(row, "OrderIndex")
            })
            .Where(value => instances.ContainsKey(value.InstanceId))
            .GroupBy(value => value.TierId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<OrderedInstance>)group
                    .GroupBy(value => value.InstanceId)
                    .Select(value => value.OrderBy(entry => entry.Order).First())
                    .Select(value => new OrderedInstance(
                        value.Order,
                        instances[value.InstanceId]))
                    .OrderBy(value => value.Order)
                    .ThenBy(value => value.Instance.Id)
                    .ToArray());
        return new TactEncounterJournalCatalog(tiers, instances, instancesByTier);
    }





    private sealed record OrderedInstance(int Order, WowEncounterJournalInstance Instance);
}
