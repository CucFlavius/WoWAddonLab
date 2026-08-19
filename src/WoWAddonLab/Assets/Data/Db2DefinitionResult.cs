using System.IO.Compression;
using System.Text.Json;

namespace WoWAddonLab.Assets;

public sealed record Db2DefinitionResult(int DefinitionCount, bool Updated, string? Warning);
