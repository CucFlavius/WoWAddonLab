using System.Text.Json;

namespace WoWAddonLab.Configuration;

public sealed class WoWAddonLabSettings
{
    public string? SelectedProductPath { get; set; }
    public float ToolPanelWidth { get; set; } = 440;
    public Dictionary<string, ProductEmulationSettings> Products { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
