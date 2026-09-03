using WoWAddonLab.Configuration;
using WoWAddonLab.Emulator;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Simulation;

public sealed class SimulationController
{
    private readonly SimulationProfileStore _store;
    private EmulatorSession? _session;
    private SimulationProfile _profile = new();
    private string? _productPath;
    private bool _dirty;

    public SimulationController(SimulationProfileStore store)
    {
        _store = store;
    }

    public SimulationProfile Profile => _profile;

    public void Attach(EmulatorSession session, string? productPath)
    {
        _session = session;
        _productPath = productPath;
        _profile = productPath is null ? new SimulationProfile() : _store.For(productPath);
        _profile.Player.Id = "player";
        ApplyAll();
    }

    public SimulationUnitProfile AddUnit()
    {
        var index = 1;
        while (_profile.Units.Any(unit => unit.Id == $"sim{index}"))
            index++;
        var profile = new SimulationUnitProfile
        {
            Id = $"sim{index}",
            Name = $"Test Dummy {index}",
            Level = 60,
            Health = 10000,
            MaximumHealth = 10000,
            Power = 1000,
            MaximumPower = 1000,
            Classification = "normal",
            FactionGroupTag = "Horde",
            IsPlayerControlled = false
        };
        _profile.Units.Add(profile);
        CreateUnit(profile);
        _dirty = true;
        return profile;
    }

    public void RemoveUnit(SimulationUnitProfile profile)
    {
        _profile.Units.Remove(profile);
        if (_profile.TargetUnitId == profile.Id)
            _profile.TargetUnitId = null;
        if (_profile.FocusUnitId == profile.Id)
            _profile.FocusUnitId = null;
        if (_profile.MouseoverUnitId == profile.Id)
            _profile.MouseoverUnitId = null;
        _session?.Lua.Units.Remove(profile.Id);
        ApplyAliases();
        _dirty = true;
    }

    public void UnitChanged(SimulationUnitProfile profile, params string[] events)
    {
        if (Resolve(profile) is { } unit)
        {
            ApplyUnit(profile, unit);
            foreach (var name in events)
                _session!.Lua.TriggerEvent(name, unit.Token);
        }
        _dirty = true;
    }

    public void WorldChanged(params string[] events)
    {
        ApplyWorld();
        foreach (var name in events)
            _session?.Lua.TriggerEvent(name);
        _dirty = true;
    }

    public void SetAlias(string alias, string? unitId)
    {
        switch (alias)
        {
            case "target": _profile.TargetUnitId = unitId; break;
            case "focus": _profile.FocusUnitId = unitId; break;
            case "mouseover": _profile.MouseoverUnitId = unitId; break;
        }
        ApplyAliases();
        _session?.Lua.TriggerEvent(alias switch
        {
            "target" => "PLAYER_TARGET_CHANGED",
            "focus" => "PLAYER_FOCUS_CHANGED",
            _ => "UPDATE_MOUSEOVER_UNIT"
        });
        _dirty = true;
    }

    public void FlushIfDirty()
    {
        if (!_dirty || _productPath is null)
            return;
        _dirty = false;
        _store.Save();
    }

    public WowClientState? LiveClient => _session?.Lua.Client;

    public WowUnitState? Live(SimulationUnitProfile profile) => Resolve(profile);

    private WowUnitState? Resolve(SimulationUnitProfile profile) =>
        _session is null
            ? null
            : profile.Id == "player"
                ? _session.Lua.Units.Player
                : _session.Lua.Units.All.GetValueOrDefault(profile.Id);

    private void ApplyAll()
    {
        if (_session is null)
            return;
        ApplyUnit(_profile.Player, _session.Lua.Units.Player);
        foreach (var profile in _profile.Units)
            CreateUnit(profile);
        ApplyAliases();
        ApplyWorld();
        if (!_session.Lua.IsLoaded)
            return;
        _session.Lua.TriggerEvent("UNIT_NAME_UPDATE", "player");
        _session.Lua.TriggerEvent("UNIT_LEVEL", "player");
        _session.Lua.TriggerEvent("UNIT_HEALTH", "player");
        _session.Lua.TriggerEvent("UNIT_MAXHEALTH", "player");
        _session.Lua.TriggerEvent("UNIT_DISPLAYPOWER", "player");
        _session.Lua.TriggerEvent("PLAYER_MONEY");
        _session.Lua.TriggerEvent("ZONE_CHANGED_NEW_AREA");
        _session.Lua.TriggerEvent("PLAYER_TARGET_CHANGED");
    }

    private void CreateUnit(SimulationUnitProfile profile)
    {
        if (_session is null)
            return;
        var existing = _session.Lua.Units.All.GetValueOrDefault(profile.Id);
        if (existing is null)
        {
            existing = new WowUnitState
            {
                Token = profile.Id,
                Guid = $"Creature-0-0-0-0-0-{profile.Id}",
                IsVisible = true,
                IsConnected = true,
                IsInRange = true,
                IsModelReadyForUi = true
            };
            existing.CurrentPowerTypeId = 0;
            existing.PowerTypes.Add(0, new WowUnitPowerTypeState(0, "MANA", 0, 0, 0));
            _session.Lua.Units.Add(existing);
        }
        ApplyUnit(profile, existing);
    }

    private void ApplyAliases()
    {
        if (_session is null)
            return;
        Assign("target", _profile.TargetUnitId);
        Assign("focus", _profile.FocusUnitId);
        Assign("mouseover", _profile.MouseoverUnitId);
    }

    private void Assign(string alias, string? unitId)
    {
        var units = _session!.Lua.Units;
        if (unitId is null || units.All.GetValueOrDefault(unitId) is not { } unit)
        {
            units.ClearAlias(alias);
            return;
        }
        units.AssignAlias(alias, unit);
    }

    private void ApplyWorld()
    {
        if (_session is null)
            return;
        var client = _session.Lua.Client;
        if (_profile.Money is { } money)
            client.Money = money;
        if (_profile.ZoneText is { } zone)
            client.ZoneText = zone;
        if (_profile.SubZoneText is { } subZone)
            client.SubZoneText = subZone;
        if (_profile.IsResting is { } resting)
            client.IsResting = resting;
    }

    private static void ApplyUnit(SimulationUnitProfile profile, WowUnitState unit)
    {
        if (profile.Name is { } name)
            unit.Name = name;
        if (profile.Realm is { } realm)
            unit.Realm = realm;
        if (profile.ClassFile is { } classFile)
        {
            unit.ClassFile = classFile;
            unit.ClassName = SimulationCatalog.ClassDisplayName(classFile);
            var classPower = SimulationCatalog.PowerTypeForClass(classFile);
            unit.CurrentPowerTypeId = classPower.Id;
            unit.PowerTypes[0] = new WowUnitPowerTypeState(
                classPower.Id,
                classPower.Token,
                classPower.Red,
                classPower.Green,
                classPower.Blue);
        }
        if (profile.ClassId is { } classId)
            unit.ClassId = classId;
        if (SimulationCatalog.FindRace(profile.RaceFile) is { } race)
        {
            unit.RaceFile = race.File;
            unit.RaceName = race.Name;
            unit.RaceId = race.Id;
        }
        if (profile.Sex is { } sex)
            unit.Sex = sex;
        if (profile.FactionGroupTag is { } faction)
        {
            unit.FactionGroupTag = faction;
            unit.LocalizedFactionGroup = faction;
        }
        if (profile.Level is { } level)
            unit.Level = level;
        if (profile.MaximumHealth is { } maximumHealth)
            unit.MaximumHealth = maximumHealth;
        if (profile.Health is { } health)
            unit.Health = health;
        unit.Health = Math.Clamp(unit.Health, 0, unit.MaximumHealth);
        if (profile.MaximumPower is { } maximumPower)
            unit.MaximumPower = maximumPower;
        if (profile.Power is { } power)
            unit.Power = power;
        unit.Power = Math.Clamp(unit.Power, 0, unit.MaximumPower);
        if (profile.PowerTypeId is { } powerType)
            unit.CurrentPowerTypeId = powerType;
        if (unit.PowerValues.TryGetValue(unit.CurrentPowerTypeId, out var powerValue))
        {
            unit.PowerValues[unit.CurrentPowerTypeId] = powerValue with
            {
                Current = unit.Power,
                Maximum = unit.MaximumPower
            };
        }
        if (profile.Experience is { } experience)
            unit.Experience = experience;
        if (profile.MaximumExperience is { } maximumExperience)
            unit.MaximumExperience = maximumExperience;
        if (profile.Classification is { } classification)
            unit.Classification = classification;
        if (profile.IsDead is { } isDead)
            unit.IsDead = isDead;
        if (profile.IsGhost is { } isGhost)
            unit.IsGhost = isGhost;
        if (profile.IsMounted is { } isMounted)
            unit.IsMounted = isMounted;
        if (profile.IsAffectingCombat is { } inCombat)
            unit.IsAffectingCombat = inCombat;
        if (profile.IsPvp is { } isPvp)
            unit.IsPvp = isPvp;
        if (profile.IsBossMob is { } isBoss)
            unit.IsBossMob = isBoss;
        if (profile.IsPlayerControlled is { } playerControlled)
            unit.IsPlayerControlled = playerControlled;
    }
}
