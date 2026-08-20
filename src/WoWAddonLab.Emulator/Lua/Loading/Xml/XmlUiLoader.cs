using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using WoWAddonLab.Emulator.Addons;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class XmlUiLoader
{
    private static readonly HashSet<string> ObjectElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "Frame", "AuraContainer", "AuraButton",
        "Browser", "Button", "CheckButton", "EditBox", "ScrollFrame", "Slider",
        "StatusBar", "GameTooltip", "MessageFrame", "ScrollingMessageFrame",
        "SimpleHTML", "Model", "PlayerModel", "DressUpModel", "TabardModel",
        "ModelScene", "Actor", "Cooldown", "ColorSelect", "Minimap", "WorldFrame",
        "Checkout", "CinematicModel", "ContainedAlertFrame", "DropdownButton",
        "DropDownToggleButton", "EventButton", "EventEditBox", "EventFrame",
        "EventScrollFrame", "FlipBook", "FogOfWarFrame", "ItemButton", "MovieFrame",
        "QuestPOIFrame", "ScenarioPOIFrame", "UIThemeContainerFrame", "UnitPositionFrame",
        "FontString", "Texture", "MaskTexture", "Line", "Font", "BarTexture", "NormalTexture",
        "PushedTexture", "DisabledTexture", "HighlightTexture", "CheckedTexture",
        "DisabledCheckedTexture", "ThumbTexture", "BlingTexture", "ColorAlphaTexture",
        "ColorAlphaThumbTexture", "ColorValueTexture", "ColorValueThumbTexture",
        "ColorWheelTexture", "ColorWheelThumbTexture", "EdgeTexture", "MaskedTexture",
        "SwipeTexture", "ButtonText", "TitleRegion", "AnimationGroup",
        "Animation", "Alpha", "Translation", "LineTranslation", "Rotation", "Scale",
        "LineScale", "VertexColor", "Path", "ControlPoint"
    };

    private static readonly HashSet<string> NativeIntrinsicObjectTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AuraContainer", "AuraButton"
    };

    private readonly LuaRuntime _runtime;
    private readonly HashSet<int> _nativeIntrinsicLoads = [];
    private readonly Dictionary<string, XmlTemplate> _templates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, XmlTemplate> _intrinsics =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(int OwnerId, string Key), int> _objectKeys = new();
    private readonly Dictionary<(int OwnerId, string Script), XmlScriptChain> _scriptChains = new();
    private readonly List<PendingMaskLink> _pendingMaskLinks = [];
    private readonly Dictionary<int, bool> _pendingInitialShownStates = [];

    public XmlUiLoader(LuaRuntime runtime)
    {
        _runtime = runtime;
    }

    public bool TryGetTemplateInfo(string name, out XmlTemplateInfo info)
    {
        if (!_templates.TryGetValue(name, out var template))
        {
            info = null!;
            return false;
        }

        var (_, width, height) = ResolveTemplateSize(
            template,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            0);
        var keyValues = template.Element.Elements()
            .Where(child => child.Name.LocalName.Equals("KeyValues", StringComparison.OrdinalIgnoreCase))
            .SelectMany(child => child.Elements())
            .Where(child => child.Name.LocalName.Equals("KeyValue", StringComparison.OrdinalIgnoreCase))
            .Select(child => new XmlTemplateKeyValueInfo(
                Attribute(child, "key") ?? string.Empty,
                Attribute(child, "keyType") ?? "string",
                Attribute(child, "type") ?? "string",
                Attribute(child, "value") ?? string.Empty))
            .ToArray();
        info = new XmlTemplateInfo(
            NormalizeObjectType(template.Element.Name.LocalName),
            width,
            height,
            keyValues,
            Attribute(template.Element, "inherits"),
            template.SourcePath);
        return true;
    }

    private (bool Found, float Width, float Height) ResolveTemplateSize(
        XmlTemplate template,
        HashSet<string> inheritanceStack,
        int depth)
    {
        if (depth > 64)
            throw new InvalidDataException(
                $"XML template inheritance is too deep in {template.SourcePath}.");

        var found = false;
        var width = 0f;
        var height = 0f;
        foreach (var inheritedName in SplitList(Attribute(template.Element, "inherits")))
        {
            if (!_templates.TryGetValue(inheritedName, out var inherited) ||
                !inheritanceStack.Add(inheritedName))
            {
                continue;
            }
            var inheritedSize = ResolveTemplateSize(inherited, inheritanceStack, depth + 1);
            inheritanceStack.Remove(inheritedName);
            if (!inheritedSize.Found)
                continue;
            found = true;
            width = inheritedSize.Width;
            height = inheritedSize.Height;
        }

        var size = template.Element.Elements().FirstOrDefault(
            child => child.Name.LocalName.Equals("Size", StringComparison.OrdinalIgnoreCase));
        if (size is not null)
        {
            found = true;
            width = NumberAttribute(size, "x");
            height = NumberAttribute(size, "y");
        }
        return (found, width, height);
    }

    public void ProcessTopLevel(XElement element, AddonManifest manifest, string sourcePath)
    {
        if (element.Name.LocalName.Equals("FontFamily", StringComparison.OrdinalIgnoreCase))
        {
            ProcessFontFamily(element, manifest, sourcePath);
            return;
        }

        if (!IsObjectElement(element.Name.LocalName))
        {
            if (element.Name.LocalName.Equals("ScopedModifier", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var child in element.Elements())
                    ProcessTopLevel(child, manifest, sourcePath);
            }
            return;
        }

        var objectWatermark = _runtime.Ui.LastObjectId;
        var templateName = Attribute(element, "name");
        if (BooleanAttribute(element, "intrinsic") && !string.IsNullOrWhiteSpace(templateName))
        {
            _intrinsics[templateName] = new XmlTemplate(
                new XElement(element),
                sourcePath,
                _runtime.CurrentAddonEnvironmentReference);
            return;
        }

        if (BooleanAttribute(element, "virtual") && !string.IsNullOrWhiteSpace(templateName))
        {
            _templates[templateName] = new XmlTemplate(
                new XElement(element),
                sourcePath,
                _runtime.CurrentAddonEnvironmentReference);
            if (element.Name.LocalName.Equals("Font", StringComparison.OrdinalIgnoreCase))
            {
                Instantiate(element, null, manifest, sourcePath, null, 0);
                var created = ObjectsCreatedSince(objectWatermark);
                ApplyInitialVisibility(created);
                InvokeInitialOnLoad(created);
                InvokeInitialVisibilityScripts(created);
            }
            return;
        }

        var parentName = ResolveInheritedAttribute(
            element,
            "parent",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var parent = string.IsNullOrWhiteSpace(parentName)
            ? null
            : _runtime.Ui.Find(parentName);
        if (!string.IsNullOrWhiteSpace(templateName) &&
            _runtime.Ui.Find(templateName) is { } existing)
        {
            ApplyDefinition(
                existing,
                element,
                manifest,
                sourcePath,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                0,
                environmentReference: _runtime.CurrentAddonEnvironmentReference);
            ResolvePendingMaskLinks();
            var createdWithExisting = ObjectsCreatedSince(objectWatermark, existing);
            ApplyInitialVisibility(createdWithExisting);
            InvokeInitialOnLoad(createdWithExisting);
            InvokeInitialVisibilityScripts(ObjectsCreatedSince(objectWatermark));
            return;
        }
        Instantiate(element, parent, manifest, sourcePath, null, 0);
        ResolvePendingMaskLinks();
        var createdObjects = ObjectsCreatedSince(objectWatermark);
        ApplyInitialVisibility(createdObjects);
        InvokeInitialOnLoad(createdObjects);
        InvokeInitialVisibilityScripts(createdObjects);
    }

    public bool ApplyTemplates(
        UiObject value,
        UiObject? parent,
        string? templateNames,
        string? intrinsicType = null)
    {
        var objectWatermark = _runtime.Ui.LastObjectId;
        var applied = false;
        RegisterNativeIntrinsicLoad(value);
        var stack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parentArrays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_intrinsics.TryGetValue(intrinsicType ?? value.ObjectType, out var intrinsic))
        {
            ApplyDefinition(
                value,
                intrinsic.Element,
                null,
                intrinsic.SourcePath,
                stack,
                0,
                true,
                environmentReference: intrinsic.EnvironmentReference,
                templateNameRoot: value);
            applied = true;
        }

        foreach (var templateName in SplitList(templateNames))
        {
            if (!_templates.TryGetValue(templateName, out var template))
                continue;
            var parentKey = ResolveInheritedAttribute(
                template.Element,
                "parentKey",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(parentKey) && parentKeys.Add(parentKey))
                BindToParent(value, parent, parentKey);
            var parentArray = ResolveInheritedAttribute(
                template.Element,
                "parentArray",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(parentArray) && parentArrays.Add(parentArray))
                BindToParentArray(value, parent, parentArray);
            ApplyDefinition(
                value,
                template.Element,
                null,
                template.SourcePath,
                stack,
                0,
                environmentReference: template.EnvironmentReference,
                templateNameRoot: value);
            applied = true;
        }

        if (applied)
        {
            LuaBindings.InitializeButtonFontString(_runtime, value);
            ResolvePendingMaskLinks();
            var created = ObjectsCreatedSince(objectWatermark, value);
            ApplyInitialVisibility(created);
            InvokeInitialOnLoad(created);
            InvokeInitialVisibilityScripts(created);
        }
        return applied;
    }

    public bool TemplatesExist(string? templateNames) =>
        SplitList(templateNames).All(templateName => _templates.ContainsKey(templateName));

    public bool TryResolveCreateFrameObjectType(
        string requestedObjectType,
        out string objectType)
    {
        objectType = requestedObjectType;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (!WowWidgetApi.IsCreatableFrameXmlObjectType(objectType))
        {
            if (!visited.Add(objectType) ||
                !_intrinsics.TryGetValue(objectType, out var intrinsic))
            {
                return false;
            }
            objectType = intrinsic.Element.Name.LocalName;
        }
        return true;
    }

    private IReadOnlyList<UiObject> ObjectsCreatedSince(int watermark, UiObject? root = null)
    {
        var objects = _runtime.Ui.Objects;
        var created = new List<UiObject>();
        for (var id = watermark + 1; id <= _runtime.Ui.LastObjectId; id++)
        {
            if (objects.TryGetValue(id, out var value))
                created.Add(value);
        }
        if (root is not null && root.Id <= watermark)
            created.Add(root);
        return created;
    }

    private void InvokeInitialOnLoad(IReadOnlyList<UiObject> created)
    {
        var candidates = created
            .Where(value => !_runtime.HasPendingGlobalMixins(value.Id))
            .OrderByDescending(ObjectDepth)
            .ThenBy(value => value.Id)
            .ToArray();
        foreach (var candidate in candidates)
        {
            if (candidate.ScriptReferences.ContainsKey("OnLoad"))
                _runtime.InvokeScript(candidate, "OnLoad");
        }
    }

    private void ApplyInitialVisibility(IReadOnlyList<UiObject> created)
    {
        foreach (var candidate in created
                     .OrderByDescending(ObjectDepth)
                     .ThenBy(value => value.Id))
        {
            if (_runtime.HasPendingGlobalMixins(candidate.Id) ||
                !_pendingInitialShownStates.Remove(candidate.Id, out var shown))
            {
                continue;
            }

            candidate.Shown = shown;
        }
    }

    private int ObjectDepth(UiObject value)
    {
        var depth = 0;
        var current = value;
        while (current.ParentId is { } parentId && _runtime.Ui.Find(parentId) is { } parent)
        {
            depth++;
            current = parent;
        }
        return depth;
    }

    private void InvokeInitialVisibilityScripts(IReadOnlyList<UiObject> created)
    {
        var candidates = created
            .Where(value => !_runtime.HasPendingGlobalMixins(value.Id))
            .OrderByDescending(ObjectDepth)
            .ThenBy(value => value.Id)
            .ToArray();
        foreach (var candidate in candidates)
        {
            if (!_runtime.Ui.IsVisible(candidate))
                continue;
            if (candidate.ScriptReferences.ContainsKey("OnShow"))
                _runtime.InvokeScript(candidate, "OnShow");
            LuaBindings.ApplyEditBoxAutoFocusAfterShow(_runtime, candidate);
        }
    }

    internal void InvokeDeferredInitialLifecycle(IReadOnlySet<int> objectIds)
    {
        if (objectIds.Count == 0)
            return;
        var candidates = new List<UiObject>(objectIds.Count);
        foreach (var id in objectIds)
        {
            if (_runtime.Ui.Objects.TryGetValue(id, out var value))
                candidates.Add(value);
        }
        ApplyInitialVisibility(candidates);
        InvokeInitialOnLoad(candidates);
        InvokeInitialVisibilityScripts(candidates);
    }

    private UiObject Instantiate(
        XElement element,
        UiObject? parent,
        AddonManifest? manifest,
        string sourcePath,
        string? drawLayer,
        int subLevel,
        UiObject? templateNameRoot = null)
    {
        var objectType = NormalizeObjectType(element.Name.LocalName);
        var name = ExpandObjectName(Attribute(element, "name"), parent, templateNameRoot);
        var value = LuaBindings.CreateObject(
            _runtime,
            objectType,
            name,
            parent,
            drawLayer,
            subLevel);
        value.SourceLocation = sourcePath;
        RegisterNativeIntrinsicLoad(value);
        ApplyCreationAttributes(value, element);
        if (name is not null)
            LuaBindings.SetGlobalObject(_runtime, value);
        BindToParent(
            value,
            parent,
            ResolveInheritedAttribute(
                element,
                "parentKey",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
        BindToParentArray(
            value,
            parent,
            ResolveInheritedAttribute(
                element,
                "parentArray",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

        var stack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_intrinsics.TryGetValue(element.Name.LocalName, out var intrinsic))
        {
            ApplyDefinition(
                value,
                intrinsic.Element,
                manifest,
                intrinsic.SourcePath,
                stack,
                0,
                true,
                environmentReference: intrinsic.EnvironmentReference,
                templateNameRoot: value);
        }

        ApplyDefinition(
            value,
            element,
            manifest,
            sourcePath,
            stack,
            0,
            environmentReference: _runtime.CurrentAddonEnvironmentReference,
            templateNameRoot: templateNameRoot);
        if (value.Texture is not null &&
            value.ParentId is not null &&
            value.AllPointsTargetId is null &&
            value.Anchors.Count == 0)
        {
            value.AllPointsTargetId = value.ParentId;
            _runtime.Ui.InvalidateLayout();
        }
        LuaBindings.InitializeButtonFontString(_runtime, value);
        return value;
    }

    private static void ApplyCreationAttributes(UiObject value, XElement element)
    {
        if (Attribute(element, "id") is { } id &&
            int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId))
        {
            value.FrameId = numericId;
        }
    }

    private void ApplyDefinition(
        UiObject value,
        XElement element,
        AddonManifest? manifest,
        string sourcePath,
        HashSet<string> inheritanceStack,
        int depth,
        bool intrinsicDefinition = false,
        bool keyValuesResolved = false,
        int? environmentReference = null,
        UiObject? templateNameRoot = null)
    {
        var previousEnvironment = _runtime.SwapAddonEnvironment(environmentReference);
        try
        {
            ApplyDefinitionCore(
                value,
                element,
                manifest,
                sourcePath,
                inheritanceStack,
                depth,
                intrinsicDefinition,
                keyValuesResolved,
                environmentReference,
                templateNameRoot);
        }
        finally
        {
            _runtime.SwapAddonEnvironment(previousEnvironment);
        }
    }

    private void ApplyDefinitionCore(
        UiObject value,
        XElement element,
        AddonManifest? manifest,
        string sourcePath,
        HashSet<string> inheritanceStack,
        int depth,
        bool intrinsicDefinition,
        bool keyValuesResolved,
        int? environmentReference,
        UiObject? templateNameRoot)
    {
        if (depth > 64)
            throw new InvalidDataException($"XML template inheritance is too deep in {sourcePath}.");

        if (!keyValuesResolved)
        {
            ApplyInheritedKeyValues(
                value,
                element,
                sourcePath,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                0,
                environmentReference);
            keyValuesResolved = true;
        }

        foreach (var templateName in SplitList(Attribute(element, "inherits")))
        {
            if (!_templates.TryGetValue(templateName, out var template))
                continue;
            if (!inheritanceStack.Add(templateName))
                continue;
            if (value.Font is not null &&
                NormalizeObjectType(template.Element.Name.LocalName)
                    .Equals("Font", StringComparison.OrdinalIgnoreCase) &&
                _runtime.Ui.Find(templateName) is { Font: not null } fontObject &&
                fontObject.Id != value.Id)
            {
                LuaBindings.AssignFontObject(_runtime, value, fontObject);
            }
            else
            {
                ApplyDefinition(
                    value,
                    template.Element,
                    manifest,
                    template.SourcePath,
                    inheritanceStack,
                    depth + 1,
                    intrinsicDefinition,
                    keyValuesResolved,
                    template.EnvironmentReference,
                    value);
            }
        }

        _runtime.ApplyGlobalMixins(value, Attribute(element, "mixin"));
        _runtime.ApplyGlobalMixins(value, Attribute(element, "secureMixin"));
        ApplyMixinElements(value, element);
        ApplyAttributes(value, element);

        foreach (var child in element.Elements())
        {
            switch (child.Name.LocalName.ToLowerInvariant())
            {
                case "size":
                    ApplySize(value, child);
                    break;
                case "anchors":
                    ApplyAnchors(value, child);
                    break;
                case "hitrectinsets":
                    ApplyHitRectInsets(value, child);
                    break;
                case "layers":
                    ApplyLayers(value, child, manifest, sourcePath, templateNameRoot);
                    break;
                case "frames":
                    ApplyFrames(value, child, manifest, sourcePath, templateNameRoot);
                    break;
                case "scrollchild":
                    ApplyScrollChild(value, child, manifest, sourcePath, templateNameRoot);
                    break;
                case "scripts":
                    ApplyScripts(value, child, sourcePath, intrinsicDefinition);
                    break;
                case "attributes":
                    ApplyFrameAttributes(value, child);
                    break;
                case "keyvalues":
                    break;
                case "animations":
                    ApplyAnimations(value, child, manifest, sourcePath, templateNameRoot);
                    break;
                case "controlpoints":
                    ApplyControlPoints(value, child, manifest, sourcePath, templateNameRoot);
                    break;
                case "offset":
                    ApplyAnimationOrControlPointOffset(value, child);
                    break;
                case "normalfont":
                    ApplyButtonFontObject(value, child, ButtonFontKind.Normal);
                    break;
                case "highlightfont":
                    ApplyButtonFontObject(value, child, ButtonFontKind.Highlight);
                    break;
                case "disabledfont":
                    ApplyButtonFontObject(value, child, ButtonFontKind.Disabled);
                    break;
                case "text":
                    if (value.Font is not null)
                        value.Font.Text = ResolveText(child.Value);
                    break;
                case "color":
                    ApplyColor(value, child);
                    break;
                case "startcolor":
                    if (value.Animation is { } startColorAnimation)
                        startColorAnimation.StartColor = QuantizeColor(ReadColor(child));
                    break;
                case "endcolor":
                    if (value.Animation is { } endColorAnimation)
                        endColorAnimation.EndColor = QuantizeColor(ReadColor(child));
                    break;
                case "shadow":
                    ApplyFontShadow(value, child);
                    break;
                case "fontheight":
                    ApplyFontHeight(value, child);
                    break;
                case "fontstring" when IsMessageFrame(value):
                    ApplyMessageFrameFontString(value, child);
                    break;
                case "fontstring" when IsEditBox(value):
                    ApplyMessageFrameFontString(value, child);
                    break;
                case "textinsets" when IsEditBox(value):
                    ApplyEditBoxTextInsets(value, child);
                    break;
                case "insets" when IsMessageFrame(value):
                    ApplyMessageFrameInsets(value, child);
                    break;
                case "gradient":
                    ApplyGradient(value, child);
                    break;
                case "texcoords":
                    ApplyTexCoords(value, child);
                    break;
                case "maskedtextures":
                    ApplyMaskedTextures(value, child);
                    break;
                case "swipetexture" when value.Cooldown is not null:
                    ApplyCooldownTexture(value.Cooldown, child, CooldownTextureKind.Swipe);
                    break;
                case "edgetexture" when value.Cooldown is not null:
                    ApplyCooldownTexture(value.Cooldown, child, CooldownTextureKind.Edge);
                    break;
                case "blingtexture" when value.Cooldown is not null:
                    ApplyCooldownTexture(value.Cooldown, child, CooldownTextureKind.Bling);
                    break;
                default:
                    if (IsObjectElement(child.Name.LocalName))
                    {
                        var region = Instantiate(
                            child,
                            value,
                            manifest,
                            sourcePath,
                            value.DrawLayer,
                            value.SubLevel,
                            templateNameRoot);
                        ApplySpecialRegionOwnership(value, region, child.Name.LocalName);
                    }
                    break;
            }
        }

        if (value.Cooldown is { } cooldown)
        {
            LuaBindings.EnsureCooldownFontString(_runtime, value, cooldown);
            LuaBindings.ApplyCooldownFont(_runtime, cooldown);
        }
        if (value.ColorSelect is not null)
            LuaBindings.RefreshColorSelectVisuals(_runtime, value);
        _runtime.Ui.InvalidateLayout();
    }

    private void ApplyInheritedKeyValues(
        UiObject value,
        XElement element,
        string sourcePath,
        HashSet<string> inheritanceStack,
        int depth,
        int? environmentReference)
    {
        var previousEnvironment = _runtime.SwapAddonEnvironment(environmentReference);
        try
        {
            ApplyInheritedKeyValuesCore(
                value,
                element,
                sourcePath,
                inheritanceStack,
                depth);
        }
        finally
        {
            _runtime.SwapAddonEnvironment(previousEnvironment);
        }
    }

    private void ApplyInheritedKeyValuesCore(
        UiObject value,
        XElement element,
        string sourcePath,
        HashSet<string> inheritanceStack,
        int depth)
    {
        if (depth > 64)
            throw new InvalidDataException($"XML template inheritance is too deep in {sourcePath}.");

        foreach (var templateName in SplitList(Attribute(element, "inherits")))
        {
            if (!_templates.TryGetValue(templateName, out var template) ||
                !inheritanceStack.Add(templateName))
            {
                continue;
            }

            ApplyInheritedKeyValues(
                value,
                template.Element,
                template.SourcePath,
                inheritanceStack,
                depth + 1,
                template.EnvironmentReference);
            inheritanceStack.Remove(templateName);
        }

        foreach (var keyValues in element.Elements().Where(
                     child => child.Name.LocalName.Equals("KeyValues", StringComparison.OrdinalIgnoreCase)))
        {
            ApplyKeyValues(value, keyValues, sourcePath);
        }
    }

    private void RegisterNativeIntrinsicLoad(UiObject value)
    {
        if (!NativeIntrinsicObjectTypes.Contains(value.ObjectType) ||
            !_nativeIntrinsicLoads.Add(value.Id))
        {
            return;
        }

        const string body =
            "local callback = self[\"OnLoad_Intrinsic\"]; " +
            "if callback then return callback(self, ...) end";
        var reference = _runtime.CompileXmlScript(
            XmlEventLocals("OnLoad") + body,
            "@intrinsic:OnLoad_Intrinsic");
        AddXmlScript(
            value,
            "OnLoad",
            reference,
            null,
            null,
            "intrinsic",
            true);
    }

    private void ApplyMixinElements(UiObject value, XElement element)
    {
        foreach (var mixins in element.Elements().Where(
                     child => child.Name.LocalName.Equals("Mixins", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var mixin in mixins.Elements().Where(
                         child => child.Name.LocalName.Equals("Mixin", StringComparison.OrdinalIgnoreCase)))
            {
                _runtime.ApplyGlobalMixins(
                    value,
                    Attribute(mixin, "key"),
                    string.Equals(Attribute(mixin, "source"), "secure", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private void ApplyAttributes(UiObject value, XElement element)
    {
        if (Attribute(element, "hidden") is { } hidden)
            _pendingInitialShownStates[value.Id] = !ParseBoolean(hidden);
        if (Attribute(element, "protected") is { } protectedValue)
        {
            value.ProtectedExplicitly = ParseBoolean(protectedValue);
            value.Protected = value.ProtectedExplicitly;
        }
        value.MouseEnabled = BooleanAttribute(element, "enableMouse", value.MouseEnabled);
        value.MouseMotionEnabled = BooleanAttribute(
            element,
            "enableMouseMotion",
            value.MouseMotionEnabled);
        value.MouseClickEnabled = BooleanAttribute(
            element,
            "enableMouseClicks",
            value.MouseClickEnabled);
        if (Attribute(element, "registerForClicks") is { } clickRegistrations)
        {
            value.ClickRegistrations.Clear();
            foreach (var registration in SplitList(clickRegistrations)
                         .Select(UiObject.NormalizeMouseButtonRegistration)
                         .Where(registration => registration is not null))
            {
                value.ClickRegistrations.Add(registration!);
            }
        }
        if (Attribute(element, "registerForDrag") is { } dragRegistrations)
        {
            value.DragRegistrations.Clear();
            foreach (var registration in SplitList(dragRegistrations)
                         .Select(UiObject.NormalizeMouseButtonName)
                         .Where(registration => registration is not null))
            {
                value.DragRegistrations.Add(registration!);
            }
        }
        value.MouseWheelEnabled = BooleanAttribute(
            element,
            "enableMouseWheel",
            value.MouseWheelEnabled);
        value.Movable = BooleanAttribute(element, "movable", value.Movable);
        value.UserPlaced = BooleanAttribute(element, "userPlaced", value.UserPlaced);
        _runtime.Ui.SetToplevel(
            value,
            BooleanAttribute(element, "toplevel", value.Toplevel));
        _runtime.Ui.SetUseParentLevel(
            value,
            BooleanAttribute(element, "useParentLevel", value.UseParentLevel));
        value.ClampedToScreen = BooleanAttribute(
            element,
            "clampedToScreen",
            value.ClampedToScreen);
        if (IsEditBox(value))
        {
            value.AutoFocus = BooleanAttribute(
                element,
                "autoFocus",
                value.AutoFocus);
        }
        value.ClipsChildren = BooleanAttribute(
            element,
            "clipChildren",
            BooleanAttribute(element, "clipsChildren", value.ClipsChildren));
        if (Attribute(element, "passThroughButtons") is { } passThroughButtons)
        {
            value.PassThroughButtons.Clear();
            foreach (var button in SplitList(passThroughButtons))
            {
                if (UiObject.NormalizeMouseButtonName(button) is { } normalized)
                    value.PassThroughButtons.Add(normalized);
            }
        }
        value.Scale = NumberAttribute(element, "scale", value.Scale);
        if (Attribute(element, "alpha") is not null)
        {
            var xmlAlpha = Math.Clamp(NumberAttribute(element, "alpha", value.Alpha), 0, 1);
            value.Alpha = MathF.Truncate(xmlAlpha * 255) / 255;
        }
        if (Attribute(element, "id") is { } id &&
            int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId))
        {
            value.FrameId = numericId;
        }
        if (Attribute(element, "frameStrata") is { } strata)
            _runtime.Ui.SetFrameStrata(value, strata);
        if (Attribute(element, "frameLevel") is { } level &&
            int.TryParse(level, NumberStyles.Integer, CultureInfo.InvariantCulture, out var frameLevel))
        {
            _runtime.Ui.SetFrameLevel(value, frameLevel);
        }

        if (BooleanAttribute(element, "setAllPoints"))
            value.AllPointsTargetId = value.ParentId;

        if (Attribute(element, "text") is { } text)
        {
            var resolvedText = ResolveText(text);
            value.TextValue = resolvedText;
            if (value.Font is not null)
                value.Font.Text = resolvedText;
        }

        if (IsMessageFrame(value))
        {
            value.MessageFading = BooleanAttribute(
                element,
                "fade",
                value.MessageFading);
            if (Attribute(element, "displayDuration") is not null)
            {
                var duration = NumberAttribute(
                    element,
                    "displayDuration",
                    value.MessageTimeVisible);
                if (duration > 0)
                    value.MessageTimeVisible = duration;
            }
            if (Attribute(element, "fadeDuration") is not null)
            {
                var duration = NumberAttribute(
                    element,
                    "fadeDuration",
                    value.MessageFadeDuration);
                if (duration > 0)
                    value.MessageFadeDuration = duration;
            }
            if (Attribute(element, "fadePower") is not null)
            {
                var power = NumberAttribute(element, "fadePower", value.MessageFadePower);
                if (power > 0)
                    value.MessageFadePower = power;
            }
            if (Attribute(element, "insertMode") is { } insertMode)
            {
                value.MessageInsertMode = insertMode.Equals(
                    "BOTTOM",
                    StringComparison.OrdinalIgnoreCase)
                    ? "BOTTOM"
                    : "TOP";
            }
        }

        if (value.Font is { } font)
            ApplyFontAttributes(value, font, element);

        if (value.Texture is not null)
        {
            if (Attribute(element, "file") is { } file)
            {
                value.Texture.Asset = file;
                value.Texture.FileDataId = null;
                value.Texture.AtlasName = null;
                value.Texture.AtlasWidth = null;
                value.Texture.AtlasHeight = null;
                value.Texture.SliceData = null;
                value.Texture.IsColor = false;
                value.Texture.Gradient = null;
                value.Texture.ClearAtlasRegion();
            }
            else if (Attribute(element, "atlas") is { } atlas)
                _runtime.ApplyAtlas(
                    value,
                    atlas,
                    BooleanAttribute(element, "useAtlasSize"),
                    wrapModeHorizontal: Attribute(element, "hWrapMode"),
                    wrapModeVertical: Attribute(element, "vWrapMode"));

            value.Texture.BlendMode = Attribute(element, "alphaMode") ??
                                      value.Texture.BlendMode;
            value.Texture.WrapHorizontal = Attribute(element, "hWrapMode") ??
                                           value.Texture.WrapHorizontal;
            value.Texture.WrapVertical = Attribute(element, "vWrapMode") ??
                                         value.Texture.WrapVertical;
            value.Texture.Desaturation = BooleanAttribute(element, "desaturated")
                ? 1
                : value.Texture.Desaturation;
            if (Attribute(element, "rotation") is not null)
            {
                value.Texture.Rotation =
                    NumberAttribute(element, "rotation") * MathF.PI / 180f;
            }

            value.Texture.SnapToPixelGrid = BooleanAttribute(
                element,
                "snapToPixelGrid",
                value.Texture.SnapToPixelGrid);
            value.Texture.TexelSnappingBias = Math.Clamp(
                NumberAttribute(
                    element,
                    "texelSnappingBias",
                    value.Texture.TexelSnappingBias),
                0,
                1);
            value.Texture.HorizontallyTiled = BooleanAttribute(
                element,
                "horizTile",
                value.Texture.HorizontallyTiled);
            value.Texture.VerticallyTiled = BooleanAttribute(
                element,
                "vertTile",
                value.Texture.VerticallyTiled);
        }

        if (value.StatusBar is { } statusBar)
        {
            var minimumAttribute = Attribute(element, "minValue");
            var maximumAttribute = Attribute(element, "maxValue");
            var defaultValueAttribute = Attribute(element, "defaultValue");
            statusBar.Minimum =
                NumberAttribute(element, "minValue", (float)statusBar.Minimum);
            statusBar.Maximum =
                NumberAttribute(element, "maxValue", (float)statusBar.Maximum);
            var isSlider =
                value.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase);
            if (isSlider)
            {
                if (minimumAttribute is not null && maximumAttribute is not null)
                {
                    statusBar.RangeInitialized = true;
                    if (defaultValueAttribute is not null)
                    {
                        statusBar.Value = Math.Clamp(
                            NumberAttribute(
                                element,
                                "defaultValue",
                                (float)statusBar.Value),
                            Math.Min(statusBar.Minimum, statusBar.Maximum),
                            Math.Max(statusBar.Minimum, statusBar.Maximum));
                        statusBar.ValueInitialized = true;
                    }
                }
            }
            else
            {
                statusBar.Value = Math.Clamp(
                    NumberAttribute(element, "defaultValue", (float)statusBar.Value),
                    Math.Min(statusBar.Minimum, statusBar.Maximum),
                    Math.Max(statusBar.Minimum, statusBar.Maximum));
            }
            if (isSlider)
            {
                value.ValueStep = NumberAttribute(element, "valueStep", 0.001f);
                var pageSteps = NumberAttribute(element, "stepsPerPage");
                value.StepsPerPage = unchecked((sbyte)(int)Math.Truncate(pageSteps));
                value.ObeyStepOnDrag = BooleanAttribute(
                    element,
                    "obeyStepOnDrag",
                    value.ObeyStepOnDrag);
            }
            statusBar.Orientation = Attribute(element, "orientation") ?? statusBar.Orientation;
            statusBar.ReverseFill = BooleanAttribute(element, "reverseFill", statusBar.ReverseFill);
            statusBar.RotatesTexture = BooleanAttribute(element, "rotatesTexture", statusBar.RotatesTexture);
        }

        if (value.Cooldown is { } cooldown)
        {
            cooldown.DrawSwipe = BooleanAttribute(
                element,
                "drawSwipe",
                cooldown.DrawSwipe);
            cooldown.DrawEdge = BooleanAttribute(
                element,
                "drawEdge",
                cooldown.DrawEdge);
            cooldown.DrawBling = BooleanAttribute(
                element,
                "drawBling",
                cooldown.DrawBling);
            cooldown.Reverse = BooleanAttribute(
                element,
                "reverse",
                cooldown.Reverse);
            cooldown.HideCountdownNumbers = BooleanAttribute(
                element,
                "hideCountdownNumbers",
                cooldown.HideCountdownNumbers);
            if (UIntAttribute(element, "minimumCountdownDuration") is { } minimum)
                cooldown.MinimumCountdownDurationMilliseconds = unchecked((int)minimum);
            if (Attribute(element, "useCircularEdge") is not null)
            {
                cooldown.EdgeScale = BooleanAttribute(element, "useCircularEdge")
                    ? 1
                    : MathF.Sqrt(2);
            }
            if (Attribute(element, "rotation") is not null)
            {
                cooldown.Rotation =
                    NumberAttribute(element, "rotation") * MathF.PI / 180f;
            }
        }

        if (value.Minimap is { } minimap)
            ApplyMinimapAttributes(minimap, element);

        if (value.Blob is { } blob)
            ApplyBlobAttributes(blob, element);

        if (value.AnimationGroup is { } group)
        {
            group.Looping = Attribute(element, "looping") ?? group.Looping;
            group.SetToFinalAlpha = BooleanAttribute(
                element,
                "setToFinalAlpha",
                group.SetToFinalAlpha);
        }

        if (value.Animation is { } animation)
        {
            animation.Order = IntegerAttribute(element, "order", animation.Order);
            animation.Duration = NumberAttribute(element, "duration", (float)animation.Duration);
            animation.StartDelay = NumberAttribute(
                element,
                "startDelay",
                (float)animation.StartDelay);
            animation.EndDelay = NumberAttribute(element, "endDelay", (float)animation.EndDelay);
            animation.Smoothing = Attribute(element, "smoothing") ?? animation.Smoothing;
            animation.FromAlpha = NumberAttribute(element, "fromAlpha", animation.FromAlpha);
            animation.ToAlpha = NumberAttribute(element, "toAlpha", animation.ToAlpha);
            ApplyDerivedAnimationAttributes(value, element, animation);
            if (Attribute(element, "target") is { } targetName)
            {
                var animationOwnerGroup = ParentOf(value);
                var owner = animationOwnerGroup is null ? null : ParentOf(animationOwnerGroup);
                animation.TargetMode = UiAnimationTargetMode.Name;
                animation.TargetNameOrKey = ExpandParentName(targetName, owner) ?? targetName;
                animation.TargetId = null;
            }
            if (Attribute(element, "childKey") is { } childKey)
            {
                animation.TargetMode = UiAnimationTargetMode.ChildKey;
                animation.TargetNameOrKey = childKey;
                animation.TargetId = null;
            }
            if (Attribute(element, "targetKey") is { } targetKey)
            {
                animation.TargetMode = UiAnimationTargetMode.TargetKey;
                animation.TargetNameOrKey = targetKey;
                animation.TargetId = null;
            }
            if (UIntAttribute(element, "flipBookRows") is > 0 and var flipBookRows)
                animation.FlipBookRows = flipBookRows;
            if (UIntAttribute(element, "flipBookColumns") is > 0 and var flipBookColumns)
                animation.FlipBookColumns = flipBookColumns;
            if (UIntAttribute(element, "flipBookFrames") is { } flipBookFrames)
                animation.FlipBookFrames = flipBookFrames;
            if (UIntAttribute(element, "flipBookFrameWidth") is { } flipBookFrameWidth)
                animation.FlipBookFrameWidth = flipBookFrameWidth;
            if (UIntAttribute(element, "flipBookFrameHeight") is { } flipBookFrameHeight)
                animation.FlipBookFrameHeight = flipBookFrameHeight;
            var curveType = Attribute(element, "curve")?.ToUpperInvariant();
            if (curveType is "NONE" or "SMOOTH")
                animation.PathCurveType = curveType;
        }

        if (value.ControlPoint is { } controlPoint)
        {
            controlPoint.Order = Math.Clamp(
                IntegerAttribute(element, "order", controlPoint.Order),
                -1,
                99);
        }
    }

    private static void ApplyMinimapAttributes(
        UiMinimapState minimap,
        XElement element)
    {
        ApplyTextureAssetAttribute(
            element,
            "minimapQuestBlobInsideTexture",
            (path, fileDataId) =>
                (minimap.Quest.InsideTexture,
                    minimap.Quest.InsideTextureFileDataId) = (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "minimapQuestBlobOutsideTexture",
            (path, fileDataId) =>
                (minimap.Quest.OutsideTexture,
                    minimap.Quest.OutsideTextureFileDataId) = (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "minimapQuestBlobOutsideSelectedTexture",
            (path, fileDataId) =>
                (minimap.Quest.OutsideSelectedTexture,
                    minimap.Quest.OutsideSelectedTextureFileDataId) = (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "minimapQuestBlobRingTexture",
            (path, fileDataId) =>
                (minimap.Quest.RingTexture,
                    minimap.Quest.RingTextureFileDataId) = (path, fileDataId));

        ApplyTextureAssetAttribute(
            element,
            "minimapTaskBlobInsideTexture",
            (path, fileDataId) =>
                (minimap.Task.InsideTexture,
                    minimap.Task.InsideTextureFileDataId) = (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "minimapTaskBlobOutsideTexture",
            (path, fileDataId) =>
                (minimap.Task.OutsideTexture,
                    minimap.Task.OutsideTextureFileDataId) = (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "minimapTaskBlobOutsideSelectedTexture",
            (path, fileDataId) =>
                (minimap.Task.OutsideSelectedTexture,
                    minimap.Task.OutsideSelectedTextureFileDataId) = (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "minimapTaskBlobRingTexture",
            (path, fileDataId) =>
                (minimap.Task.RingTexture,
                    minimap.Task.RingTextureFileDataId) = (path, fileDataId));

        ApplyTextureAssetAttribute(
            element,
            "minimapArchBlobInsideTexture",
            (path, fileDataId) =>
                (minimap.Arch.InsideTexture,
                    minimap.Arch.InsideTextureFileDataId) = (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "minimapArchBlobOutsideTexture",
            (path, fileDataId) =>
                (minimap.Arch.OutsideTexture,
                    minimap.Arch.OutsideTextureFileDataId) = (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "minimapArchBlobRingTexture",
            (path, fileDataId) =>
                (minimap.Arch.RingTexture,
                    minimap.Arch.RingTextureFileDataId) = (path, fileDataId));
    }

    private static void ApplyBlobAttributes(
        UiBlobState blob,
        XElement element)
    {
        ApplyTextureAssetAttribute(
            element,
            "fillTexture",
            (path, fileDataId) =>
                (blob.FillTexture, blob.FillTextureFileDataId) =
                (path, fileDataId));
        ApplyTextureAssetAttribute(
            element,
            "borderTexture",
            (path, fileDataId) =>
                (blob.BorderTexture, blob.BorderTextureFileDataId) =
                (path, fileDataId));
    }

    private static void ApplyTextureAssetAttribute(
        XElement element,
        string name,
        Action<string?, uint?> apply)
    {
        if (Attribute(element, name) is not { } asset)
            return;
        if (uint.TryParse(
                asset,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var fileDataId))
        {
            apply(null, fileDataId);
        }
        else
        {
            apply(asset, null);
        }
    }

    private static void ApplyDerivedAnimationAttributes(
        UiObject value,
        XElement element,
        UiAnimationState animation)
    {
        if (value.ObjectType.Equals("Rotation", StringComparison.OrdinalIgnoreCase))
        {
            if (OptionalNumberAttribute(element, "degrees") is { } degrees)
                animation.Degrees = degrees;
            if (OptionalNumberAttribute(element, "radians") is { } radians)
                animation.Radians = radians;
        }

        if (value.ObjectType.Equals("Scale", StringComparison.OrdinalIgnoreCase) ||
            value.ObjectType.Equals("LineScale", StringComparison.OrdinalIgnoreCase))
        {
            var scaleX = OptionalNumberAttribute(element, "scaleX");
            var scaleY = OptionalNumberAttribute(element, "scaleY");
            var xScratch = scaleX ?? 0;
            var yScratch = scaleY ?? 0;
            if (scaleX.HasValue || scaleY.HasValue)
            {
                animation.HasScaleRange = false;
                animation.Scale = new Vector2(
                    Math.Max(xScratch, .001f),
                    Math.Max(yScratch, .001f));
            }

            var fromScaleX = OptionalNumberAttribute(element, "fromScaleX");
            var fromScaleY = OptionalNumberAttribute(element, "fromScaleY");
            if (fromScaleX.HasValue || fromScaleY.HasValue)
            {
                animation.HasScaleRange = true;
                animation.ScaleFrom = new Vector2(
                    Math.Max(fromScaleX ?? 0, .001f),
                    Math.Max(fromScaleY ?? 0, .001f));
            }

            var toScaleX = OptionalNumberAttribute(element, "toScaleX");
            var toScaleY = OptionalNumberAttribute(element, "toScaleY");
            if (toScaleX.HasValue)
                xScratch = toScaleX.Value;
            if (toScaleY.HasValue)
                yScratch = toScaleY.Value;
            if (toScaleX.HasValue || toScaleY.HasValue)
            {
                animation.HasScaleRange = true;
                animation.ScaleTo = new Vector2(
                    Math.Max(xScratch, .001f),
                    Math.Max(yScratch, .001f));
            }
        }

        if (!value.ObjectType.Equals("Rotation", StringComparison.OrdinalIgnoreCase) &&
            !value.ObjectType.Equals("Scale", StringComparison.OrdinalIgnoreCase) &&
            !value.ObjectType.Equals("LineScale", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var origin in element.Elements().Where(child =>
                     child.Name.LocalName.Equals("Origin", StringComparison.OrdinalIgnoreCase)))
        {
            animation.OriginPoint = "CENTER";
            animation.OriginOffset = Vector2.Zero;
            if (Attribute(origin, "point") is not { } point ||
                !TryNormalizeAnchorPoint(point, out var normalizedPoint))
            {
                continue;
            }

            animation.OriginPoint = normalizedPoint;
            animation.OriginOffset = new Vector2(
                Dimension(origin, "x") ?? 0,
                Dimension(origin, "y") ?? 0);
        }
    }

    private static bool TryNormalizeAnchorPoint(string point, out string normalized)
    {
        normalized = point.ToUpperInvariant();
        return normalized is
            "TOPLEFT" or "TOP" or "TOPRIGHT" or
            "LEFT" or "CENTER" or "RIGHT" or
            "BOTTOMLEFT" or "BOTTOM" or "BOTTOMRIGHT";
    }

    private static void ApplySize(UiObject value, XElement size)
    {
        value.Width = Dimension(size, "x") ?? value.Width;
        value.Height = Dimension(size, "y") ?? value.Height;
    }

    private void ApplyButtonFontObject(UiObject value, XElement element, ButtonFontKind kind)
    {
        var name = Attribute(element, "style");
        var fontObject = name is null ? null : _runtime.Ui.Find(name);
        switch (kind)
        {
            case ButtonFontKind.Normal:
                value.NormalFontObjectId = fontObject?.Id;
                value.NormalFontObjectName = name;
                break;
            case ButtonFontKind.Highlight:
                value.HighlightFontObjectId = fontObject?.Id;
                value.HighlightFontObjectName = name;
                break;
            case ButtonFontKind.Disabled:
                value.DisabledFontObjectId = fontObject?.Id;
                value.DisabledFontObjectName = name;
                break;
        }
    }

    private string ResolveText(string text) =>
        _runtime.TryGetGlobalString(text, out var resolved) ? resolved : text;

    private void ApplyAnchors(UiObject value, XElement anchors)
    {
        var definitions = anchors.Elements().Where(
                child => child.Name.LocalName.Equals("Anchor", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (definitions.Length == 0)
            return;

        value.Anchors.Clear();
        value.AllPointsTargetId = null;

        foreach (var anchor in definitions)
        {
            var point = Attribute(anchor, "point") ?? "CENTER";
            var relativePoint = Attribute(anchor, "relativePoint") ?? point;
            var relative = ResolveAnchorTarget(value, anchor) ??
                           (value.ParentId is { } parentId ? _runtime.Ui.Find(parentId) : null);
            var offset = anchor.Elements().FirstOrDefault(child =>
                child.Name.LocalName.Equals("Offset", StringComparison.OrdinalIgnoreCase));
            var resolvedAnchor = new UiAnchor(
                point,
                relative?.Id,
                relativePoint,
                Dimension(anchor, "x") ??
                (offset is null ? null : Dimension(offset, "x")) ?? 0,
                Dimension(anchor, "y") ??
                (offset is null ? null : Dimension(offset, "y")) ?? 0);
            var existingIndex = value.Anchors.FindIndex(existing =>
                existing.Point.Equals(point, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
                value.Anchors[existingIndex] = resolvedAnchor;
            else
                value.Anchors.Add(resolvedAnchor);
        }
    }

    private static void ApplyHitRectInsets(UiObject value, XElement hitRectInsets)
    {
        var inset = hitRectInsets.Elements().FirstOrDefault(child =>
            child.Name.LocalName is "AbsInset" or "RelInset");
        if (inset is null)
            return;

        value.HitRectInsets = new UiInsets(
            Dimension(inset, "left") ?? 0,
            Dimension(inset, "right") ?? 0,
            Dimension(inset, "top") ?? 0,
            Dimension(inset, "bottom") ?? 0);
    }

    private static bool IsMessageFrame(UiObject value) =>
        value.ObjectType.EndsWith("MessageFrame", StringComparison.OrdinalIgnoreCase);

    private static bool IsEditBox(UiObject value) =>
        value.ObjectType.EndsWith("EditBox", StringComparison.OrdinalIgnoreCase);

    private static void ApplyEditBoxTextInsets(UiObject value, XElement insets)
    {
        value.TextInsets = new Vector4(
            Dimension(insets, "left") ?? 0,
            Dimension(insets, "right") ?? 0,
            Dimension(insets, "top") ?? 0,
            Dimension(insets, "bottom") ?? 0);
    }

    private static void ApplyMessageFrameInsets(UiObject value, XElement insets)
    {
        var inset = insets.Elements().FirstOrDefault(child =>
            child.Name.LocalName is "AbsInset" or "RelInset");
        if (inset is null)
            return;

        value.MessageInsets = new UiInsets(
            Dimension(inset, "left") ?? 0,
            Dimension(inset, "right") ?? 0,
            Dimension(inset, "top") ?? 0,
            Dimension(inset, "bottom") ?? 0);
    }

    private void ApplyMessageFrameFontString(UiObject value, XElement element) =>
        ApplyMessageFrameFontString(
            value,
            element,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private void ApplyMessageFrameFontString(
        UiObject value,
        XElement element,
        HashSet<string> inheritanceStack)
    {
        if (value.Font is not { } font)
            return;

        foreach (var templateName in SplitList(Attribute(element, "inherits")))
        {
            if (!inheritanceStack.Add(templateName))
                continue;
            if (_runtime.Ui.Find(templateName)?.Font is { } sourceFont)
            {
                CopyFontStyle(font, sourceFont);
                value.FontObjectId = _runtime.Ui.Find(templateName)!.Id;
            }
            else if (_templates.TryGetValue(templateName, out var template))
            {
                ApplyMessageFrameFontString(
                    value,
                    template.Element,
                    inheritanceStack);
            }
            inheritanceStack.Remove(templateName);
        }

        ApplyFontAttributes(value, font, element);
        foreach (var child in element.Elements())
        {
            switch (child.Name.LocalName.ToLowerInvariant())
            {
                case "color":
                    ApplyColor(value, child);
                    break;
                case "shadow":
                    ApplyFontShadow(value, child);
                    break;
                case "fontheight":
                    ApplyFontHeight(value, child);
                    break;
            }
        }
    }

    private void ApplyFontAttributes(
        UiObject value,
        UiFontState font,
        XElement element)
    {
        if (Attribute(element, "font") is { Length: > 0 } fontNameOrPath)
        {
            if (_runtime.Ui.Find(fontNameOrPath)?.Font is { } sourceFont)
            {
                CopyFontStyle(font, sourceFont);
                value.FontObjectId = _runtime.Ui.Find(fontNameOrPath)!.Id;
            }
            else
            {
                font.FontPath = fontNameOrPath;
                font.IsConfigured = true;
                font.LocalOverrides |= UiFontOverrides.FontPath;
                value.FontObjectId = null;
            }
        }
        if (Attribute(element, "height") is not null)
        {
            font.FontSize = Math.Max(0, NumberAttribute(element, "height", font.FontSize));
            font.LocalOverrides |= UiFontOverrides.FontSize;
        }
        if (Attribute(element, "outline") is { } outline)
        {
            font.FontFlags = ReplaceFontFlag(font.FontFlags, "OUTLINE", outline);
            font.LocalOverrides |= UiFontOverrides.FontFlags;
        }
        if (Attribute(element, "monochrome") is { } monochrome)
        {
            font.FontFlags = ReplaceFontFlag(
                font.FontFlags,
                "MONOCHROME",
                ParseBoolean(monochrome) ? "MONOCHROME" : string.Empty);
            font.LocalOverrides |= UiFontOverrides.FontFlags;
        }
        if (Attribute(element, "justifyH") is { } justifyHorizontal)
        {
            font.JustifyHorizontal = justifyHorizontal;
            font.HasLocalJustifyHorizontal = true;
            font.LocalOverrides |= UiFontOverrides.JustifyHorizontal;
        }
        if (Attribute(element, "justifyV") is { } justifyVertical)
        {
            font.JustifyVertical = justifyVertical;
            font.HasLocalJustifyVertical = true;
            font.LocalOverrides |= UiFontOverrides.JustifyVertical;
        }
        if (Attribute(element, "spacing") is not null)
        {
            font.Spacing = NumberAttribute(element, "spacing", font.Spacing);
            font.LocalOverrides |= UiFontOverrides.Spacing;
        }
        if (Attribute(element, "maxLines") is not null)
        {
            font.MaximumLines = IntegerAttribute(element, "maxLines", font.MaximumLines);
            font.LocalOverrides |= UiFontOverrides.MaximumLines;
        }
        if (Attribute(element, "wordwrap") is not null)
        {
            font.WordWrap = BooleanAttribute(element, "wordwrap", font.WordWrap);
            font.LocalOverrides |= UiFontOverrides.WordWrap;
        }
        if (Attribute(element, "nonspacewrap") is not null)
        {
            font.NonSpaceWrap = BooleanAttribute(element, "nonspacewrap", font.NonSpaceWrap);
            font.LocalOverrides |= UiFontOverrides.NonSpaceWrap;
        }
        if (Attribute(element, "indentedWordWrap") is not null)
        {
            font.IndentedWordWrap = BooleanAttribute(
                element,
                "indentedWordWrap",
                font.IndentedWordWrap);
            font.LocalOverrides |= UiFontOverrides.IndentedWordWrap;
        }
        if (Attribute(element, "canBeUserScaled") is not null)
        {
            font.CanBeUserScaled = true;
            font.LocalOverrides |= UiFontOverrides.CanBeUserScaled;
        }
        if (Attribute(element, "scaleAnimationMode") is { } scaleAnimationMode)
        {
            if (scaleAnimationMode.Equals("FontSize", StringComparison.OrdinalIgnoreCase))
                value.FontScaleAnimationMode = 0;
            else if (scaleAnimationMode.Equals("Vertex", StringComparison.OrdinalIgnoreCase))
                value.FontScaleAnimationMode = 1;
        }
        if (Attribute(element, "smoothScaling") is not null)
        {
            value.FontSmoothScaling = BooleanAttribute(
                element,
                "smoothScaling",
                value.FontSmoothScaling);
        }
    }

    private void ApplyLayers(
        UiObject owner,
        XElement layers,
        AddonManifest? manifest,
        string sourcePath,
        UiObject? templateNameRoot)
    {
        foreach (var layer in layers.Elements().Where(
                     child => child.Name.LocalName.Equals("Layer", StringComparison.OrdinalIgnoreCase)))
        {
            var drawLayer = Attribute(layer, "level") ?? "ARTWORK";
            var subLevel = IntegerAttribute(layer, "textureSubLevel");
            foreach (var element in layer.Elements().Where(
                         child => IsObjectElement(child.Name.LocalName)))
            {
                Instantiate(
                    element,
                    owner,
                    manifest,
                    sourcePath,
                    drawLayer,
                    subLevel,
                    templateNameRoot);
            }
        }
    }

    private void ApplyFrames(
        UiObject owner,
        XElement frames,
        AddonManifest? manifest,
        string sourcePath,
        UiObject? templateNameRoot)
    {
        foreach (var element in frames.Elements().Where(
                     child => IsObjectElement(child.Name.LocalName)))
        {
            var child = Instantiate(
                element,
                owner,
                manifest,
                sourcePath,
                null,
                0,
                templateNameRoot);
            ApplySpecialRegionOwnership(owner, child, element.Name.LocalName);
        }
    }

    private void ApplyAnimations(
        UiObject owner,
        XElement animations,
        AddonManifest? manifest,
        string sourcePath,
        UiObject? templateNameRoot)
    {
        foreach (var element in animations.Elements().Where(
                     child => IsObjectElement(child.Name.LocalName)))
        {
            Instantiate(
                element,
                owner,
                manifest,
                sourcePath,
                null,
                0,
                templateNameRoot);
        }
    }

    private void ApplyControlPoints(
        UiObject owner,
        XElement controlPoints,
        AddonManifest? manifest,
        string sourcePath,
        UiObject? templateNameRoot)
    {
        if (!owner.ObjectType.Equals("Path", StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var element in controlPoints.Elements().Where(
                     child => child.Name.LocalName.Equals(
                         "ControlPoint",
                         StringComparison.OrdinalIgnoreCase)))
        {
            Instantiate(
                element,
                owner,
                manifest,
                sourcePath,
                null,
                0,
                templateNameRoot);
        }
        _runtime.Ui.ResolvePathControlPoints(owner);
    }

    private static void ApplyAnimationOrControlPointOffset(
        UiObject value,
        XElement offsetElement)
    {
        var offset = new System.Numerics.Vector2(
            Dimension(offsetElement, "x") ?? 0,
            Dimension(offsetElement, "y") ?? 0);
        if (offset.LengthSquared() <= 2.3841858e-7f)
            offset = System.Numerics.Vector2.Zero;

        if (value.ControlPoint is { } controlPoint)
            controlPoint.Offset = offset;
        else if (value.Animation is { } animation)
            animation.Offset = offset;
    }

    private void ApplyScrollChild(
        UiObject owner,
        XElement scrollChild,
        AddonManifest? manifest,
        string sourcePath,
        UiObject? templateNameRoot)
    {
        var element = scrollChild.Elements().FirstOrDefault(
            child => IsObjectElement(child.Name.LocalName));
        if (element is null)
            return;
        var child = Instantiate(
            element,
            owner,
            manifest,
            sourcePath,
            null,
            0,
            templateNameRoot);
        owner.ScrollChildId = child.Id;
    }

    private void ApplyScripts(
        UiObject value,
        XElement scripts,
        string sourcePath,
        bool intrinsicDefinition)
    {
        foreach (var script in scripts.Elements())
        {
            var scriptName = script.Name.LocalName;
            string body;
            if (Attribute(script, "method") is { } method)
            {
                body =
                    $"local callback = self[{LuaString(method)}]; " +
                    "if callback then return callback(self, ...) end";
            }
            else if (Attribute(script, "function") is { Length: > 0 } function)
            {
                body = $"return {function}(self, ...)";
            }
            else
            {
                body = script.Value;
            }

            body = XmlEventLocals(scriptName) + body;
            var reference = _runtime.CompileXmlScript(
                body,
                $"@{sourcePath}:{scriptName}");
            AddXmlScript(
                value,
                scriptName,
                reference,
                Attribute(script, "intrinsicOrder"),
                Attribute(script, "inherit"),
                sourcePath,
                intrinsicDefinition);
        }
    }

    private void AddXmlScript(
        UiObject value,
        string scriptName,
        int reference,
        string? intrinsicOrder,
        string? inherit,
        string sourcePath,
        bool intrinsicDefinition)
    {
        var key = ScriptChainKey(value, scriptName);
        if (!_scriptChains.TryGetValue(key, out var chain))
        {
            chain = new XmlScriptChain();
            _scriptChains[key] = chain;
        }

        if (intrinsicDefinition || !string.IsNullOrWhiteSpace(intrinsicOrder))
        {
            if (intrinsicOrder?.Equals("postcall", StringComparison.OrdinalIgnoreCase) == true)
                chain.IntrinsicAfter.Add(reference);
            else
                chain.IntrinsicBefore.Add(reference);
        }
        else if (inherit?.Equals("append", StringComparison.OrdinalIgnoreCase) == true)
        {
            chain.Normal.Insert(0, reference);
        }
        else if (inherit?.Equals("prepend", StringComparison.OrdinalIgnoreCase) == true)
        {
            chain.Normal.Add(reference);
        }
        else
        {
            foreach (var inheritedReference in chain.Normal)
                _runtime.ReleaseReference(inheritedReference);
            chain.Normal.Clear();
            chain.Normal.Add(reference);
        }

        RebuildScriptChain(value, scriptName, chain, $"@{sourcePath}:{scriptName}:chain");
        LuaBindings.EnableInputForScript(value, scriptName);
    }

    internal void SetScript(UiObject value, string scriptName, int? reference)
    {
        var key = ScriptChainKey(value, scriptName);
        if (!_scriptChains.TryGetValue(key, out var chain))
        {
            chain = new XmlScriptChain();
            _scriptChains[key] = chain;
        }

        foreach (var previous in chain.Normal)
            _runtime.ReleaseReference(previous);
        chain.Normal.Clear();
        if (reference is { } callback)
            chain.Normal.Add(callback);

        RebuildScriptChain(
            value,
            scriptName,
            chain,
            $"@SetScript:{value.Name ?? value.ObjectType}:{scriptName}");
    }

    internal void HookScript(UiObject value, string scriptName, int reference)
    {
        var key = ScriptChainKey(value, scriptName);
        if (!_scriptChains.TryGetValue(key, out var chain))
        {
            chain = new XmlScriptChain();
            _scriptChains[key] = chain;
        }

        chain.Hooks.Add(reference);
        RebuildScriptChain(
            value,
            scriptName,
            chain,
            $"@HookScript:{value.Name ?? value.ObjectType}:{scriptName}");
    }

    internal void ClearScripts(UiObject value)
    {
        var keys = _scriptChains.Keys
            .Where(key => key.OwnerId == value.Id)
            .ToArray();
        foreach (var key in keys)
        {
            var chain = _scriptChains[key];
            foreach (var reference in chain.IntrinsicBefore
                         .Concat(chain.Normal)
                         .Concat(chain.Hooks)
                         .Concat(chain.IntrinsicAfter))
            {
                _runtime.ReleaseReference(reference);
            }
            _runtime.ReleaseReference(chain.ExposedReference);
            _scriptChains.Remove(key);
        }

        foreach (var reference in value.ScriptReferences.Values)
            _runtime.ReleaseReference(reference);
        value.ScriptReferences.Clear();
    }

    internal bool TryGetScript(UiObject value, string scriptName, out int reference)
    {
        if (_scriptChains.TryGetValue(ScriptChainKey(value, scriptName), out var chain) &&
            chain.Normal.Count > 0)
        {
            if (chain.ExposedReference > 0)
            {
                reference = chain.ExposedReference;
                return true;
            }

            reference = chain.Normal[0];
            return true;
        }

        reference = 0;
        return false;
    }

    private void RebuildScriptChain(
        UiObject value,
        string scriptName,
        XmlScriptChain chain,
        string chunkName)
    {
        if (value.ScriptReferences.Remove(scriptName, out var previousChain))
            _runtime.ReleaseReference(previousChain);
        _runtime.ReleaseReference(chain.ExposedReference);
        chain.ExposedReference = 0;
        var exposed = chain.Normal.Concat(chain.Hooks).ToArray();
        if (chain.Normal.Count > 0 && exposed.Length > 1)
        {
            chain.ExposedReference = _runtime.CompileXmlScriptChain(
                exposed,
                $"{chunkName}:exposed");
        }
        var ordered = chain.IntrinsicBefore
            .Concat(chain.Normal)
            .Concat(chain.Hooks)
            .Concat(chain.IntrinsicAfter)
            .ToArray();
        if (ordered.Length == 0)
            return;
        value.ScriptReferences[scriptName] = _runtime.CompileXmlScriptChain(ordered, chunkName);
    }

    private static (int OwnerId, string Script) ScriptChainKey(
        UiObject value,
        string scriptName) =>
        (value.Id, scriptName.ToUpperInvariant());

    private void ApplyKeyValues(UiObject value, XElement keyValues, string sourcePath)
    {
        foreach (var keyValue in keyValues.Elements().Where(
                     child => child.Name.LocalName.Equals("KeyValue", StringComparison.OrdinalIgnoreCase)))
        {
            var key = Attribute(keyValue, "key");
            var rawValue = Attribute(keyValue, "value");
            if (string.IsNullOrWhiteSpace(key) || rawValue is null)
                continue;
            switch ((Attribute(keyValue, "type") ?? "string").ToLowerInvariant())
            {
                case "global":
                    _runtime.SetObjectFieldFromGlobal(value, key, rawValue);
                    break;
                case "number":
                    _runtime.SetObjectField(
                        value,
                        key,
                        double.TryParse(
                            rawValue,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var number)
                            ? number
                            : 0);
                    break;
                case "boolean":
                    _runtime.SetObjectField(value, key, ParseBoolean(rawValue));
                    break;
                default:
                    _runtime.SetObjectField(value, key, rawValue);
                    break;
            }
        }
    }

    private static void ApplyFrameAttributes(UiObject value, XElement attributes)
    {
        foreach (var attribute in attributes.Elements().Where(
                     child => child.Name.LocalName.Equals("Attribute", StringComparison.OrdinalIgnoreCase)))
        {
            var name = Attribute(attribute, "name");
            var rawValue = Attribute(attribute, "value");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            object? parsed = (Attribute(attribute, "type") ?? "string").ToLowerInvariant() switch
            {
                "number" => double.TryParse(
                    rawValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var number)
                    ? number
                    : 0,
                "boolean" => ParseBoolean(rawValue ?? string.Empty),
                "nil" => null,
                _ => rawValue ?? string.Empty
            };
            value.Attributes[name] = parsed;
        }
    }

    private void ApplyColor(UiObject value, XElement color)
    {
        var rgba = ReadColor(color);
        if (value.Texture is not null)
        {
            if (value.Texture.Asset is not null ||
                value.Texture.FileDataId is not null ||
                value.Texture.AtlasName is not null)
            {
                value.Texture.IsColor = false;
                value.Texture.VertexColor = rgba;
            }
            else
            {
                value.Texture.IsColor = true;
                value.Texture.Gradient = null;
                value.Texture.Color = rgba;
            }
        }
        if (value.Font is not null)
        {
            value.Font.Color = rgba;
            value.Font.LocalOverrides |= UiFontOverrides.Color;
        }
        if (value.Line is not null)
            value.Line.Color = rgba;
    }

    private void ApplyFontShadow(UiObject value, XElement shadow)
    {
        if (value.Font is not { } font)
            return;

        var offset = shadow.Elements().FirstOrDefault(child =>
            child.Name.LocalName.Equals("Offset", StringComparison.OrdinalIgnoreCase));
        if (offset is not null)
        {
            font.ShadowOffset = new System.Numerics.Vector2(
                Dimension(offset, "x") ?? font.ShadowOffset.X,
                Dimension(offset, "y") ?? font.ShadowOffset.Y);
            font.LocalOverrides |= UiFontOverrides.ShadowOffset;
        }

        var color = shadow.Elements().FirstOrDefault(child =>
            child.Name.LocalName.Equals("Color", StringComparison.OrdinalIgnoreCase));
        if (color is not null)
        {
            font.ShadowColor = ReadColor(color);
            font.LocalOverrides |= UiFontOverrides.ShadowColor;
        }
    }

    private static void ApplyFontHeight(UiObject value, XElement fontHeight)
    {
        if (value.Font is not { } font)
            return;

        var absolute = fontHeight.Descendants().FirstOrDefault(child =>
            child.Name.LocalName.Equals("AbsValue", StringComparison.OrdinalIgnoreCase));
        if (absolute is not null && Attribute(absolute, "val") is not null)
        {
            font.FontSize = Math.Max(0, NumberAttribute(absolute, "val", font.FontSize));
            font.LocalOverrides |= UiFontOverrides.FontSize;
        }
    }

    private void ApplyGradient(UiObject value, XElement gradient)
    {
        if (value.Texture is null)
            return;

        var minimum = gradient.Elements().FirstOrDefault(child =>
            child.Name.LocalName.Equals("MinColor", StringComparison.OrdinalIgnoreCase));
        var maximum = gradient.Elements().FirstOrDefault(child =>
            child.Name.LocalName.Equals("MaxColor", StringComparison.OrdinalIgnoreCase));
        if (minimum is null || maximum is null)
            return;

        value.Texture.IsColor = true;
        value.Texture.Asset = null;
        value.Texture.FileDataId = null;
        value.Texture.AtlasName = null;
        value.Texture.AtlasWidth = null;
        value.Texture.AtlasHeight = null;
        value.Texture.ClearAtlasRegion();
        value.Texture.Gradient = (
            Attribute(gradient, "orientation") ?? "HORIZONTAL",
            ReadColor(minimum),
            ReadColor(maximum));
    }

    private static void ApplyTexCoords(UiObject value, XElement texCoords)
    {
        if (value.Texture is not { } texture)
            return;

        var rect = texCoords.Elements().FirstOrDefault(child =>
            child.Name.LocalName.Equals("Rect", StringComparison.OrdinalIgnoreCase));
        var freeform = rect ?? texCoords;
        if (Attribute(freeform, "ulx") is not null ||
            Attribute(freeform, "uly") is not null ||
            Attribute(freeform, "llx") is not null ||
            Attribute(freeform, "lly") is not null ||
            Attribute(freeform, "urx") is not null ||
            Attribute(freeform, "ury") is not null ||
            Attribute(freeform, "lrx") is not null ||
            Attribute(freeform, "lry") is not null)
        {
            texture.LocalUv[0] = new System.Numerics.Vector2(
                NumberAttribute(freeform, "ulx", texture.LocalUv[0].X),
                NumberAttribute(freeform, "uly", texture.LocalUv[0].Y));
            texture.LocalUv[1] = new System.Numerics.Vector2(
                NumberAttribute(freeform, "llx", texture.LocalUv[1].X),
                NumberAttribute(freeform, "lly", texture.LocalUv[1].Y));
            texture.LocalUv[2] = new System.Numerics.Vector2(
                NumberAttribute(freeform, "urx", texture.LocalUv[2].X),
                NumberAttribute(freeform, "ury", texture.LocalUv[2].Y));
            texture.LocalUv[3] = new System.Numerics.Vector2(
                NumberAttribute(freeform, "lrx", texture.LocalUv[3].X),
                NumberAttribute(freeform, "lry", texture.LocalUv[3].Y));
        }

        if (Attribute(texCoords, "left") is { })
        {
            var left = NumberAttribute(texCoords, "left");
            texture.LocalUv[0].X = left;
            texture.LocalUv[1].X = left;
        }
        if (Attribute(texCoords, "right") is { })
        {
            var right = NumberAttribute(texCoords, "right");
            texture.LocalUv[2].X = right;
            texture.LocalUv[3].X = right;
        }
        if (Attribute(texCoords, "top") is { })
        {
            var top = NumberAttribute(texCoords, "top");
            texture.LocalUv[0].Y = top;
            texture.LocalUv[2].Y = top;
        }
        if (Attribute(texCoords, "bottom") is { })
        {
            var bottom = NumberAttribute(texCoords, "bottom");
            texture.LocalUv[1].Y = bottom;
            texture.LocalUv[3].Y = bottom;
        }
        texture.ResolveUv();
    }

    private void ApplyMaskedTextures(UiObject mask, XElement maskedTextures)
    {
        if (!mask.ObjectType.Equals("MaskTexture", StringComparison.OrdinalIgnoreCase) ||
            ParentOf(mask) is not { } owner)
        {
            return;
        }

        foreach (var element in maskedTextures.Elements().Where(child =>
                     child.Name.LocalName.Equals("MaskedTexture", StringComparison.OrdinalIgnoreCase)))
        {
            if (Attribute(element, "childKey") is { Length: > 0 } childKey)
                _pendingMaskLinks.Add(new PendingMaskLink(mask.Id, owner.Id, childKey));
        }
    }

    private void ResolvePendingMaskLinks()
    {
        for (var index = _pendingMaskLinks.Count - 1; index >= 0; index--)
        {
            var pending = _pendingMaskLinks[index];
            if (_runtime.Ui.Find(pending.MaskId) is not { } mask ||
                _runtime.Ui.Find(pending.OwnerId) is not { } owner ||
                ResolveObjectKeyPath(owner, pending.ChildKey) is not { Texture: not null } texture)
            {
                continue;
            }

            if (!texture.MaskTextureIds.Contains(mask.Id))
                texture.MaskTextureIds.Add(mask.Id);
            _pendingMaskLinks.RemoveAt(index);
        }
    }

    private UiObject? ResolveObjectKeyPath(UiObject current, string path)
    {
        foreach (var segment in path.Split(
                     '.',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Equals("$parent", StringComparison.OrdinalIgnoreCase))
            {
                current = ParentOf(current) ?? current;
                continue;
            }
            if (!_objectKeys.TryGetValue((current.Id, segment), out var childId) ||
                _runtime.Ui.Find(childId) is not { } child)
            {
                return null;
            }
            current = child;
        }
        return current;
    }

    private System.Numerics.Vector4 ReadColor(XElement color)
    {
        if (Attribute(color, "color") is { Length: > 0 } globalName &&
            _runtime.TryGetGlobalColor(globalName, out var globalColor))
        {
            return globalColor;
        }

        return new System.Numerics.Vector4(
            NumberAttribute(color, "r", 1),
            NumberAttribute(color, "g", 1),
            NumberAttribute(color, "b", 1),
            NumberAttribute(color, "a", 1));
    }

    private enum CooldownTextureKind
    {
        Swipe,
        Edge,
        Bling
    }

    private void ApplyCooldownTexture(
        UiCooldownState cooldown,
        XElement element,
        CooldownTextureKind kind)
    {
        var file = Attribute(element, "file");
        var colorElement = element.Elements().FirstOrDefault(child =>
            child.Name.LocalName.Equals("Color", StringComparison.OrdinalIgnoreCase));
        var color = colorElement is null
            ? file is null
                ? new Vector4(0, 1, 0, 1)
                : Vector4.One
            : QuantizeColor(ReadColor(colorElement));

        switch (kind)
        {
            case CooldownTextureKind.Swipe:
                if (file is not null)
                {
                    cooldown.SwipeTextureAsset = file;
                    cooldown.SwipeTextureFileDataId = null;
                }
                else
                {
                    cooldown.SwipeTextureAsset = null;
                    cooldown.SwipeTextureFileDataId = 0;
                }
                cooldown.SwipeColor = color;
                break;
            case CooldownTextureKind.Edge:
                if (file is not null)
                {
                    cooldown.EdgeTextureAsset = file;
                    cooldown.EdgeTextureFileDataId = null;
                }
                else
                {
                    cooldown.EdgeTextureAsset = null;
                    cooldown.EdgeTextureFileDataId = 0;
                }
                cooldown.EdgeColor = color;
                break;
            case CooldownTextureKind.Bling:
                if (file is not null)
                {
                    cooldown.BlingTextureAsset = file;
                    cooldown.BlingTextureFileDataId = null;
                }
                else
                {
                    cooldown.BlingTextureAsset = null;
                    cooldown.BlingTextureFileDataId = 0;
                }
                cooldown.BlingColor = color;
                break;
        }
    }

    private static System.Numerics.Vector4 QuantizeColor(System.Numerics.Vector4 color) =>
        new(
            QuantizeColorByte(color.X),
            QuantizeColorByte(color.Y),
            QuantizeColorByte(color.Z),
            QuantizeColorByte(color.W));

    private static float QuantizeColorByte(float value) =>
        MathF.Floor(Math.Clamp(value, 0, 1) * 255 + 0.5f) / 255;

    private void BindToParent(UiObject child, UiObject? parent, string? parentKey)
    {
        if (parent is null || string.IsNullOrWhiteSpace(parentKey))
            return;
        _objectKeys[(parent.Id, parentKey)] = child.Id;
        _runtime.SetObjectField(parent, parentKey, child);
    }

    private void BindToParentArray(UiObject child, UiObject? parent, string? parentArray)
    {
        if (parent is null || string.IsNullOrWhiteSpace(parentArray))
            return;
        _runtime.AppendObjectField(parent, parentArray, child);
    }

    private UiObject? ResolveExplicitParent(XElement element)
    {
        var parentName = Attribute(element, "parent");
        return string.IsNullOrWhiteSpace(parentName) ? null : _runtime.Ui.Find(parentName);
    }

    private UiObject? ResolveAnchorTarget(UiObject value, XElement anchor)
    {
        if (Attribute(anchor, "relativeTo") is { } relativeTo)
            return _runtime.Ui.Find(ExpandParentName(relativeTo, ParentOf(value)) ?? relativeTo);
        if (Attribute(anchor, "relativeKey") is not { } relativeKey)
            return null;

        var current = value;
        foreach (var segment in relativeKey.Split(
                     '.',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Equals("$parent", StringComparison.OrdinalIgnoreCase))
            {
                current = ParentOf(current) ?? current;
                continue;
            }
            if (!_objectKeys.TryGetValue((current.Id, segment), out var childId) ||
                _runtime.Ui.Find(childId) is not { } child)
            {
                return null;
            }
            current = child;
        }
        return current;
    }

    private UiObject? ParentOf(UiObject value) =>
        value.ParentId is { } parentId ? _runtime.Ui.Find(parentId) : null;

    private UiObject? ResolveAnimationTarget(UiObject animation, string? childKey)
    {
        var group = ParentOf(animation);
        var owner = group is null ? null : ParentOf(group);
        if (owner is null)
            return null;
        if (string.IsNullOrWhiteSpace(childKey))
            return owner;
        return _objectKeys.TryGetValue((owner.Id, childKey), out var childId)
            ? _runtime.Ui.Find(childId)
            : null;
    }

    private bool IsObjectElement(string elementName) =>
        ObjectElements.Contains(elementName) || _intrinsics.ContainsKey(elementName);

    private static void ApplySpecialRegionOwnership(
        UiObject owner,
        UiObject child,
        string elementName)
    {
        switch (elementName.ToLowerInvariant())
        {
            case "normaltexture":
                owner.NormalTextureId = child.Id;
                child.Shown = owner.Enabled && owner.ButtonState == UiButtonState.Normal;
                break;
            case "pushedtexture":
                owner.PushedTextureId = child.Id;
                child.Shown = owner.Enabled && owner.ButtonState == UiButtonState.Pushed;
                break;
            case "disabledtexture":
                owner.DisabledTextureId = child.Id;
                child.Shown = !owner.Enabled;
                break;
            case "highlighttexture":
                owner.HighlightTextureId = child.Id;
                child.DrawLayer = "HIGHLIGHT";
                break;
            case "checkedtexture":
                owner.CheckedTextureId = child.Id;
                child.Shown = owner.Checked && owner.Enabled;
                break;
            case "disabledcheckedtexture":
                owner.DisabledCheckedTextureId = child.Id;
                child.Shown = owner.Checked && !owner.Enabled;
                break;
            case "bartexture" when owner.StatusBar is { } statusBar:
                statusBar.TextureId = child.Id;
                child.AllPointsTargetId = owner.Id;
                break;
            case "thumbtexture":
                owner.ThumbTextureId = child.Id;
                child.AllPointsTargetId = null;
                break;
            case "colorwheeltexture" when owner.ColorSelect is { } colorSelect:
                colorSelect.WheelTextureId = child.Id;
                child.DrawLayer = "ARTWORK";
                if (child.Texture is { } wheel)
                    wheel.IsColorSelectWheel = true;
                break;
            case "colorwheelthumbtexture" when owner.ColorSelect is { } colorSelect:
                colorSelect.WheelThumbTextureId = child.Id;
                child.DrawLayer = "OVERLAY";
                break;
            case "colorvaluetexture" when owner.ColorSelect is { } colorSelect:
                colorSelect.ValueTextureId = child.Id;
                child.DrawLayer = "ARTWORK";
                break;
            case "colorvaluethumbtexture" when owner.ColorSelect is { } colorSelect:
                colorSelect.ValueThumbTextureId = child.Id;
                child.DrawLayer = "OVERLAY";
                break;
            case "coloralphatexture" when owner.ColorSelect is { } colorSelect:
                colorSelect.AlphaTextureId = child.Id;
                child.DrawLayer = "ARTWORK";
                break;
            case "coloralphathumbtexture" when owner.ColorSelect is { } colorSelect:
                colorSelect.AlphaThumbTextureId = child.Id;
                child.DrawLayer = "OVERLAY";
                break;
            case "buttontext":
                owner.ButtonFontStringId = child.Id;
                if (child.AllPointsTargetId is null && child.Anchors.Count == 0)
                    child.AllPointsTargetId = owner.Id;
                break;
        }
    }

    private string NormalizeObjectType(string elementName)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (_intrinsics.TryGetValue(elementName, out var intrinsic) &&
               !intrinsic.Element.Name.LocalName.Equals(
                   elementName,
                   StringComparison.OrdinalIgnoreCase) &&
               visited.Add(elementName))
        {
            elementName = intrinsic.Element.Name.LocalName;
        }

        return elementName.ToLowerInvariant() switch
        {
            "font" => "Font",
            "buttontext" => "FontString",
            "normaltexture" or "pushedtexture" or "disabledtexture" or
                "highlighttexture" or "checkedtexture" or "disabledcheckedtexture" or
                "bartexture" or "thumbtexture" or "blingtexture" or "coloralphatexture" or
                "coloralphathumbtexture" or "colorvaluetexture" or "colorvaluethumbtexture" or
                "colorwheeltexture" or "colorwheelthumbtexture" or "edgetexture" or
                "maskedtexture" or "swipetexture" => "Texture",
            "masktexture" => "MaskTexture",
            "actor" => "ModelSceneActor",
            "titleregion" => "Frame",
            "model" or "tabardmodel" => "PlayerModel",
            _ => elementName
        };
    }

    private void ProcessFontFamily(
        XElement family,
        AddonManifest manifest,
        string sourcePath)
    {
        var selectedAlphabet = LuaBindings.CurrentFontAlphabet(_runtime);
        var members = family.Elements().Where(child =>
            child.Name.LocalName.Equals("Member", StringComparison.OrdinalIgnoreCase)).ToArray();
        var selectedMember = members.FirstOrDefault(candidate =>
            ParseFontAlphabet(Attribute(candidate, "alphabet")) == selectedAlphabet);
        var selectedFont = selectedMember?.Elements().FirstOrDefault(child =>
            child.Name.LocalName.Equals("Font", StringComparison.OrdinalIgnoreCase));
        var selected = selectedFont is not null
            ? new XElement(selectedFont)
            : new XElement(family.Name.Namespace + "Font");
        foreach (var attributeName in new[] { "name", "virtual", "intrinsic" })
        {
            if (Attribute(family, attributeName) is { } value)
                selected.SetAttributeValue(attributeName, value);
        }

        ProcessTopLevel(selected, manifest, sourcePath);
        var familyName = Attribute(family, "name");
        if (string.IsNullOrWhiteSpace(familyName) ||
            _runtime.Ui.Find(familyName) is not { } familyObject)
        {
            return;
        }

        Array.Clear(familyObject.FontFamilyMemberIds);
        foreach (var member in members)
        {
            var alphabet = ParseFontAlphabet(Attribute(member, "alphabet"));
            if (alphabet < 0)
                continue;
            if (familyObject.FontFamilyMemberIds[alphabet] is not null)
            {
                _runtime.Log.Warn(
                    "xml",
                    $"FontFamily already includes member for alphabet {alphabet}");
                continue;
            }

            var font = member.Elements().FirstOrDefault(child =>
                child.Name.LocalName.Equals("Font", StringComparison.OrdinalIgnoreCase));
            if (font is null)
                continue;
            var memberDefinition = new XElement(font);
            memberDefinition.SetAttributeValue("name", null);
            memberDefinition.SetAttributeValue("virtual", null);
            memberDefinition.SetAttributeValue("intrinsic", null);
            var memberObject = Instantiate(
                memberDefinition,
                null,
                manifest,
                sourcePath,
                null,
                0);
            familyObject.FontFamilyMemberIds[alphabet] = memberObject.Id;
        }

        if (familyObject.FontFamilyMemberIds[selectedAlphabet] is { } selectedId &&
            _runtime.Ui.Find(selectedId) is { } selectedObject)
        {
            LuaBindings.AssignFontObject(_runtime, familyObject, selectedObject);
        }
        else
        {
            LuaBindings.AssignFontObject(_runtime, familyObject, null);
        }
    }

    private static int ParseFontAlphabet(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "roman" => 0,
            "korean" => 1,
            "simplifiedchinese" => 2,
            "traditionalchinese" => 3,
            "russian" => 4,
            _ => -1
        };

    private static void CopyFontStyle(UiFontState target, UiFontState source)
    {
        var overrides = target.LocalOverrides;
        if ((overrides & UiFontOverrides.FontPath) == 0)
        {
            target.FontPath = source.FontPath;
            target.IsConfigured = source.IsConfigured;
        }
        if ((overrides & UiFontOverrides.FontSize) == 0)
            target.FontSize = source.FontSize;
        if ((overrides & UiFontOverrides.FontFlags) == 0)
            target.FontFlags = source.FontFlags;
        if ((overrides & UiFontOverrides.TextScale) == 0)
            target.TextScale = source.TextScale;
        if ((overrides & UiFontOverrides.Color) == 0)
            target.Color = source.Color;
        if ((overrides & UiFontOverrides.ShadowColor) == 0)
            target.ShadowColor = source.ShadowColor;
        if ((overrides & UiFontOverrides.ShadowOffset) == 0)
            target.ShadowOffset = source.ShadowOffset;
        if ((overrides & UiFontOverrides.JustifyHorizontal) == 0)
            target.JustifyHorizontal = source.JustifyHorizontal;
        if ((overrides & UiFontOverrides.JustifyVertical) == 0)
            target.JustifyVertical = source.JustifyVertical;
        if ((overrides & UiFontOverrides.Spacing) == 0)
            target.Spacing = source.Spacing;
        if ((overrides & UiFontOverrides.MaximumLines) == 0)
            target.MaximumLines = source.MaximumLines;
        if ((overrides & UiFontOverrides.IndentedWordWrap) == 0)
            target.IndentedWordWrap = source.IndentedWordWrap;
        if ((overrides & UiFontOverrides.WordWrap) == 0)
            target.WordWrap = source.WordWrap;
        if ((overrides & UiFontOverrides.NonSpaceWrap) == 0)
            target.NonSpaceWrap = source.NonSpaceWrap;
        if ((overrides & UiFontOverrides.CanBeUserScaled) == 0)
            target.CanBeUserScaled = source.CanBeUserScaled;
        target.HasLocalJustifyHorizontal =
            (overrides & UiFontOverrides.JustifyHorizontal) != 0 ||
            source.HasLocalJustifyHorizontal;
        target.HasLocalJustifyVertical =
            (overrides & UiFontOverrides.JustifyVertical) != 0 ||
            source.HasLocalJustifyVertical;
    }

    private static string ReplaceFontFlag(string flags, string family, string value)
    {
        var values = flags.Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(flag => family.Equals("OUTLINE", StringComparison.OrdinalIgnoreCase)
                ? !flag.Contains("OUTLINE", StringComparison.OrdinalIgnoreCase)
                : !flag.Equals(family, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!string.IsNullOrWhiteSpace(value) &&
            !value.Equals("NONE", StringComparison.OrdinalIgnoreCase))
        {
            values.Add(value.ToUpperInvariant());
        }
        return string.Join(",", values);
    }

    private enum ButtonFontKind
    {
        Normal,
        Highlight,
        Disabled
    }

    private string? ExpandParentName(string? name, UiObject? parent)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        if (!name.Contains("$parent", StringComparison.OrdinalIgnoreCase))
            return name;
        while (parent is not null && string.IsNullOrWhiteSpace(parent.Name))
            parent = ParentOf(parent);
        if (parent is null)
            return null;
        return name.Replace(
            "$parent",
            parent.Name!,
            StringComparison.OrdinalIgnoreCase);
    }

    private string? ExpandObjectName(
        string? name,
        UiObject? parent,
        UiObject? templateNameRoot)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        if (!name.Contains("$parent", StringComparison.OrdinalIgnoreCase))
            return name;
        while (parent is not null)
        {
            if (!string.IsNullOrWhiteSpace(parent.Name))
            {
                return name.Replace(
                    "$parent",
                    parent.Name,
                    StringComparison.OrdinalIgnoreCase);
            }
            if (templateNameRoot is not null && parent.Id == templateNameRoot.Id)
                return null;
            parent = ParentOf(parent);
        }
        return null;
    }

    private string? ResolveInheritedAttribute(
        XElement element,
        string attributeName,
        HashSet<string> inheritanceStack)
    {
        string? resolved = null;
        foreach (var templateName in SplitList(Attribute(element, "inherits")))
        {
            if (!_templates.TryGetValue(templateName, out var template) ||
                !inheritanceStack.Add(templateName))
            {
                continue;
            }

            resolved = ResolveInheritedAttribute(
                template.Element,
                attributeName,
                inheritanceStack) ?? resolved;
            inheritanceStack.Remove(templateName);
        }

        return Attribute(element, attributeName) ?? resolved;
    }

    private static IEnumerable<string> SplitList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string? Attribute(XElement element, string name) =>
        element.Attributes()
            .FirstOrDefault(attribute =>
                attribute.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.Value;

    private static bool BooleanAttribute(XElement element, string name, bool fallback = false) =>
        Attribute(element, name) is { } value ? ParseBoolean(value) : fallback;

    private static bool ParseBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("1", StringComparison.OrdinalIgnoreCase);

    private static float NumberAttribute(XElement element, string name, float fallback = 0) =>
        Attribute(element, name) is { } value &&
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : fallback;

    private static float? OptionalNumberAttribute(XElement element, string name) =>
        Attribute(element, name) is { } value &&
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private static int IntegerAttribute(XElement element, string name, int fallback = 0) =>
        Attribute(element, name) is { } value &&
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : fallback;

    private static uint? UIntAttribute(XElement element, string name) =>
        Attribute(element, name) is { } value &&
        uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private static float? Dimension(XElement element, string name)
    {
        if (Attribute(element, name) is { } direct &&
            float.TryParse(direct, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        var dimension = element.Descendants().FirstOrDefault(child =>
            child.Name.LocalName.Equals("AbsDimension", StringComparison.OrdinalIgnoreCase) ||
            child.Name.LocalName.Equals("RelDimension", StringComparison.OrdinalIgnoreCase));
        return dimension is null || Attribute(dimension, name) is not { } nested ||
               !float.TryParse(nested, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            ? null
            : number;
    }

    private static string LuaString(string value) =>
        '"' + value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal) + '"';

    private static string XmlEventLocals(string scriptName) =>
        scriptName.ToLowerInvariant() switch
        {
            "onupdate" => "local elapsed = ...\n",
            "onevent" => "local event = ...\n",
            "onsizechanged" => "local width, height = ...\n",
            "onmousedown" or "onmouseup" => "local button = ...\n",
            "onclick" or "ondoubleclick" => "local button, down = ...\n",
            "onmousewheel" => "local delta = ...\n",
            "onvaluechanged" => "local value, userInput = ...\n",
            "onattributechanged" => "local name, value = ...\n",
            "onkeydown" or "onkeyup" or "onchar" => "local key = ...\n",
            "ontextchanged" or "ontextset" => "local userInput = ...\n",
            "onhyperlinkclick" or "onhyperlinkenter" or "onhyperlinkleave" =>
                "local link, text, button = ...\n",
            "onverticalscroll" or "onhorizontalscroll" => "local offset = ...\n",
            "onbuttonupdate" => "local action = ...\n",
            "onexternallink" => "local url = ...\n",
            "onerror" => "local msg = ...\n",
            _ => string.Empty
        };

    private sealed record XmlTemplate(
        XElement Element,
        string SourcePath,
        int? EnvironmentReference);

    private sealed record PendingMaskLink(int MaskId, int OwnerId, string ChildKey);

    private sealed class XmlScriptChain
    {
        public List<int> IntrinsicBefore { get; } = [];
        public List<int> Normal { get; } = [];
        public List<int> Hooks { get; } = [];
        public List<int> IntrinsicAfter { get; } = [];
        public int ExposedReference { get; set; }
    }
}
