using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using WoWAddonLab.Emulator.Addons;
using WoWAddonLab.Emulator.Diagnostics;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class LuaRuntime : IDisposable
{
    private const string SavedVariablesHeader = "-- Saved by WoW Addon Lab at ";
    private readonly List<LuaTimer> _timers = [];
    private readonly List<DeferredScriptInvocation> _deferredScripts = [];
    private readonly HashSet<int> _pendingSizeChanged = [];
    private readonly HashSet<int> _pendingScrollChildRects = [];
    private readonly Dictionary<int, Vector2> _lastNotifiedSizes = [];
    private readonly Dictionary<string, List<GlobalEventCallback>> _globalEventCallbacks =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, SortedSet<int>> _eventTargetIds =
        new(StringComparer.Ordinal);
    private readonly SortedSet<int> _allEventTargetIds = [];
    private readonly Dictionary<int, List<PendingGlobalMixin>> _pendingGlobalMixins = [];
    private readonly Dictionary<string, int> _addonLocalTableReferences =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadingAddonNames =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _secureAddonNames =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _savedDisabledUserAddons =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (bool Loadable, string? Reason)> _addonLoadability =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Stopwatch _wallClock = Stopwatch.StartNew();
    private readonly XmlUiLoader _xmlUiLoader;
    private readonly LuaBytecodeCache? _bytecodeCache;
    private int? _secureEnvironmentReference;
    private int? _currentAddonEnvironmentReference;
    private int? _defaultWindowReference;
    private string? _currentAddonName;
    private bool _currentAddonIsInsecure;
    private bool _addonVersionCheckEnabled;
    private int _modelSceneCallbackDepth;

    internal int? CurrentAddonEnvironmentReference => _currentAddonEnvironmentReference;
    internal bool IsProcessingModelSceneCallbacks => _modelSceneCallbackDepth > 0;
    private bool _disposed;
    private bool _flushingSizeChanged;
    private long _nextSoundHandle;
    private long _nextTimerId;

    public LuaRuntime(
        EmulatorLog log,
        UiSystem ui,
        string? savedVariablesDirectory = null,
        WowDataProviders? providers = null,
        string? luaCacheDirectory = null)
    {
        Log = log;
        Ui = ui;
        Providers = providers ?? new WowDataProviders();
        _bytecodeCache = string.IsNullOrWhiteSpace(luaCacheDirectory)
            ? null
            : new LuaBytecodeCache(luaCacheDirectory);
        Providers.Changed += OnDataProviderChanged;
        SavedVariablesDirectory = savedVariablesDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WoWAddonLab",
            "SavedVariables");
        EditMode = new WowEditModeState(savedVariablesDirectory is null ? null : SavedVariablesDirectory, log);
        State = luaL_newstate();
        if (State == 0)
            throw new InvalidOperationException("Lua 5.1 state creation failed.");

        _xmlUiLoader = new XmlUiLoader(this);
        luaL_openlibs(State);
        LuaBindings.Attach(this);
        LuaBindings.Register(State);
        ExecuteString(ReadCompatibilityLayer(), "@WowCompat.lua");
        CreateGlobals();
        CVars.ValueChanged += OnCVarValueChanged;
        ApplyUiScaleCVars();
        ApplyDataProvider(WowDataProviderKind.GlobalColor);
        ApplyDataProvider(WowDataProviderKind.EncounterJournal);
        ApplyDataProvider(WowDataProviderKind.GlobalString);
        ApplyDataProvider(WowDataProviderKind.Spell);
        ApplyDataProvider(WowDataProviderKind.Item);
        ApplyDataProvider(WowDataProviderKind.ItemClass);
        ApplyDataProvider(WowDataProviderKind.InventorySlot);
        ApplyDataProvider(WowDataProviderKind.Race);
        WowLegacyGlobalConstants.Apply(State);
    }

    public lua_State State { get; }
    public event Action<AddonLoadProgress>? LoadProgress;
    internal string? CurrentAddonName => _currentAddonName;
    internal bool CurrentAddonIsInsecure => _currentAddonIsInsecure;
    public EmulatorLog Log { get; }
    public UiSystem Ui { get; }
    internal Action<UiObject>? AbortFrameDragHandler { get; set; }
    internal Func<UiObject, UiObject, bool>? InterceptFrameDragHandler { get; set; }
    public string LastCopiedTooltipText { get; internal set; } = string.Empty;
    public WowUnitStateCollection Units { get; } = new();
    public WowClientState Client { get; } = new();
    public WowEventUtilsState EventUtils { get; } = new();
    public WowAutoCompleteState AutoComplete { get; } = new();
    public WowGmTicketState GmTicket { get; } = new();
    public WowGameUiState GameUi { get; } = new();
    public WowInputState Input { get; } = new();
    public WowCameraState Camera { get; } = new();
    public WowPlayerInfoState PlayerInfo { get; } = new();
    public WowVideoOptionsState VideoOptions { get; } = new();
    public WowCursorState Cursor { get; } = new();
    public WowCinematicState Cinematics { get; } = new();
    public WowChatState Chat { get; } = new();
    public WowClickBindingsState ClickBindings { get; } = new();
    public WowTargetState Target { get; } = new();
    public WowTitleCollectionState Titles { get; } = new();
    public WowWorldStateState WorldStates { get; } = new();
    public WowGameRulesState GameRules { get; } = new();
    public WowRestrictedActionsState RestrictedActions { get; } = new();
    public WowPlayerMentorshipState PlayerMentorship { get; } = new();
    public WowPlayerScriptState PlayerScript { get; } = new();
    public WowAccountState Account { get; } = new();
    public WowAccountStoreState AccountStore { get; } = new();
    public WowTrainerState Trainer { get; } = new();
    public WowGroupState Group { get; } = new();
    public WowGroupLootState GroupLoot { get; } = new();
    public WowActionState Actions { get; } = new();
    public WowSpellState Spells { get; } = new();
    public WowSpellDiminishState SpellDiminish { get; } = new();
    public WowCurrencyInfoState CurrencyInfo { get; } = new();
    public WowDamageMeterState DamageMeter { get; } = new();
    public WowPetBattlesState PetBattles { get; } = new();
    public WowPetJournalState PetJournal { get; } = new();
    public WowPetInfoState PetInfo { get; } = new();
    public WowTradeSkillUiState TradeSkillUi { get; } = new();
    public WowBindingState Bindings { get; } = new();
    public WowMacroState Macros { get; } = new();
    public WowCVarState CVars { get; } = new();
    public WowTotemState Totems { get; } = new();
    public WowDateAndTimeState DateAndTime { get; } = new();
    public WowVoiceChatState VoiceChat { get; } = new();
    public WowUnitAuraState UnitAuras { get; } = new();
    public WowInstanceState Instance { get; } = new();
    public WowPvpState Pvp { get; } = new();
    public WowWeeklyRewardsState WeeklyRewards { get; } = new();
    public WowMythicPlusState MythicPlus { get; } = new();
    public WowMajorFactionsState MajorFactions { get; } = new();
    public WowPlayerInteractionManagerState PlayerInteractions { get; } = new();
    public WowLootJournalState LootJournal { get; } = new();
    public WowLootHistoryState LootHistory { get; } = new();
    public WowCommentatorState Commentator { get; } = new();
    public WowPartyInfoState PartyInfo { get; } = new();
    public WowArtifactState Artifact { get; } = new();
    public WowEquipmentState Equipment { get; } = new();
    public WowMerchantState Merchant { get; } = new();
    public WowItemState Items { get; } = new();
    public WowItemInteractionState ItemInteraction { get; } = new();
    public WowLegendaryCraftingState LegendaryCrafting { get; } = new();
    public WowScrappingMachineState ScrappingMachine { get; } = new();
    public WowSoulbindsState Soulbinds { get; } = new();
    public WowPaperDollState PaperDoll { get; } = new();
    public WowShapeshiftState Shapeshift { get; } = new();
    public WowMinimapState Minimap { get; } = new();
    public WowReputationState Reputation { get; } = new();
    public WowNamePlateState NamePlates { get; } = new();
    public WowUiWidgetManagerState UiWidgets { get; } = new();
    public WowHousingState Housing { get; } = new();
    public WowHousingBasicModeState HousingBasicMode { get; } = new();
    public WowPingState Ping { get; } = new();
    public WowContainerState Containers { get; } = new();
    public WowNewItemsState NewItems { get; } = new();
    public WowBankState Bank { get; } = new();
    public WowWorldTimerState WorldTimers { get; } = new();
    public WowLegacyProgressionState LegacyProgression { get; } = new();
    public WowQuestSessionState QuestSession { get; } = new();
    public WowCraftingOrdersState CraftingOrders { get; } = new();
    public WowCalendarState Calendar { get; } = new();
    public WowKioskState Kiosk { get; } = new();
    public WowMountJournalState MountJournal { get; } = new();
    public WowLobbyMatchmakerState LobbyMatchmaker { get; } = new();
    public WowTraitState Traits { get; } = new();
    public WowItemUpgradeState ItemUpgrade { get; } = new();
    public WowTtsSettingsState TtsSettings { get; } = new();
    public WowSettingsUtilState SettingsUtil { get; } = new();
    public WowSocialRestrictionsState SocialRestrictions { get; } = new();
    public WowGarrisonState Garrison { get; } = new();
    public WowCovenantState Covenants { get; } = new();
    public WowCovenantCallingsState CovenantCallings { get; } = new();
    public WowClubState Clubs { get; } = new();
    public WowCommunityFeatureState CommunityFeatures { get; } = new();
    public WowClubFinderState ClubFinder { get; } = new();
    public WowFriendState Friends { get; } = new();
    public WowRecentAlliesState RecentAllies { get; } = new();
    public WowEditModeState EditMode { get; }
    public WowLfgInfoState LfgInfo { get; } = new();
    public WowLfgListState LfgList { get; } = new();
    public WowLabsDataManagerState WowLabsDataManager { get; } = new();
    public WowQuestLogState QuestLog { get; } = new();
    public WowQuestInteractionState QuestInteraction { get; } = new();
    public WowQuestLineState QuestLines { get; } = new();
    public WowVignetteState Vignettes { get; } = new();
    public WowEventSchedulerState EventScheduler { get; } = new();
    public WowMapState Maps { get; } = new();
    public WowAdventureMapState AdventureMap { get; } = new();
    public WowCatalogShopState CatalogShop { get; } = new();
    public WowStoreSecureState StoreSecure { get; } = new();
    public WowStorePublicState StorePublic { get; } = new();
    public WowSocialQueueState SocialQueue { get; } = new();
    public WowAdventureJournalState AdventureJournal { get; } = new();
    public WowPlayerChoiceState PlayerChoice { get; } = new();
    public WowSummonInfoState SummonInfo { get; } = new();
    public WowInvasionInfoState InvasionInfo { get; } = new();
    public WowProfessionState Professions { get; } = new();
    public WowMapExplorationState MapExploration { get; } = new();
    public WowScenarioState Scenario { get; } = new();
    public WowScenarioInfoApiState ScenarioInfo { get; } = new();
    public WowSeasonInfoState SeasonInfo { get; } = new();
    public WowGuildInfoState Guild { get; } = new();
    public WowSpellTargetingState SpellTargeting { get; } = new();
    public WowSpellConfirmationState SpellConfirmation { get; } = new();
    public WowCombatLogState CombatLog { get; } = new();
    public WowLocalizationState Localization { get; } = new();
    internal List<WowDurationTextBindingState> DurationTextBindings { get; } = [];
    public WowContentTrackingState ContentTracking { get; } = new();
    public WowCombatAudioAlertState CombatAudioAlerts { get; } = new();
    public WowSoundState Sound { get; } = new();
    public WowSoundGameSystemState SoundGameSystem { get; } = new();
    public WowCombatTextState CombatText { get; } = new();
    public WowNavigationState Navigation { get; } = new();
    public WowAddOnProfilerState AddOnProfiler { get; } = new();
    public WowClassState Classes { get; } = new();
    public WowSpecializationState Specializations { get; } = new();
    public WowClassTalentsState ClassTalents { get; } = new();
    public WowAchievementState Achievements { get; } = new();
    public WowToyBoxState ToyBox { get; } = new();
    public WowAuctionHouseState AuctionHouse { get; } = new();
    public WowBlackMarketState BlackMarket { get; } = new();
    public WowChromieTimeState ChromieTime { get; } = new();
    public WowStableInfoState StableInfo { get; } = new();
    public WowAzeriteEmpoweredItemState AzeriteEmpoweredItem { get; } = new();
    public WowAzeriteEssenceState AzeriteEssence { get; } = new();
    public WowEncounterJournalState EncounterJournal { get; } = new();
    public WowMovementControlState Movement { get; } = new();
    public WowTaxiMapState TaxiMap { get; } = new();
    public WowFogOfWarApiState FogOfWar { get; } = new();
    public WowContributionCollectorState ContributionCollector { get; } = new();
    public WowBarberShopState BarberShop { get; } = new();
    public WowTradeInfoState TradeInfo { get; } = new();
    public WowItemSocketInfoState ItemSocketInfo { get; } = new();
    public WowIslandsQueueState IslandsQueue { get; } = new();
    public WowRemixArtifactUiState RemixArtifactUi { get; } = new();
    public WowAreaPoiInfoApiState AreaPoiInfo { get; } = new();
    public WowDeathInfoState DeathInfo { get; } = new();
    public WowDeathRecapState DeathRecap { get; } = new();
    public WowTaskQuestState TaskQuest { get; } = new();
    public WowTransmogSetState TransmogSets { get; } = new();
    public WowGossipInfoState GossipInfo { get; } = new();
    public WowCharacterServicesPublicState CharacterServicesPublic { get; } =
        new();
    public WowSecureExecutionState SecureExecution { get; } = new();
    public WowDataProviders Providers { get; }
    public IWowAtlasProvider? AtlasProvider
    {
        get => Providers.Atlas;
        internal set => Providers.Atlas = value;
    }
    public IWowDyeColorProvider? DyeColorProvider
    {
        get => Providers.DyeColor;
        internal set => Providers.DyeColor = value;
    }
    public IWowGlobalColorProvider? GlobalColorProvider
    {
        get => Providers.GlobalColor;
        internal set => Providers.GlobalColor = value;
    }
    public IWowGameRuleProvider? GameRuleProvider
    {
        get => Providers.GameRule;
        internal set => Providers.GameRule = value;
    }
    public IWowMapProvider? MapProvider
    {
        get => Providers.Map;
        internal set => Providers.Map = value;
    }
    public IWowQuestProvider? QuestProvider
    {
        get => Providers.Quest;
        internal set => Providers.Quest = value;
    }
    public IWowAchievementProvider? AchievementProvider
    {
        get => Providers.Achievement;
        internal set => Providers.Achievement = value;
    }
    public IWowAccountStoreProvider? AccountStoreProvider
    {
        get => Providers.AccountStore;
        internal set => Providers.AccountStore = value;
    }
    public IWowAzeriteEssenceProvider? AzeriteEssenceProvider
    {
        get => Providers.AzeriteEssence;
        internal set => Providers.AzeriteEssence = value;
    }
    public IWowModelInfoProvider? ModelInfoProvider
    {
        get => Providers.ModelInfo;
        internal set => Providers.ModelInfo = value;
    }
    public IWowModelResourceProvider? ModelResourceProvider
    {
        get => Providers.ModelResource;
        internal set => Providers.ModelResource = value;
    }
    public IWowMacroIconProvider? MacroIconProvider
    {
        get => Providers.MacroIcon;
        internal set => Providers.MacroIcon = value;
    }
    public IWowSpellProvider? SpellProvider
    {
        get => Providers.Spell;
        internal set => Providers.Spell = value;
    }
    public IWowCharacterServiceProvider? CharacterServiceProvider
    {
        get => Providers.CharacterService;
        internal set => Providers.CharacterService = value;
    }
    public IWowEncounterJournalProvider? EncounterJournalProvider
    {
        get => Providers.EncounterJournal;
        internal set => Providers.EncounterJournal = value;
    }
    public IWowGlobalStringProvider? GlobalStringProvider
    {
        get => Providers.GlobalString;
        internal set => Providers.GlobalString = value;
    }
    public IWowItemClassProvider? ItemClassProvider
    {
        get => Providers.ItemClass;
        internal set => Providers.ItemClass = value;
    }
    public IWowItemProvider? ItemProvider
    {
        get => Providers.Item;
        internal set => Providers.Item = value;
    }
    public IWowRaceProvider? RaceProvider
    {
        get => Providers.Race;
        internal set => Providers.Race = value;
    }
    public IWowFactionProvider? FactionProvider
    {
        get => Providers.Faction;
        internal set => Providers.Faction = value;
    }
    public IWowTransmogSetProvider? TransmogSetProvider
    {
        get => Providers.TransmogSet;
        internal set => Providers.TransmogSet = value;
    }
    public IWowTransmogAppearanceProvider? TransmogAppearanceProvider
    {
        get => Providers.TransmogAppearance;
        internal set => Providers.TransmogAppearance = value;
    }

    private void OnDataProviderChanged(WowDataProviderKind kind) =>
        ApplyDataProvider(kind);

    private void ApplyDataProvider(WowDataProviderKind kind)
    {
        switch (kind)
        {
            case WowDataProviderKind.GlobalColor when State != 0:
                WowColorApi.ApplyClientGlobals(this);
                break;
            case WowDataProviderKind.EncounterJournal
                when Providers.EncounterJournal is { } journal:
                EncounterJournal.CurrentTierIndex = Math.Clamp(
                    Account.ServerExpansionLevel + 1,
                    1,
                    Math.Max(1, journal.Tiers.Count));
                break;
            case WowDataProviderKind.GlobalString
                when State != 0 && Providers.GlobalString is { } strings:
                foreach (var (name, text) in strings.Strings)
                {
                    lua_pushstring(State, text);
                    lua_setglobal(State, name);
                }
                ApplyMissingGlobalStringPlaceholders(strings);
                break;
            case WowDataProviderKind.Spell:
                Spells.SetProvider(Providers.Spell);
                break;
            case WowDataProviderKind.Item:
                Items.SetProvider(Providers.Item);
                break;
            case WowDataProviderKind.ItemClass when Providers.ItemClass is { } itemClasses:
                Items.Classes.Clear();
                foreach (var (id, name) in itemClasses.Classes)
                    Items.Classes[id] = name;
                Items.SubClasses.Clear();
                foreach (var (key, subClass) in itemClasses.SubClasses)
                    Items.SubClasses[key] = subClass;
                break;
            case WowDataProviderKind.InventorySlot:
                Equipment.SetInventorySlotProvider(Providers.InventorySlot);
                break;
            case WowDataProviderKind.Race when Providers.Race is { } races:
                Classes.Races.Clear();
                foreach (var (id, race) in races.Races)
                    Classes.Races[id] = race;
                break;
        }
    }

    public string SavedVariablesDirectory { get; }
    public string? SavedVariablesFilePath =>
        Manifest is null ? null : AccountSavedVariablesPath(Manifest);
    public IReadOnlyList<AddonManifest> Manifests { get; private set; } = [];
    public IReadOnlyList<AddonManifest> AvailableManifests { get; private set; } = [];
    public IReadOnlyList<AddonManifest> UserManifests { get; private set; } = [];
    internal HashSet<string> DisabledUserAddons { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal bool AddonVersionCheckEnabled
    {
        get => _addonVersionCheckEnabled;
        set
        {
            if (_addonVersionCheckEnabled == value)
                return;
            _addonVersionCheckEnabled = value;
            InvalidateAddonLoadability();
        }
    }
    internal int InterfaceVersion => BuildInfo.InterfaceVersion;
    public WowBuildInfo BuildInfo { get; set; } = WowBuildInfo.Unknown;
    public IReadOnlyDictionary<string, string> AddonLoadErrors => _addonLoadErrors;
    public IReadOnlyList<AddonLoadFailure> AddonLoadFailures => _addonLoadFailures;
    public AddonManifest? Manifest => Manifests.FirstOrDefault();
    public WowFrameTimeState FrameTime { get; } = new();
    public double Time => FrameTime.TimeSeconds;
    public double FrameRate => FrameTime.FrameRate;
    public bool IsLoaded { get; private set; }
    private readonly Dictionary<string, string> _addonLoadErrors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AddonLoadFailure> _addonLoadFailures = [];
    private readonly HashSet<string> _loadedAddonNames = new(StringComparer.OrdinalIgnoreCase);
    public bool ControlDown
    {
        get => Input.ControlDown;
        set => Input.ControlDown = value;
    }
    public bool ShiftDown
    {
        get => Input.ShiftDown;
        set => Input.ShiftDown = value;
    }
    public bool AltDown
    {
        get => Input.AltDown;
        set => Input.AltDown = value;
    }
    internal bool NextTryOnUsesOffHand { get; set; }

    public AddonManifest? GetAddonManifest(string name) =>
        AvailableManifests.FirstOrDefault(value =>
            value.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public bool IsAddonLoaded(string name) => _loadedAddonNames.Contains(name);

    internal bool IsAddonLoading(string name) => _loadingAddonNames.Contains(name);

    internal bool IsSecureAddon(string name) => _secureAddonNames.Contains(name);

    internal bool TryPushAddonLocalTable(lua_State state, string name)
    {
        if (!_addonLocalTableReferences.TryGetValue(name, out var reference))
            return false;

        if (IsSecureAddon(name) && _currentAddonIsInsecure)
            return false;

        lua_rawgeti(state, LUA_REGISTRYINDEX, reference);
        return true;
    }

    internal void SaveAddonEnableState()
    {
        _savedDisabledUserAddons.Clear();
        _savedDisabledUserAddons.UnionWith(DisabledUserAddons);
    }

    internal void ResetDisabledAddonState()
    {
        DisabledUserAddons.Clear();
        DisabledUserAddons.UnionWith(_savedDisabledUserAddons);
        InvalidateAddonLoadability();
    }

    internal void InvalidateAddonLoadability() => _addonLoadability.Clear();

    internal bool TryGetAddonLoadability(
        string name,
        bool demandLoaded,
        out bool loadable,
        out string? reason)
    {
        if (_addonLoadability.TryGetValue(AddonLoadabilityKey(name, demandLoaded), out var value))
        {
            loadable = value.Loadable;
            reason = value.Reason;
            return true;
        }

        loadable = false;
        reason = null;
        return false;
    }

    internal void CacheAddonLoadability(
        string name,
        bool demandLoaded,
        bool loadable,
        string? reason) =>
        _addonLoadability[AddonLoadabilityKey(name, demandLoaded)] = (loadable, reason);

    private static string AddonLoadabilityKey(string name, bool demandLoaded) =>
        $"{(demandLoaded ? '1' : '0')}:{name}";

    internal long NextSoundHandle() => Interlocked.Increment(ref _nextSoundHandle);

    internal bool TryGetXmlTemplateInfo(string name, out XmlTemplateInfo info) =>
        _xmlUiLoader.TryGetTemplateInfo(name, out info);

    internal bool ApplyAtlas(
        UiObject value,
        string atlasName,
        bool useAtlasSize,
        bool resetTexCoords = false,
        string? filterMode = null,
        string? wrapModeHorizontal = null,
        string? wrapModeVertical = null)
    {
        if (AtlasProvider?.TryGetAtlas(atlasName, out var info) != true)
            return false;

        var texture = value.Texture ??= new UiTextureState();
        if (resetTexCoords)
            texture.ResetTexCoord();

        texture.Asset = $"atlas:{atlasName}";
        texture.AtlasName = atlasName;
        texture.AtlasWidth = info.Width;
        texture.AtlasHeight = info.Height;
        texture.SliceData = info.SliceData;
        texture.FileDataId = info.FileDataId;
        texture.IsColor = false;
        texture.Gradient = null;
        texture.SetAtlasRegion(info.Left, info.Right, info.Top, info.Bottom);
        texture.FilterMode = filterMode ?? texture.FilterMode;
        texture.WrapHorizontal = info.TilesHorizontally
            ? "REPEAT"
            : wrapModeHorizontal ?? "CLAMPTOBLACKADDITIVE";
        texture.WrapVertical = info.TilesVertically
            ? "REPEAT"
            : wrapModeVertical ?? "CLAMPTOBLACKADDITIVE";
        if (useAtlasSize)
        {
            value.Width = info.Width;
            value.Height = info.Height;
            Ui.InvalidateLayout();
        }
        return true;
    }

    public void LoadAddon(AddonManifest manifest) => LoadAddons([manifest]);

    public void LoadAddons(
        IReadOnlyList<AddonManifest> manifests,
        int bootstrapAddonCount = 0,
        IReadOnlyList<string>? bootstrapRuntimeFiles = null,
        IReadOnlyList<AddonManifest>? availableManifests = null)
    {
        ThrowIfDisposed();
        ReleaseAddonLocalTableReferences();
        var clampedBootstrapCount = Math.Clamp(bootstrapAddonCount, 0, manifests.Count);
        Manifests = AddonManifestLoadOrder.Order(manifests.Take(clampedBootstrapCount))
            .Concat(AddonManifestLoadOrder.Order(manifests.Skip(clampedBootstrapCount)))
            .ToArray();
        UserManifests = Manifests.Skip(clampedBootstrapCount).ToArray();
        AvailableManifests = AddonManifestGrouping.Apply(Manifests
            .Concat(availableManifests ?? [])
            .GroupBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray());
        InvalidateAddonLoadability();
        _secureAddonNames.Clear();
        _secureAddonNames.UnionWith(
            Manifests.Take(clampedBootstrapCount).Select(value => value.Name));
        if (clampedBootstrapCount > 0)
        {
            _secureAddonNames.UnionWith(
                (availableManifests ?? []).Select(value => value.Name));
        }
        DisabledUserAddons.Clear();
        var initiallyLoadedNames = Manifests
            .Select(value => value.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var manifest in AvailableManifests)
        {
            if (!initiallyLoadedNames.Contains(manifest.Name) &&
                !IsSecureAddon(manifest.Name) &&
                !IsTrueMetadata(manifest, "LoadOnDemand"))
            {
                DisabledUserAddons.Add(manifest.Name);
            }
        }
        SaveAddonEnableState();
        _addonLoadErrors.Clear();
        _addonLoadFailures.Clear();
        _loadedAddonNames.Clear();
        _loadingAddonNames.Clear();
        IsLoaded = false;

        ApplyDocumentedApiValues(AvailableManifests);

        foreach (var file in bootstrapRuntimeFiles ?? [])
            ExecuteFile(file);

        var useBootstrapFallback = bootstrapAddonCount > 0;
        PlayerScript.CinematicStateFlags |= 0x8;
        if (useBootstrapFallback)
        {
            ExecuteString(
                "__WoWAddonLabBootstrapFallbackEnabled = true; " +
                $"__WoWAddonLabBootstrapStringFallbackEnabled = " +
                $"{(GlobalStringProvider is null ? "true" : "false")}",
                "@bootstrap-fallback-on");
        }
        try
        {
            for (var manifestIndex = 0; manifestIndex < Manifests.Count; manifestIndex++)
            {
                if (manifestIndex == clampedBootstrapCount)
                {
                    if (useBootstrapFallback)
                    {
                        ExecuteString(
                            "__WoWAddonLabBootstrapFallbackEnabled = false; " +
                            "__WoWAddonLabBootstrapStringFallbackEnabled = false",
                            "@bootstrap-fallback-off");
                    }

                    LinkStringMetatable();
                }

                var manifest = Manifests[manifestIndex];
                LoadProgress?.Invoke(new AddonLoadProgress(
                    manifest.Name,
                    manifestIndex,
                    Manifests.Count));
                LoadManifest(manifest);
                LoadProgress?.Invoke(new AddonLoadProgress(
                    manifest.Name,
                    manifestIndex + 1,
                    Manifests.Count));
            }

            TriggerEditModeLayoutsUpdated();
            TriggerEvent("VARIABLES_LOADED");
            TriggerEvent("UPDATE_CHAT_WINDOWS");
            PlayerScript.CinematicStateFlags &= 0xFFF7;
            TriggerEvent("PLAYER_LOGIN");
            TriggerEvent("PLAYER_ENTERING_WORLD", true, false);
            IsLoaded = true;
            Log.Info("loader", $"Started {Manifests.Count} addons under {GetVersion()}.");
        }
        finally
        {
            PlayerScript.CinematicStateFlags &= 0xFFF7;
            if (useBootstrapFallback)
            {
                ExecuteString(
                    "__WoWAddonLabBootstrapFallbackEnabled = false; " +
                    "__WoWAddonLabBootstrapStringFallbackEnabled = false",
                    "@bootstrap-fallback-finally");
            }
        }
    }

    private static readonly string[] InterfaceGlobalStringTags =
    [
        "BLOCK_REDUCED",
        "OPTION_TOOLTIP_SOCIAL_DISCONNECT_DISCORD",
        "OPTION_TOOLTIP_SOCIAL_DISCORD_DISPLAY_NAME",
        "OPTION_TOOLTIP_SOCIAL_DISCORD_DISPLAY_NAME_OPTION_DEFAULT",
        "OPTION_TOOLTIP_SOCIAL_DISCORD_DISPLAY_NAME_OPTION_GLOBAL_NAME",
        "OPTION_TOOLTIP_SOCIAL_DISCORD_DISPLAY_NAME_OPTION_LAST_ONLINE",
        "OPTION_TOOLTIP_SOCIAL_ENABLE_DISCORD_FUNCTIONALITY",
        "SOCIAL_DISCORD_DISCONNECT",
        "SOCIAL_DISCORD_DISPLAY_NAME",
        "SOCIAL_DISCORD_DISPLAY_NAME_OPTION_DEFAULT",
        "SOCIAL_DISCORD_DISPLAY_NAME_OPTION_GLOBAL_NAME",
        "SOCIAL_DISCORD_DISPLAY_NAME_OPTION_LAST_ONLINE",
        "SOCIAL_ENABLE_DISCORD_FUNCTIONALITY"
    ];

    private void ApplyMissingGlobalStringPlaceholders(IWowGlobalStringProvider strings)
    {
        var missing = InterfaceGlobalStringTags
            .Where(tag => !strings.Strings.ContainsKey(tag))
            .ToArray();
        if (missing.Length == 0)
            return;

        foreach (var tag in missing)
        {
            lua_pushstring(State, tag);
            lua_setglobal(State, tag);
        }
        Log.Warn(
            "loader",
            $"The selected build's GlobalStrings table is missing {missing.Length} tag(s) the " +
            $"Blizzard interface uses as text; the tag name is shown instead: " +
            string.Join(", ", missing));
    }

    private void LinkStringMetatable() => ExecuteString(
        "local mt=debug.getmetatable(''); if mt then mt.__index=string end",
        "@string-metatable");

    private void ApplyDocumentedApiValues(IEnumerable<AddonManifest> manifests)
    {
        var manifestList = manifests.ToArray();
        var documentedEnums = WowEnumDocumentation.Read(manifestList);
        var documentedConstants = WowEnumDocumentation.ReadConstants(manifestList);
        EventUtils.ValidEvents.Clear();
        EventUtils.ValidEvents.UnionWith(WowEventDocumentation.Read(manifestList));
        EventUtils.ValidEvents.Add("FRAMES_LOADED");
        var enumFieldCount = ApplyDocumentedTables("Enum", documentedEnums);
        var constantFieldCount = ApplyDocumentedTables("Constants", documentedConstants);
        if (enumFieldCount + constantFieldCount > 0)
        {
            Log.Info(
                "compatibility",
                $"Loaded {enumFieldCount} documented enum values and {constantFieldCount} " +
                $"documented constants from client API data.");
        }
    }

    private int ApplyDocumentedTables<T>(
        string globalName,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, T>> tables)
    {
        if (tables.Count == 0)
            return 0;

        lua_getglobal(State, globalName);
        if (lua_type(State, -1) != LUA_TTABLE)
        {
            lua_pop(State, 1);
            return 0;
        }

        var fieldCount = 0;
        foreach (var (tableName, fields) in tables)
        {
            lua_getfield(State, -1, tableName);
            if (lua_type(State, -1) != LUA_TTABLE)
            {
                lua_pop(State, 1);
                lua_newtable(State);
                lua_pushvalue(State, -1);
                lua_setfield(State, -3, tableName);
            }

            foreach (var (fieldName, value) in fields)
            {
                PushDocumentedValue(value);
                lua_setfield(State, -2, fieldName);
                fieldCount++;
            }

            lua_pop(State, 1);
        }

        lua_pop(State, 1);
        return fieldCount;
    }

    private void PushDocumentedValue<T>(T value)
    {
        switch (value)
        {
            case bool boolean:
                lua_pushboolean(State, boolean ? 1 : 0);
                break;
            case string text:
                lua_pushstring(State, text);
                break;
            case IConvertible convertible:
                lua_pushnumber(
                    State,
                    convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture));
                break;
            default:
                lua_pushnil(State);
                break;
        }
    }

    public bool TryLoadAddon(string addonName, out string? reason)
    {
        ThrowIfDisposed();
        reason = null;
        if (IsAddonLoaded(addonName))
            return true;
        var manifest = GetAddonManifest(addonName);
        if (manifest is null)
        {
            reason = "MISSING";
            return false;
        }
        return TryLoadAddon(manifest, [], out reason);
    }

    private bool TryLoadAddon(
        AddonManifest manifest,
        HashSet<string> loading,
        out string? reason)
    {
        reason = null;
        if (IsAddonLoaded(manifest.Name))
            return true;
        if (!loading.Add(manifest.Name))
        {
            reason = "DEP_CYCLIC";
            return false;
        }

        try
        {
            foreach (var dependencyName in RequiredDependencies(manifest))
            {
                var dependency = GetAddonManifest(dependencyName);
                if (dependency is null)
                {
                    reason = "DEP_MISSING";
                    return false;
                }
                if (!TryLoadAddon(dependency, loading, out reason))
                    return false;
            }

            try
            {
                LoadManifest(manifest);
            }
            catch (Exception exception)
            {
                RecordLoadFailure(manifest, manifest.TocPath, "load", exception);
                Log.Error("loader", $"{manifest.Name} failed to load on demand: {exception.Message}");
                reason = "LOAD_FAILED";
                return false;
            }

            if (_addonLoadErrors.ContainsKey(manifest.Name))
            {
                reason = "LOAD_FAILED";
                return false;
            }
            return true;
        }
        finally
        {
            loading.Remove(manifest.Name);
        }
    }

    private void LoadManifest(AddonManifest manifest)
    {
        if (IsAddonLoaded(manifest.Name))
            return;

        using var manifestSpan = StartupTimeline.Begin(
            manifest.Name,
            "addon",
            $"{manifest.Files.Count} TOC entries");
        var previousEnvironmentReference = _currentAddonEnvironmentReference;
        var previousAddonName = _currentAddonName;
        var previousAddonIsInsecure = _currentAddonIsInsecure;
        _currentAddonEnvironmentReference = IsTrueMetadata(manifest, "UseSecureEnvironment")
            ? EnsureSecureEnvironment()
            : null;
        _currentAddonName = manifest.Name;
        _currentAddonIsInsecure = !IsSecureAddon(manifest.Name);
        _loadingAddonNames.Add(manifest.Name);
        var loadSavedFirst = IsTrueMetadata(manifest, "LoadSavedVariablesFirst");
        try
        {
            using (StartupTimeline.Begin("bindings", "addon-phase"))
            {
                WowBindingsXmlLoader.Load(this, Path.Combine(manifest.RootPath, "Bindings.xml"));
                WowBindingsXmlLoader.LoadDefaults(
                    this,
                    Path.Combine(manifest.RootPath, "DefaultBindings.wtf"));
            }
            if (loadSavedFirst)
            {
                using (StartupTimeline.Begin("saved variables", "addon-phase"))
                    LoadSavedVariables(manifest);
            }

            lua_newtable(State);
            var privateTableReference = luaL_ref(State, LUA_REGISTRYINDEX);
            var exposesPrivateTable = IsTrueMetadata(manifest, "AllowAddOnTableAccess");
            if (exposesPrivateTable)
                SetAddonLocalTableReference(manifest.Name, privateTableReference);
            try
            {
                var secureEnvironmentFiles = SecureEnvironmentFiles(manifest);
                using (StartupTimeline.Begin("TOC files", "addon-phase"))
                {
                    foreach (var file in manifest.Files)
                    {
                        if (!File.Exists(file))
                            throw new FileNotFoundException($"TOC file entry was not found: {file}");

                        var fileEnvironmentReference = secureEnvironmentFiles.Contains(file)
                            ? EnsureSecureEnvironment()
                            : _currentAddonEnvironmentReference;
                        var restoreEnvironmentReference = SwapAddonEnvironment(fileEnvironmentReference);
                        try
                        {
                            var extension = Path.GetExtension(file);
                            if (extension.Equals(".lua", StringComparison.OrdinalIgnoreCase))
                            {
                                TryExecuteAddonFile(file, manifest, privateTableReference);
                                continue;
                            }

                            if (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                TryExecuteXmlFile(file, manifest, privateTableReference, []);
                                continue;
                            }

                            Log.Warn(
                                "loader",
                                $"Unsupported TOC entry was skipped: {Path.GetRelativePath(manifest.RootPath, file)}.");
                        }
                        finally
                        {
                            SwapAddonEnvironment(restoreEnvironmentReference);
                        }
                    }
                }
            }
            finally
            {
                if (!exposesPrivateTable)
                    luaL_unref(State, LUA_REGISTRYINDEX, privateTableReference);
            }

            if (!loadSavedFirst)
            {
                using (StartupTimeline.Begin("saved variables", "addon-phase"))
                    LoadSavedVariables(manifest);
            }
            using (StartupTimeline.Begin("mixins and initial lifecycle", "addon-phase"))
            {
                var resolvedMixinObjects = ResolvePendingGlobalMixins();
                _xmlUiLoader.InvokeDeferredInitialLifecycle(resolvedMixinObjects);
            }
            _loadedAddonNames.Add(manifest.Name);
            using (StartupTimeline.Begin("ADDON_LOADED", "addon-phase"))
            {
                TriggerEvent(
                    "ADDON_LOADED",
                    manifest.Name,
                    File.Exists(Path.Combine(manifest.RootPath, "Bindings.xml")));
            }
            Log.Info("loader", $"Loaded files for {manifest.Name}: {manifest.Files.Count} TOC entries.");
        }
        finally
        {
            _loadingAddonNames.Remove(manifest.Name);
            _currentAddonEnvironmentReference = previousEnvironmentReference;
            _currentAddonName = previousAddonName;
            _currentAddonIsInsecure = previousAddonIsInsecure;
        }
    }

    private void SetAddonLocalTableReference(string addonName, int reference)
    {
        if (_addonLocalTableReferences.Remove(addonName, out var previousReference))
            luaL_unref(State, LUA_REGISTRYINDEX, previousReference);
        _addonLocalTableReferences[addonName] = reference;
    }

    private void ReleaseAddonLocalTableReferences()
    {
        foreach (var reference in _addonLocalTableReferences.Values)
            luaL_unref(State, LUA_REGISTRYINDEX, reference);
        _addonLocalTableReferences.Clear();
    }

    public void ExecuteFile(string path)
    {
        var status = LoadLuaFile(path);
        if (status != 0)
            throw BuildLuaException($"compile {path}");

        status = lua_pcall(State, 0, LUA_MULTRET, 0);
        if (status != 0)
            throw BuildLuaException($"execute {path}");

        lua_settop(State, 0);
        Log.Write(EmulatorLogLevel.Trace, "lua", $"Executed {path}.");
    }

    private void ExecuteAddonFile(string path, string addonName, int privateTableReference)
    {
        var status = LoadLuaFile(path);
        if (status != 0)
            throw BuildLuaException($"compile {path}");
        ApplyCurrentAddonEnvironment(-1);

        lua_pushstring(State, addonName);
        lua_rawgeti(State, LUA_REGISTRYINDEX, privateTableReference);
        status = lua_pcall(State, 2, LUA_MULTRET, 0);
        if (status != 0)
            throw BuildLuaException($"execute {path}");

        lua_settop(State, 0);
        Log.Write(EmulatorLogLevel.Trace, "lua", $"Executed {path} for {addonName}.");
    }

    private void ExecuteAddonChunk(
        string code,
        string chunkName,
        string addonName,
        int privateTableReference)
    {
        var status = LuaChunkLoader.Load(State, code, chunkName);
        if (status != 0)
            throw BuildLuaException($"compile {chunkName}");
        ApplyCurrentAddonEnvironment(-1);

        lua_pushstring(State, addonName);
        lua_rawgeti(State, LUA_REGISTRYINDEX, privateTableReference);
        status = lua_pcall(State, 2, LUA_MULTRET, 0);
        if (status != 0)
            throw BuildLuaException($"execute {chunkName}");

        lua_settop(State, 0);
    }

    private int LoadLuaFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var offset = bytes.Length >= 3 &&
                     bytes[0] == 0xEF &&
                     bytes[1] == 0xBB &&
                     bytes[2] == 0xBF
            ? 3
            : 0;
        var chunkName = $"@{path}";
        LuaBytecodeCache.CacheKey? cacheKey = null;
        if (_bytecodeCache is not null)
        {
            cacheKey = _bytecodeCache.KeyFor(path, bytes);
            if (_bytecodeCache.TryRead(cacheKey.Value, out var bytecode))
            {
                var cachedStatus = LuaChunkLoader.Load(
                    State,
                    bytecode,
                    0,
                    bytecode.Length,
                    chunkName);
                if (cachedStatus == 0)
                    return 0;
                lua_pop(State, 1);
            }
        }

        var compileStarted = Stopwatch.GetTimestamp();
        var status = LuaChunkLoader.Load(
            State,
            bytes,
            offset,
            bytes.Length - offset,
            chunkName);
        var compileMilliseconds = Stopwatch.GetElapsedTime(compileStarted).TotalMilliseconds;
        if (status == 0 &&
            cacheKey is not null &&
            compileMilliseconds >= 5 &&
            LuaChunkLoader.Dump(State) is { } compiled)
        {
            _bytecodeCache!.Write(cacheKey.Value, compiled);
        }
        return status;
    }

    private void TryExecuteAddonFile(
        string path,
        AddonManifest manifest,
        int privateTableReference)
    {
        using var span = StartupTimeline.Begin(
            Path.GetRelativePath(manifest.RootPath, path),
            "lua-file",
            manifest.Name);
        try
        {
            if (Environment.GetEnvironmentVariable("WOW_ADDON_LAB_TRACE_LOAD") == "1")
                Console.Error.WriteLine(
                    $"LOAD {manifest.Name}/{Path.GetRelativePath(manifest.RootPath, path)}");
            ExecuteAddonFile(path, manifest.Name, privateTableReference);
        }
        catch (Exception exception)
        {
            RecordLoadFailure(manifest, path, "lua", exception);
            Log.Error(
                "loader",
                $"{manifest.Name}/{Path.GetRelativePath(manifest.RootPath, path)} failed; continuing addon load: {exception.Message}");
        }
    }

    private void TryExecuteXmlFile(
        string path,
        AddonManifest manifest,
        int privateTableReference,
        HashSet<string> includeStack)
    {
        var fullPath = Path.GetFullPath(path);
        if (!includeStack.Add(fullPath))
        {
            Log.Warn("loader", $"Recursive XML include was skipped: {fullPath}");
            return;
        }

        using var span = StartupTimeline.Begin(
            Path.GetRelativePath(manifest.RootPath, fullPath),
            "xml-file",
            manifest.Name);
        try
        {
            if (Environment.GetEnvironmentVariable("WOW_ADDON_LAB_TRACE_LOAD") == "1")
                Console.Error.WriteLine(
                    $"LOAD {manifest.Name}/{Path.GetRelativePath(manifest.RootPath, fullPath)}");
            var document = WowXmlDocument.Load(fullPath);
            var directory = Path.GetDirectoryName(fullPath)!;
            foreach (var element in document.Root?.Elements() ?? [])
            {
                var elementName = element.Name.LocalName;
                if (!elementName.Equals("Script", StringComparison.OrdinalIgnoreCase) &&
                    !elementName.Equals("Include", StringComparison.OrdinalIgnoreCase))
                {
                    _xmlUiLoader.ProcessTopLevel(element, manifest, fullPath);
                    continue;
                }
                var relative = element.Attributes()
                    .FirstOrDefault(value => value.Name.LocalName.Equals("file", StringComparison.OrdinalIgnoreCase))
                    ?.Value;
                if (string.IsNullOrWhiteSpace(relative))
                {
                    if (elementName.Equals("Script", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(element.Value))
                    {
                        ExecuteAddonChunk(
                            element.Value,
                            $"@{fullPath}:Script",
                            manifest.Name,
                            privateTableReference);
                    }
                    continue;
                }
                var included = ResolveAddonInclude(manifest, directory, relative);
                if (Path.GetExtension(included).Equals(".lua", StringComparison.OrdinalIgnoreCase))
                    TryExecuteAddonFile(included, manifest, privateTableReference);
                else if (Path.GetExtension(included).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                    TryExecuteXmlFile(included, manifest, privateTableReference, includeStack);
                else
                    Log.Warn("loader", $"Unsupported XML include type was skipped: {included}");
            }
            Log.Write(
                EmulatorLogLevel.Trace,
                "loader",
                $"Processed XML {Path.GetRelativePath(manifest.RootPath, fullPath)}.");
        }
        catch (Exception exception)
        {
            RecordLoadFailure(manifest, fullPath, "xml", exception);
            Log.Error(
                "loader",
                $"{manifest.Name}/{Path.GetRelativePath(manifest.RootPath, fullPath)} failed: {exception.Message}");
        }
        finally
        {
            includeStack.Remove(fullPath);
        }
    }

    private static string ResolveAddonInclude(
        AddonManifest manifest,
        string directory,
        string relative)
    {
        var path = Path.GetFullPath(Path.Combine(
            directory,
            relative.Replace('\\', Path.DirectorySeparatorChar)));
        var root = manifest.RootPath.TrimEnd(
                       Path.DirectorySeparatorChar,
                       Path.AltDirectorySeparatorChar) +
                   Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"XML include escapes the addon directory: {relative}");
        if (!File.Exists(path))
        {
            var rootRelative = Path.GetFullPath(Path.Combine(
                manifest.RootPath,
                relative.Replace('\\', Path.DirectorySeparatorChar)));
            if (rootRelative.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                path = rootRelative;
        }
        if (!File.Exists(path))
            throw new FileNotFoundException("XML include was not found.", path);
        return path;
    }

    public string Evaluate(string expression)
    {
        var code = expression.TrimStart().StartsWith("return ", StringComparison.Ordinal)
            ? expression
            : $"return {expression}";
        var baseline = lua_gettop(State);
        var status = luaL_loadstring(State, code);
        if (status != 0)
        {
            lua_settop(State, baseline);
            code = expression;
            status = luaL_loadstring(State, code);
        }

        if (status != 0)
            throw BuildLuaException("compile expression");
        status = lua_pcall(State, 0, LUA_MULTRET, 0);
        if (status != 0)
            throw BuildLuaException("evaluate expression");

        var top = lua_gettop(State);
        var values = new List<string>();
        for (var index = baseline + 1; index <= top; index++)
            values.Add(FormatValue(index));
        lua_settop(State, baseline);
        return string.Join("\t", values);
    }

    public void Tick(double deltaSeconds)
    {
        if (_disposed)
            return;

        var delta = Math.Clamp(deltaSeconds, 0, 0.25);
        FrameTime.Advance(delta);
        FlushPendingScrollChildRects();
        FlushPendingSizeChanged();
        RunDeferredScripts();
        RunTimers();
        WowPlayerScriptApi.Tick(this);
        WowWowLabsDataManagerApi.Tick(this);
        TickAnimations(delta);
        LuaBindings.TickSimpleModelSequences(this, delta);
        TickStatusBars(delta);
        TickCooldowns();
        TickTooltips(delta);
        WowDurationTextBindingApi.Tick(this, delta);
        LuaBindings.TickColorSelects(this);

        var frames = Ui.Objects.Values
            .Where(value =>
                value.AnimationGroup is null &&
                value.Animation is null &&
                value.ScriptReferences.ContainsKey("OnUpdate") &&
                ShouldRunOnUpdate(value))
            .ToArray();
        foreach (var frame in frames)
        {
            if (frame.OnUpdateMode is UiOnUpdateMode.RunWhenVisibleOnce or UiOnUpdateMode.RunOnce)
                frame.OnUpdateMode = UiOnUpdateMode.Disabled;
            InvokeScript(frame, "OnUpdate", delta);
        }

        FlushPendingEditBoxTextChanges();

        LuaBindings.TickMinimaps(this);
        LuaBindings.TickFogOfWarFrames(this);
        LuaBindings.TickUnitPositionFrames(this);
        LuaBindings.TickMessageFrames(this, (float)delta);
        LuaBindings.UpdateSimpleModelTransforms(this);
    }

    private bool ShouldRunOnUpdate(UiObject value) => value.OnUpdateMode switch
    {
        UiOnUpdateMode.Disabled => false,
        UiOnUpdateMode.RunAlways or UiOnUpdateMode.RunOnce => true,
        _ => Ui.IsVisible(value)
    };

    public void QueueEditBoxTextChanged(UiObject value, bool userInput)
    {
        value.EditBoxTextChangedPending = true;
        value.EditBoxTextChangedByUser = userInput;
    }

    private void FlushPendingEditBoxTextChanges()
    {
        var editBoxes = Ui.Objects.Values
            .Where(value => value.EditBoxTextChangedPending)
            .ToArray();
        foreach (var value in editBoxes)
            FlushPendingEditBoxTextChange(value);
    }

    public void FlushPendingEditBoxTextChange(UiObject value)
    {
        if (!value.EditBoxTextChangedPending)
            return;
        value.EditBoxTextChangedPending = false;
        var userInput = value.EditBoxTextChangedByUser;
        value.EditBoxTextChangedByUser = false;
        InvokeScript(value, "OnTextChanged", userInput);
    }

    private void TickStatusBars(double delta)
    {
        foreach (var value in Ui.Objects.Values)
        {
            if (value.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase) ||
                value.StatusBar is not { } statusBar ||
                statusBar.TextureId is not { } textureId ||
                Ui.Find(textureId) is null ||
                statusBar.TimerDuration is null &&
                (!statusBar.RangeInitialized || !statusBar.ValueInitialized))
            {
                continue;
            }

            var target = StatusBarTargetNormalized(statusBar);
            if (statusBar.InterpolationActive)
            {
                if (Math.Abs(statusBar.DisplayNormalizedValue - target) < 0.001)
                {
                    statusBar.InterpolationActive = false;
                    statusBar.DisplayNormalizedValue = target;
                }
                else
                {
                    var elapsed = (float)delta;
                    var current = (float)statusBar.DisplayNormalizedValue;
                    var normalizedTarget = (float)target;
                    statusBar.DisplayNormalizedValue =
                        normalizedTarget -
                        MathF.Exp(-18f * elapsed) * (normalizedTarget - current);
                }
            }
            else
            {
                statusBar.DisplayNormalizedValue = target;
            }
        }
    }

    private void TickCooldowns()
    {
        foreach (var value in Ui.Objects.Values.ToArray())
        {
            if (value.Cooldown is not { } cooldown)
                continue;
            if (cooldown.StartTimeMilliseconds == 0 &&
                cooldown.DisplayDurationMilliseconds == 0)
            {
                LuaBindings.HideCooldownFontString(this, cooldown);
                continue;
            }

            var clock = LuaBindings.CooldownClockMilliseconds(
                this,
                cooldown.UsesUnixClock);
            if (cooldown.Paused)
            {
                cooldown.StartTimeMilliseconds = unchecked(
                    clock - cooldown.PausedElapsedMilliseconds);
            }

            cooldown.ElapsedDisplayMilliseconds =
                LuaBindings.CooldownElapsedDisplayMilliseconds(clock, cooldown);
            UpdateCooldownCountdownText(value, cooldown);
            if (cooldown.DisplayDurationMilliseconds <= 0 ||
                cooldown.ElapsedDisplayMilliseconds <
                cooldown.DisplayDurationMilliseconds ||
                cooldown.Paused)
            {
                continue;
            }

            if (cooldown.CompletionBlingActive)
            {
                LuaBindings.ClearCooldownState(cooldown);
                LuaBindings.HideCooldownFontString(this, cooldown);
                continue;
            }

            var canBling =
                cooldown.DrawBling &&
                (cooldown.BlingTextureFileDataId is not null ||
                 !string.IsNullOrEmpty(cooldown.BlingTextureAsset));
            if (canBling)
            {
                cooldown.CompletionBlingActive = true;
                cooldown.StartTimeMilliseconds = clock;
                cooldown.DisplayDurationMilliseconds = 1_000;
                cooldown.ModRate = 1;
                cooldown.ElapsedDisplayMilliseconds = 0;
            }
            else
            {
                LuaBindings.ClearCooldownState(cooldown);
            }

            InvokeScript(value, "OnCooldownDone");
        }
    }

    private void UpdateCooldownCountdownText(
        UiObject owner,
        UiCooldownState cooldown)
    {
        var duration = unchecked((uint)cooldown.DisplayDurationMilliseconds);
        var elapsed = unchecked((uint)cooldown.ElapsedDisplayMilliseconds);
        var minimum = unchecked(
            (uint)cooldown.MinimumCountdownDurationMilliseconds);
        if (cooldown.HideCountdownNumbers ||
            cooldown.CompletionBlingActive ||
            duration == 0 ||
            elapsed >= duration ||
            duration <= minimum)
        {
            LuaBindings.HideCooldownFontString(this, cooldown);
            return;
        }

        var remaining = duration - elapsed;
        if (remaining == 0)
        {
            LuaBindings.HideCooldownFontString(this, cooldown);
            return;
        }

        var fontString =
            LuaBindings.EnsureCooldownFontString(this, owner, cooldown);
        LuaBindings.ApplyCooldownFont(this, cooldown);
        var millisecondsThreshold = unchecked(
            (uint)cooldown.CountdownMillisecondsThreshold);
        if (remaining >= millisecondsThreshold &&
            !cooldown.UseAuraDisplayTime)
        {
            remaining = 1_000 * ((remaining - 1) / 1_000 + 1);
        }

        var abbreviationThreshold = unchecked(
            (uint)cooldown.CountdownAbbreviationThresholdMilliseconds);
        string text;
        if (abbreviationThreshold >= 3_600_000 ||
            remaining < 60_000 ||
            remaining >= abbreviationThreshold)
        {
            text = remaining >= millisecondsThreshold
                ? FormatCooldownDuration(remaining, cooldown.UseAuraDisplayTime)
                : (remaining * 0.001).ToString("0.0", CultureInfo.InvariantCulture);
        }
        else
        {
            text = string.Create(
                CultureInfo.InvariantCulture,
                $"{remaining / 60_000}:{remaining % 60_000 / 1_000:00}");
        }

        fontString.Font ??= new UiFontState();
        fontString.Font.Text = text;
        fontString.Shown = true;
        Ui.InvalidateLayout();
    }

    private static string FormatCooldownDuration(
        uint remainingMilliseconds,
        bool useAuraDisplayTime)
    {
        var seconds = useAuraDisplayTime
            ? remainingMilliseconds / 1_000
            : (remainingMilliseconds - 1) / 1_000 + 1;
        var thresholds = new uint[] { 86_400, 3_600, 60, 0 };
        var divisors = new uint[] { 86_400, 3_600, 60, 1 };
        var nextDivisors = new uint[] { 3_600, 60, 1, 1 };
        var suffixes = new[] { "d", "h", "m", string.Empty };
        var unit = 0;
        for (; unit < thresholds.Length; unit++)
        {
            if (seconds >= thresholds[unit])
                break;
            if (!useAuraDisplayTime &&
                seconds > 0 &&
                (seconds - 1) / nextDivisors[unit] + 1 >=
                thresholds[unit] / nextDivisors[unit])
            {
                break;
            }
        }
        unit = Math.Min(unit, divisors.Length - 1);
        var value = seconds == 0
            ? 0
            : useAuraDisplayTime
                ? seconds / divisors[unit]
                : (seconds - 1) / divisors[unit] + 1;
        return value.ToString(CultureInfo.InvariantCulture) + suffixes[unit];
    }

    private void TickTooltips(double delta)
    {
        foreach (var value in Ui.Objects.Values)
        {
            if (value.Tooltip is not { FadeRemaining: > 0 } tooltip)
                continue;

            tooltip.FadeRemaining = Math.Max(0, tooltip.FadeRemaining - (float)delta);
            value.Alpha = Math.Clamp(tooltip.FadeRemaining / 2, 0, 1);
            if (tooltip.FadeRemaining > 0)
                continue;
            value.Alpha = 1;
            LuaBindings.SetShown(this, value, false);
        }
    }

    internal double StatusBarTargetNormalized(UiStatusBarState statusBar)
    {
        if (statusBar.TimerDuration is { } timer)
        {
            if (timer.Duration == 0)
                return statusBar.FillStyle == 1 ? 1 : 0;

            var elapsed = (Time - timer.StartTime) * timer.ModRate;
            var progress = Math.Clamp(elapsed / timer.Duration, 0, 1);
            return statusBar.TimerDirection == 1 ? 1 - progress : progress;
        }

        if (!statusBar.RangeInitialized)
            return statusBar.FillStyle == 1 ? 1 : 0;
        var range = statusBar.Maximum - statusBar.Minimum;
        if (Math.Abs(range) < 0.001)
            return statusBar.FillStyle == 1 ? 1 : 0;
        var value = statusBar.ValueInitialized
            ? statusBar.Value
            : statusBar.Minimum;
        return Math.Clamp((value - statusBar.Minimum) / range, 0, 1);
    }

    internal void QueueSizeChanged(UiObject value) => _pendingSizeChanged.Add(value.Id);

    internal void QueueScrollChildRect(UiObject value) =>
        _pendingScrollChildRects.Add(value.Id);

    internal void FlushPendingScrollChildRects()
    {
        var pending = _pendingScrollChildRects.ToArray();
        _pendingScrollChildRects.Clear();
        foreach (var id in pending)
        {
            if (Ui.Find(id) is { } value)
                LuaBindings.UpdateScrollChildRect(this, value);
        }
    }

    internal void FlushPendingSizeChanged()
    {
        if (_flushingSizeChanged)
            return;

        _flushingSizeChanged = true;
        try
        {
            var pending = _pendingSizeChanged.ToArray();
            _pendingSizeChanged.Clear();
            foreach (var id in pending)
            {
                var value = Ui.Find(id);
                if (value is null || !value.ScriptReferences.ContainsKey("OnSizeChanged"))
                    continue;
                var bounds = Ui.ResolveBounds(id);
                if (bounds.Width <= 0 && bounds.Height <= 0)
                    continue;
                var scale = Ui.LayoutScale(value);
                var divisor = MathF.Abs(scale) < 0.000001f ? 1 : scale;
                var size = new Vector2(bounds.Width / divisor, bounds.Height / divisor);
                if (_lastNotifiedSizes.TryGetValue(id, out var previous) &&
                    Vector2.DistanceSquared(previous, size) < 0.000001f)
                    continue;
                _lastNotifiedSizes[id] = size;
                InvokeScript(value, "OnSizeChanged", size.X, size.Y);
            }
        }
        finally
        {
            _flushingSizeChanged = false;
        }
    }

    public string? TryEvaluate(string expression)
    {
        try
        {
            return Evaluate(expression);
        }
        catch
        {
            return null;
        }
    }

    public void SaveVariables()
    {
        if (Manifests.Count == 0)
            return;

        foreach (var manifest in AvailableManifests.Where(value =>
                     IsAddonLoaded(value.Name) && value.SavedVariables.Count > 0))
        {
            SaveVariables(
                manifest.AccountSavedVariables,
                AccountSavedVariablesPath(manifest),
                "account");
            SaveVariables(
                manifest.CharacterSavedVariables,
                CharacterSavedVariablesPath(manifest),
                "character");
        }
    }

    public void ImportSavedVariables(string sourcePath)
    {
        if (Manifest is null)
            throw new InvalidOperationException("No addon has been loaded.");

        var source = Path.GetFullPath(sourcePath);
        if (!File.Exists(source))
            throw new FileNotFoundException("SavedVariables source was not found.", source);

        ExecuteFile(source);
        SaveVariables();
        Log.Info("saved-variables", $"Imported {Manifest.Name} SavedVariables from {source}.");
    }

    public void TriggerEvent(string eventName, params object?[] arguments)
    {
        using var eventSpan = StartupTimeline.Begin(eventName, "event");
        var eventUnit = arguments.FirstOrDefault() as string;
        var scriptArguments = new object?[arguments.Length + 1];
        scriptArguments[0] = eventName;
        arguments.CopyTo(scriptArguments, 1);
        var targetIds = EnumerateEventTargetIds(eventName).ToArray();
        foreach (var targetId in targetIds)
        {
            if (Ui.Find(targetId) is not { } target ||
                !target.AllEventsRegistered &&
                !target.Events.Contains(eventName) &&
                !target.EventCallbackReferences.ContainsKey(eventName))
            {
                continue;
            }

            var dispatchScript = target.AllEventsRegistered || target.Events.Contains(eventName);
            if (dispatchScript &&
                target.RegisteredUnitEvents.TryGetValue(eventName, out var units) &&
                units.Count > 0)
            {
                if (eventUnit is null ||
                    !units.Contains(eventUnit, StringComparer.OrdinalIgnoreCase))
                {
                    dispatchScript = false;
                }
            }

            if (dispatchScript)
            {
                using var callbackSpan = StartupTimeline.Begin(
                    target.Name ?? $"object {target.Id}",
                    "event-callback",
                    EventCallbackDetail(target));
                InvokeScript(target, "OnEvent", scriptArguments);
            }
            if (target.EventCallbackReferences.TryGetValue(eventName, out var callbacks))
            {
                foreach (var callback in callbacks.ToArray())
                {
                    if (callback.Units.Count > 0)
                    {
                        if (eventUnit is null ||
                            !callback.Units.Contains(eventUnit, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }
                    using var callbackSpan = StartupTimeline.Begin(
                        target.Name ?? $"object {target.Id}",
                        "event-callback",
                        EventCallbackDetail(target));
                    InvokeReference(callback.Reference, target, arguments);
                }
            }
        }
        if (_globalEventCallbacks.TryGetValue(eventName, out var globalCallbacks))
        {
            object?[]? globalArguments = null;
            foreach (var callback in globalCallbacks.ToArray())
            {
                if (callback.Unit is not null &&
                    eventUnit?.Equals(
                        callback.Unit,
                        StringComparison.OrdinalIgnoreCase) != true)
                {
                    continue;
                }
                globalArguments ??= new object?[] { null }.Concat(arguments).ToArray();
                using var callbackSpan = StartupTimeline.Begin(
                    "global callback",
                    "event-callback");
                InvokeReference(
                    callback.Reference,
                    null,
                    globalArguments);
            }
        }
    }

    internal void IndexEventTarget(UiObject value, string eventName)
    {
        if (!_eventTargetIds.TryGetValue(eventName, out var targetIds))
        {
            targetIds = [];
            _eventTargetIds.Add(eventName, targetIds);
        }
        targetIds.Add(value.Id);
    }

    internal void IndexAllEventsTarget(UiObject value) => _allEventTargetIds.Add(value.Id);

    internal void UnindexAllEventsTarget(UiObject value) => _allEventTargetIds.Remove(value.Id);

    internal void UnindexEventTarget(UiObject value, string eventName)
    {
        if (value.Events.Contains(eventName) ||
            value.EventCallbackReferences.ContainsKey(eventName))
        {
            return;
        }
        if (!_eventTargetIds.TryGetValue(eventName, out var targetIds))
            return;
        targetIds.Remove(value.Id);
        if (targetIds.Count == 0)
            _eventTargetIds.Remove(eventName);
    }

    internal void UnindexEventTargets(UiObject value)
    {
        foreach (var eventName in _eventTargetIds
                     .Where(pair => pair.Value.Contains(value.Id))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            var targetIds = _eventTargetIds[eventName];
            targetIds.Remove(value.Id);
            if (targetIds.Count == 0)
                _eventTargetIds.Remove(eventName);
        }
    }

    private static string? EventCallbackDetail(UiObject value)
    {
        if (string.IsNullOrEmpty(value.SourceLocation))
            return value.AddonName;
        if (string.IsNullOrEmpty(value.AddonName))
            return value.SourceLocation;
        return $"{value.AddonName}: {value.SourceLocation}";
    }

    private IEnumerable<int> EnumerateEventTargetIds(string eventName)
    {
        _eventTargetIds.TryGetValue(eventName, out var eventTargets);
        if (eventTargets is null || eventTargets.Count == 0)
        {
            foreach (var id in _allEventTargetIds)
                yield return id;
            yield break;
        }
        if (_allEventTargetIds.Count == 0)
        {
            foreach (var id in eventTargets)
                yield return id;
            yield break;
        }

        using var eventEnumerator = eventTargets.GetEnumerator();
        using var allEnumerator = _allEventTargetIds.GetEnumerator();
        var hasEvent = eventEnumerator.MoveNext();
        var hasAll = allEnumerator.MoveNext();
        while (hasEvent || hasAll)
        {
            if (!hasAll || hasEvent && eventEnumerator.Current < allEnumerator.Current)
            {
                yield return eventEnumerator.Current;
                hasEvent = eventEnumerator.MoveNext();
                continue;
            }
            if (!hasEvent || allEnumerator.Current < eventEnumerator.Current)
            {
                yield return allEnumerator.Current;
                hasAll = allEnumerator.MoveNext();
                continue;
            }

            yield return eventEnumerator.Current;
            hasEvent = eventEnumerator.MoveNext();
            hasAll = allEnumerator.MoveNext();
        }
    }

    public void ApplyChatDisabledServerResponse(bool chatDisabled)
    {
        var accepted = SocialRestrictions.ChatDisabled == chatDisabled;
        SocialRestrictions.ChatDisabled = chatDisabled;
        SocialRestrictions.PendingChatDisabledRequest = null;
        TriggerEvent(
            accepted
                ? "CHAT_DISABLED_CHANGED"
                : "CHAT_DISABLED_CHANGE_FAILED",
            chatDisabled);
    }

    internal void SetKeyboardFocus(UiObject? value)
    {
        if (Ui.FocusedObjectId == value?.Id)
            return;

        if (Ui.FocusedObjectId is { } focusedId && Ui.Find(focusedId) is { } focused)
        {
            Ui.FocusedObjectId = null;
            InvokeScript(focused, "OnEditFocusLost");
        }

        if (value is null)
            return;

        Ui.FocusedObjectId = value.Id;
        InvokeScript(value, "OnEditFocusGained");
    }

    internal void RegisterGlobalEventCallback(
        string eventName,
        UIntPtr pointer,
        int reference,
        string? unit)
    {
        if (!_globalEventCallbacks.TryGetValue(eventName, out var callbacks))
        {
            callbacks = [];
            _globalEventCallbacks.Add(eventName, callbacks);
        }
        callbacks.Add(new GlobalEventCallback(pointer, reference, unit));
    }

    internal bool UnregisterGlobalEventCallback(
        string eventName,
        UIntPtr pointer,
        string? unit = null)
    {
        if (!_globalEventCallbacks.TryGetValue(eventName, out var callbacks))
            return false;
        var removed = false;
        for (var index = callbacks.Count - 1; index >= 0; index--)
        {
            if (callbacks[index].Pointer != pointer ||
                !string.Equals(
                    callbacks[index].Unit,
                    unit,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            ReleaseReference(callbacks[index].Reference);
            callbacks.RemoveAt(index);
            removed = true;
        }
        if (callbacks.Count == 0)
            _globalEventCallbacks.Remove(eventName);
        return removed;
    }

    internal void TriggerEditModeLayoutsUpdated(bool reconcileLayouts = false)
    {
        if (!Ui.Objects.Values.Any(value => value.Events.Contains("EDIT_MODE_LAYOUTS_UPDATED")))
            return;

        var baseline = lua_gettop(State);
        var layoutInfoReference = 0;
        try
        {
            lua_getglobal(State, "C_EditMode");
            if (lua_istable(State, -1) == 0)
                return;
            lua_getfield(State, -1, "GetLayouts");
            if (lua_isfunction(State, -1) == 0)
                return;
            if (lua_pcall(State, 0, 1, 0) != 0)
                throw BuildLuaException("C_EditMode.GetLayouts");
            layoutInfoReference = CaptureValue(-1);
            TriggerEvent(
                "EDIT_MODE_LAYOUTS_UPDATED",
                new LuaRegistryValue(layoutInfoReference),
                reconcileLayouts);
        }
        finally
        {
            lua_settop(State, baseline);
            ReleaseReference(layoutInfoReference);
        }
    }

    public void InvokeScript(UiObject value, string scriptName, params object?[] arguments)
    {
        if (!value.ScriptReferences.TryGetValue(scriptName, out var reference))
            return;
        InvokeReference(reference, value, arguments);
    }

    internal void InvokeModelSceneScript(
        UiObject value,
        string scriptName,
        params object?[] arguments)
    {
        _modelSceneCallbackDepth++;
        try
        {
            InvokeScript(value, scriptName, arguments);
        }
        finally
        {
            _modelSceneCallbackDepth--;
        }
    }

    internal bool TryGetGlobalColor(string name, out Vector4 color)
    {
        var baseline = lua_gettop(State);
        try
        {
            lua_getglobal(State, name);
            if (lua_istable(State, -1) != 0)
            {
                static bool TryReadComponent(
                    lua_State state,
                    string field,
                    out float component)
                {
                    lua_getfield(state, -1, field);
                    if (lua_isnumber(state, -1) != 0)
                    {
                        component = (float)lua_tonumber(state, -1);
                        lua_pop(state, 1);
                        return true;
                    }

                    lua_pop(state, 1);
                    component = 0;
                    return false;
                }

                if (TryReadComponent(State, "r", out var red) &&
                    TryReadComponent(State, "g", out var green) &&
                    TryReadComponent(State, "b", out var blue))
                {
                    var alpha = TryReadComponent(State, "a", out var resolvedAlpha)
                        ? resolvedAlpha
                        : 1;
                    color = new Vector4(red, green, blue, alpha);
                    return true;
                }
            }
        }
        finally
        {
            lua_settop(State, baseline);
        }

        if (GlobalColorProvider?.Colors.FirstOrDefault(value =>
                value.BaseTag.Equals(name, StringComparison.Ordinal)) is { } databaseColor &&
            !string.IsNullOrEmpty(databaseColor.BaseTag))
        {
            color = new Vector4(
                databaseColor.Red,
                databaseColor.Green,
                databaseColor.Blue,
                databaseColor.Alpha);
            return true;
        }

        color = default;
        return false;
    }

    internal bool TryGetGlobalString(string name, out string value)
    {
        var baseline = lua_gettop(State);
        try
        {
            PushCurrentGlobal(name);
            if (lua_type(State, -1) == LUA_TSTRING)
            {
                value = lua_tostring(State, -1) ?? string.Empty;
                return true;
            }
        }
        finally
        {
            lua_settop(State, baseline);
        }

        value = string.Empty;
        return false;
    }

    internal void DeferScript(
        UiObject value,
        string scriptName,
        params object?[] arguments)
    {
        _deferredScripts.Add(new DeferredScriptInvocation(
            value.Id,
            scriptName,
            arguments));
    }

    public bool SetSliderValue(
        UiObject slider,
        double requestedValue,
        bool treatAsMouseEvent)
    {
        var state = slider.StatusBar ??= new UiStatusBarState();
        if (!state.RangeInitialized)
            return false;
        var next = (float)requestedValue;
        var step = (float)slider.ValueStep;
        if ((!treatAsMouseEvent || slider.ObeyStepOnDrag) && step != 0)
        {
            var minimum = (float)state.Minimum;
            var distance = next - minimum;
            var roundedSteps = distance <= 0
                ? (int)((distance - step * 0.5f) / step)
                : (int)((distance + step * 0.5f) / step);
            next = roundedSteps * step + minimum;
        }

        next = Math.Clamp(next, (float)state.Minimum, (float)state.Maximum);
        const float changeEpsilon = 0.00000023841858f;
        if (state.ValueInitialized &&
            Math.Abs(next - state.Value) < changeEpsilon &&
            !treatAsMouseEvent)
        {
            return false;
        }

        state.Value = next;
        state.ValueInitialized = true;
        Ui.InvalidateLayout();
        InvokeScript(slider, "OnValueChanged", next, treatAsMouseEvent);
        return true;
    }

    internal void InvokeButtonClick(UiObject value, string button, bool isDown)
    {
        if (value.ObjectType.Equals("CheckButton", StringComparison.OrdinalIgnoreCase))
            SetCheckButtonChecked(value, !value.Checked);
        if (!value.Enabled || value.ButtonClickDispatching)
            return;

        value.ButtonClickDispatching = true;
        try
        {
            InvokeScript(value, "PreClick", button, isDown);
            InvokeScript(value, "OnClick", button, isDown);
            InvokeScript(value, "PostClick", button, isDown);
        }
        finally
        {
            value.ButtonClickDispatching = false;
        }
    }

    internal void InvokeButtonDoubleClick(UiObject value, string button)
    {
        if (value.Enabled)
            InvokeScript(value, "OnDoubleClick", button);
    }

    internal void SetCheckButtonChecked(UiObject value, bool checkedValue)
    {
        value.Checked = checkedValue;
        var checkedTexture = value.CheckedTextureId is { } checkedId
            ? Ui.Find(checkedId)
            : null;
        var disabledCheckedTexture = value.DisabledCheckedTextureId is { } disabledCheckedId
            ? Ui.Find(disabledCheckedId)
            : null;
        if (checkedTexture is not null)
            checkedTexture.Shown = false;
        if (disabledCheckedTexture is not null)
            disabledCheckedTexture.Shown = false;
        if (!checkedValue)
            return;

        if (!value.Enabled && disabledCheckedTexture is not null)
            disabledCheckedTexture.Shown = true;
        else if (checkedTexture is not null)
            checkedTexture.Shown = true;
    }

    public bool InvokeSlashCommand(string commandLine)
    {
        var trimmed = commandLine.Trim();
        if (!trimmed.StartsWith('/'))
            return false;

        var separator = trimmed.IndexOf(' ');
        var slash = separator < 0 ? trimmed : trimmed[..separator];
        var arguments = separator < 0 ? string.Empty : trimmed[(separator + 1)..];
        var baseline = lua_gettop(State);
        lua_getglobal(State, "__WoWAddonLabInvokeSlashCommand");
        if (lua_isfunction(State, -1) == 0)
        {
            lua_settop(State, baseline);
            return false;
        }
        lua_pushstring(State, slash);
        lua_pushstring(State, arguments);
        if (lua_pcall(State, 2, 1, 0) != 0)
            throw BuildLuaException($"slash command {slash}");
        var handled = lua_toboolean(State, -1) != 0;
        lua_settop(State, baseline);
        return handled;
    }

    internal int CaptureFunction(int index)
    {
        return CaptureValue(index);
    }

    internal int CaptureFunction(lua_State state, int index)
    {
        return CaptureValue(state, index);
    }

    internal int CaptureValue(int index)
    {
        lua_pushvalue(State, index);
        return luaL_ref(State, LUA_REGISTRYINDEX);
    }

    internal static int CaptureValue(lua_State state, int index)
    {
        lua_pushvalue(state, index);
        return luaL_ref(state, LUA_REGISTRYINDEX);
    }

    internal void ReleaseReference(int reference)
    {
        if (reference > 0)
            luaL_unref(State, LUA_REGISTRYINDEX, reference);
    }

    internal long ScheduleTimer(
        uint delayMilliseconds,
        int functionReference,
        bool repeating = false,
        uint intervalMilliseconds = 0,
        uint? iterations = null)
    {
        var id = Interlocked.Increment(ref _nextTimerId);
        _timers.Add(new LuaTimer(
            id,
            unchecked(FrameTime.TickMilliseconds + delayMilliseconds),
            FrameTime.TickMilliseconds,
            delayMilliseconds == 0,
            intervalMilliseconds,
            repeating,
            iterations is null ? null : unchecked(iterations.Value - 1),
            functionReference,
            0,
            false));
        return id;
    }

    internal void AttachTimerHandle(long id, int reference)
    {
        var index = _timers.FindIndex(timer => timer.Id == id);
        if (index < 0)
            return;
        _timers[index] = _timers[index] with
        {
            HandleReference = reference
        };
    }

    internal void CancelTimer(long id)
    {
        var index = _timers.FindIndex(timer => timer.Id == id);
        if (index >= 0)
            _timers[index] = _timers[index] with { Cancelled = true };
    }

    internal bool IsTimerCancelled(long id) =>
        _timers.FirstOrDefault(timer => timer.Id == id)?.Cancelled ?? true;

    internal void InvokeTimer(long id, int firstArgument, int argumentCount)
    {
        var timer = _timers.FirstOrDefault(candidate => candidate.Id == id);
        if (timer is null || timer.Cancelled)
            return;
        InvokeReferenceFromStack(timer.FunctionReference, firstArgument, argumentCount);
    }

    internal void PushObject(UiObject? value)
    {
        if (value is null || value.LuaReference <= 0)
        {
            lua_pushnil(State);
            return;
        }
        lua_rawgeti(State, LUA_REGISTRYINDEX, value.LuaReference);
    }

    internal void PushWindow(UiObject value)
    {
        for (UiObject? current = value; current is not null;)
        {
            if (current.WindowReference is { } reference)
            {
                lua_rawgeti(State, LUA_REGISTRYINDEX, reference);
                return;
            }

            current = current.ParentId is { } parentId ? Ui.Find(parentId) : null;
        }

        if (_defaultWindowReference is not { } defaultReference)
        {
            lua_newtable(State);
            lua_pushstring(State, "SimpleWindow");
            lua_setfield(State, -2, "__type");
            lua_pushvalue(State, -1);
            defaultReference = luaL_ref(State, LUA_REGISTRYINDEX);
            _defaultWindowReference = defaultReference;
        }

        lua_rawgeti(State, LUA_REGISTRYINDEX, defaultReference);
    }

    internal bool SetWindow(UiObject value, int index)
    {
        if (index <= lua_gettop(State) && lua_isnil(State, index) == 0)
        {
            if (lua_type(State, index) is not (LUA_TTABLE or LUA_TUSERDATA))
                return false;
            lua_getfield(State, index, "__type");
            var isSimpleWindow =
                lua_type(State, -1) == LUA_TSTRING &&
                string.Equals(
                    lua_tostring(State, -1),
                    "SimpleWindow",
                    StringComparison.Ordinal);
            lua_pop(State, 1);
            if (!isSimpleWindow)
                return false;
        }

        SetWindowRecursive(
            value,
            index <= lua_gettop(State) && lua_isnil(State, index) == 0
                ? index
                : null);
        return true;
    }

    private void SetWindowRecursive(UiObject value, int? index)
    {
        ClearWindow(value);
        if (index is { } sourceIndex)
        {
            lua_pushvalue(State, sourceIndex);
            value.WindowReference = luaL_ref(State, LUA_REGISTRYINDEX);
        }

        foreach (var childId in value.Children)
        {
            if (Ui.Find(childId) is { } child &&
                WowWidgetApi.IsFrameWidget(child.ObjectType))
            {
                SetWindowRecursive(child, index);
            }
        }
    }

    internal void ClearWindowHierarchy(UiObject value)
    {
        SetWindowRecursive(value, null);
    }

    internal void ClearWindow(UiObject value)
    {
        if (value.WindowReference is not { } reference)
            return;

        luaL_unref(State, LUA_REGISTRYINDEX, reference);
        value.WindowReference = null;
    }

    internal void InvokeReference(int reference, UiObject? self, params object?[] arguments)
    {
        var baseline = lua_gettop(State);
        lua_getglobal(State, "__WoWAddonLabTraceback");
        var errorHandler = baseline + 1;
        lua_rawgeti(State, LUA_REGISTRYINDEX, reference);
        if (lua_isfunction(State, -1) == 0)
        {
            lua_settop(State, baseline);
            return;
        }

        var count = 0;
        if (self is not null)
        {
            PushObject(self);
            count++;
        }
        foreach (var argument in arguments)
        {
            PushValue(argument);
            count++;
        }

        if (lua_pcall(State, count, 0, errorHandler) != 0)
        {
            var message = lua_tostring(State, -1) ?? "unknown Lua callback error";
            Log.Error("lua", message);
            lua_settop(State, baseline);
            return;
        }
        lua_settop(State, baseline);
    }

    private void InvokeReferenceWithRegistryArgument(int reference, int argumentReference)
    {
        var baseline = lua_gettop(State);
        lua_getglobal(State, "__WoWAddonLabTraceback");
        var errorHandler = baseline + 1;
        lua_rawgeti(State, LUA_REGISTRYINDEX, reference);
        lua_rawgeti(State, LUA_REGISTRYINDEX, argumentReference);
        if (lua_pcall(State, 1, 0, errorHandler) != 0)
        {
            var message = lua_tostring(State, -1) ?? "unknown Lua callback error";
            Log.Error("lua", message);
        }
        lua_settop(State, baseline);
    }

    private void InvokeReferenceFromStack(int reference, int firstArgument, int argumentCount)
    {
        var baseline = lua_gettop(State);
        lua_getglobal(State, "__WoWAddonLabTraceback");
        var errorHandler = baseline + 1;
        lua_rawgeti(State, LUA_REGISTRYINDEX, reference);
        for (var index = 0; index < argumentCount; index++)
            lua_pushvalue(State, firstArgument + index);
        if (lua_pcall(State, argumentCount, 0, errorHandler) != 0)
        {
            var message = lua_tostring(State, -1) ?? "unknown Lua callback error";
            Log.Error("lua", message);
        }
        lua_settop(State, baseline);
    }

    internal void InvokeObjectMethod(UiObject value, string methodName)
    {
        var baseline = lua_gettop(State);
        lua_getglobal(State, "__WoWAddonLabTraceback");
        var errorHandler = baseline + 1;
        PushObject(value);
        var objectIndex = baseline + 2;
        lua_getfield(State, objectIndex, methodName);
        if (lua_isfunction(State, -1) == 0)
        {
            lua_settop(State, baseline);
            return;
        }
        PushObject(value);
        if (lua_pcall(State, 1, 0, errorHandler) != 0)
        {
            var message = lua_tostring(State, -1) ?? $"unknown {methodName} error";
            Log.Error("lua", message);
        }
        lua_settop(State, baseline);
    }

    internal void ExecuteString(string code, string chunkName)
    {
        var status = LuaChunkLoader.Load(State, code, chunkName);
        if (status != 0)
            throw BuildLuaException($"compile {chunkName}");
        status = lua_pcall(State, 0, 0, 0);
        if (status != 0)
            throw BuildLuaException($"execute {chunkName}");
    }

    internal bool ExecuteBinding(string command, string keyState)
    {
        if (Bindings.GetScript(command) is not { } script)
            return false;

        var baseline = lua_gettop(State);
        lua_getglobal(State, "keystate");
        var previousKeyState = luaL_ref(State, LUA_REGISTRYINDEX);
        try
        {
            lua_pushstring(State, keyState);
            lua_setglobal(State, "keystate");
            var status = LuaChunkLoader.Load(State, script, $"@Bindings.xml:{command}");
            if (status != 0)
                throw BuildLuaException($"compile binding {command}");
            status = lua_pcall(State, 0, 0, 0);
            if (status != 0)
                throw BuildLuaException($"execute binding {command}");
            return true;
        }
        finally
        {
            lua_rawgeti(State, LUA_REGISTRYINDEX, previousKeyState);
            lua_setglobal(State, "keystate");
            luaL_unref(State, LUA_REGISTRYINDEX, previousKeyState);
            lua_settop(State, baseline);
        }
    }

    internal bool ApplyXmlTemplates(
        UiObject value,
        UiObject? parent,
        string? templates,
        string? intrinsicType = null) =>
        _xmlUiLoader.ApplyTemplates(value, parent, templates, intrinsicType);

    internal bool TryResolveXmlCreateFrameObjectType(
        string requestedObjectType,
        out string objectType) =>
        _xmlUiLoader.TryResolveCreateFrameObjectType(requestedObjectType, out objectType);

    internal bool XmlTemplatesExist(string? templates) =>
        _xmlUiLoader.TemplatesExist(templates);

    internal int CompileXmlScript(string body, string chunkName)
    {
        var code = $"return function(self, ...)\n{body}\nend";
        var status = LuaChunkLoader.Load(State, code, chunkName);
        if (status != 0)
            throw BuildLuaException($"compile XML script {chunkName}");
        ApplyCurrentAddonEnvironment(-1);
        status = lua_pcall(State, 0, 1, 0);
        if (status != 0)
            throw BuildLuaException($"create XML script {chunkName}");
        return luaL_ref(State, LUA_REGISTRYINDEX);
    }

    internal int CompileXmlScriptChain(IReadOnlyList<int> references, string chunkName)
    {
        if (references.Count == 0)
            throw new ArgumentException("An XML script chain requires at least one callback.", nameof(references));

        var parameters = string.Join(", ", Enumerable.Range(1, references.Count).Select(
            index => $"callback{index}"));
        var calls = string.Join(
            "\n",
            Enumerable.Range(1, references.Count - 1).Select(
                index => $"callback{index}(self, ...)"));
        var finalCall = $"return callback{references.Count}(self, ...)";
        var code =
            $"return function({parameters})\n" +
            "return function(self, ...)\n" +
            (calls.Length == 0 ? string.Empty : calls + "\n") +
            finalCall + "\nend\nend";

        var status = LuaChunkLoader.Load(State, code, chunkName);
        if (status != 0)
            throw BuildLuaException($"compile XML script chain {chunkName}");
        status = lua_pcall(State, 0, 1, 0);
        if (status != 0)
            throw BuildLuaException($"create XML script chain factory {chunkName}");
        foreach (var reference in references)
            lua_rawgeti(State, LUA_REGISTRYINDEX, reference);
        status = lua_pcall(State, references.Count, 1, 0);
        if (status != 0)
            throw BuildLuaException($"create XML script chain {chunkName}");
        return luaL_ref(State, LUA_REGISTRYINDEX);
    }

    internal void SetScript(UiObject value, string scriptName, int? reference) =>
        _xmlUiLoader.SetScript(value, scriptName, reference);

    internal void HookScript(UiObject value, string scriptName, int reference) =>
        _xmlUiLoader.HookScript(value, scriptName, reference);

    internal bool TryGetScript(UiObject value, string scriptName, out int reference) =>
        _xmlUiLoader.TryGetScript(value, scriptName, out reference);

    internal void ClearScripts(UiObject value) =>
        _xmlUiLoader.ClearScripts(value);

    internal void ApplyGlobalMixins(UiObject value, string? mixinNames, bool fromSecureEnvironment = false)
    {
        if (string.IsNullOrWhiteSpace(mixinNames))
            return;
        var environmentReference = fromSecureEnvironment
            ? EnsureSecureEnvironment()
            : _currentAddonEnvironmentReference;
        foreach (var mixinName in mixinNames.Split(
                     ',',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryApplyGlobalMixin(value, mixinName, environmentReference))
            {
                if (!_pendingGlobalMixins.TryGetValue(value.Id, out var pending))
                {
                    pending = [];
                    _pendingGlobalMixins[value.Id] = pending;
                }
                if (!pending.Any(item =>
                        item.Name.Equals(mixinName, StringComparison.Ordinal) &&
                        item.EnvironmentReference == environmentReference))
                {
                    pending.Add(new PendingGlobalMixin(
                        mixinName,
                        environmentReference));
                }
            }
        }
    }

    internal bool HasPendingGlobalMixins(int objectId) =>
        _pendingGlobalMixins.ContainsKey(objectId);

    private bool TryApplyGlobalMixin(
        UiObject value,
        string mixinName,
        int? environmentReference)
    {
        var baseline = lua_gettop(State);
        PushObject(value);
        var targetIndex = baseline + 1;
        var previousEnvironment = SwapAddonEnvironment(environmentReference);
        try
        {
            PushCurrentGlobal(mixinName);
            if (lua_istable(State, -1) == 0)
                return false;

            lua_pushnil(State);
            while (lua_next(State, -2) != 0)
            {
                lua_pushvalue(State, -2);
                lua_pushvalue(State, -2);
                lua_settable(State, targetIndex);
                lua_pop(State, 1);
            }
            return true;
        }
        finally
        {
            SwapAddonEnvironment(previousEnvironment);
            lua_settop(State, baseline);
        }
    }

    private IReadOnlySet<int> ResolvePendingGlobalMixins()
    {
        if (_pendingGlobalMixins.Count == 0)
            return new HashSet<int>();

        var resolvedObjects = new HashSet<int>();
        foreach (var (objectId, pending) in _pendingGlobalMixins.ToArray())
        {
            if (Ui.Find(objectId) is not { } value)
            {
                _pendingGlobalMixins.Remove(objectId);
                continue;
            }

            pending.RemoveAll(item =>
                TryApplyGlobalMixin(value, item.Name, item.EnvironmentReference));
            if (pending.Count != 0)
                continue;
            _pendingGlobalMixins.Remove(objectId);
            resolvedObjects.Add(objectId);
        }
        return resolvedObjects;
    }

    internal void SetObjectField(UiObject owner, string key, object? value)
    {
        var baseline = lua_gettop(State);
        PushObject(owner);
        PushValue(value);
        lua_setfield(State, -2, key);
        lua_settop(State, baseline);
    }

    internal void SetParentKey(UiObject child, string parentKey, bool clearOtherKeys)
    {
        if (child.ParentId is not { } parentId || Ui.Find(parentId) is not { } parent)
            return;

        if (clearOtherKeys)
            ClearParentKeys(child);

        if (parentKey.Length == 0)
            return;

        var baseline = lua_gettop(State);
        PushObject(parent);
        var parentIndex = baseline + 1;
        PushObject(child);
        lua_setfield(State, parentIndex, parentKey);
        child.ParentKey = parentKey;
        lua_settop(State, baseline);
    }

    internal void ClearParentKeys(UiObject child)
    {
        if (child.ParentId is not { } parentId || Ui.Find(parentId) is not { } parent)
            return;

        var baseline = lua_gettop(State);
        PushObject(parent);
        var parentIndex = baseline + 1;
        var matchingKeys = new List<string>();
        lua_pushnil(State);
        while (lua_next(State, parentIndex) != 0)
        {
            if (lua_type(State, -2) == LUA_TSTRING && lua_istable(State, -1) != 0)
            {
                lua_getfield(State, -1, "__id");
                if (lua_type(State, -1) == LUA_TNUMBER &&
                    (int)lua_tonumber(State, -1) == child.Id)
                {
                    matchingKeys.Add(lua_tostring(State, -3)!);
                }
                lua_pop(State, 1);
            }
            lua_pop(State, 1);
        }
        foreach (var key in matchingKeys)
        {
            lua_pushnil(State);
            lua_setfield(State, parentIndex, key);
        }
        child.ParentKey = null;
        lua_settop(State, baseline);
    }

    internal string? GetParentKey(UiObject child)
    {
        if (child.ParentId is not { } parentId || Ui.Find(parentId) is not { } parent)
            return null;

        var baseline = lua_gettop(State);
        PushObject(parent);
        var parentIndex = baseline + 1;
        string? selected = null;
        lua_pushnil(State);
        while (lua_next(State, parentIndex) != 0)
        {
            if (lua_type(State, -2) == LUA_TSTRING && lua_istable(State, -1) != 0)
            {
                var key = lua_tostring(State, -2);
                lua_getfield(State, -1, "__id");
                var referencesChild = lua_type(State, -1) == LUA_TNUMBER &&
                                      (int)lua_tonumber(State, -1) == child.Id;
                lua_pop(State, 1);
                if (referencesChild && key is { Length: > 0 } &&
                    (selected is null ||
                     IsAsciiUpper(key[0]) && !IsAsciiUpper(selected[0])))
                {
                    selected = key;
                }
            }
            lua_pop(State, 1);
        }
        lua_settop(State, baseline);
        return selected;
    }

    private static bool IsAsciiUpper(char value) => value is >= 'A' and <= 'Z';

    internal string GetDebugName(UiObject value, bool preferParentKey) =>
        GetDebugName(value, preferParentKey, []);

    private string GetDebugName(
        UiObject value,
        bool preferParentKey,
        HashSet<int> ancestors)
    {
        if (!ancestors.Add(value.Id))
            return value.Forbidden ? "inaccessible" : value.Id.ToString("x");

        try
        {
            var parent = value.ParentId is { } parentId ? Ui.Find(parentId) : null;
            if (value.Name is { } name && (!preferParentKey || parent is null))
                return name;

            var parentKey = GetParentKey(value);
            var fallback = value.Forbidden ? "inaccessible" : value.Id.ToString("x");
            if (parent is null)
                return parentKey ?? fallback;

            var parentDebugName = GetDebugName(parent, preferParentKey, ancestors);
            if (parentDebugName.Length == 0)
                return string.Empty;
            if (parentKey is not null || value.Name is null)
                return $"{parentDebugName}.{parentKey ?? fallback}";
            return $"{parentDebugName}, {value.Name}";
        }
        finally
        {
            ancestors.Remove(value.Id);
        }
    }

    internal void AppendObjectField(UiObject owner, string key, UiObject value)
    {
        var baseline = lua_gettop(State);
        PushObject(owner);
        var ownerIndex = baseline + 1;
        lua_getfield(State, ownerIndex, key);
        if (lua_istable(State, -1) == 0)
        {
            lua_pop(State, 1);
            lua_newtable(State);
            lua_pushvalue(State, -1);
            lua_setfield(State, ownerIndex, key);
        }

        var arrayIndex = baseline + 2;
        var index = 1;
        while (true)
        {
            lua_rawgeti(State, arrayIndex, index);
            var occupied = lua_isnil(State, -1) == 0;
            lua_pop(State, 1);
            if (!occupied)
                break;
            index++;
        }

        PushObject(value);
        lua_rawseti(State, arrayIndex, index);
        lua_settop(State, baseline);
    }

    internal void SetObjectFieldFromGlobal(
        UiObject owner,
        string key,
        string globalName)
    {
        var baseline = lua_gettop(State);
        PushObject(owner);
        var path = globalName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (path.Length == 0)
        {
            lua_pushnil(State);
        }
        else
        {
            PushCurrentGlobal(path[0]);
            for (var index = 1; index < path.Length; index++)
            {
                if (lua_type(State, -1) != LUA_TTABLE)
                {
                    lua_pop(State, 1);
                    lua_pushnil(State);
                    break;
                }

                lua_getfield(State, -1, path[index]);
                lua_remove(State, -2);
            }
        }
        lua_setfield(State, baseline + 1, key);
        lua_settop(State, baseline);
    }

    public string GetVersion()
    {
        lua_getglobal(State, "_VERSION");
        var result = lua_tostring(State, -1) ?? "Lua 5.1";
        lua_pop(State, 1);
        return result;
    }

    public bool ApplyUiScaleCVars()
    {
        var useUiScale = CVars.TryGet("useUiScale", out var useEntry) &&
                         ParseCVarBoolean(useEntry.Value);
        var uiScale = CVars.TryGet("uiScale", out var scaleEntry) &&
                      float.TryParse(
                          scaleEntry.Value,
                          NumberStyles.Float,
                          CultureInfo.InvariantCulture,
                          out var parsedScale)
            ? parsedScale
            : 1;
        var multiplier = CVars.TryGet("uiScaleMultiplier", out var multiplierEntry) &&
                         float.TryParse(
                             multiplierEntry.Value,
                             NumberStyles.Float,
                             CultureInfo.InvariantCulture,
                             out var parsedMultiplier)
            ? parsedMultiplier
            : -1;
        return Ui.ConfigureUiScale(useUiScale, uiScale, multiplier);
    }

    public void SetScreenDpiScale(float dpiScale)
    {
        if (Ui.SetScreenDpiScale(dpiScale) && IsLoaded)
            TriggerEvent("UI_SCALE_CHANGED");
    }

    private void OnCVarValueChanged(string name, string value)
    {
        var affectsUiScale =
            name.Equals("uiScale", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("useUiScale", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("uiScaleMultiplier", StringComparison.OrdinalIgnoreCase);
        if (affectsUiScale && ApplyUiScaleCVars() && IsLoaded)
            TriggerEvent("UI_SCALE_CHANGED");
        if (IsLoaded)
            TriggerEvent("CVAR_UPDATE", name, value);
    }

    private static bool ParseCVarBoolean(string value) =>
        value.Length > 0 &&
        value[0] switch
        {
            '0' or 'F' or 'N' or 'f' or 'n' => false,
            >= '1' and <= '9' or 'T' or 'Y' or 't' or 'y' => true,
            _ => value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                 value.Equals("enabled", StringComparison.OrdinalIgnoreCase)
        };

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Providers.Changed -= OnDataProviderChanged;
        CVars.ValueChanged -= OnCVarValueChanged;
        LuaBindings.Detach(this);
        foreach (var timer in _timers)
        {
            ReleaseReference(timer.FunctionReference);
            ReleaseReference(timer.HandleReference);
        }
        ReleaseAddonLocalTableReferences();
        ReleaseReference(Macros.ExecuteLineCallbackReference);
        ReleaseReference(EditMode.SavedLayoutsReference);
        foreach (var value in Ui.Objects.Values)
        {
            foreach (var reference in value.ScriptReferences.Values)
                ReleaseReference(reference);
            foreach (var reference in value.AttributeReferences.Values)
                ReleaseReference(reference);
            foreach (var reference in value.EventCallbackReferences.Values.SelectMany(
                         references => references).Select(callback => callback.Reference))
            {
                ReleaseReference(reference);
            }
            if (value.Cooldown is { CountdownFormatterReference: > 0 } cooldown)
                ReleaseReference(cooldown.CountdownFormatterReference);
            ReleaseReference(value.LuaReference);
        }
        lua_close(State);
    }

    private void CreateGlobals()
    {
        var uiParent = LuaBindings.CreateObject(this, "Frame", "UIParent", null);
        if (Ui.UsesNativeScreenMetrics)
            uiParent.Scale = Ui.AppliedUiScale;
        uiParent.Width = Ui.LogicalWidth;
        uiParent.Height = Ui.LogicalHeight;
        uiParent.MouseEnabled = false;
        LuaBindings.SetGlobalObject(this, uiParent);

        var worldFrame = LuaBindings.CreateObject(this, "Frame", "WorldFrame", uiParent);
        worldFrame.AllPointsTargetId = uiParent.Id;
        worldFrame.MouseEnabled = false;
        LuaBindings.SetGlobalObject(this, worldFrame);

        var minimap = LuaBindings.CreateObject(this, "Minimap", "Minimap", uiParent);
        minimap.Width = 140;
        minimap.Height = 140;
        minimap.MouseEnabled = true;
        minimap.Anchors.Add(new UiAnchor("TOPRIGHT", uiParent.Id, "TOPRIGHT", -30, -30));
        LuaBindings.SetGlobalObject(this, minimap);

        var tooltip = LuaBindings.CreateObject(this, "GameTooltip", "GameTooltip", uiParent);
        tooltip.Shown = false;
        tooltip.Width = 260;
        tooltip.Height = 80;
        tooltip.Font = new UiFontState { JustifyHorizontal = "LEFT" };
        LuaBindings.SetGlobalObject(this, tooltip);

        lua_pushinteger(State, 4);
        lua_setglobal(State, "NUM_LE_LFG_CATEGORYS");
        lua_pushstring(State, "Chat Settings - %s");
        lua_setglobal(State, "CHATCONFIG_HEADER");
    }

    private void RunTimers()
    {
        var now = FrameTime.TickMilliseconds;
        var dueTimerIds = _timers
            .Where(timer =>
                timer.Cancelled ||
                (!timer.ZeroDelay || now > timer.ScheduledAtTick) &&
                timer.DueTick <= now)
            .OrderByDescending(timer => unchecked(now - timer.DueTick))
            .ThenBy(timer => timer.Id)
            .Select(timer => timer.Id)
            .ToArray();

        foreach (var timerId in dueTimerIds)
        {
            var index = _timers.FindIndex(timer => timer.Id == timerId);
            if (index < 0)
                continue;
            var timer = _timers[index];
            if (timer.Cancelled)
            {
                ReleaseReference(timer.FunctionReference);
                ReleaseReference(timer.HandleReference);
                _timers.RemoveAt(index);
                continue;
            }
            if (timer.HandleReference > 0)
                InvokeReferenceWithRegistryArgument(timer.FunctionReference, timer.HandleReference);
            else
                InvokeReference(timer.FunctionReference, null);
            timer = _timers[index];
            if (timer.Cancelled)
            {
                ReleaseReference(timer.FunctionReference);
                ReleaseReference(timer.HandleReference);
                _timers.RemoveAt(index);
                continue;
            }
            if (!timer.Repeating || timer.RemainingReschedules is 0)
            {
                ReleaseReference(timer.FunctionReference);
                ReleaseReference(timer.HandleReference);
                _timers.RemoveAt(index);
                continue;
            }

            _timers[index] = timer with
            {
                DueTick = unchecked(now + timer.IntervalMilliseconds),
                ScheduledAtTick = now,
                ZeroDelay = timer.IntervalMilliseconds == 0,
                RemainingReschedules = timer.RemainingReschedules is null
                    ? null
                    : timer.RemainingReschedules - 1
            };
        }
    }

    private void RunDeferredScripts()
    {
        if (_deferredScripts.Count == 0)
            return;

        var pending = _deferredScripts.ToArray();
        _deferredScripts.Clear();
        foreach (var invocation in pending)
        {
            if (Ui.Find(invocation.ObjectId) is { } value)
            {
                InvokeScript(
                    value,
                    invocation.ScriptName,
                    invocation.Arguments);
            }
        }
    }

    private void TickAnimations(double delta)
    {
        var groups = Ui.Objects.Values
            .Where(value => value.AnimationGroup is { Playing: true })
            .ToArray();
        foreach (var groupObject in groups)
            AdvanceAnimationGroup(groupObject, delta);
    }

    internal void AdvanceAnimationGroup(UiObject groupObject, double delta)
    {
        if (groupObject.AnimationGroup is not { Playing: true } group)
            return;

        var reportedDelta = Math.Clamp(delta, 0, 60);
        var (animations, orderDurations) = GetAnimationTimeline(groupObject);
        var totalDuration = orderDurations.Sum(value => value.Duration);
        if (totalDuration <= 0.0001)
        {
            ApplyAnimationTimeline(
                animations,
                orderDurations,
                group.Reverse ? totalDuration : 0,
                group.Reverse,
                true);
            InvokeScript(groupObject, "OnUpdate", 0d);
            FinishAnimationGroup(groupObject, false);
            return;
        }

        var remaining = reportedDelta;
        var applyCurrentTime = true;
        var reachedEnd = false;
        while (group.Playing && (applyCurrentTime || remaining > 0.0001))
        {
            applyCurrentTime = false;
            if (AdvancePastManuallyStoppedOrder(
                    group,
                    animations,
                    orderDurations,
                    totalDuration))
            {
                applyCurrentTime = true;
                continue;
            }

            var elapsedBeforeCurrentOrder = group.Reverse
                ? orderDurations
                    .Skip(group.CurrentOrderIndex + 1)
                    .Sum(order => order.Duration)
                : orderDurations
                    .Take(group.CurrentOrderIndex)
                    .Sum(order => order.Duration);
            var currentOrderRemaining = Math.Max(
                0,
                orderDurations[group.CurrentOrderIndex].Duration -
                (group.Elapsed - elapsedBeforeCurrentOrder));
            var step = group.AnimationSpeedMultiplier > 0
                ? Math.Min(
                    remaining * group.AnimationSpeedMultiplier,
                    currentOrderRemaining)
                : 0;
            step = Math.Min(step, Math.Max(0, totalDuration - group.Elapsed));
            group.Elapsed = Math.Min(totalDuration, group.Elapsed + step);
            remaining = Math.Max(0, remaining - step);

            var timeline = group.Reverse
                ? totalDuration - group.Elapsed
                : group.Elapsed;
            ApplyAnimationTimeline(
                animations,
                orderDurations,
                timeline,
                group.Reverse,
                true);
            group.CurrentOrderIndex = ResolveCurrentAnimationOrderIndex(
                orderDurations,
                group.Elapsed,
                group.Reverse);

            if (group.Elapsed < totalDuration - 0.0001)
            {
                if (step < currentOrderRemaining - 0.0001 ||
                    step <= 0.0001)
                {
                    break;
                }
                continue;
            }

            var looping =
                group.Looping.Equals("REPEAT", StringComparison.OrdinalIgnoreCase) ||
                group.Looping.Equals("BOUNCE", StringComparison.OrdinalIgnoreCase);
            if (!looping || group.PendingFinish)
            {
                reachedEnd = true;
                break;
            }

            group.Elapsed = 0;
            if (group.Looping.Equals("BOUNCE", StringComparison.OrdinalIgnoreCase))
                group.Reverse = !group.Reverse;
            group.CurrentOrderIndex = group.Reverse
                ? orderDurations.Length - 1
                : 0;
            ResetAnimationTimeline(animations, orderDurations, group.Reverse);
            InvokeScript(groupObject, "OnLoop", group.Reverse ? "REVERSE" : "FORWARD");
            ActivateAnimationOrder(animations, orderDurations, group.Reverse);
        }

        InvokeScript(groupObject, "OnUpdate", reportedDelta);
        if (reachedEnd)
            FinishAnimationGroup(groupObject, group.PendingFinish);
    }

    internal void ApplyAnimationGroupAtCurrentTime(UiObject groupObject)
    {
        if (groupObject.AnimationGroup is not { } group)
            return;
        var (animations, orderDurations) = GetAnimationTimeline(groupObject);
        var totalDuration = orderDurations.Sum(value => value.Duration);
        var timeline = Math.Clamp(group.Elapsed, 0, totalDuration);
        if (group.Reverse)
            timeline = totalDuration - timeline;
        ApplyAnimationTimeline(
            animations,
            orderDurations,
            timeline,
            group.Reverse,
            false);
    }

    internal bool PlayAnimationGroup(UiObject groupObject, bool reverse, double offset)
    {
        if (groupObject.AnimationGroup is not { } group)
            return false;

        var (animations, orderDurations) = GetAnimationTimeline(groupObject);
        if (animations.Length == 0)
            return false;

        var totalDuration = orderDurations.Sum(value => value.Duration);
        if (group.Playing)
        {
            if (!group.Looping.Equals("NONE", StringComparison.OrdinalIgnoreCase) ||
                group.Reverse == reverse)
            {
                return true;
            }

            group.Elapsed = Math.Max(0, totalDuration - group.Elapsed);
            group.Reverse = reverse;
            group.CurrentOrderIndex = ResolveCurrentAnimationOrderIndex(
                orderDurations,
                group.Elapsed,
                reverse);
            ApplyAnimationGroupAtCurrentTime(groupObject);
            return true;
        }

        var wasPaused = group.Paused;
        group.Playing = true;
        group.Paused = false;
        group.Finished = false;
        if (!wasPaused)
        {
            group.PendingFinish = false;
            group.Elapsed = 0;
            group.Reverse = reverse;
            group.CurrentOrderIndex = reverse ? orderDurations.Length - 1 : 0;
            ResetAnimationTimeline(animations, orderDurations, reverse);
        }

        InvokeScript(groupObject, "OnPlay");
        if (wasPaused)
        {
            foreach (var animationObject in animations.Where(
                         animation => animation.Animation?.PlaybackState == 2))
            {
                animationObject.Animation!.PlaybackState = 1;
                InvokeScript(animationObject, "OnPlay");
            }
        }
        else
        {
            ActivateAnimationOrder(animations, orderDurations, reverse);
        }

        AdvanceAnimationGroup(groupObject, Math.Clamp(offset, 0, 60));
        return true;
    }

    internal void PauseAnimationGroup(UiObject groupObject)
    {
        if (groupObject.AnimationGroup is not { Playing: true } group)
            return;

        group.Playing = false;
        group.Paused = true;
        foreach (var childId in groupObject.Children)
        {
            if (Ui.Find(childId) is not { Animation: { PlaybackState: 1 } animation }
                animationObject)
            {
                continue;
            }

            animation.PlaybackState = 2;
            InvokeScript(animationObject, "OnPause");
        }
        InvokeScript(groupObject, "OnPause");
    }

    internal void StopAnimationGroup(UiObject groupObject, bool requested)
    {
        if (groupObject.AnimationGroup is not { } group ||
            !group.Playing && !group.Paused)
        {
            return;
        }

        var (animations, orderDurations) = GetAnimationTimeline(groupObject);
        foreach (var animationObject in animations)
        {
            var animation = animationObject.Animation!;
            if (animation.PlaybackState == 0)
                continue;
            animation.Elapsed = 0;
            animation.Progress = 0;
            animation.SmoothProgress = 0;
            animation.PlaybackState = 0;
            animation.ManuallyStopped = false;
            InvokeScript(animationObject, "OnStop", requested);
        }

        group.Playing = false;
        group.Paused = false;
        group.Finished = false;
        group.PendingFinish = false;
        group.Elapsed = 0;
        group.CurrentOrderIndex = 0;
        ClearAnimationGroupEffects(animations, false);
        InvokeScript(groupObject, "OnStop", requested);
    }

    private void AdvanceAnimationOrder(
        IReadOnlyList<UiObject> animations,
        IReadOnlyList<AnimationOrderDuration> orderDurations,
        UiAnimationGroupState group)
    {
        group.CurrentOrderIndex += group.Reverse ? -1 : 1;
        if (group.CurrentOrderIndex < 0 ||
            group.CurrentOrderIndex >= orderDurations.Count)
        {
            return;
        }

        ActivateAnimationOrder(
            animations,
            orderDurations[group.CurrentOrderIndex].Order);
    }

    private bool AdvancePastManuallyStoppedOrder(
        UiAnimationGroupState group,
        IReadOnlyList<UiObject> animations,
        IReadOnlyList<AnimationOrderDuration> orderDurations,
        double totalDuration)
    {
        if (group.Elapsed >= totalDuration - 0.0001 ||
            group.CurrentOrderIndex < 0 ||
            group.CurrentOrderIndex >= orderDurations.Count)
        {
            return false;
        }

        var currentOrder = orderDurations[group.CurrentOrderIndex].Order;
        var currentAnimations = animations
            .Where(animation => animation.Animation!.Order == currentOrder)
            .ToArray();
        if (!currentAnimations.Any(animation => animation.Animation!.ManuallyStopped) ||
            currentAnimations.Any(
                animation => animation.Animation!.PlaybackState is 1 or 2))
        {
            return false;
        }

        group.Elapsed = group.Reverse
            ? orderDurations
                .Skip(group.CurrentOrderIndex)
                .Sum(order => order.Duration)
            : orderDurations
                .Take(group.CurrentOrderIndex + 1)
                .Sum(order => order.Duration);
        group.Elapsed = Math.Min(totalDuration, group.Elapsed);
        AdvanceAnimationOrder(animations, orderDurations, group);
        return true;
    }

    private (UiObject[] Animations, AnimationOrderDuration[] OrderDurations)
        GetAnimationTimeline(UiObject groupObject)
    {
        var animations = groupObject.Children
            .Select(Ui.Find)
            .Where(value => value?.Animation is not null)
            .Cast<UiObject>()
            .ToArray();
        var orderDurations = animations
            .GroupBy(value => value.Animation!.Order)
            .OrderBy(value => value.Key)
            .Select(value => new AnimationOrderDuration(
                value.Key,
                value.Max(animation =>
                    animation.Animation!.StartDelay +
                    animation.Animation.Duration +
                    animation.Animation.EndDelay)))
            .ToArray();
        return (animations, orderDurations);
    }

    private void ApplyAnimationTimeline(
        IReadOnlyList<UiObject> animations,
        IReadOnlyList<AnimationOrderDuration> orderDurations,
        double timeline,
        bool reverse,
        bool dispatchLifecycle)
    {
        var targets = animations
            .Select(animation => ResolveAnimationTarget(animation, animation.Animation!))
            .Where(target => target is not null)
            .Cast<UiObject>()
            .DistinctBy(target => target.Id)
            .ToArray();
        foreach (var target in targets)
        {
            target.AnimationOffset = Vector2.Zero;
            target.AnimationScale = Vector2.One;
            target.AnimationScaleOriginPoint = "CENTER";
            target.AnimationScaleOriginOffset = Vector2.Zero;
            target.FontAnimationFontSizeScale = 1;
            target.FontAnimationVertexScale = 1;
            target.LineAnimationOffset = Vector2.Zero;
            target.LineAnimationScale = Vector2.One;
            target.LineAnimationScaleOriginPoint = "CENTER";
            target.LineAnimationScaleOriginOffset = Vector2.Zero;
            target.AnimationRotation = 0;
            target.AnimationRotationOriginPoint = "CENTER";
            target.AnimationRotationOriginOffset = Vector2.Zero;
        }
        foreach (var animationObject in animations)
        {
            var animation = animationObject.Animation!;
            if (animation.InitialTargetLocalUv is not { } source ||
                ResolveAnimationTarget(animationObject, animation)?.Texture is not { } texture)
                continue;
            source.CopyTo(texture.LocalUv, 0);
            texture.ResolveUv();
        }

        var orderStarts = new Dictionary<int, double>();
        var start = 0d;
        foreach (var order in orderDurations)
        {
            orderStarts[order.Order] = start;
            start += order.Duration;
        }
        var playbackElapsed = reverse ? start - timeline : timeline;
        var activeOrderIndex = ResolveCurrentAnimationOrderIndex(
            orderDurations,
            playbackElapsed,
            reverse);
        var activeOrder = activeOrderIndex >= 0
            ? orderDurations[activeOrderIndex].Order
            : int.MinValue;

        var previousStates = animations.ToDictionary(
            animation => animation.Id,
            animation => (
                animation.Animation!.PlaybackState,
                animation.Animation.Elapsed));
        foreach (var animationObject in animations)
        {
            var animation = animationObject.Animation!;
            if (animation.ManuallyStopped)
            {
                animation.PlaybackState = 0;
                continue;
            }
            if (!orderStarts.TryGetValue(animation.Order, out var orderStart))
                continue;
            var orderDuration = orderDurations
                .First(order => order.Order == animation.Order)
                .Duration;
            var animationDuration =
                animation.StartDelay + animation.Duration + animation.EndDelay;
            double animationElapsed;
            int playbackState;
            if (!reverse)
            {
                var orderElapsed = timeline - orderStart;
                animationElapsed = Math.Clamp(orderElapsed, 0, animationDuration);
                playbackState = orderElapsed < 0
                    ? 0
                    : orderElapsed >= orderDuration ||
                      animationElapsed >= animationDuration
                        ? 3
                        : 1;
            }
            else
            {
                var orderEnd = orderStart + orderDuration;
                var orderElapsed = orderEnd - timeline;
                animationElapsed = Math.Clamp(
                    animationDuration - orderElapsed,
                    0,
                    animationDuration);
                playbackState = timeline > orderEnd
                    ? 0
                    : timeline <= orderStart ||
                      animationElapsed <= 0
                        ? 3
                        : 1;
            }

            var local = animationElapsed - animation.StartDelay;
            var rawProgress = local <= 0
                ? 0
                : animation.Duration <= 0 || local >= animation.Duration
                    ? 1
                    : Math.Clamp(local / animation.Duration, 0, 1);
            var progress = ApplyAnimationSmoothing(rawProgress, animation.Smoothing);
            animation.Elapsed = animationElapsed;
            animation.Progress = rawProgress;
            animation.SmoothProgress = progress;
            animation.PlaybackState = playbackState;
        }

        if (dispatchLifecycle)
        {
            var orders = reverse
                ? orderDurations.Reverse()
                : orderDurations;
            foreach (var order in orders)
            {
                foreach (var animationObject in animations.Where(
                             candidate => candidate.Animation!.Order == order.Order))
                {
                    var animation = animationObject.Animation!;
                    var previous = previousStates[animationObject.Id];
                    if (previous.PlaybackState == 0 && animation.PlaybackState != 0)
                        InvokeScript(animationObject, "OnPlay");
                    if (previous.PlaybackState is 1 or 2 ||
                        Math.Abs(animation.Elapsed - previous.Elapsed) > 0.0001)
                    {
                        InvokeScript(
                            animationObject,
                            "OnUpdate",
                            Math.Abs(animation.Elapsed - previous.Elapsed));
                    }
                    if (animation.PlaybackState == 3 && previous.PlaybackState != 3)
                        InvokeScript(animationObject, "OnFinished");
                }
            }
        }

        foreach (var animationObject in animations)
        {
            var animation = animationObject.Animation!;
            if (!orderStarts.TryGetValue(animation.Order, out var orderStart))
                continue;
            var orderDuration = orderDurations
                .First(order => order.Order == animation.Order)
                .Duration;
            var orderHasStarted = reverse
                ? timeline <= orderStart + orderDuration
                : timeline >= orderStart;
            if (!orderHasStarted ||
                animation.ManuallyStopped && animation.Order == activeOrder)
                continue;

            var progress = animation.SmoothProgress;
            var target = ResolveAnimationTarget(animationObject, animation);
            if (animationObject.ObjectType.Equals("Alpha", StringComparison.OrdinalIgnoreCase) &&
                target is not null)
            {
                var from = (int)MathF.Round(animation.FromAlpha * 255);
                var to = (int)MathF.Round(animation.ToAlpha * 255);
                var alpha = Math.Clamp(from + (int)((to - from) * progress), 0, 255);
                target.AnimationBaseAlpha ??= target.Alpha;
                target.Alpha = alpha / 255f;
            }
            else if (animationObject.ObjectType.Equals(
                         "Translation",
                         StringComparison.OrdinalIgnoreCase) &&
                     target is not null)
            {
                target.AnimationOffset += animation.Offset * (float)progress;
            }
            else if (animationObject.ObjectType.Equals(
                         "LineTranslation",
                         StringComparison.OrdinalIgnoreCase) &&
                     target?.Line is not null)
            {
                target.LineAnimationOffset += animation.Offset * (float)progress;
            }
            else if (animationObject.ObjectType.Equals(
                         "Path",
                         StringComparison.OrdinalIgnoreCase) &&
                     target is not null)
            {
                target.AnimationOffset += EvaluatePathOffset(animationObject, progress);
            }
            else if ((animationObject.ObjectType.Equals(
                          "TextureCoord",
                          StringComparison.OrdinalIgnoreCase) ||
                      animationObject.ObjectType.Equals(
                          "TextureCoordTranslation",
                          StringComparison.OrdinalIgnoreCase)) &&
                     target?.Texture is { } translatedTexture)
            {
                animation.InitialTargetLocalUv ??= translatedTexture.LocalUv.ToArray();
                var source = animation.InitialTargetLocalUv;
                var offset = animation.Offset * (float)progress;
                for (var index = 0; index < translatedTexture.LocalUv.Length; index++)
                    translatedTexture.LocalUv[index] = source[index] + offset;
                translatedTexture.ResolveUv();
            }
            else if (animationObject.ObjectType.Equals(
                         "Rotation",
                         StringComparison.OrdinalIgnoreCase) &&
                     target is not null)
            {
                target.AnimationRotation +=
                    animation.Radians * (float)progress;
                target.AnimationRotationOriginPoint = animation.OriginPoint;
                target.AnimationRotationOriginOffset = animation.OriginOffset;
            }
            else if (animationObject.ObjectType.Equals(
                         "Scale",
                         StringComparison.OrdinalIgnoreCase) &&
                     target is not null)
            {
                var scale = animation.HasScaleRange
                    ? Vector2.Lerp(animation.ScaleFrom, animation.ScaleTo, (float)progress)
                    : Vector2.Lerp(Vector2.One, animation.Scale, (float)progress);
                if (target.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase))
                {
                    if (target.FontScaleAnimationMode == 1)
                        target.FontAnimationVertexScale *= scale.X;
                    else
                        target.FontAnimationFontSizeScale *= scale.X;
                }
                else
                {
                    target.AnimationScale *= scale;
                    target.AnimationScaleOriginPoint = animation.OriginPoint;
                    target.AnimationScaleOriginOffset = animation.OriginOffset;
                }
            }
            else if (animationObject.ObjectType.Equals(
                         "LineScale",
                         StringComparison.OrdinalIgnoreCase) &&
                     target?.Line is not null)
            {
                var scale = animation.HasScaleRange
                    ? Vector2.Lerp(animation.ScaleFrom, animation.ScaleTo, (float)progress)
                    : Vector2.Lerp(Vector2.One, animation.Scale, (float)progress);
                target.LineAnimationScale *= scale;
                target.LineAnimationScaleOriginPoint = animation.OriginPoint;
                target.LineAnimationScaleOriginOffset = animation.OriginOffset;
            }
            else if (animationObject.ObjectType.Equals(
                         "FlipBook",
                         StringComparison.OrdinalIgnoreCase) &&
                     target?.Texture is { } texture)
            {
                ApplyFlipBookFrame(animation, texture, progress);
            }
            else if (animationObject.ObjectType.Equals(
                         "VertexColor",
                         StringComparison.OrdinalIgnoreCase) &&
                     target is not null)
            {
                ApplyAnimationVertexColor(
                    target,
                    InterpolateAnimationColor(
                        animation.StartColor,
                        animation.EndColor,
                        progress));
            }
        }
        if (targets.Length > 0)
            Ui.InvalidateLayout();
    }

    private void ResetAnimationTimeline(
        IReadOnlyList<UiObject> animations,
        IReadOnlyList<AnimationOrderDuration> orderDurations,
        bool reverse)
    {
        foreach (var animationObject in animations)
        {
            var animation = animationObject.Animation!;
            var animationDuration =
                animation.StartDelay + animation.Duration + animation.EndDelay;
            animation.Elapsed = reverse ? animationDuration : 0;
            animation.Progress = reverse ? 1 : 0;
            animation.SmoothProgress = reverse
                ? ApplyAnimationSmoothing(1, animation.Smoothing)
                : ApplyAnimationSmoothing(0, animation.Smoothing);
            animation.PlaybackState = 0;
            animation.ManuallyStopped = false;
        }
    }

    private void ActivateAnimationOrder(
        IReadOnlyList<UiObject> animations,
        IReadOnlyList<AnimationOrderDuration> orderDurations,
        bool reverse)
    {
        if (orderDurations.Count == 0)
            return;
        var order = reverse ? orderDurations[^1].Order : orderDurations[0].Order;
        ActivateAnimationOrder(animations, order);
    }

    private void ActivateAnimationOrder(
        IReadOnlyList<UiObject> animations,
        int order)
    {
        foreach (var animationObject in animations.Where(
                     animation => animation.Animation!.Order == order))
        {
            animationObject.Animation!.ManuallyStopped = false;
            animationObject.Animation!.PlaybackState = 1;
            InvokeScript(animationObject, "OnPlay");
        }
    }

    private static int ResolveCurrentAnimationOrderIndex(
        IReadOnlyList<AnimationOrderDuration> orderDurations,
        double elapsed,
        bool reverse)
    {
        if (orderDurations.Count == 0)
            return -1;

        var consumed = 0d;
        if (reverse)
        {
            for (var index = orderDurations.Count - 1; index >= 0; index--)
            {
                consumed += orderDurations[index].Duration;
                if (elapsed < consumed - 0.0001)
                    return index;
            }
            return 0;
        }

        for (var index = 0; index < orderDurations.Count; index++)
        {
            consumed += orderDurations[index].Duration;
            if (elapsed < consumed - 0.0001)
                return index;
        }
        return orderDurations.Count - 1;
    }

    private void ClearAnimationGroupEffects(
        IReadOnlyList<UiObject> animations,
        bool commitFinalAlpha)
    {
        var targets = animations
            .Select(animation => ResolveAnimationTarget(animation, animation.Animation!))
            .Where(target => target is not null)
            .Cast<UiObject>()
            .DistinctBy(target => target.Id)
            .ToArray();
        foreach (var target in targets)
        {
            if (!commitFinalAlpha && target.AnimationBaseAlpha is { } baseAlpha)
                target.Alpha = baseAlpha;
            target.AnimationBaseAlpha = null;
            target.AnimationOffset = Vector2.Zero;
            target.AnimationScale = Vector2.One;
            target.AnimationScaleOriginPoint = "CENTER";
            target.AnimationScaleOriginOffset = Vector2.Zero;
            target.FontAnimationFontSizeScale = 1;
            target.FontAnimationVertexScale = 1;
            target.LineAnimationOffset = Vector2.Zero;
            target.LineAnimationScale = Vector2.One;
            target.LineAnimationScaleOriginPoint = "CENTER";
            target.LineAnimationScaleOriginOffset = Vector2.Zero;
            target.AnimationRotation = 0;
            target.AnimationRotationOriginPoint = "CENTER";
            target.AnimationRotationOriginOffset = Vector2.Zero;
        }

        foreach (var animationObject in animations)
        {
            var animation = animationObject.Animation!;
            if (animation.InitialTargetLocalUv is not { } source ||
                ResolveAnimationTarget(animationObject, animation)?.Texture is not { } texture)
            {
                continue;
            }
            source.CopyTo(texture.LocalUv, 0);
            texture.ResolveUv();
        }
        if (targets.Length > 0)
            Ui.InvalidateLayout();
    }

    private Vector2 EvaluatePathOffset(UiObject path, double progress)
    {
        var points = Ui.ResolvePathControlPoints(path);
        if (points.Count == 0)
            return Vector2.Zero;

        var absoluteProgress = Math.Abs((float)progress);
        var segment = (int)MathF.Floor(absoluteProgress * points.Count);
        Vector2 result;
        if (segment >= points.Count)
        {
            result = points[^1].ControlPoint!.Offset;
        }
        else
        {
            var currentState = points[segment].ControlPoint!;
            var previousTime = segment == 0
                ? 0
                : points[segment - 1].ControlPoint!.NormalizedTime;
            var denominator = currentState.NormalizedTime - previousTime;
            var amount = denominator == 0
                ? 1
                : (absoluteProgress - previousTime) / denominator;
            var previous = segment == 0
                ? Vector2.Zero
                : points[segment - 1].ControlPoint!.Offset;
            var current = currentState.Offset;

            if (!path.Animation!.PathCurveType.Equals(
                    "SMOOTH",
                    StringComparison.OrdinalIgnoreCase) ||
                points.Count <= 1)
            {
                result = Vector2.Lerp(previous, current, amount);
            }
            else
            {
                var beforePrevious = segment switch
                {
                    0 => -current,
                    1 => Vector2.Zero,
                    _ => points[segment - 2].ControlPoint!.Offset
                };
                var afterCurrent = segment + 1 < points.Count
                    ? points[segment + 1].ControlPoint!.Offset
                    : current * 2 - previous;
                var amountSquared = amount * amount;
                var amountCubed = amountSquared * amount;
                result = .5f * (
                    2 * previous +
                    (-beforePrevious + current) * amount +
                    (2 * beforePrevious - 5 * previous + 4 * current - afterCurrent) *
                    amountSquared +
                    (-beforePrevious + 3 * previous - 3 * current + afterCurrent) *
                    amountCubed);
            }
        }

        return progress < 0 ? -result : result;
    }

    internal UiObject? ResolveAnimationTarget(
        UiObject animationObject,
        UiAnimationState animation)
    {
        var owner = animationObject.ParentId is { } groupId &&
                    Ui.Find(groupId)?.ParentId is { } ownerId
            ? Ui.Find(ownerId)
            : null;
        UiObject? target = animation.TargetMode switch
        {
            UiAnimationTargetMode.Name => Ui.Find(animation.TargetNameOrKey ?? string.Empty),
            UiAnimationTargetMode.TargetKey => ResolveRelativeObjectKey(
                animationObject,
                animation.TargetNameOrKey),
            UiAnimationTargetMode.ChildKey => ResolveRelativeObjectKey(
                owner,
                animation.TargetNameOrKey),
            UiAnimationTargetMode.Direct => animation.TargetId is { } targetId
                ? Ui.Find(targetId)
                : null,
            _ => owner
        };
        return target is not null && IsAnimationTarget(target) ? target : null;
    }

    private UiObject? ResolveRelativeObjectKey(UiObject? start, string? relativeKey)
    {
        if (start is null || string.IsNullOrEmpty(relativeKey))
            return null;

        var current = start;
        foreach (var segment in relativeKey.Split(
                     '.',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Equals("$parent", StringComparison.OrdinalIgnoreCase))
            {
                if (current.ParentId is not { } parentId || Ui.Find(parentId) is not { } parent)
                    return null;
                current = parent;
                continue;
            }

            var baseline = lua_gettop(State);
            try
            {
                PushObject(current);
                lua_getfield(State, -1, segment);
                if (lua_istable(State, -1) == 0)
                    return current;
                lua_getfield(State, -1, "__id");
                if (lua_isnumber(State, -1) == 0 ||
                    Ui.Find((int)lua_tonumber(State, -1)) is not { } child)
                {
                    return current;
                }
                current = child;
            }
            finally
            {
                lua_settop(State, baseline);
            }
        }
        return current;
    }

    private static bool IsAnimationTarget(UiObject value) =>
        value.IsRegion ||
        !value.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase) &&
        !value.ObjectType.Equals("AnimationGroup", StringComparison.OrdinalIgnoreCase) &&
        value.Animation is null &&
        !value.ObjectType.Equals("ModelSceneActor", StringComparison.OrdinalIgnoreCase);

    private static void ApplyFlipBookFrame(
        UiAnimationState animation,
        UiTextureState texture,
        double progress)
    {
        var rows = animation.FlipBookRows;
        var columns = animation.FlipBookColumns;
        if (rows == 0 || columns == 0)
            return;

        var frames = animation.FlipBookFrames != 0
            ? animation.FlipBookFrames
            : unchecked(rows * columns);
        if (frames == 0)
            return;

        var candidate = (ulong)Math.Truncate(progress * frames);
        var frame = (uint)Math.Min(candidate, frames - 1UL);
        var row = frame / columns;
        var column = frame % columns;
        var hasPixelFrameSize =
            animation.FlipBookFrameWidth > 0 &&
            animation.FlipBookFrameHeight > 0 &&
            texture.AtlasWidth is > 0 &&
            texture.AtlasHeight is > 0;
        var frameWidth = hasPixelFrameSize
            ? animation.FlipBookFrameWidth / texture.AtlasWidth!.Value
            : 1f / columns;
        var frameHeight = hasPixelFrameSize
            ? animation.FlipBookFrameHeight / texture.AtlasHeight!.Value
            : 1f / rows;
        var left = column * frameWidth;
        var right = left + frameWidth;
        var top = row * frameHeight;
        var bottom = top + frameHeight;

        animation.InitialTargetLocalUv ??= texture.LocalUv.ToArray();
        var source = animation.InitialTargetLocalUv;
        texture.LocalUv[0] = InterpolateUv(source, left, top);
        texture.LocalUv[1] = InterpolateUv(source, left, bottom);
        texture.LocalUv[2] = InterpolateUv(source, right, top);
        texture.LocalUv[3] = InterpolateUv(source, right, bottom);
        texture.ResolveUv();
    }

    private static Vector4 InterpolateAnimationColor(
        Vector4 start,
        Vector4 end,
        double progress)
    {
        var progressByte = (int)Math.Clamp(progress * 255, 0, 255);
        if (progressByte == 0)
            return start;
        if (progressByte == 255)
            return end;
        return new Vector4(
            InterpolateAnimationColorByte(start.X, end.X, progressByte),
            InterpolateAnimationColorByte(start.Y, end.Y, progressByte),
            InterpolateAnimationColorByte(start.Z, end.Z, progressByte),
            InterpolateAnimationColorByte(start.W, end.W, progressByte));
    }

    private static float InterpolateAnimationColorByte(float start, float end, int progressByte)
    {
        var startByte = (int)MathF.Round(Math.Clamp(start, 0, 1) * 255);
        var endByte = (int)MathF.Round(Math.Clamp(end, 0, 1) * 255);
        var delta = unchecked((ushort)(progressByte * (endByte - startByte))) >> 8;
        return unchecked((byte)(startByte + delta)) / 255f;
    }

    private static void ApplyAnimationVertexColor(UiObject target, Vector4 color)
    {
        if (target.Texture is { } texture)
            texture.VertexColor = color;
        if (target.Font is not null)
            target.VertexColor = color;
        if (target.Line is { } line)
            line.Color = color;
    }

    private static Vector2 InterpolateUv(Vector2[] corners, float x, float y)
    {
        var top = Vector2.Lerp(corners[0], corners[2], x);
        var bottom = Vector2.Lerp(corners[1], corners[3], x);
        return Vector2.Lerp(top, bottom, y);
    }

    private void FinishAnimationGroup(UiObject groupObject, bool requested)
    {
        var group = groupObject.AnimationGroup!;
        var animations = groupObject.Children
            .Select(Ui.Find)
            .Where(value => value?.Animation is not null)
            .Cast<UiObject>()
            .ToArray();
        ClearAnimationGroupEffects(animations, group.SetToFinalAlpha);
        group.Playing = false;
        group.Paused = false;
        group.Finished = true;
        group.PendingFinish = false;
        group.Elapsed = 0;
        group.CurrentOrderIndex = 0;
        InvokeScript(groupObject, "OnFinished", requested);
    }

    private static double ApplyAnimationSmoothing(double value, string smoothing) =>
        smoothing.ToUpperInvariant() switch
        {
            "IN" => value * value,
            "OUT" => 1 - (1 - value) * (1 - value),
            "IN_OUT" => value * value * (3 - 2 * value),
            "OUT_IN" => value * value * (3 - 2 * value),
            _ => value
        };

    internal void PushValue(object? value)
    {
        switch (value)
        {
            case null:
                lua_pushnil(State);
                break;
            case bool boolean:
                lua_pushboolean(State, boolean ? 1 : 0);
                break;
            case string text:
                lua_pushstring(State, text);
                break;
            case byte or short or int or long or float or double or decimal:
                lua_pushnumber(State, Convert.ToDouble(value));
                break;
            case UiObject uiObject:
                PushObject(uiObject);
                break;
            case WowItemLocation itemLocation:
                WowItemApi.PushItemLocation(State, itemLocation);
                break;
            case LuaRegistryValue registryValue:
                lua_rawgeti(State, LUA_REGISTRYINDEX, registryValue.Reference);
                break;
            default:
                lua_pushstring(State, value.ToString() ?? string.Empty);
                break;
        }
    }

    private void SerializeValue(int index, StringBuilder output, int depth, HashSet<UIntPtr> visited)
    {
        if (depth > 64)
        {
            output.Append("nil --[[ depth limit ]]");
            return;
        }

        var absolute = index > 0 || index <= LUA_REGISTRYINDEX ? index : lua_gettop(State) + index + 1;
        switch (lua_type(State, absolute))
        {
            case LUA_TNIL:
                output.Append("nil");
                break;
            case LUA_TBOOLEAN:
                output.Append(lua_toboolean(State, absolute) != 0 ? "true" : "false");
                break;
            case LUA_TNUMBER:
                output.Append(lua_tonumber(State, absolute).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                break;
            case LUA_TSTRING:
                output.Append('"').Append(EscapeLua(lua_tostring(State, absolute) ?? string.Empty)).Append('"');
                break;
            case LUA_TTABLE:
            {
                var pointer = lua_topointer(State, absolute);
                if (!visited.Add(pointer))
                {
                    output.Append("nil --[[ cycle ]]");
                    break;
                }

                output.AppendLine("{");
                lua_pushnil(State);
                while (lua_next(State, absolute) != 0)
                {
                    output.Append(' ', (depth + 1) * 2).Append('[');
                    SerializeValue(-2, output, depth + 1, visited);
                    output.Append("] = ");
                    SerializeValue(-1, output, depth + 1, visited);
                    output.AppendLine(",");
                    lua_pop(State, 1);
                }
                output.Append(' ', depth * 2).Append('}');
                visited.Remove(pointer);
                break;
            }
            default:
                output.Append("nil");
                break;
        }
    }

    private void LoadSavedVariables(AddonManifest manifest)
    {
        try
        {
            var accountPath = AccountSavedVariablesPath(manifest);
            var legacyPath = Path.Combine(SavedVariablesDirectory, $"{manifest.Name}.lua");
            if (File.Exists(accountPath))
                ExecuteSavedVariablesFile(accountPath, manifest.AccountSavedVariables);
            else if (File.Exists(legacyPath))
                ExecuteSavedVariablesFile(legacyPath, manifest.AccountSavedVariables);

            var characterPath = CharacterSavedVariablesPath(manifest);
            if (File.Exists(characterPath))
                ExecuteSavedVariablesFile(characterPath, manifest.CharacterSavedVariables);
        }
        catch (Exception exception)
        {
            RecordLoadFailure(manifest, SavedVariablesDirectory, "saved-variables", exception);
            Log.Error("saved-variables", $"{manifest.Name} data failed to load: {exception.Message}");
        }
    }

    private void ExecuteSavedVariablesFile(string path, IReadOnlyList<string> names)
    {
        var source = File.ReadAllText(path);
        if (!source.StartsWith(SavedVariablesHeader, StringComparison.Ordinal))
        {
            ExecuteFile(path);
            return;
        }

        var declared = names.ToHashSet(StringComparer.Ordinal);
        var migrated = string.Join(
            '\n',
            source.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Where(line => !IsLegacyNilAssignment(line, declared)));
        ExecuteString(migrated, $"@{path}");
    }

    private static bool IsLegacyNilAssignment(string line, IReadOnlySet<string> declared)
    {
        var assignment = line.Trim();
        var separator = assignment.IndexOf('=');
        if (separator < 1 || !assignment[(separator + 1)..].Trim().Equals("nil", StringComparison.Ordinal))
            return false;
        return declared.Contains(assignment[..separator].Trim());
    }

    private void RecordLoadFailure(
        AddonManifest manifest,
        string path,
        string phase,
        Exception exception)
    {
        _addonLoadErrors[manifest.Name] = exception.Message;
        var file = Path.GetRelativePath(manifest.RootPath, path);
        if (file.StartsWith("..", StringComparison.Ordinal))
            file = path;
        _addonLoadFailures.Add(new AddonLoadFailure(
            manifest.Name,
            file.Replace(Path.DirectorySeparatorChar, '/'),
            phase,
            exception.Message));
    }

    private void SaveVariables(IReadOnlyList<string> names, string path, string scope)
    {
        if (names.Count == 0)
            return;
        var output = new StringBuilder();
        output.AppendLine($"{SavedVariablesHeader}{DateTimeOffset.Now:O}");
        foreach (var name in names)
        {
            lua_getglobal(State, name);
            if (lua_isnil(State, -1) != 0)
            {
                lua_pop(State, 1);
                continue;
            }
            output.Append(name).Append(" = ");
            SerializeValue(-1, output, 0, []);
            output.AppendLine();
            lua_pop(State, 1);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, output.ToString(), new UTF8Encoding(false));
        File.Move(temporaryPath, path, true);
        Log.Info("saved-variables", $"Saved {names.Count} {scope} variables to {path}.");
    }

    private string AccountSavedVariablesPath(AddonManifest manifest) =>
        Path.Combine(SavedVariablesDirectory, "Account", $"{manifest.Name}.lua");

    private string CharacterSavedVariablesPath(AddonManifest manifest) =>
        Path.Combine(SavedVariablesDirectory, "Character", $"{manifest.Name}.lua");

    private static IReadOnlySet<string> SecureEnvironmentFiles(AddonManifest manifest)
    {
        if (!manifest.Metadata.TryGetValue("SecureEnvironmentFiles", out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => Path.GetFullPath(Path.Combine(
                manifest.RootPath,
                entry.Replace('\\', Path.DirectorySeparatorChar))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsTrueMetadata(AddonManifest manifest, string key) =>
        manifest.Metadata.TryGetValue(key, out var value) &&
        (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase));

    private int EnsureSecureEnvironment()
    {
        if (_secureEnvironmentReference is { } existing)
            return existing;

        const string code =
            "local global = _G; local environment = {}; " +
            "for key, value in pairs(global) do environment[key] = value end; " +
            "return setmetatable(environment, { __index = global })";
        var status = LuaChunkLoader.Load(State, code, "@WoWAddonLabSecureEnvironment");
        if (status != 0)
            throw BuildLuaException("compile secure addon environment");
        status = lua_pcall(State, 0, 1, 0);
        if (status != 0)
            throw BuildLuaException("create secure addon environment");

        _secureEnvironmentReference = luaL_ref(State, LUA_REGISTRYINDEX);
        return _secureEnvironmentReference.Value;
    }

    private void ApplyCurrentAddonEnvironment(int functionIndex)
    {
        if (_currentAddonEnvironmentReference is not { } environmentReference)
            return;

        var absoluteFunctionIndex = functionIndex < 0
            ? lua_gettop(State) + functionIndex + 1
            : functionIndex;
        lua_rawgeti(State, LUA_REGISTRYINDEX, environmentReference);
        if (lua_setfenv(State, absoluteFunctionIndex) == 0)
            throw new InvalidOperationException("Lua rejected the secure addon environment.");
    }

    private void PushCurrentGlobal(string name)
    {
        if (_currentAddonEnvironmentReference is not { } environmentReference)
        {
            lua_getglobal(State, name);
            return;
        }

        lua_rawgeti(State, LUA_REGISTRYINDEX, environmentReference);
        lua_getfield(State, -1, name);
        lua_remove(State, -2);
    }

    internal int? SwapAddonEnvironment(int? environmentReference)
    {
        var previous = _currentAddonEnvironmentReference;
        _currentAddonEnvironmentReference = environmentReference;
        return previous;
    }

    private static IReadOnlyList<string> RequiredDependencies(AddonManifest manifest)
    {
        string[] keys =
        [
            "Dependencies",
            "Dep",
            "RequiredDep",
            "RequiredDeps",
            "RequiredDependencies"
        ];
        return keys
            .Where(manifest.Metadata.ContainsKey)
            .SelectMany(key => manifest.Metadata[key].Split(
                [',', ' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record PendingGlobalMixin(string Name, int? EnvironmentReference);

    private static string EscapeLua(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\t", "\\t", StringComparison.Ordinal);

    private string FormatValue(int index)
    {
        return lua_type(State, index) switch
        {
            LUA_TNIL => "nil",
            LUA_TBOOLEAN => lua_toboolean(State, index) != 0 ? "true" : "false",
            LUA_TNUMBER => lua_tonumber(State, index).ToString("G17"),
            LUA_TSTRING => lua_tostring(State, index) ?? string.Empty,
            LUA_TTABLE => $"table: 0x{lua_topointer(State, index):x}",
            LUA_TFUNCTION => $"function: 0x{lua_topointer(State, index):x}",
            LUA_TUSERDATA => $"userdata: 0x{lua_topointer(State, index):x}",
            _ => lua_typename(State, lua_type(State, index)) ?? "unknown"
        };
    }

    private Exception BuildLuaException(string operation)
    {
        var message = lua_tostring(State, -1) ?? "unknown Lua error";
        lua_pop(State, 1);
        Log.Error("lua", $"{operation}: {message}");
        return new InvalidOperationException($"Lua failed to {operation}: {message}");
    }

    private static string ReadCompatibilityLayer()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("WowCompat.lua", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)
                           ?? throw new InvalidOperationException("Embedded WowCompat.lua was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private sealed record LuaTimer(
        long Id,
        uint DueTick,
        uint ScheduledAtTick,
        bool ZeroDelay,
        uint IntervalMilliseconds,
        bool Repeating,
        uint? RemainingReschedules,
        int FunctionReference,
        int HandleReference,
        bool Cancelled);

    private sealed record DeferredScriptInvocation(
        int ObjectId,
        string ScriptName,
        object?[] Arguments);

    private readonly record struct GlobalEventCallback(
        UIntPtr Pointer,
        int Reference,
        string? Unit);

    private readonly record struct AnimationOrderDuration(int Order, double Duration);
}
