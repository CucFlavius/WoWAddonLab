using System.IO.Compression;
using System.Text.Json;

namespace WoWAddonLab.Assets;

public sealed class Db2DefinitionState
{
    public int SchemaVersion { get; set; }
    public DateTimeOffset? DownloadedUtc { get; set; }
    public int DefinitionCount { get; set; }
    public string? Source { get; set; }

    public Dictionary<string, string> Products { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
