using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Addons;
using WoWAddonLab.Emulator.Diagnostics;
using WoWAddonLab.Emulator.Lua;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator;

public sealed class EmulatorSession : IDisposable
{
    private readonly ConcurrentQueue<Action> _commands = new();
    private string? _savedVariablesDirectory;
    private readonly string? _luaCacheDirectory;
    private int? _mouseCaptureId;
    private string? _capturedMouseButton;
    private Vector2 _mouseDownPosition;
    private bool _dragStarted;
    private Vector2 _previousCursor;
    private Func<string, byte[]?>? _fontAssetReader;
    private string _clipboardText = string.Empty;
    private uint _sliderPointerMoveUpdateCount = 1;
    private uint _sliderPointerMoveWindowStartMilliseconds;
    private WowBuildInfo _buildInfo = WowBuildInfo.Unknown;
    private bool _disposed;

    public EmulatorSession(
        float logicalWidth = 1600,
        float logicalHeight = 900,
        string? savedVariablesDirectory = null,
        string? luaCacheDirectory = null)
    {
        _savedVariablesDirectory = savedVariablesDirectory;
        _luaCacheDirectory = luaCacheDirectory;
        Log = new EmulatorLog();
        Ui = new UiSystem(useNativeScreenMetrics: true);
        Ui.Resize(logicalWidth, logicalHeight);
        Providers = new WowDataProviders();
        Lua = new LuaRuntime(
            Log,
            Ui,
            _savedVariablesDirectory,
            Providers,
            _luaCacheDirectory);
        ConfigureRuntimeHooks();
    }

    public EmulatorLog Log { get; }
    public WowDataProviders Providers { get; }
    public UiSystem Ui { get; private set; }
    public LuaRuntime Lua { get; private set; }
    public event Action<AddonLoadProgress>? LoadProgress;
    public IReadOnlyList<AddonManifest> Manifests { get; private set; } = [];
    public IReadOnlyList<AddonManifest> BootstrapManifests { get; private set; } = [];
    public IReadOnlyList<AddonManifest> AvailableAddonManifests { get; private set; } = [];
    public IReadOnlyList<string> BootstrapRuntimeFiles { get; private set; } = [];
    public AddonManifest? Manifest => Manifests.FirstOrDefault();
    public AddonManifestContext ManifestContext { get; set; } = AddonManifestContext.Mainline;
    public WowBuildInfo BuildInfo
    {
        get => _buildInfo;
        set
        {
            _buildInfo = value;
            Lua.BuildInfo = value;
        }
    }
    public Func<string?>? ClipboardReader { get; set; }
    public Action<string>? ClipboardWriter { get; set; }
    public Func<uint> InputTimestampMillisecondsProvider { get; set; } =
        static () => unchecked((uint)Environment.TickCount64);
    public Func<string, byte[]?>? FontAssetReader
    {
        get => _fontAssetReader;
        set
        {
            _fontAssetReader = value;
            Ui.FontAssetReader = value;
        }
    }
    public string ClipboardText
    {
        get => ClipboardReader?.Invoke() ?? _clipboardText;
        set
        {
            _clipboardText = value ?? string.Empty;
            ClipboardWriter?.Invoke(_clipboardText);
        }
    }
    public IWowAtlasProvider? AtlasProvider
    {
        get => Providers.Atlas;
        set => Providers.Atlas = value;
    }
    public IWowDyeColorProvider? DyeColorProvider
    {
        get => Providers.DyeColor;
        set => Providers.DyeColor = value;
    }
    public IWowGlobalColorProvider? GlobalColorProvider
    {
        get => Providers.GlobalColor;
        set => Providers.GlobalColor = value;
    }
    public IWowGameRuleProvider? GameRuleProvider
    {
        get => Providers.GameRule;
        set => Providers.GameRule = value;
    }
    public IWowMapProvider? MapProvider
    {
        get => Providers.Map;
        set => Providers.Map = value;
    }
    public IWowQuestProvider? QuestProvider
    {
        get => Providers.Quest;
        set => Providers.Quest = value;
    }
    public IWowGlobalStringProvider? GlobalStringProvider
    {
        get => Providers.GlobalString;
        set => Providers.GlobalString = value;
    }
    public IWowAchievementProvider? AchievementProvider
    {
        get => Providers.Achievement;
        set => Providers.Achievement = value;
    }
    public IWowAccountStoreProvider? AccountStoreProvider
    {
        get => Providers.AccountStore;
        set => Providers.AccountStore = value;
    }
    public IWowAzeriteEssenceProvider? AzeriteEssenceProvider
    {
        get => Providers.AzeriteEssence;
        set => Providers.AzeriteEssence = value;
    }
    public IWowModelInfoProvider? ModelInfoProvider
    {
        get => Providers.ModelInfo;
        set => Providers.ModelInfo = value;
    }
    public IWowModelResourceProvider? ModelResourceProvider
    {
        get => Providers.ModelResource;
        set => Providers.ModelResource = value;
    }
    public IWowMacroIconProvider? MacroIconProvider
    {
        get => Providers.MacroIcon;
        set => Providers.MacroIcon = value;
    }
    public IWowSpellProvider? SpellProvider
    {
        get => Providers.Spell;
        set => Providers.Spell = value;
    }
    public IWowItemClassProvider? ItemClassProvider
    {
        get => Providers.ItemClass;
        set => Providers.ItemClass = value;
    }
    public IWowItemProvider? ItemProvider
    {
        get => Providers.Item;
        set => Providers.Item = value;
    }
    public IWowInventorySlotProvider? InventorySlotProvider
    {
        get => Providers.InventorySlot;
        set => Providers.InventorySlot = value;
    }
    public IWowRaceProvider? RaceProvider
    {
        get => Providers.Race;
        set => Providers.Race = value;
    }
    public IWowFactionProvider? FactionProvider
    {
        get => Providers.Faction;
        set => Providers.Faction = value;
    }
    public IWowTransmogSetProvider? TransmogSetProvider
    {
        get => Providers.TransmogSet;
        set => Providers.TransmogSet = value;
    }
    public IWowTransmogAppearanceProvider? TransmogAppearanceProvider
    {
        get => Providers.TransmogAppearance;
        set => Providers.TransmogAppearance = value;
    }
    public IWowCharacterServiceProvider? CharacterServiceProvider
    {
        get => Providers.CharacterService;
        set => Providers.CharacterService = value;
    }
    public IWowEncounterJournalProvider? EncounterJournalProvider
    {
        get => Providers.EncounterJournal;
        set => Providers.EncounterJournal = value;
    }
    public string? LastError { get; private set; }
    public string? BootstrapLastError { get; private set; }
    public string? ClientCVarConfigPath { get; set; }
    public IReadOnlyDictionary<string, string> ClientCVarOverrides { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public void Load(string addonPath, string? savedVariablesDirectory = null) =>
        Load([addonPath], savedVariablesDirectory);

    public void Load(
        IReadOnlyList<string> addonPaths,
        string? savedVariablesDirectory = null,
        IReadOnlyList<string>? bootstrapAddonPaths = null,
        IReadOnlyList<string>? bootstrapRuntimeFiles = null,
        IReadOnlyList<string>? availableAddonPaths = null)
    {
        AddonManifest[] manifests;
        AddonManifest[] bootstrapManifests;
        AddonManifest[] availableManifests;
        using (var span = StartupTimeline.Begin("parse TOC manifests"))
        {
            manifests = addonPaths.Select(path => AddonManifest.Load(path, ManifestContext)).ToArray();
            bootstrapManifests = (bootstrapAddonPaths ?? [])
                .Select(path => AddonManifest.Load(path, ManifestContext))
                .ToArray();
            availableManifests = (availableAddonPaths ?? bootstrapAddonPaths ?? [])
                .Select(path => AddonManifest.Load(path, ManifestContext))
                .GroupBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
            span.Annotate(
                $"{manifests.Length} addon, {bootstrapManifests.Length} bootstrap, " +
                $"{availableManifests.Length} available");
        }
        if (Lua.IsLoaded)
        {
            Lua.TriggerEvent("PLAYER_LOGOUT");
            Lua.SaveVariables();
        }

        _savedVariablesDirectory = savedVariablesDirectory ?? _savedVariablesDirectory;
        using (StartupTimeline.Begin("recreate Lua runtime and UI system"))
            RecreateRuntime();
        Manifests = manifests;
        BootstrapManifests = AddonRequiredDependencyClosure.Resolve(
            bootstrapManifests,
            manifests,
            availableManifests);
        AvailableAddonManifests = availableManifests;
        BootstrapRuntimeFiles = bootstrapRuntimeFiles?.ToArray() ?? [];
        try
        {
            using (StartupTimeline.Begin("execute addon and Blizzard UI Lua"))
            {
                Lua.LoadAddons(
                    BootstrapManifests.Concat(Manifests).ToArray(),
                    BootstrapManifests.Count,
                    BootstrapRuntimeFiles,
                    AvailableAddonManifests);
            }
            UpdateLoadErrors();
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Log.Error("session", exception.ToString());
            throw;
        }
    }

    public void Reload()
    {
        if (Lua.IsLoaded)
        {
            Lua.TriggerEvent("PLAYER_LOGOUT");
            Lua.SaveVariables();
        }
        RecreateRuntime();
        Lua.LoadAddons(
            BootstrapManifests.Concat(Manifests).ToArray(),
            BootstrapManifests.Count,
            BootstrapRuntimeFiles,
            AvailableAddonManifests);
        UpdateLoadErrors();
    }

    private void RecreateRuntime()
    {
        var width = Ui.PhysicalWidth;
        var height = Ui.PhysicalHeight;
        var dpiScale = Ui.ScreenDpiScale;
        Lua.Dispose();
        Ui = new UiSystem(useNativeScreenMetrics: true);
        Ui.FontAssetReader = _fontAssetReader;
        Ui.SetScreenDpiScale(dpiScale);
        Ui.Resize(width, height);
        Lua = new LuaRuntime(
            Log,
            Ui,
            _savedVariablesDirectory,
            Providers,
            _luaCacheDirectory);
        Lua.BuildInfo = _buildInfo;
        ConfigureRuntimeHooks();
        if (ClientCVarConfigPath is not null)
            Lua.CVars.ImportConfigFile(ClientCVarConfigPath);
        foreach (var (name, value) in ClientCVarOverrides)
            Lua.CVars.SetValue(name, value);
    }

    private void ConfigureRuntimeHooks()
    {
        Lua.AbortFrameDragHandler = AbortFrameDrag;
        Lua.InterceptFrameDragHandler = InterceptFrameDrag;
        Lua.LoadProgress += progress => LoadProgress?.Invoke(progress);
    }

    private void AbortFrameDrag(UiObject value)
    {
        if (!_dragStarted || _mouseCaptureId != value.Id)
            return;

        _dragStarted = false;
        Lua.InvokeScript(value, "OnDragStop", _capturedMouseButton ?? "LeftButton");
    }

    private bool InterceptFrameDrag(UiObject source, UiObject target)
    {
        if (source.Id == target.Id ||
            !_dragStarted ||
            _mouseCaptureId != source.Id)
        {
            return false;
        }

        _mouseCaptureId = target.Id;
        return true;
    }

    private void UpdateLoadErrors()
    {
        var bootstrapNames = BootstrapManifests
            .Concat(AvailableAddonManifests)
            .Select(value => value.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        BootstrapLastError = FormatErrors(Lua.AddonLoadErrors.Where(value => bootstrapNames.Contains(value.Key)));
        LastError = FormatErrors(Lua.AddonLoadErrors.Where(value => !bootstrapNames.Contains(value.Key)));
    }

    private static string? FormatErrors(IEnumerable<KeyValuePair<string, string>> errors)
    {
        var formatted = errors.Select(value => $"{value.Key}: {value.Value}").ToArray();
        return formatted.Length == 0 ? null : string.Join(Environment.NewLine, formatted);
    }

    public void Tick(double deltaSeconds)
    {
        while (_commands.TryDequeue(out var command))
            command();
        Lua.Tick(deltaSeconds);
    }

    public Task<T> InvokeAsync<T>(Func<EmulatorSession, T> action, CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _commands.Enqueue(() =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
                return;
            }
            try
            {
                completion.TrySetResult(action(this));
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        });
        return completion.Task;
    }

    public void Resize(float width, float height)
    {
        var previousScale = Ui.AppliedUiScale;
        Ui.Resize(width, height);
        if (!Lua.IsLoaded)
            return;
        Lua.TriggerEvent("DISPLAY_SIZE_CHANGED");
        if (MathF.Abs(previousScale - Ui.AppliedUiScale) >= 0.0001f)
            Lua.TriggerEvent("UI_SCALE_CHANGED");
    }

    public void MouseMove(float x, float y)
    {
        _previousCursor = Ui.CursorPosition;
        Ui.CursorPosition = new Vector2(x, y);

        if (_mouseCaptureId is { } sliderCaptureId &&
            Ui.Find(sliderCaptureId) is { SliderDraggingThumb: true } slider)
        {
            if (ShouldUpdateSliderFromMouseMove())
                UpdateSliderFromCursor(slider);
        }
        if (_mouseCaptureId is { } colorSelectCaptureId &&
            Ui.Find(colorSelectCaptureId) is { ColorSelect: not null } colorSelect)
        {
            LuaBindings.UpdateColorSelectFromCursor(
                Lua,
                colorSelect,
                Ui.CursorPosition);
        }
        if (_mouseCaptureId is { } editBoxCaptureId &&
            _capturedMouseButton?.Equals(
                "LeftButton",
                StringComparison.OrdinalIgnoreCase) == true &&
            Ui.Find(editBoxCaptureId) is { } capturedEditBox &&
            IsEditBoxLike(capturedEditBox))
        {
            SetEditBoxCursorFromMouse(capturedEditBox, extendSelection: true);
        }

        if (!_dragStarted &&
            _mouseCaptureId is { } captureId &&
            _capturedMouseButton is { } capturedButton &&
            Ui.Find(captureId) is { } captured &&
            captured.DragRegistrations.Contains(capturedButton) &&
            Vector2.Distance(_mouseDownPosition, Ui.CursorPosition) >= 5)
        {
            _dragStarted = true;
            Lua.InvokeScript(captured, "OnDragStart", capturedButton);
        }

        if (Ui.MovingObjectId is { } movingId && Ui.Find(movingId) is { } moving)
        {
            var delta = Ui.CursorPosition - _previousCursor;
            ApplyActiveFrameMotion(moving, Ui.MovingPoint ?? "CENTER", delta);
        }

        var previousFoci = Ui.MouseFoci().ToArray();
        var currentFoci = Ui.FindMouseFoci(Ui.CursorPosition).ToArray();
        if (previousFoci.Select(value => value.Id).SequenceEqual(
                currentFoci.Select(value => value.Id)))
            return;

        var previousIds = previousFoci.Select(value => value.Id).ToHashSet();
        var currentIds = currentFoci.Select(value => value.Id).ToHashSet();
        Ui.SetMouseFoci(currentFoci);

        foreach (var left in previousFoci.Where(value => !currentIds.Contains(value.Id)))
        {
            RefreshMouseFocusVisual(left);
            Lua.InvokeScript(left, "OnLeave");
        }

        foreach (var entered in currentFoci.Where(value => !previousIds.Contains(value.Id)))
        {
            RefreshMouseFocusVisual(entered);
            Lua.InvokeScript(entered, "OnEnter");
        }
    }

    private void ApplyActiveFrameMotion(UiObject value, string point, Vector2 delta)
    {
        if (delta == Vector2.Zero)
            return;

        var bounds = Ui.ResolveBounds(value.Id);
        var left = bounds.Left;
        var right = bounds.Right;
        var bottom = bounds.Bottom;
        var top = bounds.Top;
        point = point.ToUpperInvariant();

        if (point == "CENTER")
        {
            left += delta.X;
            right += delta.X;
            bottom += delta.Y;
            top += delta.Y;
        }
        else
        {
            var resizeLeft = point.Contains("LEFT", StringComparison.Ordinal);
            var resizeRight = point.Contains("RIGHT", StringComparison.Ordinal);
            var resizeTop = point.Contains("TOP", StringComparison.Ordinal);
            var resizeBottom = point.Contains("BOTTOM", StringComparison.Ordinal);

            if (resizeLeft)
                left += delta.X;
            if (resizeRight)
                right += delta.X;
            if (resizeTop)
                top += delta.Y;
            if (resizeBottom)
                bottom += delta.Y;

            var width = Math.Max(0, right - left);
            var height = Math.Max(0, top - bottom);
            width = ClampResizeExtent(
                width,
                value.ResizeMinimum.X,
                value.ResizeMaximum.X);
            height = ClampResizeExtent(
                height,
                value.ResizeMinimum.Y,
                value.ResizeMaximum.Y);

            if (resizeLeft)
                left = right - width;
            else
                right = left + width;
            if (resizeBottom)
                bottom = top - height;
            else
                top = bottom + height;
        }

        SetAbsoluteBounds(value, left, bottom, right - left, top - bottom);
        Lua.QueueSizeChanged(value);
        if (value.ParentId is { } parentId &&
            Ui.Find(parentId) is { } parent &&
            parent.ScrollChildId == value.Id)
        {
            Lua.QueueScrollChildRect(parent);
        }
    }

    private void SetAbsoluteBounds(
        UiObject value,
        float left,
        float bottom,
        float width,
        float height)
    {
        var layoutScale = Ui.LayoutScale(value);
        var divisor = MathF.Abs(layoutScale) < 0.000001f ? 1 : layoutScale;
        value.AllPointsTargetId = null;
        value.Anchors.Clear();
        value.Anchors.Add(
            new UiAnchor(
                "BOTTOMLEFT",
                Ui.UiParentId,
                "BOTTOMLEFT",
                left / divisor,
                bottom / divisor));
        value.Width = width / divisor;
        value.Height = height / divisor;
        Ui.InvalidateLayout();
    }

    private static float ClampResizeExtent(float value, float minimum, float maximum)
    {
        if (minimum > 0)
            value = Math.Max(value, minimum);
        if (maximum > 0)
            value = Math.Min(value, maximum);
        return value;
    }

    public void MouseButton(string button, bool down)
    {
        if (UiObject.NormalizeMouseButtonName(button) is { } normalizedButton)
        {
            if (down)
                Lua.Input.MouseButtonsDown.Add(normalizedButton);
            else
                Lua.Input.MouseButtonsDown.Remove(normalizedButton);
        }
        Lua.TriggerEvent(down ? "GLOBAL_MOUSE_DOWN" : "GLOBAL_MOUSE_UP", button);

        if (down)
        {
            var downTarget = Ui.HitTest(Ui.CursorPosition, requireClick: true, button);
            if (downTarget is null || !downTarget.Enabled)
                return;
            if (IsButtonLike(downTarget) &&
                !AcceptsMouseTransition(downTarget, button, isDown: true))
            {
                return;
            }
            _mouseCaptureId = downTarget.Id;
            _capturedMouseButton = button;
            _mouseDownPosition = Ui.CursorPosition;
            _dragStarted = false;
            if (IsEditBoxLike(downTarget))
            {
                if (button.Equals("LeftButton", StringComparison.OrdinalIgnoreCase))
                    SetEditBoxCursorFromMouse(downTarget, extendSelection: false);
                Lua.SetKeyboardFocus(downTarget);
            }
            BeginSliderMouseInteraction(downTarget, button);
            LuaBindings.BeginColorSelectInteraction(
                Lua,
                downTarget,
                Ui.CursorPosition);
            Lua.InvokeScript(downTarget, "OnMouseDown", button);
            if (downTarget.Enabled &&
                AcceptsClick(downTarget, button, isDown: true))
            {
                Lua.InvokeButtonClick(downTarget, button, true);
            }
            if (downTarget.Enabled)
                SetPressedVisual(downTarget, true);
            return;
        }

        var capturedId = _mouseCaptureId;
        var target = capturedId is { } capture
            ? Ui.Find(capture)
            : Ui.HitTest(Ui.CursorPosition, requireClick: true, button);
        try
        {
            if (target is null)
                return;
            if (IsButtonLike(target) &&
                !AcceptsMouseTransition(target, button, isDown: false))
            {
                return;
            }
            if (target.SliderDraggingThumb)
                target.SliderDraggingThumb = false;
            LuaBindings.EndColorSelectInteraction(target);
            if (!target.Enabled)
                return;

            Lua.InvokeScript(target, "OnMouseUp", button);
            if (_dragStarted)
            {
                Lua.InvokeScript(target, "OnDragStop", button);
            }
            else
            {
                var hovered = Ui.HitTest(Ui.CursorPosition, requireClick: true, button);
                if (target.Enabled &&
                    hovered?.Id == target.Id &&
                    AcceptsClick(target, button, isDown: false))
                {
                    var isDoubleClick =
                        target.ScriptReferences.ContainsKey("OnDoubleClick") &&
                        target.LastButtonClickTime is { } previousClick &&
                        Lua.Time - previousClick <= 0.3;
                    if (isDoubleClick)
                    {
                        Lua.InvokeButtonDoubleClick(target, button);
                        target.LastButtonClickTime = null;
                    }
                    else
                    {
                        Lua.InvokeButtonClick(target, button, false);
                        target.LastButtonClickTime = Lua.Time;
                    }
                }
            }
            if (target.Enabled)
                SetPressedVisual(target, false);
        }
        finally
        {
            _mouseCaptureId = null;
            _capturedMouseButton = null;
            _dragStarted = false;
        }
    }

    private bool SetEditBoxCursorFromMouse(
        UiObject target,
        bool extendSelection)
    {
        if (target.EditBoxCaretStops.Count == 0)
            return false;

        var cursor = Ui.CursorPosition;
        var line = target.EditBoxCaretStops
            .GroupBy(stop => (stop.Bottom, stop.Top))
            .OrderBy(group =>
            {
                var (bottom, top) = group.Key;
                if (cursor.Y < bottom)
                    return bottom - cursor.Y;
                if (cursor.Y > top)
                    return cursor.Y - top;
                return 0;
            })
            .First();
        var stop = line
            .OrderBy(candidate => MathF.Abs(candidate.X - cursor.X))
            .First();
        var rawPosition = Math.Clamp(
            stop.RawUtf16Position,
            0,
            target.TextValue.Length);

        target.CursorPosition = rawPosition;
        if (extendSelection)
        {
            target.EditBoxHighlightStart = Math.Clamp(
                target.EditBoxHighlightStart,
                0,
                target.TextValue.Length);
            target.EditBoxHighlightEnd = rawPosition;
        }
        else
        {
            target.EditBoxHighlightStart = rawPosition;
            target.EditBoxHighlightEnd = rawPosition;
        }
        Ui.InvalidateLayout();
        return true;
    }

    private void BeginSliderMouseInteraction(UiObject target, string button)
    {
        if (!target.Enabled ||
            !target.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var thumb = target.ThumbTextureId is { } thumbId
            ? Ui.Find(thumbId)
            : null;
        if (thumb is null)
        {
            target.SliderDraggingThumb = true;
            UpdateSliderFromCursor(target);
            return;
        }

        var thumbBounds = Ui.ResolveBounds(thumb.Id);
        if (thumbBounds.Contains(Ui.CursorPosition))
        {
            target.SliderDraggingThumb = true;
            UpdateSliderFromCursor(target);
            return;
        }

        if (target.StepsPerPage <= 0)
        {
            target.SliderDraggingThumb = true;
            UpdateSliderFromCursor(target);
            return;
        }

        var vertical =
            target.StatusBar?.Orientation.Equals(
                "VERTICAL",
                StringComparison.OrdinalIgnoreCase) == true;
        var beforeThumb = vertical
            ? Ui.CursorPosition.Y >= thumbBounds.Top
            : Ui.CursorPosition.X < thumbBounds.Left;
        var direction = beforeThumb ? -1 : 1;
        var current = target.StatusBar?.Value ?? 0;
        Lua.SetSliderValue(
            target,
            current + direction * target.StepsPerPage * target.ValueStep,
            false);
    }

    private void UpdateSliderFromCursor(UiObject slider)
    {
        var track = Ui.ResolveBounds(slider.Id);
        var thumb = slider.ThumbTextureId is { } thumbId
            ? Ui.Find(thumbId)
            : null;
        if (thumb is null)
            return;
        var thumbBounds = Ui.ResolveBounds(thumb.Id);
        var state = slider.StatusBar ??= new UiStatusBarState();
        double ratio;
        if (state.Orientation.Equals("VERTICAL", StringComparison.OrdinalIgnoreCase))
        {
            var usable = Math.Max(0, track.Height - thumbBounds.Height);
            ratio = usable <= float.Epsilon
                ? 0
                : (track.Top - thumbBounds.Height / 2 - Ui.CursorPosition.Y) / usable;
        }
        else
        {
            var usable = Math.Max(0, track.Width - thumbBounds.Width);
            ratio = usable <= float.Epsilon
                ? 0
                : (Ui.CursorPosition.X - track.Left - thumbBounds.Width / 2) / usable;
        }

        var next = state.Minimum +
                   Math.Clamp(ratio, 0, 1) * (state.Maximum - state.Minimum);
        Lua.SetSliderValue(slider, next, true);
    }

    private bool ShouldUpdateSliderFromMouseMove()
    {
        const uint maximumUpdatesPerWindow = 1;
        const uint windowMilliseconds = 10;

        var now = InputTimestampMillisecondsProvider();
        var updateCount = _sliderPointerMoveUpdateCount;
        if (updateCount >= maximumUpdatesPerWindow &&
            unchecked(now - _sliderPointerMoveWindowStartMilliseconds) >=
            windowMilliseconds)
        {
            _sliderPointerMoveWindowStartMilliseconds = now;
            updateCount = 0;
        }

        _sliderPointerMoveUpdateCount = updateCount + 1;
        return _sliderPointerMoveUpdateCount <= maximumUpdatesPerWindow;
    }

    private static bool AcceptsClick(UiObject target, string button, bool isDown)
    {
        var transition = isDown ? "Down" : "Up";
        return target.ClickRegistrations.Contains($"{button}{transition}") ||
               target.ClickRegistrations.Contains($"Any{transition}");
    }

    private static bool AcceptsMouseTransition(UiObject target, string button, bool isDown)
    {
        var transition = isDown ? "Down" : "Up";
        return target.MouseRegistrations.Contains($"{button}{transition}") ||
               target.MouseRegistrations.Contains($"Any{transition}");
    }

    private static bool IsButtonLike(UiObject value) =>
        value.ObjectType.EndsWith("Button", StringComparison.OrdinalIgnoreCase);

    private void RefreshMouseFocusVisual(UiObject value)
    {
        LuaBindings.RefreshButtonFont(Lua, value);
    }

    private void SetPressedVisual(UiObject value, bool pressed)
    {
        if (value.ButtonStateLocked)
            return;
        LuaBindings.SetButtonVisualState(
            Lua,
            value,
            value.Enabled,
            pressed ? UiButtonState.Pushed : UiButtonState.Normal);
    }

    public void MouseWheel(float delta)
    {
        var target = Ui.HitTestMouseWheel(Ui.CursorPosition);
        if (target is not null)
            Lua.InvokeScript(target, "OnMouseWheel", delta);
    }

    public void Key(string wowKey, bool down, bool control = false, bool shift = false, bool alt = false)
    {
        Lua.ControlDown = control;
        Lua.ShiftDown = shift;
        Lua.AltDown = alt;

        var focusedEditBox =
            Ui.FocusedObjectId is { } focusedId &&
            Ui.Find(focusedId) is { } focusedObject &&
            IsEditBoxLike(focusedObject) &&
            Ui.IsVisible(focusedObject)
                ? focusedObject
                : null;

        var focusedEditBoxReceivedKeyEvent = false;
        if (focusedEditBox is { } focused)
        {
            Lua.FlushPendingEditBoxTextChange(focused);
            if (down && focused.ScriptReferences.ContainsKey("OnKeyDown"))
            {
                focusedEditBoxReceivedKeyEvent = true;
                Lua.InvokeScript(focused, "OnKeyDown", wowKey);
            }

            if (!down && focused.ScriptReferences.ContainsKey("OnKeyUp"))
            {
                focusedEditBoxReceivedKeyEvent = true;
                Lua.InvokeScript(focused, "OnKeyUp", wowKey);
            }

            if (down &&
                EditBoxKeyScript(wowKey) is { } editBoxScript &&
                focused.ScriptReferences.ContainsKey(editBoxScript))
            {
                Lua.InvokeScript(focused, editBoxScript);
                return;
            }

            if (down && HandleEditBoxNavigationKey(focused, wowKey))
                return;

            if (down &&
                (wowKey.Length == 1 ||
                 wowKey.Equals("SPACE", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (!focused.PropagateKeyboardInput)
                return;
        }

        var propagate = true;
        foreach (var target in Ui.RenderOrder()
                     .Where(value =>
                         value.KeyboardEnabled &&
                         !value.IsRegion &&
                         (!focusedEditBoxReceivedKeyEvent ||
                          value.Id != focusedEditBox!.Id))
                     .Reverse()
                     .ToArray())
        {
            Lua.InvokeScript(target, down ? "OnKeyDown" : "OnKeyUp", wowKey);
            if (!target.PropagateKeyboardInput)
            {
                propagate = false;
                break;
            }
        }

        if (propagate && (down || focusedEditBox is null))
        {
            var bindingKey = BindingKey(wowKey);
            var action = Lua.Bindings.GetEffectiveAction(bindingKey, 0);
            if (action.Length > 0 && (down || Lua.Bindings.RunsOnUp(action)))
            {
                try
                {
                    Lua.ExecuteBinding(action, down ? "down" : "up");
                }
                catch (Exception exception)
                {
                    Log.Error("input", $"{action} binding failed: {exception.Message}");
                }
            }
            else if (down && action.Length == 0 &&
                     wowKey.Equals("ESCAPE", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Lua.Evaluate(
                        "(function() if type(ToggleGameMenu)=='function' then " +
                        "ToggleGameMenu(); return true end return false end)()");
                }
                catch (Exception exception)
                {
                    Log.Error("input", $"TOGGLEGAMEMENU failed: {exception.Message}");
                }
            }
            else if (down && action.Length == 0 && focusedEditBox is null &&
                     wowKey.Equals("ENTER", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Lua.Evaluate(
                        "(function() " +
                        "if type(ChatFrameUtil)=='table' and type(ChatFrameUtil.OpenChat)=='function' then " +
                        "ChatFrameUtil.OpenChat(''); return true end " +
                        "if type(ChatFrame_OpenChat)=='function' then " +
                        "ChatFrame_OpenChat(''); return true end " +
                        "return false end)()");
                }
                catch (Exception exception)
                {
                    Log.Error("input", $"OPENCHAT failed: {exception.Message}");
                }
            }
        }
    }

    private string BindingKey(string wowKey)
    {
        var parts = new List<string>(4);
        if (Lua.ControlDown)
            parts.Add("CTRL");
        if (Lua.ShiftDown)
            parts.Add("SHIFT");
        if (Lua.AltDown)
            parts.Add("ALT");
        parts.Add(wowKey.ToUpperInvariant());
        return string.Join('-', parts);
    }

    private static string? EditBoxKeyScript(string wowKey) =>
        wowKey.ToUpperInvariant() switch
        {
            "ENTER" => "OnEnterPressed",
            "ESCAPE" => "OnEscapePressed",
            "TAB" => "OnTabPressed",
            "SPACE" => "OnSpacePressed",
            _ => null
        };

    private bool HandleEditBoxNavigationKey(UiObject target, string wowKey)
    {
        var normalizedKey = wowKey.ToUpperInvariant();
        var isArrowKey = normalizedKey is "LEFT" or "RIGHT" or "UP" or "DOWN";
        if (isArrowKey && target.EditBoxAltArrowKeyMode && !Lua.AltDown)
            return false;

        var cursor = Math.Clamp(target.CursorPosition, 0, target.TextValue.Length);
        var selectionStart = Math.Clamp(
            Math.Min(target.EditBoxHighlightStart, target.EditBoxHighlightEnd),
            0,
            target.TextValue.Length);
        var selectionEnd = Math.Clamp(
            Math.Max(target.EditBoxHighlightStart, target.EditBoxHighlightEnd),
            selectionStart,
            target.TextValue.Length);
        var hasSelection = selectionStart != selectionEnd;

        void CollapseSelection(int position)
        {
            target.CursorPosition = position;
            target.EditBoxHighlightStart = position;
            target.EditBoxHighlightEnd = position;
            Ui.InvalidateLayout();
        }

        void MoveCursor(int position)
        {
            position = Math.Clamp(position, 0, target.TextValue.Length);
            if (!Lua.ShiftDown)
            {
                CollapseSelection(position);
                return;
            }

            var anchor = hasSelection
                ? cursor == target.EditBoxHighlightStart
                    ? target.EditBoxHighlightEnd
                    : target.EditBoxHighlightStart
                : cursor;
            target.CursorPosition = position;
            target.EditBoxHighlightStart = Math.Clamp(
                anchor,
                0,
                target.TextValue.Length);
            target.EditBoxHighlightEnd = position;
            Ui.InvalidateLayout();
        }

        void DispatchArrow(string direction) =>
            Lua.InvokeScript(target, "OnArrowPressed", direction);

        switch (normalizedKey)
        {
            case "A" when Lua.ControlDown:
                target.EditBoxHighlightStart = 0;
                target.EditBoxHighlightEnd = target.TextValue.Length;
                Ui.InvalidateLayout();
                return true;
            case "C" when Lua.ControlDown:
                CopyEditBoxSelectionToClipboard(target, selectionStart, selectionEnd);
                return true;
            case "X" when Lua.ControlDown:
                if (CopyEditBoxSelectionToClipboard(target, selectionStart, selectionEnd))
                {
                    ReplaceEditBoxText(
                        target,
                        target.TextValue.Remove(
                            selectionStart,
                            selectionEnd - selectionStart),
                        selectionStart);
                }
                return true;
            case "V" when Lua.ControlDown:
                PasteIntoEditBox(target);
                return true;
            case "B" when Lua.ControlDown:
                MoveCursor(
                    hasSelection && !Lua.ShiftDown
                        ? selectionStart
                        : PreviousUtf16Boundary(target.TextValue, cursor));
                return true;
            case "D" when Lua.ControlDown:
                if (hasSelection)
                {
                    ReplaceEditBoxText(
                        target,
                        target.TextValue.Remove(
                            selectionStart,
                            selectionEnd - selectionStart),
                        selectionStart);
                }
                else if (cursor < target.TextValue.Length)
                {
                    var next = NextUtf16Boundary(target.TextValue, cursor);
                    ReplaceEditBoxText(
                        target,
                        target.TextValue.Remove(cursor, next - cursor),
                        cursor);
                }
                return true;
            case "F" when Lua.ControlDown:
                MoveCursor(
                    hasSelection && !Lua.ShiftDown
                        ? selectionEnd
                        : NextUtf16Boundary(target.TextValue, cursor));
                return true;
            case "K" when Lua.ControlDown:
                if (cursor < target.TextValue.Length)
                {
                    ReplaceEditBoxText(
                        target,
                        target.TextValue.Remove(cursor),
                        cursor);
                }
                return true;
            case "N" when Lua.ControlDown:
                NavigateEditBoxHistory(target, previous: false);
                return true;
            case "P" when Lua.ControlDown:
                NavigateEditBoxHistory(target, previous: true);
                return true;
            case "U" when Lua.ControlDown:
                if (cursor > 0)
                {
                    ReplaceEditBoxText(
                        target,
                        target.TextValue.Remove(0, cursor),
                        0);
                }
                return true;
            case "W" when Lua.ControlDown:
                if (hasSelection)
                {
                    ReplaceEditBoxText(
                        target,
                        target.TextValue.Remove(
                            selectionStart,
                            selectionEnd - selectionStart),
                        selectionStart);
                }
                else if (cursor > 0)
                {
                    var previous = PreviousEditBoxWordBoundary(
                        target.TextValue,
                        cursor);
                    ReplaceEditBoxText(
                        target,
                        target.TextValue.Remove(previous, cursor - previous),
                        previous);
                }
                return true;
            case "LEFT":
                MoveCursor(
                    hasSelection && !Lua.ShiftDown && !Lua.ControlDown
                        ? selectionStart
                        : Lua.ControlDown
                            ? PreviousEditBoxWordBoundary(target.TextValue, cursor)
                            : PreviousUtf16Boundary(target.TextValue, cursor));
                DispatchArrow("LEFT");
                return true;
            case "RIGHT":
                MoveCursor(
                    hasSelection && !Lua.ShiftDown && !Lua.ControlDown
                        ? selectionEnd
                        : Lua.ControlDown
                            ? NextEditBoxWordBoundary(target.TextValue, cursor)
                            : NextUtf16Boundary(target.TextValue, cursor));
                DispatchArrow("RIGHT");
                return true;
            case "UP" when target.MultiLine:
                MoveCursor(AdjacentVisualLinePosition(target, cursor, -1));
                return true;
            case "DOWN" when target.MultiLine:
                MoveCursor(AdjacentVisualLinePosition(target, cursor, 1));
                return true;
            case "UP" when Lua.AltDown:
                NavigateEditBoxHistory(target, previous: true);
                return true;
            case "DOWN" when Lua.AltDown:
                NavigateEditBoxHistory(target, previous: false);
                return true;
            case "UP":
                DispatchArrow("UP");
                return true;
            case "DOWN":
                DispatchArrow("DOWN");
                return true;
            case "HOME":
                MoveCursor(
                    target.MultiLine && !Lua.ControlDown
                        ? HardLineStart(target.TextValue, cursor)
                        : 0);
                return true;
            case "END":
                MoveCursor(
                    target.MultiLine && !Lua.ControlDown
                        ? HardLineEnd(target.TextValue, cursor)
                        : target.TextValue.Length);
                return true;
            case "ENTER" when target.MultiLine:
                InsertEditBoxCodepoint(target, new Rune('\n'));
                return true;
            case "INSERT" when Lua.ControlDown:
                CopyEditBoxSelectionToClipboard(target, selectionStart, selectionEnd);
                return true;
            case "INSERT" when Lua.ShiftDown:
                PasteIntoEditBox(target);
                return true;
            case "BACKSPACE" when hasSelection:
                ReplaceEditBoxText(
                    target,
                    target.TextValue.Remove(
                        selectionStart,
                        selectionEnd - selectionStart),
                    selectionStart);
                target.EditBoxHighlightStart = selectionStart;
                target.EditBoxHighlightEnd = selectionStart;
                return true;
            case "BACKSPACE" when Lua.ControlDown && cursor > 0:
            {
                var previous = PreviousEditBoxWordBoundary(
                    target.TextValue,
                    cursor);
                ReplaceEditBoxText(
                    target,
                    target.TextValue.Remove(previous, cursor - previous),
                    previous);
                return true;
            }
            case "BACKSPACE" when cursor > 0:
            {
                var previous = PreviousUtf16Boundary(target.TextValue, cursor);
                ReplaceEditBoxText(
                    target,
                    target.TextValue.Remove(previous, cursor - previous),
                    previous);
                return true;
            }
            case "BACKSPACE":
                return true;
            case "DELETE" when Lua.ShiftDown:
                if (CopyEditBoxSelectionToClipboard(
                        target,
                        selectionStart,
                        selectionEnd))
                {
                    ReplaceEditBoxText(
                        target,
                        target.TextValue.Remove(
                            selectionStart,
                            selectionEnd - selectionStart),
                        selectionStart);
                }
                return true;
            case "DELETE" when hasSelection:
                ReplaceEditBoxText(
                    target,
                    target.TextValue.Remove(
                        selectionStart,
                        selectionEnd - selectionStart),
                    selectionStart);
                target.EditBoxHighlightStart = selectionStart;
                target.EditBoxHighlightEnd = selectionStart;
                return true;
            case "DELETE" when Lua.ControlDown && cursor < target.TextValue.Length:
            {
                var next = NextEditBoxDeletionWordBoundary(
                    target.TextValue,
                    cursor);
                ReplaceEditBoxText(
                    target,
                    target.TextValue.Remove(cursor, next - cursor),
                    cursor);
                return true;
            }
            case "DELETE" when cursor < target.TextValue.Length:
            {
                var next = NextUtf16Boundary(target.TextValue, cursor);
                ReplaceEditBoxText(
                    target,
                    target.TextValue.Remove(cursor, next - cursor),
                    cursor);
                return true;
            }
            case "DELETE":
                return true;
            default:
                return false;
        }
    }

    public void TextInput(string text)
    {
        if (Ui.FocusedObjectId is not { } focusId ||
            Ui.Find(focusId) is not { } target ||
            !IsEditBoxLike(target) ||
            !Ui.IsVisible(target))
            return;

        Lua.FlushPendingEditBoxTextChange(target);
        var insertion = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            if (TryGetEditBoxCodepointInsertion(target, rune, out var value))
                insertion.Append(value);
        }
        if (insertion.Length != 0)
            InsertEditBoxText(target, insertion.ToString());
    }

    public void InputLanguageChanged(UiEditBoxInputLanguage language)
    {
        if (Ui.FocusedObjectId is not { } focusId ||
            Ui.Find(focusId) is not { } target ||
            !IsEditBoxLike(target) ||
            !Ui.IsVisible(target) ||
            target.EditBoxPassword ||
            target.EditBoxInputLanguage == language)
        {
            return;
        }

        target.EditBoxInputLanguage = language;
        Lua.InvokeScript(target, "OnInputLanguageChanged", language.ToWowName());
    }

    private bool CopyEditBoxSelectionToClipboard(
        UiObject target,
        int selectionStart,
        int selectionEnd)
    {
        if (target.EditBoxSecureText || selectionStart == selectionEnd)
            return false;

        ClipboardText = target.TextValue[selectionStart..selectionEnd];
        return ClipboardText.Length != 0;
    }

    private void PasteIntoEditBox(UiObject target)
    {
        if (target.EditBoxSecurityDisablePaste)
            return;

        foreach (var rune in ClipboardText.EnumerateRunes())
            InsertEditBoxCodepoint(target, rune);
    }

    private void InsertEditBoxCodepoint(UiObject target, Rune rune)
    {
        if (TryGetEditBoxCodepointInsertion(target, rune, out var insertion))
            InsertEditBoxText(target, insertion);
    }

    private static bool TryGetEditBoxCodepointInsertion(
        UiObject target,
        Rune rune,
        out string insertion)
    {
        if (rune.Value == '\t')
        {
            insertion = "    ";
        }
        else if (rune.Value == '\n' && target.MultiLine)
        {
            insertion = "\n";
        }
        else if (rune.Value >= 0x20 && rune.Value != 0x7F)
        {
            insertion = rune.Value == '|'
                ? "||"
                : rune.ToString();
        }
        else
        {
            insertion = string.Empty;
            return false;
        }

        return true;
    }

    private void InsertEditBoxText(UiObject target, string insertion)
    {
        var selectionStart = Math.Clamp(
            Math.Min(target.EditBoxHighlightStart, target.EditBoxHighlightEnd),
            0,
            target.TextValue.Length);
        var selectionEnd = Math.Clamp(
            Math.Max(target.EditBoxHighlightStart, target.EditBoxHighlightEnd),
            selectionStart,
            target.TextValue.Length);
        var cursor = selectionStart != selectionEnd
            ? selectionStart
            : Math.Clamp(target.CursorPosition, 0, target.TextValue.Length);
        var baseText = selectionStart != selectionEnd
            ? target.TextValue.Remove(selectionStart, selectionEnd - selectionStart)
            : target.TextValue;
        var next = EditBoxTextRules.ApplyInsertion(
            target,
            baseText,
            cursor,
            insertion);
        var nextCursor = next.Equals(baseText, StringComparison.Ordinal)
            ? Math.Min(cursor, next.Length)
            : Math.Min(cursor + insertion.Length, next.Length);
        target.EditBoxHighlightStart = nextCursor;
        target.EditBoxHighlightEnd = nextCursor;
        if (!next.Equals(target.TextValue, StringComparison.Ordinal))
            ReplaceEditBoxText(target, next, nextCursor);
        else
            target.CursorPosition = nextCursor;
    }

    private void NavigateEditBoxHistory(UiObject target, bool previous)
    {
        var count = target.EditBoxHistoryLines;
        if (count <= 0 || target.EditBoxHistory.Count == 0 ||
            target.SecurityDisableSetText)
        {
            return;
        }

        var direction = previous ? -1 : 1;
        for (var distance = 1; distance <= count; distance++)
        {
            var index =
                (target.EditBoxHistoryWriteIndex + direction * distance) % count;
            if (index < 0)
                index += count;
            if (index >= target.EditBoxHistory.Count ||
                target.EditBoxHistory[index] is not { } historyText)
            {
                continue;
            }

            target.EditBoxHistoryWriteIndex = index;
            ReplaceEditBoxText(
                target,
                historyText,
                historyText.Length,
                userInput: false);
            target.EditBoxHighlightStart = historyText.Length;
            target.EditBoxHighlightEnd = historyText.Length;
            return;
        }
    }

    private void ReplaceEditBoxText(
        UiObject target,
        string text,
        int cursor,
        bool userInput = true)
    {
        target.TextValue = text;
        target.CursorPosition = Math.Clamp(cursor, 0, text.Length);
        target.EditBoxCaretStops.Clear();
        if (target.Font is not null)
            target.Font.Text = text;
        Ui.InvalidateLayout();
        Lua.QueueEditBoxTextChanged(target, userInput);
        Lua.FlushPendingEditBoxTextChange(target);
    }

    private static bool IsEditBoxLike(UiObject value) =>
        value.ObjectType.Equals("EditBox", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("EventEditBox", StringComparison.OrdinalIgnoreCase);

    private static int PreviousUtf16Boundary(string value, int position)
    {
        position = Math.Clamp(position, 0, value.Length);
        if (position == 0)
            return 0;
        position--;
        if (position > 0 &&
            char.IsLowSurrogate(value[position]) &&
            char.IsHighSurrogate(value[position - 1]))
        {
            position--;
        }
        return position;
    }

    private static int PreviousEditBoxWordBoundary(string value, int position)
    {
        position = Math.Clamp(position, 0, value.Length);
        while (position > 0)
        {
            var previous = PreviousUtf16Boundary(value, position);
            if (!IsEditBoxWordSeparator(value, previous))
                break;
            position = previous;
        }

        while (position > 0)
        {
            var previous = PreviousUtf16Boundary(value, position);
            if (IsEditBoxWordSeparator(value, previous))
                break;
            position = previous;
        }

        return position;
    }

    private static int NextEditBoxWordBoundary(string value, int position)
    {
        position = Math.Clamp(position, 0, value.Length);
        while (position < value.Length &&
               !IsEditBoxWordSeparator(value, position))
        {
            position = NextUtf16Boundary(value, position);
        }

        while (position < value.Length &&
               IsEditBoxWordSeparator(value, position))
        {
            position = NextUtf16Boundary(value, position);
        }

        return position;
    }

    private static int NextEditBoxDeletionWordBoundary(string value, int position)
    {
        position = Math.Clamp(position, 0, value.Length);
        while (position < value.Length &&
               IsEditBoxWordSeparator(value, position))
        {
            position = NextUtf16Boundary(value, position);
        }

        while (position < value.Length &&
               !IsEditBoxWordSeparator(value, position))
        {
            position = NextUtf16Boundary(value, position);
        }

        return position;
    }

    private static bool IsEditBoxWordSeparator(string value, int position) =>
        Rune.GetUnicodeCategory(Rune.GetRuneAt(value, position)) ==
        UnicodeCategory.SpaceSeparator;

    private static int NextUtf16Boundary(string value, int position)
    {
        position = Math.Clamp(position, 0, value.Length);
        if (position >= value.Length)
            return value.Length;
        if (char.IsHighSurrogate(value[position]) &&
            position + 1 < value.Length &&
            char.IsLowSurrogate(value[position + 1]))
        {
            return position + 2;
        }
        return position + 1;
    }

    private static int HardLineStart(string value, int position)
    {
        position = Math.Clamp(position, 0, value.Length);
        var newline = position > 0
            ? value.LastIndexOf('\n', position - 1)
            : -1;
        return newline + 1;
    }

    private static int HardLineEnd(string value, int position)
    {
        position = Math.Clamp(position, 0, value.Length);
        var newline = value.IndexOf('\n', position);
        return newline < 0 ? value.Length : newline;
    }

    private static int AdjacentVisualLinePosition(
        UiObject target,
        int position,
        int direction)
    {
        var value = target.TextValue;
        position = Math.Clamp(position, 0, value.Length);
        var visualLines = target.EditBoxCaretStops
            .GroupBy(stop => (stop.Bottom, stop.Top))
            .Select(group => group
                .OrderBy(stop => stop.X)
                .ThenBy(stop => stop.RawUtf16Position)
                .ToArray())
            .Where(line => line.Length != 0)
            .OrderByDescending(line => line[0].Top)
            .ToArray();
        if (visualLines.Length > 1)
        {
            var currentLineIndex = 0;
            for (var index = 1; index < visualLines.Length; index++)
            {
                if (position < visualLines[index][0].RawUtf16Position)
                    break;
                currentLineIndex = index;
            }

            var targetLineIndex = Math.Clamp(
                currentLineIndex + Math.Sign(direction),
                0,
                visualLines.Length - 1);
            if (targetLineIndex == currentLineIndex)
                return position;

            var currentLine = visualLines[currentLineIndex];
            var visualColumn = 0;
            for (var index = 1; index < currentLine.Length; index++)
            {
                if (currentLine[index].RawUtf16Position > position)
                    break;
                visualColumn = index;
            }

            var targetLine = visualLines[targetLineIndex];
            return targetLine[Math.Min(visualColumn, targetLine.Length - 1)]
                .RawUtf16Position;
        }

        var currentStart = HardLineStart(value, position);
        var column = position - currentStart;
        if (direction < 0)
        {
            if (currentStart == 0)
                return position;
            var previousEnd = currentStart - 1;
            var previousStart = HardLineStart(value, previousEnd);
            return Math.Min(previousStart + column, previousEnd);
        }

        var currentEnd = HardLineEnd(value, position);
        if (currentEnd >= value.Length)
            return position;
        var nextStart = currentEnd + 1;
        var nextEnd = HardLineEnd(value, nextStart);
        return Math.Min(nextStart + column, nextEnd);
    }

    public object Status() => new
    {
        Loaded = Lua.IsLoaded,
        Addon = Manifest?.Name,
        Addons = Manifests.Select(value => value.Name).ToArray(),
        BlizzardUiModules = BootstrapManifests.Select(value => value.Name).ToArray(),
        AddonPath = Manifest?.RootPath,
        Lua = Lua.GetVersion(),
        Lua.Time,
        Lua.FrameRate,
        Objects = Ui.Objects.Count,
        VisibleObjects = Ui.Objects.Values.Count(Ui.IsVisible),
        MapHighlights = new
        {
            Maps = MapProvider?.HighlightMapCount ?? 0,
            Total = MapProvider?.HighlightCount ?? 0,
            CurrentMap = Lua.Maps.BestMapForPlayer,
            CurrentMapCount = MapProvider?.GetHighlightCount(Lua.Maps.BestMapForPlayer) ?? 0
        },
        Timers = "managed",
        LastError,
        BootstrapLastError,
        BootstrapFailureCount = ClassifyLoadFailures().Bootstrap.Length,
        UserFailureCount = ClassifyLoadFailures().User.Length,
        AddonHalted = Lua.TryEvaluate("DM and tostring(DM.halted) or 'unknown'"),
        AddonError = Lua.TryEvaluate("DM and DM.lastError or nil")
    };

    public object CompatibilityReport()
    {
        var failures = ClassifyLoadFailures();
        return new
        {
            BootstrapModules = BootstrapManifests.Select(value => value.Name).ToArray(),
            UserAddons = Manifests.Select(value => value.Name).ToArray(),
            BootstrapFailures = failures.Bootstrap,
            UserFailures = failures.User
        };
    }

    private (AddonLoadFailure[] Bootstrap, AddonLoadFailure[] User) ClassifyLoadFailures()
    {
        var bootstrapNames = BootstrapManifests
            .Concat(AvailableAddonManifests)
            .Select(value => value.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (
            Lua.AddonLoadFailures.Where(value => bootstrapNames.Contains(value.AddonName)).ToArray(),
            Lua.AddonLoadFailures.Where(value => !bootstrapNames.Contains(value.AddonName)).ToArray());
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (Lua.IsLoaded)
        {
            Lua.TriggerEvent("PLAYER_LOGOUT");
            Lua.SaveVariables();
        }
        Lua.Dispose();
    }
}
