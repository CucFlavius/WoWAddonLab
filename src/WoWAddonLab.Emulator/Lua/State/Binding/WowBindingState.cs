namespace WoWAddonLab.Emulator.Lua;

public sealed class WowBindingState
{
    private readonly Dictionary<string, List<string>> _keys =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _modifiedClicks =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CHATLINK"] = "SHIFT",
            ["COMPAREITEMS"] = "SHIFT",
            ["DRESSUP"] = "CTRL",
            ["FOCUSCAST"] = "NONE",
            ["MOUSEOVERCAST"] = "NONE",
            ["PICKUPACTION"] = "SHIFT",
            ["QUESTWATCHTOGGLE"] = "SHIFT",
            ["SELFCAST"] = "ALT",
            ["SPLITSTACK"] = "SHIFT"
        };
    private readonly Dictionary<string, string> _categories =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _contexts =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int?> _customBindingTypes =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _searchTags =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _scripts =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _runOnUp =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<WowOverrideBinding> _overrides = [];
    private long _nextOverrideSequence;

    public IReadOnlyDictionary<string, List<string>> All => _keys;
    public int CurrentSet { get; set; } = 1;
    public ISet<int> ActiveContexts { get; } = new HashSet<int>();
    public int TurnStrafeStyle { get; set; }

    public string GetModifiedClick(string? action) =>
        action is not null && _modifiedClicks.TryGetValue(action, out var modifier)
            ? modifier
            : "NONE";

    public bool SetModifiedClick(string? action, string? modifier)
    {
        if (string.IsNullOrWhiteSpace(action) ||
            !_modifiedClicks.ContainsKey(action) ||
            !TryNormalizeModifier(modifier, out var normalized))
        {
            return false;
        }

        _modifiedClicks[action] = normalized;
        return true;
    }

    public bool HasModifiedClickAction(string? action) =>
        action is not null && _modifiedClicks.ContainsKey(action);

    private static bool TryNormalizeModifier(string? modifier, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(modifier))
        {
            normalized = "NONE";
            return true;
        }

        normalized = modifier.Trim().ToUpperInvariant();
        return normalized is "ALT" or "CTRL" or "SHIFT" or "NONE";
    }

    public IReadOnlyList<string> GetKeys(string? command) =>
        command is not null && _keys.TryGetValue(command, out var keys)
            ? keys
            : [];

    public void SetKeys(string command, params string[] keys)
    {
        _keys[command] = keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToList();
    }

    public void AddKey(string command, string key)
    {
        if (string.IsNullOrWhiteSpace(command) || string.IsNullOrWhiteSpace(key))
            return;
        if (!_keys.TryGetValue(command, out var keys))
        {
            keys = [];
            _keys[command] = keys;
        }
        if (!keys.Contains(key, StringComparer.OrdinalIgnoreCase))
            keys.Add(key);
    }

    public void Register(
        string command,
        string category,
        string script,
        bool runOnUp,
        int context,
        int? customBindingType,
        IEnumerable<string> searchTags)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;
        if (!_keys.ContainsKey(command))
            _keys[command] = [];
        _categories[command] = category;
        _contexts[command] = context;
        _customBindingTypes[command] = customBindingType;
        _searchTags[command] = searchTags.ToList();
        _scripts[command] = script;
        if (runOnUp)
            _runOnUp.Add(command);
        else
            _runOnUp.Remove(command);
    }

    public string? GetScript(string command) =>
        _scripts.GetValueOrDefault(command);

    public bool RunsOnUp(string command) => _runOnUp.Contains(command);

    public bool SetBinding(string key, string? command, int? context = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        foreach (var keys in _keys.Values)
            keys.RemoveAll(candidate => candidate.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(command))
            return true;

        if (!_keys.TryGetValue(command, out var commandKeys))
        {
            commandKeys = [];
            _keys[command] = commandKeys;
        }
        if (!commandKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            commandKeys.Add(key);
        if (context is >= 0 and <= 9)
            _contexts[command] = context.Value;
        return true;
    }

    public string GetAction(string? key, int? context = null) =>
        string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : _keys.FirstOrDefault(pair =>
                    pair.Value.Contains(key, StringComparer.OrdinalIgnoreCase) &&
                    (context is null || GetContext(pair.Key) == context))
                .Key ?? string.Empty;

    public string GetEffectiveAction(string? key, int? context = null) =>
        GetOverrideAction(key) ?? GetAction(key, context);

    public string? GetOverrideAction(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : _overrides
                .Where(value => value.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(value => value.Priority)
                .ThenByDescending(value => value.Sequence)
                .Select(value => value.Action)
                .FirstOrDefault();

    public void SetOverrideBinding(
        int ownerId,
        bool priority,
        string key,
        string? action)
    {
        _overrides.RemoveAll(value =>
            value.OwnerId == ownerId &&
            value.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(action))
            return;
        _overrides.Add(new WowOverrideBinding(
            ownerId,
            key,
            action,
            priority,
            ++_nextOverrideSequence));
    }

    public void ClearOverrideBindings(int ownerId) =>
        _overrides.RemoveAll(value => value.OwnerId == ownerId);

    public string GetCategory(string command) =>
        _categories.GetValueOrDefault(command) ?? string.Empty;

    public void SetCategory(string command, string category) =>
        _categories[command] = category;

    public int GetContext(string command) =>
        _contexts.GetValueOrDefault(command);

    public void SetContext(string command, int context)
    {
        if (context is >= 0 and <= 9)
            _contexts[command] = context;
    }

    public int? GetBindingIndex(string? command)
    {
        if (command is null)
            return null;

        var index = 1;
        foreach (var candidate in _keys.Keys)
        {
            if (candidate.Equals(command, StringComparison.OrdinalIgnoreCase))
                return index;
            index++;
        }
        return null;
    }

    public int? GetCustomBindingType(int oneBasedIndex)
    {
        if (oneBasedIndex < 1 || oneBasedIndex > _keys.Count)
            return null;

        var command = _keys.Keys.ElementAt(oneBasedIndex - 1);
        return _customBindingTypes.GetValueOrDefault(command);
    }

    public void SetCustomBindingType(string command, int? customBindingType) =>
        _customBindingTypes[command] = customBindingType;

    public IReadOnlyList<string>? GetSearchTags(string command) =>
        _searchTags.TryGetValue(command, out var tags) ? tags : null;

    public void SetSearchTags(string command, params string[] tags) =>
        _searchTags[command] = tags.ToList();
}
