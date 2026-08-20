using System.Text.Json;
using WoWAddonLab;
using WoWAddonLab.Emulator;
using WoWAddonLab.Addons;
using WoWAddonLab.Assets;
using WoWAddonLab.Configuration;
using WoWAddonLab.Diagnostics;
using WoWAddonLab.Automation;
using WoWAddonLab.Emulator.Diagnostics;

if (CommandLineOptions.WantsStartupReport(args))
    StartupTimeline.Start();

CommandLineOptions options;
using (StartupTimeline.Begin("parse command line"))
    options = CommandLineOptions.Parse(args);

StartupTimeline.Fact("Mode", options.Headless ? "headless" : "desktop");
StartupTimeline.Fact("Blizzard UI", (options.LoadBlizzardUi ?? true) ? "enabled" : "disabled");
StartupTimeline.Fact("TACT", options.AutoConnectTact ? "enabled" : "disabled");

if (options.Headless)
{
    var sessionSpan = StartupTimeline.Begin("create emulator session (Lua runtime construction)");
    using var session = new EmulatorSession(
        options.LogicalWidth,
        options.LogicalHeight,
        luaCacheDirectory: WoWAddonLabPaths.LuaCacheDirectory);
    sessionSpan.Dispose();
    StartupTimeline.AttachLog(session.Log);
    var addonPaths = new List<string>();
    var blizzardUiPaths = new List<string>();
    var blizzardAvailablePaths = new List<string>();
    var blizzardRuntimeFiles = new List<string>();
    WowInstallation? selectedInstallation = null;
    var loadBlizzardUi = options.LoadBlizzardUi ?? true;
    IReadOnlySet<string> disabledBlizzardModules =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    string savedVariablesDirectory;
    if (options.AddonPath is not null)
    {
        addonPaths.Add(options.AddonPath);
        savedVariablesDirectory = Path.Combine(
            WoWAddonLabPaths.UserDataDirectory,
            "SavedVariables",
            "legacy");
    }
    else
    {
        IReadOnlyList<WowInstallation> installations;
        using (StartupTimeline.Begin("detect WoW installations"))
            installations = WowInstallationDetector.Detect();
        var store = new WoWAddonLabSettingsStore();
        WoWAddonLabSettings settings;
        using (StartupTimeline.Begin("load emulator settings"))
            settings = store.Load();
        var installation = installations.FirstOrDefault(value =>
                               options.Product is not null &&
                               (value.Product.ProductCode.Equals(options.Product, StringComparison.OrdinalIgnoreCase) ||
                                value.Product.FolderName.Equals(options.Product, StringComparison.OrdinalIgnoreCase) ||
                                value.ProductPath.Equals(options.Product, StringComparison.OrdinalIgnoreCase)))
                           ?? installations.FirstOrDefault(value =>
                               value.ProductPath.Equals(
                                   settings.SelectedProductPath,
                                   StringComparison.OrdinalIgnoreCase))
                           ?? installations.FirstOrDefault(value =>
                               value.Product.ProductCode.Equals("wow", StringComparison.OrdinalIgnoreCase))
                           ?? installations.FirstOrDefault();
        if (installation is null)
        {
            savedVariablesDirectory = Path.Combine(
                store.DirectoryPath,
                "SavedVariables",
                "unconfigured");
        }
        else
        {
            selectedInstallation = installation;
            var manifestContext = AddonManifestContextFactory.For(installation);
            session.ManifestContext = manifestContext;
            var productKey = Path.GetFullPath(installation.ProductPath);
            if (!settings.Products.TryGetValue(productKey, out var productSettings))
            {
                productSettings = new ProductEmulationSettings();
                settings.Products[productKey] = productSettings;
            }
            loadBlizzardUi = options.LoadBlizzardUi ?? productSettings.LoadBlizzardUi;
            disabledBlizzardModules = productSettings.DisabledBlizzardModules;
            StartupTimeline.Fact("Product", $"{installation.Product.ProductCode} ({installation.Version})");
            IReadOnlyList<InstalledAddon> catalog;
            using (var span = StartupTimeline.Begin("discover installed addons"))
            {
                catalog = InstalledAddonCatalog.Discover(installation.ProductPath, manifestContext);
                span.Annotate($"{catalog.Count} found");
            }
            IReadOnlySet<string> enabled = options.EnabledAddons.Count > 0
                ? options.EnabledAddons.ToHashSet(StringComparer.OrdinalIgnoreCase)
                : productSettings.EnabledAddons;
            IReadOnlyList<InstalledAddon> resolvedAddons;
            using (var span = StartupTimeline.Begin("resolve addon load order"))
            {
                resolvedAddons = InstalledAddonCatalog.ResolveLoadOrder(
                    catalog,
                    enabled,
                    message => Console.Error.WriteLine($"Addon warning: {message}"),
                    WowBuildInfo.FromVersion(
                        installation.Version).InterfaceVersion);
                span.Annotate($"{resolvedAddons.Count} enabled");
            }
            addonPaths.AddRange(resolvedAddons.Select(value => value.RootPath));
            IReadOnlyList<SavedVariablesProfile> profiles;
            using (StartupTimeline.Begin("discover SavedVariables profiles"))
                profiles = SavedVariablesProfiles.Discover(installation);
            var selectedProfile = options.Profile ?? productSettings.ProfileId;
            var profile = profiles.FirstOrDefault(value =>
                              value.Id.Equals(selectedProfile, StringComparison.OrdinalIgnoreCase) ||
                              value.DisplayName.Equals(selectedProfile, StringComparison.OrdinalIgnoreCase))
                          ?? profiles[0];
            savedVariablesDirectory = SavedVariablesProfiles.EmulatorDirectory(
                store.DirectoryPath,
                installation,
                profile);
            if (options.ImportSavedVariables)
            {
                var imported = SavedVariablesProfiles.ImportCopies(
                    profile,
                    resolvedAddons,
                    savedVariablesDirectory);
                Console.WriteLine($"Imported {imported} SavedVariables file(s) into the emulator profile.");
            }
        }
    }

    if (selectedInstallation is not null)
    {
        session.BuildInfo = WowBuildInfo.FromVersion(selectedInstallation.Version);
        session.ClientCVarConfigPath =
            Path.Combine(selectedInstallation.ProductPath, "WTF", "Config.wtf");
        int importedCVars;
        using (StartupTimeline.Begin("import client CVars"))
            importedCVars = session.Lua.CVars.ImportConfigFile(session.ClientCVarConfigPath);
        Console.WriteLine($"Imported {importedCVars} read-only client CVar value(s).");
    }

    if (loadBlizzardUi && selectedInstallation is not null && options.AutoConnectTact)
    {
        var tact = new TactAssetSource(selectedInstallation);
        try
        {
            using (StartupTimeline.Begin("open WoW data through TACTSharp"))
                tact.Initialize(WoWAddonLabPaths.TactCacheDirectory);
            session.ModelResourceProvider = tact;
            tact.DefinitionsDirectory = WoWAddonLabPaths.DefinitionsCacheDirectory;
            using (var definitionSpan = StartupTimeline.Begin("ensure DB2 definitions"))
            {
                var definitions = Db2DefinitionCache.EnsureCurrent(
                    WoWAddonLabPaths.DefinitionsCacheDirectory,
                    selectedInstallation.Product.ProductCode,
                    selectedInstallation.Version);
                var summary = definitions.Updated
                    ? $"downloaded {definitions.DefinitionCount}"
                    : $"{definitions.DefinitionCount} cached";
                definitionSpan.Annotate(summary);
                StartupTimeline.Fact("DB2 definitions", summary);
                Console.WriteLine($"DB2 definitions: {summary}.");
                if (definitions.Warning is not null)
                    Console.Error.WriteLine(definitions.Warning);
            }
            using var catalogSpan = StartupTimeline.Begin("load client data catalogs");
            using var databaseCache = tact.Database.BeginCacheScope();
            var build = selectedInstallation.Version ??
                        throw new InvalidDataException("The selected WoW product has no build version.");
            var atlasCatalog = TactAtlasCatalog.Load(
                tact,
                build);
            session.AtlasProvider = atlasCatalog;
            Console.WriteLine($"Loaded {atlasCatalog.Count} client texture atlas entries.");
            var globalColors = TactGlobalColorCatalog.Load(tact, build);
            session.GlobalColorProvider = globalColors;
            Console.WriteLine($"Loaded {globalColors.Count} client global color entries.");
            var dyeColors = TactDyeColorCatalog.Load(tact, build);
            session.DyeColorProvider = dyeColors;
            Console.WriteLine($"Loaded {dyeColors.Count} client dye color entries.");
            var gameRules = TactGameRuleCatalog.Load(tact, build);
            session.GameRuleProvider = gameRules;
            Console.WriteLine($"Loaded {gameRules.Count} client game rule entries.");
            var maps = TactMapCatalog.Load(tact, build);
            session.MapProvider = maps;
            Console.WriteLine(
                $"Loaded {maps.MapCount} client map entries and {maps.ArtCount} default map-art associations.");
            var quests = TactQuestCatalog.Load(tact, build);
            session.QuestProvider = quests;
            Console.WriteLine($"Loaded {quests.Count} client task quest titles.");
            var encounterJournal = TactEncounterJournalCatalog.Load(tact, build);
            session.EncounterJournalProvider = encounterJournal;
            Console.WriteLine(
                $"Loaded {encounterJournal.Tiers.Count} Encounter Journal tiers and " +
                $"{encounterJournal.InstanceCount} instances " +
                $"({encounterJournal.DungeonCount} dungeons, {encounterJournal.RaidCount} raids, " +
                $"{encounterJournal.TierRelationCount} tier relationships).");
            var globalStrings = TactGlobalStringCatalog.Load(tact, build);
            session.GlobalStringProvider = globalStrings;
            Console.WriteLine($"Loaded {globalStrings.Count} client GlobalStrings entries.");
            var spells = TactSpellCatalog.Load(
                tact,
                build,
                WoWAddonLabPaths.DataCacheDirectory);
            session.SpellProvider = spells;
            Console.WriteLine($"Loaded {spells.Count} client spell definitions.");
            var itemClasses = TactItemClassCatalog.Load(
                tact,
                build,
                WoWAddonLabPaths.DataCacheDirectory);
            session.ItemClassProvider = itemClasses;
            session.ItemProvider = itemClasses;
            Console.WriteLine(
                $"Loaded {itemClasses.Items.Count} client items, " +
                $"{itemClasses.Classes.Count} item classes, and " +
                $"{itemClasses.SubClasses.Count} subclasses.");
            var inventorySlots = TactPaperDollItemFrameCatalog.Load(
                tact,
                build);
            session.InventorySlotProvider = inventorySlots;
            Console.WriteLine(
                $"Loaded {inventorySlots.InventorySlots.Count} client inventory slots.");
            var races = TactRaceCatalog.Load(tact, build);
            session.RaceProvider = races;
            Console.WriteLine($"Loaded {races.Races.Count} client races.");
            var factions = TactFactionCatalog.Load(tact, build);
            session.FactionProvider = factions;
            Console.WriteLine($"Loaded {factions.Count} client factions.");
            var transmogSets = TactTransmogSetCatalog.Load(
                tact,
                build,
                WoWAddonLabPaths.DataCacheDirectory);
            session.TransmogSetProvider = transmogSets;
            session.TransmogAppearanceProvider = transmogSets;
            Console.WriteLine(
                $"Loaded {transmogSets.Sets.Count} client transmog sets and " +
                $"{transmogSets.Count} appearance sources.");
            var characterServices = TactCharacterServiceCatalog.Load(tact, build);
            session.CharacterServiceProvider = characterServices;
            Console.WriteLine(
                $"Loaded {characterServices.Count} client character service definitions.");
            var achievements = TactAchievementCatalog.Load(tact, build);
            session.AchievementProvider = achievements;
            Console.WriteLine(
                $"Loaded {achievements.Achievements.Count} achievements and " +
                $"{achievements.Categories.Count} achievement categories.");
            var accountStore = TactAccountStoreCatalog.Load(tact, build);
            session.AccountStoreProvider = accountStore;
            Console.WriteLine(
                $"Loaded {accountStore.Categories.Count} account store categories and " +
                $"{accountStore.Items.Count} account store items.");
            var azeriteEssence = TactAzeriteEssenceCatalog.Load(tact, build);
            session.AzeriteEssenceProvider = azeriteEssence;
            Console.WriteLine(
                $"Loaded {azeriteEssence.Milestones.Count} Azerite essence milestones.");
            var modelInfo = TactModelInfoCatalog.Load(tact, build);
            session.ModelInfoProvider = modelInfo;
            Console.WriteLine(
                $"Loaded {modelInfo.SceneCount} model scenes, {modelInfo.ActorCount} actors, " +
                $"{modelInfo.ActorDisplayCount} actor displays, and {modelInfo.CameraCount} cameras.");
            var macroIcons = TactMacroIconCatalog.Load(
                tact,
                build,
                WoWAddonLabPaths.DataCacheDirectory);
            session.MacroIconProvider = macroIcons;
            Console.WriteLine($"Loaded {macroIcons.Count} client macro icon entries.");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Atlas metadata warning: {exception.Message}");
        }
        BlizzardUiBootstrapResult prepared;
        using (var span = StartupTimeline.Begin("prepare Blizzard UI sources"))
        {
            prepared = BlizzardUiBootstrap.Prepare(
                tact,
                selectedInstallation,
                WoWAddonLabPaths.BlizzardUiCacheDirectory);
            span.Annotate(prepared.CacheHit ? "cache hit" : "extracted");
            StartupTimeline.Fact("Blizzard UI cache", prepared.CacheHit ? "hit" : "miss (extracted)");
            StartupTimeline.Fact("Blizzard UI files", prepared.ExtractedFiles.ToString());
        }
        var enabledStartupPaths = prepared.AddonPaths
            .Where(path => !disabledBlizzardModules.Contains(Path.GetFileName(path)!))
            .ToArray();
        blizzardUiPaths.AddRange(options.BlizzardUiModuleLimit is { } moduleLimit
            ? enabledStartupPaths.Take(moduleLimit)
            : enabledStartupPaths);
        blizzardAvailablePaths.AddRange(options.BlizzardUiModuleLimit is null
            ? prepared.AvailableAddonPaths
                .Where(path => !disabledBlizzardModules.Contains(Path.GetFileName(path)!))
            : blizzardUiPaths);
        blizzardRuntimeFiles.AddRange(prepared.RuntimeFiles);
        Console.WriteLine(
            prepared.CacheHit
                ? $"Reused {blizzardUiPaths.Count}/{prepared.ModuleNames.Count} cached Blizzard UI module(s), {prepared.ExtractedFiles} files."
                : $"Prepared {blizzardUiPaths.Count}/{prepared.ModuleNames.Count} Blizzard UI module(s), {prepared.ExtractedFiles} files.");
        foreach (var warning in prepared.Warnings)
            Console.Error.WriteLine($"Blizzard UI warning: {warning}");
    }
    else if (loadBlizzardUi && selectedInstallation is not null)
    {
        Console.Error.WriteLine("Blizzard UI was skipped because TACTSharp was disabled.");
    }

    using (StartupTimeline.Begin("load addons and Blizzard UI"))
    {
        session.Load(
            addonPaths,
            savedVariablesDirectory,
            blizzardUiPaths,
            blizzardRuntimeFiles,
            blizzardAvailablePaths);
    }
    await using var automation = new AutomationServer(session, options.Port);
    using (StartupTimeline.Begin("start automation server"))
        await automation.StartAsync();
    Console.WriteLine(
        $"WoW Addon Lab loaded {session.Manifests.Count} addon(s). Automation API: {automation.BaseUrl}");

    StartupTimeline.Fact("Addons loaded", session.Manifests.Count.ToString());
    StartupTimeline.Fact("Blizzard modules loaded", session.BootstrapManifests.Count.ToString());
    StartupTimeline.Ready("addons loaded and automation API listening");
    StartupReportWriter.Write(options, "Startup Timing (headless)");

    if (options.ExitWhenReady)
        return;

    if (options.Ticks is { } ticks)
    {
        for (var index = 0; index < ticks; index++)
            session.Tick(1.0 / 60.0);
        Console.WriteLine(JsonSerializer.Serialize(session.Status(), new JsonSerializerOptions { WriteIndented = true }));
        return;
    }

    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };
    while (!cancellation.IsCancellationRequested)
    {
        session.Tick(1.0 / 60.0);
        await Task.Delay(16, cancellation.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
    return;
}

using var application = new LabHost(options);
application.Run();
