using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WoWAddonLab.Assets;
using WoWAddonLab.Configuration;
using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Addons;

public static partial class BlizzardUiBootstrap
{
    private const string InterfacePrefix = "interface/addons/";
    private static readonly string[] SeedAddons =
    [
        "Blizzard_FrameXML",
        "Blizzard_HelpFrame",
        "Blizzard_ExpansionLandingPage",
        "Blizzard_SharedTalentUI",
        "Blizzard_ActionBar",
        "Blizzard_ActionBarController",
        "Blizzard_ActionStatus",
        "Blizzard_AuraContainer",
        "Blizzard_BuffFrame",
        "Blizzard_CastingBar",
        "Blizzard_CatalogShop",
        "Blizzard_ChatFrameBase",
        "Blizzard_ChatFrame",
        "Blizzard_CompactRaidFrames",
        "Blizzard_MicroMenu",
        "Blizzard_Minimap",
        "Blizzard_MapCanvasSecureUtil",
        "Blizzard_MapCanvas",
        "Blizzard_NamePlates",
        "Blizzard_ObjectiveTracker",
        "Blizzard_QueueStatusFrame",
        "Blizzard_QuickJoin",
        "Blizzard_SharedMapDataProviders",
        "Blizzard_StatusTrayManager",
        "Blizzard_StatusUI",
        "Blizzard_TextStatusBar",
        "Blizzard_UnitFrame",
        "Blizzard_WorldMap"
    ];
    private const int CacheSchemaVersion = 41;
    private static readonly IReadOnlyDictionary<string, string[]> ImplicitInGameDependencies =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Blizzard_Settings_Shared"] = ["Blizzard_StaticPopup"],
            ["Blizzard_MicroMenu"] = ["Blizzard_CatalogShop"],
            ["Blizzard_SpellSearch"] = ["Blizzard_ActionBar"],
            ["Blizzard_TieredEntranceTraits"] = ["Blizzard_SharedTalentUI"],
            ["Blizzard_UnitFrame"] = ["Blizzard_AuraContainer"],
            ["Blizzard_CooldownViewer"] = ["Blizzard_AuraContainer"]
        };

    public static BlizzardUiBootstrapResult Prepare(
        TactAssetSource source,
        WowInstallation installation,
        string cacheRoot)
    {
        if (!source.IsInitialized || source.ListfilePath is null)
            throw new InvalidOperationException("TACTSharp must be initialized before preparing Blizzard UI.");

        var version = SafePathSegment(installation.Version ?? "unknown");
        var versionRoot = Path.Combine(
            cacheRoot,
            installation.Product.ProductCode.ToLowerInvariant(),
            version);
        var outputRoot = Path.Combine(
            versionRoot,
            "Interface",
            "AddOns");
        var cacheManifestPath = Path.Combine(versionRoot, "bootstrap-cache.json");
        if (TryLoadCache(cacheManifestPath, outputRoot, out var cached))
            return cached;
        if (Directory.Exists(outputRoot))
            Directory.Delete(outputRoot, true);

        var catalog = BuildCatalog(source, source.ListfilePath);
        var context = AddonManifestContextFactory.For(installation);
        var prepared = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var incompatible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceMetadata =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(
                StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extractedCascPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var extractedFiles = 0;

        bool PrepareAddon(string addonName)
        {
            if (prepared.ContainsKey(addonName))
                return true;
            if (incompatible.Contains(addonName))
                return false;
            if (!visiting.Add(addonName))
            {
                warnings.Add($"Blizzard UI dependency cycle includes {addonName}.");
                return false;
            }

            try
            {
                var toc = ResolveToc(source, catalog, addonName, context);
                if (toc is null)
                {
                    warnings.Add($"The selected build does not contain a compatible TOC for {addonName}.");
                    return false;
                }

                var parsed = ParseToc(toc.Bytes, context);
                if (!IsLoadableInContext(parsed.Metadata, context))
                {
                    incompatible.Add(addonName);
                    return false;
                }
                var dependencies = parsed.Dependencies
                    .Where(IsInGameDependency)
                    .Concat(
                        ImplicitInGameDependencies.TryGetValue(addonName, out var implicitDependencies)
                            ? implicitDependencies
                            : [])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                foreach (var dependency in dependencies)
                    PrepareAddon(dependency);

                var addonRoot = Path.Combine(outputRoot, addonName);
                Directory.CreateDirectory(addonRoot);
                var normalizedEntries = new List<string>();
                var secureEnvironmentEntries = new List<string>();
                var listedCascPaths = parsed.Files
                    .Select(value => NormalizePath($"{toc.Directory}/{value}"))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in parsed.Files)
                {
                    var cascPath = NormalizePath($"{toc.Directory}/{entry}");
                    if (!cascPath.StartsWith(toc.Directory + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add($"{addonName} entry escapes its CASC directory: {entry}");
                        continue;
                    }

                    if (ExtractFile(
                            source,
                            catalog,
                            cascPath,
                            toc.Directory,
                            addonRoot,
                            extractedCascPaths,
                            warnings,
                            out var count))
                    {
                        if (TryExtractSecureEnvironmentSource(
                                source,
                                catalog,
                                cascPath,
                                toc.Directory,
                                addonRoot,
                                listedCascPaths,
                                extractedCascPaths,
                                warnings,
                                out var secureEntry,
                                out var secureCount))
                        {
                            normalizedEntries.Add(secureEntry);
                            secureEnvironmentEntries.Add(secureEntry);
                            extractedFiles += secureCount;
                        }
                        normalizedEntries.Add(
                            cascPath[(toc.Directory.Length + 1)..]
                                .Replace('/', Path.DirectorySeparatorChar));
                        extractedFiles += count;
                    }
                }

                if (addonName.Equals("Blizzard_FrameXML", StringComparison.OrdinalIgnoreCase) &&
                    TryReadProductBindings(source, catalog, toc.Directory, context, out var bindingsBytes))
                {
                    WriteIfChanged(Path.Combine(addonRoot, "Bindings.xml"), bindingsBytes);
                    extractedFiles++;

                    var defaultsPath = context.GameType.Equals("wowlabs", StringComparison.OrdinalIgnoreCase)
                        ? "wtf/defaultwowlabsbindings.wtf"
                        : "wtf/defaultbindings.wtf";
                    if (source.Read(defaultsPath, null) is { } defaultsBytes)
                    {
                        WriteIfChanged(Path.Combine(addonRoot, "DefaultBindings.wtf"), defaultsBytes);
                        extractedFiles++;
                    }
                }

                var localToc = Path.Combine(addonRoot, $"{addonName}.toc");
                WriteIfChanged(
                    localToc,
                    Encoding.UTF8.GetBytes(
                        BuildLocalToc(
                            addonName,
                            parsed.Metadata,
                            dependencies,
                            normalizedEntries,
                            secureEnvironmentEntries)));
                prepared[addonName] = addonRoot;
                sourceMetadata[addonName] = parsed.Metadata;
                return true;
            }
            finally
            {
                visiting.Remove(addonName);
            }
        }

        foreach (var addonName in catalog.TocByAddon.Keys
                     .Where(IsInGameDependency)
                     .Where(value => value.StartsWith("Blizzard_", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            PrepareAddon(addonName);
        }

        var ordered = TopologicalOrder(prepared.Values);
        var manifests = ordered
            .Select(path => AddonManifest.Load(path))
            .ToDictionary(value => value.Name, StringComparer.OrdinalIgnoreCase);
        var startupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void IncludeStartupDependencies(string addonName)
        {
            if (!startupNames.Add(addonName) ||
                !manifests.TryGetValue(addonName, out var manifest))
                return;
            foreach (var dependency in Dependencies(manifest.Metadata))
                IncludeStartupDependencies(dependency);
        }

        foreach (var (addonName, metadata) in sourceMetadata)
        {
            if (ShouldLoadAtStartup(metadata))
                IncludeStartupDependencies(addonName);
        }
        foreach (var seedAddon in SeedAddons)
            IncludeStartupDependencies(seedAddon);

        var startupCandidates = SeedAddons
            .Where(name => startupNames.Contains(name) && prepared.ContainsKey(name))
            .Concat(
                ordered
                    .Select(Path.GetFileName)
                    .Where(name => name is not null && startupNames.Contains(name)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => prepared[name!])
            .ToArray();
        var startup = TopologicalOrder(startupCandidates).ToArray();
        var result = new BlizzardUiBootstrapResult(
            startup,
            ordered,
            ordered.Select(value => Path.GetFileName(value)!).ToArray(),
            [],
            extractedFiles,
            warnings,
            false);
        WriteIfChanged(
            cacheManifestPath,
            JsonSerializer.SerializeToUtf8Bytes(
                new CacheManifest(
                    CacheSchemaVersion,
                    result.AddonPaths.Select(path => Path.GetFileName(path)!).ToArray(),
                    result.ModuleNames,
                    result.ExtractedFiles),
                new JsonSerializerOptions { WriteIndented = true }));
        return result;
    }

    private static bool DeclaresSecureMixin(
        TactAssetSource source,
        Catalog catalog,
        string cascPath,
        List<string> warnings)
    {
        if (!TryRead(source, catalog, cascPath, out var bytes))
            return false;

        try
        {
            using var stream = new MemoryStream(bytes);
            var document = XDocument.Load(stream);
            return document.Descendants().Any(element =>
                element.Name.LocalName.Equals("Mixin", StringComparison.OrdinalIgnoreCase) &&
                element.Attributes().Any(attribute =>
                    attribute.Name.LocalName.Equals("source", StringComparison.OrdinalIgnoreCase) &&
                    attribute.Value.Equals("secure", StringComparison.OrdinalIgnoreCase)));
        }
        catch (Exception exception)
        {
            warnings.Add($"Could not inspect Blizzard XML mixins for {cascPath}: {exception.Message}");
            return false;
        }
    }

    private static bool TryLoadCache(
        string manifestPath,
        string outputRoot,
        out BlizzardUiBootstrapResult result)
    {
        result = new BlizzardUiBootstrapResult([], [], [], [], 0, [], false);
        if (!File.Exists(manifestPath))
            return false;
        try
        {
            var manifest = JsonSerializer.Deserialize<CacheManifest>(File.ReadAllBytes(manifestPath));
            if (manifest is null || manifest.SchemaVersion != CacheSchemaVersion)
                return false;
            var paths = manifest.ModuleNames
                .Select(name => Path.Combine(outputRoot, name))
                .ToArray();
            if (paths.Any(path =>
                    !File.Exists(Path.Combine(path, $"{Path.GetFileName(path)}.toc"))))
                return false;
            var startupPaths = manifest.StartupModuleNames
                .Select(name => Path.Combine(outputRoot, name))
                .ToArray();
            result = new BlizzardUiBootstrapResult(
                startupPaths,
                paths,
                manifest.ModuleNames,
                [],
                manifest.ExtractedFiles,
                [],
                true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyList<string> TopologicalOrder(IEnumerable<string> addonPaths)
    {
        return AddonManifestLoadOrder.Order(
                addonPaths.Select(path => AddonManifest.Load(path)))
            .Select(manifest => manifest.RootPath)
            .ToArray();
    }

    private static bool IsInGameDependency(string addonName) =>
        !addonName.StartsWith("Blizzard_Glue", StringComparison.OrdinalIgnoreCase) &&
        !addonName.Equals("Blizzard_StaticPopup_Glue", StringComparison.OrdinalIgnoreCase);

    private static Catalog BuildCatalog(TactAssetSource source, string listfilePath)
    {
        var canonicalAddonNames = CanonicalTocAddonNames(source);
        var byPath = new Dictionary<string, List<uint>>(StringComparer.OrdinalIgnoreCase);
        var tocByAddon = new Dictionary<string, List<CatalogEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(listfilePath))
        {
            var separator = line.IndexOf(';');
            if (separator <= 0 || !uint.TryParse(line.AsSpan(0, separator), out var fileDataId))
                continue;
            var path = NormalizePath(line[(separator + 1)..]);
            if (!path.StartsWith(InterfacePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!byPath.TryGetValue(path, out var ids))
            {
                ids = [];
                byPath[path] = ids;
            }
            ids.Add(fileDataId);

            if (!path.EndsWith(".toc", StringComparison.OrdinalIgnoreCase))
                continue;
            var relative = path[InterfacePrefix.Length..];
            var slash = relative.IndexOf('/');
            if (slash <= 0)
                continue;
            var listedAddonName = relative[..slash];
            var addonName = canonicalAddonNames.GetValueOrDefault(listedAddonName) ?? listedAddonName;
            if (!tocByAddon.TryGetValue(addonName, out var tocs))
            {
                tocs = [];
                tocByAddon[addonName] = tocs;
            }
            tocs.Add(new CatalogEntry(fileDataId, path));
        }
        return new Catalog(byPath, tocByAddon);
    }

    private static IReadOnlyDictionary<string, string> CanonicalTocAddonNames(TactAssetSource source)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bytes = source.Read("interface/ui-toc-list.txt", null);
        if (bytes is null)
            return result;

        using var reader = new StringReader(Encoding.UTF8.GetString(bytes));
        while (reader.ReadLine() is { } sourceLine)
        {
            var path = sourceLine.Replace('\\', '/').Trim().TrimStart('/');
            if (!path.StartsWith(InterfacePrefix, StringComparison.OrdinalIgnoreCase) ||
                !path.EndsWith(".toc", StringComparison.OrdinalIgnoreCase))
                continue;
            var relative = path[InterfacePrefix.Length..];
            var slash = relative.IndexOf('/');
            if (slash <= 0)
                continue;
            result[relative[..slash]] = relative[..slash];
        }
        return result;
    }

    private static ResolvedToc? ResolveToc(
        TactAssetSource source,
        Catalog catalog,
        string addonName,
        AddonManifestContext context)
    {
        if (!catalog.TocByAddon.TryGetValue(addonName, out var candidates))
            return null;

        foreach (var candidate in candidates
                     .Where(value => TocScore(value.Path, addonName, context) < 20)
                     .OrderBy(value => TocScore(value.Path, addonName, context))
                     .ThenByDescending(value => value.FileDataId))
        {
            var bytes = source.Read(candidate.FileDataId);
            if (bytes is null)
                continue;
            return new ResolvedToc(
                candidate.Path[..candidate.Path.LastIndexOf('/')],
                bytes);
        }
        return null;
    }

    private static int TocScore(string path, string addonName, AddonManifestContext context)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (name.EndsWith($"_{context.GameType}", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (name.EndsWith($"_{context.Family}", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (context.Family.Equals("mainline", StringComparison.OrdinalIgnoreCase) &&
            name.EndsWith("_standard", StringComparison.OrdinalIgnoreCase))
            return 2;
        if (name.Equals(addonName, StringComparison.OrdinalIgnoreCase))
            return 3;
        return 20;
    }

    private static ParsedToc ParseToc(byte[] bytes, AddonManifestContext context)
    {
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<string>();
        using var reader = new StringReader(Encoding.UTF8.GetString(bytes));
        while (reader.ReadLine() is { } sourceLine)
        {
            var line = sourceLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') && !line.StartsWith("##", StringComparison.Ordinal))
                continue;
            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                var separator = line.IndexOf(':');
                if (separator <= 2)
                    continue;
                var key = line[2..separator].Trim();
                var value = line[(separator + 1)..];
                var metadataCondition = TocConditionStartRegex().Match(value);
                var metadataConditions = metadataCondition.Success
                    ? value[metadataCondition.Index..]
                    : string.Empty;
                if (!MatchesConditions(metadataConditions, context))
                    continue;
                if (metadataCondition.Success)
                    value = value[..metadataCondition.Index];
                value = value.Trim();
                if (key.Equals("Dependencies", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Dep", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("RequiredDep", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("RequiredDeps", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("RequiredDependencies", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dependency in SplitNames(value))
                        dependencies.Add(dependency);
                }
                else
                {
                    metadata[key] = value;
                }
                continue;
            }

            var fileCondition = TocConditionStartRegex().Match(line);
            var conditionStart = fileCondition.Success ? fileCondition.Index : -1;
            var conditions = conditionStart >= 0 ? line[conditionStart..] : string.Empty;
            if (!MatchesConditions(conditions, context))
                continue;
            var entry = conditionStart >= 0 ? line[..conditionStart] : line;
            entry = entry
                .Replace("[Family]", context.FamilyDirectory, StringComparison.OrdinalIgnoreCase)
                .Replace("[Game]", context.GameDirectory, StringComparison.OrdinalIgnoreCase)
                .Replace("[TextLocale]", context.TextLocale, StringComparison.OrdinalIgnoreCase)
                .Trim();
            if (entry.Length > 0)
                files.Add(NormalizePath(entry));
        }
        return new ParsedToc(metadata, dependencies.ToArray(), files);
    }

    private static bool MatchesConditions(string conditions, AddonManifestContext context)
    {
        var allowed = ConditionValues(AllowGameTypeRegex(), conditions);
        if (allowed.Length > 0 &&
            !allowed.Any(value => context.MatchesGameType(value)))
            return false;
        var excluded = ConditionValues(ExcludeGameTypeRegex(), conditions);
        if (excluded.Any(value => context.MatchesGameType(value)))
            return false;
        var allowedLocales = ConditionValues(AllowTextLocaleRegex(), conditions);
        if (allowedLocales.Length > 0 &&
            !allowedLocales.Any(value => context.MatchesTextLocale(value)))
            return false;
        var excludedLocales = ConditionValues(ExcludeTextLocaleRegex(), conditions);
        return !excludedLocales.Any(value => context.MatchesTextLocale(value));
    }

    private static bool IsLoadableInContext(
        IReadOnlyDictionary<string, string> metadata,
        AddonManifestContext context)
    {
        if (IsTrueMetadata(metadata, "OnlyBetaAndPTR") && !context.IsPublicTestClient)
            return false;

        if (metadata.TryGetValue("AllowLoad", out var allowLoad))
        {
            var environments = SplitNames(allowLoad).ToArray();
            if (!environments.Any(value =>
                    value.Equals("Game", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("Both", StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (metadata.TryGetValue("AllowLoadGameType", out var allowGameType) &&
            !SplitNames(allowGameType).Any(context.MatchesGameType))
            return false;
        return !metadata.TryGetValue("ExcludeLoadGameType", out var excludeGameType) ||
               !SplitNames(excludeGameType).Any(context.MatchesGameType);
    }

    private static readonly char[] ConditionSeparators = [' ', '\t', ','];

    private static string[] ConditionValues(Regex regex, string conditions) =>
        regex.Matches(conditions)
            .SelectMany(match => match.Groups[1].Value.Split(
                ConditionSeparators,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();

    private static bool TryExtractSecureEnvironmentSource(
        TactAssetSource source,
        Catalog catalog,
        string cascPath,
        string addonDirectory,
        string addonRoot,
        IReadOnlySet<string> listedCascPaths,
        HashSet<string> extractedCascPaths,
        List<string> warnings,
        out string normalizedEntry,
        out int extracted)
    {
        normalizedEntry = string.Empty;
        extracted = 0;
        var sourcePath = cascPath.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)
            ? cascPath[..^4] + "secure.lua"
            : cascPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? cascPath[..^4] + ".lua"
                : null;
        if (sourcePath is null ||
            listedCascPaths.Contains(sourcePath) ||
            extractedCascPaths.Contains(sourcePath) ||
            !catalog.ByPath.ContainsKey(sourcePath) ||
            !TryRead(source, catalog, sourcePath, out _))
        {
            return false;
        }
        if (cascPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
            !DeclaresSecureMixin(source, catalog, cascPath, warnings))
        {
            return false;
        }

        if (!ExtractFile(
                source,
                catalog,
                sourcePath,
                addonDirectory,
                addonRoot,
                extractedCascPaths,
                warnings,
                out extracted))
            return false;

        normalizedEntry = sourcePath[(addonDirectory.Length + 1)..]
            .Replace('/', Path.DirectorySeparatorChar);
        return true;
    }

    private static bool ExtractFile(
        TactAssetSource source,
        Catalog catalog,
        string cascPath,
        string addonDirectory,
        string addonRoot,
        HashSet<string> extractedCascPaths,
        List<string> warnings,
        out int extracted)
    {
        extracted = 0;
        if (!IsCacheableSource(cascPath))
        {
            warnings.Add($"Blizzard runtime asset was left in TACT storage: {cascPath}");
            return false;
        }
        if (!extractedCascPaths.Add(cascPath))
            return true;
        if (!TryRead(source, catalog, cascPath, out var bytes))
        {
            warnings.Add($"Missing Blizzard UI file in selected build: {cascPath}");
            return false;
        }

        var relative = cascPath[(addonDirectory.Length + 1)..];
        var localPath = Path.GetFullPath(Path.Combine(
            addonRoot,
            relative.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = Path.GetFullPath(addonRoot)
                             .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                         Path.DirectorySeparatorChar;
        if (!localPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Refused Blizzard UI extraction outside cache root: {cascPath}");
            return false;
        }

        WriteIfChanged(localPath, bytes);
        extracted++;
        if (!cascPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            using var stream = new MemoryStream(bytes);
            var document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            var directory = cascPath[..cascPath.LastIndexOf('/')];
            foreach (var element in document.Descendants().Where(value =>
                         value.Name.LocalName.Equals("Script", StringComparison.OrdinalIgnoreCase) ||
                         value.Name.LocalName.Equals("Include", StringComparison.OrdinalIgnoreCase)))
            {
                var file = element.Attributes()
                    .FirstOrDefault(value => value.Name.LocalName.Equals("file", StringComparison.OrdinalIgnoreCase))
                    ?.Value;
                if (string.IsNullOrWhiteSpace(file))
                    continue;
                var included = CombineCascPath(directory, file);
                if (!catalog.ByPath.ContainsKey(included))
                    included = CombineCascPath(addonDirectory, file);
                if (!included.StartsWith(addonDirectory + "/", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"Cross-addon Blizzard XML include was skipped: {included}");
                    continue;
                }
                if (ExtractFile(
                        source,
                        catalog,
                        included,
                        addonDirectory,
                        addonRoot,
                        extractedCascPaths,
                        warnings,
                        out var nested))
                    extracted += nested;
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"Could not inspect Blizzard XML includes for {cascPath}: {exception.Message}");
        }
        return true;
    }

    private static bool TryReadProductBindings(
        TactAssetSource source,
        Catalog catalog,
        string addonDirectory,
        AddonManifestContext context,
        out byte[] bytes)
    {
        foreach (var name in new[]
                 {
                     $"bindings_{context.GameType}.xml",
                     $"bindings_{context.Family}.xml",
                     "bindings.xml"
                 }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (TryRead(source, catalog, NormalizePath($"{addonDirectory}/{name}"), out bytes))
                return true;
        }

        bytes = [];
        return false;
    }

    private static bool IsCacheableSource(string path) =>
        path.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);

    private static bool TryRead(
        TactAssetSource source,
        Catalog catalog,
        string path,
        out byte[] bytes)
    {
        bytes = [];
        if (!catalog.ByPath.TryGetValue(path, out var candidates))
            return false;
        foreach (var fileDataId in candidates.AsEnumerable().Reverse())
        {
            var result = source.Read(fileDataId);
            if (result is null)
                continue;
            bytes = result;
            return true;
        }
        return false;
    }

    private static string BuildLocalToc(
        string addonName,
        IReadOnlyDictionary<string, string> metadata,
        IReadOnlyList<string> dependencies,
        IReadOnlyList<string> files,
        IReadOnlyList<string> secureEnvironmentFiles)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"## Title: {addonName}");
        builder.AppendLine("## Author: Blizzard Entertainment");
        foreach (var (key, value) in metadata)
        {
            if (key.Equals("Title", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Author", StringComparison.OrdinalIgnoreCase) ||
                IsDependencyKey(key))
                continue;
            builder.AppendLine($"## {key}: {value}");
        }
        if (dependencies.Count > 0)
            builder.AppendLine($"## Dependencies: {string.Join(", ", dependencies)}");
        if (secureEnvironmentFiles.Count > 0)
            builder.AppendLine($"## SecureEnvironmentFiles: {string.Join(", ", secureEnvironmentFiles)}");
        foreach (var file in files)
            builder.AppendLine(file);
        return builder.ToString();
    }

    private static IReadOnlyList<string> Dependencies(IReadOnlyDictionary<string, string> metadata)
    {
        var keys = new[]
        {
            "Dependencies",
            "Dep",
            "RequiredDep",
            "RequiredDeps",
            "RequiredDependencies"
        };
        return keys
            .Where(metadata.ContainsKey)
            .SelectMany(key => SplitNames(metadata[key]))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsDependencyKey(string key) =>
        key.Equals("Dependencies", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("Dep", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("RequiredDeps", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("RequiredDependencies", StringComparison.OrdinalIgnoreCase);

    private static bool IsTrueMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string key) =>
        metadata.TryGetValue(key, out var value) &&
        (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase));

    internal static bool ShouldLoadAtStartup(
        IReadOnlyDictionary<string, string> metadata) =>
        !IsTrueMetadata(metadata, "LoadOnDemand");

    internal static bool IsOnlyBetaAndPtrCompatible(
        IReadOnlyDictionary<string, string> metadata,
        string productCode) =>
        !IsTrueMetadata(metadata, "OnlyBetaAndPTR") ||
        AddonManifestContextFactory.IsPublicTestProduct(productCode);

    private static IEnumerable<string> SplitNames(string value) =>
        value.Split(
            [',', ' ', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string CombineCascPath(string directory, string relative)
    {
        var segments = new List<string>();
        foreach (var segment in $"{directory}/{relative.Replace('\\', '/')}".Split('/'))
        {
            if (segment is "" or ".")
                continue;
            if (segment == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);
                continue;
            }
            segments.Add(segment);
        }
        return string.Join('/', segments).ToLowerInvariant();
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').Trim().TrimStart('/').ToLowerInvariant();

    private static string SafePathSegment(string value) =>
        string.Concat(value.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private static void WriteIfChanged(string path, byte[] bytes)
    {
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    [GeneratedRegex(@"AllowLoadGameType\s+([^\]]+)", RegexOptions.IgnoreCase)]
    private static partial Regex AllowGameTypeRegex();

    [GeneratedRegex(@"ExcludeLoadGameType\s+([^\]]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ExcludeGameTypeRegex();

    [GeneratedRegex(@"AllowLoadTextLocale\s+([^\]]+)", RegexOptions.IgnoreCase)]
    private static partial Regex AllowTextLocaleRegex();

    [GeneratedRegex(@"ExcludeLoadTextLocale\s+([^\]]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ExcludeTextLocaleRegex();

    [GeneratedRegex(@"\s+\[(?:Allow|Exclude)", RegexOptions.IgnoreCase)]
    private static partial Regex TocConditionStartRegex();

    private sealed record Catalog(
        IReadOnlyDictionary<string, List<uint>> ByPath,
        IReadOnlyDictionary<string, List<CatalogEntry>> TocByAddon);

    private sealed record CatalogEntry(uint FileDataId, string Path);
    private sealed record ResolvedToc(string Directory, byte[] Bytes);
    private sealed record ParsedToc(
        IReadOnlyDictionary<string, string> Metadata,
        IReadOnlyList<string> Dependencies,
        IReadOnlyList<string> Files);
    private sealed record CacheManifest(
        int SchemaVersion,
        IReadOnlyList<string> StartupModuleNames,
        IReadOnlyList<string> ModuleNames,
        int ExtractedFiles);

}
