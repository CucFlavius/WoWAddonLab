using System.Text;
using System.Text.RegularExpressions;

namespace WoWAddonLab.Emulator.Addons;

public sealed record AddonManifest(
    string RootPath,
    string TocPath,
    string Name,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> AccountSavedVariables,
    IReadOnlyList<string> CharacterSavedVariables,
    IReadOnlyList<string> SavedVariables)
{
    private static readonly Regex TocCondition = new(
        @"\[(?:Allow|Exclude)Load(?:GameType|TextLocale)\s+[^\]]+\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AllowGameType = new(
        @"AllowLoadGameType\s+([^\]]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExcludeGameType = new(
        @"ExcludeLoadGameType\s+([^\]]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AllowTextLocale = new(
        @"AllowLoadTextLocale\s+([^\]]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExcludeTextLocale = new(
        @"ExcludeLoadTextLocale\s+([^\]]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static AddonManifest Load(
        string addonPath,
        AddonManifestContext? context = null)
    {
        context ??= AddonManifestContext.Mainline;
        var root = Path.GetFullPath(addonPath);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Addon directory was not found: {root}");

        var tocPath = SelectTocPath(root, context);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<string>();

        foreach (var sourceLine in File.ReadLines(tocPath, Encoding.UTF8))
        {
            var line = sourceLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') && !line.StartsWith("##", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                var separator = line.IndexOf(':');
                if (separator > 2)
                {
                    var value = line[(separator + 1)..];
                    if (!MatchesConditions(value, context))
                        continue;
                    value = TocCondition.Replace(value, string.Empty);
                    metadata[line[2..separator].Trim()] = value.Trim();
                }
                continue;
            }

            if (!MatchesConditions(line, context))
                continue;
            var entry = TocCondition.Replace(line, string.Empty)
                .Replace("[Family]", context.FamilyDirectory, StringComparison.OrdinalIgnoreCase)
                .Replace("[Game]", context.GameDirectory, StringComparison.OrdinalIgnoreCase)
                .Replace("[TextLocale]", context.TextLocale, StringComparison.OrdinalIgnoreCase)
                .Trim();
            if (entry.Length == 0)
                continue;

            var fullPath = Path.GetFullPath(Path.Combine(root, entry.Replace('\\', Path.DirectorySeparatorChar)));
            var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"TOC entry escapes the addon directory: {line}");
            files.Add(fullPath);
        }

        var accountSaved = SavedNames(metadata, "SavedVariables");
        var characterSaved = SavedNames(metadata, "SavedVariablesPerCharacter");
        var saved = accountSaved
            .Concat(characterSaved)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new AddonManifest(
            root,
            tocPath,
            Path.GetFileName(root),
            metadata,
            files,
            accountSaved,
            characterSaved,
            saved);
    }

    private static string SelectTocPath(string root, AddonManifestContext context)
    {
        var name = Path.GetFileName(root);
        var candidates = new[]
        {
            Path.Combine(root, $"{name}.toc"),
            Path.Combine(root, $"{name}_{context.GameDirectory}.toc"),
            Path.Combine(root, $"{name}_{context.FamilyDirectory}.toc")
        };
        return candidates.FirstOrDefault(File.Exists)
               ?? Directory.EnumerateFiles(root, "*.toc").FirstOrDefault()
               ?? throw new FileNotFoundException($"No .toc file was found in {root}.");
    }

    private static IReadOnlyList<string> SavedNames(
        IReadOnlyDictionary<string, string> metadata,
        string key) =>
        metadata.TryGetValue(key, out var value)
            ? value.Split(
                [',', ' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray()
            : [];

    private static bool MatchesConditions(string conditions, AddonManifestContext context)
    {
        var allowedGames = ConditionValues(AllowGameType, conditions);
        if (allowedGames.Length > 0 && !allowedGames.Any(context.MatchesGameType))
            return false;

        var excludedGames = ConditionValues(ExcludeGameType, conditions);
        if (excludedGames.Any(context.MatchesGameType))
            return false;

        var allowedLocales = ConditionValues(AllowTextLocale, conditions);
        if (allowedLocales.Length > 0 && !allowedLocales.Any(context.MatchesTextLocale))
            return false;

        var excludedLocales = ConditionValues(ExcludeTextLocale, conditions);
        return !excludedLocales.Any(context.MatchesTextLocale);
    }

    private static string[] ConditionValues(Regex regex, string conditions) =>
        regex.Matches(conditions)
            .SelectMany(match => match.Groups[1].Value.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
}
