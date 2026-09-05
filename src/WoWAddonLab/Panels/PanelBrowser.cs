using System.Numerics;
using ImGuiNET;
using WoWAddonLab.Emulator;

namespace WoWAddonLab.Panels;

public sealed class PanelBrowser
{
    private const double RefreshSeconds = 1;

    private const string PanelQuery =
        """
        local names = {}
        for name in pairs(UIPanelWindows) do names[#names + 1] = name end
        table.sort(names)
        local out = {}
        for _, name in ipairs(names) do
            out[#out + 1] = name .. "|" .. (_G[name] and "1" or "0")
        end
        return table.concat(out, "\n")
        """;

    private const string AddonQuery =
        """
        local out = {}
        for index = 1, C_AddOns.GetNumAddOns() do
            local name = C_AddOns.GetAddOnInfo(index)
            if name and name:sub(1, 9) == "Blizzard_" and C_AddOns.IsAddOnLoadOnDemand(index) then
                out[#out + 1] = name .. "|" .. (C_AddOns.IsAddOnLoaded(index) and "1" or "0")
            end
        end
        table.sort(out)
        return table.concat(out, "\n")
        """;

    private readonly List<PanelEntry> _panels = [];
    private readonly List<AddonEntry> _addons = [];
    private readonly HashSet<string> _panelNames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _shown = new(StringComparer.Ordinal);
    private EmulatorSession? _session;
    private string _filter = string.Empty;
    private string? _error;
    private double _nextRefresh;
    private bool _stale = true;

    public bool Visible;

    public Action? FocusTaken;

    public void Attach(EmulatorSession? session)
    {
        _session = session;
        _panels.Clear();
        _addons.Clear();
        _panelNames.Clear();
        _shown.Clear();
        _error = null;
        _nextRefresh = 0;
        _stale = true;
    }

    public void Render()
    {
        if (!Visible)
            return;

        ImGui.SetNextWindowSize(new Vector2(360, 620), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Windows", ref Visible))
        {
            ImGui.End();
            return;
        }

        if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            FocusTaken?.Invoke();

        if (_session is null)
        {
            ImGui.TextDisabled("No session.");
            ImGui.End();
            return;
        }

        if (_stale || ImGui.GetTime() >= _nextRefresh)
            Refresh();
        UpdateShownState();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##panel-filter", "Filter...", ref _filter, 128);
        if (_error is { } error)
            ImGui.TextColored(new Vector4(1, 0.45f, 0.4f, 1), error);

        RenderPanels();
        RenderAddons();
        ImGui.End();
    }

    private void RenderPanels()
    {
        var matches = _panels.Where(Matches).ToArray();
        if (!ImGui.CollapsingHeader($"Panels ({matches.Length})", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        foreach (var panel in matches)
        {
            if (!panel.Available)
            {
                ImGui.BeginDisabled();
                var unavailable = false;
                ImGui.Checkbox($"{panel.Name}##panel-{panel.Name}", ref unavailable);
                ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.TextDisabled("not loaded");
                continue;
            }

            var shown = _shown.GetValueOrDefault(panel.Name);
            if (ImGui.Checkbox($"{panel.Name}##panel-{panel.Name}", ref shown))
                Toggle(panel.Name, shown);
        }
    }

    private void RenderAddons()
    {
        var matches = _addons.Where(Matches).ToArray();
        var pending = matches.Count(value => !value.Loaded);
        if (!ImGui.CollapsingHeader($"Load on demand ({pending}/{matches.Length})"))
            return;

        if (pending > 0 && ImGui.Button("Load all"))
        {
            foreach (var addon in matches.Where(value => !value.Loaded))
                Evaluate($"C_AddOns.LoadAddOn(\"{addon.Name}\")");
            _stale = true;
        }

        foreach (var addon in matches)
        {
            if (addon.Loaded)
            {
                ImGui.TextDisabled(addon.Name);
                continue;
            }

            if (ImGui.SmallButton($"Load##addon-{addon.Name}"))
            {
                Evaluate($"C_AddOns.LoadAddOn(\"{addon.Name}\")");
                _stale = true;
            }
            ImGui.SameLine();
            ImGui.TextUnformatted(addon.Name);
        }
    }

    private bool Matches(PanelEntry value) => Matches(value.Name);

    private bool Matches(AddonEntry value) => Matches(value.Name);

    private bool Matches(string name) =>
        string.IsNullOrWhiteSpace(_filter) ||
        name.Contains(_filter, StringComparison.OrdinalIgnoreCase);

    private void Toggle(string name, bool show)
    {
        var action = show ? "ShowUIPanel" : "HideUIPanel";
        Evaluate($"local frame = _G[\"{name}\"] if frame then {action}(frame) end return true");
        _stale = true;
    }

    private void Refresh()
    {
        _nextRefresh = ImGui.GetTime() + RefreshSeconds;
        _stale = false;
        if (Evaluate(PanelQuery) is { } panels)
        {
            _panels.Clear();
            _panelNames.Clear();
            foreach (var (name, flag) in Rows(panels))
            {
                _panels.Add(new PanelEntry(name, flag));
                _panelNames.Add(name);
            }
        }

        if (Evaluate(AddonQuery) is { } addons)
        {
            _addons.Clear();
            foreach (var (name, flag) in Rows(addons))
                _addons.Add(new AddonEntry(name, flag));
        }
    }

    private static IEnumerable<(string Name, bool Flag)> Rows(string payload)
    {
        foreach (var line in payload.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.LastIndexOf('|');
            yield return (line[..separator], line[(separator + 1)..] == "1");
        }
    }

    private void UpdateShownState()
    {
        _shown.Clear();
        if (_panelNames.Count == 0)
            return;
        foreach (var value in _session!.Ui.Objects.Values)
        {
            if (value.Name is { } name && _panelNames.Contains(name))
                _shown[name] = value.Shown;
        }
    }

    private string? Evaluate(string code)
    {
        try
        {
            var result = _session!.Lua.Evaluate(code);
            _error = null;
            return result;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
            return null;
        }
    }

    private readonly record struct PanelEntry(string Name, bool Available);

    private readonly record struct AddonEntry(string Name, bool Loaded);
}
