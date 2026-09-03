namespace WoWAddonLab.Emulator.Lua;

public sealed class WowUnitStateCollection
{
    private readonly Dictionary<string, WowUnitState> _units =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WowUnitState> _aliases =
        new(StringComparer.OrdinalIgnoreCase);

    public WowUnitStateCollection()
    {
        var player = new WowUnitState
        {
            Token = "player",
            Guid = "Player-0-00000001",
            IsPlayer = true,
            IsHumanPlayer = true,
            IsPlayerControlled = true,
            IsInRange = true,
            IsModelReadyForUi = true,
            AvailableRoles = new WowUnitAvailableRolesState(false, false, true)
        };
        player.PowerTypes.Add(0, new WowUnitPowerTypeState(1, "RAGE", 1, 0, 0));
        _units.Add(player.Token, player);
    }

    public IReadOnlyDictionary<string, WowUnitState> All => _units;
    public WowUnitState Player => _units["player"];
    public bool PlayerAvailable { get; set; } = true;

    public WowUnitState? Find(string? token)
    {
        if (token is null)
            return null;

        var unit = _aliases.GetValueOrDefault(token) ?? _units.GetValueOrDefault(token);
        return !PlayerAvailable && ReferenceEquals(unit, Player) ? null : unit;
    }

    public WowUnitState? FindByGuid(string? guid)
    {
        if (string.IsNullOrEmpty(guid))
            return null;

        var unit = _units.Values.FirstOrDefault(
            candidate => candidate.Guid.Equals(guid, StringComparison.OrdinalIgnoreCase));
        return !PlayerAvailable && ReferenceEquals(unit, Player) ? null : unit;
    }

    public WowUnitState? ResolveTarget(string? name, bool exactMatch)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (Find(name) is { } tokenMatch)
            return tokenMatch;

        var exactNameMatch = _units.Values.FirstOrDefault(
            unit =>
                unit.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                $"{unit.Name}-{unit.Realm}".Equals(name, StringComparison.OrdinalIgnoreCase));
        if (exactNameMatch is not null || exactMatch)
            return exactNameMatch;

        return _units.Values.FirstOrDefault(
            unit =>
                unit.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase) ||
                $"{unit.Name}-{unit.Realm}".StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    public void AssignAlias(string alias, WowUnitState unit) => _aliases[alias] = unit;

    public void ClearAlias(string alias) => _aliases.Remove(alias);

    public void Add(WowUnitState unit)
    {
        if (unit.Token.Equals("player", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The player unit cannot be replaced.", nameof(unit));
        _units[unit.Token] = unit;
    }

    public bool Remove(string token)
    {
        if (token.Equals("player", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The player unit cannot be removed.", nameof(token));
        if (!_units.Remove(token, out var removed))
            return false;

        foreach (var alias in _aliases
                     .Where(entry => ReferenceEquals(entry.Value, removed))
                     .Select(entry => entry.Key)
                     .ToArray())
        {
            _aliases.Remove(alias);
        }
        return true;
    }

    public IReadOnlyDictionary<string, WowUnitState> Aliases => _aliases;
}
