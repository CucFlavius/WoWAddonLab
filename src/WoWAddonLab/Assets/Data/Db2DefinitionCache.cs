using System.IO.Compression;
using System.Text.Json;

namespace WoWAddonLab.Assets;

public static class Db2DefinitionCache
{
    private const int SchemaVersion = 1;
    private const string SourceUrl = "https://github.com/wowdev/WoWDBDefs/archive/refs/heads/master.zip";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static Db2DefinitionResult EnsureCurrent(
        string directory,
        string productCode,
        string? build,
        bool force = false)
    {
        var state = LoadState(directory);
        var present = CountDefinitions(directory);
        var buildChanged = build is not null &&
                           (!state.Products.TryGetValue(productCode, out var recorded) || recorded != build);

        if (!force && present > 0 && state.SchemaVersion == SchemaVersion && !buildChanged)
            return new Db2DefinitionResult(present, false, null);

        try
        {
            var count = Download(directory);
            state.SchemaVersion = SchemaVersion;
            state.DownloadedUtc = DateTimeOffset.UtcNow;
            state.DefinitionCount = count;
            state.Source = SourceUrl;
            if (build is not null)
                state.Products[productCode] = build;
            SaveState(directory, state);
            return new Db2DefinitionResult(count, true, null);
        }
        catch (Exception exception)
        {
            if (present > 0)
            {
                return new Db2DefinitionResult(
                    present,
                    false,
                    $"DB2 definitions could not be refreshed ({exception.Message}). " +
                    $"Continuing with {present} cached definition(s).");
            }
            throw new InvalidOperationException(
                $"DB2 definitions are unavailable and could not be downloaded from {SourceUrl}.",
                exception);
        }
    }

    public static int CountDefinitions(string directory) =>
        Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.dbd").Count() : 0;

    public static Db2DefinitionState LoadState(string directory)
    {
        var path = StatePath(directory);
        if (!File.Exists(path))
            return new Db2DefinitionState();
        try
        {
            return JsonSerializer.Deserialize<Db2DefinitionState>(File.ReadAllText(path), JsonOptions)
                   ?? new Db2DefinitionState();
        }
        catch (Exception)
        {
            return new Db2DefinitionState();
        }
    }

    private static void SaveState(string directory, Db2DefinitionState state)
    {
        Directory.CreateDirectory(directory);
        var path = StatePath(directory);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    private static string StatePath(string directory) => Path.Combine(directory, "state.json");

    private static int Download(string directory)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WoWAddonLab");
        var payload = client.GetByteArrayAsync(SourceUrl).GetAwaiter().GetResult();

        var staging = directory + ".incoming";
        if (Directory.Exists(staging))
            Directory.Delete(staging, recursive: true);
        Directory.CreateDirectory(staging);

        var count = 0;
        using (var archive = new ZipArchive(new MemoryStream(payload), ZipArchiveMode.Read))
        {
            foreach (var entry in archive.Entries)
            {
                if (!entry.Name.EndsWith(".dbd", StringComparison.OrdinalIgnoreCase) ||
                    !entry.FullName.Contains("/definitions/", StringComparison.OrdinalIgnoreCase))
                    continue;
                entry.ExtractToFile(Path.Combine(staging, entry.Name), overwrite: true);
                count++;
            }
        }

        if (count == 0)
        {
            Directory.Delete(staging, recursive: true);
            throw new InvalidDataException("The definition archive contained no .dbd files.");
        }

        var previousState = File.Exists(StatePath(directory)) ? File.ReadAllText(StatePath(directory)) : null;
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
        Directory.Move(staging, directory);
        if (previousState is not null)
            File.WriteAllText(StatePath(directory), previousState);
        return count;
    }
}
