using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiSystem
{
    private static readonly Vector4 EditBoxConstructorHighlightColor =
        new(96f / 255f, 96f / 255f, 96f / 255f, 1);

    private static readonly Dictionary<string, int> StrataOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WORLD"] = 0,
        ["BACKGROUND"] = 1,
        ["LOW"] = 2,
        ["MEDIUM"] = 3,
        ["HIGH"] = 4,
        ["DIALOG"] = 5,
        ["FULLSCREEN"] = 6,
        ["FULLSCREEN_DIALOG"] = 7,
        ["TOOLTIP"] = 8,
        ["BLIZZARD"] = 9
    };

    private static readonly Dictionary<string, int> LayerOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BACKGROUND"] = 0,
        ["BORDER"] = 1,
        ["ARTWORK"] = 2,
        ["OVERLAY"] = 3,
        ["HIGHLIGHT"] = 4
    };

    private readonly Dictionary<int, UiObject> _objects = [];
    private readonly Dictionary<string, int> _names = new(StringComparer.Ordinal);
    private readonly Dictionary<int, UiRect> _layoutCache = [];
    private readonly HashSet<int> _resolvedRectIds = [];
    private readonly List<int> _mouseMotionFocusIds = [];
    private readonly Dictionary<string, TrueTypeAdvanceMetrics?> _fontMetrics =
        new(StringComparer.OrdinalIgnoreCase);
    private Func<string, byte[]?>? _fontAssetReader;
    private int? _hoveredObjectId;
    private int _nextId;
    private int _nextRaisedFrameLevel = 1;

    private float _physicalWidth = 1600;
    private float _physicalHeight = 900;
    private float _screenDpiScale = 1;
    private bool _useUiScale;
    private float _uiScale = 1;
    private float _uiScaleMultiplier = -1;

    public UiSystem(bool useNativeScreenMetrics = false)
    {
        UsesNativeScreenMetrics = useNativeScreenMetrics;
    }

    public bool UsesNativeScreenMetrics { get; }

    public Func<string, byte[]?>? FontAssetReader
    {
        get => _fontAssetReader;
        set
        {
            _fontAssetReader = value;
            _fontMetrics.Clear();
        }
    }

    public float MeasureTextAdvance(
        UiFontState font,
        string text,
        float logicalEmHeight)
    {
        if (string.IsNullOrEmpty(text) || !(logicalEmHeight > 0))
            return 0;

        var key = font.FontPath.Replace('/', '\\').TrimStart('\\');
        if (!_fontMetrics.TryGetValue(key, out var metrics))
        {
            metrics = null;
            byte[]? bytes = null;
            try
            {
                bytes = _fontAssetReader?.Invoke(key);
            }
            catch
            {
            }
            if (bytes is not null)
                TrueTypeAdvanceMetrics.TryRead(bytes, out metrics);
            _fontMetrics[key] = metrics;
        }

        return metrics?.MeasureAdvance(text, logicalEmHeight) ??
               text.EnumerateRunes().Count() * logicalEmHeight * 0.54f;
    }

    public float PhysicalWidth => _physicalWidth;
    public float PhysicalHeight => _physicalHeight;
    public float ScreenAspectRatio => _physicalWidth / _physicalHeight;
    public float NormalizedScreenWidth
    {
        get
        {
            var aspect = ScreenAspectRatio;
            return aspect / MathF.Sqrt(aspect * aspect + 1);
        }
    }
    public float NormalizedScreenHeight
    {
        get
        {
            var aspect = ScreenAspectRatio;
            return 1 / MathF.Sqrt(aspect * aspect + 1);
        }
    }
    public float CoordinateScale => ScreenAspectRatio * .75f;
    public float NativeCoordinateUnitsPerLogicalUnit =>
        UsesNativeScreenMetrics && LogicalWidth > float.Epsilon
            ? CoordinateScale * 1024 / LogicalWidth
            : 1;
    public float NativeScaledRectScale =>
        UsesNativeScreenMetrics ? UiParentEffectiveScale : 1;

    public float ScreenDpiScale => _screenDpiScale;
    public bool UseUiScale => _useUiScale;
    public float UiScale => _uiScale;
    public float UiScaleMultiplier => _uiScaleMultiplier;

    public float DefaultUiScale => CalculateDefaultUiScale(
        (int)_physicalWidth,
        (int)_physicalHeight);

    public float AutomaticUiScale
    {
        get
        {
            var scaleFactor = _uiScaleMultiplier is >= .5f and <= 2f
                ? _uiScaleMultiplier
                : _screenDpiScale;
            if (!(scaleFactor > 0))
                return 1;
            return CalculateDefaultUiScale(
                (int)(_physicalWidth / scaleFactor),
                (int)(_physicalHeight / scaleFactor));
        }
    }

    public float AppliedUiScale => _useUiScale
        ? ClampUiScaleForAspect(_uiScale)
        : AutomaticUiScale;

    public float LogicalWidth =>
        UsesNativeScreenMetrics
            ? CoordinateScale * 1024 / UiParentEffectiveScale
            : _physicalWidth;

    public float LogicalHeight =>
        UsesNativeScreenMetrics
            ? NormalizedScreenHeight / NormalizedScreenWidth * LogicalWidth
            : _physicalHeight;
    public Vector2 CursorPosition { get; set; }
    public string? CursorAsset { get; set; }
    public int? HoveredObjectId
    {
        get => _hoveredObjectId;
        set
        {
            if (_hoveredObjectId == value)
                return;

            var previous = _hoveredObjectId;
            _hoveredObjectId = value;
            if (_mouseMotionFocusIds.Count != 0)
                return;

            if (previous is { } previousId && Find(previousId) is { } previousObject)
                UpdateHighlightDrawLayer(previousObject);
            if (value is { } currentId && Find(currentId) is { } currentObject)
                UpdateHighlightDrawLayer(currentObject);
        }
    }
    public int? FocusedObjectId { get; set; }
    public int? MovingObjectId { get; set; }
    public string? MovingPoint { get; set; }
    public int UiParentId { get; private set; }

    public IReadOnlyDictionary<int, UiObject> Objects => _objects;
    public IReadOnlyList<int> MouseMotionFocusIds => _mouseMotionFocusIds;
    internal int LastObjectId => _nextId;

    private readonly Dictionary<int, HashSet<int>> _fontDependents = [];

    internal void UpdateFontDependent(int objectId, int? previous, int? current)
    {
        if (previous is { } old && _fontDependents.TryGetValue(old, out var existing))
        {
            existing.Remove(objectId);
            if (existing.Count == 0)
                _fontDependents.Remove(old);
        }
        if (current is not { } added)
            return;
        if (!_fontDependents.TryGetValue(added, out var dependents))
            _fontDependents[added] = dependents = [];
        dependents.Add(objectId);
    }

    public IReadOnlyList<UiObject> FontDependents(int fontObjectId)
    {
        if (!_fontDependents.TryGetValue(fontObjectId, out var ids) || ids.Count == 0)
            return [];
        var result = new List<UiObject>(ids.Count);
        foreach (var id in ids)
        {
            if (_objects.TryGetValue(id, out var value))
                result.Add(value);
        }
        result.Sort((left, right) => left.Id.CompareTo(right.Id));
        return result;
    }

    public UiObject Create(string objectType, string? name, int? parentId, string? drawLayer = null, int subLevel = 0)
    {
        var id = ++_nextId;
        var objectValue = new UiObject
        {
            Id = id,
            ObjectType = objectType,
            Name = name,
            ParentId = parentId,
            FrameStrata = parentId is null ? "MEDIUM" : string.Empty,
            DrawLayer = drawLayer ?? "ARTWORK",
            SubLevel = subLevel
        };
        objectValue.Owner = this;

        switch (objectType.ToLowerInvariant())
        {
            case "button":
            case "aurabutton":
            case "checkbutton":
            case "dropdownbutton":
            case "dropdowntogglebutton":
            case "eventbutton":
            case "itembutton":
                objectValue.MouseEnabled = true;
                objectValue.ClickRegistrations.Add("LeftButtonUp");
                objectValue.MouseRegistrations.Add("AnyDown");
                objectValue.MouseRegistrations.Add("AnyUp");
                break;
            case "editbox":
            case "eventeditbox":
                objectValue.MouseEnabled = true;
                objectValue.AutoFocus = true;
                objectValue.EditBoxBlinkSpeed = 0.5f;
                objectValue.EditBoxHighlightColor = EditBoxConstructorHighlightColor;
                objectValue.Font = new UiFontState
                {
                    JustifyHorizontal = "LEFT",
                    JustifyVertical = "MIDDLE",
                    WordWrap = false,
                    HasLocalJustifyHorizontal = true,
                    HasLocalJustifyVertical = true,
                    LocalOverrides =
                        UiFontOverrides.JustifyHorizontal |
                        UiFontOverrides.JustifyVertical |
                        UiFontOverrides.WordWrap
                };
                break;
            case "browser":
            case "checkout":
                objectValue.MouseEnabled = true;
                objectValue.MouseWheelEnabled = true;
                objectValue.KeyboardEnabled = true;
                objectValue.Resizable = true;
                break;
            case "texture":
            case "masktexture":
                objectValue.Texture = new UiTextureState();
                objectValue.FrameStrata = string.Empty;
                break;
            case "font":
            case "fontstring":
                objectValue.Font = new UiFontState();
                objectValue.FrameStrata = string.Empty;
                break;
            case "messageframe":
            case "scrollingmessageframe":
                objectValue.Font = new UiFontState();
                break;
            case "simplehtml":
                var paragraph = new UiFontState();
                objectValue.Font = paragraph;
                objectValue.HtmlFonts["P"] = paragraph;
                objectValue.HtmlFonts["H1"] = new UiFontState();
                objectValue.HtmlFonts["H2"] = new UiFontState();
                objectValue.HtmlFonts["H3"] = new UiFontState();
                break;
            case "gametooltip":
                objectValue.Tooltip = new UiTooltipState();
                break;
            case "line":
                objectValue.Line = new UiLineState();
                objectValue.FrameStrata = string.Empty;
                break;
            case "modelscene":
                objectValue.ModelScene = new ModelSceneState();
                break;
            case "slider":
                objectValue.StatusBar = new UiStatusBarState { Maximum = 0 };
                objectValue.MouseEnabled = true;
                break;
            case "statusbar":
                objectValue.StatusBar = new UiStatusBarState { Maximum = 0 };
                break;
            case "cooldown":
                objectValue.Cooldown = new UiCooldownState();
                break;
            case "colorselect":
                objectValue.ColorSelect = new UiColorSelectState();
                objectValue.MouseEnabled = true;
                break;
            case "minimap":
            case "simpleminimap":
                objectValue.Minimap = new UiMinimapState();
                objectValue.MouseEnabled = true;
                break;
            case "blobframe":
            case "archaeologydigsiteframe":
            case "questpoiframe":
            case "scenariopoiframe":
                objectValue.Blob = new UiBlobState();
                break;
            case "animationgroup":
                objectValue.AnimationGroup = new UiAnimationGroupState();
                objectValue.FrameStrata = string.Empty;
                break;
            case "animation":
            case "alpha":
            case "translation":
            case "linetranslation":
            case "texturecoord":
            case "texturecoordtranslation":
            case "rotation":
            case "scale":
            case "linescale":
            case "flipbook":
            case "vertexcolor":
            case "path":
                objectValue.Animation = new UiAnimationState();
                objectValue.FrameStrata = string.Empty;
                break;
            case "controlpoint":
                objectValue.ControlPoint = new UiControlPointState();
                objectValue.FrameStrata = string.Empty;
                break;
            case "modelsceneactor":
                break;
        }

        _objects.Add(id, objectValue);
        if (name is not null)
            _names[name] = id;
        if (parentId is { } parent && _objects.TryGetValue(parent, out var parentObject))
        {
            parentObject.Children.Add(id);
            if (objectValue.IsFrameWidget)
            {
                objectValue.FrameStrata = EffectiveFrameStrata(parentObject);
                objectValue.FrameLevel = EffectiveFrameLevel(parentObject) + 1;
            }
            objectValue.RaisedFrameLevel = parentObject.RaisedFrameLevel;
            if (parentObject.Forbidden)
                SetForbidden(objectValue);
        }

        if (string.Equals(name, "UIParent", StringComparison.Ordinal))
            UiParentId = id;

        InvalidateLayout();
        return objectValue;
    }

    public IReadOnlyList<UiObject> ResolvePathControlPoints(UiObject path)
    {
        var uniqueByOrder = new SortedDictionary<int, UiObject>();
        var nextOrder = 0;
        foreach (var childId in path.Children)
        {
            if (Find(childId) is not { ControlPoint: { } controlPoint } child)
                continue;

            if (controlPoint.Order == -1 || controlPoint.Order < nextOrder)
                controlPoint.Order = nextOrder;
            nextOrder = controlPoint.Order + 1 <= 99
                ? controlPoint.Order + 1
                : 0;
            uniqueByOrder.TryAdd(controlPoint.Order, child);
        }

        var result = uniqueByOrder.Values.ToArray();
        for (var index = 0; index < result.Length; index++)
            result[index].ControlPoint!.NormalizedTime = (index + 1f) / result.Length;
        return result;
    }

    public UiObject Get(int id) =>
        _objects.TryGetValue(id, out var value)
            ? value
            : throw new KeyNotFoundException($"Unknown UI object id {id}.");

    public UiObject? Find(int id) => _objects.GetValueOrDefault(id);

    public UiObject? Find(string name) =>
        _names.TryGetValue(name, out var id) ? Find(id) : null;

    internal void RemoveInternalSubtree(int objectId)
    {
        if (!_objects.TryGetValue(objectId, out var value))
            return;

        InvalidateRectValidity(value);

        foreach (var childId in value.Children.ToArray())
            RemoveInternalSubtree(childId);

        if (value.ParentId is { } parentId && Find(parentId) is { } parent)
            parent.Children.Remove(objectId);
        value.FontObjectId = null;
        _fontDependents.Remove(objectId);
        if (value.Name is { } name &&
            _names.TryGetValue(name, out var namedId) &&
            namedId == objectId)
        {
            _names.Remove(name);
        }
        _objects.Remove(objectId);
        _layoutCache.Remove(objectId);
        _mouseMotionFocusIds.Remove(objectId);
        if (HoveredObjectId == objectId)
            HoveredObjectId = null;
        if (FocusedObjectId == objectId)
            FocusedObjectId = null;
        if (MovingObjectId == objectId)
            MovingObjectId = null;
        InvalidateLayout();
    }

    public void Resize(float width, float height)
    {
        _physicalWidth = Math.Max(1, width);
        _physicalHeight = Math.Max(1, height);
        if (!ApplyRootScale())
            InvalidateLayout();
    }

    public bool ConfigureUiScale(bool useUiScale, float uiScale, float uiScaleMultiplier)
    {
        _useUiScale = useUiScale;
        _uiScale = float.IsFinite(uiScale) ? uiScale : 1;
        _uiScaleMultiplier = float.IsFinite(uiScaleMultiplier) ? uiScaleMultiplier : -1;
        return ApplyRootScale();
    }

    public bool SetScreenDpiScale(float dpiScale)
    {
        var normalized = float.IsFinite(dpiScale) && dpiScale > 0 ? dpiScale : 1;
        if (MathF.Abs(_screenDpiScale - normalized) < 0.0001f)
            return false;
        _screenDpiScale = normalized;
        return ApplyRootScale();
    }

    private bool ApplyRootScale()
    {
        if (!UsesNativeScreenMetrics || Find(UiParentId) is not { } uiParent)
            return false;

        var scale = AppliedUiScale;
        var changed = MathF.Abs(uiParent.Scale - scale) >= 0.0001f;
        uiParent.Scale = scale;
        uiParent.Width = CoordinateScale * 1024 / scale;
        uiParent.Height = _physicalHeight / _physicalWidth * uiParent.Width;
        InvalidateLayout();
        return changed;
    }

    private float CalculateDefaultUiScale(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return ClampUiScaleForAspect(1);
        if (ScreenAspectRatio < 1)
            return 1;
        if (height <= 768)
            return 1;
        return ClampUiScaleForAspect(768f / height);
    }

    private float ClampUiScaleForAspect(float proposedScale)
    {
        var aspectLimit = ScreenAspectRatio >= 4f / 3f
            ? proposedScale
            : MathF.Min(ScreenAspectRatio * .75f, proposedScale);
        return Math.Clamp(aspectLimit, .64f, 1.15f);
    }

    public void Reparent(UiObject value, int? parentId)
    {
        if (value.ParentId == parentId)
            return;

        InvalidateRectValidity(value);

        if (value.ParentId is { } oldParent && Find(oldParent) is { } old)
            old.Children.Remove(value.Id);
        value.ParentId = parentId;
        if (parentId is { } newParent && Find(newParent) is { } next && !next.Children.Contains(value.Id))
        {
            next.Children.Add(value.Id);
            if (value.IsFrameWidget)
            {
                SetFrameStrata(value, EffectiveFrameStrata(next));
                ReparentFrameLevel(value, EffectiveFrameLevel(next) + 1);
            }
            SetRaisedFrameLevelRecursive(value, next.RaisedFrameLevel);
            if (next.Forbidden)
                SetForbidden(value);
        }
        else if (parentId is null)
        {
            if (value.IsFrameWidget)
            {
                SetFrameStrata(value, "MEDIUM");
                ReparentFrameLevel(value, 0);
            }
            SetRaisedFrameLevelRecursive(value, 0);
        }
        InvalidateLayout();
    }

    public void InvalidateLayout() => _layoutCache.Clear();

    public void SetForbidden(UiObject value)
    {
        if (value.Forbidden)
            return;

        value.Forbidden = true;
        foreach (var childId in value.Children)
        {
            if (Find(childId) is { } child)
                SetForbidden(child);
        }
    }

    public void SetToplevel(UiObject value, bool toplevel)
    {
        if (value.Toplevel == toplevel)
            return;

        value.Toplevel = toplevel;
    }

    public void Raise(UiObject value)
    {
        if (FindToplevelRoot(value) is not { } root ||
            root.RaisedFrameLevel == _nextRaisedFrameLevel)
        {
            return;
        }

        SetRaisedFrameLevelRecursive(root, ++_nextRaisedFrameLevel);
    }

    public void Lower(UiObject value)
    {
        if (FindToplevelRoot(value) is { } root)
            SetRaisedFrameLevelRecursive(root, 0);
    }

    private void SetRaisedFrameLevelRecursive(UiObject value, int level)
    {
        value.RaisedFrameLevel = level;
        var strata = EffectiveFrameStrata(value);
        foreach (var childId in value.Children)
        {
            if (Find(childId) is not { } child ||
                !EffectiveFrameStrata(child).Equals(strata, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SetRaisedFrameLevelRecursive(child, level);
        }
    }

    public UiRect ResolveBounds(int objectId) => ResolveBounds(objectId, []);

    public Vector2 ResolveAnchor(UiAnchor anchor, UiObject? owner = null)
    {
        var target = anchor.RelativeToId ?? UiParentId;
        var bounds = ResolveBounds(target);
        var scale = owner is null ? 1 : LayoutScale(owner);
        return Point(bounds, anchor.RelativePoint) + new Vector2(anchor.X, anchor.Y) * scale;
    }

    public Vector2 ResolveTextTopLeft(UiObject value, float measuredWidth, float measuredHeight)
    {
        var bounds = ResolveTextBounds(value);
        var fixedWidth = IsWidthConstrained(value);
        var fixedHeight = IsHeightConstrained(value);

        var x = fixedWidth
            ? value.Font?.JustifyHorizontal.ToUpperInvariant() switch
            {
                "LEFT" => bounds.Left,
                "RIGHT" => bounds.Right - measuredWidth,
                _ => bounds.Center.X - measuredWidth / 2
            }
            : bounds.Left +
              ResolveTextPositioningFactor(value, horizontal: true) *
              (bounds.Width - measuredWidth);

        var top = fixedHeight
            ? value.Font?.JustifyVertical.ToUpperInvariant() switch
            {
                "TOP" => bounds.Top,
                "BOTTOM" => bounds.Bottom + measuredHeight,
                _ => bounds.Center.Y + measuredHeight / 2
            }
            : ResolveUnconstrainedTextTop(value, bounds, measuredHeight);

        return new Vector2(x, top);
    }

    private static float ResolveUnconstrainedTextTop(
        UiObject value,
        UiRect bounds,
        float measuredHeight)
    {
        var factor = ResolveTextPositioningFactor(value, horizontal: false);
        return bounds.Bottom + factor * bounds.Height + (1 - factor) * measuredHeight;
    }

    private static float ResolveTextPositioningFactor(UiObject value, bool horizontal)
    {
        AxisConstraint? positioningConstraint = null;
        AxisConstraint? firstConstraint = null;
        foreach (var anchor in value.Anchors)
        {
            var factors = AnchorFactors(anchor.Point);
            var targetFactors = AnchorFactors(anchor.RelativePoint);
            var factor = horizontal ? factors.X : factors.Y;
            var targetFactor = horizontal ? targetFactors.X : targetFactors.Y;
            var constraint = new AxisConstraint(
                factor,
                0,
                NearlyEqual(factor, targetFactor),
                AnchorPriority(anchor.Point));
            firstConstraint ??= constraint;
            if (MathF.Abs(factor) > 0.000001f &&
                MathF.Abs(factor - 1) > 0.000001f)
            {
                continue;
            }
            if (positioningConstraint is null ||
                (!positioningConstraint.Value.FamilyAligned && constraint.FamilyAligned) ||
                (positioningConstraint.Value.FamilyAligned == constraint.FamilyAligned &&
                 constraint.Priority < positioningConstraint.Value.Priority))
            {
                positioningConstraint = constraint;
            }
        }

        return (positioningConstraint ?? firstConstraint)?.Factor ?? .5f;
    }

    public UiRect ResolveTextBounds(UiObject value)
    {
        var bounds = ResolveBounds(value.Id);
        if (!value.ObjectType.EndsWith("EditBox", StringComparison.OrdinalIgnoreCase))
            return bounds;

        var scale = LayoutScale(value);
        var left = bounds.Left + value.TextInsets.X * scale;
        var right = bounds.Right - value.TextInsets.Y * scale;
        var top = bounds.Top - value.TextInsets.Z * scale;
        var bottom = bounds.Bottom + value.TextInsets.W * scale;
        return new UiRect(
            left,
            bottom,
            Math.Max(0, right - left),
            Math.Max(0, top - bottom));
    }

    public bool IsVisible(UiObject value)
    {
        for (var current = value; ;)
        {
            if (!current.Shown)
                return false;
            if (current.ParentId is not { } parentId || Find(parentId) is not { } parent)
                return true;
            current = parent;
        }
    }

    public float EffectiveScale(UiObject value)
    {
        var result = value.Scale;
        if (value.IgnoreParentScale)
            return result;
        var parentId = value.ParentId;
        while (parentId is { } id && Find(id) is { } parent)
        {
            result *= parent.Scale;
            parentId = parent.IgnoreParentScale ? null : parent.ParentId;
        }
        return result;
    }

    public float LayoutScale(UiObject value)
    {
        var effectiveScale = EffectiveScale(value);
        if (!UsesNativeScreenMetrics)
            return effectiveScale;
        var rootScale = UiParentEffectiveScale;
        return MathF.Abs(rootScale) < 0.000001f
            ? effectiveScale
            : effectiveScale / rootScale;
    }

    private float UiParentEffectiveScale =>
        Find(UiParentId) is { } uiParent
            ? EffectiveScale(uiParent)
            : AutomaticUiScale;

    public float EffectiveAlpha(UiObject value)
    {
        return EffectiveAlphaByte(value) / 255f;
    }

    public float RenderAlpha(UiObject value, int? frameBufferOwnerId)
    {
        return EffectiveAlphaByte(value, frameBufferOwnerId) / 255f;
    }

    private byte EffectiveAlphaByte(UiObject value, int? stopBeforeParentId = null)
    {
        var localAlpha = QuantizeAlpha(value.Alpha);
        if (value.IgnoreParentAlpha ||
            value.ParentId is not { } parentId ||
            parentId == stopBeforeParentId ||
            Find(parentId) is not { } parent ||
            parent.IsFrameBuffer)
        {
            return localAlpha;
        }

        var inheritedAlpha = EffectiveAlphaByte(parent, stopBeforeParentId);
        return (byte)(localAlpha * inheritedAlpha / 255);
    }

    private static byte QuantizeAlpha(float alpha) =>
        (byte)MathF.Floor(Math.Clamp(alpha, 0, 1) * 255 + .5f);

    public bool EffectivelyFlattensRenderLayers(UiObject value)
    {
        if (value.FlattensRenderLayers ||
            value.IsFrameBuffer ||
            value.ClipsChildren ||
            value.Toplevel)
        {
            return true;
        }

        var parent = value.ParentId is { } parentId ? Find(parentId) : null;
        if (parent?.ScrollChildId == value.Id)
            return true;

        return value.WindowReference is { } window &&
               window != parent?.WindowReference;
    }

    public int HighestFrameLevel(UiObject value, bool iterateAllChildren)
    {
        var highest = EffectiveFrameLevel(value);
        foreach (var childId in value.Children)
        {
            if (Find(childId) is not { IsFrameWidget: true } child)
                continue;
            highest = Math.Max(
                highest,
                iterateAllChildren
                    ? HighestFrameLevel(child, true)
                    : EffectiveFrameLevel(child));
        }
        return highest;
    }

    public UiRect? ResolveFrameBoundsRect(UiObject value) =>
        ResolveFrameBoundsRect(value, [], includeUnresolvedRoot: false);

    public UiRect? ResolveScrollChildBoundsRect(UiObject value) =>
        ResolveFrameBoundsRect(value, [], includeUnresolvedRoot: true);

    private UiRect? ResolveFrameBoundsRect(
        UiObject value,
        HashSet<int> visited,
        bool includeUnresolvedRoot)
    {
        if (!visited.Add(value.Id))
            return null;

        UiRect? result = null;
        if (includeUnresolvedRoot || HasResolvedRect(value))
        {
            var own = ResolveBounds(value.Id);
            if (own.Width > 0 && own.Height > 0)
                result = own;
        }

        if (!value.IgnoreChildrenForBounds)
        {
            foreach (var childId in value.Children)
            {
                if (Find(childId) is not { } child)
                    continue;
                var childBounds = child.IsFrameWidget
                    ? ResolveFrameBoundsRect(
                        child,
                        visited,
                        includeUnresolvedRoot: false)
                    : HasResolvedRect(child)
                        ? ResolveBounds(child.Id)
                        : null;
                if (childBounds is not { Width: > 0, Height: > 0 } bounds)
                    continue;
                result = result is null ? bounds : Union(result.Value, bounds);
            }
        }

        return result;
    }

    private static UiRect Union(UiRect left, UiRect right)
    {
        var minimumX = Math.Min(left.Left, right.Left);
        var minimumY = Math.Min(left.Bottom, right.Bottom);
        var maximumX = Math.Max(left.Right, right.Right);
        var maximumY = Math.Max(left.Top, right.Top);
        return new UiRect(
            minimumX,
            minimumY,
            maximumX - minimumX,
            maximumY - minimumY);
    }

    public void InvalidateRectValidity(UiObject value)
    {
        if (_resolvedRectIds.Contains(value.Id))
            _resolvedRectIds.Clear();
    }

    public bool HasResolvedRect(UiObject value) => HasResolvedRect(value, []);

    private bool HasResolvedRect(UiObject value, HashSet<int> stack)
    {
        if (value.Id == UiParentId)
            return true;
        if (_resolvedRectIds.Contains(value.Id))
            return true;
        if (!stack.Add(value.Id))
            return false;

        bool resolved;
        try
        {
            if (value.Line is { Start: { } start, End: { } end })
            {
                resolved = true;
                foreach (var anchor in new[] { start, end })
                {
                    var targetId = anchor.RelativeToId ?? value.ParentId ?? UiParentId;
                    if (Find(targetId) is not { } target || !HasResolvedRect(target, stack))
                    {
                        resolved = false;
                        break;
                    }
                }
            }
            else if (value.ParentId is { } scrollParentId &&
                     Find(scrollParentId) is { } scrollParent &&
                     scrollParent.ScrollChildId == value.Id)
            {
                resolved = HasResolvedRect(scrollParent, stack);
            }
            else if (value.ParentId is { } specialParentId &&
                     Find(specialParentId) is { } specialParent &&
                     (specialParent.ThumbTextureId == value.Id ||
                      specialParent.ColorSelect is { } colorSelect &&
                      (colorSelect.WheelThumbTextureId == value.Id ||
                       colorSelect.ValueThumbTextureId == value.Id ||
                       colorSelect.AlphaThumbTextureId == value.Id)))
            {
                resolved = HasResolvedRect(specialParent, stack);
            }
            else if (value.AllPointsTargetId is { } allPointsTargetId)
            {
                resolved = Find(allPointsTargetId) is { } allPointsTarget &&
                           HasResolvedRect(allPointsTarget, stack);
            }
            else if (value.Anchors.Count == 0)
            {
                resolved = false;
            }
            else
            {
                resolved = true;
                foreach (var anchor in value.Anchors)
                {
                    var targetId = anchor.RelativeToId ?? value.ParentId ?? UiParentId;
                    if (Find(targetId) is not { } target ||
                        !HasResolvedRect(target, stack))
                    {
                        resolved = false;
                        break;
                    }
                }
            }
        }
        finally
        {
            stack.Remove(value.Id);
        }

        if (resolved)
            _resolvedRectIds.Add(value.Id);
        return resolved;
    }

    public IEnumerable<UiObject> RenderOrder() =>
        OrderedObjects(static value => value.DrawLayer);

    public IEnumerable<UiObject> VisualRenderOrder() =>
        OrderedObjects(static value => IsSimpleModel(value)
            ? value.ModelDrawLayer
            : value.DrawLayer);

    private IEnumerable<UiObject> OrderedObjects(
        Func<UiObject, string> drawLayerSelector) =>
        _objects.Values
            .Where(value =>
                IsVisible(value) &&
                HasResolvedRect(value) &&
                IsDrawLayerVisible(value))
            .OrderBy(value => StrataOrder.GetValueOrDefault(EffectiveFrameStrata(value), 3))
            .ThenBy(value => value.RaisedFrameLevel)
            .ThenBy(EffectiveFrameLevel)
            .ThenBy(value => LayerOrder.GetValueOrDefault(drawLayerSelector(value), 2))
            .ThenBy(value => value.Font is null ? value.SubLevel : 8)
            .ThenBy(value => value.IsRegion ? value.ParentId ?? value.Id : value.Id)
            .ThenBy(value => value.Id);

    private static bool IsSimpleModel(UiObject value) =>
        value.ObjectType.Equals("Model", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("PlayerModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("CharacterModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("DressUpModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("TabardModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("CinematicModel", StringComparison.OrdinalIgnoreCase);

    private bool IsDrawLayerVisible(UiObject value)
    {
        if (!value.IsRegion)
            return true;

        if (value.ParentId is { } ownerId &&
            Find(ownerId) is { } owner)
            return owner.EnabledDrawLayers.Contains(value.DrawLayer);

        return false;
    }

    private UiObject? FindToplevelRoot(UiObject value)
    {
        var current = value;
        while (true)
        {
            if (current.Toplevel)
                return current;
            if (current.ParentId is not { } parentId || Find(parentId) is not { } parent)
                return null;
            current = parent;
        }
    }

    public UiObject? HitTest(
        Vector2 point,
        bool requireClick = false,
        string? button = null) =>
        FindBestMouseFocusCandidate(RenderOrder()
            .Where(value =>
                (requireClick ? value.MouseClickEnabled : value.MouseMotionEnabled) &&
                (requireClick ||
                 !IsButtonLike(value) ||
                 value.Enabled ||
                 value.MotionScriptsWhileDisabled) &&
                (button is null || !value.ShouldButtonPassThrough(button)) &&
                !value.IsRegion &&
                HitRect(value).Contains(point) &&
                IsInsideClippingAncestors(value, point)));

    private static bool IsButtonLike(UiObject value) =>
        value.ObjectType.EndsWith("Button", StringComparison.OrdinalIgnoreCase);

    public UiObject? HitTestMouseWheel(Vector2 point) =>
        FindBestMouseFocusCandidate(RenderOrder()
            .Where(value =>
                value.MouseWheelEnabled &&
                value.ScriptReferences.ContainsKey("OnMouseWheel") &&
                !value.IsRegion &&
                HitRect(value).Contains(point) &&
                IsInsideClippingAncestors(value, point)));

    public UiObject? HitTestVisibleFrame(Vector2 point)
    {
        foreach (var region in RenderOrder().Reverse())
        {
            if (!region.IsRegion ||
                region.ObjectType.Equals("MaskTexture", StringComparison.OrdinalIgnoreCase) ||
                EffectiveAlpha(region) <= float.Epsilon ||
                !IsRenderedRegion(region) ||
                !IsInsideClippingAncestors(region, point) ||
                !IsRenderedRegionHit(region, point))
            {
                continue;
            }

            var parentId = region.ParentId;
            while (parentId is { } id && Find(id) is { } parent)
            {
                if (!parent.IsRegion)
                    return parent;
                parentId = parent.ParentId;
            }
        }

        return null;
    }

    public IReadOnlyList<UiObject> FindMouseFoci(Vector2 point)
    {
        var result = new List<UiObject>();
        var candidates = RenderOrder()
            .Where(value =>
                value.MouseMotionEnabled &&
                !value.IsRegion &&
                (!IsButtonLike(value) || value.Enabled || value.MotionScriptsWhileDisabled) &&
                HitRect(value).Contains(point) &&
                IsInsideClippingAncestors(value, point))
            .ToList();
        while (FindBestMouseFocusCandidate(candidates) is { } value)
        {
            result.Add(value);
            if (!value.PropagateMouseMotion)
                break;
            candidates.Remove(value);
        }

        return result;
    }

    private UiObject? FindBestMouseFocusCandidate(IEnumerable<UiObject> source)
    {
        var candidates = source as IReadOnlyList<UiObject> ?? source.ToArray();
        if (candidates.Count == 0)
            return null;

        var best = candidates[^1];
        while (true)
        {
            UiObject? descendant = null;
            for (var index = candidates.Count - 1; index >= 0; index--)
            {
                var candidate = candidates[index];
                if (candidate.Id != best.Id && IsDescendantOf(candidate, best))
                {
                    descendant = candidate;
                    break;
                }
            }

            if (descendant is null)
                return best;
            best = descendant;
        }
    }

    private bool IsDescendantOf(UiObject value, UiObject ancestor)
    {
        var parentId = value.ParentId;
        while (parentId is { } id && Find(id) is { } parent)
        {
            if (parent.Id == ancestor.Id)
                return true;
            parentId = parent.ParentId;
        }

        return false;
    }

    public IReadOnlyList<UiObject> MouseFoci()
    {
        if (_mouseMotionFocusIds.Count == 0 && HoveredObjectId is { } hoveredId)
            return Find(hoveredId) is { } hovered ? [hovered] : [];

        return _mouseMotionFocusIds
            .Select(Find)
            .Where(value => value is not null)
            .Cast<UiObject>()
            .ToArray();
    }

    public void SetMouseFoci(IEnumerable<UiObject> foci)
    {
        var affectedIds = _mouseMotionFocusIds.ToHashSet();
        _mouseMotionFocusIds.Clear();
        _mouseMotionFocusIds.AddRange(foci.Select(value => value.Id));
        affectedIds.UnionWith(_mouseMotionFocusIds);
        _hoveredObjectId = _mouseMotionFocusIds.Count > 0
            ? _mouseMotionFocusIds[0]
            : null;

        foreach (var id in affectedIds)
        {
            if (Find(id) is { } value)
                UpdateHighlightDrawLayer(value);
        }
    }

    public bool IsMouseMotionFocus(UiObject value) =>
        _mouseMotionFocusIds.Count > 0
            ? _mouseMotionFocusIds.Contains(value.Id)
            : HoveredObjectId == value.Id;

    public void UpdateHighlightDrawLayer(UiObject value)
    {
        var active =
            (!IsButtonLike(value) || value.Enabled) &&
            (value.HighlightLocked || IsMouseMotionFocus(value));
        if (active)
            value.EnabledDrawLayers.Add("HIGHLIGHT");
        else
            value.EnabledDrawLayers.Remove("HIGHLIGHT");
    }

    public void SetHighlightLocked(UiObject value, bool locked)
    {
        value.HighlightLocked = locked;
        UpdateHighlightDrawLayer(value);
    }

    private static bool IsRenderedRegion(UiObject value) =>
        value.Texture is { } texture &&
        (texture.IsColor ||
         texture.FileDataId is > 0 ||
         !string.IsNullOrWhiteSpace(texture.Asset) ||
         !string.IsNullOrWhiteSpace(texture.AtlasName)) ||
        value.Font is { Text.Length: > 0 } ||
        value.Line is { Start: not null, End: not null };

    private bool IsRenderedRegionHit(UiObject value, Vector2 point)
    {
        if (value.Line is not { Start: not null, End: not null } line)
            return ResolveBounds(value.Id).Contains(point);

        var quad = ResolveLineQuad(value);
        var startPoint = (quad[0] + quad[1]) * .5f;
        var endPoint = (quad[2] + quad[3]) * .5f;
        var segment = endPoint - startPoint;
        var lengthSquared = segment.LengthSquared();
        var factor = lengthSquared <= float.Epsilon
            ? 0
            : Math.Clamp(Vector2.Dot(point - startPoint, segment) / lengthSquared, 0, 1);
        var closest = startPoint + segment * factor;
        var tolerance = Math.Max(3, line.Thickness * LayoutScale(value) * .5f);
        return Vector2.DistanceSquared(point, closest) <= tolerance * tolerance;
    }

    public Vector2[] ResolveLineQuad(UiObject value)
    {
        if (value.Line is not { Start: { } startAnchor, End: { } endAnchor } line)
            return [Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero];

        var start = ResolveAnchor(startAnchor, value);
        var end = ResolveAnchor(endAnchor, value);
        var direction = end - start;
        var perpendicular = new Vector2(-direction.Y, direction.X);
        if (perpendicular.LengthSquared() > 0.00000023841858f)
            perpendicular = Vector2.Normalize(perpendicular);
        else
            perpendicular = Vector2.Zero;

        var layoutScale = LayoutScale(value);
        var halfThickness = line.Thickness * layoutScale * .5f;
        var thicknessOffset = perpendicular * halfThickness;

        Vector2[] quad =
        [
            start + thicknessOffset,
            start - thicknessOffset,
            end + thicknessOffset,
            end - thicknessOffset
        ];

        if (direction.LengthSquared() > 0.00000023841858f)
        {
            var lineDirection = Vector2.Normalize(direction);
            for (var index = 0; index < quad.Length; index++)
            {
                var localOffset = line.Texture.VertexOffsets[index] * layoutScale;
                quad[index] += new Vector2(
                    localOffset.X * lineDirection.X -
                    localOffset.Y * lineDirection.Y,
                    localOffset.X * lineDirection.Y +
                    localOffset.Y * lineDirection.X);
            }
        }

        TranslateQuad(quad, value.AnimationOffset * layoutScale);
        ApplyNativeLineTranslation(
            quad,
            value.LineAnimationOffset * layoutScale);

        var rotationOrigin = ResolveQuadTransformOrigin(
            quad,
            value.AnimationRotationOriginPoint,
            value.AnimationRotationOriginOffset * layoutScale);
        if (Math.Abs(value.AnimationRotation) > float.Epsilon)
        {
            var sine = MathF.Sin(value.AnimationRotation);
            var cosine = MathF.Cos(value.AnimationRotation);
            for (var index = 0; index < quad.Length; index++)
            {
                var relative = quad[index] - rotationOrigin;
                quad[index] = rotationOrigin + new Vector2(
                    relative.X * cosine - relative.Y * sine,
                    relative.X * sine + relative.Y * cosine);
            }
        }

        ScaleQuadScreenAxes(
            quad,
            ResolveQuadTransformOrigin(
                quad,
                value.AnimationScaleOriginPoint,
                value.AnimationScaleOriginOffset * layoutScale),
            value.AnimationScale);
        ScaleQuadLineAxes(
            quad,
            ResolveQuadTransformOrigin(
                quad,
                value.LineAnimationScaleOriginPoint,
                value.LineAnimationScaleOriginOffset * layoutScale),
            value.LineAnimationScale);
        return quad;
    }

    public IReadOnlyList<UiCooldownVertex> ResolveCooldownSwipeVertices(UiObject value)
    {
        if (value.Cooldown is not { } cooldown ||
            cooldown.DisplayDurationMilliseconds <= 0)
        {
            return [];
        }

        var progress = cooldown.CompletionBlingActive
            ? 1
            : Math.Clamp(
                cooldown.ElapsedDisplayMilliseconds /
                (float)cooldown.DisplayDurationMilliseconds,
                0,
                1);
        var start = cooldown.Reverse ? 0 : progress;
        var end = cooldown.Reverse ? progress : 1;
        if (end - start <= float.Epsilon)
            return [];

        var bounds = ResolveBounds(value.Id);
        var vertices = new List<UiCooldownVertex>(12)
        {
            new(bounds.Center, CooldownUv(cooldown, Vector2.Zero))
        };

        AddCooldownBoundaryVertex(vertices, bounds, cooldown, start);
        var firstBoundary = (int)MathF.Floor(start * 8) + 1;
        var lastBoundary = (int)MathF.Ceiling(end * 8) - 1;
        for (var boundary = firstBoundary; boundary <= lastBoundary; boundary++)
        {
            var position = boundary / 8f;
            if (position > start && position < end)
                AddCooldownBoundaryVertex(vertices, bounds, cooldown, position);
        }
        AddCooldownBoundaryVertex(vertices, bounds, cooldown, end);
        return vertices;
    }

    public UiCooldownQuad? ResolveCooldownEdgeQuad(UiObject value)
    {
        if (value.Cooldown is not { } cooldown ||
            cooldown.DisplayDurationMilliseconds <= 0)
        {
            return null;
        }

        var progress = Math.Clamp(
            cooldown.ElapsedDisplayMilliseconds /
            (float)cooldown.DisplayDurationMilliseconds,
            0,
            1);
        var angle = progress * -MathF.Tau - cooldown.Rotation;
        var reciprocalScale = 1 / MathF.Max(cooldown.EdgeScale, 0.001f);
        return ResolveCooldownQuad(
            ResolveBounds(value.Id),
            angle,
            reciprocalScale);
    }

    public UiCooldownQuad? ResolveCooldownBlingQuad(UiObject value)
    {
        if (value.Cooldown is not
            {
                CompletionBlingActive: true
            } cooldown)
        {
            return null;
        }

        var elapsed = Math.Clamp(cooldown.ElapsedDisplayMilliseconds, 0, 1_000);
        var (segment, amount) = CooldownBlingKeyframe(elapsed);
        var angle = Lerp(
            CooldownBlingAngles[segment],
            CooldownBlingAngles[segment + 1],
            amount);
        var scale = Lerp(
            CooldownBlingScales[segment],
            CooldownBlingScales[segment + 1],
            amount);
        return ResolveCooldownQuad(ResolveBounds(value.Id), angle, scale);
    }

    public float ResolveCooldownBlingAlpha(UiObject value)
    {
        if (value.Cooldown is not
            {
                CompletionBlingActive: true
            } cooldown)
        {
            return 0;
        }

        var elapsed = Math.Clamp(cooldown.ElapsedDisplayMilliseconds, 0, 1_000);
        var (segment, amount) = CooldownBlingKeyframe(elapsed);
        return Lerp(
            CooldownBlingAlphas[segment],
            CooldownBlingAlphas[segment + 1],
            amount);
    }

    private static float Lerp(float start, float end, float amount) =>
        start + (end - start) * amount;

    private static readonly float[] CooldownBlingAlphas =
        [0, 0.5f, 1, 0.75f, 0.5f, 0.25f, 0];

    private static readonly float[] CooldownBlingAngles =
    [
        0,
        -0.1303761005f,
        -0.2591814101f,
        -0.3926990926f,
        -0.5183628201f,
        -0.6518805027f,
        -0.7853981853f
    ];

    private static readonly float[] CooldownBlingScales =
    [
        1,
        0.6993007064f,
        0.5405405164f,
        0.6289308071f,
        0.7518796921f,
        0.625f,
        0.8695652485f
    ];

    private static (int Segment, float Amount) CooldownBlingKeyframe(int elapsed)
    {
        var segment = Math.Clamp((int)(elapsed * 0.006f), 0, 5);
        var amount =
            (elapsed - segment * 166.66667f) *
            0.006f;
        return (segment, Math.Clamp(amount, 0, 1));
    }

    private static UiCooldownQuad ResolveCooldownQuad(
        UiRect bounds,
        float angle,
        float scale)
    {
        var sine = MathF.Sin(angle);
        var cosine = MathF.Cos(angle);
        var halfScale = scale * 0.5f;
        var nativeUpperLeft = new Vector2(
            0.5f + (sine - cosine) * halfScale,
            0.5f - (cosine + sine) * halfScale);
        var nativeUpperRight = new Vector2(
            0.5f + (cosine + sine) * halfScale,
            0.5f + (sine - cosine) * halfScale);
        var nativeLowerLeft = new Vector2(
            0.5f - (cosine + sine) * halfScale,
            0.5f + (cosine - sine) * halfScale);
        var nativeLowerRight = new Vector2(
            0.5f + (cosine - sine) * halfScale,
            0.5f + (cosine + sine) * halfScale);

        Vector2 ToLogical(Vector2 point) =>
            new(
                bounds.Left + point.X * bounds.Width,
                bounds.Top - point.Y * bounds.Height);

        return new UiCooldownQuad(
            ToLogical(nativeUpperLeft),
            ToLogical(nativeLowerLeft),
            ToLogical(nativeUpperRight),
            ToLogical(nativeLowerRight));
    }

    private static void AddCooldownBoundaryVertex(
        ICollection<UiCooldownVertex> vertices,
        UiRect bounds,
        UiCooldownState cooldown,
        float progress)
    {
        var radians = progress * MathF.Tau;
        var direction = new Vector2(MathF.Sin(radians), MathF.Cos(radians));
        var divisor = MathF.Max(MathF.Abs(direction.X), MathF.Abs(direction.Y));
        var normalizedOffset = divisor <= float.Epsilon
            ? new Vector2(0, 0.5f)
            : direction * (0.5f / divisor);
        var rotatedOffset = RotateCooldownOffset(normalizedOffset, cooldown.Rotation);
        var position = bounds.Center + new Vector2(
            rotatedOffset.X * bounds.Width,
            rotatedOffset.Y * bounds.Height);
        vertices.Add(new UiCooldownVertex(
            position,
            CooldownUv(cooldown, normalizedOffset)));
    }

    private static Vector2 CooldownUv(UiCooldownState cooldown, Vector2 uiOffset)
    {
        var textureOffset = new Vector2(uiOffset.X, -uiOffset.Y);
        textureOffset = RotateCooldownOffset(textureOffset, cooldown.Rotation);
        var normalized = textureOffset + new Vector2(0.5f);
        return new Vector2(
            cooldown.TextureCoordinateLow.X +
            (cooldown.TextureCoordinateHigh.X - cooldown.TextureCoordinateLow.X) *
            normalized.X,
            cooldown.TextureCoordinateLow.Y +
            (cooldown.TextureCoordinateHigh.Y - cooldown.TextureCoordinateLow.Y) *
            normalized.Y);
    }

    private static Vector2 RotateCooldownOffset(Vector2 offset, float radians)
    {
        if (Math.Abs(radians) <= float.Epsilon)
            return offset;
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);
        return new Vector2(
            offset.X * cosine - offset.Y * sine,
            offset.X * sine + offset.Y * cosine);
    }

    private static void TranslateQuad(Vector2[] quad, Vector2 offset)
    {
        for (var index = 0; index < quad.Length; index++)
            quad[index] += offset;
    }

    private static void ApplyNativeLineTranslation(
        Vector2[] quad,
        Vector2 requestedOffset)
    {
        var magnitude = requestedOffset.Length();
        if (magnitude <= 0)
            return;

        var edge = quad[0] - quad[2];
        var transformedX =
            (edge.X * requestedOffset.X + edge.Y * requestedOffset.Y) * edge.X;
        var transformedY =
            (edge.Y * requestedOffset.X - edge.X * requestedOffset.Y) * edge.X;
        var transformedLength = MathF.Sqrt(
            transformedX * transformedX + transformedY * transformedY);
        if (transformedLength <= 0)
            return;

        var factor = magnitude / transformedLength;
        TranslateQuad(
            quad,
            new Vector2(transformedX * factor, transformedY * factor));
    }

    private static void ScaleQuadScreenAxes(
        Vector2[] quad,
        Vector2 origin,
        Vector2 scale)
    {
        for (var index = 0; index < quad.Length; index++)
        {
            var relative = quad[index] - origin;
            quad[index] = origin + relative * scale;
        }
    }

    private static void ScaleQuadLineAxes(
        Vector2[] quad,
        Vector2 origin,
        Vector2 scale)
    {
        var direction = quad[2] - quad[0];
        if (direction.LengthSquared() <= 0.00000023841858f)
            return;

        direction = Vector2.Normalize(direction);
        var perpendicular = new Vector2(-direction.Y, direction.X);
        for (var index = 0; index < quad.Length; index++)
        {
            var relative = quad[index] - origin;
            var local = new Vector2(
                Vector2.Dot(relative, direction),
                Vector2.Dot(relative, perpendicular));
            local *= scale;
            quad[index] = origin +
                          direction * local.X +
                          perpendicular * local.Y;
        }
    }

    private static Vector2 ResolveQuadTransformOrigin(
        IReadOnlyList<Vector2> quad,
        string point,
        Vector2 offset)
    {
        var origin = point.ToUpperInvariant() switch
        {
            "TOPLEFT" => quad[0],
            "TOP" => (quad[0] + quad[2]) * .5f,
            "TOPRIGHT" => quad[2],
            "LEFT" => (quad[0] + quad[1]) * .5f,
            "RIGHT" => (quad[2] + quad[3]) * .5f,
            "BOTTOMLEFT" => quad[1],
            "BOTTOM" => (quad[1] + quad[3]) * .5f,
            "BOTTOMRIGHT" => quad[3],
            _ => (quad[0] + quad[3]) * .5f
        };
        if (offset.LengthSquared() <= 0.00000011920929f)
            return origin;

        var horizontal = quad[3] - quad[1];
        if (horizontal.LengthSquared() > 0.00000023841858f)
            horizontal = Vector2.Normalize(horizontal);
        return origin +
               horizontal * offset.X +
               new Vector2(-horizontal.Y, horizontal.X) * offset.Y;
    }

    public bool IsMouseOver(
        UiObject value,
        Vector2 point,
        float offsetTop = 0,
        float offsetBottom = 0,
        float offsetLeft = 0,
        float offsetRight = 0)
    {
        if (!IsVisible(value))
            return false;

        var bounds = ResolveBounds(value.Id);
        var scale = LayoutScale(value);
        var left = bounds.Left + offsetLeft * scale;
        var right = bounds.Right + offsetRight * scale;
        var bottom = bounds.Bottom + offsetBottom * scale;
        var top = bounds.Top + offsetTop * scale;
        return point.X > Math.Min(left, right) &&
               point.X < Math.Max(left, right) &&
               point.Y > Math.Min(bottom, top) &&
               point.Y < Math.Max(bottom, top);
    }

    public IReadOnlyList<object> SnapshotTree()
    {
        return _objects.Values.Select(value => (object)new
        {
            value.Id,
            value.Name,
            value.ObjectType,
            value.ParentId,
            value.Children,
            value.Shown,
            value.Enabled,
            Visible = IsVisible(value) && HasResolvedRect(value),
            Bounds = ResolveBounds(value.Id),
            FrameStrata = EffectiveFrameStrata(value),
            FrameLevel = EffectiveFrameLevel(value),
            value.Toplevel,
            ToplevelRootId = FindToplevelRoot(value)?.Id,
            value.RaisedFrameLevel,
            value.DrawLayer,
            value.SubLevel,
            value.MouseEnabled,
            value.MouseClickEnabled,
            value.MouseMotionEnabled,
            value.MouseWheelEnabled,
            value.KeyboardEnabled,
            value.ClipsChildren,
            value.ScrollChildId,
            value.HorizontalScroll,
            value.HorizontalScrollRange,
            value.VerticalScroll,
            value.VerticalScrollRange,
            Scripts = value.ScriptReferences.Keys.Order().ToArray(),
            Events = value.Events.Order().ToArray(),
            Texture = value.Texture?.Asset,
            Text = value.Font?.Text ?? value.TextValue
        }).ToArray();
    }

    private UiRect HitRect(UiObject value)
    {
        var bounds = ResolveBounds(value.Id);
        var scale = LayoutScale(value);
        var insets = value.HitRectInsets;
        var left = bounds.Left + insets.Left * scale;
        var right = bounds.Right - insets.Right * scale;
        var bottom = bounds.Bottom + insets.Bottom * scale;
        var top = bounds.Top - insets.Top * scale;
        return new UiRect(left, bottom, Math.Max(0, right - left), Math.Max(0, top - bottom));
    }

    private UiRect ResolveBounds(int objectId, HashSet<int> stack)
    {
        if (_layoutCache.TryGetValue(objectId, out var cached))
            return cached;
        if (!stack.Add(objectId))
            return new UiRect(0, 0, 0, 0);

        var value = Get(objectId);
        if (objectId == UiParentId)
        {
            var root = new UiRect(0, 0, LogicalWidth, LogicalHeight);
            _layoutCache[objectId] = root;
            stack.Remove(objectId);
            return root;
        }

        if (TryResolveSliderThumbBounds(value, stack, out var thumbBounds))
        {
            thumbBounds = ApplyAnimationTransform(value, thumbBounds);
            _layoutCache[objectId] = thumbBounds;
            stack.Remove(objectId);
            return thumbBounds;
        }

        if (TryResolveColorSelectThumbBounds(value, stack, out var colorThumbBounds))
        {
            colorThumbBounds = ApplyAnimationTransform(value, colorThumbBounds);
            _layoutCache[objectId] = colorThumbBounds;
            stack.Remove(objectId);
            return colorThumbBounds;
        }

        if (value.Line is { Start: { } startAnchor, End: { } endAnchor })
        {
            var quad = ResolveLineQuad(value);
            var left = quad.Min(point => point.X);
            var right = quad.Max(point => point.X);
            var bottom = quad.Min(point => point.Y);
            var top = quad.Max(point => point.Y);
            var lineBounds = new UiRect(
                left,
                bottom,
                right - left,
                top - bottom);
            _layoutCache[objectId] = lineBounds;
            stack.Remove(objectId);
            return lineBounds;
        }

        if (value.AllPointsTargetId is { } target)
        {
            var targetBounds = ResolveBounds(target, stack);
            targetBounds = ApplyScrollChildTransform(value, targetBounds, stack);
            targetBounds = ApplyScreenClamp(value, targetBounds);
            targetBounds = ApplyAnimationTransform(value, targetBounds);
            _layoutCache[objectId] = targetBounds;
            stack.Remove(objectId);
            return targetBounds;
        }

        var effectiveScale = LayoutScale(value);
        var intrinsic = value.Font is not null
            ? MeasureText(value)
            : value.Texture is { AtlasName: null } texture
                ? new Vector2(
                    texture.IntrinsicWidth.GetValueOrDefault(),
                    texture.IntrinsicHeight.GetValueOrDefault())
                : Vector2.Zero;
        intrinsic *= effectiveScale;
        var width = value.Width is { } explicitWidth &&
                    !(value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) &&
                      explicitWidth == 0)
            ? explicitWidth * effectiveScale
            : intrinsic.X;
        var explicitHeight = value.Height.GetValueOrDefault();
        var hasExplicitHeight = value.Height.HasValue &&
                                !(value.ObjectType.Equals(
                                      "FontString",
                                      StringComparison.OrdinalIgnoreCase) &&
                                  explicitHeight == 0);
        var height = hasExplicitHeight
            ? explicitHeight * effectiveScale
            : intrinsic.Y;
        var horizontalConstraints = new List<AxisConstraint>(value.Anchors.Count);
        var verticalConstraints = new List<AxisConstraint>(value.Anchors.Count);

        foreach (var anchor in value.Anchors)
        {
            var targetId = anchor.RelativeToId ?? value.ParentId ?? UiParentId;
            var targetBounds = ResolveBounds(targetId, stack);
            var targetPoint = Point(targetBounds, anchor.RelativePoint) +
                              new Vector2(anchor.X, anchor.Y) * effectiveScale;
            var factors = AnchorFactors(anchor.Point);
            var targetFactors = AnchorFactors(anchor.RelativePoint);
            horizontalConstraints.Add(new AxisConstraint(
                factors.X,
                targetPoint.X,
                NearlyEqual(factors.X, targetFactors.X),
                AnchorPriority(anchor.Point)));
            verticalConstraints.Add(new AxisConstraint(
                factors.Y,
                targetPoint.Y,
                NearlyEqual(factors.Y, targetFactors.Y),
                AnchorPriority(anchor.Point)));
        }

        var horizontal = ResolveAxis(horizontalConstraints, width);
        if (!hasExplicitHeight &&
            value.Font is { WordWrap: true } wrappedFont &&
            horizontal.Extent > 0 &&
            effectiveScale > 0)
        {
            height = MeasureText(value, horizontal.Extent / effectiveScale).Y *
                     effectiveScale;
        }
        var vertical = ResolveAxis(verticalConstraints, height);

        var result = new UiRect(
            horizontal.Origin,
            vertical.Origin,
            horizontal.Extent,
            vertical.Extent);
        result = ApplyScrollChildTransform(value, result, stack);
        result = ApplyScreenClamp(value, result);
        result = ApplyAnimationTransform(value, result);
        _layoutCache[objectId] = result;
        stack.Remove(objectId);
        return result;
    }

    private UiRect ApplyAnimationTransform(UiObject value, UiRect bounds)
    {
        var effectiveScale = LayoutScale(value);
        var offset = value.AnimationOffset * effectiveScale;
        var scale = value.AnimationScale;
        var translated = new UiRect(
            bounds.Left + offset.X,
            bounds.Bottom + offset.Y,
            bounds.Width,
            bounds.Height);
        var origin = ResolveTransformOrigin(
            value,
            translated,
            value.AnimationScaleOriginPoint,
            value.AnimationScaleOriginOffset);
        var left = origin.X + (translated.Left - origin.X) * scale.X;
        var bottom = origin.Y + (translated.Bottom - origin.Y) * scale.Y;
        return new UiRect(
            left,
            bottom,
            translated.Width * scale.X,
            translated.Height * scale.Y);
    }

    public Vector2 ResolveTransformOrigin(
        UiObject value,
        UiRect bounds,
        string point,
        Vector2 offset) =>
        Point(bounds, point) + offset * LayoutScale(value);

    private bool TryResolveSliderThumbBounds(
        UiObject value,
        HashSet<int> stack,
        out UiRect bounds)
    {
        bounds = default;
        if (value.ParentId is not { } parentId ||
            Find(parentId) is not { } parent ||
            !parent.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase) ||
            parent.ThumbTextureId != value.Id)
        {
            return false;
        }

        var track = ResolveBounds(parentId, stack);
        var scale = LayoutScale(value);
        var width = Math.Max(0, (value.Width ?? 0) * scale);
        var height = Math.Max(0, (value.Height ?? 0) * scale);
        var state = parent.StatusBar;
        if (state is not { RangeInitialized: true, ValueInitialized: true })
            return false;
        var minimum = state?.Minimum ?? 0;
        var maximum = state?.Maximum ?? 1;
        var range = maximum - minimum;
        var ratio = Math.Abs(range) < double.Epsilon
            ? 0
            : Math.Clamp(((state?.Value ?? minimum) - minimum) / range, 0, 1);

        if (state?.Orientation.Equals("VERTICAL", StringComparison.OrdinalIgnoreCase) == true)
        {
            bounds = new UiRect(
                track.Left + (track.Width - width) / 2,
                track.Bottom +
                (float)((1 - ratio) * Math.Max(0, track.Height - height)),
                width,
                height);
        }
        else
        {
            bounds = new UiRect(
                track.Left + (float)(ratio * Math.Max(0, track.Width - width)),
                track.Bottom + (track.Height - height) / 2,
                width,
                height);
        }

        bounds = ApplyScrollChildTransform(value, bounds, stack);
        bounds = ApplyScreenClamp(value, bounds);
        return true;
    }

    private bool TryResolveColorSelectThumbBounds(
        UiObject value,
        HashSet<int> stack,
        out UiRect bounds)
    {
        bounds = default;
        if (value.ParentId is not { } parentId ||
            Find(parentId) is not { ColorSelect: { } colorSelect } parent ||
            colorSelect.WheelTextureId is not { } wheelId)
        {
            return false;
        }

        var role = value.Id == colorSelect.WheelThumbTextureId
            ? 1
            : value.Id == colorSelect.ValueThumbTextureId
                ? 2
                : value.Id == colorSelect.AlphaThumbTextureId
                    ? 3
                    : 0;
        if (role == 0)
            return false;

        var wheel = ResolveBounds(wheelId, stack);
        var scale = LayoutScale(value);
        var width = Math.Max(0, (value.Width ?? 0) * scale);
        var height = Math.Max(0, (value.Height ?? 0) * scale);
        Vector2 center;
        if (role == 1)
        {
            var radians = colorSelect.Hue * MathF.PI / 180;
            var radius = wheel.Width * 0.5f * colorSelect.Saturation;
            center = wheel.Center + new Vector2(
                -MathF.Cos(radians) * radius,
                -MathF.Sin(radians) * radius);
        }
        else
        {
            var trackId = role == 2
                ? colorSelect.ValueTextureId
                : colorSelect.AlphaTextureId;
            if (trackId is not { } resolvedTrackId)
                return false;
            var track = ResolveBounds(resolvedTrackId, stack);
            var fraction = role == 2 ? colorSelect.Value : colorSelect.Alpha;
            center = new Vector2(
                track.Center.X,
                track.Bottom + wheel.Width * fraction);
        }

        bounds = new UiRect(
            center.X - width * 0.5f,
            center.Y - height * 0.5f,
            width,
            height);
        bounds = ApplyScrollChildTransform(value, bounds, stack);
        bounds = ApplyScreenClamp(value, bounds);
        return true;
    }

    private UiRect ApplyScrollChildTransform(UiObject value, UiRect bounds, HashSet<int> stack)
    {
        if (value.ParentId is not { } parentId ||
            Find(parentId) is not { } parent ||
            parent.ScrollChildId != value.Id)
            return bounds;

        var viewport = ResolveBounds(parentId, stack);
        if (value.Anchors.Count == 0 && value.AllPointsTargetId is null)
            bounds = new UiRect(viewport.Left, viewport.Top - bounds.Height, bounds.Width, bounds.Height);

        return new UiRect(
            bounds.Left - parent.HorizontalScroll * LayoutScale(parent),
            bounds.Bottom + parent.VerticalScroll * LayoutScale(parent),
            bounds.Width,
            bounds.Height);
    }

    private UiRect ApplyScreenClamp(UiObject value, UiRect bounds)
    {
        if (!value.ClampedToScreen || value.Id == UiParentId)
            return bounds;

        var scale = LayoutScale(value);
        var insets = value.ClampRectInsets * scale;
        var minimumLeft = insets.X;
        var maximumRight = LogicalWidth - insets.Y;
        var maximumTop = LogicalHeight - insets.Z;
        var minimumBottom = insets.W;

        var x = bounds.Left;
        if (bounds.Width <= maximumRight - minimumLeft)
            x = Math.Clamp(x, minimumLeft, maximumRight - bounds.Width);
        else
            x = minimumLeft;

        var bottom = bounds.Bottom;
        if (bounds.Height <= maximumTop - minimumBottom)
            bottom = Math.Clamp(bottom, minimumBottom, maximumTop - bounds.Height);
        else
            bottom = minimumBottom;

        return bounds with { Left = x, Bottom = bottom };
    }

    private bool IsInsideClippingAncestors(UiObject value, Vector2 point)
    {
        var child = value;
        var parentId = value.ParentId;
        while (parentId is { } id && Find(id) is { } parent)
        {
            if ((parent.ClipsChildren || parent.ScrollChildId == child.Id) &&
                !ResolveBounds(parent.Id).Contains(point))
            {
                return false;
            }
            child = parent;
            parentId = parent.ParentId;
        }
        return true;
    }

    private Vector2 MeasureText(UiObject value, float? maximumWidth = null)
    {
        var font = value.Font!;
        if (font.Text.Length == 0)
            return Vector2.Zero;

        var lines = WowTextMarkup.PlainText(font.Text)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
        var lineHeight = UiTextLineMetrics.ResolveLogicalLineHeight(
            font.FontSize,
            font.TextScale,
            _physicalHeight,
            EffectiveScale(value),
            value.FontSmoothScaling);
        if (!(lineHeight > 0))
            return Vector2.Zero;

        var positiveShadowWidth = UiTextLineMetrics.ResolveLogicalPositiveShadowWidth(
            font.ShadowOffset.X,
            _physicalHeight,
            EffectiveScale(value));
        var constrainedWidth = font.WordWrap && maximumWidth is > 0
            ? MathF.Max(0, maximumWidth.Value - positiveShadowWidth)
            : float.PositiveInfinity;
        var indentWidth = font.IndentedWordWrap
            ? UiTextLineMetrics.ResolveLogicalIndentedWordWrapWidth(
                _physicalHeight,
                EffectiveScale(value))
            : 0;
        var measuredLines = UiMeasuredTextWrapping.Wrap(
                string.Join('\n', lines),
                constrainedWidth,
                float.IsFinite(constrainedWidth)
                    ? MathF.Max(0, constrainedWidth - indentWidth)
                    : float.PositiveInfinity,
                font.NonSpaceWrap,
                line => MeasureTextAdvance(font, line, lineHeight))
            .ToList();
        if (font.MaximumLines > 0 && measuredLines.Count > font.MaximumLines)
            measuredLines.RemoveRange(font.MaximumLines, measuredLines.Count - font.MaximumLines);
        var lineCount = measuredLines.Count;
        var spacing = UiTextLineMetrics.ResolveLogicalSpacing(
            font.Spacing,
            _physicalHeight,
            EffectiveScale(value));
        return new Vector2(
            measuredLines.Select(line => MeasureTextAdvance(font, line, lineHeight))
                .DefaultIfEmpty(0)
                .Max() + positiveShadowWidth,
            lineCount * lineHeight + Math.Max(0, lineCount - 1) * spacing);
    }

    public static bool IsWidthConstrained(UiObject value)
    {
        if (value.AllPointsTargetId.HasValue)
            return true;
        var hasLeft = value.Anchors.Any(anchor =>
            anchor.Point.Contains("LEFT", StringComparison.OrdinalIgnoreCase));
        var hasRight = value.Anchors.Any(anchor =>
            anchor.Point.Contains("RIGHT", StringComparison.OrdinalIgnoreCase));
        if (hasLeft && hasRight)
            return true;
        return value.Width.HasValue &&
               (!value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) ||
                value.Width != 0);
    }

    public static bool IsHeightConstrained(UiObject value)
    {
        if (value.AllPointsTargetId.HasValue)
            return true;
        var hasTop = value.Anchors.Any(anchor =>
            anchor.Point.Contains("TOP", StringComparison.OrdinalIgnoreCase));
        var hasBottom = value.Anchors.Any(anchor =>
            anchor.Point.Contains("BOTTOM", StringComparison.OrdinalIgnoreCase));
        if (hasTop && hasBottom)
            return true;
        return value.Height.HasValue &&
               (!value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) ||
                value.Height != 0);
    }

    private static Vector2 Point(UiRect rect, string point) => point.ToUpperInvariant() switch
    {
        "TOPLEFT" => new(rect.Left, rect.Top),
        "TOP" => new(rect.Center.X, rect.Top),
        "TOPRIGHT" => new(rect.Right, rect.Top),
        "LEFT" => new(rect.Left, rect.Center.Y),
        "CENTER" => rect.Center,
        "RIGHT" => new(rect.Right, rect.Center.Y),
        "BOTTOMLEFT" => new(rect.Left, rect.Bottom),
        "BOTTOM" => new(rect.Center.X, rect.Bottom),
        "BOTTOMRIGHT" => new(rect.Right, rect.Bottom),
        _ => rect.Center
    };

    private static Vector2 AnchorFactors(string point) => point.ToUpperInvariant() switch
    {
        "TOPLEFT" => new(0, 1),
        "TOP" => new(.5f, 1),
        "TOPRIGHT" => new(1, 1),
        "LEFT" => new(0, .5f),
        "RIGHT" => new(1, .5f),
        "BOTTOMLEFT" => new(0, 0),
        "BOTTOM" => new(.5f, 0),
        "BOTTOMRIGHT" => new(1, 0),
        _ => new(.5f, .5f)
    };

    private static int AnchorPriority(string point) => point.ToUpperInvariant() switch
    {
        "TOPLEFT" => 0,
        "TOPRIGHT" => 1,
        "BOTTOMLEFT" => 2,
        "BOTTOMRIGHT" => 3,
        "TOP" => 4,
        "BOTTOM" => 5,
        "LEFT" => 6,
        "RIGHT" => 7,
        _ => 8
    };

    private static ResolvedAxis ResolveAxis(
        IReadOnlyList<AxisConstraint> constraints,
        float fallbackExtent)
    {
        if (constraints.Count == 0)
            return new ResolvedAxis(0, Math.Max(0, fallbackExtent));

        AxisConstraint? lowerEdge = null;
        AxisConstraint? upperEdge = null;
        foreach (var constraint in constraints)
        {
            if (MathF.Abs(constraint.Factor) <= 0.000001f &&
                (lowerEdge is null || constraint.Priority < lowerEdge.Value.Priority))
                lowerEdge = constraint;
            else if (MathF.Abs(constraint.Factor - 1) <= 0.000001f &&
                     (upperEdge is null || constraint.Priority < upperEdge.Value.Priority))
                upperEdge = constraint;
        }

        if (lowerEdge is { } lower && upperEdge is { } upper)
        {
            var extent = Math.Max(0, upper.Position - lower.Position);
            return new ResolvedAxis(
                lower.Position,
                extent);
        }

        var extentFallback = Math.Max(0, fallbackExtent);
        AxisConstraint? positioningConstraint = null;
        foreach (var constraint in constraints)
        {
            if (MathF.Abs(constraint.Factor) > 0.000001f &&
                MathF.Abs(constraint.Factor - 1) > 0.000001f)
                continue;
            if (positioningConstraint is null ||
                (!positioningConstraint.Value.FamilyAligned && constraint.FamilyAligned) ||
                (positioningConstraint.Value.FamilyAligned == constraint.FamilyAligned &&
                 constraint.Priority < positioningConstraint.Value.Priority))
                positioningConstraint = constraint;
        }

        var position = positioningConstraint ?? constraints[0];
        return new ResolvedAxis(
            position.Position - position.Factor * extentFallback,
            extentFallback);
    }

    private static bool NearlyEqual(float left, float right) =>
        MathF.Abs(left - right) <= 0.000001f;

    private readonly record struct AxisConstraint(
        float Factor,
        float Position,
        bool FamilyAligned,
        int Priority);
    private readonly record struct ResolvedAxis(float Origin, float Extent);

    public string EffectiveFrameStrata(UiObject value)
    {
        for (var current = value; ;)
        {
            if (!string.IsNullOrWhiteSpace(current.FrameStrata))
                return current.FrameStrata;
            if (current.ParentId is not { } parentId || Find(parentId) is not { } parent)
                return "MEDIUM";
            current = parent;
        }
    }

    public void SetFrameStrata(UiObject value, string frameStrata)
    {
        if (value.FixedFrameStrata ||
            value.FrameStrata.Equals(frameStrata, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        value.FrameStrata = frameStrata;
        foreach (var childId in value.Children)
        {
            if (Find(childId) is { IsFrameWidget: true } child)
                SetFrameStrata(child, frameStrata);
        }
    }

    public int EffectiveFrameLevel(UiObject value)
    {
        if (value.IsRegion)
            return value.ParentId is { } regionParentId && Find(regionParentId) is { } regionParent
                ? EffectiveFrameLevel(regionParent)
                : 0;
        return value.FrameLevel;
    }

    public void SetFrameLevel(UiObject value, int frameLevel)
    {
        if (value.FixedFrameLevel)
            return;

        var oldFrameLevel = value.FrameLevel;
        var nextFrameLevel =
            value.UseParentLevel &&
            value.ParentId is { } parentId &&
            Find(parentId) is { } parent
                ? parent.FrameLevel
                : Math.Clamp(frameLevel, 0, 10_000);
        if (oldFrameLevel == nextFrameLevel)
            return;

        var children = value.Children
            .Select(Find)
            .Where(child =>
                child is { IsFrameWidget: true } &&
                EffectiveFrameStrata(child).Equals(
                    EffectiveFrameStrata(value),
                    StringComparison.OrdinalIgnoreCase))
            .Select(child => (Value: child!, OldFrameLevel: child!.FrameLevel))
            .ToArray();

        value.FrameLevel = nextFrameLevel;
        foreach (var (child, oldChildFrameLevel) in children)
        {
            SetFrameLevel(
                child,
                nextFrameLevel + oldChildFrameLevel - oldFrameLevel);
        }
    }

    private void ReparentFrameLevel(UiObject value, int frameLevel)
    {
        SetFrameLevel(value, frameLevel);
    }

    public void SetFixedFrameLevel(UiObject value, bool isFixed)
    {
        value.FixedFrameLevel = isFixed;
    }

    public void SetUseParentLevel(UiObject value, bool useParentLevel)
    {
        if (value.UseParentLevel == useParentLevel)
            return;

        if (useParentLevel)
        {
            if (value.ParentId is { } parentId && Find(parentId) is { } parent)
                SetFrameLevel(value, parent.FrameLevel);
            value.UseParentLevel = true;
            return;
        }

        value.UseParentLevel = false;
        if (value.ParentId is { } nextParentId && Find(nextParentId) is { } nextParent)
            SetFrameLevel(value, nextParent.FrameLevel + 1);
    }

    public void SetFixedFrameStrata(UiObject value, bool isFixed)
    {
        value.FixedFrameStrata = isFixed;
    }
}
