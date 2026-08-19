namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSpellState
{
    private readonly HashSet<int> _providerDefinitionIds = [];

    public IDictionary<int, WowSpellDefinition> Definitions { get; } =
        new Dictionary<int, WowSpellDefinition>();
    public ISet<int> RangeCheckedSpellIds { get; } = new HashSet<int>();
    public ISet<int> RequestedLoadSpellIds { get; } = new HashSet<int>();
    public ISet<int> KnownSpellIds { get; } = new HashSet<int>();

    public int QueueWindowMilliseconds { get; set; } = 400;
    public int? ActiveCastSpellId { get; set; }
    public int? LastCancelledSpellId { get; set; }
    public int? PickedUpSpellId { get; set; }
    public int? RangedAutoAttackSpellId { get; set; }
    public bool TargetSpellIsEnchanting { get; set; }
    public bool TargetSpellJumpsUpgradeTrack { get; set; }
    public bool TargetSpellReplacesBonusTree { get; set; }

    public WowSpellDefinition Add(int id, string name)
    {
        var definition = new WowSpellDefinition(id, name);
        Definitions[id] = definition;
        _providerDefinitionIds.Remove(id);
        return definition;
    }

    public WowSpellDefinition? Find(int id)
    {
        if (Definitions.TryGetValue(id, out var definition))
            return definition;

        var info = Provider?.Find(id);
        if (info is null)
            return null;

        definition = new WowSpellDefinition(info.Id, info.Name)
        {
            IconId = info.IconId,
            OriginalIconId = info.OriginalIconId,
            CastTimeMilliseconds = info.CastTimeMilliseconds,
            MinRange = info.MinRange,
            MaxRange = info.MaxRange,
            HasRange = info.MaxRange > 0
        };
        Definitions[id] = definition;
        _providerDefinitionIds.Add(id);
        return definition;
    }

    public int FindIdByName(string name)
    {
        foreach (var definition in Definitions.Values)
        {
            if (definition.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return definition.Id;
        }

        return Provider?.FindIdByName(name) ?? 0;
    }

    internal IWowSpellProvider? Provider { get; private set; }

    internal void SetProvider(IWowSpellProvider? provider)
    {
        foreach (var id in _providerDefinitionIds)
            Definitions.Remove(id);
        _providerDefinitionIds.Clear();
        Provider = provider;
    }
}
