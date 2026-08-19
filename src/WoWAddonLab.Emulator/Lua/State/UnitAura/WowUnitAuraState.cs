namespace WoWAddonLab.Emulator.Lua;

public sealed class WowUnitAuraState
{
    public IDictionary<string, bool> AlteredFormByUnitToken { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    public bool PrivateAuraShowDispelType { get; set; }
    public int PrivateAuraAnchorAddedCallbackReference;
    public int PrivateAuraAnchorRemovedCallbackReference;
    public int ShowDispelTypeCallbackReference;
    public int PrivateRaidBossMessageCallbackReference;
    public int? PrivateWarningTextFrameObjectId { get; set; }
    public IDictionary<string, List<int>> PrivateAuraUpdateCallbackReferences { get; } =
        new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<int, WowPrivateAuraBindingState> PrivateAuraBindings { get; } =
        new Dictionary<int, WowPrivateAuraBindingState>();
    public IList<WowPrivateAuraAppliedSoundInfoState> PrivateAuraAppliedSounds { get; } =
        new List<WowPrivateAuraAppliedSoundInfoState>();
    private long _nextPrivateAuraAnchorId;
    private readonly Dictionary<long, WowPrivateAuraAnchorState> _privateAuraAnchors = [];
    private readonly Dictionary<string, List<WowUnitAuraInfoState>> _auras =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<long>> _blockedAuras =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<long, WowPrivateAuraAnchorState> PrivateAuraAnchors =>
        _privateAuraAnchors;
    public WowPrivateWarningTextAnchorState? PrivateWarningTextAnchor { get; set; }

    public IList<WowUnitAuraInfoState> ForUnit(string unitToken)
    {
        if (!_auras.TryGetValue(unitToken, out var auras))
        {
            auras = [];
            _auras.Add(unitToken, auras);
        }
        return auras;
    }

    public IReadOnlyList<WowUnitAuraInfoState> Find(string? unitToken) =>
        unitToken is not null && _auras.TryGetValue(unitToken, out var auras)
            ? auras
            : [];

    public IReadOnlySet<long> BlockedAuras(string unitToken) =>
        _blockedAuras.TryGetValue(unitToken, out var blocked) ? blocked : new HashSet<long>();

    public void AddBlockedAura(string unitToken, long auraInstanceId)
    {
        if (!_blockedAuras.TryGetValue(unitToken, out var blocked))
        {
            blocked = [];
            _blockedAuras.Add(unitToken, blocked);
        }
        blocked.Add(auraInstanceId);
    }

    public void ClearBlockedAuras(string unitToken) => _blockedAuras.Remove(unitToken);

    public IReadOnlyList<(int Slot, WowUnitAuraInfoState Aura)> Filter(
        string? unitToken,
        string? filter)
    {
        var source = Find(unitToken);
        var result = new List<(int Slot, WowUnitAuraInfoState Aura)>();
        for (var index = 0; index < source.Count; index++)
        {
            if (MatchesAura(source[index], filter))
                result.Add((index + 1, source[index]));
        }
        return result;
    }

    public WowPrivateAuraAnchorState AddPrivateAuraAnchor(
        string unitToken,
        uint auraIndex,
        int parentId,
        bool showCountdownFrame,
        bool showCountdownNumbers,
        bool isContainer,
        WowAuraAnchorPointState? iconAnchor,
        double? iconWidth,
        double? iconHeight,
        double? borderScale,
        WowAuraAnchorPointState? durationAnchor)
    {
        var id = ++_nextPrivateAuraAnchorId;
        var anchor = new WowPrivateAuraAnchorState(
            id,
            unitToken,
            auraIndex,
            parentId,
            showCountdownFrame,
            showCountdownNumbers,
            isContainer,
            iconAnchor,
            iconWidth,
            iconHeight,
            borderScale,
            durationAnchor);
        _privateAuraAnchors.Add(id, anchor);
        return anchor;
    }

    public bool RemovePrivateAuraAnchor(long id) => _privateAuraAnchors.Remove(id);

    public static bool MatchesAura(WowUnitAuraInfoState aura, string? filter)
    {
        var components = string.IsNullOrWhiteSpace(filter)
            ? []
            : filter.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (components.Length == 0)
            return false;

        var includesNameplateOnly = components.Any(component =>
            component.Equals(
                "INCLUDE_NAME_PLATE_ONLY",
                StringComparison.OrdinalIgnoreCase));
        if (includesNameplateOnly ? !aura.IsNameplateOnly : aura.IsNameplateOnly)
            return false;

        string? category = null;
        string? cancelability = null;
        foreach (var component in components)
        {
            if (component.Equals("HELPFUL", StringComparison.OrdinalIgnoreCase))
                category = "HELPFUL";
            else if (component.Equals("HARMFUL", StringComparison.OrdinalIgnoreCase))
                category = "HARMFUL";
            else if (component.Equals("CANCELABLE", StringComparison.OrdinalIgnoreCase))
                cancelability = "CANCELABLE";
            else if (component.Equals("NOT_CANCELABLE", StringComparison.OrdinalIgnoreCase))
                cancelability = "NOT_CANCELABLE";
        }
        if (category is null ||
            (aura.IsHarmful
                ? !category.Equals("HARMFUL", StringComparison.Ordinal)
                : !category.Equals("HELPFUL", StringComparison.Ordinal)))
            return false;
        if (cancelability is not null &&
            (aura.IsCancelable
                ? !cancelability.Equals("CANCELABLE", StringComparison.Ordinal)
                : !cancelability.Equals("NOT_CANCELABLE", StringComparison.Ordinal)))
            return false;

        foreach (var component in components)
        {
            if (component.Equals("HELPFUL", StringComparison.OrdinalIgnoreCase) ||
                component.Equals("HARMFUL", StringComparison.OrdinalIgnoreCase) ||
                component.Equals("CANCELABLE", StringComparison.OrdinalIgnoreCase) ||
                component.Equals("NOT_CANCELABLE", StringComparison.OrdinalIgnoreCase))
                continue;
            var matched = component.ToUpperInvariant() switch
            {
                "PLAYER" => aura.IsFromPlayerOrPlayerPet,
                "RAID" => aura.IsRaid,
                "MAW" => aura.IsMawAura,
                "EXTERNAL_DEFENSIVE" => aura.IsTankRoleAura,
                "BIG_DEFENSIVE" => aura.IsTankRoleAura,
                "INCLUDE_NAME_PLATE_ONLY" => true,
                _ => true
            };
            if (!matched)
                return false;
        }
        return true;
    }
}
