using System.Globalization;
using System.Numerics;
using WoWAddonLab.Emulator;
using WoWAddonLab.Emulator.Addons;
using WoWAddonLab.Addons;
using WoWAddonLab.Assets;
using WoWAddonLab.Configuration;
using WoWAddonLab.Diagnostics;
using WoWAddonLab.Rendering;
using WoWAddonLab.Automation;
using WoWAddonLab.Emulator.Diagnostics;
using WoWAddonLab.Emulator.UI;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace WoWAddonLab;

public sealed class LabHost : IDisposable
{
    private readonly CommandLineOptions _options;
    private readonly IWindow _window;
    private readonly DesktopViewportCaptureProvider _viewportCapture = new();
    private EmulatorSession? _session;
    private AutomationServer? _automation;
    private GL? _gl;
    private IInputContext? _input;
    private WowImGuiController? _imgui;
    private TextureCache? _textures;
    private AddonFontCache? _addonFonts;
    private WowUiRenderer? _renderer;
    private FontSet _fonts = new([]);
    private IReadOnlyList<WowInstallation> _installations = [];
    private IReadOnlyList<InstalledAddon> _addonCatalog = [];
    private IReadOnlyList<InstalledAddon> _loadedAddons = [];
    private IReadOnlyList<SavedVariablesProfile> _savedVariableProfiles = [];
    private readonly WoWAddonLabSettingsStore _settingsStore = new();
    private WoWAddonLabSettings _settings = new();
    private TactAssetSource? _tact;
    private TactAtlasCatalog? _atlasCatalog;
    private TactDyeColorCatalog? _dyeColorCatalog;
    private TactGlobalColorCatalog? _globalColorCatalog;
    private TactGameRuleCatalog? _gameRuleCatalog;
    private TactMapCatalog? _mapCatalog;
    private TactQuestCatalog? _questCatalog;
    private TactEncounterJournalCatalog? _encounterJournalCatalog;
    private TactGlobalStringCatalog? _globalStringCatalog;
    private TactSpellCatalog? _spellCatalog;
    private TactItemClassCatalog? _itemClassCatalog;
    private TactPaperDollItemFrameCatalog? _inventorySlotCatalog;
    private TactRaceCatalog? _raceCatalog;
    private TactFactionCatalog? _factionCatalog;
    private TactTransmogSetCatalog? _transmogSetCatalog;
    private TactCharacterServiceCatalog? _characterServiceCatalog;
    private TactAchievementCatalog? _achievementCatalog;
    private TactAccountStoreCatalog? _accountStoreCatalog;
    private TactAzeriteEssenceCatalog? _azeriteEssenceCatalog;
    private TactModelInfoCatalog? _modelInfoCatalog;
    private TactMacroIconCatalog? _macroIconCatalog;
    private string? _atlasError;
    private string? _definitionWarning;
    private bool _forceDefinitionUpdate;
    private Task? _tactInitialization;
    private volatile float _loadingProgress;
    private volatile string? _loadingStatus;
    private bool _reloadRequested;
    private int _reloadOverlayFrames;
    private Task? _reloadTask;
    private ReloadPlan? _reloadPlan;
    private BlizzardUiBootstrapResult? _blizzardUi;
    private string? _tactError;
    private int _selectedInstallation;
    private int _selectedProfile;
    private int _tactGeneration;
    private int _tactInitializationGeneration;
    private bool _reloadAfterTactInitialization;
    private bool _paused;
    private bool _step;
    private bool _toolsVisible = true;
    private bool _viewportHovered;
    private bool _fontAtlasCanMutate;
    private bool _viewportFocused = true;
    private bool _leftMouseCaptured;
    private bool _rightMouseCaptured;
    private Vector2 _workspaceOrigin;
    private Vector2 _workspaceSize;
    private float _addonPanelWidth;
    private float _toolPanelWidth;
    private float _workspaceGap;
    private bool _splitterDirty;
    private Vector2 _canvasOrigin;
    private Vector2 _canvasSize;
    private float _canvasScale = 1;
    private int? _selectedObjectId;
    private int? _inspectorHoveredObjectId;
    private bool _inspectorSelectionMode;
    private bool _inspectorRevealSelection;
    private string _addonFilter = string.Empty;
    private string _profileMessage = string.Empty;
    private string _inspectorFilter = string.Empty;
    private string _luaInput = "DM and DM.Game and DM.Game.depth";
    private string _luaResult = string.Empty;
    private string _slashInput = "/dm";
    private string _logSearch = string.Empty;
    private string _logCategory = string.Empty;
    private bool _logShowTrace = true;
    private bool _logShowInformation = true;
    private bool _logShowWarnings = true;
    private bool _logShowErrors = true;
    private bool _logAutoScroll = true;
    private long? _selectedLogSequence;
    private long _lastRenderedLogSequence;
    private int? _remainingSmokeTicks;
    private StartupSpan? _windowInitSpan;
    private bool _startupComplete;
    private bool _disposed;

    public LabHost(CommandLineOptions options)
    {
        _options = options;
        _remainingSmokeTicks = options.Ticks;
        var creationSpan = StartupTimeline.Begin("create application window object");
        var windowOptions = WindowOptions.Default with
        {
            Title = "WoW Addon Lab",
            Size = new Vector2D<int>(1700, 1050),
            API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.ForwardCompatible,
                new APIVersion(4, 0)),
            VSync = true
        };
        _window = Window.Create(windowOptions);
        creationSpan.Dispose();
        _windowInitSpan = StartupTimeline.Begin("Silk.NET window and driver initialization");
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.FramebufferResize += size => _gl?.Viewport(size);
        _window.Closing += DisposeGraphics;
    }

    public void Run() => _window.Run();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _viewportCapture.Dispose();
        _automation?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _session?.Dispose();
        DisposeGraphics();
        _window.Dispose();
    }

    private void OnLoad()
    {
        _windowInitSpan?.Dispose();
        _windowInitSpan = null;
        using var loadSpan = StartupTimeline.Begin("window load callback");

        using (StartupTimeline.Begin("create OpenGL context"))
            _gl = _window.CreateOpenGL();
        using (StartupTimeline.Begin("create input context"))
            _input = _window.CreateInput();
        using (StartupTimeline.Begin("create ImGui controller and shell font atlas"))
            _imgui = CreateImGuiController(_gl, _window, _input);
        var io = ImGui.GetIO();
        io.ConfigWindowsMoveFromTitleBarOnly = true;
        using (StartupTimeline.Begin("configure ImGui style"))
            ConfigureStyle();

        using (var span = StartupTimeline.Begin("detect WoW installations"))
        {
            _installations = WowInstallationDetector.Detect();
            span.Annotate($"{_installations.Count} found");
        }
        using (StartupTimeline.Begin("load emulator settings"))
            _settings = _settingsStore.Load();
        _selectedInstallation = SelectInitialInstallation();
        using (StartupTimeline.Begin("create emulator session (Lua runtime construction)"))
            _session = new EmulatorSession(
                _options.LogicalWidth,
                _options.LogicalHeight,
                luaCacheDirectory: WoWAddonLabPaths.LuaCacheDirectory);
        _session.LoadProgress += OnAddonLoadProgress;
        if (_input.Keyboards.FirstOrDefault() is { } primaryKeyboard)
        {
            _session.ClipboardReader = () => primaryKeyboard.ClipboardText;
            _session.ClipboardWriter = value => primaryKeyboard.ClipboardText = value;
        }
        StartupTimeline.AttachLog(_session.Log);
        if (_installations.Count > 0)
        {
            var installation = _installations[_selectedInstallation];
            StartupTimeline.Fact("Product", $"{installation.Product.ProductCode} ({installation.Version})");
            StartupTimeline.Fact("Product path", installation.ProductPath);
        }

        if (_installations.Count == 0)
        {
            RefreshProduct(loadAddons: true);
        }
        else
        {
            RefreshProduct(loadAddons: false);
            if (ShouldLoadBlizzardUi() && _options.AutoConnectTact)
            {
                using (StartupTimeline.Begin("create placeholder texture renderer"))
                    RecreateTextureRenderer([]);
                _reloadAfterTactInitialization = true;
                BeginTactInitialization();
            }
            else
            {
                Reload();
            }
        }
        using (StartupTimeline.Begin("start automation server"))
        {
            _automation = new AutomationServer(_session, _options.Port, _viewportCapture);
            _automation.StartAsync().GetAwaiter().GetResult();
        }
        if (_options.AutoConnectTact &&
            _installations.Count > 0 &&
            _tactInitialization is null)
            BeginTactInitialization();

        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
            keyboard.KeyChar += OnKeyChar;
        }

        _gl.ClearColor(0.025f, 0.028f, 0.035f, 1);
    }

    private void OnUpdate(double deltaSeconds)
    {
        if (_fontAtlasCanMutate)
        {
            _addonFonts?.ProcessPending();
            _fontAtlasCanMutate = false;
        }
        _imgui!.Update((float)deltaSeconds);
        if (_reloadTask is null)
            UpdateDisplayDpiScale();
        if (_reloadTask is null)
        {
            if (!_paused || _step)
            {
                _session!.Tick(deltaSeconds);
                _step = false;
            }
            else
            {
                _session!.Tick(0);
            }
            SyncUiScaleSettingsFromRuntime();
        }

        if (_remainingSmokeTicks is { } ticks)
        {
            _remainingSmokeTicks = ticks - 1;
            if (_remainingSmokeTicks <= 0)
                _window.Close();
        }
    }

    private void OnRender(double deltaSeconds)
    {
        _gl!.Clear(ClearBufferMask.ColorBufferBit);
        CompleteTactInitialization();
        ServicePendingReload();

        RenderMainMenu();
        UpdateWorkspaceLayout();
        RenderWorkspaceSplitter();
        RenderAddonViewport();
        RenderToolWorkspace();

        _imgui!.Render();
        _viewportCapture.ServicePending(
            _gl,
            _canvasOrigin,
            _canvasSize,
            ImGui.GetIO().DisplayFramebufferScale,
            _window.FramebufferSize);
        _fontAtlasCanMutate = true;
        TryCompleteStartupReport();
    }

    private void TryCompleteStartupReport()
    {
        if (_startupComplete)
            return;
        if (!StartupTimeline.Enabled && !_options.ExitWhenReady)
            return;
        if (_session is null || _renderer is null)
            return;
        if (_tactInitialization is not null || _reloadAfterTactInitialization)
            return;
        if (_reloadTask is not null || _reloadRequested)
            return;
        if (_options.AutoConnectTact && _installations.Count > 0 && ShouldLoadBlizzardUi() && _blizzardUi is null)
            return;

        _startupComplete = true;
        StartupTimeline.Ready("first frame drawn with addon content");
        StartupReportWriter.Write(_options, "Startup Timing (desktop)");
        if (_options.ExitWhenReady)
            _window.Close();
    }

    private void RenderMainMenu()
    {
        if (!ImGui.BeginMainMenuBar())
            return;
        if (ImGui.BeginMenu("Emulator"))
        {
            if (ImGui.MenuItem("Reload", "Ctrl+R"))
                Reload();
            if (ImGui.MenuItem("Update DB2 definitions", null, false, _tactInitialization is null))
                UpdateDefinitionsAndReload();
            if (ImGui.MenuItem(_paused ? "Resume" : "Pause", "F6"))
                _paused = !_paused;
            if (ImGui.MenuItem("Step frame", "F7", false, _paused))
                _step = true;
            if (ImGui.MenuItem("Exit"))
                _window.Close();
            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.MenuItem("Tool workspace", "F1", _toolsVisible))
                _toolsVisible = !_toolsVisible;
            ImGui.EndMenu();
        }

        ImGui.Separator();
        var stateColor = _paused
            ? new Vector4(1, 0.72f, 0.28f, 1)
            : new Vector4(0.38f, 0.86f, 0.54f, 1);
        ImGui.TextColored(stateColor, _paused ? "PAUSED" : "RUNNING");
        ImGui.Separator();
        ImGui.TextDisabled($"{_session!.Ui.Objects.Count:n0} regions");
        ImGui.Separator();
        ImGui.TextDisabled($"API 127.0.0.1:{_options.Port}");
        ImGui.EndMainMenuBar();
    }

    private void UpdateWorkspaceLayout()
    {
        var viewport = ImGui.GetMainViewport();
        _workspaceOrigin = viewport.WorkPos;
        _workspaceSize = viewport.WorkSize;
        _workspaceGap = _toolsVisible ? 8 : 0;
        if (!_toolsVisible)
        {
            _toolPanelWidth = 0;
            _addonPanelWidth = _workspaceSize.X;
            return;
        }

        var minimumViewportWidth = MathF.Min(480, _workspaceSize.X * 0.55f);
        var maximumToolWidth = MathF.Max(240, _workspaceSize.X - minimumViewportWidth - _workspaceGap);
        _toolPanelWidth = Math.Clamp(_settings.ToolPanelWidth, 240, maximumToolWidth);
        _addonPanelWidth = MathF.Max(0, _workspaceSize.X - _toolPanelWidth - _workspaceGap);
    }

    private void RenderWorkspaceSplitter()
    {
        if (!_toolsVisible)
            return;

        var position = _workspaceOrigin + new Vector2(_addonPanelWidth, 0);
        ImGui.SetNextWindowPos(position);
        ImGui.SetNextWindowSize(new Vector2(_workspaceGap, _workspaceSize.Y));
        var flags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNav;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("##workspace-splitter", flags);
        ImGui.PopStyleVar();
        ImGui.InvisibleButton("##horizontal-resize", new Vector2(_workspaceGap, _workspaceSize.Y));
        if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
        if (ImGui.IsItemActive())
        {
            var mouse = ImGui.GetMousePos();
            var requested = _workspaceOrigin.X + _workspaceSize.X - mouse.X;
            var minimumViewportWidth = MathF.Min(480, _workspaceSize.X * 0.55f);
            var maximum = MathF.Max(240, _workspaceSize.X - minimumViewportWidth - _workspaceGap);
            _settings.ToolPanelWidth = Math.Clamp(requested, 240, maximum);
            _splitterDirty = true;
        }
        if (_splitterDirty && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            _splitterDirty = false;
            PersistSettings();
        }
        ImGui.End();
    }

    private void RenderAddonViewport()
    {
        ImGui.SetNextWindowPos(_workspaceOrigin);
        ImGui.SetNextWindowSize(new Vector2(_addonPanelWidth, _workspaceSize.Y));
        var flags =
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        var viewportOpen = ImGui.Begin("Addon Viewport", flags);
        ImGui.PopStyleVar(2);
        if (!viewportOpen)
        {
            ImGui.End();
            return;
        }

        var contentOrigin = ImGui.GetCursorScreenPos();
        var available = ImGui.GetContentRegionAvail();
        if (_reloadTask is not null)
        {
            var drawing = ImGui.GetWindowDrawList();
            drawing.AddRectFilled(contentOrigin, contentOrigin + available, 0xFF050506);
            RenderLoadingOverlay(drawing, contentOrigin, available);
            ImGui.End();
            return;
        }
        var scale = MathF.Max(0.01f, MathF.Min(
            available.X / _session!.Ui.LogicalWidth,
            available.Y / _session.Ui.LogicalHeight));
        _canvasScale = scale;
        _canvasSize = new Vector2(_session.Ui.LogicalWidth * scale, _session.Ui.LogicalHeight * scale);
        _canvasOrigin = contentOrigin + Vector2.Max(Vector2.Zero, (available - _canvasSize) / 2);

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(contentOrigin, contentOrigin + available, 0xFF050506);
        drawList.PushClipRect(_canvasOrigin, _canvasOrigin + _canvasSize, true);
        RenderViewportGrid(drawList, _canvasOrigin, _canvasSize, scale);
        if (_loadingStatus is null)
        {
            RouteCanvasInput(ImGui.IsWindowHovered());
        }
        else
        {
            _viewportHovered = false;
            _inspectorHoveredObjectId = null;
            _leftMouseCaptured = false;
            _rightMouseCaptured = false;
        }
        _renderer!.Render(
            drawList,
            _session.Ui,
            _canvasOrigin,
            scale,
            _session.MapProvider);
        RenderInspectorSelectionOverlay(drawList);
        drawList.PopClipRect();
        RenderLoadingOverlay(drawList, contentOrigin, available);
        ImGui.End();
    }

    private static void RenderViewportGrid(
        ImDrawListPtr drawList,
        Vector2 origin,
        Vector2 size,
        float scale)
    {
        const uint backgroundColor = 0xFF171719;
        const uint lineColor = 0xFF000000;
        const float logicalCellSize = 32;
        var end = origin + size;
        drawList.AddRectFilled(origin, end, backgroundColor);

        var cellSize = Math.Max(8, logicalCellSize * scale);
        for (var x = origin.X; x <= end.X; x += cellSize)
        {
            var snappedX = MathF.Round(x);
            drawList.AddLine(
                new Vector2(snappedX, origin.Y),
                new Vector2(snappedX, end.Y),
                lineColor,
                1);
        }
        for (var y = origin.Y; y <= end.Y; y += cellSize)
        {
            var snappedY = MathF.Round(y);
            drawList.AddLine(
                new Vector2(origin.X, snappedY),
                new Vector2(end.X, snappedY),
                lineColor,
                1);
        }
    }

    private void RenderToolWorkspace()
    {
        if (!_toolsVisible)
        {
            _inspectorSelectionMode = false;
            _inspectorHoveredObjectId = null;
            return;
        }

        ImGui.SetNextWindowPos(_workspaceOrigin + new Vector2(_addonPanelWidth + _workspaceGap, 0));
        ImGui.SetNextWindowSize(new Vector2(_toolPanelWidth, _workspaceSize.Y));
        var flags =
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoSavedSettings;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        var toolsOpen = ImGui.Begin("WoW Addon Lab Tools", flags);
        ImGui.PopStyleVar(2);
        if (!toolsOpen)
        {
            ImGui.End();
            return;
        }

        if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            _viewportFocused = false;

        var inspectorActive = false;
        if (ImGui.BeginTabBar("##workspace-tabs", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            if (ImGui.BeginTabItem("Addons"))
            {
                RenderAddonManagerContent();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Runtime"))
            {
                RenderRuntimeContent();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Inspector"))
            {
                inspectorActive = true;
                RenderInspectorContent();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Console"))
            {
                RenderConsoleContent();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Log"))
            {
                RenderLogContent();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
        _inspectorSelectionMode = inspectorActive;
        if (!inspectorActive)
            _inspectorHoveredObjectId = null;

        ImGui.End();
    }

    private void RenderLoadingOverlay(
        ImDrawListPtr drawList,
        Vector2 panelOrigin,
        Vector2 panelSize)
    {
        if (_loadingStatus is not { } status)
            return;

        if (panelSize.X <= 1 || panelSize.Y <= 1)
            return;

        var progress = _loadingProgress;
        var indeterminate = progress < 0;
        var title = _tactInitialization is not null
            ? "Preparing game data"
            : _reloadTask is not null
                ? "Loading Blizzard UI and addons"
                : "Reloading addons";
        var cardWidth = MathF.Min(480, MathF.Max(240, panelSize.X - 48));
        const float cardHeight = 112;
        const float horizontalPadding = 22;
        var cardSize = new Vector2(cardWidth, cardHeight);
        var cardOrigin = panelOrigin + (panelSize - cardSize) / 2;
        var cardEnd = cardOrigin + cardSize;

        drawList.PushClipRect(panelOrigin, panelOrigin + panelSize, true);
        drawList.AddRectFilled(
            panelOrigin,
            panelOrigin + panelSize,
            ImGui.GetColorU32(new Vector4(0.02f, 0.022f, 0.028f, 0.72f)));
        drawList.AddRectFilled(
            cardOrigin,
            cardEnd,
            ImGui.GetColorU32(new Vector4(0.075f, 0.078f, 0.09f, 0.97f)),
            6);

        var titleFont = _fonts.Select(22);
        drawList.AddText(
            titleFont.Font,
            titleFont.GlyphSize,
            cardOrigin + new Vector2(horizontalPadding, 18),
            ImGui.GetColorU32(new Vector4(0.95f, 0.95f, 0.96f, 1)),
            title);

        var innerWidth = cardWidth - horizontalPadding * 2;
        RenderLoadingBar(
            drawList,
            cardOrigin + new Vector2(horizontalPadding, 57),
            new Vector2(innerWidth, 10),
            progress,
            indeterminate);

        var statusFont = _fonts.Select(16);
        var statusOrigin = cardOrigin + new Vector2(horizontalPadding, 79);
        var percentWidth = 0f;
        var percent = string.Empty;
        if (!indeterminate)
        {
            percent = $"{Math.Clamp(progress, 0, 1) * 100:0}%";
            percentWidth = statusFont.Font.CalcTextSizeA(
                statusFont.GlyphSize,
                float.MaxValue,
                0,
                percent).X;
        }

        drawList.PushClipRect(
            statusOrigin,
            new Vector2(cardEnd.X - horizontalPadding - percentWidth - 12, cardEnd.Y),
            true);
        drawList.AddText(
            statusFont.Font,
            statusFont.GlyphSize,
            statusOrigin,
            ImGui.GetColorU32(new Vector4(0.72f, 0.74f, 0.78f, 1)),
            status);
        drawList.PopClipRect();

        if (!indeterminate)
        {
            drawList.AddText(
                statusFont.Font,
                statusFont.GlyphSize,
                new Vector2(cardEnd.X - horizontalPadding - percentWidth, statusOrigin.Y),
                ImGui.GetColorU32(new Vector4(0.85f, 0.62f, 0.28f, 1)),
                percent);
        }
        drawList.PopClipRect();
    }

    private static void RenderLoadingBar(
        ImDrawListPtr drawList,
        Vector2 origin,
        Vector2 size,
        float progress,
        bool indeterminate)
    {
        var radius = size.Y / 2;
        drawList.AddRectFilled(
            origin,
            origin + size,
            ImGui.GetColorU32(new Vector4(0.16f, 0.17f, 0.2f, 1)),
            radius);

        var fill = ImGui.GetColorU32(new Vector4(0.85f, 0.62f, 0.28f, 1));
        if (indeterminate)
        {
            const float segment = 0.28f;
            var phase = (float)(ImGui.GetTime() * 0.75 % 1.0);
            var start = (size.X + segment * size.X) * phase - segment * size.X;
            var left = MathF.Max(0, start);
            var right = MathF.Min(size.X, start + segment * size.X);
            if (right > left)
            {
                drawList.AddRectFilled(
                    origin + new Vector2(left, 0),
                    origin + new Vector2(right, size.Y),
                    fill,
                    radius);
            }
        }
        else
        {
            var width = size.X * Math.Clamp(progress, 0, 1);
            if (width > 0.5f)
                drawList.AddRectFilled(origin, origin + new Vector2(width, size.Y), fill, radius);
        }

    }

    private bool SessionBusy => _reloadTask is not null;

    private static void RenderSessionBusyNotice() =>
        ImGui.TextDisabled("Loading addons...");

    private void RenderRuntimeContent()
    {
        if (SessionBusy)
        {
            RenderSessionBusyNotice();
            return;
        }
        if (ImGui.BeginTable("##runtime-metrics", 2, ImGuiTableFlags.SizingStretchProp))
        {
            RenderMetric("Lua", _session!.Lua.GetVersion());
            RenderMetric("Addons", $"{_session.Manifests.Count}");
            RenderMetric("Blizzard UI", $"{_session.BootstrapManifests.Count} modules");
            RenderMetric("Regions", $"{_session.Ui.Objects.Count:n0}");
            RenderMetric("Drawn", $"{_renderer!.DrawnObjects:n0}");
            RenderMetric("Textures", $"{_textures!.LoadedCount} loaded / {_textures.MissingCount} missing");
            RenderMetric("Automation", _automation!.BaseUrl);
            ImGui.EndTable();
        }
        if (_session!.LastError is not null)
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1, 0.3f, 0.25f, 1), _session.LastError);
        }
        if (_session.BootstrapLastError is not null)
        {
            ImGui.Spacing();
            ImGui.TextColored(
                new Vector4(1, 0.67f, 0.25f, 1),
                "Blizzard UI compatibility errors are available in the log.");
        }

        ImGui.SeparatorText("Execution");
        if (ImGui.Button("Reload enabled addons"))
            Reload();
        ImGui.SameLine();
        if (ImGui.Button(_paused ? "Resume" : "Pause"))
            _paused = !_paused;
        ImGui.SameLine();
        ImGui.BeginDisabled(!_paused);
        if (ImGui.Button("Step"))
            _step = true;
        ImGui.EndDisabled();

        RenderUiScaleControls();

        ImGui.SeparatorText("World of Warcraft assets");
        if (_tactInitialization is { IsCompleted: false })
            ImGui.TextDisabled("TACTSharp is indexing the selected build...");
        else if (_tact?.IsInitialized == true)
            ImGui.TextColored(new Vector4(0.35f, 0.9f, 0.5f, 1), $"Connected: {_tact.Description}");
        else if (_installations.Count > 0 && ImGui.Button("Connect TACTSharp"))
            BeginTactInitialization();
        else if (_installations.Count == 0)
            ImGui.TextDisabled("No WoW installation detected.");
        if (_tactError is not null)
            ImGui.TextWrapped($"TACT error: {_tactError}");
    }

    private void RenderUiScaleControls()
    {
        ImGui.SeparatorText("UI scale");
        var ui = _session!.Ui;
        var useUiScale = ui.UseUiScale;
        if (ImGui.Checkbox("Enable UI Scale", ref useUiScale))
            _session.Lua.CVars.SetValue("useUiScale", useUiScale ? "1" : "0");

        var requestedScale = ui.UiScale;
        ImGui.BeginDisabled(!useUiScale);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderFloat("UI Scale", ref requestedScale, .65f, 1.15f, "%.2f"))
        {
            _session.Lua.CVars.SetValue(
                "uiScale",
                requestedScale.ToString("R", CultureInfo.InvariantCulture));
        }
        ImGui.EndDisabled();

        var automaticMultiplierOverride = ui.UiScaleMultiplier is >= .5f and <= 2f;
        ImGui.BeginDisabled(useUiScale);
        if (ImGui.Checkbox("Override automatic DPI multiplier", ref automaticMultiplierOverride))
        {
            var value = automaticMultiplierOverride
                ? Math.Clamp(ui.ScreenDpiScale, .5f, 2f)
                : -1;
            _session.Lua.CVars.SetValue(
                "uiScaleMultiplier",
                value.ToString("R", CultureInfo.InvariantCulture));
        }

        var automaticMultiplier = automaticMultiplierOverride
            ? ui.UiScaleMultiplier
            : Math.Clamp(ui.ScreenDpiScale, .5f, 2f);
        ImGui.BeginDisabled(!automaticMultiplierOverride);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderFloat(
                "Automatic multiplier",
                ref automaticMultiplier,
                .5f,
                2f,
                "%.2f"))
        {
            _session.Lua.CVars.SetValue(
                "uiScaleMultiplier",
                automaticMultiplier.ToString("R", CultureInfo.InvariantCulture));
        }
        ImGui.EndDisabled();
        ImGui.EndDisabled();

        ImGui.TextDisabled(
            $"Applied {ui.AppliedUiScale:0.000}  |  DPI {ui.ScreenDpiScale * 100:0}%  |  " +
            $"Display {ui.PhysicalWidth:0} x {ui.PhysicalHeight:0}");
        if (!useUiScale && !automaticMultiplierOverride)
        {
            ImGui.TextWrapped(
                "Automatic mode uses the host display DPI and WoW's 768-pixel baseline.");
        }
    }

    private void UpdateDisplayDpiScale()
    {
        if (_session is null)
            return;
        var framebufferScale = ImGui.GetIO().DisplayFramebufferScale;
        var dpiScale = framebufferScale.Y > 0 ? framebufferScale.Y : 1;
        _session.Lua.SetScreenDpiScale(dpiScale);
    }

    private void SyncUiScaleSettingsFromRuntime()
    {
        if (_installations.Count == 0 || _session is null || !_session.Lua.IsLoaded)
            return;

        var productSettings = CurrentProductSettings();
        var ui = _session.Ui;
        var changed = productSettings.UseUiScale != ui.UseUiScale ||
                      productSettings.UiScale is not { } savedScale ||
                      MathF.Abs(savedScale - ui.UiScale) >= 0.0001f ||
                      productSettings.UiScaleMultiplier is not { } savedMultiplier ||
                      MathF.Abs(savedMultiplier - ui.UiScaleMultiplier) >= 0.0001f;
        if (!changed)
            return;

        productSettings.UseUiScale = ui.UseUiScale;
        productSettings.UiScale = ui.UiScale;
        productSettings.UiScaleMultiplier = ui.UiScaleMultiplier;
        PersistSettings();
    }

    private void RenderAddonManagerContent()
    {
        if (_installations.Count == 0)
        {
            ImGui.TextWrapped(
                _options.AddonPath is null
                    ? "No World of Warcraft installation was detected."
                    : $"Legacy addon folder: {_options.AddonPath}");
            ImGui.TextDisabled("Use --addon <folder> only as a compatibility fallback.");
            return;
        }

        var installation = _installations[_selectedInstallation];
        if (ImGui.BeginCombo(
                "Product",
                $"{installation.Product.UninstallName} {installation.Version}"))
        {
            for (var index = 0; index < _installations.Count; index++)
            {
                var candidate = _installations[index];
                var selected = index == _selectedInstallation;
                if (ImGui.Selectable(
                        $"{candidate.Product.UninstallName} {candidate.Version}##{candidate.ProductPath}",
                        selected) &&
                    !selected)
                {
                    SelectProduct(index);
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(installation.ProductPath);

        var productSettings = CurrentProductSettings();
        var loadBlizzardUi = ShouldLoadBlizzardUi();
        ImGui.BeginDisabled(_options.LoadBlizzardUi.HasValue);
        if (ImGui.Checkbox("Load Blizzard UI before addons", ref loadBlizzardUi))
        {
            productSettings.LoadBlizzardUi = loadBlizzardUi;
            PersistSettings();
            _blizzardUi = null;
            if (loadBlizzardUi && _options.AutoConnectTact)
            {
                _reloadAfterTactInitialization = true;
                BeginTactInitialization();
            }
            else
            {
                _reloadAfterTactInitialization = false;
                Reload();
            }
        }
        ImGui.EndDisabled();
        if (_options.LoadBlizzardUi.HasValue && ImGui.IsItemHovered())
            ImGui.SetTooltip("This setting is fixed by the command line.");
        if (loadBlizzardUi)
        {
            if (_tactInitialization is { IsCompleted: false })
                ImGui.TextDisabled("Preparing Blizzard UI from the selected game build...");
            else if (_blizzardUi is not null)
                ImGui.TextDisabled(
                    $"{_blizzardUi.ModuleNames.Count} startup modules · {_blizzardUi.ExtractedFiles} cached files" +
                    (_blizzardUi.CacheHit ? " · reused" : " · refreshed"));
            else if (!_options.AutoConnectTact)
                ImGui.TextDisabled("TACTSharp is disabled; Blizzard UI cannot be loaded.");
        }

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##addon-filter", "Filter installed addons...", ref _addonFilter, 256);

        if (ImGui.Button("All off"))
        {
            productSettings.EnabledAddons.Clear();
            PersistSettings();
        }
        ImGui.SameLine();
        if (ImGui.Button("Apply / reload"))
            Reload();
        ImGui.SameLine();
        if (ImGui.Button("Rescan"))
            RefreshProduct(loadAddons: false);

        var filtered = _addonCatalog
            .Where(value =>
                string.IsNullOrWhiteSpace(_addonFilter) ||
                value.PlainTitle.Contains(_addonFilter, StringComparison.OrdinalIgnoreCase) ||
                value.DirectoryName.Contains(_addonFilter, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var listHeight = Math.Clamp(ImGui.GetContentRegionAvail().Y * 0.45f, 160, 420);
        if (ImGui.BeginChild("installed-addons", new Vector2(0, listHeight), ImGuiChildFlags.Border))
        {
            foreach (var addon in filtered)
            {
                if (addon.Manifest is null)
                {
                    ImGui.BeginDisabled();
                    var invalid = false;
                    ImGui.Checkbox($"##invalid-{addon.DirectoryName}", ref invalid);
                    ImGui.SameLine();
                    ImGui.TextWrapped($"{addon.DirectoryName} — {addon.Error}");
                    ImGui.EndDisabled();
                    continue;
                }

                var enabled = productSettings.EnabledAddons.Contains(addon.DirectoryName);
                if (ImGui.Checkbox($"##enabled-{addon.DirectoryName}", ref enabled))
                {
                    if (enabled)
                        productSettings.EnabledAddons.Add(addon.DirectoryName);
                    else
                        productSettings.EnabledAddons.Remove(addon.DirectoryName);
                    PersistSettings();
                }
                ImGui.SameLine();
                ImGui.BeginGroup();
                RenderWowColoredText(addon.Title);
                if (!string.IsNullOrWhiteSpace(addon.Version))
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled(addon.Version);
                }
                if (addon.LoadOnDemand)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled("load on demand");
                }
                ImGui.EndGroup();
                if (ImGui.IsItemHovered())
                {
                    var dependencies = addon.RequiredDependencies.Count == 0
                        ? "None"
                        : string.Join(", ", addon.RequiredDependencies);
                    ImGui.SetTooltip($"{addon.DirectoryName}\nInterface: {addon.InterfaceVersion}\nRequired: {dependencies}");
                }
            }
        }
        ImGui.EndChild();
        ImGui.TextDisabled(
            $"{productSettings.EnabledAddons.Count} selected · {_loadedAddons.Count} loaded including dependencies");

        ImGui.SeparatorText("Saved variables profile");
        _selectedProfile = Math.Clamp(_selectedProfile, 0, Math.Max(0, _savedVariableProfiles.Count - 1));
        var profile = _savedVariableProfiles[_selectedProfile];
        var accountProfiles = _savedVariableProfiles
            .Where(value => value.AccountName is not null && value.CharacterName is null)
            .ToArray();
        if (ImGui.BeginCombo("Account", profile.AccountName ?? "Emulator-only (this product)"))
        {
            var cleanIndex = _savedVariableProfiles.ToList().FindIndex(value => value.AccountName is null);
            if (ImGui.Selectable("Emulator-only (this product)", profile.AccountName is null))
                SelectSavedVariablesProfile(cleanIndex);
            foreach (var accountProfile in accountProfiles)
            {
                if (ImGui.Selectable(
                        accountProfile.AccountName!,
                        string.Equals(
                            accountProfile.AccountName,
                            profile.AccountName,
                            StringComparison.OrdinalIgnoreCase)))
                    SelectSavedVariablesProfile(_savedVariableProfiles.ToList().IndexOf(accountProfile));
            }
            ImGui.EndCombo();
        }

        profile = _savedVariableProfiles[_selectedProfile];
        ImGui.BeginDisabled(profile.AccountName is null);
        var characterProfiles = _savedVariableProfiles
            .Where(value =>
                value.CharacterName is not null &&
                value.AccountName!.Equals(profile.AccountName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var characterPreview = profile.CharacterName is null
            ? "Account-wide only"
            : $"{profile.CharacterName} — {profile.RealmName}";
        if (ImGui.BeginCombo("Character", characterPreview))
        {
            var accountIndex = _savedVariableProfiles.ToList().FindIndex(value =>
                value.AccountName?.Equals(profile.AccountName, StringComparison.OrdinalIgnoreCase) == true &&
                value.CharacterName is null);
            if (ImGui.Selectable("Account-wide only", profile.CharacterName is null))
                SelectSavedVariablesProfile(accountIndex);
            foreach (var characterProfile in characterProfiles)
            {
                if (ImGui.Selectable(
                        $"{characterProfile.CharacterName} — {characterProfile.RealmName}",
                        characterProfile.Id.Equals(profile.Id, StringComparison.OrdinalIgnoreCase)))
                    SelectSavedVariablesProfile(_savedVariableProfiles.ToList().IndexOf(characterProfile));
            }
            ImGui.EndCombo();
        }
        ImGui.EndDisabled();

        profile = _savedVariableProfiles[_selectedProfile];
        var emulatorDirectory = CurrentSavedVariablesDirectory();
        ImGui.TextDisabled("Emulator-owned data");
        ImGui.TextWrapped(emulatorDirectory);
        if (profile.AccountSourceDirectory is not null || profile.CharacterSourceDirectory is not null)
        {
            if (ImGui.Button("Import copies from WoW"))
                ImportSavedVariablesProfile();
            ImGui.SameLine();
            ImGui.TextDisabled("WoW's WTF folder is never written.");
        }
        else
        {
            ImGui.TextDisabled("This selection has no WoW addon data. New data stays in the emulator.");
        }
        if (!string.IsNullOrWhiteSpace(_profileMessage))
            ImGui.TextWrapped(_profileMessage);
    }

    private static void RenderMetric(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(value);
    }

    private void RenderInspectorContent()
    {
        if (SessionBusy)
        {
            RenderSessionBusyNotice();
            return;
        }
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(.38f, .78f, 1, 1));
        ImGui.TextUnformatted("Select mode active");
        ImGui.PopStyleColor();
        ImGui.SameLine();
        ImGui.TextDisabled("Click a visible frame in the viewport.");

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##frame-filter", "Filter by name or type...", ref _inspectorFilter, 256);

        var availableHeight = ImGui.GetContentRegionAvail().Y;
        var treeHeight = Math.Clamp(availableHeight * .4f, 180, 360);
        var revealPath = _inspectorRevealSelection &&
                         _selectedObjectId is { } selectedId
            ? InspectorSelectionPath(selectedId)
            : null;
        if (ImGui.BeginChild("frame-tree", new Vector2(0, treeHeight), ImGuiChildFlags.Border))
        {
            if (string.IsNullOrWhiteSpace(_inspectorFilter))
            {
                foreach (var root in _session!.Ui.Objects.Values.Where(value => value.ParentId is null).ToArray())
                    RenderObjectNode(root, revealPath);
            }
            else
            {
                var matches = _session!.Ui.Objects.Values
                    .Where(value =>
                        (value.Name?.Contains(_inspectorFilter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        value.ObjectType.Contains(_inspectorFilter, StringComparison.OrdinalIgnoreCase))
                    .Take(500)
                    .ToArray();
                ImGui.TextDisabled($"{matches.Length}{(matches.Length == 500 ? "+" : string.Empty)} matches");
                foreach (var match in matches)
                {
                    if (ImGui.Selectable(
                            $"{match.Name ?? "(anonymous)"} [{match.ObjectType} #{match.Id}]",
                            _selectedObjectId == match.Id))
                        _selectedObjectId = match.Id;
                }
            }
        }
        ImGui.EndChild();
        _inspectorRevealSelection = false;

        if (_selectedObjectId is { } id && _session!.Ui.Find(id) is { } selected)
        {
            RenderInspectorDetails(selected);
            RenderInspectorPreview(selected);
        }
        else
        {
            ImGui.SeparatorText("Selection");
            ImGui.TextDisabled("Select a frame to inspect its computed layout and scripts.");
        }
    }

    private void RenderInspectorDetails(UiObject selected)
    {
        ImGui.SeparatorText("Selection");
        var title = $"{selected.Name ?? "(anonymous)"} [{selected.ObjectType} #{selected.Id}]";
        ImGui.TextUnformatted(title);

        if (ImGui.BeginChild(
                "frame-details",
                new Vector2(0, 190),
                ImGuiChildFlags.Border))
        {
            var bounds = _session!.Ui.ResolveBounds(selected.Id);
            var parent = selected.ParentId is { } parentId
                ? _session.Ui.Find(parentId)
                : null;
            if (ImGui.BeginTable(
                    "##selection-properties",
                    2,
                    ImGuiTableFlags.SizingStretchProp |
                    ImGuiTableFlags.RowBg))
            {
                RenderMetric(
                    "Parent",
                    parent is null
                        ? "None"
                        : $"{parent.Name ?? parent.ObjectType} #{parent.Id}");
                RenderMetric(
                    "Bounds",
                    $"{bounds.Left:0.0}, {bounds.Bottom:0.0} · {bounds.Width:0.0} × {bounds.Height:0.0}");
                RenderMetric(
                    "Layer",
                    $"{_session.Ui.EffectiveFrameStrata(selected)}/" +
                    $"{_session.Ui.EffectiveFrameLevel(selected)} · " +
                    $"{selected.DrawLayer}/{selected.SubLevel}");
                RenderMetric(
                    "Visibility",
                    $"shown {selected.Shown}, effective {_session.Ui.IsVisible(selected)}");
                RenderMetric(
                    "Transform",
                    $"scale {selected.Scale:0.###} " +
                    $"(effective {_session.Ui.EffectiveScale(selected):0.###}), " +
                    $"alpha {selected.Alpha:0.###}");
                RenderMetric(
                    "Children",
                    $"{selected.Children.Count} direct, {CountSubtree(selected) - 1} total descendants");
                RenderMetric(
                    "Input",
                    $"click {selected.MouseClickEnabled}, motion {selected.MouseMotionEnabled}, " +
                    $"keyboard {selected.KeyboardEnabled}");
                RenderMetric(
                    "Clipping",
                    selected.ClipsChildren ? "clips children" : "none");
                ImGui.EndTable();
            }

            foreach (var anchor in selected.Anchors)
            {
                var relative = anchor.RelativeToId is { } relativeId
                    ? _session.Ui.Find(relativeId)
                    : null;
                ImGui.TextWrapped(
                    $"{anchor.Point} → " +
                    $"{relative?.Name ?? relative?.ObjectType ?? "UIParent"}." +
                    $"{anchor.RelativePoint}  ({anchor.X:0.##}, {anchor.Y:0.##})");
            }
            if (selected.Texture is { } texture)
                ImGui.TextWrapped($"Texture: {texture.Asset ?? texture.AtlasName ?? $"file {texture.FileDataId}"}");
            if (selected.Font is { Text.Length: > 0 } font)
                ImGui.TextWrapped($"Text: {font.Text}");
            if (selected.ScriptReferences.Count > 0)
            {
                ImGui.TextWrapped(
                    $"Scripts: {string.Join(", ", selected.ScriptReferences.Keys.Order())}");
            }
        }
        ImGui.EndChild();
    }

    private void RenderInspectorPreview(UiObject selected)
    {
        ImGui.SeparatorText("Isolated preview");
        var previewHeight = MathF.Max(180, ImGui.GetContentRegionAvail().Y);
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse;
        if (ImGui.BeginChild(
                "selection-preview",
                new Vector2(0, previewHeight),
                ImGuiChildFlags.Border,
                flags))
        {
            var origin = ImGui.GetCursorScreenPos();
            var size = ImGui.GetContentRegionAvail();
            var drawList = ImGui.GetWindowDrawList();
            RenderInspectorPreviewGrid(drawList, origin, size);
            if (!_renderer!.RenderSubtree(
                    drawList,
                    _session!.Ui,
                    selected.Id,
                    origin,
                    size))
            {
                const string message = "This frame has no visible rendered regions.";
                var textSize = ImGui.CalcTextSize(message);
                drawList.AddText(
                    origin + Vector2.Max(Vector2.Zero, (size - textSize) * .5f),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(.55f, .58f, .64f, 1)),
                    message);
            }
        }
        ImGui.EndChild();
    }

    private static void RenderInspectorPreviewGrid(
        ImDrawListPtr drawList,
        Vector2 origin,
        Vector2 size)
    {
        var end = origin + size;
        drawList.AddRectFilled(origin, end, 0xFF111113);
        const float cellSize = 16;
        const uint minor = 0xFF202024;
        const uint major = 0xFF29292E;
        for (var x = origin.X; x <= end.X; x += cellSize)
        {
            var index = (int)MathF.Round((x - origin.X) / cellSize);
            drawList.AddLine(
                new Vector2(MathF.Round(x), origin.Y),
                new Vector2(MathF.Round(x), end.Y),
                index % 4 == 0 ? major : minor);
        }
        for (var y = origin.Y; y <= end.Y; y += cellSize)
        {
            var index = (int)MathF.Round((y - origin.Y) / cellSize);
            drawList.AddLine(
                new Vector2(origin.X, MathF.Round(y)),
                new Vector2(end.X, MathF.Round(y)),
                index % 4 == 0 ? major : minor);
        }
    }

    private static void RenderWowColoredText(string value)
    {
        var first = true;
        foreach (var span in WowTextMarkup.ParseColorSpans(value))
        {
            if (!first)
                ImGui.SameLine(0, 0);
            if (span.Argb is { } argb)
            {
                var alpha = ((argb >> 24) & 0xff) / 255f;
                var red = ((argb >> 16) & 0xff) / 255f;
                var green = ((argb >> 8) & 0xff) / 255f;
                var blue = (argb & 0xff) / 255f;
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(red, green, blue, alpha));
                ImGui.TextUnformatted(span.Text);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.TextUnformatted(span.Text);
            }
            first = false;
        }

        if (first)
            ImGui.TextUnformatted(string.Empty);
    }

    private void RenderObjectNode(
        UiObject value,
        IReadOnlySet<int>? revealPath = null)
    {
        if (revealPath?.Contains(value.Id) == true)
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);

        var flags =
            ImGuiTreeNodeFlags.OpenOnArrow |
            ImGuiTreeNodeFlags.OpenOnDoubleClick |
            ImGuiTreeNodeFlags.SpanAvailWidth;
        if (value.Children.Count == 0)
            flags |= ImGuiTreeNodeFlags.Leaf;
        if (_selectedObjectId == value.Id)
            flags |= ImGuiTreeNodeFlags.Selected;
        var open = ImGui.TreeNodeEx(
            $"{value.Name ?? "(anonymous)"} [{value.ObjectType}]##{value.Id}",
            flags);
        if (ImGui.IsItemClicked())
            _selectedObjectId = value.Id;
        if (_selectedObjectId == value.Id &&
            revealPath?.Contains(value.Id) == true)
        {
            ImGui.SetScrollHereY(.5f);
        }
        if (!open)
            return;
        foreach (var childId in value.Children)
        {
            if (_session!.Ui.Find(childId) is { } child)
                RenderObjectNode(child, revealPath);
        }
        ImGui.TreePop();
    }

    private HashSet<int> InspectorSelectionPath(int selectedId)
    {
        var result = new HashSet<int>();
        var currentId = (int?)selectedId;
        while (currentId is { } id &&
               result.Add(id) &&
               _session!.Ui.Find(id) is { } current)
        {
            currentId = current.ParentId;
        }
        return result;
    }

    private int CountSubtree(UiObject root)
    {
        var count = 0;
        var pending = new Stack<int>();
        pending.Push(root.Id);
        var visited = new HashSet<int>();
        while (pending.TryPop(out var id))
        {
            if (!visited.Add(id) || _session!.Ui.Find(id) is not { } value)
                continue;
            count++;
            foreach (var childId in value.Children)
                pending.Push(childId);
        }
        return count;
    }

    private void RenderConsoleContent()
    {
        if (SessionBusy)
        {
            RenderSessionBusyNotice();
            return;
        }
        ImGui.SeparatorText("Lua");
        ImGui.SetNextItemWidth(-1);
        var submitLua = ImGui.InputTextWithHint(
            "##lua-expression",
            "Expression or Lua statement...",
            ref _luaInput,
            2048,
            ImGuiInputTextFlags.EnterReturnsTrue);
        if (ImGui.Button("Evaluate") || submitLua)
        {
            try
            {
                _luaResult = _session!.Lua.Evaluate(_luaInput);
            }
            catch (Exception exception)
            {
                _luaResult = exception.Message;
            }
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Result");
        if (ImGui.BeginChild("lua-result", new Vector2(0, 120), ImGuiChildFlags.Border))
            ImGui.TextWrapped(string.IsNullOrEmpty(_luaResult) ? "No result yet." : _luaResult);
        ImGui.EndChild();

        ImGui.SeparatorText("Slash command");
        ImGui.SetNextItemWidth(-1);
        var submitSlash = ImGui.InputTextWithHint(
            "##slash-command",
            "/dm",
            ref _slashInput,
            1024,
            ImGuiInputTextFlags.EnterReturnsTrue);
        if (ImGui.Button("Run command") || submitSlash)
        {
            try
            {
                _luaResult = _session!.Lua.InvokeSlashCommand(_slashInput) ? "handled" : "unknown command";
            }
            catch (Exception exception)
            {
                _luaResult = exception.Message;
            }
        }
    }

    private void RenderLogContent()
    {
        var entries = _session!.Log.Snapshot(5_000);
        var categories = entries
            .Select(entry => entry.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ImGui.Button("Clear"))
        {
            _session.Log.Clear();
            _selectedLogSequence = null;
            entries = [];
        }
        ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref _logAutoScroll);

        ImGui.SameLine();
        ImGui.Checkbox("Trace", ref _logShowTrace);
        ImGui.SameLine();
        ImGui.Checkbox("Info", ref _logShowInformation);
        ImGui.SameLine();
        ImGui.Checkbox("Warnings", ref _logShowWarnings);
        ImGui.SameLine();
        ImGui.Checkbox("Errors", ref _logShowErrors);

        var categoryPreview = _logCategory.Length == 0 ? "All categories" : _logCategory;
        ImGui.SetNextItemWidth(Math.Min(190, ImGui.GetContentRegionAvail().X * .36f));
        if (ImGui.BeginCombo("##log-category", categoryPreview))
        {
            if (ImGui.Selectable("All categories", _logCategory.Length == 0))
                _logCategory = string.Empty;
            foreach (var category in categories)
            {
                if (ImGui.Selectable(category, category.Equals(
                        _logCategory,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    _logCategory = category;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint(
            "##log-search",
            "Search messages, categories, or details...",
            ref _logSearch,
            512);

        var filtered = entries.Where(LogEntryMatchesFilters).ToArray();
        var selected = _selectedLogSequence is { } selectedSequence
            ? entries.FirstOrDefault(entry => entry.Sequence == selectedSequence)
            : null;
        var availableHeight = ImGui.GetContentRegionAvail().Y;
        var detailHeight = selected is null
            ? 0
            : Math.Clamp(availableHeight * .3f, 120, 230);
        var listHeight = Math.Max(80, availableHeight - detailHeight - (selected is null ? 0 : 12));
        var newestSequence = entries.Count == 0 ? 0 : entries[^1].Sequence;
        var horizontalCellPadding = ImGui.GetStyle().CellPadding.X * 2;
        var timeColumnWidth = ImGui.CalcTextSize("00:00:00").X + horizontalCellPadding;
        var categoryColumnWidth = Math.Min(
            180,
            Math.Max(
                ImGui.CalcTextSize("Category").X,
                categories.Select(category => ImGui.CalcTextSize(category).X).DefaultIfEmpty().Max()) +
            horizontalCellPadding);

        if (ImGui.BeginChild("log-scroll", new Vector2(0, listHeight), ImGuiChildFlags.None))
        {
            if (ImGui.BeginTable(
                    "##log-table",
                    3,
                    ImGuiTableFlags.BordersInnerV |
                    ImGuiTableFlags.RowBg |
                    ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.ScrollY |
                    ImGuiTableFlags.SizingStretchProp))
            {
                var wasAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4;
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(
                    "Time",
                    ImGuiTableColumnFlags.WidthFixed,
                    timeColumnWidth);
                ImGui.TableSetupColumn(
                    "Category",
                    ImGuiTableColumnFlags.WidthFixed,
                    categoryColumnWidth);
                ImGui.TableSetupColumn(
                    "Message",
                    ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var entry in filtered)
                    RenderLogRow(entry);

                if (_logAutoScroll && wasAtBottom && newestSequence > _lastRenderedLogSequence)
                    ImGui.SetScrollHereY(1);
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();

        _lastRenderedLogSequence = Math.Max(_lastRenderedLogSequence, newestSequence);
        if (selected is not null)
            RenderLogDetails(selected, detailHeight);
    }

    private bool LogEntryMatchesFilters(EmulatorLogEntry entry)
    {
        var levelVisible = entry.Level switch
        {
            EmulatorLogLevel.Trace => _logShowTrace,
            EmulatorLogLevel.Information => _logShowInformation,
            EmulatorLogLevel.Warning => _logShowWarnings,
            EmulatorLogLevel.Error => _logShowErrors,
            _ => true
        };
        if (!levelVisible)
            return false;
        if (_logCategory.Length > 0 &&
            !entry.Category.Equals(_logCategory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (_logSearch.Length == 0)
            return true;
        return entry.Category.Contains(_logSearch, StringComparison.OrdinalIgnoreCase) ||
               entry.Message.Contains(_logSearch, StringComparison.OrdinalIgnoreCase) ||
               entry.Details?.Contains(_logSearch, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void RenderLogRow(EmulatorLogEntry entry)
    {
        var selected = _selectedLogSequence == entry.Sequence;
        var color = LogColor(entry.Level);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        if (ImGui.Selectable(
                $"{entry.Timestamp:HH:mm:ss}##log-{entry.Sequence}",
                selected,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap))
        {
            _selectedLogSequence = entry.Sequence;
        }

        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(entry.Category);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextUnformatted(SingleLine(entry.Message));
        ImGui.PopStyleColor();
    }

    private static string SingleLine(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');

    private void RenderLogDetails(EmulatorLogEntry entry, float height)
    {
        ImGui.SeparatorText("Details");
        if (!ImGui.BeginChild(
                "log-details",
                new Vector2(0, Math.Max(80, height - ImGui.GetTextLineHeightWithSpacing())),
                ImGuiChildFlags.Border))
        {
            ImGui.EndChild();
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, LogColor(entry.Level));
        ImGui.TextUnformatted(
            $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}  {entry.Level}  " +
            $"{entry.Category}  #{entry.Sequence}");
        ImGui.PopStyleColor();
        ImGui.SameLine();
        if (ImGui.SmallButton("Copy"))
        {
            ImGui.SetClipboardText(
                $"[{entry.Timestamp:O}] {entry.Level} {entry.Category}: {entry.Message}" +
                (entry.Details is null ? string.Empty : $"{Environment.NewLine}{entry.Details}"));
        }
        ImGui.Separator();
        ImGui.TextWrapped(entry.Message);
        if (!string.IsNullOrWhiteSpace(entry.Details) && entry.Details != entry.Message)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Technical details");
            ImGui.TextWrapped(entry.Details);
        }
        ImGui.EndChild();
    }

    private static Vector4 LogColor(EmulatorLogLevel level) => level switch
    {
        EmulatorLogLevel.Error => new Vector4(1, 0.3f, 0.25f, 1),
        EmulatorLogLevel.Warning => new Vector4(1, 0.75f, 0.25f, 1),
        EmulatorLogLevel.Trace => new Vector4(0.55f, 0.58f, 0.65f, 1),
        _ => new Vector4(0.85f, 0.87f, 0.92f, 1)
    };

    private void RenderInspectorSelectionOverlay(ImDrawListPtr drawList)
    {
        if (!_inspectorSelectionMode)
            return;

        var accent = ImGui.ColorConvertFloat4ToU32(new Vector4(.25f, .76f, 1, 1));
        var accentFill = ImGui.ColorConvertFloat4ToU32(new Vector4(.25f, .76f, 1, .12f));
        var labelBackground = ImGui.ColorConvertFloat4ToU32(new Vector4(.035f, .04f, .05f, .94f));
        const string hint = "Inspector · click a visible frame";
        var hintSize = ImGui.CalcTextSize(hint);
        var hintMinimum = _canvasOrigin + new Vector2(8);
        drawList.AddRectFilled(
            hintMinimum,
            hintMinimum + hintSize + new Vector2(12, 8),
            labelBackground,
            3);
        drawList.AddText(
            hintMinimum + new Vector2(6, 4),
            accent,
            hint);

        var targetId = _inspectorHoveredObjectId ?? _selectedObjectId;
        if (targetId is not { } id ||
            _session!.Ui.Find(id) is not { } target ||
            !_session.Ui.IsVisible(target) ||
            !_session.Ui.HasResolvedRect(target))
        {
            return;
        }

        var bounds = _session.Ui.ResolveBounds(target.Id);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;
        var minimum = CanvasPoint(bounds.Left, bounds.Top);
        var maximum = CanvasPoint(bounds.Right, bounds.Bottom);
        drawList.AddRectFilled(minimum, maximum, accentFill);
        drawList.AddRect(minimum, maximum, accent, 0, ImDrawFlags.None, 2);

        var label = $"{target.Name ?? "(anonymous)"} [{target.ObjectType} #{target.Id}]";
        var labelSize = ImGui.CalcTextSize(label);
        var labelMinimum = new Vector2(
            Math.Clamp(
                minimum.X,
                _canvasOrigin.X,
                Math.Max(_canvasOrigin.X, _canvasOrigin.X + _canvasSize.X - labelSize.X - 10)),
            minimum.Y - labelSize.Y - 8);
        if (labelMinimum.Y < _canvasOrigin.Y)
            labelMinimum.Y = minimum.Y + 4;
        drawList.AddRectFilled(
            labelMinimum,
            labelMinimum + labelSize + new Vector2(10, 6),
            labelBackground,
            2);
        drawList.AddText(
            labelMinimum + new Vector2(5, 3),
            accent,
            label);
    }

    private Vector2 CanvasPoint(float logicalX, float logicalY) =>
        new(
            _canvasOrigin.X + logicalX * _canvasScale,
            _canvasOrigin.Y + (_session!.Ui.LogicalHeight - logicalY) * _canvasScale);

    private void RouteCanvasInput(bool viewportWindowHovered)
    {
        var mouse = ImGui.GetMousePos();
        var withinCanvas =
            mouse.X >= _canvasOrigin.X &&
            mouse.Y >= _canvasOrigin.Y &&
            mouse.X < _canvasOrigin.X + _canvasSize.X &&
            mouse.Y < _canvasOrigin.Y + _canvasSize.Y;
        _viewportHovered = viewportWindowHovered && withinCanvas;

        if (_viewportHovered)
        {
            var logicalX = (mouse.X - _canvasOrigin.X) / _canvasScale;
            var logicalY = _session!.Ui.LogicalHeight - (mouse.Y - _canvasOrigin.Y) / _canvasScale;
            if (_inspectorSelectionMode)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                var selected = _session.Ui.HitTestVisibleFrame(new Vector2(logicalX, logicalY));
                _inspectorHoveredObjectId = selected?.Id;
                if (selected is not null &&
                    ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _selectedObjectId = selected.Id;
                    _inspectorRevealSelection = true;
                    _inspectorFilter = string.Empty;
                    _viewportFocused = false;
                }
            }
            else
            {
                _inspectorHoveredObjectId = null;
                _session.MouseMove(logicalX, logicalY);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _session.MouseButton("LeftButton", true);
                    _leftMouseCaptured = true;
                    _viewportFocused = true;
                }
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    _session.MouseButton("RightButton", true);
                    _rightMouseCaptured = true;
                    _viewportFocused = true;
                }

                var wheel = ImGui.GetIO().MouseWheel;
                if (MathF.Abs(wheel) > 0.0001f)
                    _session.MouseWheel(wheel);
            }
        }
        else if (_inspectorSelectionMode)
        {
            _inspectorHoveredObjectId = null;
        }

        if (_leftMouseCaptured && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            _session!.MouseButton("LeftButton", false);
            _leftMouseCaptured = false;
        }
        if (_rightMouseCaptured && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
        {
            _session!.MouseButton("RightButton", false);
            _rightMouseCaptured = false;
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scanCode)
    {
        if (key == Key.F1)
            _toolsVisible = !_toolsVisible;
        if (key == Key.F6)
            _paused = !_paused;
        if (key == Key.F7 && _paused)
            _step = true;
        if (key == Key.R && IsControlDown(keyboard))
        {
            Reload();
            return;
        }
        if (_loadingStatus is null &&
            _viewportFocused &&
            !_inspectorSelectionMode &&
            ToWowKey(key) is { } wowKey)
            _session!.Key(wowKey, true, IsControlDown(keyboard), IsShiftDown(keyboard), IsAltDown(keyboard));
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scanCode)
    {
        if (_loadingStatus is null &&
            _viewportFocused &&
            !_inspectorSelectionMode &&
            ToWowKey(key) is { } wowKey)
            _session!.Key(wowKey, false, IsControlDown(keyboard), IsShiftDown(keyboard), IsAltDown(keyboard));
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        if (_loadingStatus is null &&
            _viewportFocused &&
            !_inspectorSelectionMode &&
            !char.IsControl(character))
            _session!.TextInput(character.ToString());
    }

    private void Reload()
    {
        _reloadRequested = true;
        _reloadOverlayFrames = 0;
        ReportLoading(-1, "Reloading addons and Blizzard UI...");
    }

    private void ServicePendingReload()
    {
        if (_reloadTask is { } running)
        {
            if (!running.IsCompleted)
                return;
            _reloadTask = null;
            try
            {
                running.GetAwaiter().GetResult();
                if (_reloadPlan is { } completed)
                    ApplyLoadedSession(completed);
            }
            catch (Exception exception)
            {
                _session!.Log.Error("app", exception.ToString());
                Console.Error.WriteLine(exception);
                if (_renderer is null)
                    RecreateTextureRenderer([]);
            }
            _reloadPlan = null;
            _loadingProgress = 1;
            _loadingStatus = null;
            return;
        }

        if (!_reloadRequested)
            return;
        if (_reloadOverlayFrames++ < 1)
            return;
        _reloadRequested = false;

        ReloadPlan? plan;
        try
        {
            plan = PrepareReload();
        }
        catch (Exception exception)
        {
            _session!.Log.Error("app", exception.ToString());
            _loadingStatus = null;
            return;
        }
        if (plan is null)
        {
            return;
        }

        _reloadPlan = plan;
        ConfigureFontAssetReader(plan);
        var session = _session!;
        _reloadTask = Task.Run(() => session.Load(
            plan.Paths,
            plan.SavedVariablesDirectory,
            plan.BootstrapPaths,
            plan.RuntimeFiles,
            plan.AvailablePaths));
    }

    private void OnAddonLoadProgress(AddonLoadProgress progress)
    {
        var total = Math.Max(1, progress.Total);
        _loadingProgress = Math.Clamp(progress.Completed / (float)total, 0, 1);
        _loadingStatus = $"{progress.AddonName}   {progress.Completed} of {progress.Total}";
    }

    private sealed record ReloadPlan(
        IReadOnlyList<string> Paths,
        IReadOnlyList<string> BootstrapPaths,
        IReadOnlyList<string> AvailablePaths,
        IReadOnlyList<string> RuntimeFiles,
        string SavedVariablesDirectory);

    private ReloadPlan? PrepareReload()
    {
        _selectedObjectId = null;
        _inspectorHoveredObjectId = null;
        _inspectorRevealSelection = false;
        _session!.ClientCVarConfigPath = null;
        _session.ClientCVarOverrides =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_installations.Count > 0)
        {
            _session.ClientCVarConfigPath = Path.Combine(
                _installations[_selectedInstallation].ProductPath,
                "WTF",
                "Config.wtf");
            var productSettings = CurrentProductSettings();
            var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (productSettings.UseUiScale is { } useUiScale)
                overrides["useUiScale"] = useUiScale ? "1" : "0";
            if (productSettings.UiScale is { } uiScale)
                overrides["uiScale"] = uiScale.ToString("R", CultureInfo.InvariantCulture);
            if (productSettings.UiScaleMultiplier is { } multiplier)
            {
                overrides["uiScaleMultiplier"] =
                    multiplier.ToString("R", CultureInfo.InvariantCulture);
            }
            _session.ClientCVarOverrides = overrides;
        }

        if (_installations.Count > 0 &&
            ShouldLoadBlizzardUi() &&
            _options.AutoConnectTact &&
            _blizzardUi is null)
        {
            _reloadAfterTactInitialization = true;
            BeginTactInitialization();
            return null;
        }

        IReadOnlyList<string> paths;
        using (StartupTimeline.Begin("resolve enabled addon paths"))
            paths = ResolveEnabledAddonPaths();
        var bootstrapPaths = ShouldLoadBlizzardUi()
            ? LimitBlizzardUiModules(_blizzardUi?.AddonPaths ?? [])
            : [];
        var availablePaths = ShouldLoadBlizzardUi()
            ? _options.BlizzardUiModuleLimit is null
                ? _blizzardUi?.AvailableAddonPaths ?? []
                : bootstrapPaths
            : [];
        return new ReloadPlan(
            paths,
            bootstrapPaths,
            availablePaths,
            ShouldLoadBlizzardUi() ? _blizzardUi?.RuntimeFiles ?? [] : [],
            CurrentSavedVariablesDirectory());
    }

    private void ApplyLoadedSession(ReloadPlan plan)
    {
        var paths = plan.Paths;
        StartupTimeline.Fact("Addons loaded", _session!.Manifests.Count.ToString());
        StartupTimeline.Fact("Blizzard modules loaded", _session.BootstrapManifests.Count.ToString());
        _loadedAddons = _addonCatalog
            .Where(value => paths.Contains(value.RootPath, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        var assetPaths = AssetPaths(plan);
        _renderer?.Dispose();
        _renderer = null;
        _textures?.Dispose();
        using (StartupTimeline.Begin("create texture cache"))
        {
            _textures = new TextureCache(
                _gl!,
                assetPaths,
                _session.Log,
                WoWAddonLabPaths.ImageCacheDirectory)
            { Tact = _tact };
        }
        using (StartupTimeline.Begin("create addon font cache"))
        {
            if (_addonFonts is null)
                _addonFonts = CreateAddonFontCache(_textures);
            else
                _addonFonts.UpdateAssets(_textures);
        }
        using (StartupTimeline.Begin("create UI renderer"))
            _renderer = new WowUiRenderer(_textures, _addonFonts);
    }

    private void ConfigureFontAssetReader(ReloadPlan plan)
    {
        var localAssets = new LocalAddonAssetSource(AssetPaths(plan));
        var tact = _tact;
        _session!.FontAssetReader = asset =>
            localAssets.Read(asset, defaultToBlp: true) ?? tact?.Read(asset, null);
    }

    private IReadOnlyList<string> AssetPaths(ReloadPlan plan) =>
        plan.AvailablePaths
            .Concat(plan.Paths)
            .Concat(_addonCatalog.Select(addon => addon.RootPath))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private int SelectInitialInstallation()
    {
        if (_installations.Count == 0)
            return 0;
        var requested = _installations.ToList().FindIndex(value =>
            _options.Product is not null &&
            (value.Product.ProductCode.Equals(_options.Product, StringComparison.OrdinalIgnoreCase) ||
             value.Product.FolderName.Equals(_options.Product, StringComparison.OrdinalIgnoreCase) ||
             value.ProductPath.Equals(_options.Product, StringComparison.OrdinalIgnoreCase)));
        if (requested >= 0)
            return requested;
        var remembered = _installations.ToList().FindIndex(value =>
            value.ProductPath.Equals(_settings.SelectedProductPath, StringComparison.OrdinalIgnoreCase));
        if (remembered >= 0)
            return remembered;
        var retail = _installations.ToList().FindIndex(value =>
            value.Product.ProductCode.Equals("wow", StringComparison.OrdinalIgnoreCase));
        return Math.Max(0, retail);
    }

    private void SelectProduct(int index)
    {
        _selectedInstallation = Math.Clamp(index, 0, _installations.Count - 1);
        _settings.SelectedProductPath = _installations[_selectedInstallation].ProductPath;
        _tactGeneration++;
        _tact = null;
        _blizzardUi = null;
        _tactError = null;
        PersistSettings();
        RefreshProduct(loadAddons: false);
        if (ShouldLoadBlizzardUi() && _options.AutoConnectTact)
        {
            _reloadAfterTactInitialization = true;
            BeginTactInitialization();
        }
        else
        {
            _reloadAfterTactInitialization = false;
            Reload();
            if (_options.AutoConnectTact)
                BeginTactInitialization();
        }
    }

    private void RefreshProduct(bool loadAddons)
    {
        if (_installations.Count == 0)
        {
            _addonCatalog = [];
            _loadedAddons = [];
            _savedVariableProfiles =
                [new SavedVariablesProfile("clean", "Emulator-only product data", null, null, null, null, null)];
            _selectedProfile = 0;
            var legacyPaths = _options.AddonPath is null ? Array.Empty<string>() : [_options.AddonPath];
            _session!.Load(legacyPaths, Path.Combine(_settingsStore.DirectoryPath, "SavedVariables", "legacy"));
            RecreateTextureRenderer(legacyPaths);
            return;
        }

        var installation = _installations[_selectedInstallation];
        var manifestContext = AddonManifestContextFactory.For(installation);
        _session!.ManifestContext = manifestContext;
        _settings.SelectedProductPath = installation.ProductPath;
        using (var span = StartupTimeline.Begin("discover installed addons"))
        {
            _addonCatalog = InstalledAddonCatalog.Discover(installation.ProductPath, manifestContext);
            span.Annotate($"{_addonCatalog.Count} found");
        }
        using (StartupTimeline.Begin("discover SavedVariables profiles"))
            _savedVariableProfiles = SavedVariablesProfiles.Discover(installation);
        var productSettings = CurrentProductSettings();
        var requestedProfile = _options.Profile ?? productSettings.ProfileId;
        _selectedProfile = Math.Max(
            0,
            _savedVariableProfiles.ToList().FindIndex(value =>
                value.Id.Equals(requestedProfile, StringComparison.OrdinalIgnoreCase)));
        productSettings.ProfileId = _savedVariableProfiles[_selectedProfile].Id;
        PersistSettings();
        if (loadAddons)
            Reload();
    }

    private ProductEmulationSettings CurrentProductSettings()
    {
        if (_installations.Count == 0)
            return new ProductEmulationSettings();
        var key = Path.GetFullPath(_installations[_selectedInstallation].ProductPath);
        if (!_settings.Products.TryGetValue(key, out var settings))
        {
            settings = new ProductEmulationSettings();
            _settings.Products[key] = settings;
        }
        return settings;
    }

    private bool ShouldLoadBlizzardUi() =>
        _options.LoadBlizzardUi ?? CurrentProductSettings().LoadBlizzardUi;

    private IReadOnlyList<string> LimitBlizzardUiModules(IReadOnlyList<string> paths) =>
        _options.BlizzardUiModuleLimit is { } limit
            ? paths.Take(limit).ToArray()
            : paths;

    private IReadOnlyList<string> ResolveEnabledAddonPaths()
    {
        if (_installations.Count == 0)
            return _options.AddonPath is null ? [] : [_options.AddonPath];
        IReadOnlySet<string> enabled = _options.EnabledAddons.Count > 0
            ? _options.EnabledAddons.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : CurrentProductSettings().EnabledAddons;
        var ordered = InstalledAddonCatalog.ResolveLoadOrder(
            _addonCatalog,
            enabled,
            warning => _session!.Log.Warn("addons", warning),
            WowBuildInfo.FromVersion(
                _installations[_selectedInstallation].Version)
                .InterfaceVersion);
        return ordered.Select(value => value.RootPath).ToArray();
    }

    private string CurrentSavedVariablesDirectory()
    {
        if (_installations.Count == 0)
            return Path.Combine(_settingsStore.DirectoryPath, "SavedVariables", "legacy");
        return SavedVariablesProfiles.EmulatorDirectory(
            _settingsStore.DirectoryPath,
            _installations[_selectedInstallation],
            _savedVariableProfiles[_selectedProfile]);
    }

    private void ImportSavedVariablesProfile()
    {
        try
        {
            var profile = _savedVariableProfiles[_selectedProfile];
            var selected = InstalledAddonCatalog.ResolveLoadOrder(
                _addonCatalog,
                CurrentProductSettings().EnabledAddons,
                minimumInterfaceVersion: WowBuildInfo.FromVersion(
                    _installations[_selectedInstallation].Version)
                    .InterfaceVersion);
            var imported = SavedVariablesProfiles.ImportCopies(
                profile,
                selected,
                CurrentSavedVariablesDirectory());
            _profileMessage = imported == 0
                ? "No matching SavedVariables files were found for the selected addons."
                : $"Imported {imported} addon data file{(imported == 1 ? string.Empty : "s")} into the emulator profile.";
            Reload();
        }
        catch (Exception exception)
        {
            _profileMessage = $"Import failed: {exception.Message}";
            _session!.Log.Error("saved-variables", exception.ToString());
        }
    }

    private void SelectSavedVariablesProfile(int index)
    {
        if (index < 0 || index >= _savedVariableProfiles.Count || index == _selectedProfile)
            return;
        _selectedProfile = index;
        CurrentProductSettings().ProfileId = _savedVariableProfiles[index].Id;
        _profileMessage = string.Empty;
        PersistSettings();
        Reload();
    }

    private void PersistSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception exception)
        {
            _session?.Log.Warn("settings", $"Could not save settings: {exception.Message}");
        }
    }

    private void RecreateTextureRenderer(IEnumerable<string> addonPaths)
    {
        _renderer?.Dispose();
        _renderer = null;
        _textures?.Dispose();
        _textures = new TextureCache(
            _gl!,
            addonPaths,
            _session!.Log,
            WoWAddonLabPaths.ImageCacheDirectory)
        { Tact = _tact };
        if (_addonFonts is null)
            _addonFonts = CreateAddonFontCache(_textures);
        else
            _addonFonts.UpdateAssets(_textures);
        _renderer = new WowUiRenderer(_textures, _addonFonts);
    }

    private AddonFontCache CreateAddonFontCache(TextureCache textures) =>
        new(
            _gl!,
            textures,
            _fonts,
            _session!.Log,
            WoWAddonLabPaths.FontCacheDirectory);

    private void BeginTactInitialization()
    {
        if (_installations.Count == 0 || _tactInitialization is { IsCompleted: false })
            return;
        _tactError = null;
        ReportLoading(0.02f, "Preparing game data...");
        var generation = ++_tactGeneration;
        _tactInitializationGeneration = generation;
        var installation = _installations[_selectedInstallation];
        _session!.BuildInfo = WowBuildInfo.FromVersion(installation.Version);
        var prepareBlizzardUi = ShouldLoadBlizzardUi();
        var forceDefinitionUpdate = _forceDefinitionUpdate;
        _forceDefinitionUpdate = false;
        var source = new TactAssetSource(installation);
        _tactInitialization = Task.Run(() =>
        {
            using var tactSpan = StartupTimeline.BeginBackground("game data initialization (worker thread)");
            ReportLoading(0.05f, "Opening World of Warcraft data...");
            using (StartupTimeline.Begin("open WoW data through TACTSharp"))
                source.Initialize(WoWAddonLabPaths.TactCacheDirectory);
            source.DefinitionsDirectory = WoWAddonLabPaths.DefinitionsCacheDirectory;
            string? definitionWarning = null;
            TactAtlasCatalog? atlasCatalog = null;
            TactDyeColorCatalog? dyeColorCatalog = null;
            TactGlobalColorCatalog? globalColorCatalog = null;
            TactGameRuleCatalog? gameRuleCatalog = null;
            TactMapCatalog? mapCatalog = null;
            TactQuestCatalog? questCatalog = null;
            TactEncounterJournalCatalog? encounterJournalCatalog = null;
            TactGlobalStringCatalog? globalStringCatalog = null;
            TactSpellCatalog? spellCatalog = null;
            TactItemClassCatalog? itemClassCatalog = null;
            TactPaperDollItemFrameCatalog? inventorySlotCatalog = null;
            TactRaceCatalog? raceCatalog = null;
            TactFactionCatalog? factionCatalog = null;
            TactTransmogSetCatalog? transmogSetCatalog = null;
            TactCharacterServiceCatalog? characterServiceCatalog = null;
            TactAchievementCatalog? achievementCatalog = null;
            TactAccountStoreCatalog? accountStoreCatalog = null;
            TactAzeriteEssenceCatalog? azeriteEssenceCatalog = null;
            TactModelInfoCatalog? modelInfoCatalog = null;
            TactMacroIconCatalog? macroIconCatalog = null;
            string? atlasError = null;
            try
            {
                ReportLoading(0.08f, "Updating DB2 definitions...");
                using (var definitionSpan = StartupTimeline.Begin("ensure DB2 definitions"))
                {
                    var definitions = Db2DefinitionCache.EnsureCurrent(
                        WoWAddonLabPaths.DefinitionsCacheDirectory,
                        installation.Product.ProductCode,
                        installation.Version,
                        forceDefinitionUpdate);
                    definitionWarning = definitions.Warning;
                    var summary = definitions.Updated
                        ? $"downloaded {definitions.DefinitionCount}"
                        : $"{definitions.DefinitionCount} cached";
                    definitionSpan.Annotate(summary);
                    StartupTimeline.Fact("DB2 definitions", summary);
                }

                using var catalogSpan = StartupTimeline.Begin("load client data catalogs");
                using var databaseCache = source.Database.BeginCacheScope();
                ReportLoading(0.12f, "Loading texture atlases...");
                using (StartupTimeline.Begin("catalog: texture atlases"))
                    atlasCatalog = TactAtlasCatalog.Load(
                        source,
                        installation.Version ??
                        throw new InvalidDataException("The selected WoW product has no build version."));
                ReportLoading(0.22f, "Loading interface colors...");
                using (StartupTimeline.Begin("catalog: global colors"))
                    globalColorCatalog = TactGlobalColorCatalog.Load(
                        source,
                        installation.Version);
                using (StartupTimeline.Begin("catalog: dye colors"))
                    dyeColorCatalog = TactDyeColorCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.28f, "Loading game rules...");
                using (StartupTimeline.Begin("catalog: game rules"))
                    gameRuleCatalog = TactGameRuleCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.34f, "Loading maps...");
                using (StartupTimeline.Begin("catalog: maps and map art"))
                    mapCatalog = TactMapCatalog.Load(
                        source,
                        installation.Version);
                using (StartupTimeline.Begin("catalog: task quests"))
                    questCatalog = TactQuestCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.43f, "Loading encounter data...");
                using (StartupTimeline.Begin("catalog: encounter journal"))
                    encounterJournalCatalog = TactEncounterJournalCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.51f, "Loading localized strings...");
                using (StartupTimeline.Begin("catalog: global strings"))
                    globalStringCatalog = TactGlobalStringCatalog.Load(
                        source,
                        installation.Version);
                using (StartupTimeline.Begin("catalog: spells"))
                    spellCatalog = TactSpellCatalog.Load(
                        source,
                        installation.Version,
                        WoWAddonLabPaths.DataCacheDirectory);
                using (StartupTimeline.Begin("catalog: item classes"))
                    itemClassCatalog = TactItemClassCatalog.Load(
                        source,
                        installation.Version,
                        WoWAddonLabPaths.DataCacheDirectory);
                using (StartupTimeline.Begin("catalog: inventory slots"))
                    inventorySlotCatalog =
                        TactPaperDollItemFrameCatalog.Load(
                            source,
                            installation.Version);
                using (StartupTimeline.Begin("catalog: races"))
                    raceCatalog = TactRaceCatalog.Load(
                        source,
                        installation.Version);
                using (StartupTimeline.Begin("catalog: factions"))
                    factionCatalog = TactFactionCatalog.Load(
                        source,
                        installation.Version);
                using (StartupTimeline.Begin("catalog: transmog sets"))
                    transmogSetCatalog = TactTransmogSetCatalog.Load(
                        source,
                        installation.Version,
                        WoWAddonLabPaths.DataCacheDirectory);
                ReportLoading(0.58f, "Loading character services...");
                using (StartupTimeline.Begin("catalog: character services"))
                    characterServiceCatalog = TactCharacterServiceCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.64f, "Loading achievements...");
                using (StartupTimeline.Begin("catalog: achievements"))
                    achievementCatalog = TactAchievementCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.70f, "Loading account collections...");
                using (StartupTimeline.Begin("catalog: account store"))
                    accountStoreCatalog = TactAccountStoreCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.76f, "Loading Azerite data...");
                using (StartupTimeline.Begin("catalog: Azerite essences"))
                    azeriteEssenceCatalog = TactAzeriteEssenceCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.82f, "Loading model scenes...");
                using (StartupTimeline.Begin("catalog: model scenes"))
                    modelInfoCatalog = TactModelInfoCatalog.Load(
                        source,
                        installation.Version);
                ReportLoading(0.88f, "Loading interface icons...");
                using (StartupTimeline.Begin("catalog: macro icons"))
                    macroIconCatalog = TactMacroIconCatalog.Load(
                        source,
                        installation.Version,
                        WoWAddonLabPaths.DataCacheDirectory);
            }
            catch (Exception exception)
            {
                atlasError = exception.Message;
            }
            BlizzardUiBootstrapResult? blizzardUi = null;
            if (prepareBlizzardUi)
            {
                ReportLoading(0.93f, "Preparing Blizzard interface...");
                using var span = StartupTimeline.Begin("prepare Blizzard UI sources");
                blizzardUi = BlizzardUiBootstrap.Prepare(
                    source,
                    installation,
                    WoWAddonLabPaths.BlizzardUiCacheDirectory);
                span.Annotate(blizzardUi.CacheHit
                    ? $"cache hit, {blizzardUi.ExtractedFiles} files"
                    : $"extracted {blizzardUi.ExtractedFiles} files");
                StartupTimeline.Fact("Blizzard UI cache", blizzardUi.CacheHit ? "hit" : "miss (extracted)");
                StartupTimeline.Fact("Blizzard UI files", blizzardUi.ExtractedFiles.ToString());
                StartupTimeline.Fact("Blizzard UI modules", blizzardUi.ModuleNames.Count.ToString());
            }
            if (generation == _tactGeneration)
            {
                ReportLoading(0.99f, "Starting WoW Addon Lab...");
                _tact = source;
                _atlasCatalog = atlasCatalog;
                _dyeColorCatalog = dyeColorCatalog;
                _globalColorCatalog = globalColorCatalog;
                _gameRuleCatalog = gameRuleCatalog;
                _mapCatalog = mapCatalog;
                _encounterJournalCatalog = encounterJournalCatalog;
                _globalStringCatalog = globalStringCatalog;
                _spellCatalog = spellCatalog;
                _itemClassCatalog = itemClassCatalog;
                _inventorySlotCatalog = inventorySlotCatalog;
                _raceCatalog = raceCatalog;
                _factionCatalog = factionCatalog;
                _transmogSetCatalog = transmogSetCatalog;
                _questCatalog = questCatalog;
                _characterServiceCatalog = characterServiceCatalog;
                _achievementCatalog = achievementCatalog;
                _accountStoreCatalog = accountStoreCatalog;
                _azeriteEssenceCatalog = azeriteEssenceCatalog;
                _modelInfoCatalog = modelInfoCatalog;
                _macroIconCatalog = macroIconCatalog;
                _atlasError = atlasError;
                _definitionWarning = definitionWarning;
                _blizzardUi = blizzardUi;
            }
        });
    }

    private void UpdateDefinitionsAndReload()
    {
        if (_installations.Count == 0)
            return;
        _forceDefinitionUpdate = true;
        _tactGeneration++;
        _tact = null;
        _blizzardUi = null;
        _tactError = null;
        _reloadAfterTactInitialization = true;
        BeginTactInitialization();
    }

    private void ReportLoading(float progress, string status)
    {
        _loadingProgress = progress;
        _loadingStatus = status;
    }

    private void CompleteTactInitialization()
    {
        if (_tactInitialization is not { IsCompleted: true } task)
            return;
        var current = _tactInitializationGeneration == _tactGeneration;
        if (current && task.IsFaulted)
        {
            _tactError = task.Exception?.GetBaseException().Message;
            _blizzardUi = new BlizzardUiBootstrapResult(
                [],
                [],
                [],
                [],
                0,
                _tactError is null ? [] : [_tactError],
                false);
            if (_reloadAfterTactInitialization)
            {
                _reloadAfterTactInitialization = false;
                Reload();
            }
        }
        else if (current && _tact is not null && _textures is not null)
        {
            using var applySpan = StartupTimeline.Begin("apply game data to the session");
            _session!.ModelResourceProvider = _tact;
            _session!.AtlasProvider = _atlasCatalog;
            _session.DyeColorProvider = _dyeColorCatalog;
            _session.GlobalColorProvider = _globalColorCatalog;
            _session.GameRuleProvider = _gameRuleCatalog;
            _session.MapProvider = _mapCatalog;
            _session.QuestProvider = _questCatalog;
            _session.EncounterJournalProvider = _encounterJournalCatalog;
            _session.GlobalStringProvider = _globalStringCatalog;
            _session.SpellProvider = _spellCatalog;
            _session.ItemClassProvider = _itemClassCatalog;
            _session.ItemProvider = _itemClassCatalog;
            _session.InventorySlotProvider = _inventorySlotCatalog;
            _session.RaceProvider = _raceCatalog;
            _session.FactionProvider = _factionCatalog;
            _session.TransmogSetProvider = _transmogSetCatalog;
            _session.TransmogAppearanceProvider = _transmogSetCatalog;
            _session.CharacterServiceProvider = _characterServiceCatalog;
            _session.AchievementProvider = _achievementCatalog;
            _session.AccountStoreProvider = _accountStoreCatalog;
            _session.AzeriteEssenceProvider = _azeriteEssenceCatalog;
            _session.ModelInfoProvider = _modelInfoCatalog;
            _session.MacroIconProvider = _macroIconCatalog;
            if (_atlasCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_atlasCatalog.Count} client texture atlas entries.");
            }
            if (_globalColorCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_globalColorCatalog.Count} client global color entries.");
            }
            if (_dyeColorCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_dyeColorCatalog.Count} client dye color entries.");
            }
            if (_gameRuleCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_gameRuleCatalog.Count} client game rule entries.");
            }
            if (_encounterJournalCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_encounterJournalCatalog.Tiers.Count} Encounter Journal tiers, " +
                    $"{_encounterJournalCatalog.InstanceCount} instances " +
                    $"({_encounterJournalCatalog.DungeonCount} dungeons, " +
                    $"{_encounterJournalCatalog.RaidCount} raids), and " +
                    $"{_encounterJournalCatalog.TierRelationCount} tier relationships.");
            }
            if (_azeriteEssenceCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_azeriteEssenceCatalog.Milestones.Count} Azerite essence milestones.");
            }
            if (_modelInfoCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_modelInfoCatalog.SceneCount} model scenes, " +
                    $"{_modelInfoCatalog.ActorCount} actors, " +
                    $"{_modelInfoCatalog.ActorDisplayCount} actor displays, and " +
                    $"{_modelInfoCatalog.CameraCount} cameras.");
            }
            if (_macroIconCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_macroIconCatalog.Count} client macro icon entries.");
            }
            if (_mapCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_mapCatalog.MapCount} client map entries and " +
                    $"{_mapCatalog.ArtCount} default map-art associations.");
            }
            if (_globalStringCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_globalStringCatalog.Count} client GlobalStrings entries.");
            }
            if (_spellCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_spellCatalog.Count} client spell definitions.");
            }
            if (_itemClassCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_itemClassCatalog.Classes.Count} client item classes and " +
                    $"{_itemClassCatalog.SubClasses.Count} subclasses.");
            }
            if (_raceCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_raceCatalog.Races.Count} client races.");
            }
            if (_characterServiceCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_characterServiceCatalog.Count} client character service definitions.");
            }
            if (_transmogSetCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_transmogSetCatalog.Sets.Count} client transmog sets.");
            }
            if (_achievementCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_achievementCatalog.Achievements.Count} achievements and " +
                    $"{_achievementCatalog.Categories.Count} achievement categories.");
            }
            if (_accountStoreCatalog is not null)
            {
                _session.Log.Info(
                    "assets",
                    $"Loaded {_accountStoreCatalog.Categories.Count} account store categories and " +
                    $"{_accountStoreCatalog.Items.Count} account store items.");
            }
            if (_atlasError is not null)
            {
                _session.Log.Warn("assets", $"Atlas metadata was unavailable: {_atlasError}");
            }
            if (_definitionWarning is not null)
            {
                _session.Log.Warn("definitions", _definitionWarning);
                _definitionWarning = null;
            }
            if (_blizzardUi is not null)
            {
                _session!.Log.Info(
                    "blizzard-ui",
                    $"{(_blizzardUi.CacheHit ? "Reused" : "Prepared")} {_blizzardUi.ModuleNames.Count} " +
                    $"startup modules and {_blizzardUi.ExtractedFiles} cached files.");
                foreach (var warning in _blizzardUi.Warnings)
                    _session.Log.Warn("blizzard-ui", warning);
            }
            _textures.Tact = _tact;
            using (StartupTimeline.Begin("retry textures and fonts that were missing before TACT"))
            {
                _textures.RetryMissing();
                _addonFonts?.RetryMissing();
            }
            if (_reloadAfterTactInitialization)
            {
                _reloadAfterTactInitialization = false;
                Reload();
            }
        }
        _tactInitialization = null;
        if (!_reloadRequested)
        {
            _loadingProgress = 1;
            _loadingStatus = null;
        }
        if (!current && _options.AutoConnectTact)
            BeginTactInitialization();
    }

    private void DisposeGraphics()
    {
        _renderer?.Dispose();
        _renderer = null;
        _addonFonts?.Dispose();
        _addonFonts = null;
        _textures?.Dispose();
        _textures = null;
        _imgui?.Dispose();
        _imgui = null;
        _input?.Dispose();
        _input = null;
        _gl?.Dispose();
        _gl = null;
    }

    private static string? ToWowKey(Key key)
    {
        var name = key.ToString();
        if (name.Length == 1 && char.IsLetter(name[0]))
            return name.ToUpperInvariant();
        if (name.StartsWith("Number", StringComparison.Ordinal) && name.Length == 7)
            return name[^1].ToString();
        return key switch
        {
            Key.Space => "SPACE",
            Key.Enter => "ENTER",
            Key.Escape => "ESCAPE",
            Key.Tab => "TAB",
            Key.Backspace => "BACKSPACE",
            Key.Delete => "DELETE",
            Key.Up => "UP",
            Key.Down => "DOWN",
            Key.Left => "LEFT",
            Key.Right => "RIGHT",
            Key.Home => "HOME",
            Key.End => "END",
            Key.Insert => "INSERT",
            Key.ShiftLeft or Key.ShiftRight => "LSHIFT",
            Key.ControlLeft or Key.ControlRight => "LCTRL",
            Key.AltLeft or Key.AltRight => "LALT",
            _ => null
        };
    }

    private static bool IsControlDown(IKeyboard keyboard) =>
        keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);

    private static bool IsShiftDown(IKeyboard keyboard) =>
        keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight);

    private static bool IsAltDown(IKeyboard keyboard) =>
        keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight);

    private unsafe WowImGuiController CreateImGuiController(
        GL gl,
        IWindow window,
        IInputContext input)
    {
        var fontPath = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf")
            }
            .FirstOrDefault(File.Exists);
        if (fontPath is null)
            return new WowImGuiController(gl, window, input);

        var configuredFonts = new List<RenderFont>();
        var controller = new WowImGuiController(gl, window, input, () =>
        {
            var io = ImGui.GetIO();
            foreach (var size in new[] { 12f, 14f, 16f, 18f, 22f, 28f, 36f, 48f })
            {
                var font = io.Fonts.AddFontFromFileTTF(fontPath, size);
                configuredFonts.Add(new RenderFont(size, size, font));
            }
            _fonts = new FontSet(configuredFonts);
            io.NativePtr->FontDefault = _fonts.Select(18).Font.NativePtr;
            io.FontGlobalScale = 1;
        });
        return controller;
    }

    private static void ConfigureStyle()
    {
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        style.WindowPadding = new Vector2(12, 12);
        style.FramePadding = new Vector2(8, 4);
        style.ItemSpacing = new Vector2(8, 8);
        style.ItemInnerSpacing = new Vector2(4, 4);
        style.CellPadding = new Vector2(8, 4);
        style.IndentSpacing = 16;
        style.ScrollbarSize = 12;
        style.WindowMinSize = new Vector2(280, 160);
        style.WindowRounding = 3;
        style.FrameRounding = 2;
        style.TabRounding = 2;
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.055f, 0.06f, 0.075f, 1);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.045f, 0.048f, 0.06f, 1);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.07f, 0.073f, 0.09f, 1);
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.25f, 0.19f, 0.12f, 1);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.42f, 0.29f, 0.14f, 1);
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.25f, 0.19f, 0.12f, 1);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.42f, 0.29f, 0.14f, 1);
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.08f, 0.085f, 0.105f, 1);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.42f, 0.29f, 0.14f, 1);
        style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.3f, 0.21f, 0.12f, 1);
    }
}
