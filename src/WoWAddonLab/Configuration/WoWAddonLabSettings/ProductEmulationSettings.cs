using System.Text.Json;

namespace WoWAddonLab.Configuration;

public sealed class ProductEmulationSettings
{
    public HashSet<string> EnabledAddons { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DisabledBlizzardModules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string ProfileId { get; set; } = "clean";
    public bool LoadBlizzardUi { get; set; } = true;
    public bool? UseUiScale { get; set; }
    public float? UiScale { get; set; }
    public float? UiScaleMultiplier { get; set; }
}
