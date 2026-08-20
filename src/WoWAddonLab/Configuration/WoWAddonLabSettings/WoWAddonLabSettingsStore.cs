using System.Text.Json;

namespace WoWAddonLab.Configuration;

public sealed class WoWAddonLabSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public WoWAddonLabSettingsStore()
    {
        DirectoryPath = WoWAddonLabPaths.UserDataDirectory;
        FilePath = Path.Combine(DirectoryPath, "settings.json");
    }

    public string DirectoryPath { get; }
    public string FilePath { get; }

    public WoWAddonLabSettings Load()
    {
        if (!File.Exists(FilePath))
            return new WoWAddonLabSettings();
        try
        {
            var deserialized =
                JsonSerializer.Deserialize<WoWAddonLabSettings>(File.ReadAllText(FilePath), JsonOptions)
                ?? new WoWAddonLabSettings();
            var normalized = new Dictionary<string, ProductEmulationSettings>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in deserialized.Products)
            {
                value.EnabledAddons = new HashSet<string>(
                    value.EnabledAddons ?? [],
                    StringComparer.OrdinalIgnoreCase);
                value.DisabledBlizzardModules = new HashSet<string>(
                    value.DisabledBlizzardModules ?? [],
                    StringComparer.OrdinalIgnoreCase);
                normalized[key] = value;
            }
            deserialized.Products = normalized;
            return deserialized;
        }
        catch
        {
            return new WoWAddonLabSettings();
        }
    }

    public void Save(WoWAddonLabSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporary, FilePath, true);
    }
}
