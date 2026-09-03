using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoWAddonLab.Configuration;

public sealed class SimulationProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly Dictionary<string, SimulationProfile> _products;

    public SimulationProfileStore()
    {
        DirectoryPath = WoWAddonLabPaths.UserDataDirectory;
        FilePath = Path.Combine(DirectoryPath, "simulation.json");
        _products = Load();
    }

    public string DirectoryPath { get; }
    public string FilePath { get; }

    public SimulationProfile For(string productPath)
    {
        var key = Path.GetFullPath(productPath);
        if (!_products.TryGetValue(key, out var profile))
        {
            profile = new SimulationProfile();
            _products[key] = profile;
        }
        return profile;
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(
            temporary,
            JsonSerializer.Serialize(new SimulationProfileFile(_products), JsonOptions));
        File.Move(temporary, FilePath, true);
    }

    private Dictionary<string, SimulationProfile> Load()
    {
        var products = new Dictionary<string, SimulationProfile>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(FilePath))
            return products;

        var file = JsonSerializer.Deserialize<SimulationProfileFile>(
            File.ReadAllText(FilePath),
            JsonOptions);
        foreach (var (key, value) in file?.Products ?? [])
            products[key] = value;
        return products;
    }

    private sealed record SimulationProfileFile(
        Dictionary<string, SimulationProfile> Products);
}
