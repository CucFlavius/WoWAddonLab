using System.Text.RegularExpressions;
using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Emulator.Lua;

internal static partial class WowEventDocumentation
{
    public static IReadOnlySet<string> Read(IEnumerable<AddonManifest> manifests)
    {
        var events = new HashSet<string>(StringComparer.Ordinal);
        foreach (var manifest in manifests.Where(manifest => manifest.Name.Equals(
                     "Blizzard_APIDocumentationGenerated",
                     StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var file in manifest.Files.Where(file =>
                         file.EndsWith("documentation.lua", StringComparison.OrdinalIgnoreCase) &&
                         File.Exists(file)))
            {
                var source = File.ReadAllText(file);
                foreach (Match match in EventRegex().Matches(source))
                    events.Add(match.Groups["name"].Value);
                foreach (Match match in LiteralEventRegex().Matches(source))
                    events.Add(match.Groups["name"].Value);
            }
        }
        return events;
    }

    [GeneratedRegex(
        "Name\\s*=\\s*\"(?<name>[A-Z][A-Z0-9_]+)\"\\s*,\\s*Type\\s*=\\s*\"Event\"",
        RegexOptions.CultureInvariant)]
    private static partial Regex EventRegex();

    [GeneratedRegex(
        "LiteralName\\s*=\\s*\"(?<name>[A-Z][A-Z0-9_]+)\"",
        RegexOptions.CultureInvariant)]
    private static partial Regex LiteralEventRegex();
}
