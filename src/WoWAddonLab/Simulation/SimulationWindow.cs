using System.Numerics;
using ImGuiNET;
using WoWAddonLab.Configuration;

namespace WoWAddonLab.Simulation;

public sealed class SimulationWindow
{
    private const float LabelWidth = 110;

    private readonly SimulationController _controller;
    private int _selectedUnit;

    public SimulationWindow(SimulationController controller)
    {
        _controller = controller;
    }

    public bool Visible;
    public Action? FocusTaken;

    public void Render()
    {
        if (!Visible)
            return;

        ImGui.SetNextWindowSize(new Vector2(880, 620), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Simulation", ref Visible))
        {
            ImGui.End();
            return;
        }

        if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            FocusTaken?.Invoke();

        if (ImGui.BeginTabBar("##simulation-tabs", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            if (ImGui.BeginTabItem("Player"))
            {
                RenderUnitTabs(_controller.Profile.Player, isPlayer: true);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Units"))
            {
                RenderUnitsTab();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        if (!ImGui.IsAnyItemActive())
            _controller.FlushIfDirty();
        ImGui.End();
    }

    private void RenderUnitTabs(SimulationUnitProfile profile, bool isPlayer)
    {
        if (!ImGui.BeginTable($"##columns-{profile.Id}", 2, ImGuiTableFlags.SizingStretchSame))
            return;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.SeparatorText("Identity");
        RenderIdentity(profile, isPlayer);

        ImGui.TableSetColumnIndex(1);
        ImGui.SeparatorText("Vitals");
        RenderVitals(profile, isPlayer);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.SeparatorText("Status");
        RenderStatus(profile, isPlayer);

        ImGui.TableSetColumnIndex(1);
        if (isPlayer)
        {
            ImGui.SeparatorText("Location");
            RenderLocation();
        }
        ImGui.EndTable();
    }

    private void RenderUnitsTab()
    {
        var units = _controller.Profile.Units;
        if (ImGui.Button("Add unit"))
            _selectedUnit = units.IndexOf(_controller.AddUnit());
        ImGui.SameLine();
        ImGui.BeginDisabled(units.Count == 0);
        if (ImGui.Button("Remove"))
        {
            _controller.RemoveUnit(units[Math.Clamp(_selectedUnit, 0, units.Count - 1)]);
            _selectedUnit = Math.Max(0, _selectedUnit - 1);
        }
        ImGui.EndDisabled();

        if (units.Count == 0)
        {
            ImGui.TextDisabled("No simulated units.");
            return;
        }
        _selectedUnit = Math.Clamp(_selectedUnit, 0, units.Count - 1);

        if (ImGui.BeginChild("##simulation-units", new Vector2(0, 110), ImGuiChildFlags.Border))
        {
            for (var index = 0; index < units.Count; index++)
            {
                var unit = units[index];
                if (ImGui.Selectable($"{unit.Name ?? unit.Id}   [{unit.Id}]", index == _selectedUnit))
                    _selectedUnit = index;
            }
        }
        ImGui.EndChild();

        var profile = units[_selectedUnit];
        ImGui.SeparatorText("Assign");
        RenderAlias("target", profile);
        ImGui.SameLine();
        RenderAlias("focus", profile);
        ImGui.SameLine();
        RenderAlias("mouseover", profile);

        RenderUnitTabs(profile, isPlayer: false);
    }

    private void RenderAlias(string alias, SimulationUnitProfile profile)
    {
        var current = alias switch
        {
            "target" => _controller.Profile.TargetUnitId,
            "focus" => _controller.Profile.FocusUnitId,
            _ => _controller.Profile.MouseoverUnitId
        };
        var assigned = current == profile.Id;
        if (ImGui.Checkbox(alias, ref assigned))
            _controller.SetAlias(alias, assigned ? profile.Id : null);
    }

    private static bool Field(string label)
    {
        ImGui.TableNextRow();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine(LabelWidth);
        ImGui.SetNextItemWidth(-1);
        return true;
    }

    private static void Label(string label)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine(LabelWidth);
        ImGui.SetNextItemWidth(-1);
    }

    private void RenderIdentity(SimulationUnitProfile profile, bool isPlayer)
    {
        var live = _controller.Live(profile);

        Label("Name");
        var name = profile.Name ?? live?.Name ?? string.Empty;
        if (ImGui.InputText($"##name-{profile.Id}", ref name, 64))
        {
            profile.Name = name;
            _controller.UnitChanged(profile, "UNIT_NAME_UPDATE");
        }

        Label("Realm");
        var realm = profile.Realm ?? live?.Realm ?? string.Empty;
        if (ImGui.InputText($"##realm-{profile.Id}", ref realm, 64))
        {
            profile.Realm = realm;
            _controller.UnitChanged(profile, "UNIT_NAME_UPDATE");
        }

        Label("Class");
        var classIndex = Math.Max(
            0,
            Array.IndexOf(
                SimulationCatalog.ClassFiles,
                profile.ClassFile ?? live?.ClassFile ?? "WARRIOR"));
        if (ImGui.Combo(
                $"##class-{profile.Id}",
                ref classIndex,
                SimulationCatalog.ClassNames,
                SimulationCatalog.ClassNames.Length))
        {
            profile.ClassFile = SimulationCatalog.ClassFiles[classIndex];
            profile.ClassId = classIndex + 1;
            _controller.UnitChanged(
                profile,
                "UNIT_NAME_UPDATE",
                "UNIT_DISPLAYPOWER",
                "UNIT_MAXPOWER",
                "UNIT_POWER_UPDATE",
                "PLAYER_SPECIALIZATION_CHANGED");
        }

        Label("Race");
        var raceNames = SimulationCatalog.Races.Select(race => race.Name).ToArray();
        var raceIndex = Math.Max(
            0,
            Array.FindIndex(
                SimulationCatalog.Races,
                race => race.File.Equals(
                    profile.RaceFile ?? live?.RaceFile ?? "Human",
                    StringComparison.OrdinalIgnoreCase)));
        if (ImGui.Combo($"##race-{profile.Id}", ref raceIndex, raceNames, raceNames.Length))
        {
            profile.RaceFile = SimulationCatalog.Races[raceIndex].File;
            _controller.UnitChanged(profile, "UNIT_NAME_UPDATE", "UNIT_PORTRAIT_UPDATE");
        }

        Label("Faction");
        var factionIndex = Math.Max(
            0,
            Array.IndexOf(
                SimulationCatalog.Factions,
                profile.FactionGroupTag ?? live?.FactionGroupTag ?? "Alliance"));
        if (ImGui.Combo(
                $"##faction-{profile.Id}",
                ref factionIndex,
                SimulationCatalog.Factions,
                SimulationCatalog.Factions.Length))
        {
            profile.FactionGroupTag = SimulationCatalog.Factions[factionIndex];
            _controller.UnitChanged(profile, "UNIT_FACTION");
        }

        Label("Sex");
        var sexIndex = Math.Clamp((profile.Sex ?? live?.Sex ?? 2) - 1, 0, 2);
        if (ImGui.Combo(
                $"##sex-{profile.Id}",
                ref sexIndex,
                SimulationCatalog.SexNames,
                SimulationCatalog.SexNames.Length))
        {
            profile.Sex = sexIndex + 1;
            _controller.UnitChanged(profile, "UNIT_PORTRAIT_UPDATE");
        }

        if (isPlayer)
            return;
        Label("Classification");
        var classification = Math.Max(
            0,
            Array.IndexOf(
                SimulationCatalog.Classifications,
                profile.Classification ?? live?.Classification ?? "normal"));
        if (ImGui.Combo(
                $"##classification-{profile.Id}",
                ref classification,
                SimulationCatalog.Classifications,
                SimulationCatalog.Classifications.Length))
        {
            profile.Classification = SimulationCatalog.Classifications[classification];
            _controller.UnitChanged(profile, "UNIT_CLASSIFICATION_CHANGED");
        }
    }

    private void RenderVitals(SimulationUnitProfile profile, bool isPlayer)
    {
        var live = _controller.Live(profile);

        Label("Level");
        var level = profile.Level ?? live?.Level ?? 1;
        if (ImGui.InputInt($"##level-{profile.Id}", ref level))
        {
            profile.Level = Math.Clamp(level, 1, 200);
            _controller.UnitChanged(
                profile,
                isPlayer ? "PLAYER_LEVEL_UP" : "UNIT_LEVEL",
                "UNIT_LEVEL");
        }

        Label("Max health");
        var maximumHealth = (int)(profile.MaximumHealth ?? live?.MaximumHealth ?? 100);
        if (ImGui.InputInt($"##maxhp-{profile.Id}", ref maximumHealth))
        {
            profile.MaximumHealth = Math.Max(1, maximumHealth);
            _controller.UnitChanged(profile, "UNIT_MAXHEALTH", "UNIT_HEALTH");
        }

        Label("Health");
        var health = (int)(profile.Health ?? live?.Health ?? 100);
        if (ImGui.SliderInt($"##hp-{profile.Id}", ref health, 0, Math.Max(1, maximumHealth)))
        {
            profile.Health = health;
            _controller.UnitChanged(profile, "UNIT_HEALTH");
        }

        Label("Max power");
        var maximumPower = (int)(profile.MaximumPower ?? live?.MaximumPower ?? 100);
        if (ImGui.InputInt($"##maxpower-{profile.Id}", ref maximumPower))
        {
            profile.MaximumPower = Math.Max(0, maximumPower);
            _controller.UnitChanged(profile, "UNIT_MAXPOWER", "UNIT_POWER_UPDATE");
        }

        Label("Power");
        var power = (int)(profile.Power ?? live?.Power ?? 100);
        if (ImGui.SliderInt($"##power-{profile.Id}", ref power, 0, Math.Max(1, maximumPower)))
        {
            profile.Power = power;
            _controller.UnitChanged(profile, "UNIT_POWER_UPDATE");
        }
        ImGui.TextDisabled($"Power type follows class: {live?.PowerTypes.GetValueOrDefault(0)?.Token ?? "?"}");

        if (!isPlayer)
            return;
        Label("Max XP");
        var maximumExperience = (int)(profile.MaximumExperience ?? live?.MaximumExperience ?? 1000);
        if (ImGui.InputInt($"##maxxp-{profile.Id}", ref maximumExperience))
        {
            profile.MaximumExperience = Math.Max(1, maximumExperience);
            _controller.UnitChanged(profile, "PLAYER_XP_UPDATE");
        }

        Label("XP");
        var experience = (int)(profile.Experience ?? live?.Experience ?? 0);
        if (ImGui.SliderInt($"##xp-{profile.Id}", ref experience, 0, Math.Max(1, maximumExperience)))
        {
            profile.Experience = experience;
            _controller.UnitChanged(profile, "PLAYER_XP_UPDATE");
        }
    }

    private void RenderStatus(SimulationUnitProfile profile, bool isPlayer)
    {
        var live = _controller.Live(profile);

        var dead = profile.IsDead ?? live?.IsDead ?? false;
        if (ImGui.Checkbox($"Dead##{profile.Id}", ref dead))
        {
            profile.IsDead = dead;
            _controller.UnitChanged(profile, "UNIT_HEALTH", "PLAYER_DEAD");
        }
        ImGui.SameLine();
        var ghost = profile.IsGhost ?? live?.IsGhost ?? false;
        if (ImGui.Checkbox($"Ghost##{profile.Id}", ref ghost))
        {
            profile.IsGhost = ghost;
            _controller.UnitChanged(profile, "PLAYER_ALIVE");
        }

        var combat = profile.IsAffectingCombat ?? live?.IsAffectingCombat ?? false;
        if (ImGui.Checkbox($"In combat##{profile.Id}", ref combat))
        {
            profile.IsAffectingCombat = combat;
            _controller.UnitChanged(
                profile,
                combat ? "PLAYER_REGEN_DISABLED" : "PLAYER_REGEN_ENABLED");
        }
        ImGui.SameLine();
        var mounted = profile.IsMounted ?? live?.IsMounted ?? false;
        if (ImGui.Checkbox($"Mounted##{profile.Id}", ref mounted))
        {
            profile.IsMounted = mounted;
            _controller.UnitChanged(profile, "UNIT_AURA");
        }

        var pvp = profile.IsPvp ?? live?.IsPvp ?? false;
        if (ImGui.Checkbox($"PvP##{profile.Id}", ref pvp))
        {
            profile.IsPvp = pvp;
            _controller.UnitChanged(profile, "UNIT_FACTION");
        }

        if (isPlayer)
        {
            ImGui.SameLine();
            var simulation = _controller.Profile;
            var resting = simulation.IsResting ?? _controller.LiveClient?.IsResting ?? false;
            if (ImGui.Checkbox("Resting", ref resting))
            {
                simulation.IsResting = resting;
                _controller.WorldChanged("PLAYER_UPDATE_RESTING");
            }
            return;
        }

        ImGui.SameLine();
        var boss = profile.IsBossMob ?? live?.IsBossMob ?? false;
        if (ImGui.Checkbox($"Boss##{profile.Id}", ref boss))
        {
            profile.IsBossMob = boss;
            _controller.UnitChanged(profile, "UNIT_CLASSIFICATION_CHANGED");
        }
    }

    private void RenderLocation()
    {
        var simulation = _controller.Profile;

        Label("Zone");
        var zone = simulation.ZoneText ?? _controller.LiveClient?.ZoneText ?? string.Empty;
        if (ImGui.InputText("##zone", ref zone, 128))
        {
            simulation.ZoneText = zone;
            _controller.WorldChanged("ZONE_CHANGED_NEW_AREA", "ZONE_CHANGED");
        }

        Label("Subzone");
        var subZone = simulation.SubZoneText ?? _controller.LiveClient?.SubZoneText ?? string.Empty;
        if (ImGui.InputText("##subzone", ref subZone, 128))
        {
            simulation.SubZoneText = subZone;
            _controller.WorldChanged("ZONE_CHANGED_INDOORS");
        }

        Label("Money");
        var money = (int)(simulation.Money ?? _controller.LiveClient?.Money ?? 0);
        if (ImGui.InputInt("##money", ref money))
        {
            simulation.Money = Math.Max(0, money);
            _controller.WorldChanged("PLAYER_MONEY");
        }
    }
}
