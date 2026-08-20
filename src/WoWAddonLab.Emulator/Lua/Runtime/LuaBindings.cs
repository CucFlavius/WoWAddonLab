using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using WoWAddonLab.Emulator.Diagnostics;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal static class LuaBindings
{
    private const string ObjectMetatable = "WoWAddonLab.Emulator.WowObject";
    private const string RuntimeRegistryKey = "WoWAddonLab.Emulator.RuntimeId";
    private static readonly float[] StandardMinimapWorldRadii =
    [
        20 * (50f / 3f),
        14 * (50f / 3f),
        12 * (50f / 3f),
        10 * (50f / 3f),
        8 * (50f / 3f),
        6 * (50f / 3f)
    ];

    private static readonly ConcurrentDictionary<lua_State, LuaRuntime> Runtimes = new();
    private static readonly ConcurrentDictionary<long, LuaRuntime> RuntimesById = new();
    private static readonly ConcurrentDictionary<LuaRuntime, long> RuntimeIds = new();
    private static readonly ConcurrentDictionary<lua_State, HashSet<string>> ObjectMetatables = new();
    private static long _nextRuntimeId;
    private static readonly lua_CFunction GlobalCallback = DispatchGlobal;
    private static readonly lua_CFunction WidgetCallback = DispatchWidget;
    private static readonly HashSet<string> FrameStrataNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "WORLD",
        "BACKGROUND",
        "LOW",
        "MEDIUM",
        "HIGH",
        "DIALOG",
        "FULLSCREEN",
        "FULLSCREEN_DIALOG",
        "TOOLTIP",
        "BLIZZARD"
    };
    private static readonly HashSet<string> LayerNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "BACKGROUND",
        "BORDER",
        "ARTWORK",
        "OVERLAY",
        "HIGHLIGHT"
    };
    private static readonly HashSet<string> FramePointNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "TOPLEFT",
        "TOP",
        "TOPRIGHT",
        "LEFT",
        "CENTER",
        "RIGHT",
        "BOTTOMLEFT",
        "BOTTOM",
        "BOTTOMRIGHT"
    };
    private static readonly HashSet<int> VisibleArmorInventorySlots =
    [
        1, 3, 4, 5, 6, 7, 8, 9, 10, 15, 19
    ];
    private static readonly HashSet<int> VisibleAppearanceInventorySlots =
    [
        1, 3, 4, 5, 6, 7, 8, 9, 10, 15, 16, 17, 19
    ];
    private static readonly HashSet<string> RegionScriptNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "OnLoad", "OnShow", "OnHide", "OnEnter", "OnLeave", "OnMouseDown",
        "OnMouseUp", "OnMouseWheel"
    };
    private static readonly HashSet<string> FrameScriptNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "OnSizeChanged", "OnUpdate", "OnDragStart", "OnDragStop", "OnReceiveDrag",
        "OnChar", "OnHyperlinkClick", "OnHyperlinkEnter", "OnHyperlinkLeave",
        "OnEvent", "OnKeyDown", "OnKeyUp", "OnGamePadButtonDown",
        "OnGamePadButtonUp", "OnGamePadStick", "OnAttributeChanged", "OnEnable",
        "OnDisable"
    };
    private static readonly HashSet<string> ButtonScriptNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "OnClick", "PreClick", "PostClick", "OnDoubleClick"
    };
    private static readonly HashSet<string> EditBoxScriptNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "OnEnterPressed", "OnEscapePressed", "OnTabPressed", "OnEditFocusLost",
        "OnEditFocusGained", "OnTextChanged", "OnSpacePressed", "OnTextSet",
        "OnInputLanguageChanged", "OnCharComposition", "OnCursorChanged", "OnArrowPressed"
    };
    private static readonly HashSet<string> ModelSceneActorScriptNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "OnLoad", "OnUpdate", "OnModelLoaded", "OnModelCleared", "OnModelLoading",
        "OnAnimFinished"
    };
    private static readonly HashSet<string> AnimationScriptNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "OnLoad", "OnFinished", "OnPlay", "OnPause", "OnStop", "OnUpdate"
    };
    private static readonly HashSet<string> AnimationGroupScriptNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "OnLoad", "OnFinished", "OnPlay", "OnPause", "OnStop", "OnUpdate", "OnLoop"
    };

    private static readonly string[] GlobalFunctions =
    [
        "CreateFont", "CreateFontFamily", "CreateForbiddenFrame", "CreateFrame", "EnumerateFrames", "fastrandom", "GetBuildInfo", "GetButtonMetatable", "GetFontInfo", "GetFonts", "GetFontStringMetatable", "GetFrameMetatable", "strsplittable",
        "GetCurrentKeyBoardFocus",
        "GetPhysicalScreenSize", "GetScreenDPIScale", "GetScreenHeight", "GetScreenWidth"
    ];

    public static void Attach(LuaRuntime runtime)
    {
        var id = Interlocked.Increment(ref _nextRuntimeId);
        Runtimes[runtime.State] = runtime;
        RuntimesById[id] = runtime;
        RuntimeIds[runtime] = id;
        ObjectMetatables[runtime.State] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        lua_pushnumber(runtime.State, id);
        lua_setfield(runtime.State, LUA_REGISTRYINDEX, RuntimeRegistryKey);
    }

    public static void Detach(LuaRuntime runtime)
    {
        foreach (var state in Runtimes
                     .Where(pair => ReferenceEquals(pair.Value, runtime))
                     .Select(pair => pair.Key))
        {
            Runtimes.TryRemove(state, out _);
            ObjectMetatables.TryRemove(state, out _);
        }
        if (RuntimeIds.TryRemove(runtime, out var id))
            RuntimesById.TryRemove(id, out _);
    }

    public static void Register(lua_State state)
    {
        LuaApiCatalog.Bootstrap.RegisterAll(state);
        foreach (var function in GlobalFunctions)
            RegisterClosureGlobal(state, function, GlobalCallback);
        LuaApiCatalog.Game.RegisterAll(state);

        RegisterNamespace(
            state,
            "C_EncounterEvents",
            "PlayEventSound", "SetEventColor", "SetEventSound");
        RegisterEditModeConstants(state);

    }

    public static UiObject CreateObject(
        LuaRuntime runtime,
        string objectType,
        string? name,
        UiObject? parent,
        string? drawLayer = null,
        int subLevel = 0)
    {
        name = ExpandParentObjectName(runtime, name, parent);
        var value = runtime.Ui.Create(objectType, name, parent?.Id, drawLayer, subLevel);
        value.AddonName = runtime.CurrentAddonName;
        var state = runtime.State;
        lua_newtable(state);
        lua_pushlightuserdata(state, (nuint)value.Id);
        lua_rawseti(state, -2, 0);
        lua_pushinteger(state, value.Id);
        lua_setfield(state, -2, "__id");
        lua_pushstring(state, objectType);
        lua_setfield(state, -2, "__type");
        var metatableName = EnsureObjectMetatable(state, objectType);
        luaL_getmetatable(state, metatableName);
        lua_setmetatable(state, -2);
        lua_pushvalue(state, -1);
        value.LuaReference = luaL_ref(state, LUA_REGISTRYINDEX);
        lua_pop(state, 1);
        return value;
    }

    private static string? ExpandParentObjectName(
        LuaRuntime runtime,
        string? name,
        UiObject? parent)
    {
        if (name is null || !name.Contains("$parent", StringComparison.OrdinalIgnoreCase))
            return name;

        while (parent is not null && string.IsNullOrWhiteSpace(parent.Name))
        {
            parent = parent.ParentId is { } parentId
                ? runtime.Ui.Find(parentId)
                : null;
        }
        return parent?.Name is { } parentName
            ? name.Replace("$parent", parentName, StringComparison.OrdinalIgnoreCase)
            : null;
    }

    private static string EnsureObjectMetatable(lua_State state, string objectType)
    {
        var metatableName = $"{ObjectMetatable}.{objectType.ToLowerInvariant()}";
        var registered = ObjectMetatables.GetOrAdd(
            state,
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        if (!registered.Add(metatableName))
            return metatableName;

        luaL_newmetatable(state, metatableName);
        lua_newtable(state);
        foreach (var method in WowWidgetApi.MethodsFor(objectType))
        {
            lua_pushstring(state, method);
            lua_pushcclosure(state, WidgetCallback, 1);
            lua_setfield(state, -2, method);
        }
        lua_setfield(state, -2, "__index");
        lua_pop(state, 1);
        return metatableName;
    }

    public static void SetGlobalObject(LuaRuntime runtime, UiObject value)
    {
        if (value.Name is null)
            return;
        runtime.PushObject(value);
        lua_setglobal(runtime.State, value.Name);
    }

    private static int DispatchGlobal(lua_State state)
    {
        var runtime = GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "CreateFont":
                {
                    if (!TryReadRequiredString(state, 1, out var name))
                        return luaL_error(state, "Usage: CreateFont(\"name\")");
                    var font = FindFontByName(runtime, name);
                    if (font is null)
                    {
                        font = CreateObject(runtime, "Font", name, null);
                        SetGlobalObject(runtime, font);
                    }
                    runtime.PushObject(font);
                    return 1;
                }
            case "CreateFontFamily":
                return CreateFontFamily(runtime);
            case "CreateFrame":
                return CreateFrame(runtime);
            case "CreateForbiddenFrame":
                return CreateFrame(runtime, true);
            case "fastrandom":
                return FastRandom(state);
            case "strsplittable":
                return StringSplitTable(state);
            case "GetBuildInfo":
                lua_pushstring(state, runtime.BuildInfo.Version);
                lua_pushstring(state, runtime.BuildInfo.Build);
                lua_pushstring(state, runtime.BuildInfo.Date);
                lua_pushinteger(state, runtime.BuildInfo.InterfaceVersion);
                return 4;
            case "SetEventColor":
                {
                    if (lua_type(state, 1) != LUA_TNUMBER ||
                        lua_type(state, 2) != LUA_TNUMBER)
                    {
                        return luaL_error(
                            state,
                            "Usage: C_EncounterEvents.SetEventColor(encounterEventID, trigger [, color])");
                    }

                    var trigger = (int)lua_tonumber(state, 2);
                    if (trigger is < 0 or > 2 ||
                        (lua_gettop(state) >= 3 &&
                         lua_type(state, 3) is not LUA_TNIL and not LUA_TTABLE))
                    {
                        return luaL_error(
                            state,
                            "Usage: C_EncounterEvents.SetEventColor(encounterEventID, trigger [, color])");
                    }
                    return 0;
                }
            case "GetFontInfo":
                {
                    UiObject? fontObject;
                    if (lua_istable(state, 1) != 0)
                    {
                        fontObject = GetObject(runtime, 1);
                    }
                    else if (TryReadRequiredString(state, 1, out var fontName))
                    {
                        fontObject = FindFontByName(runtime, fontName);
                        if (fontObject is null)
                        {
                            lua_pushnil(state);
                            return 1;
                        }
                    }
                    else
                    {
                        return luaL_error(state, "Usage: local info = GetFontInfo(fontObject)");
                    }

                    if (fontObject?.Font is not { } font ||
                        !fontObject.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase))
                        return luaL_error(state, "Usage: local info = GetFontInfo(fontObject)");

                    lua_newtable(state);
                    PushColorMixin(state, font.Color);
                    lua_setfield(state, -2, "color");
                    SetTableNumber(
                        state,
                        "height",
                        font.IsConfigured ? (int)MathF.Truncate(font.FontSize) : 0);
                    SetTableString(state, "outline", FormatFontFlags(font.FontFlags));
                    if (font.ShadowOffset != Vector2.Zero)
                    {
                        lua_newtable(state);
                        PushColorMixin(state, font.ShadowColor);
                        lua_setfield(state, -2, "color");
                        SetTableNumber(state, "x", font.ShadowOffset.X);
                        SetTableNumber(state, "y", font.ShadowOffset.Y);
                        lua_setfield(state, -2, "shadow");
                    }
                    runtime.PushObject(fontObject);
                    lua_setfield(state, -2, "fontObject");
                    SetTableBoolean(state, "canBeUserScaled", font.CanBeUserScaled);
                    return 1;
                }
            case "GetFonts":
                {
                    lua_newtable(state);
                    var resultIndex = 1;
                    foreach (var font in runtime.Ui.Objects.Values
                                 .Where(candidate =>
                                     candidate.Name is not null &&
                                     candidate.ObjectType.Equals(
                                         "Font",
                                         StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(candidate => candidate.Id))
                    {
                        lua_pushstring(state, font.Name!);
                        lua_rawseti(state, -2, resultIndex++);
                    }
                    return 1;
                }
            case "GetFrameMetatable":
                EnsureObjectMetatable(state, "Frame");
                luaL_getmetatable(state, $"{ObjectMetatable}.frame");
                return 1;
            case "GetButtonMetatable":
                EnsureObjectMetatable(state, "Button");
                luaL_getmetatable(state, $"{ObjectMetatable}.button");
                return 1;
            case "GetFontStringMetatable":
                EnsureObjectMetatable(state, "FontString");
                luaL_getmetatable(state, $"{ObjectMetatable}.fontstring");
                return 1;
            case "GetSpellInfo":
                {
                    var spellId = (int)OptionalNumber(state, 1);
                    lua_newtable(state);
                    lua_pushstring(state, $"Spell #{spellId}");
                    lua_setfield(state, -2, "name");
                    lua_pushinteger(state, 136243);
                    lua_setfield(state, -2, "iconID");
                    lua_pushinteger(state, spellId);
                    lua_setfield(state, -2, "spellID");
                    return 1;
                }
            case "IsToyUsable":
            case "SetEventSound":
                return 0;
            case "PlayEventSound":
                lua_pushinteger(state, runtime.NextSoundHandle());
                return 1;
            case "GetCurrentKeyBoardFocus":
                runtime.PushObject(
                    runtime.Ui.FocusedObjectId is { } focusedId
                        ? runtime.Ui.Find(focusedId)
                        : null);
                return 1;
            case "EnumerateFrames":
                {
                    UiObject? current = null;
                    if (lua_type(state, 1) == LUA_TTABLE)
                    {
                        current = GetObject(runtime, 1);
                        if (current is null)
                        {
                            return luaL_error(
                                state,
                                "EnumerateFrames: Couldn't find 'this' in current object");
                        }
                        if (!IsFrameObject(current))
                        {
                            return luaL_error(
                                state,
                                "EnumerateFrames: Wrong current object type, expected frame");
                        }
                    }

                    UiObject? next = null;
                    var firstCandidateId = current?.Id + 1 ?? 1;
                    for (var objectId = firstCandidateId;
                         objectId <= runtime.Ui.LastObjectId;
                         objectId++)
                    {
                        if (runtime.Ui.Objects.TryGetValue(objectId, out var candidate) &&
                            IsFrameObject(candidate) &&
                            !candidate.Forbidden)
                        {
                            next = candidate;
                            break;
                        }
                    }
                    runtime.PushObject(next);
                    return 1;
                }
            case "GetPhysicalScreenSize":
                lua_pushnumber(state, runtime.Ui.PhysicalWidth);
                lua_pushnumber(state, runtime.Ui.PhysicalHeight);
                return 2;
            case "GetScreenDPIScale":
                lua_pushnumber(state, runtime.Ui.ScreenDpiScale);
                return 1;
            case "GetScreenWidth":
                lua_pushnumber(state, runtime.Ui.LogicalWidth);
                return 1;
            case "GetScreenHeight":
                lua_pushnumber(state, runtime.Ui.LogicalHeight);
                return 1;
            default:
                return 0;
        }
    }

    private static int CreateFontFamily(LuaRuntime runtime)
    {
        const string usage =
            "Usage: local fontFamily = CreateFontFamily(name, members)";
        var state = runtime.State;
        if (!TryReadRequiredString(state, 1, out var name) ||
            lua_istable(state, 2) == 0)
        {
            return luaL_error(state, usage);
        }

        if (FindFontByName(runtime, name) is not null)
        {
            return luaL_error(
                state,
                $"Attempted to create a FontFamily object with a duplicate name '{name}'");
        }

        var memberCount = checked((int)lua_objlen(state, 2));
        if (memberCount != 5)
        {
            return luaL_error(
                state,
                "Attempted to create a FontFamily object with an unexpected number " +
                $"of member fonts (got {memberCount}, expected 5)");
        }

        var members = new FontFamilyMember[5];
        var seen = new bool[5];
        for (var index = 1; index <= memberCount; index++)
        {
            lua_rawgeti(state, 2, index);
            if (lua_istable(state, -1) == 0 ||
                !TryReadFontFamilyMember(state, -1, out var member))
            {
                lua_pop(state, 1);
                return luaL_error(state, usage);
            }
            lua_pop(state, 1);

            if (seen[member.Alphabet])
            {
                return luaL_error(
                    state,
                    "Attempted to create a FontFamily object with a duplicate member " +
                    $"font for alphabet '{FontAlphabetName(member.Alphabet)}'");
            }
            if (member.Height <= 0)
            {
                return luaL_error(
                    state,
                    "Attempted to create a FontFamily object with an invalid font height " +
                    $"for alphabet '{FontAlphabetName(member.Alphabet)}' (must be > 0)");
            }

            seen[member.Alphabet] = true;
            members[member.Alphabet] = member;
        }

        var family = CreateObject(runtime, "Font", name, null);
        SetGlobalObject(runtime, family);
        for (var alphabet = 0; alphabet < members.Length; alphabet++)
        {
            var member = members[alphabet];
            var memberFont = CreateObject(runtime, "Font", null, null);
            memberFont.Font = new UiFontState
            {
                FontPath = member.File,
                FontSize = MathF.Min(member.Height, 120),
                FontFlags = member.Flags,
                IsConfigured = true,
                LocalOverrides = UiFontOverrides.FontPath |
                                 UiFontOverrides.FontSize |
                                 UiFontOverrides.FontFlags
            };
            family.FontFamilyMemberIds[alphabet] = memberFont.Id;
        }

        if (family.FontFamilyMemberIds[CurrentFontAlphabet(runtime)] is { } selectedId &&
            runtime.Ui.Find(selectedId) is { } selected)
        {
            AssignFontObject(runtime, family, selected);
        }

        runtime.PushObject(family);
        return 1;
    }

    private static UiObject? FindFontByName(LuaRuntime runtime, string name) =>
        runtime.Ui.Objects.Values
            .Where(value =>
                value.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true &&
                value.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase))
            .OrderBy(value => value.Id)
            .FirstOrDefault();

    private static bool TryReadFontFamilyMember(
        lua_State state,
        int tableIndex,
        out FontFamilyMember member)
    {
        member = default;
        var absolute = AbsoluteIndex(state, tableIndex);

        lua_getfield(state, absolute, "alphabet");
        var alphabetText = lua_type(state, -1) == LUA_TSTRING
            ? lua_tostring(state, -1)
            : null;
        var validAlphabet = TryParseFontAlphabet(alphabetText, out var alphabet);
        lua_pop(state, 1);

        lua_getfield(state, absolute, "file");
        var validFile = TryReadRequiredString(state, -1, out var file);
        lua_pop(state, 1);

        lua_getfield(state, absolute, "height");
        var validHeight = lua_isnumber(state, -1) != 0;
        var height = validHeight ? (float)lua_tonumber(state, -1) : 0;
        lua_pop(state, 1);

        lua_getfield(state, absolute, "flags");
        var flags = string.Empty;
        var validFlags = TryReadRequiredString(state, -1, out var flagsText) &&
                         TryNormalizeFontFlags(flagsText, out flags);
        lua_pop(state, 1);

        if (!validAlphabet || !validFile || !validHeight || !validFlags)
            return false;
        member = new FontFamilyMember(alphabet, file, height, flags);
        return true;
    }

    private readonly record struct FontFamilyMember(
        int Alphabet,
        string File,
        float Height,
        string Flags);

    private static int DispatchWidget(lua_State state)
    {
        var runtime = GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var value = GetObject(runtime, state, 1);
        if (value is null)
            return luaL_error(state, $"{operation}: invalid UI object");

        switch (operation)
        {
            case "ClearColorWheelTexture":
                {
                    var colorSelect = EnsureColorSelect(value);
                    if (ColorSelectTexture(runtime, colorSelect.WheelTextureId) is { } wheel)
                        wheel.Shown = false;
                    colorSelect.WheelTextureId = null;
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "GetColorAlpha":
                lua_pushnumber(state, EnsureColorSelect(value).Alpha);
                return 1;
            case "GetColorHSV":
                {
                    var colorSelect = EnsureColorSelect(value);
                    lua_pushnumber(state, colorSelect.Hue);
                    lua_pushnumber(state, colorSelect.Saturation);
                    lua_pushnumber(state, colorSelect.Value);
                    return 3;
                }
            case "GetColorRGB":
                {
                    var rgb = QuantizedColorSelectRgb(EnsureColorSelect(value));
                    lua_pushnumber(state, rgb.X);
                    lua_pushnumber(state, rgb.Y);
                    lua_pushnumber(state, rgb.Z);
                    return 3;
                }
            case "GetColorWheelTexture":
                return PushColorSelectTexture(
                    runtime,
                    EnsureColorSelect(value).WheelTextureId);
            case "GetColorWheelThumbTexture":
                return PushColorSelectTexture(
                    runtime,
                    EnsureColorSelect(value).WheelThumbTextureId);
            case "GetColorValueTexture":
                return PushColorSelectTexture(
                    runtime,
                    EnsureColorSelect(value).ValueTextureId);
            case "GetColorValueThumbTexture":
                return PushColorSelectTexture(
                    runtime,
                    EnsureColorSelect(value).ValueThumbTextureId);
            case "GetColorAlphaTexture":
                return PushColorSelectTexture(
                    runtime,
                    EnsureColorSelect(value).AlphaTextureId);
            case "GetColorAlphaThumbTexture":
                return PushColorSelectTexture(
                    runtime,
                    EnsureColorSelect(value).AlphaThumbTextureId);
            case "SetColorAlpha":
                {
                    if (!TryReadRequiredFloat(state, 2, out var alpha))
                        return luaL_error(state, "Usage: self:SetColorAlpha(alpha)");
                    EnsureColorSelect(value).Alpha = Math.Clamp((float)alpha, 0, 1);
                    CommitColorSelect(runtime, value);
                    return 0;
                }
            case "SetColorHSV":
                {
                    const string usage = "Usage: self:SetColorHSV(hsv)";
                    if (!TryReadRequiredFloat(state, 2, out var hue) ||
                        !TryReadRequiredFloat(state, 3, out var saturation) ||
                        !TryReadRequiredFloat(state, 4, out var colorValue))
                    {
                        return luaL_error(state, usage);
                    }
                    var colorSelect = EnsureColorSelect(value);
                    colorSelect.Hue = Math.Clamp((float)hue, 0, 360);
                    colorSelect.Saturation = Math.Clamp((float)saturation, 0, 1);
                    colorSelect.Value = Math.Clamp((float)colorValue, 0, 1);
                    CommitColorSelect(runtime, value);
                    return 0;
                }
            case "SetColorRGB":
                {
                    const string usage = "Usage: self:SetColorRGB(rgb)";
                    if (!TryReadRequiredFloat(state, 2, out var red) ||
                        !TryReadRequiredFloat(state, 3, out var green) ||
                        !TryReadRequiredFloat(state, 4, out var blue))
                    {
                        return luaL_error(state, usage);
                    }
                    var rgb = new Vector3(
                        QuantizeColorSelectInput((float)red),
                        QuantizeColorSelectInput((float)green),
                        QuantizeColorSelectInput((float)blue));
                    SetColorSelectRgb(EnsureColorSelect(value), rgb);
                    CommitColorSelect(runtime, value);
                    return 0;
                }
            case "SetColorWheelTexture":
                return SetColorSelectSourceTexture(
                    runtime,
                    value,
                    ColorSelectTexturePart.Wheel);
            case "SetColorValueTexture":
                return SetColorSelectSourceTexture(
                    runtime,
                    value,
                    ColorSelectTexturePart.Value);
            case "SetColorAlphaTexture":
                return SetColorSelectSourceTexture(
                    runtime,
                    value,
                    ColorSelectTexturePart.Alpha);
            case "SetColorWheelThumbTexture":
                return SetColorSelectThumbTexture(
                    runtime,
                    value,
                    ColorSelectTexturePart.WheelThumb);
            case "SetColorValueThumbTexture":
                return SetColorSelectThumbTexture(
                    runtime,
                    value,
                    ColorSelectTexturePart.ValueThumb);
            case "SetColorAlphaThumbTexture":
                return SetColorSelectThumbTexture(
                    runtime,
                    value,
                    ColorSelectTexturePart.AlphaThumb);
            case "NavigateHome":
                {
                    if (!TryReadRequiredString(state, 2, out var browserPage))
                        return luaL_error(state, "Usage: self:NavigateHome(urlType)");
                    value.BrowserPage = browserPage.ToUpperInvariant() switch
                    {
                        "KNOWLEDGEBASE" => "KnowledgeBase",
                        "GMTICKET" => "GMTicket",
                        "GMTICKETSTATUS" => "GMTicketStatus",
                        "PHOTOSHARING" => "PhotoSharing",
                        _ => value.BrowserPage
                    };
                    return 0;
                }
            case "OpenTicket":
                if (!TryReadRequiredUInt32(state, 2, out var browserTicketIndex))
                    return luaL_error(state, "Usage: self:OpenTicket(index)");
                value.BrowserTicketIndex = browserTicketIndex;
                value.BrowserPage = "GMTicketStatus";
                return 0;
            case "NavigateTo":
                if (!TryReadRequiredString(state, 2, out _))
                    return luaL_error(state, "Usage: self:NavigateTo(url)");
                return 0;
            case "NavigateBack":
            case "NavigateForward":
            case "NavigateReload":
            case "NavigateStop":
            case "OpenExternalLink":
            case "CopyExternalLink":
            case "DeleteCookies":
                return 0;
            case "CancelOpenCheckout":
                value.CheckoutOpen = false;
                return 0;
            case "CloseCheckout":
                value.CheckoutOpen = false;
                return 0;
            case "OpenCheckout":
                {
                    const string usage =
                        "Usage: local wasOpened = self:OpenCheckout(checkoutID)";
                    if (!TryReadRequiredInt32(state, 2, out var checkoutId))
                        return luaL_error(state, usage);

                    value.CheckoutLastRequestedId = checkoutId;
                    value.CheckoutOpen = false;
                    lua_pushboolean(state, 0);
                    return 1;
                }
            case "EnableSubtitles":
                {
                    const string usage = "Usage: self:EnableSubtitles(enable)";
                    if (!TryReadRequiredBoolean(state, 2, out var enableSubtitles))
                        return luaL_error(state, usage);
                    EnsureMovie(value).SubtitlesEnabled = enableSubtitles;
                    return 0;
                }
            case "StartMovie":
                {
                    const string usage =
                        "Usage: local success, returnCode = self:StartMovie(movieID [, looping])";
                    if (!TryReadRequiredInt32(state, 2, out var movieId))
                        return luaL_error(state, usage);

                    var movie = EnsureMovie(value);
                    movie.RequestedMovieId = movieId;
                    movie.Looping = OptionalBoolean(state, 3, false);
                    movie.Playing = false;
                    movie.ReturnCode = 2;
                    lua_pushboolean(state, 0);
                    lua_pushinteger(state, movie.ReturnCode);
                    return 2;
                }
            case "StartMovieByName":
                {
                    const string usage =
                        "Usage: local success, returnCode = self:StartMovieByName(movieName [, looping, resolution])";
                    if (!TryReadRequiredString(state, 2, out _) ||
                        !TryReadOptionalInt32(state, 4, 0, out _))
                    {
                        return luaL_error(state, usage);
                    }

                    _ = OptionalBoolean(state, 3, false);
                    lua_pushboolean(state, 1);
                    lua_pushinteger(state, 0);
                    return 2;
                }
            case "StopMovie":
                {
                    var movie = EnsureMovie(value);
                    movie.RequestedMovieId = null;
                    movie.Looping = false;
                    movie.Playing = false;
                    movie.ReturnCode = 0;
                    return 0;
                }
            case "CreateTexture":
                return CreateFrameRegion(
                    runtime,
                    value,
                    "Texture",
                    "Usage: local texture = self:CreateTexture([name, drawLayer, templateName, subLevel])",
                    supportsSubLevel: true);
            case "CreateMaskTexture":
                return CreateFrameRegion(
                    runtime,
                    value,
                    "MaskTexture",
                    "Usage: local maskTexture = self:CreateMaskTexture([name, drawLayer, templateName, subLevel])",
                    supportsSubLevel: true);
            case "CreateFontString":
                return CreateFrameRegion(
                    runtime,
                    value,
                    "FontString",
                    "Usage: local line = self:CreateFontString([name, drawLayer, templateName, subLevel])",
                    supportsSubLevel: false);
            case "CreateLine":
                return CreateFrameRegion(
                    runtime,
                    value,
                    "Line",
                    "Usage: local line = self:CreateLine([name, drawLayer, templateName, subLevel])",
                    supportsSubLevel: true);
            case "CreateActor":
                return CreateModelSceneActor(runtime, value);
            case "GetActorAtIndex":
                {
                    if (lua_isnumber(state, 2) == 0)
                    {
                        runtime.Log.Warn("ui", "GetActorAtIndex: Invalid index passed");
                        return 0;
                    }

                    var requestedIndex = unchecked((int)lua_tonumber(state, 2)) - 1;
                    if (requestedIndex < 0)
                        return 0;
                    var actor = value.Children
                        .Select(runtime.Ui.Find)
                        .Where(child => child is not null &&
                                        child.ObjectType.Equals(
                                            "ModelSceneActor",
                                            StringComparison.OrdinalIgnoreCase))
                        .ElementAtOrDefault(requestedIndex);
                    if (actor is null)
                        return 0;
                    runtime.PushObject(actor);
                    return 1;
                }
            case "GetNumActors":
                lua_pushinteger(
                    state,
                    value.Children.Count(childId =>
                        runtime.Ui.Find(childId) is { } child &&
                        child.ObjectType.Equals(
                            "ModelSceneActor",
                            StringComparison.OrdinalIgnoreCase)));
                return 1;
            case "TakeActor":
                {
                    var actor = GetObject(runtime, 2);
                    if (actor is null ||
                        !actor.ObjectType.Equals(
                            "ModelSceneActor",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, "Usage: self:TakeActor(actor)");
                    }

                    runtime.Ui.Reparent(actor, value.Id);
                    return 0;
                }
            case "CreateAnimationGroup":
                {
                    const string usage =
                        "Usage: local group = self:CreateAnimationGroup([name, templateName])";
                    if (!TryReadOptionalString(state, 2, out var name) ||
                        !TryReadOptionalString(state, 3, out var templateName))
                    {
                        return luaL_error(state, usage);
                    }

                    var group = CreateObject(runtime, "AnimationGroup", name, value);
                    runtime.ApplyXmlTemplates(group, value, templateName);
                    runtime.PushObject(group);
                    return 1;
                }
            case "GetAnimationGroups":
                {
                    var animationGroups = value.Children
                        .Select(runtime.Ui.Find)
                        .Where(child => child?.AnimationGroup is not null)
                        .Cast<UiObject>()
                        .ToArray();
                    foreach (var animationGroup in animationGroups)
                        runtime.PushObject(animationGroup);
                    return animationGroups.Length;
                }
            case "StopAnimating":
                foreach (var animationGroup in value.Children
                             .Select(runtime.Ui.Find)
                             .Where(child => child?.AnimationGroup is not null)
                             .Cast<UiObject>())
                {
                    runtime.StopAnimationGroup(animationGroup, false);
                }
                return 0;
            case "CreateAnimation":
                {
                    var requestedType = OptionalString(state, 2);
                    var animationType = requestedType?.ToUpperInvariant() switch
                    {
                        "ALPHA" => "Alpha",
                        "FLIPBOOK" => "FlipBook",
                        "LINESCALE" => "LineScale",
                        "LINETRANSLATION" => "LineTranslation",
                        "PATH" => "Path",
                        "ROTATION" => "Rotation",
                        "SCALE" => "Scale",
                        "TEXTURECOORD" => "TextureCoordTranslation",
                        "TRANSLATION" => "Translation",
                        "VERTEXCOLOR" => "VertexColor",
                        _ => "Animation"
                    };
                    var animation = CreateObject(
                        runtime,
                        animationType,
                        OptionalString(state, 3),
                        value);
                    runtime.PushObject(animation);
                    return 1;
                }
            case "CreateControlPoint":
                {
                    if (value.Animation is null ||
                        !value.ObjectType.Equals("Path", StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(
                            state,
                            "Usage: local point = self:CreateControlPoint([name, templateName, order])");
                    }

                    int? requestedOrder = null;
                    if (HasRequiredValue(state, 4))
                    {
                        if (!TryReadRequiredInt32(state, 4, out var parsedOrder))
                        {
                            return luaL_error(
                                state,
                                "Usage: local point = self:CreateControlPoint([name, templateName, order])");
                        }
                        requestedOrder = Math.Clamp(parsedOrder, 0, 99);
                    }

                    var point = CreateObject(
                        runtime,
                        "ControlPoint",
                        OptionalString(state, 2),
                        value);
                    if (point.Name is not null)
                        SetGlobalObject(runtime, point);
                    runtime.ApplyXmlTemplates(point, value, OptionalString(state, 3));

                    if (requestedOrder is { } order)
                    {
                        point.ControlPoint!.Order = order;
                    }
                    else
                    {
                        var existing = runtime.Ui.ResolvePathControlPoints(value);
                        point.ControlPoint!.Order = existing.Count == 0
                            ? 99
                            : existing[^1].ControlPoint!.Order is >= 0 and <= 99
                                ? existing[^1].ControlPoint!.Order
                                : 99;
                    }
                    runtime.Ui.ResolvePathControlPoints(value);
                    runtime.PushObject(point);
                    return 1;
                }
            case "SetScript":
                return SetScript(runtime, value);
            case "HookScript":
                return HookScript(runtime, value);
            case "GetScript":
                {
                    if (lua_type(state, 2) != LUA_TSTRING)
                    {
                        runtime.Log.Warn(
                            "ui",
                            "GetScript: Usage: (\"frameScriptTypeName\"[, bindingType])");
                        return 0;
                    }
                    var name = lua_tostring(state, 2)!;
                    if (!SupportsScript(value, name))
                    {
                        runtime.Log.Warn("ui", $"GetScript: Doesn't have a \"{name}\" script");
                        return 0;
                    }
                    if (runtime.TryGetScript(value, name, out var reference))
                        lua_rawgeti(state, LUA_REGISTRYINDEX, reference);
                    else
                        lua_pushnil(state);
                    return 1;
                }
            case "HasScript":
                {
                    if (!HasRequiredValue(state, 2) ||
                        !TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local hasScript = self:HasScript(scriptName)");
                    }
                    lua_pushboolean(state, SupportsScript(value, name) ? 1 : 0);
                    return 1;
                }
            case "RegisterEvent":
                {
                    if (!TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local registered = self:RegisterEvent(eventName)");
                    }

                    var registered = value.Events.Add(name) ||
                                     value.RegisteredUnitEvents.ContainsKey(name);
                    value.RegisteredUnitEvents.Remove(name);
                    runtime.IndexEventTarget(value, name);
                    lua_pushboolean(state, registered ? 1 : 0);
                    return 1;
                }
            case "RegisterEventCallback":
                {
                    if (!TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local registered = self:RegisterEventCallback(eventName, cb)");
                    }
                    if (!WowFunctionContainersApi.IsCallbackEvent(name))
                        return 0;
                    if (lua_isfunction(state, 3) == 0)
                    {
                        return luaL_error(
                            state,
                            "Usage: local registered = self:RegisterEventCallback(eventName, cb)");
                    }

                    if (!value.EventCallbackReferences.TryGetValue(name, out var callbacks))
                    {
                        callbacks = [];
                        value.EventCallbackReferences.Add(name, callbacks);
                    }
                    callbacks.Add(new UiFrameEventCallback(runtime.CaptureFunction(state, 3), []));
                    runtime.IndexEventTarget(value, name);
                    lua_pushboolean(state, 1);
                    return 1;
                }
            case "RegisterUnitEvent":
                {
                    if (!TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: (\"event\", \"unit1\" [,\"unit2\", \"unit3\", \"unit4\"])");
                    }

                    var units = new List<string>(4);
                    var argumentCount = Math.Min(lua_gettop(state) - 2, 4);
                    for (var offset = 0; offset < argumentCount; offset++)
                    {
                        var index = 3 + offset;
                        if (!TryReadOptionalString(state, index, out var unit) ||
                            unit is null)
                        {
                            break;
                        }
                        if (IsRecognizedUnitToken(unit))
                            units.Add(unit.ToLowerInvariant());
                    }

                    var wasRegistered = value.Events.Contains(name);
                    var hadUnits = value.RegisteredUnitEvents.TryGetValue(
                        name,
                        out var previousUnits);
                    var registered = !wasRegistered ||
                                     hadUnits != (units.Count > 0) ||
                                     (hadUnits &&
                                      !previousUnits!.SequenceEqual(
                                          units,
                                          StringComparer.OrdinalIgnoreCase));
                    value.Events.Add(name);
                    if (units.Count == 0)
                        value.RegisteredUnitEvents.Remove(name);
                    else
                        value.RegisteredUnitEvents[name] = units;
                    runtime.IndexEventTarget(value, name);
                    lua_pushboolean(state, registered ? 1 : 0);
                    return 1;
                }
            case "RegisterUnitEventCallback":
                {
                    if (!TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local registered = self:RegisterUnitEventCallback(eventName, cb, unit)");
                    }
                    if (!WowFunctionContainersApi.IsUnitCallbackEvent(name))
                        return 0;
                    if (lua_isfunction(state, 3) == 0 ||
                        !TryReadOptionalString(state, 4, out var unit) ||
                        unit is null ||
                        !IsRecognizedUnitToken(unit))
                    {
                        return luaL_error(
                            state,
                            "Usage: local registered = self:RegisterUnitEventCallback(eventName, cb, unit)");
                    }

                    if (!value.EventCallbackReferences.TryGetValue(name, out var callbacks))
                    {
                        callbacks = [];
                        value.EventCallbackReferences.Add(name, callbacks);
                    }
                    callbacks.Add(
                        new UiFrameEventCallback(
                            runtime.CaptureFunction(state, 3),
                            [unit.ToLowerInvariant()]));
                    runtime.IndexEventTarget(value, name);
                    lua_pushboolean(state, 1);
                    return 1;
                }
            case "IsEventRegistered":
                {
                    if (!TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local isRegistered, (units)* = self:IsEventRegistered(eventName)");
                    }

                    var registered = value.Events.Contains(name);
                    lua_pushboolean(state, registered ? 1 : 0);
                    if (!registered ||
                        !value.RegisteredUnitEvents.TryGetValue(name, out var units))
                    {
                        lua_pushnil(state);
                        return 2;
                    }
                    foreach (var unit in units)
                        lua_pushstring(state, unit);
                    return units.Count + 1;
                }
            case "UnregisterEvent":
                {
                    if (!TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local registered = self:UnregisterEvent(eventName)");
                    }

                    var unregistered = value.Events.Remove(name);
                    value.RegisteredUnitEvents.Remove(name);
                    if (value.EventCallbackReferences.Remove(name, out var callbacks))
                    {
                        foreach (var callback in callbacks)
                            runtime.ReleaseReference(callback.Reference);
                        unregistered = true;
                    }
                    runtime.UnindexEventTarget(value, name);
                    lua_pushboolean(state, unregistered ? 1 : 0);
                    return 1;
                }
            case "UnregisterAllEvents":
                value.Events.Clear();
                value.RegisteredUnitEvents.Clear();
                value.AllEventsRegistered = false;
                runtime.UnindexAllEventsTarget(value);
                runtime.UnindexEventTargets(value);
                foreach (var callback in value.EventCallbackReferences.Values.SelectMany(
                             callbacks => callbacks))
                {
                    runtime.ReleaseReference(callback.Reference);
                }
                value.EventCallbackReferences.Clear();
                return 0;
            case "SetPoint":
                SetPoint(runtime, value);
                return 0;
            case "AdjustPointsOffset":
                if (!TryReadRequiredVector2(state, 2, out var pointsOffsetDelta))
                    return luaL_error(
                        state,
                        "Usage: self:AdjustPointsOffset(x, y)");
                MaterializeAllPointsAnchors(value);
                for (var index = 0; index < value.Anchors.Count; index++)
                {
                    var anchor = value.Anchors[index];
                    value.Anchors[index] = anchor with
                    {
                        X = anchor.X + pointsOffsetDelta.X,
                        Y = anchor.Y + pointsOffsetDelta.Y
                    };
                }
                runtime.Ui.InvalidateLayout();
                return 0;
            case "SetPointsOffset":
                if (!TryReadRequiredVector2(state, 2, out var pointsOffset))
                    return luaL_error(
                        state,
                        "Usage: self:SetPointsOffset(x, y)");
                MaterializeAllPointsAnchors(value);
                for (var index = 0; index < value.Anchors.Count; index++)
                {
                    value.Anchors[index] = value.Anchors[index] with
                    {
                        X = pointsOffset.X,
                        Y = pointsOffset.Y
                    };
                }
                runtime.Ui.InvalidateLayout();
                return 0;
            case "ClearPointsOffset":
                MaterializeAllPointsAnchors(value);
                for (var index = 0; index < value.Anchors.Count; index++)
                    value.Anchors[index] = value.Anchors[index] with { X = 0, Y = 0 };
                runtime.Ui.InvalidateLayout();
                return 0;
            case "CanChangeAttribute":
                lua_pushboolean(
                    state,
                    !value.Protected || !runtime.Client.InCombatLockdown ? 1 : 0);
                return 1;
            case "ClearAttribute":
                {
                    if (!TryReadRequiredString(state, 2, out var attributeName))
                        return luaL_error(state, "Arguments: (\"attributeName\")");

                    var cleared = false;
                    if (value.AttributeReferences.Remove(attributeName, out var attributeReference))
                    {
                        cleared = attributeReference > 0;
                        runtime.ReleaseReference(attributeReference);
                    }
                    if (value.Attributes.TryGetValue(attributeName, out var primitiveAttribute) &&
                        primitiveAttribute is not null)
                    {
                        value.Attributes.Remove(attributeName);
                        cleared = true;
                    }
                    lua_pushboolean(state, cleared ? 1 : 0);
                    return 1;
                }
            case "ClearAttributes":
                foreach (var reference in value.AttributeReferences.Values)
                    runtime.ReleaseReference(reference);
                value.AttributeReferences.Clear();
                value.Attributes.Clear();
                return 0;
            case "ExecuteAttribute":
                {
                    if (!TryReadRequiredString(state, 2, out var attributeName))
                        return luaL_error(state, "Arguments: (\"name\" [, ...])");

                    var originalTop = lua_gettop(state);
                    if (!value.AttributeReferences.TryGetValue(
                            attributeName,
                            out var attributeReference))
                    {
                        lua_pushboolean(state, 0);
                        return 1;
                    }

                    lua_rawgeti(state, LUA_REGISTRYINDEX, attributeReference);
                    if (lua_isfunction(state, -1) == 0)
                    {
                        lua_pop(state, 1);
                        lua_pushboolean(state, 0);
                        return 1;
                    }

                    for (var index = 3; index <= originalTop; index++)
                        lua_pushvalue(state, index);
                    if (lua_pcall(state, originalTop - 2, LUA_MULTRET, 0) != 0)
                        return lua_error(state);

                    var resultCount = lua_gettop(state) - originalTop;
                    lua_pushboolean(state, 1);
                    lua_insert(state, originalTop + 1);
                    return resultCount + 1;
                }
            case "SetAttribute":
            case "SetAttributeNoHandler":
                {
                    if (!TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(state, "Arguments: (\"name\", value)");
                    }

                    if (value.AttributeReferences.Remove(name, out var previousReference))
                        runtime.ReleaseReference(previousReference);

                    object? argument;
                    var argumentType = lua_type(state, 3);
                    if (argumentType is LUA_TTABLE or LUA_TFUNCTION or LUA_TUSERDATA or LUA_TTHREAD)
                    {
                        var reference = LuaRuntime.CaptureValue(state, 3);
                        value.AttributeReferences[name] = reference;
                        value.Attributes.Remove(name);
                        argument = new LuaRegistryValue(reference);
                    }
                    else
                    {
                        argument = ReadPrimitive(state, 3);
                        value.Attributes[name] = argument;
                    }
                    if (operation == "SetAttribute")
                    {
                        runtime.InvokeScript(
                            value,
                            "OnAttributeChanged",
                            name.ToLowerInvariant(),
                            argument);
                    }
                    return 0;
                }
            case "GetAttribute":
                {
                    if (lua_gettop(state) == 4 &&
                        TryReadOptionalString(state, 2, out var prefix) &&
                        TryReadOptionalString(state, 3, out var infix) &&
                        TryReadOptionalString(state, 4, out var suffix) &&
                        prefix is not null &&
                        infix is not null &&
                        suffix is not null)
                    {
                        var candidateNames = new[]
                        {
                        prefix + infix + suffix,
                        "*" + infix + suffix,
                        prefix + infix + "*",
                        "*" + infix + "*",
                        infix
                    };
                        foreach (var candidateName in candidateNames)
                        {
                            if (PushAttributeValue(runtime, value, candidateName))
                                return 1;
                        }
                        lua_pushnil(state);
                        return 1;
                    }

                    if (!TryReadOptionalString(state, 2, out var name) ||
                        name is null)
                    {
                        return luaL_error(state, "Arguments: (\"name\")");
                    }

                    if (!PushAttributeValue(runtime, value, name))
                        lua_pushnil(state);
                    return 1;
                }
            case "ClearAllPoints":
                if (value.Line is { } lineState)
                {
                    lineState.Start = null;
                    lineState.End = null;
                }
                else
                {
                    value.Anchors.Clear();
                    value.AllPointsTargetId = null;
                }
                runtime.Ui.InvalidateRectValidity(value);
                runtime.Ui.InvalidateLayout();
                return 0;
            case "ClearPoint":
                {
                    if (!TryReadRequiredFramePoint(state, 2, out var point))
                        return luaL_error(state, "Usage: self:ClearPoint(point)");

                    MaterializeAllPointsAnchors(value);
                    value.Anchors.RemoveAll(
                        anchor => anchor.Point.Equals(point, StringComparison.OrdinalIgnoreCase));
                    runtime.Ui.InvalidateRectValidity(value);
                    runtime.Ui.InvalidateLayout();
                    NotifySizeChanged(runtime, value);
                    return 0;
                }
            case "SetAllPoints":
                {
                    var target = runtime.Ui.Find(value.ParentId ?? runtime.Ui.UiParentId);
                    if (lua_gettop(state) >= 2)
                    {
                        var targetType = lua_type(state, 2);
                        if (targetType == LUA_TNIL)
                        {
                            target = runtime.Ui.Find(runtime.Ui.UiParentId);
                        }
                        else if (targetType is LUA_TTABLE or LUA_TUSERDATA or LUA_TSTRING)
                        {
                            target = GetObject(runtime, 2);
                            if (target is null)
                            {
                                if (targetType == LUA_TSTRING)
                                {
                                    runtime.Log.Warn(
                                        "ui",
                                        $"SetAllPoints: Couldn't find region named '{lua_tostring(state, 2)}'");
                                }
                                else
                                {
                                    runtime.Log.Warn("ui", "SetAllPoints: Invalid relative region");
                                }
                                return 0;
                            }
                        }
                    }
                    value.AllPointsTargetId = target?.Id;
                    value.Anchors.Clear();
                    runtime.Ui.InvalidateRectValidity(value);
                    runtime.Ui.InvalidateLayout();
                    NotifySizeChanged(runtime, value);
                    return 0;
                }
            case "SetSize":
                if (!TryReadRequiredFloat(state, 2, out var requiredSizeWidth) ||
                    !TryReadRequiredFloat(state, 3, out var requiredSizeHeight))
                    return luaL_error(state, "Usage: self:SetSize(x, y)");
                value.Width = (float)requiredSizeWidth;
                value.Height = (float)requiredSizeHeight;
                runtime.Ui.InvalidateLayout();
                NotifySizeChanged(runtime, value);
                return 0;
            case "SetWidth":
                if (!TryReadRequiredFloat(state, 2, out var requiredWidth))
                    return luaL_error(state, "Usage: self:SetWidth(width)");
                value.Width = (float)requiredWidth;
                runtime.Ui.InvalidateLayout();
                NotifySizeChanged(runtime, value);
                return 0;
            case "SetHeight":
                if (!TryReadRequiredFloat(state, 2, out var requiredHeight))
                    return luaL_error(state, "Usage: self:SetHeight(height)");
                value.Height = (float)requiredHeight;
                runtime.Ui.InvalidateLayout();
                NotifySizeChanged(runtime, value);
                return 0;
            case "GetWidth":
                lua_pushnumber(
                    state,
                    OptionalBoolean(state, 2, false) || !runtime.Ui.HasResolvedRect(value)
                        ? ResolveUnrectedSize(runtime, value).X
                        : Unscaled(runtime, value, runtime.Ui.ResolveBounds(value.Id).Width));
                return 1;
            case "GetHeight":
                lua_pushnumber(
                    state,
                    OptionalBoolean(state, 2, false) || !runtime.Ui.HasResolvedRect(value)
                        ? ResolveUnrectedSize(runtime, value).Y
                        : Unscaled(runtime, value, runtime.Ui.ResolveBounds(value.Id).Height));
                return 1;
            case "GetSize":
                {
                    var ignoreRect = OptionalBoolean(state, 2, false);
                    if (ignoreRect || !runtime.Ui.HasResolvedRect(value))
                    {
                        var unrectedSize = ResolveUnrectedSize(runtime, value);
                        lua_pushnumber(state, unrectedSize.X);
                        lua_pushnumber(state, unrectedSize.Y);
                        return 2;
                    }

                    var bounds = runtime.Ui.ResolveBounds(value.Id);
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Width));
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Height));
                    return 2;
                }
            case "GetLeft":
                if (!runtime.Ui.HasResolvedRect(value))
                    return 0;
                lua_pushnumber(state, Unscaled(runtime, value, runtime.Ui.ResolveBounds(value.Id).Left));
                return 1;
            case "GetRight":
                if (!runtime.Ui.HasResolvedRect(value))
                    return 0;
                lua_pushnumber(state, Unscaled(runtime, value, runtime.Ui.ResolveBounds(value.Id).Right));
                return 1;
            case "GetBottom":
                if (!runtime.Ui.HasResolvedRect(value))
                    return 0;
                lua_pushnumber(state, Unscaled(runtime, value, runtime.Ui.ResolveBounds(value.Id).Bottom));
                return 1;
            case "GetTop":
                if (!runtime.Ui.HasResolvedRect(value))
                    return 0;
                lua_pushnumber(state, Unscaled(runtime, value, runtime.Ui.ResolveBounds(value.Id).Top));
                return 1;
            case "GetCenter":
                {
                    if (!runtime.Ui.HasResolvedRect(value))
                        return 0;
                    var center = runtime.Ui.ResolveBounds(value.Id).Center;
                    lua_pushnumber(state, Unscaled(runtime, value, center.X));
                    lua_pushnumber(state, Unscaled(runtime, value, center.Y));
                    return 2;
                }
            case "GetRect":
                {
                    if (!runtime.Ui.HasResolvedRect(value))
                        return 0;
                    var bounds = runtime.Ui.ResolveBounds(value.Id);
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Left));
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Bottom));
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Width));
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Height));
                    return 4;
                }
            case "GetBoundsRect":
                {
                    var bounds = runtime.Ui.ResolveFrameBoundsRect(value) ??
                                 new UiRect(0, 0, 0, 0);
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Left));
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Bottom));
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Width));
                    lua_pushnumber(state, Unscaled(runtime, value, bounds.Height));
                    return 4;
                }
            case "GetScaledRect":
                {
                    if (!runtime.Ui.HasResolvedRect(value))
                        return 0;
                    var bounds = runtime.Ui.ResolveBounds(value.Id);
                    var nativeScale = runtime.Ui.NativeScaledRectScale;
                    lua_pushnumber(state, bounds.Left * nativeScale);
                    lua_pushnumber(state, bounds.Bottom * nativeScale);
                    lua_pushnumber(state, bounds.Width * nativeScale);
                    lua_pushnumber(state, bounds.Height * nativeScale);
                    return 4;
                }
            case "IsRectValid":
                lua_pushboolean(state, runtime.Ui.HasResolvedRect(value) ? 1 : 0);
                return 1;
            case "Intersects":
                {
                    var other = lua_istable(state, 2) != 0
                        ? GetObject(runtime, 2)
                        : null;
                    if (other is null)
                        return luaL_error(state, "Usage: self:Intersects(region)");

                    if (!runtime.Ui.HasResolvedRect(value) ||
                        !runtime.Ui.HasResolvedRect(other))
                    {
                        lua_pushboolean(state, 0);
                        return 1;
                    }

                    var bounds = runtime.Ui.ResolveBounds(value.Id);
                    var otherBounds = runtime.Ui.ResolveBounds(other.Id);
                    lua_pushboolean(
                        state,
                        bounds.Left < otherBounds.Right &&
                        bounds.Right > otherBounds.Left &&
                        bounds.Bottom < otherBounds.Top &&
                        bounds.Top > otherBounds.Bottom
                            ? 1
                            : 0);
                    return 1;
                }
            case "GetPoint":
                return GetPoint(runtime, value);
            case "GetPointByName":
                return GetPointByName(runtime, value);
            case "GetNumPoints":
                lua_pushinteger(
                    state,
                    value.AllPointsTargetId is not null ? 2 : value.Anchors.Count);
                return 1;
            case "GetID":
                lua_pushinteger(state, value.FrameId);
                return 1;
            case "SetID":
                {
                    if (!TryReadRequiredInt32(state, 2, out var frameId))
                        return luaL_error(state, "Usage: self:SetID(id)");
                    value.FrameId = frameId;
                    return 0;
                }
            case "SetRolesets":
                {
                    value.Rolesets.Clear();
                    if (TryReadOptionalString(state, 2, out var rolesetsString) &&
                        rolesetsString is not null)
                    {
                        foreach (var roleset in rolesetsString.Split(
                                     ',',
                                     StringSplitOptions.RemoveEmptyEntries |
                                     StringSplitOptions.TrimEntries))
                        {
                            value.Rolesets.Add(roleset);
                        }
                    }
                    return 0;
                }
            case "AddRoleset":
                {
                    if (!TryReadRequiredString(state, 2, out var addedRoleset))
                        return luaL_error(state, "Usage: self:AddRoleset(roleset)");
                    value.Rolesets.Add(addedRoleset);
                    return 0;
                }
            case "RemoveRoleset":
                {
                    if (!TryReadRequiredString(state, 2, out var removedRoleset))
                        return luaL_error(state, "Usage: self:RemoveRoleset(roleset)");
                    value.Rolesets.Remove(removedRoleset);
                    return 0;
                }
            case "GetRolesetNames":
                {
                    lua_newtable(state);
                    var rolesetIndex = 1;
                    foreach (var roleset in value.Rolesets.OrderBy(
                                 name => name,
                                 StringComparer.OrdinalIgnoreCase))
                    {
                        lua_pushstring(state, roleset);
                        lua_rawseti(state, -2, rolesetIndex++);
                    }
                    return 1;
                }
            case "IsRolesetFiltered":
                lua_pushboolean(state, 0);
                return 1;
            case "SetScale":
                {
                    if (value.Animation is { } setScaleAnimationState)
                    {
                        if (!TryReadRequiredVector2(state, 2, out var animationScale))
                            return luaL_error(state, "Usage: self:SetScale(scale)");
                        setScaleAnimationState.HasScaleRange = false;
                        setScaleAnimationState.Scale =
                            Vector2.Max(animationScale, new Vector2(.001f));
                        setScaleAnimationState.ScaleFrom = Vector2.One;
                        setScaleAnimationState.ScaleTo = setScaleAnimationState.Scale;
                        return 0;
                    }

                    if (!TryReadRequiredFloat(state, 2, out var requiredScale))
                        return luaL_error(state, "Usage: self:SetScale(scale)");
                    if (value.ObjectType.Equals(
                            "ModelSceneActor",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        value.ModelScale = MathF.Max((float)requiredScale, .01f);
                        return 0;
                    }
                    var scale = (float)requiredScale;
                    if (scale <= 0)
                    {
                        runtime.Log.Warn(
                            "ui",
                            $"{value.Name ?? value.ObjectType}:SetScale(scale): Scale must be > 0");
                        return 0;
                    }
                    if (scale != value.Scale)
                    {
                        value.Scale = scale;
                        runtime.Ui.InvalidateLayout();
                    }
                    return 0;
                }
            case "GetScale":
                if (value.Animation is { } scaleAnimationState)
                {
                    return PushVector2(
                        state,
                        scaleAnimationState.HasScaleRange
                            ? Vector2.Zero
                            : scaleAnimationState.Scale);
                }
                if (value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase))
                {
                    lua_pushnumber(state, value.ModelScale);
                    return 1;
                }
                lua_pushnumber(state, value.Scale);
                return 1;
            case "GetEffectiveScale":
                lua_pushnumber(state, runtime.Ui.EffectiveScale(value));
                return 1;
            case "GetEffectiveAlpha":
                lua_pushnumber(state, runtime.Ui.EffectiveAlpha(value));
                return 1;
            case "SetAlpha":
                {
                    if (!TryReadRequiredFloat(state, 2, out var requiredAlpha))
                        return luaL_error(state, "Usage: self:SetAlpha(alpha)");
                    var alpha = Math.Clamp((float)requiredAlpha, 0, 1);
                    if (value.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase))
                    {
                        var font = EnsureFont(value);
                        font.Color = new Vector4(
                            font.Color.X,
                            font.Color.Y,
                            font.Color.Z,
                            MathF.Floor(alpha * 255 + .5f) / 255);
                        MarkFontOverride(runtime, value, font, UiFontOverrides.Color);
                        return 0;
                    }
                    if (value.ObjectType.Equals(
                            "ModelSceneActor",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        value.ModelAlpha = alpha;
                        return 0;
                    }
                    value.Alpha = MathF.Floor(alpha * 255 + .5f) / 255;
                    if (value.AnimationBaseAlpha is not null)
                        value.AnimationBaseAlpha = value.Alpha;
                    return 0;
                }
            case "SetAlphaFromBoolean":
                {
                    const string usage =
                        "Usage: self:SetAlphaFromBoolean(value [, alphaIfTrue, alphaIfFalse])";
                    if (!TryReadRequiredBoolean(state, 2, out var condition) ||
                        !TryReadOptionalNormalizedByte(state, 3, out var alphaIfTrue) ||
                        !TryReadOptionalNormalizedByte(state, 4, out var alphaIfFalse))
                    {
                        return luaL_error(state, usage);
                    }

                    value.Alpha =
                        (condition
                            ? alphaIfTrue.GetValueOrDefault(byte.MaxValue)
                            : alphaIfFalse.GetValueOrDefault()) /
                        255f;
                    if (value.AnimationBaseAlpha is not null)
                        value.AnimationBaseAlpha = value.Alpha;
                    return 0;
                }
            case "GetAlpha":
                lua_pushnumber(
                    state,
                    value.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase)
                        ? EnsureFont(value).Color.W
                        : value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase)
                        ? value.ModelAlpha
                        : value.Alpha);
                return 1;
            case "IsIgnoringParentAlpha":
                lua_pushboolean(state, value.IgnoreParentAlpha ? 1 : 0);
                return 1;
            case "IsIgnoringParentScale":
                lua_pushboolean(state, value.IgnoreParentScale ? 1 : 0);
                return 1;
            case "Show":
                if (value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase))
                {
                    value.Shown = true;
                    return 0;
                }
                if (value.Tooltip is { } shownTooltip &&
                    shownTooltip.Lines.Count == 0 &&
                    !shownTooltip.AllowShowWithNoLines)
                {
                    return 0;
                }
                SetShown(runtime, value, true);
                return 0;
            case "Hide":
                if (value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase))
                {
                    value.Shown = false;
                    return 0;
                }
                SetShown(runtime, value, false);
                return 0;
            case "SetShown":
                if (value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase))
                {
                    value.Shown = lua_gettop(state) < 2 ||
                                  lua_toboolean(state, 2) != 0;
                    return 0;
                }
                SetShown(runtime, value, OptionalBoolean(state, 2, false));
                return 0;
            case "IsShown":
                lua_pushboolean(state, value.Shown ? 1 : 0);
                return 1;
            case "IsVisible":
                lua_pushboolean(state, runtime.Ui.IsVisible(value) ? 1 : 0);
                return 1;
            case "IsObjectLoaded":
                lua_pushboolean(state, value.ObjectLoaded ? 1 : 0);
                return 1;
            case "IsClampedToScreen":
                lua_pushboolean(state, value.ClampedToScreen ? 1 : 0);
                return 1;
            case "SetParent":
                if (value.Animation is { } reparentedAnimation)
                {
                    UiObject? newGroup = null;
                    if (lua_type(state, 2) == LUA_TSTRING)
                    {
                        newGroup = runtime.Ui.Find(lua_tostring(state, 2) ?? string.Empty);
                    }
                    else
                    {
                        newGroup = GetObject(runtime, 2);
                    }

                    if (newGroup?.AnimationGroup is null)
                        return 0;

                    if (!TryReadOptionalInt32(
                            state,
                            3,
                            reparentedAnimation.Order,
                            out var animationOrder))
                    {
                        return luaL_error(state, "Usage: self:SetParent(parent [, order])");
                    }

                    reparentedAnimation.Order = Math.Clamp(animationOrder, 0, 99);
                    runtime.Ui.Reparent(value, newGroup.Id);
                }
                else if (value.ControlPoint is { } reparentedPoint)
                {
                    var oldPath = value.ParentId is { } oldPathId
                        ? runtime.Ui.Find(oldPathId)
                        : null;
                    var newPath = GetObject(runtime, 2) ??
                                  (OptionalString(state, 2) is { } parentName
                                      ? runtime.Ui.Find(parentName)
                                      : null);
                    if (newPath is null ||
                        !newPath.ObjectType.Equals("Path", StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, "Invalid object for SetParent() call.");
                    }

                    if (HasRequiredValue(state, 3))
                    {
                        if (!TryReadRequiredInt32(state, 3, out var parentOrder))
                            return luaL_error(state, "Usage: self:SetParent(parent [, order])");
                        reparentedPoint.Order = Math.Clamp(parentOrder, 0, 99);
                    }
                    else if (reparentedPoint.Order == -1)
                    {
                        var points = runtime.Ui.ResolvePathControlPoints(newPath);
                        reparentedPoint.Order = points.Count == 0
                            ? 99
                            : points[^1].ControlPoint!.Order is >= 0 and <= 99
                                ? points[^1].ControlPoint!.Order
                                : 99;
                    }

                    runtime.Ui.Reparent(value, newPath.Id);
                    if (oldPath is not null)
                        runtime.Ui.ResolvePathControlPoints(oldPath);
                    runtime.Ui.ResolvePathControlPoints(newPath);
                }
                else
                {
                    UiObject? newParent = null;
                    if (HasRequiredValue(state, 2))
                    {
                        if (lua_type(state, 2) == LUA_TSTRING)
                            return luaL_error(
                                state,
                                "Usage: self:SetParent([parent])");
                        newParent = GetObject(runtime, 2);
                        if (newParent is null ||
                            !WowWidgetApi.IsFrameWidget(newParent.ObjectType))
                        {
                            return luaL_error(
                                state,
                                "Usage: self:SetParent([parent])");
                        }
                    }
                    ReparentWithVisibility(runtime, value, newParent?.Id);
                }
                return 0;
            case "GetParent":
                runtime.PushObject(value.ParentId is { } parentId ? runtime.Ui.Find(parentId) : null);
                return 1;
            case "GetSourceLocation":
                lua_pushstring(state, value.SourceLocation);
                return 1;
            case "ClearParentKey":
                runtime.ClearParentKeys(value);
                return 0;
            case "GetDebugName":
                lua_pushstring(
                    state,
                    runtime.GetDebugName(value, OptionalBoolean(state, 2, false)));
                return 1;
            case "GetParentKey":
                PushOptionalString(state, runtime.GetParentKey(value));
                return 1;
            case "SetParentKey":
                {
                    if (!HasRequiredValue(state, 2) ||
                        !TryReadOptionalString(state, 2, out var parentKey) ||
                        parentKey is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetParentKey(parentKey [, clearOtherKeys])");
                    }
                    runtime.SetParentKey(value, parentKey, OptionalBoolean(state, 3, false));
                    return 0;
                }
            case "GetChildren":
                return PushChildren(runtime, value, regions: false);
            case "GetRegions":
                return PushChildren(runtime, value, regions: true);
            case "GetNumChildren":
                lua_pushinteger(
                    state,
                    value.Children.Count(id =>
                        runtime.Ui.Find(id) is { } child &&
                        WowWidgetApi.IsFrameWidget(child.ObjectType)));
                return 1;
            case "GetNumRegions":
                lua_pushinteger(
                    state,
                    value.Children.Count(id => runtime.Ui.Find(id)?.IsRegion == true));
                return 1;
            case "GetName":
                PushOptionalString(state, value.Name);
                return 1;
            case "GetObjectType":
                lua_pushstring(state, value.ObjectType);
                return 1;
            case "HasAnySecretAspect":
                lua_pushboolean(
                    state,
                    (value.SecretAspectMask | value.SecondarySecretAspectMask) != 0 ? 1 : 0);
                return 1;
            case "HasSecretAspect":
                {
                    const string usage =
                        "Usage: local hasSecretAspect = self:HasSecretAspect(aspect)";
                    if (!TryReadSecretAspectMask(state, 2, out var aspect))
                        return luaL_error(state, usage);
                    lua_pushboolean(
                        state,
                        ((value.SecretAspectMask | value.SecondarySecretAspectMask) &
                         aspect) != 0
                            ? 1
                            : 0);
                    return 1;
                }
            case "HasSecretValues":
                lua_pushboolean(state, value.ContainsSecretValues ? 1 : 0);
                return 1;
            case "IsPreventingSecretValues":
                lua_pushboolean(state, value.PreventsSecretValues ? 1 : 0);
                return 1;
            case "IsObjectType":
                if (!HasRequiredValue(state, 2) ||
                    !TryReadOptionalString(state, 2, out var requestedObjectType) ||
                    requestedObjectType is null)
                {
                    return luaL_error(
                        state,
                        "Usage: local isType = self:IsObjectType(objectType)");
                }
                lua_pushboolean(
                    state,
                    MatchesObjectType(value, requestedObjectType) ? 1 : 0);
                return 1;
            case "SetFillTexture":
                {
                    if (!TryReadRequiredTextureAsset(
                            state,
                            2,
                            out var asset,
                            out var fileDataId))
                        return luaL_error(state, "Usage: self:SetFillTexture(asset)");
                    var blob = EnsureBlob(value);
                    blob.FillTexture = asset;
                    blob.FillTextureFileDataId = fileDataId;
                    return 0;
                }
            case "SetBorderTexture":
                {
                    if (!TryReadRequiredTextureAsset(
                            state,
                            2,
                            out var asset,
                            out var fileDataId))
                        return luaL_error(state, "Usage: self:SetBorderTexture(asset)");
                    var blob = EnsureBlob(value);
                    blob.BorderTexture = asset;
                    blob.BorderTextureFileDataId = fileDataId;
                    return 0;
                }
            case "SetFillAlpha":
                if (!TryReadRequiredByte(state, 2, out var fillAlpha))
                    return luaL_error(state, "Usage: self:SetFillAlpha(alpha)");
                EnsureBlob(value).FillAlpha = fillAlpha;
                return 0;
            case "SetBorderAlpha":
                if (!TryReadRequiredByte(state, 2, out var borderAlpha))
                    return luaL_error(state, "Usage: self:SetBorderAlpha(alpha)");
                EnsureBlob(value).BorderAlpha = borderAlpha;
                return 0;
            case "SetBorderScalar":
                if (!TryReadRequiredFloat(state, 2, out var borderScalar))
                    return luaL_error(state, "Usage: self:SetBorderScalar(scalar)");
                EnsureBlob(value).BorderScalar = Math.Clamp((float)borderScalar, 0, 10);
                return 0;
            case "SetMapID":
                {
                    if (!TryReadRequiredInt32(state, 2, out var mapId))
                        return luaL_error(state, "Usage: self:SetMapID(uiMapID)");
                    EnsureBlob(value).MapId = mapId;
                    return 0;
                }
            case "GetMapID":
                lua_pushinteger(state, EnsureBlob(value).MapId);
                return 1;
            case "GetPingPosition":
                {
                    var minimap = EnsureMinimap(value);
                    ResolveMinimapPingPosition(
                        runtime,
                        minimap,
                        out var pingX,
                        out var pingY);
                    lua_pushnumber(state, pingX);
                    lua_pushnumber(state, pingY);
                    return 2;
                }
            case "GetZoom":
                lua_pushinteger(state, runtime.Minimap.Zoom);
                return 1;
            case "GetZoomLevels":
                lua_pushinteger(state, EnsureMinimap(value).ZoomLevels);
                return 1;
            case "PingLocation":
                {
                    var minimap = EnsureMinimap(value);
                    float localX = 0;
                    float localY = 0;
                    if (lua_gettop(state) >= 2)
                    {
                        if (!TryReadRequiredFloat(state, 2, out var parsedPingX) ||
                            !TryReadRequiredFloat(state, 3, out var parsedPingY))
                            return luaL_error(state, "Usage: self:PingLocation([location])");
                        localX = (float)parsedPingX;
                        localY = (float)parsedPingY;
                    }
                    PingMinimapLocation(runtime, value, minimap, localX, localY);
                    return 0;
                }
            case "SetZoom":
                if (value.ObjectType.Equals("Browser", StringComparison.OrdinalIgnoreCase) ||
                    value.ObjectType.Equals("Checkout", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryReadRequiredFloat(state, 2, out var browserZoom))
                    {
                        return luaL_error(
                            state,
                            value.ObjectType.Equals(
                                "Checkout",
                                StringComparison.OrdinalIgnoreCase)
                                ? "Usage: self:SetZoom(zoomLevel)"
                                : "Usage: self:SetZoom(zoom)");
                    }
                    value.BrowserZoom = value.ObjectType.Equals(
                        "Checkout",
                        StringComparison.OrdinalIgnoreCase)
                        ? (float)browserZoom
                        : browserZoom;
                    return 0;
                }
                if (!TryReadRequiredUInt32(state, 2, out var minimapZoom))
                    return luaL_error(state, "Usage: self:SetZoom(zoomFactor)");
                var zoom = (int)Math.Min(minimapZoom, 5);
                runtime.Minimap.Zoom = zoom;
                EnsureMinimap(value).Zoom = zoom;
                return 0;
            case "SetMaskTexture":
                {
                    var minimap = EnsureMinimap(value);
                    if (!TryReadRequiredTextureAsset(
                            state,
                            2,
                            out var maskTexture,
                            out var maskTextureFileDataId))
                        return luaL_error(state, "Usage: self:SetMaskTexture(asset)");
                    (minimap.MaskTexture, minimap.MaskTextureFileDataId) =
                        (maskTexture, maskTextureFileDataId);
                    return 0;
                }
            case "SetArchBlobInsideAlpha":
                if (!TryReadRequiredByte(state, 2, out var archInsideAlpha))
                    return luaL_error(state, "Usage: self:SetArchBlobInsideAlpha(alpha)");
                EnsureMinimap(value).Arch.InsideAlpha = archInsideAlpha;
                return 0;
            case "SetArchBlobInsideTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Arch, "inside"))
                    return luaL_error(state, "Usage: self:SetArchBlobInsideTexture(asset)");
                return 0;
            case "SetArchBlobOutsideAlpha":
                if (!TryReadRequiredByte(state, 2, out var archOutsideAlpha))
                    return luaL_error(state, "Usage: self:SetArchBlobOutsideAlpha(alpha)");
                EnsureMinimap(value).Arch.OutsideAlpha = archOutsideAlpha;
                return 0;
            case "SetArchBlobOutsideTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Arch, "outside"))
                    return luaL_error(state, "Usage: self:SetArchBlobOutsideTexture(asset)");
                return 0;
            case "SetArchBlobRingAlpha":
                if (!TryReadRequiredByte(state, 2, out var archRingAlpha))
                    return luaL_error(state, "Usage: self:SetArchBlobRingAlpha(alpha)");
                EnsureMinimap(value).Arch.RingAlpha = archRingAlpha;
                return 0;
            case "SetArchBlobRingScalar":
                if (!TryReadRequiredFloat(state, 2, out var archRingScalar))
                    return luaL_error(state, "Usage: self:SetArchBlobRingScalar(scalar)");
                EnsureMinimap(value).Arch.RingScalar = (float)archRingScalar;
                return 0;
            case "SetArchBlobRingTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Arch, "ring"))
                    return luaL_error(state, "Usage: self:SetArchBlobRingTexture(asset)");
                return 0;
            case "SetQuestBlobInsideAlpha":
                if (!TryReadRequiredByte(state, 2, out var questInsideAlpha))
                    return luaL_error(state, "Usage: self:SetQuestBlobInsideAlpha(alpha)");
                EnsureMinimap(value).Quest.InsideAlpha = questInsideAlpha;
                return 0;
            case "SetQuestBlobInsideTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Quest, "inside"))
                    return luaL_error(state, "Usage: self:SetQuestBlobInsideTexture(asset)");
                return 0;
            case "SetQuestBlobOutsideAlpha":
                if (!TryReadRequiredByte(state, 2, out var questOutsideAlpha))
                    return luaL_error(state, "Usage: self:SetQuestBlobOutsideAlpha(alpha)");
                EnsureMinimap(value).Quest.OutsideAlpha = questOutsideAlpha;
                return 0;
            case "SetQuestBlobOutsideTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Quest, "outside"))
                    return luaL_error(state, "Usage: self:SetQuestBlobOutsideTexture(asset)");
                return 0;
            case "SetQuestBlobRingAlpha":
                if (!TryReadRequiredByte(state, 2, out var questRingAlpha))
                    return luaL_error(state, "Usage: self:SetQuestBlobRingAlpha(alpha)");
                EnsureMinimap(value).Quest.RingAlpha = questRingAlpha;
                return 0;
            case "SetQuestBlobRingScalar":
                if (!TryReadRequiredFloat(state, 2, out var questRingScalar))
                    return luaL_error(state, "Usage: self:SetQuestBlobRingScalar(scalar)");
                EnsureMinimap(value).Quest.RingScalar = (float)questRingScalar;
                return 0;
            case "SetQuestBlobRingTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Quest, "ring"))
                    return luaL_error(state, "Usage: self:SetQuestBlobRingTexture(asset)");
                return 0;
            case "SetTaskBlobInsideAlpha":
                if (!TryReadRequiredByte(state, 2, out var taskInsideAlpha))
                    return luaL_error(state, "Usage: self:SetTaskBlobInsideAlpha(alpha)");
                EnsureMinimap(value).Task.InsideAlpha = taskInsideAlpha;
                return 0;
            case "SetTaskBlobInsideTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Task, "inside"))
                    return luaL_error(state, "Usage: self:SetTaskBlobInsideTexture(asset)");
                return 0;
            case "SetTaskBlobOutsideAlpha":
                if (!TryReadRequiredByte(state, 2, out var taskOutsideAlpha))
                    return luaL_error(state, "Usage: self:SetTaskBlobOutsideAlpha(alpha)");
                EnsureMinimap(value).Task.OutsideAlpha = taskOutsideAlpha;
                return 0;
            case "SetTaskBlobOutsideTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Task, "outside"))
                    return luaL_error(state, "Usage: self:SetTaskBlobOutsideTexture(asset)");
                return 0;
            case "SetTaskBlobRingAlpha":
                if (!TryReadRequiredByte(state, 2, out var taskRingAlpha))
                    return luaL_error(state, "Usage: self:SetTaskBlobRingAlpha(alpha)");
                EnsureMinimap(value).Task.RingAlpha = taskRingAlpha;
                return 0;
            case "SetTaskBlobRingScalar":
                if (!TryReadRequiredFloat(state, 2, out var taskRingScalar))
                    return luaL_error(state, "Usage: self:SetTaskBlobRingScalar(scalar)");
                EnsureMinimap(value).Task.RingScalar = (float)taskRingScalar;
                return 0;
            case "SetTaskBlobRingTexture":
                if (!TrySetMinimapBlobTexture(state, EnsureMinimap(value).Task, "ring"))
                    return luaL_error(state, "Usage: self:SetTaskBlobRingTexture(asset)");
                return 0;
            case "UpdateBlips":
                EnsureMinimap(value).BlipRefreshAccumulator = 0;
                return 0;
            case "SetMergeThreshold":
                if (!TryReadRequiredFloat(state, 2, out var mergeThreshold))
                    return luaL_error(state, "Usage: self:SetMergeThreshold(threshold)");
                EnsureBlob(value).MergeThreshold =
                    Math.Clamp((float)mergeThreshold, 0.1f, 0.5f);
                return 0;
            case "SetNumSplinePoints":
                if (!TryReadRequiredInt32(state, 2, out var numSplinePoints))
                    return luaL_error(
                        state,
                        "Usage: self:SetNumSplinePoints(numSplinePoints)");
                EnsureBlob(value).NumSplinePoints = Math.Clamp(numSplinePoints, 8, 30);
                return 0;
            case "EnableMerging":
                EnsureBlob(value).MergingEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "EnableSmoothing":
                EnsureBlob(value).SmoothingEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "DrawAll":
                {
                    if (value.ObjectType.Equals(
                            "ScenarioPOIFrame",
                            StringComparison.OrdinalIgnoreCase))
                        EnsureBlob(value).DrawAll = true;
                    return 0;
                }
            case "DrawBlob":
                {
                    if (!TryReadRequiredInt32(state, 2, out var blobId))
                        return luaL_error(
                            state,
                            "Usage: self:DrawBlob(questID [, draw])");
                    var draw = OptionalBoolean(state, 3, false);
                    if (!value.ObjectType.Equals(
                            "QuestPOIFrame",
                            StringComparison.OrdinalIgnoreCase))
                        return 0;
                    var blob = EnsureBlob(value);
                    if (!draw)
                    {
                        blob.DrawnBlobIds.Remove(blobId);
                        return 0;
                    }
                    if (!blob.DrawnBlobIds.Contains(blobId) &&
                        blob.DrawnBlobIds.Count < 8)
                        blob.DrawnBlobIds.Add(blobId);
                    return 0;
                }
            case "DrawNone":
                {
                    var blob = EnsureBlob(value);
                    blob.DrawAll = false;
                    blob.DrawnBlobIds.Clear();
                    return 0;
                }
            case "GetNumTooltips":
                lua_pushinteger(state, EnsureBlob(value).MouseOverObjectiveIndices.Count);
                return 1;
            case "GetTooltipIndex":
                {
                    if (!TryReadRequiredUInt32(state, 2, out var tooltipIndex) ||
                        tooltipIndex == 0)
                        return luaL_error(
                            state,
                            "Usage: local objectiveIndex = self:GetTooltipIndex(index)");
                    var objectiveIndices = EnsureBlob(value).MouseOverObjectiveIndices;
                    lua_pushinteger(
                        state,
                        tooltipIndex <= objectiveIndices.Count && tooltipIndex <= 24
                            ? objectiveIndices[(int)tooltipIndex - 1]
                            : 0);
                    return 1;
                }
            case "GetScenarioTooltipText":
                {
                    var blob = EnsureBlob(value);
                    var scenarioIndex = blob.MouseOverScenarioIndex;
                    if ((uint)scenarioIndex < blob.ScenarioTooltipTexts.Count &&
                        blob.ScenarioTooltipTexts[scenarioIndex] is { } tooltipText)
                        lua_pushstring(state, tooltipText);
                    else
                        lua_pushnil(state);
                    return 1;
                }
            case "UpdateMouseOverTooltip":
                {
                    const string questUsage =
                        "Usage: local questID, numObjectives = " +
                        "self:UpdateMouseOverTooltip(x, y)";
                    const string scenarioUsage =
                        "Usage: local hasTooltip = self:UpdateMouseOverTooltip(x, y)";
                    var isScenario = value.ObjectType.Equals(
                        "ScenarioPOIFrame",
                        StringComparison.OrdinalIgnoreCase);
                    if (!TryReadRequiredFloat(state, 2, out var blobX) ||
                        !TryReadRequiredFloat(state, 3, out var blobY))
                        return luaL_error(state, isScenario ? scenarioUsage : questUsage);

                    var blob = EnsureBlob(value);
                    blob.ClearMouseOverTooltip();
                    var hit = UiBlobGeometry.TryHitTest(
                        runtime.Ui,
                        value,
                        runtime.MapProvider,
                        (float)blobX,
                        (float)blobY,
                        out var hitBlobId,
                        out var objectiveIndices,
                        out var tooltipText);
                    if (hit)
                    {
                        if (isScenario)
                        {
                            var scenarioIndex = blob.Areas
                                .Select(area => area.BlobId)
                                .Distinct()
                                .Take(8)
                                .ToList()
                                .IndexOf(hitBlobId);
                            if ((uint)scenarioIndex < 8)
                            {
                                blob.MouseOverScenarioIndex = scenarioIndex;
                                blob.SetScenarioTooltipText(
                                    scenarioIndex,
                                    tooltipText);
                            }
                        }
                        else
                        {
                            blob.MouseOverQuestId = hitBlobId;
                            blob.MouseOverObjectiveIndices.AddRange(
                                objectiveIndices);
                        }
                    }
                    if (isScenario)
                    {
                        lua_pushboolean(state, hit ? 1 : 0);
                        return 1;
                    }
                    if (hit)
                    {
                        lua_pushinteger(state, blob.MouseOverQuestId);
                        lua_pushinteger(
                            state,
                            blob.MouseOverObjectiveIndices.Count);
                    }
                    else
                    {
                        lua_pushnil(state);
                        lua_pushnil(state);
                    }
                    return 2;
                }
            case "SetUiMapID":
                {
                    if (value.ObjectType.Equals("UnitPositionFrame", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!TryReadRequiredInt32(state, 2, out var unitPositionMapId))
                            return luaL_error(state, "Usage: self:SetUiMapID(mapID)");
                        EnsureUnitPosition(value).UiMapId = unitPositionMapId;
                        return 0;
                    }
                    if (!TryReadRequiredInt32(state, 2, out var fogMapId))
                        return luaL_error(state, "Usage: self:SetUiMapID(uiMapID)");
                    var fog = EnsureFogOfWar(value);
                    if (fog.UiMapId == fogMapId)
                        return 0;
                    fog.UiMapId = fogMapId;
                    runtime.InvokeScript(value, "OnUiMapChanged", fogMapId);
                    return 0;
                }
            case "GetUiMapID":
                {
                    if (value.ObjectType.Equals(
                            "UnitPositionFrame",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        lua_pushinteger(state, EnsureUnitPosition(value).UiMapId);
                    }
                    else
                        lua_pushinteger(state, EnsureFogOfWar(value).UiMapId);
                    return 1;
                }
            case "SetFogOfWarBackgroundAtlas":
                {
                    const string usage = "Usage: self:SetFogOfWarBackgroundAtlas(atlas)";
                    if (!TryReadRequiredAtlasName(state, 2, out var atlasName))
                        return luaL_error(state, usage);
                    if (runtime.AtlasProvider?.TryGetAtlas(atlasName, out _) != true)
                        return 0;
                    var fog = EnsureFogOfWar(value);
                    fog.BackgroundAtlas = atlasName;
                    fog.BackgroundTexture = null;
                    fog.BackgroundTextureFileDataId = null;
                    fog.BackgroundTextureTilesHorizontally = false;
                    fog.BackgroundTextureTilesVertically = false;
                    SynchronizeFogOfWarBackgroundTexture(runtime, value);
                    return 0;
                }
            case "GetFogOfWarBackgroundAtlas":
                PushOptionalString(state, EnsureFogOfWar(value).BackgroundAtlas);
                return 1;
            case "SetFogOfWarBackgroundTexture":
                {
                    const string usage =
                        "Usage: self:SetFogOfWarBackgroundTexture(" +
                        "asset, horizontalTile, verticalTile)";
                    if (!TryReadRequiredTextureAsset(
                            state,
                            2,
                            out var backgroundAsset,
                            out var backgroundFileDataId) ||
                        !TryReadRequiredBoolean(
                            state,
                            3,
                            out var backgroundTilesHorizontally) ||
                        !TryReadRequiredBoolean(
                            state,
                            4,
                            out var backgroundTilesVertically))
                        return luaL_error(state, usage);
                    var fog = EnsureFogOfWar(value);
                    fog.BackgroundTexture =
                        string.IsNullOrEmpty(backgroundAsset) ? null : backgroundAsset;
                    fog.BackgroundTextureFileDataId =
                        backgroundFileDataId is > 0 ? backgroundFileDataId : null;
                    fog.BackgroundAtlas = null;
                    fog.BackgroundTextureTilesHorizontally = backgroundTilesHorizontally;
                    fog.BackgroundTextureTilesVertically = backgroundTilesVertically;
                    SynchronizeFogOfWarBackgroundTexture(runtime, value);
                    return 0;
                }
            case "GetFogOfWarBackgroundTexture":
                return PushTextureAsset(
                    state,
                    EnsureFogOfWar(value).BackgroundTexture,
                    EnsureFogOfWar(value).BackgroundTextureFileDataId);
            case "SetFogOfWarMaskAtlas":
                {
                    const string usage = "Usage: self:SetFogOfWarMaskAtlas(atlas)";
                    if (!TryReadRequiredAtlasName(state, 2, out var atlasName))
                        return luaL_error(state, usage);
                    if (runtime.AtlasProvider?.TryGetAtlas(atlasName, out _) != true)
                        return 0;
                    var fog = EnsureFogOfWar(value);
                    fog.MaskAtlas = atlasName;
                    fog.MaskTexture = null;
                    fog.MaskTextureFileDataId = null;
                    SynchronizeFogOfWarMaskTextures(runtime, value);
                    return 0;
                }
            case "GetFogOfWarMaskAtlas":
                PushOptionalString(state, EnsureFogOfWar(value).MaskAtlas);
                return 1;
            case "SetFogOfWarMaskTexture":
                {
                    if (!TryReadRequiredTextureAsset(
                            state,
                            2,
                            out var maskAsset,
                            out var maskFileDataId))
                        return luaL_error(
                            state,
                            "Usage: self:SetFogOfWarMaskTexture(asset)");
                    var fog = EnsureFogOfWar(value);
                    fog.MaskTexture = string.IsNullOrEmpty(maskAsset) ? null : maskAsset;
                    fog.MaskTextureFileDataId =
                        maskFileDataId is > 0 ? maskFileDataId : null;
                    fog.MaskAtlas = null;
                    SynchronizeFogOfWarMaskTextures(runtime, value);
                    return 0;
                }
            case "GetFogOfWarMaskTexture":
                return PushTextureAsset(
                    state,
                    EnsureFogOfWar(value).MaskTexture,
                    EnsureFogOfWar(value).MaskTextureFileDataId);
            case "SetMaskScalar":
                if (!TryReadRequiredFloat(state, 2, out var maskScalar))
                    return luaL_error(state, "Usage: self:SetMaskScalar(scalar)");
                EnsureFogOfWar(value).MaskScalar = Math.Max((float)maskScalar, 0.01f);
                return 0;
            case "GetMaskScalar":
                lua_pushnumber(state, EnsureFogOfWar(value).MaskScalar);
                return 1;
            case "AddUnit":
                {
                    const string usage = "Usage: self:AddUnit(params)";
                    if (!TryReadRequiredString(state, 2, out var unit) ||
                        !TryReadRequiredTextureAsset(
                            state,
                            3,
                            out var asset,
                            out var fileDataId) ||
                        !TryReadOptionalFloat(state, 4, out var width) ||
                        !TryReadOptionalFloat(state, 5, out var height) ||
                        !TryReadOptionalFloat(state, 6, out var red) ||
                        !TryReadOptionalFloat(state, 7, out var green) ||
                        !TryReadOptionalFloat(state, 8, out var blue) ||
                        !TryReadOptionalFloat(state, 9, out var alpha) ||
                        !TryReadOptionalInt8(state, 10, out var subLayer))
                    {
                        return luaL_error(state, usage);
                    }

                    if (!IsRecognizedUnitToken(unit))
                        return 0;

                    var unitPosition = EnsureUnitPosition(value);
                    if (unitPosition.Units.ContainsKey(unit))
                        return 0;
                    var resolvedUnit = runtime.Units.Find(unit);
                    if (resolvedUnit is null)
                        return 0;

                    var hasCompleteColor =
                        HasRequiredValue(state, 6) &&
                        HasRequiredValue(state, 7) &&
                        HasRequiredValue(state, 8) &&
                        HasRequiredValue(state, 9);
                    unitPosition.Units.Add(unit, new UiUnitPositionEntry
                    {
                        Unit = unit,
                        UnitGuid = resolvedUnit.Guid,
                        Asset = asset,
                        FileDataId = fileDataId,
                        Width = (float)width,
                        Height = (float)height,
                        Color = hasCompleteColor
                            ? new System.Numerics.Vector4(
                                QuantizeNormalizedByte(red),
                                QuantizeNormalizedByte(green),
                                QuantizeNormalizedByte(blue),
                                QuantizeNormalizedByte(alpha))
                            : System.Numerics.Vector4.One,
                        SubLayer = subLayer,
                        ShowFacing = OptionalBoolean(state, 11, false)
                    });
                    return 0;
                }
            case "ClearUnits":
                {
                    var unitPosition = EnsureUnitPosition(value);
                    foreach (var entry in unitPosition.Units.Values)
                    {
                        if (entry.TextureId is not { } markerTextureId)
                            continue;
                        if (runtime.Ui.Find(markerTextureId) is { } texture)
                        {
                            texture.Shown = false;
                            texture.Anchors.Clear();
                            texture.AllPointsTargetId = null;
                            EnsureTexture(texture).ResetTexCoord();
                        }
                        unitPosition.UnitTexturePool.Add(markerTextureId);
                    }
                    unitPosition.Units.Clear();
                    unitPosition.MouseOverUnits.Clear();
                    unitPosition.UnitsFinalized = false;
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "FinalizeUnits":
                {
                    var unitPosition = EnsureUnitPosition(value);
                    unitPosition.UnitsFinalized = true;
                    UpdateUnitPositionTextures(runtime, value, force: true);
                    return 0;
                }
            case "GetMouseOverUnits":
                {
                    var mouseOverUnits = EnsureUnitPosition(value).MouseOverUnits;
                    foreach (var unit in mouseOverUnits)
                        lua_pushstring(state, unit);
                    return mouseOverUnits.Count;
                }
            case "SetPlayerPingScale":
                if (!TryReadRequiredFloat(state, 2, out var playerPingScale))
                    return luaL_error(
                        state,
                        "Usage: self:SetPlayerPingScale(scale)");
                if (playerPingScale > 0)
                    EnsureUnitPosition(value).PlayerPingScale = (float)playerPingScale;
                return 0;
            case "GetPlayerPingScale":
                lua_pushnumber(state, EnsureUnitPosition(value).PlayerPingScale);
                return 1;
            case "SetPlayerPingTexture":
                {
                    const string usage =
                        "Usage: self:SetPlayerPingTexture(" +
                        "textureType, asset [, width, height])";
                    if (!TryReadRequiredInt32(state, 2, out var textureType) ||
                        textureType is < 0 or > 2 ||
                        !TryReadRequiredTextureAsset(
                            state,
                            3,
                            out var asset,
                            out var fileDataId) ||
                        !TryReadOptionalFloat(state, 4, out var pingWidth) ||
                        !TryReadOptionalFloat(state, 5, out var pingHeight))
                    {
                        return luaL_error(state, usage);
                    }
                    EnsureUnitPosition(value).PlayerPingTextures[textureType] =
                        new UiUnitPositionPingTexture
                        {
                            Asset = asset,
                            FileDataId = fileDataId,
                            Width = (float)pingWidth,
                            Height = (float)pingHeight
                        };
                    SynchronizeUnitPositionPingTextures(runtime, value);
                    return 0;
                }
            case "SetUnitColor":
                {
                    const string usage = "Usage: self:SetUnitColor(unit, color)";
                    if (!TryReadRequiredString(state, 2, out var unit) ||
                        !IsRecognizedUnitToken(unit) ||
                        !TryReadRequiredFloat(state, 3, out var red) ||
                        !TryReadRequiredFloat(state, 4, out var green) ||
                        !TryReadRequiredFloat(state, 5, out var blue) ||
                        !TryReadRequiredFloat(state, 6, out var alpha))
                    {
                        return luaL_error(state, usage);
                    }
                    if (EnsureUnitPosition(value).Units.TryGetValue(unit, out var entry))
                    {
                        entry.Color = new System.Numerics.Vector4(
                            QuantizeNormalizedByte(red),
                            QuantizeNormalizedByte(green),
                            QuantizeNormalizedByte(blue),
                            QuantizeNormalizedByte(alpha));
                    }
                    return 0;
                }
            case "StartPlayerPing":
                {
                    const string usage =
                        "Usage: self:StartPlayerPing([duration, fadeDuration])";
                    if (!TryReadOptionalFloat(state, 2, out var duration) ||
                        !TryReadOptionalFloat(state, 3, out var fadeDuration))
                    {
                        return luaL_error(state, usage);
                    }
                    var unitPosition = EnsureUnitPosition(value);
                    unitPosition.PlayerPingActive = true;
                    unitPosition.PlayerPingStartedAt = runtime.Time;
                    var totalDuration = Math.Max((float)(duration + fadeDuration), 0);
                    unitPosition.PlayerPingDuration = totalDuration;
                    unitPosition.PlayerPingFadeDuration =
                        Math.Clamp((float)fadeDuration, 0, totalDuration);
                    foreach (var pingTextureSlot in unitPosition.PlayerPingTextureIds)
                    {
                        if (pingTextureSlot is { } id && runtime.Ui.Find(id) is { } texture)
                            texture.Shown = true;
                    }
                    return 0;
                }
            case "StopPlayerPing":
                {
                    var unitPosition = EnsureUnitPosition(value);
                    unitPosition.PlayerPingActive = false;
                    unitPosition.PlayerPingStartedAt = 0;
                    unitPosition.PlayerPingDuration = 0;
                    unitPosition.PlayerPingFadeDuration = 0;
                    HideUnitPositionPingTextures(runtime, unitPosition);
                    return 0;
                }
            case "SetFrameLevel":
                {
                    const string usage = "Usage: self:SetFrameLevel(frameLevel)";
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, usage);
                    var requestedLevel = lua_tonumber(state, 2);
                    if (!double.IsFinite(requestedLevel) ||
                        requestedLevel < ushort.MinValue ||
                        requestedLevel > ushort.MaxValue)
                    {
                        return luaL_error(
                            state,
                            "outside of expected range 0 to 65535 - " + usage);
                    }
                    runtime.Ui.SetFrameLevel(
                        value,
                        Math.Min(10_000, (int)Math.Truncate(requestedLevel)));
                    return 0;
                }
            case "SetFixedFrameLevel":
                if (!TryReadRequiredBoolean(state, 2, out var fixedFrameLevel))
                    return luaL_error(state, "Usage: self:SetFixedFrameLevel(isFixed)");
                runtime.Ui.SetFixedFrameLevel(value, fixedFrameLevel);
                return 0;
            case "SetFixedFrameStrata":
                if (!TryReadRequiredBoolean(state, 2, out var fixedFrameStrata))
                    return luaL_error(state, "Usage: self:SetFixedFrameStrata(isFixed)");
                runtime.Ui.SetFixedFrameStrata(value, fixedFrameStrata);
                return 0;
            case "SetForbidden":
                runtime.Ui.SetForbidden(value);
                return 0;
            case "IsForbidden":
                lua_pushboolean(state, value.Forbidden ? 1 : 0);
                return 1;
            case "CanChangeProtectedState":
                lua_pushboolean(
                    state,
                    !value.Protected || !runtime.Client.InCombatLockdown ? 1 : 0);
                return 1;
            case "IsProtected":
                lua_pushboolean(state, value.Protected ? 1 : 0);
                lua_pushboolean(state, value.ProtectedExplicitly ? 1 : 0);
                return 2;
            case "IsAnchoringRestricted":
                lua_pushboolean(state, value.AnchoringRestricted ? 1 : 0);
                return 1;
            case "IsAnchoringSecret":
                lua_pushboolean(state, value.AnchoringSecret ? 1 : 0);
                return 1;
            case "CollapsesLayout":
                lua_pushboolean(state, value.CollapsesLayout ? 1 : 0);
                return 1;
            case "SetCollapsesLayout":
                if (!TryReadRequiredBoolean(state, 2, out var collapsesLayout))
                    return luaL_error(
                        state,
                        "Usage: self:SetCollapsesLayout(collapsesLayout)");
                value.CollapsesLayout = collapsesLayout;
                runtime.Ui.InvalidateLayout();
                return 0;
            case "IsCollapsed":
                lua_pushboolean(
                    state,
                    value.CollapsesLayout && !runtime.Ui.IsVisible(value) ? 1 : 0);
                return 1;
            case "IsDragging":
                lua_pushboolean(
                    state,
                    runtime.Ui.MovingObjectId == value.Id ? 1 : 0);
                return 1;
            case "GetFrameLevel":
                runtime.FlushPendingSizeChanged();
                lua_pushinteger(state, runtime.Ui.EffectiveFrameLevel(value));
                return 1;
            case "GetHighestFrameLevel":
                lua_pushinteger(
                    state,
                    runtime.Ui.HighestFrameLevel(
                        value,
                        OptionalBoolean(state, 2, false)));
                return 1;
            case "HasFixedFrameLevel":
                lua_pushboolean(state, value.FixedFrameLevel ? 1 : 0);
                return 1;
            case "HasFixedFrameStrata":
                lua_pushboolean(state, value.FixedFrameStrata ? 1 : 0);
                return 1;
            case "IsUsingParentLevel":
                lua_pushboolean(state, value.UseParentLevel ? 1 : 0);
                return 1;
            case "SetUsingParentLevel":
                if (!TryReadRequiredBoolean(state, 2, out var usingParentLevel))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetUsingParentLevel(usingParentLevel)");
                }
                runtime.Ui.SetUseParentLevel(value, usingParentLevel);
                return 0;
            case "SetFrameStrata":
                {
                    const string usage = "Usage: self:SetFrameStrata(strata)";
                    var strata = OptionalString(state, 2);
                    if (strata is null || !FrameStrataNames.Contains(strata))
                        return luaL_error(state, usage);
                    var normalizedStrata = strata.ToUpperInvariant();
                    if (!normalizedStrata.Equals("WORLD", StringComparison.Ordinal))
                        runtime.Ui.SetFrameStrata(value, normalizedStrata);
                    return 0;
                }
            case "GetFrameStrata":
                lua_pushstring(state, runtime.Ui.EffectiveFrameStrata(value));
                return 1;
            case "SetDrawLayer":
                {
                    var isModelScene = value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase);
                    var usage = isModelScene
                        ? "Usage: self:SetDrawLayer(layer)"
                        : "Usage: self:SetDrawLayer(layer [, sublevel])";
                    var layer = OptionalString(state, 2);
                    if (layer is null ||
                        !LayerNames.Contains(layer))
                        return luaL_error(state, usage);

                    var subLevel = 0;
                    if (!isModelScene && HasRequiredValue(state, 3))
                    {
                        if (lua_isnumber(state, 3) == 0)
                            return luaL_error(state, usage);
                        var numericSubLevel = lua_tonumber(state, 3);
                        if (double.IsNaN(numericSubLevel) ||
                            numericSubLevel is < -128 or > 127)
                            return luaL_error(state, usage);
                        subLevel = (int)numericSubLevel;
                    }
                    if (subLevel is < -8 or > 7)
                    {
                        runtime.Log.Warn(
                            "ui",
                            "SetDrawLayer: sublevel must be between -8 and 7!");
                        return 0;
                    }
                    value.DrawLayer = layer.ToUpperInvariant();
                    value.SubLevel = subLevel;
                    return 0;
                }
            case "GetDrawLayer":
                lua_pushstring(state, value.DrawLayer);
                lua_pushinteger(
                    state,
                    value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase)
                        ? 0
                        : value.SubLevel);
                return 2;
            case "EnableDrawLayer":
            case "DisableDrawLayer":
            case "IsDrawLayerEnabled":
            case "SetDrawLayerEnabled":
                {
                    var usage = operation switch
                    {
                        "EnableDrawLayer" => "Usage: self:EnableDrawLayer(layer)",
                        "DisableDrawLayer" => "Usage: self:DisableDrawLayer(layer)",
                        "IsDrawLayerEnabled" =>
                            "Usage: local isEnabled = self:IsDrawLayerEnabled(layer)",
                        _ => "Usage: self:SetDrawLayerEnabled(layer [, isEnabled])"
                    };
                    var layer = OptionalString(state, 2);
                    if (layer is null || !LayerNames.Contains(layer))
                        return luaL_error(state, usage);

                    var normalizedLayer = layer.ToUpperInvariant();
                    if (operation == "IsDrawLayerEnabled")
                    {
                        lua_pushboolean(
                            state,
                            value.EnabledDrawLayers.Contains(normalizedLayer) ? 1 : 0);
                        return 1;
                    }

                    var enabled = operation switch
                    {
                        "EnableDrawLayer" => true,
                        "DisableDrawLayer" => false,
                        _ => OptionalBoolean(state, 3, false)
                    };
                    if (enabled)
                        value.EnabledDrawLayers.Add(normalizedLayer);
                    else
                        value.EnabledDrawLayers.Remove(normalizedLayer);
                    return 0;
                }
            case "Raise":
                runtime.Ui.Raise(value);
                return 0;
            case "Lower":
                runtime.Ui.Lower(value);
                return 0;
            case "GetRaisedFrameLevel":
                lua_pushinteger(state, value.RaisedFrameLevel);
                return 1;
            case "DoesClipChildren":
                lua_pushboolean(state, value.ClipsChildren ? 1 : 0);
                return 1;
            case "SetClipsChildren":
                if (!TryReadRequiredBoolean(state, 2, out var clipsChildren))
                    return luaL_error(state, "Usage: self:SetClipsChildren(clipsChildren)");
                value.ClipsChildren = clipsChildren;
                return 0;
            case "GetDontSavePosition":
                lua_pushboolean(state, value.DontSavePosition ? 1 : 0);
                return 1;
            case "SetDontSavePosition":
                if (!TryReadRequiredBoolean(state, 2, out var dontSavePosition))
                    return luaL_error(
                        state,
                        "Usage: self:SetDontSavePosition(dontSave)");
                value.DontSavePosition = dontSavePosition;
                return 0;
            case "GetFlattensRenderLayers":
                lua_pushboolean(state, value.FlattensRenderLayers ? 1 : 0);
                return 1;
            case "GetEffectivelyFlattensRenderLayers":
                lua_pushboolean(
                    state,
                    runtime.Ui.EffectivelyFlattensRenderLayers(value) ? 1 : 0);
                return 1;
            case "SetFlattensRenderLayers":
                if (!TryReadRequiredBoolean(state, 2, out var flattensRenderLayers))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetFlattensRenderLayers(flatten)");
                }
                value.FlattensRenderLayers = flattensRenderLayers;
                return 0;
            case "IsFrameBuffer":
                lua_pushboolean(state, value.IsFrameBuffer ? 1 : 0);
                return 1;
            case "SetIsFrameBuffer":
                if (!TryReadRequiredBoolean(state, 2, out var isFrameBuffer))
                    return luaL_error(
                        state,
                        "Usage: self:SetIsFrameBuffer(isFrameBuffer)");
                value.IsFrameBuffer = isFrameBuffer;
                return 0;
            case "IsIgnoringChildrenForBounds":
                lua_pushboolean(state, value.IgnoreChildrenForBounds ? 1 : 0);
                return 1;
            case "SetIgnoringChildrenForBounds":
                if (!TryReadRequiredBoolean(state, 2, out var ignoreChildrenForBounds))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetIgnoringChildrenForBounds(ignore)");
                }
                value.IgnoreChildrenForBounds = ignoreChildrenForBounds;
                return 0;
            case "DoesHyperlinkPropagateToParent":
                lua_pushboolean(state, value.HyperlinkPropagateToParent ? 1 : 0);
                return 1;
            case "SetHyperlinkPropagateToParent":
                if (!TryReadRequiredBoolean(state, 2, out var hyperlinkPropagate))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetHyperlinkPropagateToParent(canPropagate)");
                }
                value.HyperlinkPropagateToParent = hyperlinkPropagate;
                return 0;
            case "SetClampedToScreen":
                if (lua_gettop(state) < 2)
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetClampedToScreen(clampedToScreen)");
                }
                value.ClampedToScreen = lua_toboolean(state, 2) != 0;
                return 0;
            case "SetClampRectInsets":
                if (!TryReadRequiredFloat(state, 2, out var clampLeft) ||
                    !TryReadRequiredFloat(state, 3, out var clampRight) ||
                    !TryReadRequiredFloat(state, 4, out var clampTop) ||
                    !TryReadRequiredFloat(state, 5, out var clampBottom))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetClampRectInsets(left, right, top, bottom)");
                }
                value.ClampRectInsets = new Vector4(
                    (float)clampLeft,
                    (float)clampRight,
                    (float)clampTop,
                    (float)clampBottom);
                return 0;
            case "GetClampRectInsets":
                lua_pushnumber(state, value.ClampRectInsets.X);
                lua_pushnumber(state, value.ClampRectInsets.Y);
                lua_pushnumber(state, value.ClampRectInsets.Z);
                lua_pushnumber(state, value.ClampRectInsets.W);
                return 4;
            case "SetHitRectInsets":
                if (!TryReadRequiredFloat(state, 2, out var hitLeft) ||
                    !TryReadRequiredFloat(state, 3, out var hitRight) ||
                    !TryReadRequiredFloat(state, 4, out var hitTop) ||
                    !TryReadRequiredFloat(state, 5, out var hitBottom))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetHitRectInsets(left, right, top, bottom)");
                }
                value.HitRectInsets = new UiInsets(
                    (float)hitLeft,
                    (float)hitRight,
                    (float)hitTop,
                    (float)hitBottom);
                return 0;
            case "GetHitRectInsets":
                lua_pushnumber(state, value.HitRectInsets.Left);
                lua_pushnumber(state, value.HitRectInsets.Right);
                lua_pushnumber(state, value.HitRectInsets.Top);
                lua_pushnumber(state, value.HitRectInsets.Bottom);
                return 4;
            case "SetHyperlinksEnabled":
                value.HyperlinksEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "GetHyperlinksEnabled":
                lua_pushboolean(state, value.HyperlinksEnabled ? 1 : 0);
                return 1;
            case "DesaturateHierarchy":
                if (!TryReadRequiredFloat(state, 2, out var hierarchyDesaturation))
                {
                    return luaL_error(
                        state,
                        "Usage: self:DesaturateHierarchy(desaturation [, excludeRoot])");
                }
                DesaturateHierarchy(
                    runtime,
                    value,
                    Math.Clamp((float)hierarchyDesaturation, 0, 1),
                    OptionalBoolean(state, 3, false));
                return 0;
            case "SetIgnoreParentScale":
                if (!TryReadRequiredBoolean(state, 2, out var ignoreParentScale))
                    return luaL_error(
                        state,
                        "Usage: self:SetIgnoreParentScale(ignore)");
                value.IgnoreParentScale = ignoreParentScale;
                runtime.Ui.InvalidateLayout();
                return 0;
            case "SetIgnoreParentAlpha":
                if (!TryReadRequiredBoolean(state, 2, out var ignoreParentAlpha))
                    return luaL_error(
                        state,
                        "Usage: self:SetIgnoreParentAlpha(ignore)");
                value.IgnoreParentAlpha = ignoreParentAlpha;
                return 0;
            case "SetMovable":
                if (!TryReadRequiredBoolean(state, 2, out var movable))
                    return luaL_error(state, "Usage: self:SetMovable(movable)");
                value.Movable = movable;
                return 0;
            case "SetToplevel":
                if (!TryReadRequiredBoolean(state, 2, out var topLevel))
                    return luaL_error(state, "Usage: self:SetToplevel(topLevel)");
                runtime.Ui.SetToplevel(value, topLevel);
                return 0;
            case "IsToplevel":
                lua_pushboolean(state, value.Toplevel ? 1 : 0);
                return 1;
            case "IsMovable":
                lua_pushboolean(state, value.Movable ? 1 : 0);
                return 1;
            case "SetUserPlaced":
                if (!TryReadRequiredBoolean(state, 2, out var userPlaced))
                    return luaL_error(state, "Usage: self:SetUserPlaced(userPlaced)");
                if (value.Movable || value.Resizable)
                    value.UserPlaced = userPlaced;
                else
                    runtime.Log.Warn("ui", "Frame is not movable or resizable");
                return 0;
            case "IsUserPlaced":
                lua_pushboolean(state, value.UserPlaced ? 1 : 0);
                return 1;
            case "GetAnimations":
                return PushAnimations(runtime, value);
            case "GetDuration":
                lua_pushnumber(
                    state,
                    value.AnimationGroup is not null
                        ? AnimationGroupDuration(runtime, value)
                        : value.Animation?.Duration ?? 0);
                return 1;
            case "GetStartDelay":
                lua_pushnumber(state, value.Animation?.StartDelay ?? 0);
                return 1;
            case "GetElapsed":
                lua_pushnumber(
                    state,
                    value.AnimationGroup?.Elapsed ??
                    value.Animation?.Elapsed ??
                    0);
                return 1;
            case "GetEndDelay":
                lua_pushnumber(state, value.Animation?.EndDelay ?? 0);
                return 1;
            case "GetOrder":
                lua_pushinteger(
                    state,
                    value.ControlPoint?.Order ?? value.Animation?.Order ?? 1);
                return 1;
            case "GetControlPoints":
                {
                    var controlPoints = runtime.Ui.ResolvePathControlPoints(value);
                    foreach (var controlPoint in controlPoints)
                        runtime.PushObject(controlPoint);
                    return controlPoints.Count;
                }
            case "GetCurveType":
                lua_pushstring(state, value.Animation?.PathCurveType ?? "NONE");
                return 1;
            case "GetMaxControlPointOrder":
                {
                    var controlPoints = runtime.Ui.ResolvePathControlPoints(value);
                    lua_pushinteger(
                        state,
                        controlPoints.Count == 0
                            ? -1
                            : controlPoints[^1].ControlPoint!.Order);
                    return 1;
                }
            case "GetProgress":
                if (value.AnimationGroup is { } progressGroup)
                {
                    lua_pushnumber(
                        state,
                        progressGroup.Elapsed / AnimationGroupDuration(runtime, value));
                }
                else
                {
                    lua_pushnumber(state, value.Animation?.Progress ?? 0);
                }
                return 1;
            case "GetSmoothProgress":
                lua_pushnumber(state, value.Animation?.SmoothProgress ?? 0);
                return 1;
            case "GetSmoothing":
                lua_pushstring(state, value.Animation?.Smoothing ?? "NONE");
                return 1;
            case "GetRegionParent":
                {
                    var owner = value.ParentId is { } groupId &&
                                runtime.Ui.Find(groupId)?.ParentId is { } ownerId
                        ? runtime.Ui.Find(ownerId)
                        : null;
                    runtime.PushObject(owner);
                    return 1;
                }
            case "GetTarget":
                {
                    var animationTarget = value.Animation is { } animation
                        ? runtime.ResolveAnimationTarget(value, animation)
                        : null;
                    runtime.PushObject(animationTarget);
                    return 1;
                }
            case "GetFromAlpha":
                lua_pushnumber(state, value.Animation?.FromAlpha ?? 1);
                return 1;
            case "GetToAlpha":
                lua_pushnumber(state, value.Animation?.ToAlpha ?? 1);
                return 1;
            case "GetStartColor":
                return PushColorMixin(state, value.Animation?.StartColor ?? Vector4.One);
            case "GetEndColor":
                return PushColorMixin(state, value.Animation?.EndColor ?? Vector4.One);
            case "GetDegrees":
                lua_pushnumber(state, value.Animation?.Degrees ?? 0);
                return 1;
            case "GetRadians":
                lua_pushnumber(state, value.Animation?.Radians ?? 0);
                return 1;
            case "GetScaleFrom":
                return PushVector2(
                    state,
                    value.Animation is { HasScaleRange: true } getScaleFromState
                        ? getScaleFromState.ScaleFrom
                        : Vector2.Zero);
            case "GetScaleTo":
                return PushVector2(
                    state,
                    value.Animation is { HasScaleRange: true } getScaleToState
                        ? getScaleToState.ScaleTo
                        : Vector2.Zero);
            case "GetOrigin":
                lua_pushstring(state, value.Animation?.OriginPoint ?? "CENTER");
                return 1 + PushVector2(
                    state,
                    value.Animation?.OriginOffset ?? Vector2.Zero);
            case "IsPlaying":
                lua_pushboolean(
                    state,
                    value.AnimationGroup?.Playing == true ||
                    value.Animation?.PlaybackState == 1
                        ? 1
                        : 0);
                return 1;
            case "IsPaused":
                lua_pushboolean(
                    state,
                    value.Cooldown?.Paused == true ||
                    value.AnimationGroup?.Paused == true ||
                    value.Animation?.PlaybackState == 2
                        ? 1
                        : 0);
                return 1;
            case "IsDone":
                {
                    if (value.Animation is { } animation)
                    {
                        lua_pushboolean(state, animation.PlaybackState == 3 ? 1 : 0);
                        return 1;
                    }
                    var duration = value.AnimationGroup is not null
                        ? AnimationGroupDuration(runtime, value)
                        : 0;
                    var animationGroup = value.AnimationGroup;
                    var isDone =
                        duration <= 0.0001 ||
                        animationGroup is not null &&
                        !animationGroup.Looping.Equals("REPEAT", StringComparison.OrdinalIgnoreCase) &&
                        !animationGroup.Looping.Equals("BOUNCE", StringComparison.OrdinalIgnoreCase) &&
                        animationGroup.Elapsed / duration >= 1;
                    lua_pushboolean(state, isDone ? 1 : 0);
                    return 1;
                }
            case "IsDelaying":
                {
                    var animation = value.Animation;
                    var total = animation is null
                        ? 0
                        : animation.StartDelay + animation.EndDelay + animation.Duration;
                    var delaying = animation is not null &&
                                   Math.Abs(total) >= 0.00000023841858 &&
                                   (animation.Elapsed < animation.StartDelay ||
                                    animation.Elapsed > animation.StartDelay + animation.Duration);
                    lua_pushboolean(state, delaying ? 1 : 0);
                    return 1;
                }
            case "IsStopped":
                lua_pushboolean(state, value.Animation?.PlaybackState == 0 ? 1 : 0);
                return 1;
            case "GetLooping":
                lua_pushstring(state, value.AnimationGroup?.Looping ?? "NONE");
                return 1;
            case "GetLoopState":
                lua_pushstring(
                    state,
                    value.AnimationGroup is { Playing: true } loopStateGroup
                        ? loopStateGroup.Reverse ? "REVERSE" : "FORWARD"
                        : "NONE");
                return 1;
            case "GetAnimationSpeedMultiplier":
                lua_pushnumber(state, value.AnimationGroup?.AnimationSpeedMultiplier ?? 1);
                return 1;
            case "IsPendingFinish":
                lua_pushboolean(state, value.AnimationGroup?.PendingFinish == true ? 1 : 0);
                return 1;
            case "IsReverse":
                lua_pushboolean(
                    state,
                    value.AnimationGroup is { Playing: true, Reverse: true } ? 1 : 0);
                return 1;
            case "IsSetToFinalAlpha":
                lua_pushboolean(
                    state,
                    value.AnimationGroup?.SetToFinalAlpha == true ? 1 : 0);
                return 1;
            case "Finish":
                if (value.AnimationGroup is { Playing: true } finishingGroup)
                {
                    var duration = AnimationGroupDuration(runtime, value);
                    var looping =
                        finishingGroup.Looping.Equals(
                            "REPEAT",
                            StringComparison.OrdinalIgnoreCase) ||
                        finishingGroup.Looping.Equals(
                            "BOUNCE",
                            StringComparison.OrdinalIgnoreCase);
                    if (duration > 0.0001 &&
                        (looping || finishingGroup.Elapsed / duration < 1))
                    {
                        finishingGroup.PendingFinish = true;
                    }
                }
                return 0;
            case "Play":
                if (value.AnimationGroup is { } playingGroup)
                {
                    const string usage = "Usage: self:Play([reverse, offset])";
                    if (!TryReadOptionalFloat(state, 3, out var offset))
                        return luaL_error(state, usage);
                    PlayAnimationGroup(
                        runtime,
                        value,
                        playingGroup,
                        OptionalBoolean(state, 2, false),
                        offset);
                }
                else if (value.Animation is { } playingAnimation)
                {
                    PlayAnimation(runtime, value, playingAnimation);
                }
                return 0;
            case "SetPlaying":
                if (!HasRequiredValue(state, 2))
                    return luaL_error(state, "Usage: self:SetPlaying(play)");
                var shouldPlay = lua_toboolean(state, 2) != 0;
                if (value.AnimationGroup is { } settingPlayingGroup)
                {
                    if (shouldPlay)
                    {
                        PlayAnimationGroup(
                            runtime,
                            value,
                            settingPlayingGroup,
                            false,
                            0);
                    }
                    else
                    {
                        StopAnimationGroup(runtime, value, settingPlayingGroup);
                    }
                }
                else if (value.Animation is { } settingPlayingAnimation)
                {
                    if (shouldPlay)
                        PlayAnimation(runtime, value, settingPlayingAnimation);
                    else
                        StopAnimation(runtime, value, settingPlayingAnimation);
                }
                return 0;
            case "Pause":
                if (value.Cooldown is { } pausingCooldown)
                {
                    PauseCooldown(runtime, pausingCooldown);
                    return 0;
                }
                if (value.AnimationGroup is { Playing: true } pausingGroup)
                {
                    runtime.PauseAnimationGroup(value);
                }
                else if (value.Animation is { PlaybackState: 1 } pausingAnimation)
                {
                    if (value.ParentId is { } groupId &&
                        runtime.Ui.Find(groupId) is
                        {
                            AnimationGroup: { Playing: true } parentGroup
                        } groupObject)
                    {
                        runtime.PauseAnimationGroup(groupObject);
                    }
                    else
                    {
                        pausingAnimation.PlaybackState = 2;
                        runtime.InvokeScript(value, "OnPause");
                    }
                }
                return 0;
            case "Resume":
                if (value.Cooldown is { } resumingCooldown)
                    resumingCooldown.Paused = false;
                return 0;
            case "SetPaused":
                if (value.Cooldown is { } pausedCooldown)
                {
                    if (!TryReadRequiredBoolean(state, 2, out var paused))
                        return luaL_error(state, "Usage: self:SetPaused(paused)");
                    if (pausedCooldown.Paused != paused)
                    {
                        if (paused)
                            PauseCooldown(runtime, pausedCooldown);
                        else
                            pausedCooldown.Paused = false;
                    }
                }
                else if (value.ModelScene is not null)
                {
                    const string usage =
                        "Usage: self:SetPaused(paused [, affectsGlobalPause])";
                    if (!TryReadRequiredBoolean(state, 2, out var paused))
                        return luaL_error(state, usage);
                    var affectsGlobalPause = lua_gettop(state) < 3
                        ? true
                        : lua_toboolean(state, 3) != 0;
                    foreach (var childId in value.Children)
                    {
                        if (runtime.Ui.Find(childId) is not
                            {
                                ObjectType: "ModelSceneActor"
                            } actor ||
                            !HasLoadedModel(actor))
                        {
                            continue;
                        }
                        actor.ModelPaused = paused;
                        if (affectsGlobalPause)
                            actor.ModelGlobalPaused = paused;
                    }
                }
                else if (value.ObjectType.Equals(
                             "ModelSceneActor",
                             StringComparison.OrdinalIgnoreCase))
                {
                    const string usage =
                        "Usage: self:SetPaused(paused [, affectsGlobalPause])";
                    if (!TryReadRequiredBoolean(state, 2, out var paused))
                        return luaL_error(state, usage);
                    var affectsGlobalPause = lua_gettop(state) < 3
                        ? true
                        : lua_toboolean(state, 3) != 0;
                    if (HasLoadedModel(value))
                    {
                        value.ModelPaused = paused;
                        if (affectsGlobalPause)
                            value.ModelGlobalPaused = paused;
                    }
                }
                else if (value.ObjectType.EndsWith(
                             "Model",
                             StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryReadRequiredBoolean(state, 2, out var paused))
                        return luaL_error(state, "Usage: self:SetPaused(paused)");
                    if (HasLoadedModel(value))
                        value.ModelPaused = paused;
                }
                return 0;
            case "GetPaused":
                if (value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var hasModel = HasLoadedModel(value);
                    lua_pushboolean(state, hasModel && value.ModelPaused ? 1 : 0);
                    lua_pushboolean(state, hasModel && value.ModelGlobalPaused ? 1 : 0);
                    return 2;
                }
                lua_pushboolean(
                    state,
                    value.ObjectType.EndsWith(
                        "Model",
                        StringComparison.OrdinalIgnoreCase)
                        ? HasLoadedModel(value) && value.ModelPaused ? 1 : 0
                        : value.ModelPaused ? 1 : 0);
                return 1;
            case "AddMessage":
                {
                    const string usage =
                        "Usage: self:AddMessage(text [, color, a, messageID])";
                    if (!TryReadRequiredString(state, 2, out var messageText) ||
                        !TryReadOptionalMessageColor(state, 3, out var messageColor) ||
                        !TryReadOptionalNormalizedByte(state, 6, out var messageAlpha) ||
                        !TryReadOptionalUInt32(state, 7, out var messageId))
                    {
                        return luaL_error(state, usage);
                    }

                    if (value.Messages.Count <= 100)
                    {
                        value.Messages.Add(new UiMessageFrameMessage
                        {
                            Text = messageText,
                            Color = messageColor,
                            Alpha = messageAlpha,
                            MessageId = messageId ?? 0
                        });
                    }
                    return 0;
                }
            case "GetFadeDuration":
                lua_pushnumber(state, value.MessageFadeDuration);
                return 1;
            case "GetFadePower":
                lua_pushnumber(state, value.MessageFadePower);
                return 1;
            case "GetFading":
                lua_pushboolean(state, value.MessageFading ? 1 : 0);
                return 1;
            case "GetFontStringByID":
                if (!TryReadRequiredUInt32(state, 2, out var fontStringMessageId))
                    return luaL_error(
                        state,
                        "Usage: local fontString = self:GetFontStringByID(messageID)");
                runtime.PushObject(
                    FindActiveMessageLine(runtime, value, fontStringMessageId) is { } fontLine
                        ? runtime.Ui.Find(fontLine.FontStringId)
                        : null);
                return 1;
            case "GetInsertMode":
                lua_pushstring(state, value.MessageInsertMode);
                return 1;
            case "GetTimeVisible":
                lua_pushnumber(state, value.MessageTimeVisible);
                return 1;
            case "HasMessageByID":
                if (!TryReadRequiredUInt32(state, 2, out var queriedMessageId))
                    return luaL_error(
                        state,
                        "Usage: local hasMessage = self:HasMessageByID(messageID)");
                lua_pushboolean(
                    state,
                    FindActiveMessageLine(runtime, value, queriedMessageId) is not null ? 1 : 0);
                return 1;
            case "ResetMessageFadeByID":
                {
                    if (!TryReadRequiredUInt32(state, 2, out var resetMessageId))
                        return luaL_error(
                            state,
                            "Usage: self:ResetMessageFadeByID(messageID)");
                    if (FindActiveMessageLine(runtime, value, resetMessageId) is { } resetMessage)
                    {
                        resetMessage.TimeVisible = value.MessageTimeVisible;
                        resetMessage.FadeDuration = value.MessageFadeDuration;
                        if (runtime.Ui.Find(resetMessage.FontStringId) is { } fontString)
                            fontString.Alpha = 1;
                    }
                    return 0;
                }
            case "SetFadeDuration":
                {
                    if (!TryReadRequiredFloat(state, 2, out var messageFadeDuration))
                        return luaL_error(
                            state,
                            "Usage: self:SetFadeDuration(fadeDurationSeconds)");
                    foreach (var message in ActiveMessageLines(value))
                    {
                        if (message.FadeDuration != 0)
                            message.FadeDuration = (float)messageFadeDuration;
                    }
                    value.MessageFadeDuration = (float)messageFadeDuration;
                    return 0;
                }
            case "SetFadePower":
                {
                    if (!TryReadRequiredFloat(state, 2, out var fadePower))
                        return luaL_error(state, "Usage: self:SetFadePower(fadePower)");
                    if (fadePower > 0)
                        value.MessageFadePower = (float)fadePower;
                    return 0;
                }
            case "SetFading":
                {
                    if (!TryReadRequiredBoolean(state, 2, out var fading))
                        return luaL_error(state, "Usage: self:SetFading(fading)");
                    value.MessageFading = fading;
                    return 0;
                }
            case "SetInsertMode":
                {
                    if (!TryReadRequiredString(state, 2, out var insertMode) ||
                        !insertMode.Equals("TOP", StringComparison.OrdinalIgnoreCase) &&
                        !insertMode.Equals("BOTTOM", StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, "Usage: self:SetInsertMode(mode)");
                    }
                    value.MessageInsertMode = insertMode.ToUpperInvariant();
                    LayoutMessageFrameLines(runtime, value);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "SetTimeVisible":
                {
                    if (!TryReadRequiredFloat(state, 2, out var timeVisible))
                        return luaL_error(
                            state,
                            "Usage: self:SetTimeVisible(timeVisibleSeconds)");
                    foreach (var message in ActiveMessageLines(value))
                    {
                        if (message.TimeVisible != 0)
                            message.TimeVisible = (float)timeVisible;
                    }
                    value.MessageTimeVisible = (float)timeVisible;
                    return 0;
                }
            case "Restart":
                if (value.AnimationGroup is { } restartingGroup)
                {
                    const string usage = "Usage: self:Restart([reverse, offset])";
                    if (!TryReadOptionalFloat(state, 3, out var offset))
                        return luaL_error(state, usage);
                    StopAnimationGroup(runtime, value, restartingGroup);
                    PlayAnimationGroup(
                        runtime,
                        value,
                        restartingGroup,
                        OptionalBoolean(state, 2, false),
                        offset);
                }
                else if (value.Animation is { } restartingAnimation)
                {
                    StopAnimation(runtime, value, restartingAnimation);
                    PlayAnimation(runtime, value, restartingAnimation);
                }
                return 0;
            case "Stop":
                if (value.AnimationGroup is { } stoppingGroup)
                    StopAnimationGroup(runtime, value, stoppingGroup);
                else if (value.Animation is { } stoppingAnimation)
                    StopAnimation(runtime, value, stoppingAnimation);
                return 0;
            case "RemoveAnimations":
                runtime.StopAnimationGroup(value, false);
                foreach (var animationObject in value.Children
                             .Select(runtime.Ui.Find)
                             .Where(child => child?.Animation is not null)
                             .Cast<UiObject>()
                             .ToArray())
                {
                    runtime.Ui.Reparent(animationObject, null);
                }
                return 0;
            case "SetAnimationSpeedMultiplier":
                if (value.AnimationGroup is { } speedGroup)
                {
                    if (!TryReadRequiredFloat(state, 2, out var speedMultiplier))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetAnimationSpeedMultiplier(animationSpeedMultiplier)");
                    }
                    speedGroup.AnimationSpeedMultiplier = (float)speedMultiplier;
                }
                return 0;
            case "SetLooping":
                if (value.AnimationGroup is { } loopingGroup)
                {
                    var looping = OptionalString(state, 2)?.ToUpperInvariant();
                    if (looping is not ("NONE" or "REPEAT" or "BOUNCE"))
                        return luaL_error(state, "Usage: self:SetLooping(loopType)");
                    loopingGroup.Looping = looping;
                }
                return 0;
            case "SetToFinalAlpha":
                if (value.AnimationGroup is { } finalAlphaGroup)
                {
                    if (!HasRequiredValue(state, 2))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetToFinalAlpha(setToFinalAlpha)");
                    }
                    finalAlphaGroup.SetToFinalAlpha = lua_toboolean(state, 2) != 0;
                }
                return 0;
            case "SetOrigin":
                if (value.Animation is { } originAnimation)
                {
                    if (!TryReadRequiredFramePoint(state, 2, out var originPoint) ||
                        !TryReadRequiredVector2(state, 3, out var originOffset))
                    {
                        return luaL_error(state, "Usage: self:SetOrigin(point, origin)");
                    }
                    originAnimation.OriginPoint = originPoint;
                    originAnimation.OriginOffset = originOffset;
                }
                return 0;
            case "SetDuration":
                if (value.Animation is { } durationAnimation)
                {
                    if (!TryReadRequiredFloat(state, 2, out var duration))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetDuration(durationSec [, recomputeGroupDuration])");
                    }
                    durationAnimation.Duration = Math.Max(0, duration);
                }
                return 0;
            case "SetChildKey":
                if (value.Animation is { } childKeyAnimation)
                {
                    var childKey = OptionalString(state, 2);
                    if (!HasRequiredValue(state, 2) || childKey is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local success = self:SetChildKey(childKey)");
                    }
                    childKeyAnimation.TargetMode = UiAnimationTargetMode.ChildKey;
                    childKeyAnimation.TargetNameOrKey = childKey;
                    childKeyAnimation.TargetId = null;
                }
                lua_pushboolean(state, 1);
                return 1;
            case "SetEndDelay":
                if (value.Animation is { } endDelayAnimation)
                {
                    if (!TryReadRequiredFloat(state, 2, out var endDelay))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetEndDelay(delaySec [, recomputeGroupDuration])");
                    }
                    endDelayAnimation.EndDelay = Math.Max(0, endDelay);
                }
                return 0;
            case "SetFromAlpha":
                if (value.Animation is { } fromAlphaAnimation)
                {
                    if (!TryReadRequiredFloat(state, 2, out var fromAlpha))
                        return luaL_error(state, "Usage: self:SetFromAlpha(normalizedAlpha)");
                    fromAlphaAnimation.FromAlpha = QuantizeNormalizedByteTruncated(fromAlpha);
                }
                return 0;
            case "SetStartColor":
                if (value.Animation is not { } startColorAnimation ||
                    !TryReadRequiredColorTable(state, 2, out var startColor))
                {
                    return luaL_error(state, "Usage: self:SetStartColor(color)");
                }
                startColorAnimation.StartColor = startColor;
                return 0;
            case "SetEndColor":
                if (value.Animation is not { } endColorAnimation ||
                    !TryReadRequiredColorTable(state, 2, out var endColor))
                {
                    return luaL_error(state, "Usage: self:SetEndColor(color)");
                }
                endColorAnimation.EndColor = endColor;
                return 0;
            case "SetOrder":
                if (value.ControlPoint is { } orderedPoint)
                {
                    if (!TryReadRequiredInt32(state, 2, out var order))
                        return luaL_error(state, "Usage: self:SetOrder(order)");
                    var clampedOrder = Math.Clamp(order, 0, 99);
                    if (value.ParentId is { } pathId &&
                        runtime.Ui.Find(pathId) is { } path)
                    {
                        var existingId = path.Children.FirstOrDefault(childId =>
                            childId != value.Id &&
                            runtime.Ui.Find(childId)?.ControlPoint?.Order == clampedOrder);
                        if (existingId != 0)
                        {
                            path.Children.Remove(value.Id);
                            path.Children.Insert(path.Children.IndexOf(existingId), value.Id);
                        }
                    }
                    orderedPoint.Order = clampedOrder;
                    if (value.ParentId is { } ownerPathId &&
                        runtime.Ui.Find(ownerPathId) is { } ownerPath)
                    {
                        runtime.Ui.ResolvePathControlPoints(ownerPath);
                    }
                }
                else if (value.Animation is { } orderAnimation)
                {
                    if (!TryReadRequiredInt32(state, 2, out var order))
                        return luaL_error(state, "Usage: self:SetOrder(newOrder)");
                    orderAnimation.Order = Math.Clamp(order, 0, 99);
                }
                return 0;
            case "SetCurveType":
                if (value.Animation is { } pathAnimation)
                {
                    var curveType = OptionalString(state, 2)?.ToUpperInvariant();
                    if (curveType is not ("NONE" or "SMOOTH"))
                        return luaL_error(state, "Usage: self:SetCurveType(curveType)");
                    pathAnimation.PathCurveType = curveType;
                }
                return 0;
            case "SetSmoothing":
                if (value.Animation is { } smoothingAnimation)
                {
                    var smoothing = OptionalString(state, 2)?.ToUpperInvariant();
                    if (smoothing is not ("NONE" or "IN" or "OUT" or "IN_OUT" or "OUT_IN"))
                        return luaL_error(state, "Usage: self:SetSmoothing(weights)");
                    smoothingAnimation.Smoothing =
                        smoothing.Equals("OUT_IN", StringComparison.Ordinal)
                            ? "IN_OUT"
                            : smoothing;
                }
                return 0;
            case "SetSmoothProgress":
                if (value.Animation is { } smoothProgressAnimation)
                {
                    if (!TryReadRequiredFloat(state, 2, out var smoothProgress))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetSmoothProgress(durationSec)");
                    }
                    smoothProgressAnimation.SmoothProgress = smoothProgress;
                }
                return 0;
            case "SetStartDelay":
                if (value.Animation is { } startDelayAnimation)
                {
                    if (!TryReadRequiredFloat(state, 2, out var startDelay))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetStartDelay(delaySec [, recomputeGroupDuration])");
                    }
                    startDelayAnimation.StartDelay = Math.Max(0, startDelay);
                }
                return 0;
            case "SetTarget":
                if (value.Animation is not { } targetAnimation ||
                    !HasRequiredValue(state, 2) ||
                    GetObject(runtime, 2) is not { } suppliedAnimationTarget)
                {
                    return luaL_error(
                        state,
                        "Usage: local success = self:SetTarget(target)");
                }
                var validTarget =
                    suppliedAnimationTarget.IsRegion || IsFrameObject(suppliedAnimationTarget);
                if (validTarget)
                {
                    targetAnimation.TargetMode = UiAnimationTargetMode.Direct;
                    targetAnimation.TargetId = suppliedAnimationTarget.Id;
                    targetAnimation.TargetNameOrKey = null;
                }
                lua_pushboolean(state, validTarget ? 1 : 0);
                return 1;
            case "SetTargetKey":
                if (value.Animation is { } targetKeyAnimation)
                {
                    var targetKey = OptionalString(state, 2);
                    if (!HasRequiredValue(state, 2) || targetKey is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local success = self:SetTargetKey(key)");
                    }
                    targetKeyAnimation.TargetMode = UiAnimationTargetMode.TargetKey;
                    targetKeyAnimation.TargetNameOrKey = targetKey;
                    targetKeyAnimation.TargetId = null;
                }
                lua_pushboolean(state, 1);
                return 1;
            case "SetTargetName":
                if (value.Animation is { } targetNameAnimation)
                {
                    var targetName = OptionalString(state, 2);
                    if (!HasRequiredValue(state, 2) || targetName is null)
                    {
                        return luaL_error(
                            state,
                            "Usage: local success = self:SetTargetName(name)");
                    }
                    targetNameAnimation.TargetMode = UiAnimationTargetMode.Name;
                    targetNameAnimation.TargetNameOrKey =
                        ExpandAnimationTargetName(runtime, value, targetName);
                    targetNameAnimation.TargetId = null;
                }
                lua_pushboolean(state, 1);
                return 1;
            case "SetTargetParent":
                if (value.Animation is { } targetParentAnimation)
                {
                    targetParentAnimation.TargetMode = UiAnimationTargetMode.Parent;
                    targetParentAnimation.TargetNameOrKey = null;
                    targetParentAnimation.TargetId = null;
                }
                lua_pushboolean(state, 1);
                return 1;
            case "SetToAlpha":
                if (value.Animation is { } toAlphaAnimation)
                {
                    if (!TryReadRequiredFloat(state, 2, out var toAlpha))
                        return luaL_error(state, "Usage: self:SetToAlpha(normalizedAlpha)");
                    toAlphaAnimation.ToAlpha = QuantizeNormalizedByteTruncated(toAlpha);
                }
                return 0;
            case "GetOffset":
                {
                    var offset = value.ControlPoint?.Offset ??
                                 value.Animation?.Offset ??
                                 default;
                    lua_pushnumber(state, offset.X);
                    lua_pushnumber(state, offset.Y);
                    return 2;
                }
            case "SetOffset":
                if (value.ControlPoint is not null || value.Animation is not null)
                {
                    if (!TryReadRequiredFloat(state, 2, out var offsetX) ||
                        !TryReadRequiredFloat(state, 3, out var offsetY))
                        return luaL_error(state, "Usage: self:SetOffset(offsetX, offsetY)");
                    var offset = new Vector2((float)offsetX, (float)offsetY);
                    offset = offset.LengthSquared() <= 2.3841858e-7f
                        ? Vector2.Zero
                        : offset;
                    if (value.ControlPoint is { } controlPoint)
                        controlPoint.Offset = offset;
                    else
                        value.Animation!.Offset = offset;
                }
                return 0;
            case "GetFlipBookRows":
                lua_pushinteger(state, value.Animation?.FlipBookRows ?? 1);
                return 1;
            case "GetFlipBookColumns":
                lua_pushinteger(state, value.Animation?.FlipBookColumns ?? 1);
                return 1;
            case "GetFlipBookFrames":
                lua_pushinteger(state, value.Animation?.FlipBookFrames ?? 1);
                return 1;
            case "GetFlipBookFrameWidth":
                lua_pushnumber(state, value.Animation?.FlipBookFrameWidth ?? 0);
                return 1;
            case "GetFlipBookFrameHeight":
                lua_pushnumber(state, value.Animation?.FlipBookFrameHeight ?? 0);
                return 1;
            case "SetFlipBookRows":
                if (value.Animation is { } rowAnimation)
                {
                    if (!TryReadRequiredUInt32(state, 2, out var rows))
                        return luaL_error(state, "Usage: self:SetFlipBookRows(rows)");
                    rowAnimation.FlipBookRows = rows;
                }
                return 0;
            case "SetFlipBookColumns":
                if (value.Animation is { } columnAnimation)
                {
                    if (!TryReadRequiredUInt32(state, 2, out var columns))
                        return luaL_error(state, "Usage: self:SetFlipBookColumns(columns)");
                    columnAnimation.FlipBookColumns = columns;
                }
                return 0;
            case "SetFlipBookFrames":
                if (value.Animation is { } frameAnimation)
                {
                    if (!TryReadRequiredUInt32(state, 2, out var frames))
                        return luaL_error(state, "Usage: self:SetFlipBookFrames(frames)");
                    frameAnimation.FlipBookFrames = frames;
                }
                return 0;
            case "SetFlipBookFrameWidth":
                if (value.Animation is { } frameWidthAnimation)
                {
                    if (!TryReadRequiredUInt32(state, 2, out var frameWidth))
                        return luaL_error(state, "Usage: self:SetFlipBookFrameWidth(width)");
                    frameWidthAnimation.FlipBookFrameWidth = frameWidth;
                }
                return 0;
            case "SetFlipBookFrameHeight":
                if (value.Animation is { } frameHeightAnimation)
                {
                    if (!TryReadRequiredUInt32(state, 2, out var frameHeight))
                        return luaL_error(state, "Usage: self:SetFlipBookFrameHeight(height)");
                    frameHeightAnimation.FlipBookFrameHeight = frameHeight;
                }
                return 0;
            case "SetDegrees":
                if (value.Animation is { } rotationAnimation)
                {
                    if (!TryReadRequiredFloat(state, 2, out var degrees))
                        return luaL_error(state, "Usage: self:SetDegrees(angle)");
                    rotationAnimation.Degrees = (float)degrees;
                }
                return 0;
            case "SetRadians":
                if (value.Animation is { } radiansAnimation)
                {
                    if (!TryReadRequiredFloat(state, 2, out var radians))
                        return luaL_error(state, "Usage: self:SetRadians(angle)");
                    radiansAnimation.Radians = (float)radians;
                }
                return 0;
            case "SetScaleFrom":
                if (value.Animation is { } setScaleFromState)
                {
                    if (!TryReadRequiredVector2(state, 2, out var scaleFrom))
                        return luaL_error(state, "Usage: self:SetScaleFrom(scale)");
                    setScaleFromState.HasScaleRange = true;
                    setScaleFromState.ScaleFrom =
                        Vector2.Max(scaleFrom, new Vector2(.001f));
                }
                return 0;
            case "SetScaleTo":
                if (value.Animation is { } setScaleToState)
                {
                    if (!TryReadRequiredVector2(state, 2, out var scaleTo))
                        return luaL_error(state, "Usage: self:SetScaleTo(scale)");
                    setScaleToState.HasScaleRange = true;
                    setScaleToState.ScaleTo =
                        Vector2.Max(scaleTo, new Vector2(.001f));
                    setScaleToState.Scale = setScaleToState.ScaleTo;
                }
                return 0;
            case "SetGradientMask":
                {
                    var gradients = new sbyte[4];
                    for (var index = 0; index < gradients.Length; index++)
                    {
                        if (!TryReadOptionalInt8(
                                state,
                                index + 2,
                                out gradients[index]))
                        {
                            return luaL_error(
                                state,
                                value.ObjectType.Equals(
                                    "ModelSceneActor",
                                    StringComparison.OrdinalIgnoreCase)
                                    ? "Usage: self:SetGradientMask(" +
                                      "gradientIndex0, gradientIndex1, " +
                                      "gradientIndex2, gradientIndex3)"
                                    : "Usage: self:SetGradientMask(" +
                                      "grad0, grad1, grad2, grad3)");
                        }
                    }

                    var isActor = value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase);
                    var enabled = isActor
                        ? gradients[0] != 0
                        : gradients[0] != 0 ||
                          gradients[1] != 0 ||
                          gradients[2] != 0;
                    value.ModelRenderEffectKind = enabled
                        ? UiModelRenderEffectKind.GradientMask
                        : UiModelRenderEffectKind.None;
                    value.ModelGradientMaskEnabled = enabled;
                    value.ModelShadowEffectStrength = 0;
                    value.ModelShadowEffectState = null;
                    value.ModelDissolveEffectState = null;
                    value.ModelEdgeGlowEffectState = null;
                    Array.Clear(value.ModelGradientMaskIndices);
                    if (enabled)
                        gradients.CopyTo(value.ModelGradientMaskIndices, 0);
                    return 0;
                }
            case "SetGradientMaskWithDyes":
                {
                    const string usage =
                        "Usage: self:SetGradientMaskWithDyes(" +
                        "[grad0DyeColorID, grad1DyeColorID, grad2DyeColorID])";
                    var anyDyeSupplied = false;
                    var dyeColorIds = new int?[3];
                    for (var index = 0; index < 3; index++)
                    {
                        var argumentIndex = index + 2;
                        if (!HasRequiredValue(state, argumentIndex))
                            continue;
                        if (!TryReadRequiredInt32(
                                state,
                                argumentIndex,
                                out var dyeColorId))
                        {
                            return luaL_error(state, usage);
                        }
                        dyeColorIds[index] = dyeColorId;
                        anyDyeSupplied = true;
                    }
                    dyeColorIds.CopyTo(value.ModelGradientDyeColorIds, 0);
                    Array.Clear(value.ModelGradientDyeTextureIndices);
                    for (var index = 0; index < dyeColorIds.Length; index++)
                    {
                        if (dyeColorIds[index] is not { } dyeColorId ||
                            runtime.DyeColorProvider is not { } dyeColorProvider ||
                            !dyeColorProvider.TryGetGradientTextureIndex(
                                dyeColorId,
                                out var gradientTextureIndex))
                        {
                            continue;
                        }

                        value.ModelGradientDyeTextureIndices[index] =
                            gradientTextureIndex;
                    }
                    value.ModelGradientDyesEnabled = anyDyeSupplied;
                    return 0;
                }
            case "SetAnimation":
                {
                    if (!value.ObjectType.Equals(
                            "ModelSceneActor",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        const string usage =
                            "Usage: self:SetAnimation(anim [, variation])";
                        if (!TryReadRequiredUInt32(state, 2, out var characterAnimation) ||
                            characterAnimation > 1857)
                        {
                            return luaL_error(state, usage);
                        }

                        value.ModelAnimationId = (ushort)characterAnimation;
                        value.ModelAnimationFrozenFrame = -1;
                        if (HasRequiredValue(state, 3))
                        {
                            if (!TryReadRequiredUInt32(
                                    state,
                                    3,
                                    out var characterVariation))
                            {
                                return luaL_error(state, usage);
                            }
                            value.ModelAnimationVariation =
                                unchecked((int)characterVariation);
                        }
                        value.ModelAnimationSpeed = 1;
                        value.ModelAnimationTimeOffsetMilliseconds = 0;
                        CancelCharacterRotationAnimation(value);
                        if (HasLoadedModel(value) &&
                            value.ModelAnimationKitId is null)
                            ApplyModelSceneActorAnimationState(runtime, value);
                        return 0;
                    }

                    if (lua_gettop(state) < 2 || lua_isnumber(state, 2) == 0)
                    {
                        runtime.Log.Warn("ui", "SetAnimation: Invalid animation type used");
                        return 0;
                    }

                    var animation = unchecked((ushort)(long)lua_tonumber(state, 2));
                    if (animation > 1857)
                        return 0;

                    var variation = -1;
                    if (lua_gettop(state) >= 3 && lua_isnumber(state, 3) != 0)
                        variation = unchecked((int)(long)lua_tonumber(state, 3));

                    var speed = 1.0;
                    if (lua_gettop(state) >= 4 && lua_isnil(state, 4) == 0)
                    {
                        if (lua_isnumber(state, 4) == 0)
                            return luaL_error(state, "SetAnimation: animation speed must be a number");
                        speed = lua_tonumber(state, 4);
                    }

                    var timeOffsetSeconds = 0.0;
                    if (lua_gettop(state) >= 5 && lua_isnil(state, 5) == 0)
                    {
                        if (lua_isnumber(state, 5) == 0)
                            return luaL_error(state, "SetAnimation: time offset must be a number");
                        timeOffsetSeconds = lua_tonumber(state, 5);
                    }

                    value.ModelAnimationId = animation;
                    value.ModelAnimationVariation = variation;
                    value.ModelAnimationSpeed = (float)speed;
                    value.ModelAnimationTimeOffsetMilliseconds =
                        ConvertAnimationTimeOffsetToMilliseconds(timeOffsetSeconds);
                    if (HasLoadedModel(value))
                        ApplyModelSceneActorAnimationState(runtime, value);
                    return 0;
                }
            case "SetAnimationBlendOperation":
                if (!TryReadRequiredInt32(state, 2, out var blendOperation) ||
                    blendOperation is < 0 or > 1)
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetAnimationBlendOperation(blendOp)");
                }
                value.ModelAnimationBlendOperation = blendOperation;
                return 0;
            case "GetAnimation":
                lua_pushinteger(state, value.ModelAnimationId);
                return 1;
            case "GetAnimationVariation":
                lua_pushinteger(state, value.ModelAnimationVariation);
                return 1;
            case "GetAnimationBlendOperation":
                lua_pushinteger(state, value.ModelAnimationBlendOperation);
                return 1;
            case "AttachToMount":
                {
                    const string usage =
                        "Usage: local success = self:AttachToMount(rider, animation " +
                        "[, spellKitVisualID])";
                    var rider = GetObject(runtime, 2);
                    if (rider is null ||
                        !rider.ObjectType.Equals(
                            "ModelSceneActor",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, usage);
                    }

                    ushort riderAnimation = 0;
                    if (lua_gettop(state) >= 3 && lua_isnil(state, 3) == 0)
                    {
                        if (lua_type(state, 3) != LUA_TNUMBER)
                            return luaL_error(state, usage);
                        riderAnimation = unchecked((ushort)(long)lua_tonumber(state, 3));
                        if (riderAnimation > 1857)
                            return luaL_error(state, usage);
                    }

                    var spellVisualKitId = 0;
                    var hasSpellVisualKit =
                        lua_gettop(state) >= 4 && lua_isnil(state, 4) == 0;
                    if (hasSpellVisualKit &&
                        !TryReadRequiredInt32(state, 4, out spellVisualKitId))
                    {
                        return luaL_error(state, usage);
                    }

                    var attached = HasLoadedModel(value) && HasLoadedModel(rider);
                    if (attached)
                    {
                        value.ModelMountedRiderActorId = rider.Id;
                        rider.ModelMountedToActorId = value.Id;
                        rider.ModelAnimationId =
                            riderAnimation == 0 ? (ushort)91 : riderAnimation;
                        rider.ModelAnimationVariation = 0;
                        rider.ModelAnimationSpeed = 1;
                        rider.ModelAnimationTimeOffsetMilliseconds = 0;
                        ApplyModelSceneActorAnimationState(runtime, rider);
                        if (hasSpellVisualKit)
                        {
                            rider.ModelSpellVisualKitId =
                                unchecked((uint)spellVisualKitId);
                            rider.ModelSpellVisualOneShot = false;
                        }
                    }
                    lua_pushboolean(state, attached ? 1 : 0);
                    return 1;
                }
            case "CalculateMountScale":
                {
                    var rider = GetObject(runtime, 2);
                    if (rider is null ||
                        !rider.ObjectType.Equals(
                            "ModelSceneActor",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(
                            state,
                            "Usage: local scale = self:CalculateMountScale(rider)");
                    }

                    lua_pushnumber(state, 1);
                    return 1;
                }
            case "DetachFromMount":
                {
                    const string usage =
                        "Usage: local success = self:DetachFromMount(rider)";
                    var rider = GetObject(runtime, 2);
                    if (rider is null ||
                        !rider.ObjectType.Equals(
                            "ModelSceneActor",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, usage);
                    }

                    var detached = HasLoadedModel(value) && HasLoadedModel(rider);
                    if (detached)
                    {
                        if (value.ModelMountedRiderActorId == rider.Id)
                            value.ModelMountedRiderActorId = null;
                        if (rider.ModelMountedToActorId == value.Id)
                            rider.ModelMountedToActorId = null;
                    }
                    lua_pushboolean(state, detached ? 1 : 0);
                    return 1;
                }
            case "PlayAnimationKit":
                if (!TryReadRequiredInt32(state, 2, out var animationKitId))
                {
                    return luaL_error(
                        state,
                        "Usage: self:PlayAnimationKit(animationKit [, isLooping])");
                }
                var animationKitLooping = OptionalBoolean(state, 3, false);
                if (HasLoadedModel(value) &&
                    runtime.ModelResourceProvider is { } animationKitProvider &&
                    animationKitProvider.TryGetAnimationKit(
                        animationKitId,
                        out _))
                {
                    PlayCharacterModelAnimationKit(
                        runtime,
                        value,
                        animationKitId,
                        animationKitLooping);
                }
                else
                {
                    if (value.ModelAnimationKitLooping)
                    {
                        StopCharacterModelAnimationKit(
                            runtime,
                            value,
                            restoreBase: HasLoadedModel(value));
                    }
                    else if (!HasLoadedModel(value))
                    {
                        ClearCharacterModelAnimationKitState(value);
                    }
                    value.ModelAnimationKitId = animationKitId;
                    value.ModelAnimationKitLooping = animationKitLooping;
                }
                return 0;
            case "StopAnimationKit":
                StopCharacterModelAnimationKit(
                    runtime,
                    value,
                    restoreBase: true);
                return 0;
            case "UseUnitSheatheCategory":
                if (!TryReadRequiredBoolean(state, 2, out var useUnitSheatheCategory))
                {
                    return luaL_error(
                        state,
                        "Usage: self:UseUnitSheatheCategory(useCategory)");
                }
                value.ModelUsesUnitSheatheCategory = useUnitSheatheCategory;
                return 0;
            case "SetSpellVisualKit":
                {
                    if (value.ObjectType.Equals(
                            "CinematicModel",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (!TryReadRequiredUInt32(
                                state,
                                2,
                                out var cinematicVisualKitId))
                        {
                            return luaL_error(
                                state,
                                "Usage: self:SetSpellVisualKit(visualKitID)");
                        }
                        value.ModelSpellVisualKitId = cinematicVisualKitId;
                        value.ModelSpellVisualOneShot = true;
                        return 0;
                    }

                    var visualKitId = 0;
                    if (lua_gettop(state) >= 2 &&
                        lua_isnil(state, 2) == 0 &&
                        !TryReadRequiredInt32(state, 2, out visualKitId))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetSpellVisualKit([spellVisualKitID, oneShot])");
                    }
                    value.ModelSpellVisualKitId = unchecked((uint)visualKitId);
                    value.ModelSpellVisualOneShot = OptionalBoolean(state, 3, false);
                    return 0;
                }
            case "Dress":
                return 0;
            case "DressPlayerSlot":
                if (!TryReadRequiredUInt32(state, 2, out var playerInventorySlot) ||
                    playerInventorySlot == 0)
                {
                    return luaL_error(
                        state,
                        "Usage: self:DressPlayerSlot(invSlot)");
                }
                return 0;
            case "ReleaseFrontEndCharacterDisplays":
                lua_pushboolean(state, 1);
                return 1;
            case "ResetNextHandSlot":
                runtime.NextTryOnUsesOffHand =
                    value.ModelItemTransmogInfoBySlot.ContainsKey(16);
                return 0;
            case "SetFrontEndLobbyModelFromDefaultCharacterDisplay":
                if (!TryReadRequiredUInt32(state, 2, out _))
                {
                    return luaL_error(
                        state,
                        "Usage: local success = self:" +
                        "SetFrontEndLobbyModelFromDefaultCharacterDisplay(" +
                        "characterIndex)");
                }
                lua_pushboolean(state, 0);
                return 1;
            case "Undress":
                {
                    var includeWeapons = OptionalBoolean(state, 2, true);
                    foreach (var slot in VisibleArmorInventorySlots)
                        value.ModelItemTransmogInfoBySlot.Remove(slot);
                    if (includeWeapons)
                    {
                        value.ModelItemTransmogInfoBySlot.Remove(16);
                        value.ModelItemTransmogInfoBySlot.Remove(17);
                        value.ModelMainHandUsesPairedWeapon = false;
                    }
                    return 0;
                }
            case "UndressSlot":
                if (!TryReadRequiredInt32(state, 2, out var undressInventorySlot))
                {
                    return luaL_error(
                        state,
                        "Usage: self:UndressSlot(inventorySlots)");
                }
                if (VisibleAppearanceInventorySlots.Contains(undressInventorySlot))
                {
                    value.ModelItemTransmogInfoBySlot.Remove(undressInventorySlot);
                    if (undressInventorySlot is 16 or 17)
                        value.ModelMainHandUsesPairedWeapon = false;
                }
                return 0;
            case "GetAutoDress":
                lua_pushboolean(state, value.ModelAutoDress ? 1 : 0);
                return 1;
            case "GetKeepModelOnHide":
                lua_pushboolean(state, value.ModelKeepModelOnHide ? 1 : 0);
                return 1;
            case "GetDoBlend":
                lua_pushboolean(state, value.ModelDoBlend ? 1 : 0);
                return 1;
            case "GetObeyHideInTransmogFlag":
                lua_pushboolean(state, value.ModelObeyHideInTransmogFlag ? 1 : 0);
                return 1;
            case "GetSheathed":
                lua_pushboolean(state, value.ModelSheathed ? 1 : 0);
                return 1;
            case "GetUseTransmogChoices":
                lua_pushboolean(state, value.ModelUseTransmogChoices ? 1 : 0);
                return 1;
            case "GetUseTransmogSkin":
                lua_pushboolean(state, value.ModelUseTransmogSkin ? 1 : 0);
                return 1;
            case "SetAutoDress":
                if (!TryReadRequiredBoolean(state, 2, out var autoDress))
                    return luaL_error(state, "Usage: self:SetAutoDress(autoDress)");
                value.ModelAutoDress = autoDress;
                return 0;
            case "SetObeyHideInTransmogFlag":
                {
                    var isActor = value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase);
                    var actorObeyHide = false;
                    if (isActor &&
                        !TryReadRequiredBoolean(state, 2, out actorObeyHide))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetObeyHideInTransmogFlag(obey)");
                    }
                    value.ModelObeyHideInTransmogFlag = isActor
                        ? actorObeyHide
                        : OptionalBoolean(state, 2, false);
                    return 0;
                }
            case "SetUseTransmogChoices":
                {
                    var isActor = value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase);
                    var actorUseChoices = false;
                    if (isActor &&
                        !TryReadRequiredBoolean(state, 2, out actorUseChoices))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetUseTransmogChoices(use)");
                    }
                    value.ModelUseTransmogChoices = isActor
                        ? actorUseChoices
                        : OptionalBoolean(state, 2, false);
                    return 0;
                }
            case "SetUseTransmogSkin":
                {
                    var isActor = value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase);
                    var actorUseSkin = false;
                    if (isActor &&
                        !TryReadRequiredBoolean(state, 2, out actorUseSkin))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetUseTransmogSkin(use)");
                    }
                    value.ModelUseTransmogSkin = isActor
                        ? actorUseSkin
                        : OptionalBoolean(state, 2, false);
                    return 0;
                }
            case "SetSheathed":
                {
                    var isActor = value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase);
                    var actorSheathed = false;
                    if (isActor &&
                        !TryReadRequiredBoolean(state, 2, out actorSheathed))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetSheathed(sheathed [, hidden])");
                    }
                    value.ModelSheathed = isActor
                        ? actorSheathed
                        : OptionalBoolean(state, 2, false);
                    value.ModelHideWeapons = OptionalBoolean(state, 3, false);
                    return 0;
                }
            case "SetSheathedCategory":
                {
                    if (!TryReadRequiredInt32(state, 2, out var inventorySlot) ||
                        !TryReadRequiredInt32(state, 3, out var category) ||
                        category is < 0 or > 3)
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetSheathedCategory(inventorySlots, category)");
                    }

                    switch (inventorySlot)
                    {
                        case 16:
                            value.ModelMainHandSheathedCategory = (byte)category;
                            break;
                        case 17:
                            value.ModelOffHandSheathedCategory = (byte)category;
                            break;
                    }
                    return 0;
                }
            case "SetUseCenterForOrigin":
                value.ModelUseCenterForOriginX = OptionalBoolean(state, 2, false);
                value.ModelUseCenterForOriginY = OptionalBoolean(state, 3, false);
                value.ModelUseCenterForOriginZ = OptionalBoolean(state, 4, false);
                return 0;
            case "IsUsingCenterForOrigin":
                lua_pushboolean(state, value.ModelUseCenterForOriginX ? 1 : 0);
                lua_pushboolean(state, value.ModelUseCenterForOriginY ? 1 : 0);
                lua_pushboolean(state, value.ModelUseCenterForOriginZ ? 1 : 0);
                return 3;
            case "GetParticleOverrideScale":
                if (value.ModelParticleOverrideScale is { } currentParticleOverrideScale)
                    lua_pushnumber(state, currentParticleOverrideScale);
                else
                    lua_pushnil(state);
                return 1;
            case "SetParticleOverrideScale":
                if (lua_gettop(state) < 2 || lua_isnil(state, 2) != 0)
                {
                    value.ModelParticleOverrideScale = null;
                    return 0;
                }
                if (!TryReadRequiredFloat(state, 2, out var particleOverrideScale))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetParticleOverrideScale([scale])");
                }
                value.ModelParticleOverrideScale =
                    MathF.Max((float)particleOverrideScale, .01f);
                return 0;
            case "IsPreferringModelCollisionBounds":
                lua_pushboolean(state, value.ModelPreferCollisionBounds ? 1 : 0);
                return 1;
            case "SetPreferModelCollisionBounds":
                if (!TryReadRequiredBoolean(state, 2, out var preferCollisionBounds))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetPreferModelCollisionBounds(" +
                        "preferCollisionBounds)");
                }
                value.ModelPreferCollisionBounds = preferCollisionBounds;
                UpdateModelSceneActorActiveBoundingBox(value);
                return 0;
            case "GetModelUnitGUID":
                PushOptionalString(
                    state,
                    value.ModelUnitToken is { } modelUnitToken
                        ? runtime.Units.Find(modelUnitToken)?.Guid
                        : null);
                return 1;
            case "SetPlayerModelFromGlues":
                {
                    const string usage =
                        "Usage: local success = self:SetPlayerModelFromGlues(" +
                        "[characterIndex, sheatheWeapons, autoDress, hideWeapons, " +
                        "usePlayerNativeForm, customRaceID])";
                    if (!TryReadOptionalInt32(state, 2, 0, out _) ||
                        !TryReadOptionalInt32(state, 7, -1, out _))
                    {
                        return luaL_error(state, usage);
                    }

                    _ = lua_gettop(state) < 3
                        ? false
                        : lua_toboolean(state, 3) != 0;
                    _ = lua_gettop(state) < 4 ||
                        lua_toboolean(state, 4) != 0;
                    _ = lua_gettop(state) < 5
                        ? false
                        : lua_toboolean(state, 5) != 0;
                    _ = lua_gettop(state) < 6 ||
                        lua_toboolean(state, 6) != 0;

                    lua_pushboolean(state, 0);
                    return 1;
                }
            case "SetDoBlend":
                value.ModelDoBlend = OptionalBoolean(state, 2, false);
                return 0;
            case "SetKeepModelOnHide":
                if (!TryReadRequiredBoolean(state, 2, out var keepModelOnHide))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetKeepModelOnHide(keepModelOnHide)");
                }
                value.ModelKeepModelOnHide = keepModelOnHide;
                if (!value.Shown && !keepModelOnHide)
                    ClearModel(runtime, value);
                return 0;
            case "SetLight":
                if (!TryReadRequiredBoolean(state, 2, out var modelLightEnabled) ||
                    !TryReadRequiredModelLight(state, 3, out var modelLight))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetLight(enabled, light)");
                }
                value.ModelLightEnabled = modelLightEnabled;
                value.ModelLight = modelLightEnabled
                    ? modelLight
                    : new UiModelLightState();
                return 0;
            case "GetLight":
                lua_pushboolean(state, value.ModelLightEnabled ? 1 : 0);
                PushModelLight(state, value.ModelLight);
                return 2;
            case "SetUnit":
                {
                    const string usage =
                        "Usage: local success = self:SetUnit(" +
                        "unit [, blend, useNativeForm])";
                    if (!HasRequiredValue(state, 2) ||
                        !TryReadOptionalString(state, 2, out var unitToken) ||
                        unitToken is null)
                    {
                        return luaL_error(state, usage);
                    }

                    var blend = OptionalBoolean(state, 3, true);
                    var useNativeForm = OptionalBoolean(state, 4, true);
                    ResetCharacterModelSourceState(value);
                    value.ModelUnitToken = unitToken;
                    value.ModelGuildTabardInfo = ResolveModelGuildTabardInfo(
                        runtime,
                        unitToken);
                    value.ModelDoBlend = blend;
                    value.ModelUseNativeForm = useNativeForm;

                    var success = runtime.Units.Find(unitToken) is not null &&
                                  (value.Shown || value.ModelKeepModelOnHide);
                    if (success)
                    {
                        value.ModelResourceLoaded = true;
                        runtime.InvokeScript(value, "OnModelLoaded");
                    }
                    lua_pushboolean(state, success ? 1 : 0);
                    return 1;
                }
            case "SetFacingLeft":
                value.ModelFacingLeft = OptionalBoolean(state, 2, false);
                value.ModelCameraRefreshRevision++;
                return 0;
            case "SetAnimOffset":
                if (!TryReadRequiredFloat(state, 2, out var animationOffset))
                    return luaL_error(state, "Usage: self:SetAnimOffset(offset)");
                value.ModelAnimationOffset = (float)animationOffset;
                return 0;
            case "EquipItem":
                if (!TryReadRequiredInt32(state, 2, out var equippedItemId))
                    return luaL_error(state, "Usage: self:EquipItem(itemID)");
                value.ModelCinematicEquippedItemIds.Add(equippedItemId);
                return 0;
            case "SetCreatureData":
                if (!TryReadRequiredInt32(state, 2, out var cinematicCreatureId))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetCreatureData(creatureID)");
                }
                value.ModelCreatureId = cinematicCreatureId;
                return 0;
            case "UnequipItems":
                value.ModelCinematicEquippedItemIds.Clear();
                return 0;
            case "SetFadeTimes":
                {
                    const string usage =
                        "Usage: self:SetFadeTimes(fadeInSeconds, fadeOutSeconds)";
                    if (!TryReadRequiredFloat(state, 2, out var fadeInSeconds) ||
                        !TryReadRequiredFloat(state, 3, out var fadeOutSeconds))
                    {
                        return luaL_error(state, usage);
                    }
                    value.ModelCinematicFadeInSeconds = (float)fadeInSeconds;
                    value.ModelCinematicFadeOutSeconds = (float)fadeOutSeconds;
                    return 0;
                }
            case "SetJumpInfo":
                {
                    const string usage =
                        "Usage: self:SetJumpInfo(jumpLength, jumpHeight)";
                    if (!TryReadRequiredFloat(state, 2, out var jumpLength) ||
                        !TryReadRequiredFloat(state, 3, out var jumpHeight))
                    {
                        return luaL_error(state, usage);
                    }
                    value.ModelCinematicJumpLength = (float)jumpLength;
                    value.ModelCinematicJumpHeight = (float)jumpHeight;
                    return 0;
                }
            case "StartPan":
                {
                    const string usage =
                        "Usage: self:StartPan(panType, durationSeconds " +
                        "[, doFade, visKitID, startPositionScale, speedMultiplier])";
                    if (!TryReadRequiredUInt32(state, 2, out var oneBasedPanType) ||
                        !TryReadRequiredFloat(state, 3, out var panDurationSeconds) ||
                        !TryReadOptionalInt32(state, 5, 0, out var panVisualKitId) ||
                        !TryReadOptionalFloat(
                            state,
                            6,
                            0,
                            out var panStartPositionScale) ||
                        !TryReadOptionalFloat(
                            state,
                            7,
                            1,
                            out var panSpeedMultiplier))
                    {
                        return luaL_error(state, usage);
                    }

                    if (panDurationSeconds > 0)
                    {
                        value.ModelCinematicPanType =
                            unchecked((int)(oneBasedPanType - 1));
                        value.ModelCinematicPanDurationSeconds =
                            (float)panDurationSeconds;
                        value.ModelCinematicPanElapsedSeconds = 0;
                        value.ModelCinematicPanDoFade =
                            OptionalBoolean(state, 4, false);
                        value.ModelCinematicPanVisualKitId = panVisualKitId;
                        value.ModelCinematicPanStartPositionScale =
                            (float)panStartPositionScale;
                        value.ModelCinematicPanSpeedMultiplier =
                            (float)panSpeedMultiplier;
                        value.ModelCinematicPanActive = true;
                    }
                    return 0;
                }
            case "StopPan":
                value.ModelCinematicPanElapsedSeconds =
                    value.ModelCinematicPanDurationSeconds;
                return 0;
            case "SetHeightFactor":
                if (!TryReadRequiredFloat(state, 2, out var heightFactor))
                    return luaL_error(state, "Usage: self:SetHeightFactor(factor)");
                value.ModelHeightFactor = (float)heightFactor;
                value.ModelCameraRefreshRevision++;
                return 0;
            case "SetTargetDistance":
                if (!TryReadRequiredFloat(state, 2, out var targetDistance))
                    return luaL_error(state, "Usage: self:SetTargetDistance(scale)");
                value.ModelTargetDistance = (float)targetDistance;
                value.ModelCameraRefreshRevision++;
                return 0;
            case "SetPanDistance":
                if (!TryReadRequiredFloat(state, 2, out var panDistance))
                    return luaL_error(state, "Usage: self:SetPanDistance(scale)");
                value.ModelPanDistance = (float)panDistance;
                value.ModelCameraRefreshRevision++;
                return 0;
            case "InitializeCamera":
                if (!TryReadOptionalFloat(state, 2, out var cameraScaleFactor))
                {
                    return luaL_error(
                        state,
                        "Usage: self:InitializeCamera([scaleFactor])");
                }
                if (cameraScaleFactor > 0)
                    value.ModelCameraScaleFactor = (float)cameraScaleFactor;
                value.ModelCameraRefreshRevision++;
                return 0;
            case "InitializePanCamera":
                if (!TryReadOptionalFloat(state, 2, out var panCameraScaleFactor))
                {
                    return luaL_error(
                        state,
                        "Usage: self:InitializePanCamera([scaleFactor])");
                }
                if (panCameraScaleFactor > 0)
                    value.ModelCameraScaleFactor = (float)panCameraScaleFactor;
                value.ModelTargetDistance = 0.3f;
                value.ModelCameraRefreshRevision++;
                return 0;
            case "SetSecurityDisableSetText":
                value.SecurityDisableSetText = true;
                return 0;
            case "GetItemTransmogInfoList":
                {
                    lua_newtable(state);
                    var resultTable = AbsoluteIndex(state, -1);
                    for (var inventorySlot = 1; inventorySlot <= 19; inventorySlot++)
                    {
                        var info = value.ModelItemTransmogInfoBySlot.GetValueOrDefault(
                            inventorySlot);
                        if (inventorySlot == 16)
                        {
                            info = info with
                            {
                                SecondaryAppearanceId =
                                    value.ModelMainHandUsesPairedWeapon ? 0 : -1
                            };
                        }
                        else if (inventorySlot == 17 &&
                                 value.ModelMainHandUsesPairedWeapon)
                        {
                            info = default;
                        }
                        PushItemTransmogInfo(state, info);
                        lua_rawseti(state, resultTable, inventorySlot);
                    }
                    return 1;
                }
            case "GetItemTransmogInfo":
                {
                    if (!TryReadRequiredInt32(state, 2, out var transmogInventorySlot))
                    {
                        return luaL_error(
                            state,
                            "Usage: local itemTransmogInfo = " +
                            "self:GetItemTransmogInfo(inventorySlots)");
                    }
                    if (transmogInventorySlot is < 1 or > 105 ||
                        !value.ModelItemTransmogInfoBySlot.TryGetValue(
                            transmogInventorySlot,
                            out var transmogInfo))
                    {
                        return 0;
                    }
                    PushItemTransmogInfo(state, transmogInfo);
                    return 1;
                }
            case "SetItemTransmogInfo":
                {
                    const string usage =
                        "Usage: local result = self:SetItemTransmogInfo(" +
                        "transmogInfo [, inventorySlots, ignoreChildItems])";
                    if (!TryReadItemTransmogInfo(state, 2, out var transmogInfo))
                        return luaL_error(state, usage);

                    int? requestedInventorySlot = null;
                    if (HasRequiredValue(state, 3))
                    {
                        if (!TryReadRequiredInt32(
                                state,
                                3,
                                out var parsedInventorySlot))
                        {
                            return luaL_error(state, usage);
                        }
                        requestedInventorySlot = parsedInventorySlot;
                    }
                    _ = OptionalBoolean(state, 4, false);

                    var resolvedInventorySlot = requestedInventorySlot switch
                    {
                        16 => 16,
                        17 => 17,
                        _ => 0
                    };
                    if (resolvedInventorySlot == 0)
                    {
                        lua_pushinteger(state, 2);
                        return 1;
                    }

                    value.ModelItemTransmogInfoBySlot[resolvedInventorySlot] =
                        transmogInfo;
                    if (resolvedInventorySlot == 16)
                    {
                        value.ModelMainHandUsesPairedWeapon =
                            transmogInfo.SecondaryAppearanceId != -1;
                        if (value.ModelMainHandUsesPairedWeapon)
                            value.ModelItemTransmogInfoBySlot.Remove(17);
                    }
                    else
                    {
                        value.ModelMainHandUsesPairedWeapon = false;
                    }
                    lua_pushinteger(state, 0);
                    return 1;
                }
            case "TryOn":
                {
                    var isActor = value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase);
                    var usage = isActor
                        ? "Usage: local reason = self:TryOn(" +
                          "itemLinkOrItemModifiedAppearanceID " +
                          "[, handSlotName, spellEnchantmentID])"
                        : "Usage: local result = self:TryOn(" +
                          "linkOrItemModifiedAppearanceID " +
                          "[, handSlotName, spellEnchantID])";
                    if (!HasRequiredValue(state, 2) ||
                        lua_isstring(state, 2) == 0 ||
                        lua_istable(state, 2) != 0)
                    {
                        return luaL_error(state, usage);
                    }
                    var item = lua_tostring(state, 2);
                    if (item is null ||
                        !TryReadOptionalString(state, 3, out _) ||
                        HasRequiredValue(state, 4) &&
                        !TryReadRequiredInt32(state, 4, out _))
                    {
                        return luaL_error(state, usage);
                    }

                    if (item.Length == 0 ||
                        TryParseNonzeroNativeInteger(item, out _))
                    {
                        return 0;
                    }
                    if (!isActor)
                        return 0;

                    lua_pushinteger(state, 2);
                    return 1;
                }
            case "CanSetUnit":
                if (!HasRequiredValue(state, 2) ||
                    lua_isstring(state, 2) == 0)
                {
                    return luaL_error(state, "Usage: self:CanSetUnit(unit)");
                }
                return 0;
            case "HasAnimation":
                if (!TryReadRequiredUInt32(state, 2, out var queriedAnimation) ||
                    queriedAnimation > 1857)
                {
                    return luaL_error(
                        state,
                        "Usage: local hasAnimation = self:HasAnimation(anim)");
                }
                lua_pushboolean(
                    state,
                    HasLoadedModel(value) &&
                    value.ModelAvailableAnimationIds.Contains(
                        (ushort)queriedAnimation)
                        ? 1
                        : 0);
                return 1;
            case "FreezeAnimation":
                {
                    const string usage =
                        "Usage: self:FreezeAnimation(anim, variation, frame)";
                    if (!TryReadRequiredUInt32(state, 2, out var frozenAnimation) ||
                        frozenAnimation > 1857 ||
                        !TryReadRequiredUInt32(state, 3, out var frozenVariation) ||
                        !TryReadRequiredUInt32(state, 4, out var frozenFrame))
                    {
                        return luaL_error(state, usage);
                    }
                    value.ModelAnimationId = (ushort)frozenAnimation;
                    value.ModelAnimationVariation = unchecked((int)frozenVariation);
                    value.ModelAnimationFrozenFrame = unchecked((ushort)frozenFrame);
                    value.ModelAnimationSpeed = 0;
                    value.ModelAnimationTimeOffsetMilliseconds =
                        value.ModelAnimationFrozenFrame;
                    CancelCharacterRotationAnimation(value);
                    if (HasLoadedModel(value) &&
                        value.ModelAnimationKitId is null)
                        ApplyModelSceneActorAnimationState(runtime, value);
                    return 0;
                }
            case "PlayAnimKit":
                if (!TryReadRequiredInt32(state, 2, out var characterAnimKit))
                {
                    return luaL_error(
                        state,
                        "Usage: self:PlayAnimKit(animKit [, loop])");
                }
                if (HasLoadedModel(value))
                {
                    PlayCharacterModelAnimationKit(
                        runtime,
                        value,
                        characterAnimKit,
                        OptionalBoolean(state, 3, false));
                }
                return 0;
            case "StopAnimKit":
                StopCharacterModelAnimationKit(runtime, value, restoreBase: true);
                return 0;
            case "ApplySpellVisualKit":
                if (!TryReadRequiredInt32(
                        state,
                        2,
                        out var characterSpellVisualKit))
                {
                    return luaL_error(
                        state,
                        "Usage: self:ApplySpellVisualKit(" +
                        "spellVisualKitID [, oneShot])");
                }
                if (HasLoadedModel(value))
                {
                    ApplyCharacterModelSpellVisualKit(
                        runtime,
                        value,
                        unchecked((uint)characterSpellVisualKit),
                        OptionalBoolean(state, 3, false));
                }
                return 0;
            case "IsSlotAllowed":
                {
                    if (!TryReadRequiredInt32(state, 2, out var allowedInventorySlot))
                    {
                        return luaL_error(
                            state,
                            "Usage: local allowed = self:IsSlotAllowed(inventorySlots)");
                    }
                    lua_pushboolean(
                        state,
                        allowedInventorySlot is >= 1 and <= 105 &&
                        value.ModelAllowedInventorySlots.Contains(allowedInventorySlot)
                            ? 1
                            : 0);
                    return 1;
                }
            case "IsSlotVisible":
                {
                    if (!TryReadRequiredInt32(state, 2, out var visibleInventorySlot))
                    {
                        return luaL_error(
                            state,
                            "Usage: local visible = self:IsSlotVisible(inventorySlots)");
                    }
                    lua_pushboolean(
                        state,
                        visibleInventorySlot is >= 1 and <= 105 &&
                        value.ModelVisibleInventorySlots.Contains(visibleInventorySlot)
                            ? 1
                            : 0);
                    return 1;
                }
            case "IsGeoReady":
                lua_pushboolean(state, HasLoadedModel(value) ? 1 : 0);
                return 1;
            case "GetDisplayInfo":
                lua_pushinteger(state, value.ModelDisplayId);
                return 1;
            case "SetDisplayInfo":
                {
                    const string usage =
                        "Usage: self:SetDisplayInfo(displayID [, mountDisplayID])";
                    if (!TryReadRequiredInt32(state, 2, out var displayId))
                        return luaL_error(state, usage);
                    var mountDisplayId = 0;
                    if (HasRequiredValue(state, 3) &&
                        !TryReadRequiredInt32(state, 3, out mountDisplayId))
                    {
                        return luaL_error(state, usage);
                    }
                    ResetCharacterModelSourceState(value);
                    value.ModelDisplayId = displayId;
                    value.ModelMountDisplayId = mountDisplayId;
                    if (displayId > 0)
                    {
                        value.ModelCreatureDisplayId = (uint)displayId;
                        value.ModelPath = $"Creature Display ID {displayId}";
                        value.ModelResourceLoaded = true;
                        runtime.InvokeScript(value, "OnModelLoaded");
                    }
                    return 0;
                }
            case "SetCreature":
                {
                    const string usage =
                        "Usage: self:SetCreature(creatureID [, displayID])";
                    if (!TryReadRequiredInt32(state, 2, out var creatureId))
                        return luaL_error(state, usage);
                    var creatureDisplayId = 0;
                    if (HasRequiredValue(state, 3) &&
                        !TryReadRequiredInt32(state, 3, out creatureDisplayId))
                    {
                        return luaL_error(state, usage);
                    }
                    ResetCharacterModelSourceState(value);
                    value.ModelCreatureId = creatureId;
                    value.ModelDisplayId = creatureDisplayId;
                    if (creatureDisplayId > 0)
                    {
                        value.ModelCreatureDisplayId = (uint)creatureDisplayId;
                        value.ModelPath =
                            $"Creature Display ID {creatureDisplayId}";
                        value.ModelResourceLoaded = true;
                        runtime.InvokeScript(value, "OnModelLoaded");
                    }
                    return 0;
                }
            case "SetItem":
                {
                    const string usage =
                        "Usage: self:SetItem(" +
                        "itemID [, appearanceModID, itemVisualID])";
                    if (!TryReadRequiredInt32(state, 2, out var modelItemId))
                        return luaL_error(state, usage);
                    var appearanceModifierId = 0;
                    var itemVisualId = 0;
                    if ((HasRequiredValue(state, 3) &&
                         !TryReadRequiredInt32(
                             state,
                             3,
                             out appearanceModifierId)) ||
                        (HasRequiredValue(state, 4) &&
                         !TryReadRequiredInt32(state, 4, out itemVisualId)))
                    {
                        return luaL_error(state, usage);
                    }
                    ResetCharacterModelSourceState(value);
                    value.ModelItemId = modelItemId;
                    value.ModelItemAppearanceModifierId = appearanceModifierId;
                    value.ModelItemVisualId = itemVisualId;
                    return 0;
                }
            case "SetItemAppearance":
                {
                    const string usage =
                        "Usage: self:SetItemAppearance(" +
                        "itemAppearanceID [, itemVisualID, itemSubclass])";
                    if (!TryReadRequiredInt32(
                            state,
                            2,
                            out var modelItemAppearanceId))
                    {
                        return luaL_error(state, usage);
                    }
                    var appearanceVisualId = 0;
                    var itemSubclass = -1;
                    if ((HasRequiredValue(state, 3) &&
                         !TryReadRequiredInt32(
                             state,
                             3,
                             out appearanceVisualId)) ||
                        (HasRequiredValue(state, 4) &&
                         (!TryReadRequiredInt32(state, 4, out itemSubclass) ||
                          itemSubclass is < 0 or > 20)))
                    {
                        return luaL_error(state, usage);
                    }
                    ResetCharacterModelSourceState(value);
                    value.ModelItemAppearanceId = modelItemAppearanceId;
                    value.ModelItemVisualId = appearanceVisualId;
                    value.ModelItemSubclass = itemSubclass;
                    return 0;
                }
            case "SetBarberShopAlternateForm":
                value.ModelBarberShopAlternateForm = true;
                value.ModelMountDisplayId = 0;
                value.ModelDoBlend = true;
                ResetCharacterModelAnimationState(value);
                value.ModelCreatureDisplayId = value.ModelDisplayId > 0
                    ? (uint)value.ModelDisplayId
                    : null;
                value.ModelPath = value.ModelDisplayId > 0
                    ? $"Creature Display ID {value.ModelDisplayId}"
                    : null;
                if (value.ModelDisplayId > 0)
                {
                    value.ModelResourceLoaded = true;
                    runtime.InvokeScript(value, "OnModelLoaded");
                }
                return 0;
            case "GetSpellVisualKit":
                lua_pushinteger(state, value.ModelSpellVisualKitId ?? 0);
                return 1;
            case "IsLoaded":
                lua_pushboolean(state, HasLoadedModel(value) ? 1 : 0);
                return 1;
            case "AbortDrag":
                runtime.AbortFrameDragHandler?.Invoke(value);
                return 0;
            case "InterceptStartDrag":
                {
                    var dragDelegate = GetObject(runtime, 2);
                    if (dragDelegate is not { IsFrameWidget: true })
                    {
                        return luaL_error(
                            state,
                            "Usage: local success = self:InterceptStartDrag(delegate)");
                    }
                    lua_pushboolean(
                        state,
                        runtime.InterceptFrameDragHandler?.Invoke(value, dragDelegate) == true
                            ? 1
                            : 0);
                    return 1;
                }
            case "StartMoving":
                _ = OptionalBoolean(state, 2, false);
                if (!value.Movable)
                {
                    runtime.Log.Warn("ui", "Frame is not movable");
                    return 0;
                }
                if (runtime.Ui.MovingObjectId is not null)
                    return 0;
                value.UserPlaced = true;
                runtime.Ui.MovingObjectId = value.Id;
                runtime.Ui.MovingPoint = "CENTER";
                return 0;
            case "SetResizable":
                if (!TryReadRequiredBoolean(state, 2, out var resizable))
                    return luaL_error(state, "Usage: self:SetResizable(resizable)");
                value.Resizable = resizable;
                return 0;
            case "IsResizable":
                lua_pushboolean(state, value.Resizable ? 1 : 0);
                return 1;
            case "SetResizeBounds":
                {
                    const string usage =
                        "Usage: self:SetResizeBounds(minWidth, minHeight " +
                        "[, maxWidth, maxHeight])";
                    if (!TryReadRequiredFloat(state, 2, out var minWidth) ||
                        !TryReadRequiredFloat(state, 3, out var minHeight) ||
                        !TryReadOptionalFloat(state, 4, out var maxWidth) ||
                        !TryReadOptionalFloat(state, 5, out var maxHeight))
                    {
                        return luaL_error(state, usage);
                    }
                    value.ResizeMinimum = new Vector2((float)minWidth, (float)minHeight);
                    value.ResizeMaximum = new Vector2(
                        HasRequiredValue(state, 4) ? (float)maxWidth : 0,
                        HasRequiredValue(state, 5) ? (float)maxHeight : 0);
                    return 0;
                }
            case "GetResizeBounds":
                lua_pushnumber(state, value.ResizeMinimum.X);
                lua_pushnumber(state, value.ResizeMinimum.Y);
                lua_pushnumber(state, value.ResizeMaximum.X);
                lua_pushnumber(state, value.ResizeMaximum.Y);
                return 4;
            case "StartSizing":
                {
                    const string usage =
                        "Usage: self:StartSizing([resizePoint, alwaysStartFromMouse])";
                    if (!TryReadOptionalFramePoint(state, 2, out var resizePoint))
                        return luaL_error(state, usage);
                    _ = OptionalBoolean(state, 3, false);
                    if (!value.Resizable)
                    {
                        runtime.Log.Warn("ui", "Frame is not resizable");
                        return 0;
                    }
                    if (runtime.Ui.MovingObjectId is not null)
                        return 0;
                    value.UserPlaced = true;
                    runtime.Ui.MovingObjectId = value.Id;
                    runtime.Ui.MovingPoint = resizePoint;
                    return 0;
                }
            case "StopMovingOrSizing":
                if (runtime.Ui.MovingObjectId == value.Id)
                {
                    runtime.Ui.MovingObjectId = null;
                    runtime.Ui.MovingPoint = null;
                }
                return 0;
            case "EnableMouse":
                value.MouseEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "EnableMouseMotion":
                value.MouseMotionEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "SetMouseClickEnabled":
                value.MouseClickEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "IsMouseClickEnabled":
                lua_pushboolean(state, value.MouseClickEnabled ? 1 : 0);
                return 1;
            case "SetMouseMotionEnabled":
                value.MouseMotionEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "IsMouseMotionEnabled":
                lua_pushboolean(state, value.MouseMotionEnabled ? 1 : 0);
                return 1;
            case "IsMouseEnabled":
                lua_pushboolean(state, value.MouseEnabled ? 1 : 0);
                return 1;
            case "EnableMouseWheel":
                value.MouseWheelEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "IsMouseWheelEnabled":
                lua_pushboolean(state, value.MouseWheelEnabled ? 1 : 0);
                return 1;
            case "EnableKeyboard":
                value.KeyboardEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "IsKeyboardEnabled":
                lua_pushboolean(state, value.KeyboardEnabled ? 1 : 0);
                return 1;
            case "EnableGamePadButton":
                value.GamePadButtonEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "EnableGamePadStick":
                value.GamePadStickEnabled = OptionalBoolean(state, 2, false);
                return 0;
            case "IsGamePadButtonEnabled":
                lua_pushboolean(state, value.GamePadButtonEnabled ? 1 : 0);
                return 1;
            case "IsGamePadStickEnabled":
                lua_pushboolean(state, value.GamePadStickEnabled ? 1 : 0);
                return 1;
            case "GetOnUpdateMode":
                lua_pushnumber(state, (int)value.OnUpdateMode);
                return 1;
            case "SetOnUpdateMode":
                if (lua_isnumber(state, 2) == 0)
                    return luaL_error(state, "Usage: self:SetOnUpdateMode(onUpdateMode)");
                value.OnUpdateMode = (UiOnUpdateMode)(int)lua_tonumber(state, 2);
                return 0;
            case "GetPropagateKeyboardInput":
                lua_pushboolean(state, value.PropagateKeyboardInput ? 1 : 0);
                return 1;
            case "SetPropagateKeyboardInput":
                if (!TryReadRequiredBoolean(state, 2, out var propagateKeyboard))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetPropagateKeyboardInput(propagate)");
                }
                value.PropagateKeyboardInput = propagateKeyboard;
                return 0;
            case "GetPropagateMouseClicks":
                lua_pushboolean(state, value.PropagateMouseClicks ? 1 : 0);
                return 1;
            case "CanPropagateMouseClicks":
                lua_pushboolean(state, value.PropagateMouseClicks ? 1 : 0);
                return 1;
            case "SetPropagateMouseClicks":
                if (!TryReadRequiredBoolean(state, 2, out var propagateClicks))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetPropagateMouseClicks(propagate)");
                }
                value.PropagateMouseClicks = propagateClicks;
                return 0;
            case "GetPropagateMouseMotion":
                lua_pushboolean(state, value.PropagateMouseMotion ? 1 : 0);
                return 1;
            case "CanPropagateMouseMotion":
                lua_pushboolean(state, value.PropagateMouseMotion ? 1 : 0);
                return 1;
            case "SetPropagateMouseMotion":
                if (!TryReadRequiredBoolean(state, 2, out var propagateMotion))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetPropagateMouseMotion(propagate)");
                }
                value.PropagateMouseMotion = propagateMotion;
                return 0;
            case "SetPassThroughButtons":
                {
                    var buttons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (var index = 2; index <= lua_gettop(state); index++)
                    {
                        if (lua_type(state, index) != LUA_TSTRING)
                        {
                            return luaL_error(
                                state,
                                "Usage: self:SetPassThroughButtons(buttons)");
                        }
                        if (UiObject.NormalizeMouseButtonName(lua_tostring(state, index)) is
                            { } normalized)
                        {
                            buttons.Add(normalized);
                        }
                    }
                    value.PassThroughButtons.Clear();
                    value.PassThroughButtons.UnionWith(buttons);
                    return 0;
                }
            case "ClearScripts":
                runtime.ClearScripts(value);
                return 0;
            case "ShouldButtonPassThrough":
                if (lua_type(state, 2) != LUA_TSTRING)
                {
                    return luaL_error(
                        state,
                        "Usage: local shouldPassThrough = " +
                        "self:ShouldButtonPassThrough(button)");
                }
                lua_pushboolean(
                    state,
                    value.ShouldButtonPassThrough(lua_tostring(state, 2)) ? 1 : 0);
                return 1;
            case "SetToDefaults":
                ResetObjectToDefaults(runtime, value);
                return 0;
            case "GetWindow":
                runtime.PushWindow(value);
                return 1;
            case "SetWindow":
                if (!runtime.SetWindow(value, 2))
                    return luaL_error(state, "Usage: self:SetWindow([window])");
                return 0;
            case "ClearText":
                SetObjectText(runtime, value, string.Empty);
                runtime.Ui.InvalidateLayout();
                return 0;
            case "AddHistoryLine":
                {
                    if (!TryReadRequiredString(state, 2, out var historyLine))
                        return luaL_error(state, "Usage: self:AddHistoryLine(text)");
                    if (value.EditBoxHistoryLines > 0)
                    {
                        EnsureEditBoxHistoryCapacity(value);
                        value.EditBoxHistory[value.EditBoxHistoryWriteIndex] = historyLine;
                        value.EditBoxHistoryWriteIndex =
                            (value.EditBoxHistoryWriteIndex + 1) % value.EditBoxHistoryLines;
                    }
                    return 0;
                }
            case "ClearHighlightText":
                value.EditBoxHighlightStart = 0;
                value.EditBoxHighlightEnd = 0;
                return 0;
            case "ClearHistory":
                value.EditBoxHistoryWriteIndex = 0;
                for (var index = 0; index < value.EditBoxHistory.Count; index++)
                    value.EditBoxHistory[index] = null;
                return 0;
            case "GetAltArrowKeyMode":
                lua_pushboolean(state, value.EditBoxAltArrowKeyMode ? 1 : 0);
                return 1;
            case "GetBlinkSpeed":
                lua_pushnumber(state, value.EditBoxBlinkSpeed);
                return 1;
            case "GetDisplayText":
                {
                    var displayText = WowTextMarkup.PlainText(value.TextValue);
                    lua_pushstring(state, displayText);
                    return 1;
                }
            case "GetHighlightColor":
                lua_pushnumber(state, value.EditBoxHighlightColor.X);
                lua_pushnumber(state, value.EditBoxHighlightColor.Y);
                lua_pushnumber(state, value.EditBoxHighlightColor.Z);
                lua_pushnumber(state, value.EditBoxHighlightColor.W);
                return 4;
            case "GetHistoryLines":
                lua_pushnumber(state, value.EditBoxHistoryLines);
                return 1;
            case "GetMaxBytes":
                lua_pushnumber(state, value.EditBoxMaximumBytes);
                return 1;
            case "GetMaxLetters":
                lua_pushnumber(state, value.MaximumLetters);
                return 1;
            case "GetNumLetters":
                {
                    var countedText = value.EditBoxCountInvisibleLetters
                        ? value.TextValue
                        : WowTextMarkup.PlainText(value.TextValue);
                    lua_pushnumber(state, Utf8CharacterCount(countedText));
                    return 1;
                }
            case "GetTextInsets":
                lua_pushnumber(state, value.TextInsets.X);
                lua_pushnumber(state, value.TextInsets.Y);
                lua_pushnumber(state, value.TextInsets.Z);
                lua_pushnumber(state, value.TextInsets.W);
                return 4;
            case "GetVisibleTextByteLimit":
                lua_pushnumber(state, value.EditBoxVisibleTextByteLimit);
                return 1;
            case "HasText":
                lua_pushboolean(state, value.TextValue.Length > 0 ? 1 : 0);
                return 1;
            case "HighlightText":
                {
                    if (!TryReadOptionalInt32(state, 2, 0, out var highlightStart) ||
                        !TryReadOptionalInt32(state, 3, -1, out var highlightEnd))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:HighlightText([start, stop])");
                    }
                    var byteLength = Encoding.UTF8.GetByteCount(value.TextValue);
                    highlightStart = Math.Clamp(highlightStart, 0, byteLength);
                    highlightEnd = highlightEnd >= highlightStart
                        ? Math.Clamp(highlightEnd, 0, byteLength)
                        : byteLength;
                    value.EditBoxHighlightStart = Utf16PositionFromUtf8ByteOffset(
                        value.TextValue,
                        highlightStart);
                    value.EditBoxHighlightEnd = Utf16PositionFromUtf8ByteOffset(
                        value.TextValue,
                        highlightEnd);
                    return 0;
                }
            case "Insert":
                {
                    if (!TryReadRequiredString(state, 2, out var insertedText))
                        return luaL_error(state, "Usage: self:Insert(text)");
                    if (value.SecurityDisableSetText)
                    {
                        return luaL_error(
                            state,
                            "Call is illegal when disabled by security settings.");
                    }

                    var selectionStart = Math.Clamp(
                        Math.Min(value.EditBoxHighlightStart, value.EditBoxHighlightEnd),
                        0,
                        value.TextValue.Length);
                    var selectionEnd = Math.Clamp(
                        Math.Max(value.EditBoxHighlightStart, value.EditBoxHighlightEnd),
                        selectionStart,
                        value.TextValue.Length);
                    var cursor = selectionStart != selectionEnd
                        ? selectionStart
                        : Math.Clamp(value.CursorPosition, 0, value.TextValue.Length);
                    var baseText = selectionStart != selectionEnd
                        ? value.TextValue.Remove(selectionStart, selectionEnd - selectionStart)
                        : value.TextValue;
                    var nextText = EditBoxTextRules.ApplyInsertion(
                        value,
                        baseText,
                        cursor,
                        insertedText);
                    var changed = !value.TextValue.Equals(nextText, StringComparison.Ordinal);
                    SetObjectText(runtime, value, nextText);
                    value.CursorPosition = nextText.Equals(baseText, StringComparison.Ordinal)
                        ? Math.Min(cursor, nextText.Length)
                        : Math.Min(cursor + insertedText.Length, nextText.Length);
                    value.EditBoxHighlightStart = value.CursorPosition;
                    value.EditBoxHighlightEnd = value.CursorPosition;
                    runtime.Ui.InvalidateLayout();
                    if (changed)
                        runtime.QueueEditBoxTextChanged(value, true);
                    return 0;
                }
            case "IsAlphabeticOnly":
                lua_pushboolean(state, value.EditBoxAlphabeticOnly ? 1 : 0);
                return 1;
            case "IsAutoFocus":
                lua_pushboolean(state, value.AutoFocus ? 1 : 0);
                return 1;
            case "IsCountInvisibleLetters":
                lua_pushboolean(state, value.EditBoxCountInvisibleLetters ? 1 : 0);
                return 1;
            case "IsInIMECompositionMode":
                lua_pushboolean(state, value.EditBoxImeCompositionMode ? 1 : 0);
                return 1;
            case "IsMultiLine":
                lua_pushboolean(state, value.MultiLine ? 1 : 0);
                return 1;
            case "IsNumericFullRange":
                lua_pushboolean(state, value.EditBoxNumericFullRange ? 1 : 0);
                return 1;
            case "IsPassword":
                lua_pushboolean(state, value.EditBoxPassword ? 1 : 0);
                return 1;
            case "IsSecureText":
                lua_pushboolean(state, value.EditBoxSecureText ? 1 : 0);
                return 1;
            case "ResetInputMode":
                value.EditBoxAlphabeticOnly = false;
                value.EditBoxNumericFullRange = false;
                value.EditBoxPassword = false;
                value.Attributes["Numeric"] = false;
                return 0;
            case "SetAlphabeticOnly":
                {
                    var alphabeticOnly = OptionalBoolean(state, 2, false);
                    if (value.EditBoxAlphabeticOnly == alphabeticOnly)
                        return 0;
                    SetEditBoxInputMode(value, alphabeticOnly, false, false);
                    if (alphabeticOnly)
                        ReapplyEditBoxTextRules(runtime, value);
                    return 0;
                }
            case "SetAltArrowKeyMode":
                value.EditBoxAltArrowKeyMode = OptionalBoolean(state, 2, false);
                return 0;
            case "SetBlinkSpeed":
                if (!TryReadRequiredFloat(state, 2, out var blinkSpeed))
                    return luaL_error(state, "Usage: self:SetBlinkSpeed(cursorBlinkSpeedSec)");
                value.EditBoxBlinkSpeed = (float)blinkSpeed;
                return 0;
            case "SetCountInvisibleLetters":
                value.EditBoxCountInvisibleLetters = OptionalBoolean(state, 2, false);
                return 0;
            case "SetHighlightColor":
                if (!TryReadRequiredNormalizedColor(
                        state,
                        2,
                        out var editBoxHighlightColor))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetHighlightColor(color [, a])");
                }
                value.EditBoxHighlightColor = editBoxHighlightColor;
                return 0;
            case "SetHistoryLines":
                if (!TryReadRequiredInt32(state, 2, out var historyLines))
                    return luaL_error(state, "Usage: self:SetHistoryLines(numHistoryLines)");
                if (historyLines > 0)
                {
                    value.EditBoxHistoryLines = historyLines;
                    value.EditBoxHistoryWriteIndex =
                        Math.Min(value.EditBoxHistoryWriteIndex, historyLines - 1);
                    EnsureEditBoxHistoryCapacity(value);
                }
                return 0;
            case "SetMaxBytes":
                if (!TryReadRequiredInt32(state, 2, out var maximumBytes))
                    return luaL_error(state, "Usage: self:SetMaxBytes(maxBytes)");
                value.EditBoxMaximumBytes = Math.Max(maximumBytes, 0);
                return 0;
            case "SetNumericFullRange":
                {
                    var numericFullRange = OptionalBoolean(state, 2, false);
                    if (value.EditBoxNumericFullRange == numericFullRange)
                        return 0;
                    SetEditBoxInputMode(value, false, numericFullRange, false);
                    if (numericFullRange)
                        ReapplyEditBoxTextRules(runtime, value);
                    return 0;
                }
            case "SetPassword":
                {
                    var password = OptionalBoolean(state, 2, false);
                    if (value.EditBoxPassword == password)
                        return 0;
                    SetEditBoxInputMode(value, false, false, password);
                    if (password)
                        ReapplyEditBoxTextRules(runtime, value);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "SetSecureText":
                value.EditBoxSecureText = OptionalBoolean(state, 2, false);
                return 0;
            case "SetSecurityDisablePaste":
                value.EditBoxSecurityDisablePaste = true;
                return 0;
            case "SetVisibleTextByteLimit":
                if (!TryReadRequiredInt32(state, 2, out var visibleTextByteLimit))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetVisibleTextByteLimit(maxVisibleBytes)");
                }
                value.EditBoxVisibleTextByteLimit = Math.Max(visibleTextByteLimit, 0);
                var previousText = value.TextValue;
                var previousCursor = value.CursorPosition;
                var limitedText = EditBoxTextRules.EnforceLimits(value, previousText);
                if (!limitedText.Equals(previousText, StringComparison.Ordinal))
                {
                    SetObjectText(runtime, value, limitedText);
                    value.CursorPosition = Math.Min(previousCursor, limitedText.Length);
                    value.EditBoxHighlightStart = Math.Min(
                        value.EditBoxHighlightStart,
                        limitedText.Length);
                    value.EditBoxHighlightEnd = Math.Min(
                        value.EditBoxHighlightEnd,
                        limitedText.Length);
                    runtime.QueueEditBoxTextChanged(value, true);
                }
                runtime.Ui.InvalidateLayout();
                return 0;
            case "ToggleInputLanguage":
                return 0;
            case "SetCursorPosition":
                if (!TryReadRequiredInt32(state, 2, out var cursorBytePosition))
                    return luaL_error(state, "Usage: self:SetCursorPosition(cursorPosition)");
                value.CursorPosition = Utf16PositionFromUtf8ByteOffset(
                    value.TextValue,
                    cursorBytePosition);
                return 0;
            case "GetCursorPosition":
                lua_pushinteger(
                    state,
                    Encoding.UTF8.GetByteCount(
                        value.TextValue.AsSpan(0, Math.Clamp(
                            value.CursorPosition,
                            0,
                            value.TextValue.Length))));
                return 1;
            case "GetUTF8CursorPosition":
                lua_pushinteger(
                    state,
                    Utf8CharacterCount(
                        value.TextValue.AsSpan(0, Math.Clamp(
                            value.CursorPosition,
                            0,
                            value.TextValue.Length))));
                return 1;
            case "IsMouseOver":
                {
                    const string usage =
                        "Usage: local isMouseOver = self:IsMouseOver(" +
                        "[offsetTop, offsetBottom, offsetLeft, offsetRight])";
                    if (!TryReadOptionalFloat(state, 2, out var offsetTop) ||
                        !TryReadOptionalFloat(state, 3, out var offsetBottom) ||
                        !TryReadOptionalFloat(state, 4, out var offsetLeft) ||
                        !TryReadOptionalFloat(state, 5, out var offsetRight))
                    {
                        return luaL_error(state, usage);
                    }
                    lua_pushboolean(
                        state,
                        runtime.Ui.IsMouseOver(
                            value,
                            runtime.Ui.CursorPosition,
                            (float)offsetTop,
                            (float)offsetBottom,
                            (float)offsetLeft,
                            (float)offsetRight)
                            ? 1
                            : 0);
                    return 1;
                }
            case "IsMouseMotionFocus":
                lua_pushboolean(
                    state,
                    runtime.Ui.IsMouseMotionFocus(value) ? 1 : 0);
                return 1;
            case "RegisterForClicks":
                return RegisterMouseButtons(
                    state,
                    2,
                    value.ClickRegistrations,
                    "Usage: self:RegisterForClicks(buttons)");
            case "RegisterForMouse":
                return RegisterMouseButtons(
                    state,
                    2,
                    value.MouseRegistrations,
                    "Usage: self:RegisterForMouse(buttons)");
            case "RegisterForDrag":
                return RegisterDragButtons(state, value.DragRegistrations);
            case "RotateTextures":
                {
                    const string usage = "Usage: self:RotateTextures(radians [, x, y])";
                    if (!TryReadRequiredFloat(state, 2, out var radians))
                        return luaL_error(state, usage);
                    var x = 0.5;
                    var y = 0.5;
                    if (HasRequiredValue(state, 3) &&
                        !TryReadRequiredFloat(state, 3, out x))
                    {
                        return luaL_error(state, usage);
                    }
                    if (HasRequiredValue(state, 4) &&
                        !TryReadRequiredFloat(state, 4, out y))
                    {
                        return luaL_error(state, usage);
                    }

                    foreach (var childId in value.Children)
                    {
                        if (runtime.Ui.Find(childId) is not { } child ||
                            !MatchesObjectType(child, "Texture"))
                        {
                            continue;
                        }
                        var texture = EnsureTexture(child);
                        texture.Rotation = (float)radians;
                        texture.RotationPoint = new Vector2((float)x, (float)y);
                    }
                    return 0;
                }
            case "Click":
                if (lua_gettop(state) >= 2 &&
                    lua_isnil(state, 2) == 0 &&
                    lua_type(state, 2) != LUA_TSTRING)
                {
                    return luaL_error(state, "Usage: self:Click([button, isDown])");
                }
                if (value.Forbidden)
                {
                    runtime.Log.Warn(
                        "ui",
                        ":Click cannot be called on Forbidden frames.");
                    return 0;
                }
                runtime.InvokeButtonClick(
                    value,
                    OptionalString(state, 2) ?? "LeftButton",
                    OptionalBoolean(state, 3, false));
                return 0;
            case "Enable":
                if (value.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase))
                {
                    value.Enabled = true;
                    runtime.InvokeScript(value, "OnEnable");
                }
                else if (SetButtonVisualState(runtime, value, true, UiButtonState.Normal))
                {
                    runtime.InvokeScript(value, "OnEnable");
                }
                return 0;
            case "SetEnabled":
                {
                    var isSlider =
                        value.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase);
                    var isEditBox =
                        value.ObjectType.Equals("EditBox", StringComparison.OrdinalIgnoreCase) ||
                        value.ObjectType.Equals("EventEditBox", StringComparison.OrdinalIgnoreCase);
                    if (isSlider && lua_gettop(state) < 2)
                    {
                        return luaL_error(state, "Usage: self:SetEnabled(enabled)");
                    }
                    var enabled = OptionalBoolean(state, 2, false);
                    if (isSlider)
                    {
                        value.Enabled = enabled;
                        runtime.InvokeScript(value, enabled ? "OnEnable" : "OnDisable");
                    }
                    else if (isEditBox)
                    {
                        if (!enabled && runtime.Ui.FocusedObjectId == value.Id)
                            runtime.SetKeyboardFocus(null);
                        value.Enabled = enabled;
                        runtime.InvokeScript(value, enabled ? "OnEnable" : "OnDisable");
                    }
                    else if (SetButtonVisualState(
                                 runtime,
                                 value,
                                 enabled,
                                 UiButtonState.Normal))
                    {
                        runtime.InvokeScript(value, enabled ? "OnEnable" : "OnDisable");
                    }
                    return 0;
                }
            case "Disable":
                if (value.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase))
                {
                    value.Enabled = false;
                    runtime.InvokeScript(value, "OnDisable");
                }
                else if (SetButtonVisualState(runtime, value, false, UiButtonState.Normal))
                {
                    runtime.InvokeScript(value, "OnDisable");
                }
                return 0;
            case "IsEnabled":
                lua_pushboolean(state, value.Enabled ? 1 : 0);
                return 1;
            case "SetChecked":
                runtime.SetCheckButtonChecked(value, OptionalBoolean(state, 2, false));
                return 0;
            case "LockHighlight":
                runtime.Ui.SetHighlightLocked(value, true);
                RefreshButtonFont(runtime, value);
                return 0;
            case "UnlockHighlight":
                runtime.Ui.SetHighlightLocked(value, false);
                RefreshButtonFont(runtime, value);
                return 0;
            case "SetHighlightLocked":
                if (lua_gettop(state) < 2)
                    return luaL_error(
                        state,
                        "Usage: self:SetHighlightLocked(locked)");
                runtime.Ui.SetHighlightLocked(
                    value,
                    lua_toboolean(state, 2) != 0);
                RefreshButtonFont(runtime, value);
                return 0;
            case "IsHighlightLocked":
                lua_pushboolean(state, value.HighlightLocked ? 1 : 0);
                return 1;
            case "SetMotionScriptsWhileDisabled":
                if (lua_gettop(state) < 2 || lua_isnil(state, 2) != 0)
                    return luaL_error(
                        state,
                        "Usage: self:SetMotionScriptsWhileDisabled(motionScriptsWhileDisabled)");
                value.MotionScriptsWhileDisabled = lua_toboolean(state, 2) != 0;
                return 0;
            case "GetMotionScriptsWhileDisabled":
                lua_pushboolean(state, value.MotionScriptsWhileDisabled ? 1 : 0);
                return 1;
            case "GetChecked":
                lua_pushboolean(state, value.Checked ? 1 : 0);
                return 1;
            case "SetNormalFontObject":
            case "SetHighlightFontObject":
            case "SetDisabledFontObject":
                SetButtonFontObject(runtime, value, operation);
                return 0;
            case "GetNormalFontObject":
                PushButtonFontObject(runtime, value.NormalFontObjectId, value.NormalFontObjectName);
                return 1;
            case "GetHighlightFontObject":
                PushButtonFontObject(runtime, value.HighlightFontObjectId, value.HighlightFontObjectName);
                return 1;
            case "GetDisabledFontObject":
                PushButtonFontObject(runtime, value.DisabledFontObjectId, value.DisabledFontObjectName);
                return 1;
            case "GetButtonState":
                lua_pushstring(
                    state,
                    !value.Enabled
                        ? "DISABLED"
                        : value.ButtonState == UiButtonState.Pushed
                            ? "PUSHED"
                            : "NORMAL");
                return 1;
            case "SetButtonState":
                {
                    var buttonState = OptionalString(state, 2);
                    if (buttonState == "NORMAL")
                    {
                        SetButtonVisualState(runtime, value, true, UiButtonState.Normal);
                    }
                    else if (buttonState == "PUSHED")
                    {
                        SetButtonVisualState(runtime, value, true, UiButtonState.Pushed);
                    }
                    else if (buttonState == "DISABLED")
                    {
                        SetButtonVisualState(runtime, value, false, UiButtonState.Normal);
                    }
                    else
                        return luaL_error(state, "Usage: self:SetButtonState(buttonState [, lock])");
                    value.ButtonStateLocked = OptionalBoolean(state, 3, false);
                    return 0;
                }
            case "GetPushedTextOffset":
                lua_pushnumber(state, value.PushedTextOffset.X);
                lua_pushnumber(state, value.PushedTextOffset.Y);
                return 2;
            case "SetPushedTextOffset":
                if (lua_isnumber(state, 2) == 0 || lua_isnumber(state, 3) == 0)
                    return luaL_error(state, "Usage: self:SetPushedTextOffset(offsetX, offsetY)");
                if (value.ButtonState != UiButtonState.Pushed)
                {
                    value.PushedTextOffset = new Vector2(
                        (float)lua_tonumber(state, 2),
                        (float)lua_tonumber(state, 3));
                }
                return 0;
            case "SetTexture":
                if (value.Line is not null)
                {
                    SetTexture(runtime, value);
                    return 0;
                }
                SetTexture(runtime, value);
                lua_pushboolean(state, 1);
                return 1;
            case "SetAtlas":
                {
                    var atlasArgumentType = lua_type(state, 2);
                    if (atlasArgumentType == LUA_TNIL)
                    {
                        ClearTextureAsset(EnsureTexture(value));
                        return 0;
                    }
                    if (atlasArgumentType != LUA_TSTRING)
                        return 0;

                    var atlasName = lua_tostring(state, 2)!;
                    var atlasTexture = EnsureTexture(value);
                    var applied = runtime.ApplyAtlas(
                        value,
                        atlasName,
                        OptionalBoolean(state, 3, false),
                        OptionalBoolean(state, 5, false),
                        ReadTextureFilterMode(state, 4),
                        ReadTextureWrapMode(state, 6, atlasTexture.WrapHorizontal),
                        ReadTextureWrapMode(state, 7, atlasTexture.WrapVertical));
                    if (!applied)
                    {
                        if (atlasName.Length > 0)
                            runtime.Log.Warn("lua", $"Invalid atlasName: {atlasName}");
                        return 0;
                    }

                    lua_pushboolean(state, 1);
                    return 1;
                }
            case "GetAtlas":
                PushOptionalString(state, EnsureTexture(value).AtlasName);
                return 1;
            case "SetNormalAtlas":
                SetButtonAtlas(runtime, value, ButtonTextureKind.Normal);
                return 0;
            case "SetPushedAtlas":
                SetButtonAtlas(runtime, value, ButtonTextureKind.Pushed);
                return 0;
            case "SetDisabledAtlas":
                SetButtonAtlas(runtime, value, ButtonTextureKind.Disabled);
                return 0;
            case "SetHighlightAtlas":
                SetButtonAtlas(runtime, value, ButtonTextureKind.Highlight);
                return 0;
            case "SetFontObject":
                {
                    if (IsSimpleHtml(value))
                    {
                        const string usage =
                            "Usage: self:SetFontObject(textType, font)";
                        if (!TryReadSimpleHtmlTextType(state, 2, out var textType))
                            return luaL_error(state, usage);
                        var htmlSource = GetObject(runtime, 3);
                        if (lua_isnil(state, 3) == 0 &&
                            htmlSource is not
                            {
                                Font: not null,
                                ObjectType: "Font"
                            })
                        {
                            return luaL_error(state, usage);
                        }
                        AssignHtmlFontObject(runtime, value, textType, htmlSource);
                        return 0;
                    }
                    var source = GetObject(runtime, 2);
                    var isFontString =
                        value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase);
                    if (source is { Font: not null } &&
                        source.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase) &&
                        !WouldCreateFontObjectLoop(runtime, value, source))
                    {
                        AssignFontObject(runtime, value, source);
                    }
                    else if (isFontString && lua_isnil(state, 2) != 0)
                    {
                        AssignFontObject(runtime, value, null);
                    }
                    return 0;
                }
            case "CopyFontObject":
                {
                    var source = GetObject(runtime, 2);
                    if (source is
                        {
                            Font: not null
                        } &&
                        source.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase) &&
                        source.Id != value.Id)
                    {
                        CopyFontObjectState(runtime, value, source);
                        runtime.Ui.InvalidateLayout();
                    }
                    return 0;
                }
            case "GetFontObject":
                if (IsSimpleHtml(value))
                {
                    if (!TryReadSimpleHtmlTextType(state, 2, out var textType))
                    {
                        return luaL_error(
                            state,
                            "Usage: local font = self:GetFontObject(textType)");
                    }
                    runtime.PushObject(
                        value.HtmlFontObjectIds.TryGetValue(
                            textType,
                            out var htmlFontObjectId)
                            ? runtime.Ui.Find(htmlFontObjectId)
                            : null);
                    return 1;
                }
                runtime.PushObject(
                    value.FontObjectId is { } fontObjectId
                        ? runtime.Ui.Find(fontObjectId)
                        : null);
                return 1;
            case "GetFontObjectForAlphabet":
                {
                    const string usage =
                        "Usage: local font = self:GetFontObjectForAlphabet(alphabet)";
                    if (!TryReadRequiredString(state, 2, out var alphabet) ||
                        alphabet is null ||
                        !alphabet.Equals("Roman", StringComparison.OrdinalIgnoreCase) &&
                        !alphabet.Equals("Korean", StringComparison.OrdinalIgnoreCase) &&
                        !alphabet.Equals("SimplifiedChinese", StringComparison.OrdinalIgnoreCase) &&
                        !alphabet.Equals("TraditionalChinese", StringComparison.OrdinalIgnoreCase) &&
                        !alphabet.Equals("Russian", StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, usage);
                    }

                    TryParseFontAlphabet(alphabet, out var fontAlphabet);
                    runtime.PushObject(ResolveFontObjectForAlphabet(runtime, value, fontAlphabet));
                    return 1;
                }
            case "SetNonSpaceWrap":
                {
                    if (lua_gettop(state) < 2 || lua_isnil(state, 2) != 0)
                        return luaL_error(state, "Usage: self:SetNonSpaceWrap(wrap)");
                    var font = EnsureFont(value);
                    font.NonSpaceWrap = lua_toboolean(state, 2) != 0;
                    MarkFontOverride(runtime, value, font, UiFontOverrides.NonSpaceWrap);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "SetWordWrap":
                {
                    if (lua_gettop(state) < 2 || lua_isnil(state, 2) != 0)
                        return luaL_error(state, "Usage: self:SetWordWrap(wrap)");
                    var font = EnsureFont(value);
                    font.WordWrap = lua_toboolean(state, 2) != 0;
                    MarkFontOverride(runtime, value, font, UiFontOverrides.WordWrap);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "CanNonSpaceWrap":
                lua_pushboolean(state, EnsureFont(value).NonSpaceWrap ? 1 : 0);
                return 1;
            case "CanWordWrap":
                lua_pushboolean(state, EnsureFont(value).WordWrap ? 1 : 0);
                return 1;
            case "AddMaskTexture":
                {
                    var mask = GetObject(runtime, 2);
                    if (mask is null ||
                        !mask.ObjectType.Equals("MaskTexture", StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, "AddMaskTexture: mask must be a MaskTexture");
                    }
                    if (value.MaskTextureIds.Count >= 3)
                    {
                        return luaL_error(
                            state,
                            "Texture already has the maximum number of mask textures (3)");
                    }
                    if (!value.MaskTextureIds.Contains(mask.Id))
                        value.MaskTextureIds.Add(mask.Id);
                    return 0;
                }
            case "GetMaskTexture":
                {
                    const string usage = "Usage: local mask = self:GetMaskTexture(index)";
                    if (lua_gettop(state) < 2 || lua_isnumber(state, 2) == 0)
                        return luaL_error(state, usage);
                    var requestedIndex = lua_tonumber(state, 2);
                    if (requestedIndex < 1 ||
                        requestedIndex > uint.MaxValue ||
                        Math.Truncate(requestedIndex) != requestedIndex)
                    {
                        return luaL_error(state, usage);
                    }
                    var index = (long)requestedIndex - 1;
                    runtime.PushObject(
                        index < value.MaskTextureIds.Count
                            ? runtime.Ui.Find(value.MaskTextureIds[(int)index])
                            : null);
                    return 1;
                }
            case "GetNumMaskTextures":
                lua_pushinteger(state, value.MaskTextureIds.Count);
                return 1;
            case "RemoveMaskTexture":
                {
                    var mask = GetObject(runtime, 2);
                    if (mask is null ||
                        !mask.ObjectType.Equals("MaskTexture", StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, "RemoveMaskTexture: mask must be a MaskTexture");
                    }
                    value.MaskTextureIds.Remove(mask.Id);
                    return 0;
                }
            case "SetMask":
                {
                    const string usage = "Usage: self:SetMask(file)";
                    if (lua_gettop(state) < 2 || lua_isstring(state, 2) == 0)
                        return luaL_error(state, usage);
                    var mask = lua_tostring(state, 2);
                    EnsureTexture(value).LegacyMaskAsset =
                        string.IsNullOrEmpty(mask) ? null : mask;
                    return 0;
                }
            case "SetNormalTexture":
                SetButtonTexture(runtime, value, ButtonTextureKind.Normal);
                return 0;
            case "SetPushedTexture":
                SetButtonTexture(runtime, value, ButtonTextureKind.Pushed);
                return 0;
            case "SetDisabledTexture":
                SetButtonTexture(runtime, value, ButtonTextureKind.Disabled);
                return 0;
            case "SetCheckedTexture":
                SetButtonTexture(runtime, value, ButtonTextureKind.Checked);
                return 0;
            case "SetDisabledCheckedTexture":
                SetButtonTexture(runtime, value, ButtonTextureKind.DisabledChecked);
                return 0;
            case "ClearNormalTexture":
                ClearButtonTexture(runtime, value, ButtonTextureKind.Normal);
                return 0;
            case "ClearPushedTexture":
                ClearButtonTexture(runtime, value, ButtonTextureKind.Pushed);
                return 0;
            case "ClearDisabledTexture":
                ClearButtonTexture(runtime, value, ButtonTextureKind.Disabled);
                return 0;
            case "ClearHighlightTexture":
                ClearButtonTexture(runtime, value, ButtonTextureKind.Highlight);
                return 0;
            case "GetNormalTexture":
                runtime.PushObject(value.NormalTextureId is { } normalId ? runtime.Ui.Find(normalId) : null);
                return 1;
            case "GetPushedTexture":
                runtime.PushObject(value.PushedTextureId is { } pushedId ? runtime.Ui.Find(pushedId) : null);
                return 1;
            case "GetDisabledTexture":
                runtime.PushObject(value.DisabledTextureId is { } disabledId ? runtime.Ui.Find(disabledId) : null);
                return 1;
            case "GetCheckedTexture":
                runtime.PushObject(value.CheckedTextureId is { } checkedId ? runtime.Ui.Find(checkedId) : null);
                return 1;
            case "GetDisabledCheckedTexture":
                runtime.PushObject(
                    value.DisabledCheckedTextureId is { } disabledCheckedId
                        ? runtime.Ui.Find(disabledCheckedId)
                        : null);
                return 1;
            case "SetColorTexture":
                {
                    if (!TryReadRequiredNormalizedColor(state, 2, out var color))
                        return luaL_error(
                            state,
                            "Usage: self:SetColorTexture(color [, a])");

                    var colorTexture = EnsureTexture(value);
                    colorTexture.IsColor = true;
                    colorTexture.AtlasName = null;
                    colorTexture.AtlasWidth = null;
                    colorTexture.AtlasHeight = null;
                    colorTexture.SliceData = null;
                    colorTexture.FileDataId = null;
                    colorTexture.Asset = null;
                    colorTexture.Gradient = null;
                    colorTexture.ClearAtlasRegion();
                    colorTexture.Color = color;
                    return 0;
                }
            case "SetVertexColor":
                {
                    if (!TryReadRequiredNormalizedColor(state, 2, out var color))
                        return luaL_error(state, "Usage: self:SetVertexColor(color [, a])");
                    if (value.Font is not null)
                        value.VertexColor = color;
                    else if (value.Line is not null)
                        value.Line.Texture.VertexColor = color;
                    else if (value.Texture is not null)
                        value.Texture.VertexColor = color;
                    else
                        value.VertexColor = color;
                    return 0;
                }
            case "SetVertexColorFromBoolean":
                {
                    const string usage =
                        "Usage: self:SetVertexColorFromBoolean(value, colorIfTrue, colorIfFalse)";
                    if (!TryReadRequiredBoolean(state, 2, out var condition) ||
                        !TryReadRequiredColorTable(state, 3, out var colorIfTrue) ||
                        !TryReadRequiredColorTable(state, 4, out var colorIfFalse))
                    {
                        return luaL_error(state, usage);
                    }

                    var color = condition ? colorIfTrue : colorIfFalse;
                    if (value.Font is not null)
                        value.VertexColor = color;
                    else if (value.Line is not null)
                        value.Line.Texture.VertexColor = color;
                    else if (value.Texture is not null)
                        value.Texture.VertexColor = color;
                    else
                        value.VertexColor = color;
                    return 0;
                }
            case "GetVertexColor":
                {
                    var vertexColor = value.Font is not null
                        ? value.VertexColor
                        : value.Line?.Texture.VertexColor ??
                          value.Texture?.VertexColor ??
                          value.VertexColor;
                    lua_pushnumber(state, vertexColor.X);
                    lua_pushnumber(state, vertexColor.Y);
                    lua_pushnumber(state, vertexColor.Z);
                    lua_pushnumber(state, vertexColor.W);
                    return 4;
                }
            case "SetBlendMode":
                {
                    var blendMode = OptionalString(state, 2)?.ToUpperInvariant();
                    if (blendMode is not ("DISABLE" or "BLEND" or "ALPHAKEY" or "ADD" or "MOD"))
                        return luaL_error(state, "Usage: self:SetBlendMode(blendMode)");
                    EnsureTexture(value).BlendMode = blendMode;
                    return 0;
                }
            case "GetBlendMode":
                lua_pushstring(state, EnsureTexture(value).BlendMode);
                return 1;
            case "SetDesaturated":
                EnsureTexture(value).Desaturation = OptionalBoolean(state, 2, false) ? 1 : 0;
                return 0;
            case "IsDesaturated":
                lua_pushboolean(state, EnsureTexture(value).Desaturation > 0 ? 1 : 0);
                return 1;
            case "SetDesaturation":
                if (value.ModelScene is not null)
                {
                    if (!TryReadRequiredFloat(state, 2, out var sceneDesaturation))
                        return luaL_error(state, "Usage: self:SetDesaturation(strength)");
                    var strength = Math.Clamp((float)sceneDesaturation, 0, 1);
                    foreach (var childId in value.Children)
                    {
                        if (runtime.Ui.Find(childId) is
                            {
                                ObjectType: "ModelSceneActor"
                            } actor)
                        {
                            actor.ModelDesaturation = strength;
                            actor.ModelRenderEffectKind = strength > 0
                                ? UiModelRenderEffectKind.Desaturation
                                : UiModelRenderEffectKind.None;
                            actor.ModelGradientMaskEnabled = false;
                            actor.ModelShadowEffectStrength = 0;
                            actor.ModelShadowEffectState = null;
                            actor.ModelDissolveEffectState = null;
                            actor.ModelEdgeGlowEffectState = null;
                        }
                    }
                }
                else if (value.ObjectType.Equals(
                             "ModelSceneActor",
                             StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryReadRequiredFloat(state, 2, out var modelDesaturation))
                        return luaL_error(state, "Usage: self:SetDesaturation(strength)");
                    value.ModelDesaturation = Math.Clamp((float)modelDesaturation, 0, 1);
                    value.ModelRenderEffectKind = value.ModelDesaturation > 0
                        ? UiModelRenderEffectKind.Desaturation
                        : UiModelRenderEffectKind.None;
                    value.ModelGradientMaskEnabled = false;
                    value.ModelShadowEffectStrength = 0;
                    value.ModelShadowEffectState = null;
                    value.ModelDissolveEffectState = null;
                    value.ModelEdgeGlowEffectState = null;
                }
                else if (value.ObjectType.EndsWith(
                             "Model",
                             StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryReadRequiredFloat(state, 2, out var modelDesaturation))
                        return luaL_error(state, "Usage: self:SetDesaturation(strength)");
                    if (HasLoadedModel(value))
                    {
                        value.ModelDesaturation = Math.Clamp(
                            (float)modelDesaturation,
                            0,
                            1);
                        value.ModelRenderEffectKind = value.ModelDesaturation > 0
                            ? UiModelRenderEffectKind.Desaturation
                            : UiModelRenderEffectKind.None;
                        value.ModelGradientMaskEnabled = false;
                        value.ModelShadowEffectStrength = 0;
                        value.ModelShadowEffectState = null;
                        value.ModelDissolveEffectState = null;
                        value.ModelEdgeGlowEffectState = null;
                    }
                }
                else
                {
                    if (!TryReadRequiredFloat(state, 2, out var textureDesaturation))
                        return luaL_error(
                            state,
                            "Usage: self:SetDesaturation(desaturation)");
                    EnsureTexture(value).Desaturation =
                        Math.Clamp((float)textureDesaturation, 0, 1);
                }
                return 0;
            case "GetDesaturation":
                var isSimpleModel =
                    value.ObjectType.EndsWith("Model", StringComparison.OrdinalIgnoreCase) &&
                    !value.ObjectType.Equals(
                        "ModelSceneActor",
                        StringComparison.OrdinalIgnoreCase);
                lua_pushnumber(
                    state,
                    value.ModelScene is not null ||
                    value.ObjectType.Equals("ModelSceneActor", StringComparison.OrdinalIgnoreCase)
                        ? value.ModelDesaturation
                        : isSimpleModel
                            ? HasLoadedModel(value)
                                ? value.ModelRenderEffectKind ==
                                  UiModelRenderEffectKind.Desaturation
                                    ? value.ModelDesaturation
                                    : 0
                                : 0
                        : EnsureTexture(value).Desaturation);
                return 1;
            case "SetRotation":
                if (value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryReadRequiredFloat(state, 2, out var fontRotation))
                        return luaL_error(state, "Usage: self:SetRotation(radians)");
                    value.FontRotation = (float)fontRotation;
                    runtime.Ui.InvalidateLayout();
                }
                else if (value.ObjectType.EndsWith("Model", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryReadRequiredFloat(
                            state,
                            2,
                            out var characterRotation))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetRotation(radians [, animate])");
                    }
                    var requestedRotation = (float)characterRotation;
                    var animateRotation = OptionalBoolean(state, 3, true);
                    StartCharacterRotationAnimation(
                        runtime,
                        value,
                        value.ModelYaw,
                        requestedRotation,
                        animateRotation);
                    value.ModelYaw = requestedRotation;
                }
                else if (value.Cooldown is { } rotatingCooldown)
                {
                    if (!TryReadRequiredFloat(state, 2, out var cooldownRotation))
                        return luaL_error(state, "Usage: self:SetRotation(rotationRadians)");
                    rotatingCooldown.Rotation = (float)cooldownRotation;
                }
                else
                {
                    const string usage =
                        "Usage: self:SetRotation(radians [, normalizedRotationPoint])";
                    if (!TryReadRequiredFloat(state, 2, out var textureRotation))
                        return luaL_error(state, usage);

                    var rotationPoint = new Vector2(0.5f, 0.5f);
                    if (HasRequiredValue(state, 3) &&
                        !TryReadRequiredVector2Table(
                            state,
                            3,
                            out rotationPoint))
                    {
                        return luaL_error(state, usage);
                    }

                    var rotatingTexture = EnsureTexture(value);
                    rotatingTexture.Rotation = (float)textureRotation;
                    rotatingTexture.RotationPoint = rotationPoint;
                }
                return 0;
            case "GetRotation":
                if (value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase))
                {
                    lua_pushnumber(state, value.FontRotation);
                    return 1;
                }
                if (value.Texture is { } rotatedTexture)
                {
                    lua_pushnumber(state, rotatedTexture.Rotation);
                    PushVector2Table(state, rotatedTexture.RotationPoint);
                    return 2;
                }
                lua_pushnumber(state, value.Cooldown?.Rotation ?? 0);
                return 1;
            case "SetTexCoord":
                SetTexCoord(runtime, value);
                return 0;
            case "ResetTexCoord":
                if (value.Line is null)
                    EnsureTexture(value).ResetTexCoord();
                return 0;
            case "GetTexCoord":
                {
                    foreach (var coordinate in EnsureTexture(value).LocalUv)
                    {
                        lua_pushnumber(state, coordinate.X);
                        lua_pushnumber(state, coordinate.Y);
                    }
                    return 8;
                }
            case "IsBlockingLoadRequested":
                lua_pushboolean(
                    state,
                    EnsureTexture(value).BlockingLoadRequested ? 1 : 0);
                return 1;
            case "SetBlockingLoadRequested":
                EnsureTexture(value).BlockingLoadRequested =
                    OptionalBoolean(state, 2, false);
                return 0;
            case "GetTexelSnappingBias":
                lua_pushnumber(state, EnsureTexture(value).TexelSnappingBias);
                return 1;
            case "IsSnappingToPixelGrid":
                lua_pushboolean(state, EnsureTexture(value).SnapToPixelGrid ? 1 : 0);
                return 1;
            case "SetTexelSnappingBias":
                if (!TryReadRequiredFloat(state, 2, out var texelSnappingBias))
                    return luaL_error(
                        state,
                        "Usage: self:SetTexelSnappingBias(bias)");
                EnsureTexture(value).TexelSnappingBias =
                    Math.Clamp((float)texelSnappingBias, 0, 1);
                return 0;
            case "SetSnapToPixelGrid":
                EnsureTexture(value).SnapToPixelGrid = OptionalBoolean(state, 2, false);
                return 0;
            case "GetHorizTile":
                lua_pushboolean(state, EnsureTexture(value).HorizontallyTiled ? 1 : 0);
                return 1;
            case "SetHorizTile":
                EnsureTexture(value).HorizontallyTiled = OptionalBoolean(state, 2, false);
                return 0;
            case "GetVertTile":
                lua_pushboolean(state, EnsureTexture(value).VerticallyTiled ? 1 : 0);
                return 1;
            case "SetVertTile":
                EnsureTexture(value).VerticallyTiled = OptionalBoolean(state, 2, false);
                return 0;
            case "ClearVertexOffsets":
                Array.Clear(EnsureTexture(value).VertexOffsets);
                return 0;
            case "GetVertexOffset":
                {
                    const string usage =
                        "Usage: local offsetX, offsetY = self:GetVertexOffset(vertexIndex)";
                    if (!TryReadRequiredOneBasedIndex(state, 2, out var index))
                        return luaL_error(state, usage);

                    var offset = index < 4
                        ? EnsureTexture(value).VertexOffsets[index]
                        : Vector2.Zero;
                    lua_pushnumber(state, offset.X);
                    lua_pushnumber(state, offset.Y);
                    return 2;
                }
            case "SetVertexOffset":
                {
                    const string usage =
                        "Usage: self:SetVertexOffset(vertexIndex, offsetX, offsetY)";
                    if (!TryReadRequiredOneBasedIndex(state, 2, out var index) ||
                        !TryReadRequiredFloat(state, 3, out var offsetX) ||
                        !TryReadRequiredFloat(state, 4, out var offsetY))
                    {
                        return luaL_error(state, usage);
                    }

                    if (index < 4)
                    {
                        EnsureTexture(value).VertexOffsets[index] = new Vector2(
                            (float)offsetX,
                            (float)offsetY);
                    }
                    return 0;
                }
            case "SetSpriteSheetCell":
                return SetSpriteSheetCell(state, EnsureTexture(value));
            case "SetGradient":
                return SetGradient(state, EnsureTexture(value));
            case "ClearTextureSlice":
                EnsureTexture(value).SliceData = null;
                return 0;
            case "SetTextureSliceMargins":
                {
                    const string usage =
                        "Usage: self:SetTextureSliceMargins(left, top, right, bottom)";
                    if (lua_gettop(state) < 5 ||
                        lua_isnumber(state, 2) == 0 ||
                        lua_isnumber(state, 3) == 0 ||
                        lua_isnumber(state, 4) == 0 ||
                        lua_isnumber(state, 5) == 0)
                    {
                        return luaL_error(state, usage);
                    }
                    var left = lua_tonumber(state, 2);
                    var top = lua_tonumber(state, 3);
                    var right = lua_tonumber(state, 4);
                    var bottom = lua_tonumber(state, 5);
                    if (!IsUInt32(left) ||
                        !IsUInt32(top) ||
                        !IsUInt32(right) ||
                        !IsUInt32(bottom))
                    {
                        return luaL_error(state, usage);
                    }
                    var texture = EnsureTexture(value);
                    var current = texture.SliceData;
                    texture.SliceData = new UiTextureSliceData(
                        (float)left,
                        (float)top,
                        (float)right,
                        (float)bottom,
                        current?.Mode ?? UiTextureSliceMode.Stretched);
                    return 0;
                }
            case "GetTextureSliceMargins":
                if (EnsureTexture(value).SliceData is not { } margins)
                    return 0;
                lua_pushnumber(state, margins.MarginLeft);
                lua_pushnumber(state, margins.MarginTop);
                lua_pushnumber(state, margins.MarginRight);
                lua_pushnumber(state, margins.MarginBottom);
                return 4;
            case "SetTextureSliceMode":
                {
                    const string usage = "Usage: self:SetTextureSliceMode(sliceMode)";
                    if (lua_gettop(state) < 2 || lua_isnumber(state, 2) == 0)
                        return luaL_error(state, usage);
                    var numericMode = lua_tonumber(state, 2);
                    var mode = (int)numericMode;
                    if (numericMode < 0 || numericMode > int.MaxValue || mode is < 0 or > 1)
                        return luaL_error(state, usage);
                    var texture = EnsureTexture(value);
                    var current = texture.SliceData ?? new UiTextureSliceData(0, 0, 0, 0, UiTextureSliceMode.Stretched);
                    texture.SliceData = current with
                    {
                        Mode = mode == 1 ? UiTextureSliceMode.Tiled : UiTextureSliceMode.Stretched
                    };
                    return 0;
                }
            case "GetTextureSliceMode":
                if (EnsureTexture(value).SliceData is not { } sliced)
                    return 0;
                lua_pushinteger(state, (int)sliced.Mode);
                return 1;
            case "GetTexture":
                {
                    var texture = EnsureTexture(value);
                    if (texture.FileDataId is { } fileDataId)
                        lua_pushnumber(state, fileDataId);
                    else if (texture.Asset is { } asset)
                        lua_pushstring(state, asset);
                    else
                        lua_pushnil(state);
                    return 1;
                }
            case "GetTextureFileID":
                {
                    var texture = EnsureTexture(value);
                    lua_pushnumber(state, texture.FileDataId ?? 0);
                    return 1;
                }
            case "GetTextureFilePath":
                {
                    var texture = EnsureTexture(value);
                    if (!string.IsNullOrEmpty(texture.Asset))
                        lua_pushstring(state, texture.Asset);
                    else if (texture.FileDataId is { } fileDataId)
                        lua_pushnumber(state, fileDataId);
                    else
                        lua_pushnil(state);
                    return 1;
                }
            case "SetTitle":
                {
                    var title = OptionalString(state, 2) ?? string.Empty;
                    value.TextValue = title;
                    if (value.Attributes.TryGetValue("TemplateTitleTextId", out var titleIdValue) &&
                        titleIdValue is int titleId &&
                        runtime.Ui.Find(titleId) is { Font: { } font })
                    {
                        font.Text = title;
                    }
                    return 0;
                }
            case "SetText":
                {
                    if (IsEditBox(value) && value.SecurityDisableSetText)
                    {
                        return luaL_error(
                            state,
                            "Call is illegal when disabled by security settings.");
                    }
                    if (IsSimpleHtml(value))
                    {
                        if (!TryReadRequiredString(state, 2, out var htmlText))
                        {
                            return luaL_error(
                                state,
                                "Usage: self:SetText(text [, ignoreMarkup])");
                        }
                        value.HtmlIgnoreMarkup = OptionalBoolean(state, 3, false);
                        SetObjectText(runtime, value, htmlText);
                        RebuildSimpleHtmlContent(runtime, value, htmlText);
                        runtime.Ui.InvalidateLayout();
                        return 0;
                    }
                    if (lua_gettop(state) >= 2 &&
                        lua_isnil(state, 2) == 0 &&
                        lua_isstring(state, 2) == 0)
                    {
                        return luaL_error(state, "Usage: self:SetText(text)");
                    }
                    var text = OptionalString(state, 2) ?? string.Empty;
                    text = ProcessStoredFontStringText(value, text);
                    if (value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) &&
                        !EnsureFont(value).IsConfigured)
                    {
                        runtime.Log.Warn(
                            "lua",
                            $"FontString:SetText(): Font not set " +
                            $"[{runtime.GetDebugName(value, preferParentKey: true)}; " +
                            $"{value.SourceLocation}]");
                        return 0;
                    }
                    if (value.Tooltip is not null)
                    {
                        const string usage =
                            "Usage: self:SetText(text [, color, alpha, wrap])";
                        if (lua_gettop(state) < 2 || lua_isstring(state, 2) == 0)
                            return luaL_error(state, usage);
                        var color = new Vector4(1, 209 / 255f, 0, 1);
                        if (lua_gettop(state) >= 3 && lua_isnil(state, 3) == 0)
                        {
                            if (lua_isnumber(state, 3) == 0 ||
                                lua_isnumber(state, 4) == 0 ||
                                lua_isnumber(state, 5) == 0)
                            {
                                return luaL_error(state, usage);
                            }
                            color.X = QuantizeNormalizedByte(lua_tonumber(state, 3));
                            color.Y = QuantizeNormalizedByte(lua_tonumber(state, 4));
                            color.Z = QuantizeNormalizedByte(lua_tonumber(state, 5));
                        }
                        if (lua_gettop(state) >= 6 && lua_isnil(state, 6) == 0)
                        {
                            if (lua_isnumber(state, 6) == 0)
                                return luaL_error(state, usage);
                            color.W = QuantizeNormalizedByte(lua_tonumber(state, 6));
                        }
                        ClearTooltip(runtime, value);
                        AddTooltipLine(
                            runtime,
                            value,
                            text,
                            null,
                            color,
                            null,
                            OptionalBoolean(state, 7, false));
                        return 0;
                    }
                    var nextText = IsEditBox(value)
                            ? EditBoxTextRules.ApplyReplacement(value, text)
                            : value.MaximumLetters > 0 && text.Length > value.MaximumLetters
                                ? text[..value.MaximumLetters]
                                : text;
                    var changed = !value.TextValue.Equals(nextText, StringComparison.Ordinal);
                    SetObjectText(runtime, value, nextText);
                    runtime.Ui.InvalidateLayout();
                    if (changed && IsEditBox(value))
                    {
                        runtime.QueueEditBoxTextChanged(value, false);
                        runtime.InvokeScript(value, "OnTextSet");
                    }
                    return 0;
                }
            case "GetContentHeight":
                UpdateSimpleHtmlContentHeight(value);
                lua_pushnumber(state, value.HtmlContentHeight);
                return 1;
            case "GetHyperlinkFormat":
                lua_pushstring(state, value.HtmlHyperlinkFormat);
                return 1;
            case "SetHyperlinkFormat":
                if (!TryReadRequiredString(state, 2, out var hyperlinkFormat))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetHyperlinkFormat(format)");
                }
                value.HtmlHyperlinkFormat = hyperlinkFormat;
                return 0;
            case "GetTextData":
                {
                    lua_newtable(state);
                    var outputIndex = 1;
                    foreach (var node in value.HtmlContentNodes)
                    {
                        if (node.TextType is null ||
                            runtime.Ui.Find(node.RegionId) is not { Font: { } nodeFont } region ||
                            !runtime.Ui.IsVisible(region))
                        {
                            continue;
                        }

                        lua_newtable(state);
                        SetTableString(state, "text", nodeFont.Text);
                        SetTableString(state, "type", node.TextType);
                        SetTableString(state, "align", node.Align);
                        lua_rawseti(state, -2, outputIndex++);
                    }
                    return 1;
                }
            case "SetFormattedText":
                {
                    if (value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) &&
                        !EnsureFont(value).IsConfigured)
                    {
                        runtime.Log.Warn("lua", "FontString:SetFormattedText(): Font not set");
                        return 0;
                    }
                    var argumentCount = lua_gettop(state) - 1;
                    lua_getglobal(state, "string");
                    lua_getfield(state, -1, "format");
                    for (var index = 2; index <= argumentCount + 1; index++)
                        lua_pushvalue(state, index);
                    if (lua_pcall(state, argumentCount, 1, 0) != 0)
                        return lua_error(state);
                    var text = TruncateUtf8(lua_tostring(state, -1) ?? string.Empty, 4095);
                    text = ProcessStoredFontStringText(value, text);
                    var nextText = value.MaximumLetters > 0 && text.Length > value.MaximumLetters
                        ? text[..value.MaximumLetters]
                        : text;
                    var changed = !value.TextValue.Equals(nextText, StringComparison.Ordinal);
                    SetObjectText(runtime, value, nextText);
                    runtime.Ui.InvalidateLayout();
                    if (changed && IsEditBox(value))
                    {
                        runtime.QueueEditBoxTextChanged(value, false);
                        runtime.InvokeScript(value, "OnTextSet");
                    }
                    return 0;
                }
            case "SetTextToFit":
                {
                    if (lua_gettop(state) >= 2 &&
                        lua_isnil(state, 2) == 0 &&
                        lua_isstring(state, 2) == 0)
                    {
                        return luaL_error(state, "Usage: self:SetTextToFit([text])");
                    }
                    var text = ProcessStoredFontStringText(
                        value,
                        OptionalString(state, 2) ?? string.Empty);
                    if (EnsureFont(value).IsConfigured)
                        SetObjectText(runtime, value, text);
                    else
                        runtime.Log.Warn("lua", "FontString:SetTextToFit(): Font not set");
                    value.Width = 0;
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "SetTextHeight":
                {
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, "Usage: self:SetTextHeight(height)");
                    var textHeight = (float)lua_tonumber(state, 2);
                    if (textHeight > 0 && EnsureFont(value).IsConfigured)
                    {
                        var font = EnsureFont(value);
                        if (MathF.Abs(font.FontSize - textHeight) >= 2.3841858e-7f)
                        {
                            font.FontSize = textHeight;
                            MarkFontOverride(runtime, value, font, UiFontOverrides.FontSize);
                            runtime.Ui.InvalidateLayout();
                        }
                    }
                    return 0;
                }
            case "SetNumeric":
                {
                    var editBoxNumeric = OptionalBoolean(state, 2, true);
                    var isNumeric =
                        value.Attributes.TryGetValue("Numeric", out var numericState) &&
                        numericState is true;
                    if (isNumeric == editBoxNumeric &&
                        (!editBoxNumeric || !value.EditBoxNumericFullRange))
                    {
                        return 0;
                    }
                    SetEditBoxInputMode(value, false, false, false, editBoxNumeric);
                    if (editBoxNumeric)
                        ReapplyEditBoxTextRules(runtime, value);
                    return 0;
                }
            case "IsNumeric":
                lua_pushboolean(
                    state,
                    value.Attributes.TryGetValue("Numeric", out var numeric) &&
                    numeric is true ? 1 : 0);
                return 1;
            case "SetNumber":
                {
                    if (value.SecurityDisableSetText)
                        return luaL_error(
                            state,
                            "Call is illegal when disabled by security settings.");
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, "Usage: self:SetNumber(number)");
                    var numericValue = (float)lua_tonumber(state, 2);
                    var integral = MathF.Truncate(numericValue);
                    var text = numericValue - integral == 0
                        ? ((int)numericValue).ToString(CultureInfo.InvariantCulture)
                        : numericValue.ToString("F6", CultureInfo.InvariantCulture);
                    var formattedNumber = EditBoxTextRules.ApplyReplacement(value, text);
                    var numberChanged = !formattedNumber.Equals(
                        value.TextValue,
                        StringComparison.Ordinal);
                    SetObjectText(runtime, value, formattedNumber);
                    runtime.Ui.InvalidateLayout();
                    if (numberChanged)
                    {
                        runtime.QueueEditBoxTextChanged(value, false);
                        runtime.InvokeScript(value, "OnTextSet");
                    }
                    return 0;
                }
            case "GetNumber":
                lua_pushnumber(
                    state,
                    double.TryParse(
                        value.TextValue,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var number)
                            ? number
                            : 0);
                return 1;
            case "HasFocus":
                lua_pushboolean(state, runtime.Ui.FocusedObjectId == value.Id ? 1 : 0);
                return 1;
            case "GetInputLanguage":
                lua_pushstring(state, value.EditBoxInputLanguage.ToWowName());
                return 1;
            case "SetAlphaGradient":
                {
                    if (value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase))
                    {
                        const string usage =
                            "Usage: local isWithinText = self:SetAlphaGradient(start, length)";
                        if (!TryReadRequiredInt32(state, 2, out var start) ||
                            !TryReadRequiredInt32(state, 3, out var length))
                        {
                            return luaL_error(state, usage);
                        }

                        var font = EnsureFont(value);
                        var isWithinText = false;
                        if (length > 0)
                        {
                            value.FontAlphaGradientStart = unchecked((ushort)start);
                            value.FontAlphaGradientLength = unchecked((ushort)length);
                            var drawableQuadCount = WowTextMarkup
                                .PlainText(font.Text)
                                .EnumerateRunes()
                                .Count(rune =>
                                    !Rune.IsWhiteSpace(rune) &&
                                    !Rune.IsControl(rune));
                            isWithinText = UiTextAlphaGradient.ContainsQuad(
                                value.FontAlphaGradientStart,
                                value.FontAlphaGradientLength,
                                drawableQuadCount);
                            runtime.Ui.InvalidateLayout();
                        }

                        lua_pushboolean(state, isWithinText ? 1 : 0);
                        return 1;
                    }

                    const string frameUsage =
                        "Usage: self:SetAlphaGradient(index, gradient)";
                    if (!TryReadRequiredInt32(state, 2, out var edgeIndex) ||
                        !TryReadRequiredVector2Table(state, 3, out var gradient))
                    {
                        return luaL_error(state, frameUsage);
                    }

                    value.HasFrameAlphaGradient = true;
                    if (edgeIndex is 0 or 1)
                        value.FrameAlphaGradientEdges[edgeIndex] = gradient;
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "ClearAlphaGradient":
                if (value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase))
                {
                    value.FontAlphaGradientStart = ushort.MaxValue;
                    value.FontAlphaGradientLength = 0;
                }
                else
                {
                    value.HasFrameAlphaGradient = false;
                    Array.Clear(value.FrameAlphaGradientEdges);
                }
                runtime.Ui.InvalidateLayout();
                return 0;
            case "HasAlphaGradient":
                lua_pushboolean(state, value.HasFrameAlphaGradient ? 1 : 0);
                return 1;
            case "GetAlphaGradient":
                lua_pushnumber(state, unchecked((short)value.FontAlphaGradientStart));
                lua_pushnumber(state, unchecked((short)value.FontAlphaGradientLength));
                return 2;
            case "SetSmoothScaling":
                if (!TryReadRequiredBoolean(state, 2, out var smoothScaling))
                    return luaL_error(state, "Usage: self:SetSmoothScaling(smoothScaling)");
                value.FontSmoothScaling = smoothScaling;
                runtime.Ui.InvalidateLayout();
                return 0;
            case "GetSmoothScaling":
                lua_pushboolean(state, value.FontSmoothScaling ? 1 : 0);
                return 1;
            case "SetScaleAnimationMode":
                if (!TryReadRequiredInt32(state, 2, out var scaleAnimationMode) ||
                    scaleAnimationMode is < 0 or > 1)
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetScaleAnimationMode(scaleAnimationMode)");
                }
                value.FontScaleAnimationMode = (byte)scaleAnimationMode;
                return 0;
            case "GetScaleAnimationMode":
                lua_pushinteger(state, value.FontScaleAnimationMode);
                return 1;
            case "SetFixedColor":
                if (!TryReadRequiredBoolean(state, 2, out var fixedColor))
                    return luaL_error(state, "Usage: self:SetFixedColor(fixedColor)");
                value.FontFixedColor = fixedColor;
                runtime.Ui.InvalidateLayout();
                return 0;
            case "OnColorsUpdated":
                runtime.Ui.InvalidateLayout();
                return 0;
            case "RegisterAllEvents":
                value.AllEventsRegistered = true;
                runtime.IndexAllEventsTarget(value);
                return 0;
            case "SetMinimumWidth":
                {
                    const string usage = "Usage: self:SetMinimumWidth(width [, force])";
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, usage);
                    var tooltip = EnsureTooltip(value);
                    tooltip.MinimumWidth = (float)lua_tonumber(state, 2);
                    tooltip.ForceMinimumWidth = OptionalBoolean(state, 3, false);
                    LayoutTooltip(runtime, value);
                    return 0;
                }
            case "GetMinimumWidth":
                {
                    var tooltip = EnsureTooltip(value);
                    lua_pushnumber(state, tooltip.MinimumWidth);
                    lua_pushboolean(state, tooltip.ForceMinimumWidth ? 1 : 0);
                    return 2;
                }
            case "SetPadding":
                {
                    const string usage =
                        "Usage: self:SetPadding(right, bottom [, left, top])";
                    if (lua_isnumber(state, 2) == 0 ||
                        lua_isnumber(state, 3) == 0 ||
                        lua_gettop(state) >= 4 &&
                        lua_isnil(state, 4) == 0 &&
                        lua_isnumber(state, 4) == 0 ||
                        lua_gettop(state) >= 5 &&
                        lua_isnil(state, 5) == 0 &&
                        lua_isnumber(state, 5) == 0)
                    {
                        return luaL_error(state, usage);
                    }
                    var current = EnsureTooltip(value).Padding;
                    var right = (float)lua_tonumber(state, 2);
                    var bottom = (float)lua_tonumber(state, 3);
                    var left = lua_gettop(state) >= 4 && lua_isnil(state, 4) == 0
                        ? (float)lua_tonumber(state, 4)
                        : current.Left;
                    var top = lua_gettop(state) >= 5 && lua_isnil(state, 5) == 0
                        ? (float)lua_tonumber(state, 5)
                        : current.Top;
                    EnsureTooltip(value).Padding = new UiInsets(left, right, top, bottom);
                    LayoutTooltip(runtime, value);
                    return 0;
                }
            case "GetPadding":
                {
                    var padding = EnsureTooltip(value).Padding;
                    lua_pushnumber(state, padding.Right);
                    lua_pushnumber(state, padding.Bottom);
                    lua_pushnumber(state, padding.Left);
                    lua_pushnumber(state, padding.Top);
                    return 4;
                }
            case "ClearPadding":
                EnsureTooltip(value).Padding = default;
                LayoutTooltip(runtime, value);
                return 0;
            case "GetText":
                {
                    var text = IsButton(value)
                        ? value.ButtonFontStringId is { } buttonFontStringId &&
                          runtime.Ui.Find(buttonFontStringId) is { } buttonFontString
                            ? buttonFontString.Font?.Text ?? buttonFontString.TextValue
                            : string.Empty
                        : value.Font?.Text ?? value.TextValue;
                    if (value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) &&
                        text.Length == 0 ||
                        IsButton(value) && text.Length == 0)
                    {
                        lua_pushnil(state);
                    }
                    else
                    {
                        lua_pushstring(state, text);
                    }
                    return 1;
                }
            case "GetFontString":
                if (IsButton(value))
                {
                    if (value.ButtonFontStringId is { } fontStringId &&
                        runtime.Ui.Find(fontStringId) is { } fontString)
                    {
                        runtime.PushObject(fontString);
                    }
                    else
                    {
                        lua_pushnil(state);
                    }
                }
                else
                    lua_pushnil(state);
                return 1;
            case "SetFontString":
                {
                    var fontString = GetObject(runtime, 2);
                    if (fontString is null ||
                        !fontString.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase))
                    {
                        return luaL_error(state, "Usage: self:SetFontString(fontString)");
                    }
                    if (value.ButtonFontStringId == fontString.Id)
                        return 0;

                    if (value.ButtonFontStringId is { } previousId &&
                        runtime.Ui.Find(previousId) is { } previous)
                    {
                        previous.Shown = false;
                    }

                    runtime.Ui.Reparent(fontString, value.Id);
                    value.ButtonFontStringId = fontString.Id;
                    fontString.Shown = true;
                    value.TextValue = fontString.Font?.Text ?? fontString.TextValue;
                    RefreshButtonFont(runtime, value);
                    AnchorButtonFontString(value, fontString);
                    return 0;
                }
            case "SetFont":
                {
                    const string usage = "Usage: self:SetFont(fontFile, height, flags)";
                    var fontFileIndex = 2;
                    if (IsSimpleHtml(value))
                    {
                        if (!TryReadSimpleHtmlTextType(state, 2, out _))
                        {
                            return luaL_error(
                                state,
                                "Usage: self:SetFont(textType, fontFile, height, flags)");
                        }
                        fontFileIndex = 3;
                    }
                    if (lua_gettop(state) < fontFileIndex + 1 ||
                        lua_type(state, fontFileIndex) != LUA_TSTRING ||
                        lua_type(state, fontFileIndex + 1) != LUA_TNUMBER)
                    {
                        return luaL_error(state, usage);
                    }
                    var fontPath = lua_tostring(state, fontFileIndex);
                    var fontHeight = lua_tonumber(state, fontFileIndex + 1);
                    if (string.IsNullOrEmpty(fontPath) || fontHeight <= 0)
                        return luaL_error(state, usage);
                    var flagsIndex = fontFileIndex + 2;
                    if (lua_gettop(state) >= flagsIndex &&
                        lua_isnil(state, flagsIndex) == 0 &&
                        lua_type(state, flagsIndex) != LUA_TSTRING)
                    {
                        return luaL_error(state, usage);
                    }
                    if (!TryNormalizeFontFlags(
                            OptionalString(state, flagsIndex),
                            out var fontFlags))
                        return luaL_error(state, usage);

                    var font = IsSimpleHtml(value) &&
                               TryReadSimpleHtmlTextType(state, 2, out var textType)
                        ? EnsureHtmlFont(value, textType)
                        : EnsureFont(value);
                    font.FontPath = fontPath;
                    font.FontSize = (float)Math.Min(fontHeight, 120);
                    font.FontFlags = fontFlags;
                    font.IsConfigured = true;
                    MarkFontOverride(
                        runtime,
                        value,
                        font,
                        UiFontOverrides.FontPath |
                        UiFontOverrides.FontSize |
                        UiFontOverrides.FontFlags);
                    if (IsSimpleHtml(value))
                        UpdateSimpleHtmlContentHeight(value);
                    runtime.Ui.InvalidateLayout();
                    if (value.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase))
                        return 0;
                    lua_pushboolean(state, 1);
                    return 1;
                }
            case "SetFontHeight":
                {
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, "Usage: self:SetFontHeight(height)");
                    var font = EnsureFont(value);
                    var isFont = value.ObjectType.Equals(
                        "Font",
                        StringComparison.OrdinalIgnoreCase);
                    if (!font.IsConfigured)
                    {
                        if (!isFont)
                            runtime.Log.Warn("lua", "FontString:SetFontHeight(): Font not set");
                        return 0;
                    }
                    var fontHeight = (float)lua_tonumber(state, 2);
                    if (isFont && !(fontHeight > 0))
                        return 0;
                    font.FontSize = fontHeight;
                    MarkFontOverride(runtime, value, font, UiFontOverrides.FontSize);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "GetFont":
                {
                    UiFontState font;
                    if (IsSimpleHtml(value))
                    {
                        if (!TryReadSimpleHtmlTextType(state, 2, out var textType))
                        {
                            return luaL_error(
                                state,
                                "Usage: local fontFile, height, flags = self:GetFont(textType)");
                        }
                        font = EnsureHtmlFont(value, textType);
                    }
                    else
                    {
                        font = EnsureFont(value);
                    }
                    if (font.IsConfigured)
                    {
                        lua_pushstring(state, font.FontPath);
                        lua_pushnumber(state, font.FontSize);
                    }
                    else
                    {
                        if (value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase))
                        {
                            lua_pushnil(state);
                            lua_pushnumber(state, -1);
                        }
                        else
                        {
                            lua_pushnil(state);
                            lua_pushnumber(state, 0);
                        }
                    }
                    lua_pushstring(state, font.FontFlags);
                    return 3;
                }
            case "GetFontHeight":
                {
                    var font = EnsureFont(value);
                    var height = font.IsConfigured
                        ? font.FontSize
                        : value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase)
                            ? -1
                            : 0;
                    if (font.IsConfigured &&
                        value.ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) &&
                        OptionalBoolean(state, 2, true))
                    {
                        height = UiTextLineMetrics.ResolveLogicalLineHeight(
                            font.FontSize,
                            font.TextScale,
                            runtime.Ui.PhysicalHeight,
                            runtime.Ui.EffectiveScale(value),
                            value.FontSmoothScaling);
                    }
                    lua_pushnumber(
                        state,
                        height);
                    return 1;
                }
            case "SetTextColor":
                {
                    var start = IsSimpleHtml(value) ? 3 : 2;
                    if (!TryReadRequiredNormalizedColor(state, start, out var color))
                        return luaL_error(state, "Usage: self:SetTextColor(color [, a])");
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: self:SetTextColor(textType, color [, a])");
                    font.Color = color;
                    MarkFontOverride(runtime, value, font, UiFontOverrides.Color);
                    return 0;
                }
            case "GetTextColor":
                {
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: local color = self:GetTextColor(textType)");
                    var color = font.Color;
                    lua_pushnumber(state, color.X);
                    lua_pushnumber(state, color.Y);
                    lua_pushnumber(state, color.Z);
                    lua_pushnumber(state, color.W);
                    return 4;
                }
            case "SetShadowColor":
                {
                    var start = IsSimpleHtml(value) ? 3 : 2;
                    if (!TryReadRequiredNormalizedColor(state, start, out var color))
                        return luaL_error(state, "Usage: self:SetShadowColor(color [, a])");
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: self:SetShadowColor(textType, color [, a])");
                    font.ShadowColor = color;
                    MarkFontOverride(runtime, value, font, UiFontOverrides.ShadowColor);
                    return 0;
                }
            case "GetShadowColor":
                {
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: local color = self:GetShadowColor(textType)");
                    var color = font.ShadowColor;
                    lua_pushnumber(state, color.X);
                    lua_pushnumber(state, color.Y);
                    lua_pushnumber(state, color.Z);
                    lua_pushnumber(state, color.W);
                    return 4;
                }
            case "SetShadowOffset":
                {
                    var start = IsSimpleHtml(value) ? 3 : 2;
                    if (lua_gettop(state) < start + 1 ||
                        lua_isnumber(state, start) == 0 ||
                        lua_isnumber(state, start + 1) == 0)
                    {
                        return luaL_error(state, "Usage: self:SetShadowOffset(offset)");
                    }
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: self:SetShadowOffset(textType, offset)");
                    font.ShadowOffset = new Vector2(
                        (float)lua_tonumber(state, start),
                        (float)lua_tonumber(state, start + 1));
                    MarkFontOverride(runtime, value, font, UiFontOverrides.ShadowOffset);
                    return 0;
                }
            case "GetShadowOffset":
                {
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: local offset = self:GetShadowOffset(textType)");
                    var offset = font.ShadowOffset;
                    lua_pushnumber(state, offset.X);
                    lua_pushnumber(state, offset.Y);
                    return 2;
                }
            case "SetJustifyH":
                {
                    var argumentIndex = IsSimpleHtml(value) ? 3 : 2;
                    if (lua_type(state, argumentIndex) != LUA_TSTRING)
                        return luaL_error(state, "Usage: self:SetJustifyH(justifyH)");
                    var justification = lua_tostring(state, argumentIndex);
                    if (justification is null ||
                        (!justification.Equals("LEFT", StringComparison.OrdinalIgnoreCase) &&
                         !justification.Equals("CENTER", StringComparison.OrdinalIgnoreCase) &&
                         !justification.Equals("RIGHT", StringComparison.OrdinalIgnoreCase)))
                    {
                        return luaL_error(state, "Usage: self:SetJustifyH(justifyH)");
                    }
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: self:SetJustifyH(textType, justifyH)");
                    font.JustifyHorizontal = justification.ToUpperInvariant();
                    font.HasLocalJustifyHorizontal = true;
                    MarkFontOverride(
                        runtime,
                        value,
                        font,
                        UiFontOverrides.JustifyHorizontal);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "GetJustifyH":
                if (!TryGetCallFont(value, state, out var horizontalFont))
                    return luaL_error(state, "Usage: local justifyH = self:GetJustifyH(textType)");
                lua_pushstring(state, horizontalFont.JustifyHorizontal);
                return 1;
            case "SetJustifyV":
                {
                    var argumentIndex = IsSimpleHtml(value) ? 3 : 2;
                    if (lua_type(state, argumentIndex) != LUA_TSTRING)
                        return luaL_error(state, "Usage: self:SetJustifyV(justifyV)");
                    var justification = lua_tostring(state, argumentIndex);
                    if (justification is null ||
                        (!justification.Equals("TOP", StringComparison.OrdinalIgnoreCase) &&
                         !justification.Equals("MIDDLE", StringComparison.OrdinalIgnoreCase) &&
                         !justification.Equals("BOTTOM", StringComparison.OrdinalIgnoreCase)))
                    {
                        return luaL_error(state, "Usage: self:SetJustifyV(justifyV)");
                    }
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: self:SetJustifyV(textType, justifyV)");
                    font.JustifyVertical = justification.ToUpperInvariant();
                    font.HasLocalJustifyVertical = true;
                    MarkFontOverride(
                        runtime,
                        value,
                        font,
                        UiFontOverrides.JustifyVertical);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "GetJustifyV":
                if (!TryGetCallFont(value, state, out var verticalFont))
                    return luaL_error(state, "Usage: local justifyV = self:GetJustifyV(textType)");
                lua_pushstring(state, verticalFont.JustifyVertical);
                return 1;
            case "SetSpacing":
                {
                    var argumentIndex = IsSimpleHtml(value) ? 3 : 2;
                    if (lua_isnumber(state, argumentIndex) == 0)
                        return luaL_error(state, "Usage: self:SetSpacing(spacing)");
                    if (!TryGetCallFont(value, state, out var font))
                        return luaL_error(state, "Usage: self:SetSpacing(textType, spacing)");
                    var requestedSpacing = (float)lua_tonumber(state, argumentIndex);
                    font.Spacing = value.ObjectType.Equals(
                        "Font",
                        StringComparison.OrdinalIgnoreCase)
                        ? requestedSpacing
                        : float.IsNaN(requestedSpacing)
                            ? 0
                            : MathF.Max(requestedSpacing, 0);
                    MarkFontOverride(runtime, value, font, UiFontOverrides.Spacing);
                    if (IsSimpleHtml(value))
                        UpdateSimpleHtmlContentHeight(value);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "GetSpacing":
                if (!TryGetCallFont(value, state, out var spacingFont))
                    return luaL_error(state, "Usage: local spacing = self:GetSpacing(textType)");
                lua_pushnumber(state, spacingFont.Spacing);
                return 1;
            case "SetMaxLines":
                {
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, "Usage: self:SetMaxLines(maxLines)");
                    var maxLines = lua_tonumber(state, 2);
                    if (!double.IsFinite(maxLines) || maxLines < 0 || maxLines > uint.MaxValue)
                        return luaL_error(state, "Usage: self:SetMaxLines(maxLines)");
                    var font = EnsureFont(value);
                    font.MaximumLines = (int)((uint)maxLines & 0x00FF_FFFFu);
                    MarkFontOverride(runtime, value, font, UiFontOverrides.MaximumLines);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "GetMaxLines":
                lua_pushinteger(state, EnsureFont(value).MaximumLines);
                return 1;
            case "SetIndentedWordWrap":
                {
                    var argumentIndex = IsSimpleHtml(value) ? 3 : 2;
                    if (lua_gettop(state) < argumentIndex ||
                        lua_isnil(state, argumentIndex) != 0)
                        return luaL_error(state, "Usage: self:SetIndentedWordWrap(wrap)");
                    if (!TryGetCallFont(value, state, out var font))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetIndentedWordWrap(textType, wordWrap)");
                    }
                    font.IndentedWordWrap = lua_toboolean(state, argumentIndex) != 0;
                    MarkFontOverride(
                        runtime,
                        value,
                        font,
                        UiFontOverrides.IndentedWordWrap);
                    return 0;
                }
            case "GetIndentedWordWrap":
                if (!TryGetCallFont(value, state, out var indentedFont))
                {
                    return luaL_error(
                        state,
                        "Usage: local wordWrap = self:GetIndentedWordWrap(textType)");
                }
                lua_pushboolean(state, indentedFont.IndentedWordWrap ? 1 : 0);
                return 1;
            case "SetTextScale":
                {
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, "Usage: self:SetTextScale(textScale)");
                    var textScaleNumber = lua_tonumber(state, 2);
                    if (textScaleNumber < -float.MaxValue || textScaleNumber > float.MaxValue)
                        return luaL_error(state, "Usage: self:SetTextScale(textScale)");
                    var textScale = (float)textScaleNumber;
                    if (textScale > 1.1920929e-7f)
                    {
                        var font = EnsureFont(value);
                        if (MathF.Abs(font.TextScale - textScale) >= 2.3841858e-7f)
                        {
                            font.TextScale = textScale;
                            MarkFontOverride(runtime, value, font, UiFontOverrides.TextScale);
                            runtime.Ui.InvalidateLayout();
                        }
                    }
                    return 0;
                }
            case "GetTextScale":
                lua_pushnumber(state, EnsureFont(value).TextScale);
                return 1;
            case "GetLineHeight":
                {
                    var font = EnsureFont(value);
                    lua_pushnumber(
                        state,
                        font.IsConfigured
                            ? UiTextLineMetrics.ResolveLogicalLineHeight(
                                font.FontSize,
                                font.TextScale,
                                runtime.Ui.PhysicalHeight,
                                runtime.Ui.EffectiveScale(value),
                                value.FontSmoothScaling)
                            : 0);
                    return 1;
                }
            case "GetStringWidth":
                lua_pushnumber(state, MeasureText(runtime, value).Size.X);
                return 1;
            case "GetStringHeight":
                lua_pushnumber(state, MeasureText(runtime, value).Size.Y);
                return 1;
            case "GetUnboundedStringWidth":
                lua_pushnumber(
                    state,
                    MeasureText(
                        runtime,
                        value,
                        ignoreMaximumLines: true,
                        ignoreWidthConstraint: true).Size.X);
                return 1;
            case "GetUnboundedStringWidthForText":
                {
                    if (!TryReadRequiredString(state, 2, out var measuredText))
                    {
                        return luaL_error(
                            state,
                            "Usage: local width = self:GetUnboundedStringWidthForText(text)");
                    }
                    var measuredFont = CopyFont(EnsureFont(value));
                    measuredFont.Text = measuredText;
                    lua_pushnumber(
                        state,
                        MeasureText(
                            runtime,
                            value,
                            measuredFont,
                            ignoreMaximumLines: true,
                            ignoreWidthConstraint: true).Size.X);
                    return 1;
                }
            case "GetWrappedWidth":
                lua_pushnumber(
                    state,
                    MeasureText(
                        runtime,
                        value,
                        ignoreMaximumLines: true).Size.X);
                return 1;
            case "GetFieldSize":
                lua_pushnumber(state, 0x1FFF);
                return 1;
            case "FindCharacterIndexAtCoordinate":
                {
                    const string usage =
                        "Usage: local characterIndex, inside = self:FindCharacterIndexAtCoordinate(x, y)";
                    if (!TryReadRequiredFloat(state, 2, out var coordinateX) ||
                        !TryReadRequiredFloat(state, 3, out var coordinateY))
                    {
                        return luaL_error(state, usage);
                    }
                    if (!TryBuildFontStringScreenLines(runtime, value, out var screenLines))
                        return 0;

                    var line = screenLines.FirstOrDefault(
                        candidate => coordinateY >= candidate.HitBottom);
                    if (line is null)
                        return 0;

                    var characterOffset = line.CharacterWidth <= 0
                        ? 0
                        : (int)MathF.Floor(
                            ((float)coordinateX - line.Left) / line.CharacterWidth + 0.5f);
                    characterOffset = Math.Clamp(
                        characterOffset,
                        0,
                        line.ByteBoundaries.Count - 1);
                    lua_pushinteger(
                        state,
                        line.ByteBoundaries[characterOffset] + 1);
                    var bounds = runtime.Ui.ResolveBounds(value.Id);
                    var insideLeft = value.Font!.JustifyHorizontal.Equals(
                        "CENTER",
                        StringComparison.OrdinalIgnoreCase)
                            ? line.Left
                            : bounds.Left;
                    var insideRight = value.Font.JustifyHorizontal.Equals(
                        "CENTER",
                        StringComparison.OrdinalIgnoreCase)
                            ? line.Right
                            : bounds.Right;
                    lua_pushboolean(
                        state,
                        coordinateX >= insideLeft &&
                        coordinateX <= insideRight &&
                        coordinateY >= bounds.Bottom &&
                        coordinateY <= bounds.Top
                            ? 1
                            : 0);
                    return 2;
                }
            case "CalculateScreenAreaFromCharacterSpan":
                {
                    const string usage =
                        "Usage: local areas = self:CalculateScreenAreaFromCharacterSpan(leftIndex, rightIndex)";
                    if (!TryReadRequiredOneBasedIndex(state, 2, out var left) ||
                        !TryReadRequiredOneBasedIndex(state, 3, out var right))
                    {
                        return luaL_error(state, usage);
                    }
                    if (!TryBuildFontStringScreenLines(
                            runtime,
                            value,
                            out var screenLines,
                            applySpanIndent: true))
                    {
                        lua_pushnil(state);
                        return 1;
                    }

                    var textLength = screenLines.Max(line => line.EndByteOffset);
                    if (left > right || right > (uint)textLength)
                    {
                        lua_pushnil(state);
                        return 1;
                    }

                    var leftOffset = (int)left;
                    var rightOffset = (int)right;
                    lua_newtable(state);
                    var resultIndex = 1;
                    foreach (var line in screenLines)
                    {
                        var spanStart = Math.Max(leftOffset, line.StartByteOffset);
                        var spanEnd = Math.Min(rightOffset, line.EndByteOffset);
                        if (spanStart > spanEnd ||
                            spanStart == spanEnd && leftOffset != rightOffset)
                        {
                            continue;
                        }

                        var startCharacter = line.CharacterIndexAtOrBefore(spanStart);
                        var endCharacter = line.CharacterIndexAtOrBefore(spanEnd);
                        lua_newtable(state);
                        SetTableNumber(
                            state,
                            "left",
                            line.Left + startCharacter * line.CharacterWidth);
                        SetTableNumber(state, "bottom", line.Bottom);
                        SetTableNumber(
                            state,
                            "width",
                            (endCharacter - startCharacter) * line.CharacterWidth);
                        SetTableNumber(state, "height", line.Top - line.Bottom);
                        lua_rawseti(state, -2, resultIndex++);
                    }
                    return 1;
                }
            case "GetTextWidth":
                if (IsButton(value))
                {
                    var width = value.ButtonFontStringId is { } fontStringId &&
                                runtime.Ui.Find(fontStringId) is { } fontString
                        ? MeasureText(runtime, fontString).Size.X
                        : 0;
                    lua_pushnumber(state, width);
                }
                else
                {
                    lua_pushnumber(
                        state,
                        MeasureText(
                            runtime,
                            value,
                            ignoreWidthConstraint: true).Size.X);
                }
                return 1;
            case "GetTextHeight":
                {
                    var height = IsButton(value) &&
                                 value.ButtonFontStringId is { } fontStringId &&
                                 runtime.Ui.Find(fontStringId) is { } fontString
                        ? MeasureText(runtime, fontString).Size.Y
                        : 0;
                    lua_pushnumber(state, height);
                    return 1;
                }
            case "IsTruncated":
                lua_pushboolean(state, IsFontStringTruncated(runtime, value) ? 1 : 0);
                return 1;
            case "GetNumLines":
                {
                    var lines = MeasureText(
                        runtime,
                        value,
                        ignoreMaximumLines: true).LineCount;
                    lua_pushinteger(state, Math.Clamp(lines, 0, 1024));
                    return 1;
                }
            case "SetAutoFocus":
                value.AutoFocus = OptionalBoolean(state, 2, false);
                return 0;
            case "SetMultiLine":
                value.MultiLine = OptionalBoolean(state, 2, false);
                var editBoxFont = EnsureFont(value);
                editBoxFont.JustifyVertical = value.MultiLine ? "TOP" : "MIDDLE";
                editBoxFont.WordWrap = value.MultiLine;
                runtime.Ui.InvalidateLayout();
                return 0;
            case "SetMaxLetters":
                if (!TryReadRequiredInt32(state, 2, out var maximumLetters))
                    return luaL_error(state, "Usage: self:SetMaxLetters(maxLetters)");
                value.MaximumLetters = maximumLetters;
                return 0;
            case "SetTextInsets":
                if (!TryReadRequiredFloat(state, 2, out var textInsetLeft) ||
                    !TryReadRequiredFloat(state, 3, out var textInsetRight) ||
                    !TryReadRequiredFloat(state, 4, out var textInsetTop) ||
                    !TryReadRequiredFloat(state, 5, out var textInsetBottom))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetTextInsets(left, right, top, bottom)");
                }
                value.TextInsets = new Vector4(
                    (float)textInsetLeft,
                    (float)textInsetRight,
                    (float)textInsetTop,
                    (float)textInsetBottom);
                runtime.Ui.InvalidateLayout();
                return 0;
            case "SetFocus":
                runtime.SetKeyboardFocus(value);
                return 0;
            case "ClearFocus":
                if (runtime.Ui.FocusedObjectId == value.Id)
                    runtime.SetKeyboardFocus(null);
                return 0;
            case "SetHighlightTexture":
                SetButtonTexture(runtime, value, ButtonTextureKind.Highlight);
                return 0;
            case "GetHighlightTexture":
                runtime.PushObject(value.HighlightTextureId is { } textureId ? runtime.Ui.Find(textureId) : null);
                return 1;
            case "SetPortraitZoom":
                if (!TryReadRequiredFloat(state, 2, out var portraitZoom))
                    return luaL_error(state, "Usage: self:SetPortraitZoom(zoom)");
                if (value.ModelPortraitZoom != (float)portraitZoom)
                {
                    value.ModelPortraitZoom = (float)portraitZoom;
                    if (HasLoadedModel(value))
                    {
                        value.ModelCameraRefreshRevision++;
                        RefreshCharacterModelCamera(runtime, value);
                    }
                }
                return 0;
            case "SetCamDistanceScale":
                if (!TryReadRequiredFloat(state, 2, out var camDistanceScale))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetCamDistanceScale(scale)");
                }
                var clampedCamDistanceScale = MathF.Max((float)camDistanceScale, 0.1f);
                if (value.ModelCamDistanceScale != clampedCamDistanceScale)
                {
                    value.ModelCamDistanceScale = clampedCamDistanceScale;
                    if (HasLoadedModel(value))
                    {
                        value.ModelCameraRefreshRevision++;
                        RefreshCharacterModelCamera(runtime, value);
                    }
                }
                return 0;
            case "RefreshCamera":
                if (value.ObjectType.Equals(
                        "CinematicModel",
                        StringComparison.OrdinalIgnoreCase) ||
                    HasLoadedModel(value))
                {
                    value.ModelCameraRefreshRevision++;
                    if (!value.ObjectType.Equals(
                            "CinematicModel",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        RefreshCharacterModelCamera(runtime, value);
                    }
                }
                return 0;
            case "RefreshUnit":
                value.ModelUnitRefreshRevision++;
                if (HasLoadedModel(value))
                {
                    value.ModelCameraRefreshRevision++;
                    RefreshCharacterModelCamera(runtime, value);
                }
                return 0;
            case "ZeroCachedCenterXY":
                value.ModelCachedCenterXY = Vector2.Zero;
                return 0;
            case "SetScrollChild":
                {
                    var child = GetObject(runtime, 2);
                    if (child is null || !IsFrameObject(child))
                        return luaL_error(state, "Usage: self:SetScrollChild(scrollChild)");
                    if (ScrollChildWouldCreateLoop(runtime, value, child))
                        return luaL_error(
                            state,
                            $"Would create a loop adding child {child.Name ?? child.ObjectType}");
                    if (value.ScrollChildId == child.Id)
                        return 0;
                    if (value.ScrollChildId is { } previousId &&
                        runtime.Ui.Find(previousId) is { } previous)
                    {
                        runtime.Ui.Reparent(previous, null);
                    }
                    runtime.Ui.Reparent(child, value.Id);
                    value.ScrollChildId = child.Id;
                    runtime.Ui.InvalidateLayout();
                    runtime.QueueScrollChildRect(value);
                    return 0;
                }
            case "SetHorizontalScroll":
                {
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, "Usage: self:SetHorizontalScroll(offset)");
                    var next = (float)lua_tonumber(state, 2);
                    if (Math.Abs(next - value.HorizontalScroll) >= 0.00000095367432f)
                    {
                        value.HorizontalScroll = next;
                        runtime.Ui.InvalidateLayout();
                        runtime.InvokeScript(value, "OnHorizontalScroll", next);
                    }
                    return 0;
                }
            case "SetVerticalScroll":
                {
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, "Usage: self:SetVerticalScroll(offset)");
                    var next = (float)lua_tonumber(state, 2);
                    if (Math.Abs(next - value.VerticalScroll) >= 0.00000095367432f)
                    {
                        value.VerticalScroll = next;
                        runtime.Ui.InvalidateLayout();
                        runtime.InvokeScript(value, "OnVerticalScroll", next);
                    }
                    return 0;
                }
            case "GetHorizontalScroll":
                lua_pushnumber(state, value.HorizontalScroll);
                return 1;
            case "GetScrollChild":
                runtime.PushObject(value.ScrollChildId is { } scrollChildId
                    ? runtime.Ui.Find(scrollChildId)
                    : null);
                return 1;
            case "GetVerticalScroll":
                lua_pushnumber(state, value.VerticalScroll);
                return 1;
            case "GetHorizontalScrollRange":
                lua_pushnumber(state, value.HorizontalScrollRange);
                return 1;
            case "GetVerticalScrollRange":
                lua_pushnumber(state, value.VerticalScrollRange);
                return 1;
            case "UpdateScrollChildRect":
                UpdateScrollChildRect(runtime, value);
                return 0;
            case "SetObeyStepOnDrag":
                if (lua_gettop(state) < 2 || lua_isnil(state, 2) != 0)
                    return luaL_error(
                        state,
                        "Usage: self:SetObeyStepOnDrag(obeyStepOnDrag)");
                value.ObeyStepOnDrag = lua_toboolean(state, 2) != 0;
                return 0;
            case "GetObeyStepOnDrag":
                lua_pushboolean(state, value.ObeyStepOnDrag ? 1 : 0);
                return 1;
            case "SetValueStep":
                {
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, "Usage: self:SetValueStep(valueStep)");
                    const float minimumStep = 0.00000011920929f;
                    const float changeEpsilon = 0.00000023841858f;
                    var nextStep = MathF.Max((float)lua_tonumber(state, 2), minimumStep);
                    if (Math.Abs(nextStep - value.ValueStep) >= changeEpsilon)
                    {
                        value.ValueStep = nextStep;
                        var slider = EnsureStatusBar(value);
                        if (slider.ValueInitialized)
                            runtime.SetSliderValue(value, slider.Value, false);
                    }
                    return 0;
                }
            case "GetValueStep":
                lua_pushnumber(state, value.ValueStep);
                return 1;
            case "SetStepsPerPage":
                {
                    const string usage = "Usage: self:SetStepsPerPage(stepsPerPage)";
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(state, usage);
                    var requestedSteps = lua_tonumber(state, 2);
                    if (!double.IsFinite(requestedSteps) ||
                        requestedSteps < int.MinValue ||
                        requestedSteps > int.MaxValue)
                    {
                        return luaL_error(state, usage);
                    }
                    value.StepsPerPage = unchecked((sbyte)(int)Math.Truncate(requestedSteps));
                    return 0;
                }
            case "GetStepsPerPage":
                lua_pushnumber(state, value.StepsPerPage);
                return 1;
            case "IsDraggingThumb":
                lua_pushboolean(
                    state,
                    value.SliderDraggingThumb &&
                    value.ThumbTextureId is { } thumbId &&
                    runtime.Ui.Find(thumbId) is not null ? 1 : 0);
                return 1;
            case "SetMinMaxValues":
                {
                    var statusBar = EnsureStatusBar(value);
                    var isSlider = value.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase);
                    var usage = isSlider
                        ? "Usage: self:SetMinMaxValues(minValue, maxValue)"
                        : "Usage: self:SetMinMaxValues(minValue, maxValue [, interpolation])";
                    if (lua_isnumber(state, 2) == 0 || lua_isnumber(state, 3) == 0)
                        return luaL_error(state, usage);
                    var minimum = (float)lua_tonumber(state, 2);
                    var maximum = (float)lua_tonumber(state, 3);
                    if (isSlider && minimum > maximum)
                    {
                        return luaL_error(state, "max must be >= min");
                    }
                    var interpolation = 0;
                    if (!isSlider &&
                        !TryReadOptionalStatusBarInterpolation(
                            state,
                            4,
                            out interpolation))
                    {
                        return luaL_error(state, usage);
                    }
                    if (!isSlider)
                    {
                        minimum = Math.Min(minimum, maximum);
                        const float maximumMagnitude = 1.0e18f;
                        if (!float.IsFinite(minimum) ||
                            !float.IsFinite(maximum) ||
                            Math.Abs(minimum) > maximumMagnitude ||
                            Math.Abs(maximum) > maximumMagnitude)
                        {
                            return luaL_error(state, "Min or Max out of range");
                        }
                        if (maximum - minimum > maximumMagnitude)
                        {
                            return luaL_error(state, "Min and Max too far apart");
                        }
                    }
                    const float changeEpsilon = 0.00000023841858f;
                    var rangeChanged =
                        isSlider
                            ? !statusBar.RangeInitialized ||
                              Math.Abs(minimum - statusBar.Minimum) >= changeEpsilon ||
                              Math.Abs(maximum - statusBar.Maximum) >= changeEpsilon
                            : minimum != (float)statusBar.Minimum ||
                              maximum != (float)statusBar.Maximum;
                    if (!rangeChanged)
                        return 0;
                    var previous = statusBar.Value;
                    var valueWasInitialized = statusBar.ValueInitialized;
                    if (!isSlider && !statusBar.RangeInitialized)
                    {
                        statusBar.TimerDuration = null;
                        statusBar.Value = 0;
                        statusBar.ValueInitialized = false;
                        valueWasInitialized = false;
                    }
                    statusBar.Minimum = minimum;
                    statusBar.Maximum = maximum;
                    statusBar.RangeInitialized = true;
                    runtime.InvokeScript(value, "OnMinMaxChanged", minimum, maximum);
                    if (isSlider)
                    {
                        if (statusBar.ValueInitialized)
                            runtime.SetSliderValue(value, previous, false);
                    }
                    else if (valueWasInitialized)
                    {
                        SetNativeStatusBarValue(
                            runtime,
                            value,
                            statusBar,
                            previous,
                            interpolation);
                    }
                    return 0;
                }
            case "GetMinMaxValues":
                {
                    var statusBar = EnsureStatusBar(value);
                    lua_pushnumber(state, statusBar.RangeInitialized ? statusBar.Minimum : 0);
                    lua_pushnumber(state, statusBar.RangeInitialized ? statusBar.Maximum : 0);
                    return 2;
                }
            case "SetValue":
                {
                    if (value.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase))
                    {
                        if (lua_isnumber(state, 2) == 0)
                            return luaL_error(
                                state,
                                "Usage: self:SetValue(value [, treatAsMouseEvent])");
                        runtime.SetSliderValue(
                            value,
                            (float)lua_tonumber(state, 2),
                            OptionalBoolean(state, 3, false));
                        return 0;
                    }

                    var statusBar = EnsureStatusBar(value);
                    const string usage = "Usage: self:SetValue(value [, interpolation])";
                    if (lua_isnumber(state, 2) == 0 ||
                        !TryReadOptionalStatusBarInterpolation(
                            state,
                            3,
                            out var interpolation))
                    {
                        return luaL_error(state, usage);
                    }
                    SetNativeStatusBarValue(
                        runtime,
                        value,
                        statusBar,
                        (float)lua_tonumber(state, 2),
                        interpolation);
                    return 0;
                }
            case "GetValue":
                {
                    var statusBar = EnsureStatusBar(value);
                    lua_pushnumber(
                        state,
                        value.ObjectType.Equals("Slider", StringComparison.OrdinalIgnoreCase)
                            ? statusBar.Value
                            : !statusBar.RangeInitialized
                                ? 0
                                : statusBar.ValueInitialized
                                    ? statusBar.Value
                                    : statusBar.Minimum);
                    return 1;
                }
            case "GetInterpolatedValue":
                {
                    var statusBar = EnsureStatusBar(value);
                    var interpolatedValue =
                        statusBar.InterpolationActive && statusBar.RangeInitialized
                            ? statusBar.Minimum +
                              statusBar.DisplayNormalizedValue *
                              (statusBar.Maximum - statusBar.Minimum)
                            : !statusBar.RangeInitialized
                                ? 0
                                : statusBar.ValueInitialized
                                    ? statusBar.Value
                                    : statusBar.Minimum;
                    lua_pushnumber(state, interpolatedValue);
                    return 1;
                }
            case "IsInterpolating":
                lua_pushboolean(
                    state,
                    EnsureStatusBar(value).InterpolationActive ? 1 : 0);
                return 1;
            case "SetToTargetValue":
                {
                    var statusBar = EnsureStatusBar(value);
                    if (statusBar.InterpolationActive)
                    {
                        statusBar.InterpolationActive = false;
                        statusBar.DisplayNormalizedValue =
                            runtime.StatusBarTargetNormalized(statusBar);
                        runtime.Ui.InvalidateLayout();
                    }
                    return 0;
                }
            case "SetTimerDuration":
                {
                    const string usage =
                        "Usage: self:SetTimerDuration(duration [, interpolation, direction])";
                    if (!TryReadRequiredDurationObject(
                            state,
                            2,
                            out var duration) ||
                        !TryReadOptionalStatusBarInterpolation(
                            state,
                            3,
                            out var interpolation) ||
                        !TryReadOptionalStatusBarTimerDirection(
                            state,
                            4,
                            out var direction))
                    {
                        return luaL_error(state, usage);
                    }

                    var statusBar = EnsureStatusBar(value);
                    statusBar.RangeInitialized = false;
                    statusBar.ValueInitialized = false;
                    statusBar.TimerDuration = duration;
                    statusBar.TimerDirection = direction;
                    statusBar.InterpolationActive = interpolation == 1;
                    if (!statusBar.InterpolationActive)
                        statusBar.DisplayNormalizedValue =
                            runtime.StatusBarTargetNormalized(statusBar);
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "GetTimerDuration":
                {
                    var statusBar = EnsureStatusBar(value);
                    if (statusBar.TimerDuration is not { } duration)
                    {
                        lua_pushnil(state);
                        return 1;
                    }

                    PushDurationObject(state, duration);
                    return 1;
                }
            case "SetOrientation":
                {
                    var orientation = OptionalString(state, 2)?.ToUpperInvariant();
                    if (orientation is not "HORIZONTAL" and not "VERTICAL")
                        return luaL_error(state, "Usage: self:SetOrientation(orientation)");
                    EnsureStatusBar(value).Orientation = orientation;
                    return 0;
                }
            case "GetOrientation":
                lua_pushstring(state, EnsureStatusBar(value).Orientation);
                return 1;
            case "SetReverseFill":
                if (lua_gettop(state) < 2 || lua_isnil(state, 2) != 0)
                    return luaL_error(state, "Usage: self:SetReverseFill(reverseFill)");
                EnsureStatusBar(value).ReverseFill = lua_toboolean(state, 2) != 0;
                runtime.Ui.InvalidateLayout();
                return 0;
            case "GetReverseFill":
                lua_pushboolean(state, EnsureStatusBar(value).ReverseFill ? 1 : 0);
                return 1;
            case "SetRotatesTexture":
                {
                    if (lua_gettop(state) < 2 || lua_isnil(state, 2) != 0)
                        return luaL_error(state, "Usage: self:SetRotatesTexture(rotatesTexture)");
                    var statusBar = EnsureStatusBar(value);
                    if (StatusBarTexture(runtime, statusBar) is not null)
                        statusBar.RotatesTexture = lua_toboolean(state, 2) != 0;
                    return 0;
                }
            case "GetRotatesTexture":
                lua_pushboolean(state, EnsureStatusBar(value).RotatesTexture ? 1 : 0);
                return 1;
            case "SetFillStyle":
                {
                    const string usage = "Usage: self:SetFillStyle(fillStyle)";
                    if (!TryReadRequiredStatusBarFillStyle(state, 2, out var fillStyle))
                        return luaL_error(state, usage);
                    EnsureStatusBar(value).FillStyle = fillStyle;
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "GetFillStyle":
                lua_pushinteger(state, EnsureStatusBar(value).FillStyle);
                return 1;
            case "SetStatusBarColor":
                {
                    var statusBar = EnsureStatusBar(value);
                    const string usage = "Usage: self:SetStatusBarColor(color [, a])";
                    if (!HasRequiredNormalizedColor(state, 2))
                        return luaL_error(state, usage);
                    if (StatusBarTexture(runtime, statusBar)?.Texture is { } statusBarColorTexture)
                        statusBarColorTexture.VertexColor = ReadNormalizedColor(state, 2, 1);
                    return 0;
                }
            case "SetColorFill":
                if (!HasRequiredNormalizedColor(state, 2))
                    return luaL_error(state, "Usage: self:SetColorFill(color [, a])");
                SetStatusBarColorFill(runtime, value);
                return 0;
            case "GetStatusBarColor":
                {
                    var color =
                        StatusBarTexture(runtime, EnsureStatusBar(value))?.Texture?.VertexColor ??
                        Vector4.One;
                    lua_pushnumber(state, color.X);
                    lua_pushnumber(state, color.Y);
                    lua_pushnumber(state, color.Z);
                    lua_pushnumber(state, color.W);
                    return 4;
                }
            case "SetStatusBarDesaturation":
                {
                    var statusBar = EnsureStatusBar(value);
                    if (lua_isnumber(state, 2) == 0)
                        return luaL_error(
                            state,
                            "Usage: self:SetStatusBarDesaturation(desaturation)");
                    if (StatusBarTexture(runtime, statusBar)?.Texture is { } statusBarDesaturationTexture)
                        statusBarDesaturationTexture.Desaturation =
                            Math.Clamp((float)lua_tonumber(state, 2), 0, 1);
                    return 0;
                }
            case "GetStatusBarDesaturation":
                lua_pushnumber(
                    state,
                    StatusBarTexture(runtime, EnsureStatusBar(value))?.Texture?.Desaturation ?? 0);
                return 1;
            case "SetStatusBarDesaturated":
                if (StatusBarTexture(runtime, EnsureStatusBar(value))?.Texture is { } statusTexture)
                    statusTexture.Desaturation = OptionalBoolean(state, 2, false) ? 1 : 0;
                return 0;
            case "IsStatusBarDesaturated":
                lua_pushboolean(
                    state,
                    StatusBarTexture(runtime, EnsureStatusBar(value))?.Texture?.Desaturation > 0
                        ? 1
                        : 0);
                return 1;
            case "SetHideCountdownNumbers":
                EnsureCooldown(value).HideCountdownNumbers = OptionalBoolean(state, 2, false);
                return 0;
            case "SetCountdownAbbrevThreshold":
                if (!TryReadRequiredCooldownTimeMilliseconds(
                        state,
                        2,
                        out var abbreviationThreshold))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetCountdownAbbrevThreshold(seconds)");
                }
                EnsureCooldown(value).CountdownAbbreviationThresholdMilliseconds =
                    abbreviationThreshold;
                return 0;
            case "SetCountdownFont":
                if (!HasRequiredValue(state, 2) ||
                    !TryReadOptionalString(state, 2, out var countdownFontName))
                {
                    return luaL_error(state, "Usage: self:SetCountdownFont(fontName)");
                }
                var fontCooldown = EnsureCooldown(value);
                fontCooldown.CountdownFontName = countdownFontName;
                _ = EnsureCooldownFontString(runtime, value, fontCooldown);
                ApplyCooldownFont(runtime, fontCooldown);
                return 0;
            case "GetCountdownFontString":
                {
                    var cooldown = EnsureCooldown(value);
                    var fontString = EnsureCooldownFontString(runtime, value, cooldown);
                    ApplyCooldownFont(runtime, cooldown);
                    runtime.PushObject(fontString);
                    return 1;
                }
            case "SetCountdownFormatter":
                {
                    const string usage =
                        "Usage: self:SetCountdownFormatter([formatter])";
                    var formatterType = lua_gettop(state) < 2
                        ? LUA_TNIL
                        : lua_type(state, 2);
                    if (formatterType is not (LUA_TNIL or LUA_TTABLE or LUA_TUSERDATA))
                        return luaL_error(state, usage);
                    var cooldown = EnsureCooldown(value);
                    runtime.ReleaseReference(cooldown.CountdownFormatterReference);
                    cooldown.CountdownFormatterReference =
                        formatterType == LUA_TNIL ? 0 : LuaRuntime.CaptureValue(state, 2);
                    return 0;
                }
            case "GetCountdownFormatter":
                {
                    var reference = EnsureCooldown(value).CountdownFormatterReference;
                    if (reference > 0)
                        lua_rawgeti(state, LUA_REGISTRYINDEX, reference);
                    else
                        lua_pushnil(state);
                    return 1;
                }
            case "SetCountdownMillisecondsThreshold":
                if (!TryReadRequiredCooldownTimeMilliseconds(
                        state,
                        2,
                        out var millisecondsThreshold))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetCountdownMillisecondsThreshold(seconds)");
                }
                EnsureCooldown(value).CountdownMillisecondsThreshold =
                    millisecondsThreshold;
                return 0;
            case "SetMinimumCountdownDuration":
                if (!TryReadRequiredUInt32(state, 2, out var minimumDuration))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetMinimumCountdownDuration(milliseconds)");
                }
                EnsureCooldown(value).MinimumCountdownDurationMilliseconds =
                    unchecked((int)minimumDuration);
                return 0;
            case "SetUseAuraDisplayTime":
                EnsureCooldown(value).UseAuraDisplayTime = OptionalBoolean(state, 2, false);
                return 0;
            case "GetCountdownAbbrevThreshold":
                lua_pushnumber(
                    state,
                    EnsureCooldown(value).CountdownAbbreviationThresholdMilliseconds *
                    0.001);
                return 1;
            case "GetCountdownMillisecondsThreshold":
                lua_pushnumber(
                    state,
                    EnsureCooldown(value).CountdownMillisecondsThreshold * 0.001);
                return 1;
            case "GetMinimumCountdownDuration":
                lua_pushinteger(
                    state,
                    EnsureCooldown(value).MinimumCountdownDurationMilliseconds);
                return 1;
            case "GetUseAuraDisplayTime":
                lua_pushboolean(state, EnsureCooldown(value).UseAuraDisplayTime ? 1 : 0);
                return 1;
            case "GetHideCountdownNumbers":
                lua_pushboolean(state, EnsureCooldown(value).HideCountdownNumbers ? 1 : 0);
                return 1;
            case "GetEdgeScale":
                lua_pushnumber(state, EnsureCooldown(value).EdgeScale);
                return 1;
            case "Clear":
                {
                    if (value.ObjectType.EndsWith("MessageFrame", StringComparison.OrdinalIgnoreCase))
                    {
                        ClearMessageFrame(runtime, value);
                        runtime.Ui.InvalidateLayout();
                        return 0;
                    }
                    var cooldown = EnsureCooldown(value);
                    ClearCooldownState(cooldown);
                    HideCooldownFontString(runtime, cooldown);
                    return 0;
                }
            case "SetCooldown":
                {
                    const string usage =
                        "Usage: self:SetCooldown(start, duration [, modRate])";
                    if (!TryReadRequiredCooldownTimeMilliseconds(
                            state,
                            2,
                            out var startMilliseconds) ||
                        !TryReadRequiredCooldownTimeMilliseconds(
                            state,
                            3,
                            out var durationMilliseconds) ||
                        !TryReadOptionalFloat(state, 4, 1, out var requestedModRate))
                    {
                        return luaL_error(state, usage);
                    }

                    var cooldown = EnsureCooldown(value);
                    SetNativeCooldown(
                        runtime,
                        cooldown,
                        startMilliseconds,
                        durationMilliseconds,
                        (float)requestedModRate,
                        false);
                    return 0;
                }
            case "SetCooldownDuration":
                {
                    const string usage =
                        "Usage: self:SetCooldownDuration(duration [, modRate])";
                    if (!TryReadRequiredCooldownTimeMilliseconds(
                            state,
                            2,
                            out var durationMilliseconds) ||
                        !TryReadOptionalFloat(state, 3, 1, out var requestedModRate))
                    {
                        return luaL_error(state, usage);
                    }
                    if (durationMilliseconds != 0)
                    {
                        SetNativeCooldown(
                            runtime,
                            EnsureCooldown(value),
                            CooldownClockMilliseconds(runtime, false),
                            durationMilliseconds,
                            (float)requestedModRate,
                            false);
                    }
                    return 0;
                }
            case "SetCooldownFromExpirationTime":
                {
                    const string usage =
                        "Usage: self:SetCooldownFromExpirationTime(expirationTime, duration [, modRate])";
                    if (!TryReadRequiredCooldownTimeMilliseconds(
                            state,
                            2,
                            out var expirationMilliseconds) ||
                        !TryReadRequiredCooldownTimeMilliseconds(
                            state,
                            3,
                            out var durationMilliseconds) ||
                        !TryReadOptionalFloat(state, 4, 1, out var requestedModRate))
                    {
                        return luaL_error(state, usage);
                    }
                    SetNativeCooldown(
                        runtime,
                        EnsureCooldown(value),
                        unchecked(expirationMilliseconds - durationMilliseconds),
                        durationMilliseconds,
                        (float)requestedModRate,
                        false);
                    return 0;
                }
            case "SetCooldownUNIX":
                {
                    const string usage =
                        "Usage: self:SetCooldownUNIX(start, duration [, modRate])";
                    if (!TryReadRequiredUInt32(state, 2, out var start) ||
                        !TryReadRequiredUInt32(state, 3, out var duration) ||
                        !TryReadOptionalFloat(state, 4, 1, out var requestedModRate))
                    {
                        return luaL_error(state, usage);
                    }
                    SetNativeCooldown(
                        runtime,
                        EnsureCooldown(value),
                        unchecked((int)start),
                        unchecked((int)duration),
                        (float)requestedModRate,
                        true);
                    return 0;
                }
            case "SetCooldownFromDurationObject":
                {
                    const string usage =
                        "Usage: self:SetCooldownFromDurationObject(duration [, clearIfZero])";
                    if (!TryReadRequiredDurationObject(state, 2, out var duration))
                        return luaL_error(state, usage);
                    var clearIfZero = lua_gettop(state) < 3 ||
                                      lua_isnil(state, 3) != 0 ||
                                      lua_toboolean(state, 3) != 0;
                    var cooldown = EnsureCooldown(value);
                    var durationMilliseconds =
                        ConvertDurationSecondsToMilliseconds(duration.Duration);
                    if (clearIfZero && durationMilliseconds == 0)
                    {
                        ClearCooldownState(cooldown);
                    }
                    else
                    {
                        SetNativeCooldown(
                            runtime,
                            cooldown,
                            ConvertDurationSecondsToMilliseconds(duration.StartTime),
                            durationMilliseconds,
                            (float)duration.ModRate,
                            false);
                    }
                    return 0;
                }
            case "GetCooldownTimes":
                {
                    var cooldown = EnsureCooldown(value);
                    lua_pushinteger(state, cooldown.StartTimeMilliseconds);
                    lua_pushinteger(state, NativeCooldownReportedDuration(cooldown));
                    return 2;
                }
            case "GetCooldownDuration":
                lua_pushinteger(
                    state,
                    NativeCooldownReportedDuration(EnsureCooldown(value)));
                return 1;
            case "GetCooldownDisplayDuration":
                lua_pushinteger(
                    state,
                    EnsureCooldown(value).DisplayDurationMilliseconds);
                return 1;
            case "SetSwipeColor":
                if (!TryReadRequiredNormalizedColor(
                        state,
                        2,
                        out var swipeColor))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetSwipeColor(color [, a])");
                }
                EnsureCooldown(value).SwipeColor = swipeColor;
                return 0;
            case "SetEdgeColor":
                if (!TryReadRequiredNormalizedColor(
                        state,
                        2,
                        out var edgeColor))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetEdgeColor(color [, a])");
                }
                EnsureCooldown(value).EdgeColor = edgeColor;
                return 0;
            case "SetSwipeTexture":
                return SetCooldownTexture(
                    state,
                    EnsureCooldown(value),
                    CooldownTexturePart.Swipe);
            case "SetEdgeTexture":
                return SetCooldownTexture(
                    state,
                    EnsureCooldown(value),
                    CooldownTexturePart.Edge);
            case "SetBlingTexture":
                return SetCooldownTexture(
                    state,
                    EnsureCooldown(value),
                    CooldownTexturePart.Bling);
            case "SetEdgeScale":
                if (!TryReadRequiredFloat(state, 2, out var edgeScale))
                    return luaL_error(state, "Usage: self:SetEdgeScale(scale)");
                EnsureCooldown(value).EdgeScale =
                    MathF.Max((float)edgeScale, 0.001f);
                return 0;
            case "SetUseCircularEdge":
                EnsureCooldown(value).EdgeScale =
                    OptionalBoolean(state, 2, false) ? 1 : MathF.Sqrt(2);
                return 0;
            case "SetTexCoordRange":
                {
                    const string usage = "Usage: self:SetTexCoordRange(low, high)";
                    if (!TryReadRequiredVector2Table(state, 2, out var low) ||
                        !TryReadRequiredVector2Table(state, 3, out var high))
                    {
                        return luaL_error(state, usage);
                    }
                    if (low.X is >= 0 and <= 1 &&
                        low.Y is >= 0 and <= 1 &&
                        high.X is >= 0 and <= 1 &&
                        high.Y is >= 0 and <= 1 &&
                        low.X <= high.X &&
                        low.Y <= high.Y)
                    {
                        var cooldown = EnsureCooldown(value);
                        cooldown.TextureCoordinateLow = low;
                        cooldown.TextureCoordinateHigh = high;
                    }
                    return 0;
                }
            case "SetDrawSwipe":
                EnsureCooldown(value).DrawSwipe = OptionalBoolean(state, 2, false);
                return 0;
            case "GetDrawSwipe":
                lua_pushboolean(state, EnsureCooldown(value).DrawSwipe ? 1 : 0);
                return 1;
            case "SetDrawEdge":
                EnsureCooldown(value).DrawEdge = OptionalBoolean(state, 2, false);
                return 0;
            case "GetDrawEdge":
                lua_pushboolean(state, EnsureCooldown(value).DrawEdge ? 1 : 0);
                return 1;
            case "SetDrawBling":
                EnsureCooldown(value).DrawBling = OptionalBoolean(state, 2, false);
                return 0;
            case "GetDrawBling":
                lua_pushboolean(state, EnsureCooldown(value).DrawBling ? 1 : 0);
                return 1;
            case "SetReverse":
                EnsureCooldown(value).Reverse = OptionalBoolean(state, 2, false);
                return 0;
            case "GetReverse":
                lua_pushboolean(state, EnsureCooldown(value).Reverse ? 1 : 0);
                return 1;
            case "SetStatusBarTexture":
                return SetStatusBarTexture(runtime, value);
            case "GetStatusBarTexture":
                {
                    var statusBarTextureId = EnsureStatusBar(value).TextureId;
                    runtime.PushObject(
                        statusBarTextureId is { } statusBarTextureObjectId
                            ? runtime.Ui.Find(statusBarTextureObjectId)
                            : null);
                    return 1;
                }
            case "GetThumbTexture":
                runtime.PushObject(
                    value.ThumbTextureId is { } thumbTextureId
                        ? runtime.Ui.Find(thumbTextureId)
                        : null);
                return 1;
            case "SetThumbTexture":
                {
                    if (lua_gettop(state) < 2)
                        return luaL_error(state, "Usage: self:SetThumbTexture(asset)");
                    const string usage = "Usage: self:SetThumbTexture(asset)";
                    var argumentType = lua_type(state, 2);
                    var suppliedTexture = GetObject(runtime, 2);
                    if (suppliedTexture is not null &&
                        suppliedTexture.Texture is null)
                    {
                        return luaL_error(state, usage);
                    }
                    if (suppliedTexture is null &&
                        argumentType is not (LUA_TNIL or LUA_TNUMBER or LUA_TSTRING))
                    {
                        return luaL_error(state, usage);
                    }

                    var previous = value.ThumbTextureId is { } previousThumbId
                        ? runtime.Ui.Find(previousThumbId)
                        : null;
                    if (suppliedTexture is not null &&
                        suppliedTexture.Id == previous?.Id)
                    {
                        return 0;
                    }

                    var texture = suppliedTexture;
                    if (texture is null)
                    {
                        texture = previous ??
                                  CreateObject(
                                      runtime,
                                      "Texture",
                                      null,
                                      value,
                                      "OVERLAY");
                        ClearTextureAsset(EnsureTexture(texture));
                        if (argumentType == LUA_TNUMBER)
                        {
                            if (!TryReadRequiredUInt32(state, 2, out var fileDataId))
                                return luaL_error(state, usage);
                            EnsureTexture(texture).FileDataId = fileDataId;
                        }
                        else if (OptionalString(state, 2) is { } asset)
                        {
                            EnsureTexture(texture).Asset = asset;
                        }
                    }

                    if (previous is not null && previous.Id != texture.Id)
                        previous.Shown = false;
                    if (texture.ParentId != value.Id)
                        runtime.Ui.Reparent(texture, value.Id);
                    texture.DrawLayer = "OVERLAY";
                    texture.SubLevel = 0;
                    texture.Shown = true;
                    value.ThumbTextureId = texture.Id;
                    runtime.Ui.InvalidateLayout();
                    return 0;
                }
            case "SetStartPoint":
                if (TryReadLineAnchor(runtime, value, 2, out var startPoint))
                {
                    EnsureLine(value).Start = startPoint;
                    runtime.Ui.InvalidateLayout();
                }
                return 0;
            case "SetEndPoint":
                if (TryReadLineAnchor(runtime, value, 2, out var endPoint))
                {
                    EnsureLine(value).End = endPoint;
                    runtime.Ui.InvalidateLayout();
                }
                return 0;
            case "GetStartPoint":
                return PushLineAnchor(runtime, EnsureLine(value).Start);
            case "GetEndPoint":
                return PushLineAnchor(runtime, EnsureLine(value).End);
            case "GetThickness":
                lua_pushnumber(state, EnsureLine(value).Thickness);
                return 1;
            case "SetThickness":
                if (lua_isnumber(state, 2) == 0)
                    return luaL_error(state, "Usage: self:SetThickness(thickness)");
                EnsureLine(value).Thickness = (float)lua_tonumber(state, 2);
                return 0;
            case "GetHitRectThickness":
                lua_pushnumber(state, EnsureLine(value).HitRectThickness);
                return 1;
            case "SetHitRectThickness":
                if (lua_isnumber(state, 2) == 0)
                    return luaL_error(state, "Usage: self:SetHitRectThickness(thickness)");
                EnsureLine(value).HitRectThickness = (float)lua_tonumber(state, 2);
                return 0;
            case "SetCameraPosition":
                {
                    if (!TryReadRequiredVector3(state, 2, out var cameraPosition))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetCameraPosition(position)");
                    }
                    if (value.ObjectType.Equals(
                            "ModelScene",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        EnsureModelScene(value).CameraPosition = cameraPosition;
                    }
                    else
                    {
                        if (value.ObjectType.Equals(
                                "CinematicModel",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            value.ModelCameraPosition = cameraPosition;
                        }
                        else
                        {
                            SetSimpleModelCameraPosition(value, cameraPosition);
                        }
                    }
                    return 0;
                }
            case "GetCameraPosition":
                {
                    var cameraPosition = value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase)
                        ? EnsureModelScene(value).CameraPosition
                        : value.ModelCameraPosition;
                    lua_pushnumber(state, cameraPosition.X);
                    lua_pushnumber(state, cameraPosition.Y);
                    lua_pushnumber(state, cameraPosition.Z);
                    return 3;
                }
            case "SetCameraTarget":
                {
                    if (!TryReadRequiredVector3(state, 2, out var cameraTarget))
                        return luaL_error(state, "Usage: self:SetCameraTarget(position)");
                    if (value.ObjectType.Equals(
                            "CinematicModel",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        value.ModelCameraTarget = cameraTarget;
                    }
                    else
                    {
                        SetSimpleModelCameraTarget(value, cameraTarget);
                    }
                    return 0;
                }
            case "GetCameraTarget":
                lua_pushnumber(state, value.ModelCameraTarget.X);
                lua_pushnumber(state, value.ModelCameraTarget.Y);
                lua_pushnumber(state, value.ModelCameraTarget.Z);
                return 3;
            case "SetCameraDistance":
                {
                    if (!TryReadRequiredFloat(state, 2, out var cameraDistance))
                        return luaL_error(state, "Usage: self:SetCameraDistance(distance)");
                    if (value.ModelHasCustomCamera)
                    {
                        var distance = MathF.Max((float)cameraDistance, 0.1f);
                        var facing = GetSimpleModelCameraFacing(value);
                        value.ModelCameraPosition = new Vector3(
                            value.ModelCameraTarget.X + MathF.Cos(facing) * distance,
                            value.ModelCameraTarget.Y + MathF.Sin(facing) * distance,
                            value.ModelCameraPosition.Z);
                        value.ModelCameraDistance = distance;
                    }
                    return 0;
                }
            case "GetCameraDistance":
                lua_pushnumber(state, value.ModelCameraDistance);
                return 1;
            case "SetCameraFacing":
                {
                    if (!TryReadRequiredFloat(state, 2, out var cameraFacing))
                        return luaL_error(state, "Usage: self:SetCameraFacing(radians)");
                    if (value.ModelHasCustomCamera)
                    {
                        var facing = (float)cameraFacing;
                        value.ModelCameraPosition = new Vector3(
                            value.ModelCameraTarget.X +
                                MathF.Cos(facing) * value.ModelCameraDistance,
                            value.ModelCameraTarget.Y +
                                MathF.Sin(facing) * value.ModelCameraDistance,
                            value.ModelCameraPosition.Z);
                    }
                    return 0;
                }
            case "GetCameraFacing":
                lua_pushnumber(state, GetSimpleModelCameraFacing(value));
                return 1;
            case "SetCameraRoll":
                if (!TryReadRequiredFloat(state, 2, out var cameraRoll))
                    return luaL_error(state, "Usage: self:SetCameraRoll(radians)");
                if (value.ModelHasCustomCamera)
                    value.ModelCameraRoll = (float)cameraRoll;
                return 0;
            case "GetCameraRoll":
                lua_pushnumber(state, value.ModelCameraRoll);
                return 1;
            case "HasCustomCamera":
                lua_pushboolean(state, value.ModelHasCustomCamera ? 1 : 0);
                return 1;
            case "MakeCurrentCameraCustom":
                if (!value.ModelHasCustomCamera)
                {
                    if (!value.ModelHasCurrentCamera)
                    {
                        value.ModelCameraPosition = new Vector3(100, 0, 0);
                        value.ModelCameraTarget = Vector3.Zero;
                        value.ModelCameraRoll = 0;
                        value.ModelHasCurrentCamera = true;
                    }
                    value.ModelHasCustomCamera = true;
                    value.ModelCharacterCameraActive = false;
                    value.ModelSelectedCameraIndex = null;
                    value.ModelCameraDistance = GetSimpleModelCameraDistance(value);
                }
                return 0;
            case "SetCamera":
                if (!TryReadRequiredUInt32(state, 2, out var cameraIndex))
                    return luaL_error(state, "Usage: self:SetCamera(cameraIndex)");
                SelectSimpleModelCamera(value, cameraIndex);
                return 0;
            case "SetCustomCamera":
                if (!TryReadRequiredUInt32(state, 2, out var customCameraIndex))
                    return luaL_error(state, "Usage: self:SetCustomCamera(cameraIndex)");
                if (HasLoadedModel(value) &&
                    customCameraIndex < value.ModelCameras.Count)
                {
                    ApplySimpleModelCameraSnapshot(
                        value,
                        value.ModelCameras[(int)customCameraIndex]);
                    value.ModelHasCurrentCamera = true;
                    value.ModelHasCustomCamera = true;
                    value.ModelCharacterCameraActive = false;
                    value.ModelCameraIndex = null;
                    value.ModelSelectedCameraIndex = null;
                }
                return 0;
            case "SetCameraOrientationByYawPitchRoll":
                {
                    const string usage =
                        "Usage: self:SetCameraOrientationByYawPitchRoll(yaw, pitch, roll)";
                    if (!TryReadRequiredFloat(state, 2, out var yaw) ||
                        !TryReadRequiredFloat(state, 3, out var pitch) ||
                        !TryReadRequiredFloat(state, 4, out var roll))
                    {
                        return luaL_error(state, usage);
                    }
                    EnsureModelScene(value).SetOrientationByYawPitchRoll(
                        (float)yaw,
                        (float)pitch,
                        (float)roll);
                    return 0;
                }
            case "SetCameraOrientationByAxisVectors":
                {
                    const string usage =
                        "Usage: self:SetCameraOrientationByAxisVectors(forward, right, up)";
                    if (!TryReadRequiredVector3(state, 2, out var forward) ||
                        !TryReadRequiredVector3(state, 5, out var right) ||
                        !TryReadRequiredVector3(state, 8, out var up))
                    {
                        return luaL_error(state, usage);
                    }
                    if (!EnsureModelScene(value).TrySetOrientationByAxisVectors(
                            forward,
                            right,
                            up))
                    {
                        runtime.Log.Warn(
                            "ui",
                            "SetCameraOrientationByAxisVectors: Each vector must be orthonormal");
                    }
                    return 0;
                }
            case "SetCameraFieldOfView":
                if (!TryReadRequiredFloat(state, 2, out var fieldOfView))
                    return luaL_error(state, "Usage: self:SetCameraFieldOfView(fov)");
                EnsureModelScene(value).FieldOfView = Math.Clamp(
                    (float)fieldOfView,
                    0,
                    MathF.Tau);
                return 0;
            case "GetCameraFieldOfView":
                lua_pushnumber(state, EnsureModelScene(value).FieldOfView);
                return 1;
            case "SetCameraNearClip":
                if (!TryReadRequiredFloat(state, 2, out var nearClip))
                    return luaL_error(state, "Usage: self:SetCameraNearClip(nearClip)");
                EnsureModelScene(value).NearClip = Math.Max(0, (float)nearClip);
                return 0;
            case "GetCameraNearClip":
                lua_pushnumber(state, EnsureModelScene(value).NearClip);
                return 1;
            case "SetCameraFarClip":
                if (!TryReadRequiredFloat(state, 2, out var farClip))
                    return luaL_error(state, "Usage: self:SetCameraFarClip(farClip)");
                EnsureModelScene(value).FarClip = Math.Max(0, (float)farClip);
                return 0;
            case "GetCameraFarClip":
                lua_pushnumber(state, EnsureModelScene(value).FarClip);
                return 1;
            case "GetCameraForward":
                {
                    var forward = EnsureModelScene(value).Forward;
                    lua_pushnumber(state, forward.X);
                    lua_pushnumber(state, forward.Y);
                    lua_pushnumber(state, forward.Z);
                    return 3;
                }
            case "GetCameraUp":
                {
                    var up = EnsureModelScene(value).Up;
                    lua_pushnumber(state, up.X);
                    lua_pushnumber(state, up.Y);
                    lua_pushnumber(state, up.Z);
                    return 3;
                }
            case "GetCameraRight":
                {
                    var right = EnsureModelScene(value).Right;
                    lua_pushnumber(state, right.X);
                    lua_pushnumber(state, right.Y);
                    lua_pushnumber(state, right.Z);
                    return 3;
                }
            case "Project3DPointTo2D":
                {
                    const string usage =
                        "Usage: local x, y, depth = self:Project3DPointTo2D(point)";
                    if (!TryReadRequiredVector3(state, 2, out var point))
                        return luaL_error(state, usage);
                    if (!runtime.Ui.HasResolvedRect(value))
                        return 0;
                    var bounds = runtime.Ui.ResolveBounds(value.Id);
                    var modelCoordinateScale =
                        runtime.Ui.EffectiveScale(value) *
                        value.Scale *
                        runtime.Ui.NormalizedScreenHeight *
                        1.6666666f;
                    var result = EnsureModelScene(value).Project(
                        point,
                        bounds.Width,
                        bounds.Height,
                        modelCoordinateScale);
                    lua_pushnumber(state, result.X);
                    lua_pushnumber(state, result.Y);
                    lua_pushnumber(state, result.Depth);
                    return 3;
                }
            case "SetLightAmbientColor":
                if (!TryReadRequiredNormalizedRgb(state, 2, out var ambientLight))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetLightAmbientColor(color)");
                }
                EnsureModelScene(value).AmbientLight = ambientLight;
                return 0;
            case "GetLightAmbientColor":
                {
                    var color = EnsureModelScene(value).AmbientLight;
                    lua_pushnumber(state, color.X);
                    lua_pushnumber(state, color.Y);
                    lua_pushnumber(state, color.Z);
                    return 3;
                }
            case "SetLightDiffuseColor":
                if (!TryReadRequiredNormalizedRgb(state, 2, out var diffuseLight))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetLightDiffuseColor(color)");
                }
                EnsureModelScene(value).DiffuseLight = diffuseLight;
                return 0;
            case "GetLightDiffuseColor":
                {
                    var color = EnsureModelScene(value).DiffuseLight;
                    lua_pushnumber(state, color.X);
                    lua_pushnumber(state, color.Y);
                    lua_pushnumber(state, color.Z);
                    return 3;
                }
            case "SetLightDirection":
                if (!TryReadRequiredVector3(state, 2, out var lightDirection))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetLightDirection(direction)");
                }
                EnsureModelScene(value).LightDirection = lightDirection;
                return 0;
            case "GetLightDirection":
                {
                    var direction = EnsureModelScene(value).LightDirection;
                    lua_pushnumber(state, direction.X);
                    lua_pushnumber(state, direction.Y);
                    lua_pushnumber(state, direction.Z);
                    return 3;
                }
            case "SetLightPosition":
                if (!TryReadRequiredVector3(state, 2, out var lightPosition))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetLightPosition(position)");
                }
                EnsureModelScene(value).LightPosition = lightPosition;
                return 0;
            case "GetLightPosition":
                {
                    var position = EnsureModelScene(value).LightPosition;
                    lua_pushnumber(state, position.X);
                    lua_pushnumber(state, position.Y);
                    lua_pushnumber(state, position.Z);
                    return 3;
                }
            case "SetLightType":
                if (!TryReadRequiredInt32(state, 2, out var lightType))
                    return luaL_error(state, "Usage: self:SetLightType(lightType)");
                if (lightType is 0 or 1)
                    EnsureModelScene(value).LightType = lightType;
                return 0;
            case "GetLightType":
                lua_pushinteger(state, EnsureModelScene(value).LightType);
                return 1;
            case "SetLightVisible":
                EnsureModelScene(value).LightVisible = lua_gettop(state) < 2 ||
                                                       lua_toboolean(state, 2) != 0;
                return 0;
            case "IsLightVisible":
                lua_pushboolean(state, EnsureModelScene(value).LightVisible ? 1 : 0);
                return 1;
            case "SetFogColor":
                {
                    if (value.ObjectType.Equals(
                            "ModelScene",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (!TryReadRequiredNormalizedRgb(state, 2, out var fogColor))
                            return luaL_error(state, "Usage: self:SetFogColor(color)");
                        var scene = EnsureModelScene(value);
                        scene.FogColor = new Vector4(fogColor, 1);
                    }
                    else
                    {
                        if (!TryReadRequiredNormalizedColor(state, 2, out var fogColor))
                        {
                            return luaL_error(
                                state,
                                "Usage: self:SetFogColor(color [, a])");
                        }
                        value.ModelFogColor = fogColor;
                        value.ModelFogEnabled = true;
                    }
                    return 0;
                }
            case "GetFogColor":
                if (value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var fogColor = EnsureModelScene(value).FogColor;
                    lua_pushnumber(state, fogColor.X);
                    lua_pushnumber(state, fogColor.Y);
                    lua_pushnumber(state, fogColor.Z);
                    return 3;
                }
                lua_pushnumber(state, value.ModelFogColor.X);
                lua_pushnumber(state, value.ModelFogColor.Y);
                lua_pushnumber(state, value.ModelFogColor.Z);
                lua_pushnumber(state, value.ModelFogColor.W);
                return 4;
            case "SetFogFar":
                {
                    if (!TryReadRequiredFloat(state, 2, out var fogFar))
                        return luaL_error(state, "Usage: self:SetFogFar(far)");
                    if (value.ObjectType.Equals(
                            "ModelScene",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var scene = EnsureModelScene(value);
                        scene.FogFar = (float)fogFar;
                        scene.FogEnabled = true;
                    }
                    else
                    {
                        value.ModelFogFar = (float)fogFar;
                    }
                    return 0;
                }
            case "GetFogFar":
                lua_pushnumber(
                    state,
                    value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase)
                        ? EnsureModelScene(value).FogFar
                        : value.ModelFogFar);
                return 1;
            case "SetFogNear":
                {
                    if (!TryReadRequiredFloat(state, 2, out var fogNear))
                        return luaL_error(state, "Usage: self:SetFogNear(near)");
                    if (value.ObjectType.Equals(
                            "ModelScene",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        EnsureModelScene(value).FogNear = (float)fogNear;
                    }
                    else
                    {
                        value.ModelFogNear = (float)fogNear;
                    }
                    return 0;
                }
            case "GetFogNear":
                lua_pushnumber(
                    state,
                    value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase)
                        ? EnsureModelScene(value).FogNear
                        : value.ModelFogNear);
                return 1;
            case "ClearFog":
                {
                    if (value.ObjectType.Equals(
                            "ModelScene",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var scene = EnsureModelScene(value);
                        scene.FogNear = 10_000_000;
                        scene.FogFar = 100_000_000;
                        scene.FogEnabled = false;
                    }
                    else
                    {
                        value.ModelFogEnabled = false;
                    }
                    return 0;
                }
            case "AdvanceTime":
                return 0;
            case "GetModelFileID":
                lua_pushinteger(
                    state,
                    value.ModelResourceLoaded ? value.ModelFileDataId ?? 0 : 0);
                return 1;
            case "HasAttachmentPoints":
                lua_pushboolean(
                    state,
                    HasLoadedModel(value) && value.ModelHasAttachmentPoints ? 1 : 0);
                return 1;
            case "ReplaceIconTexture":
                {
                    const string usage = "Usage: self:ReplaceIconTexture(asset)";
                    if (!TryReadRequiredModelAsset(
                            state,
                            2,
                            out var iconFileDataId,
                            out var iconPath))
                    {
                        return luaL_error(state, usage);
                    }
                    if (HasLoadedModel(value))
                    {
                        var resolvedIconFileDataId = ResolveFileAssetId(
                            runtime,
                            iconFileDataId,
                            iconPath);
                        value.ModelIconTextureFileDataId =
                            resolvedIconFileDataId > 0
                                ? resolvedIconFileDataId
                                : null;
                        value.ModelIconTexturePath = null;
                    }
                    return 0;
                }
            case "SetModel":
                {
                    const string usage = "Usage: self:SetModel(asset [, noMip])";
                    if (!TryReadRequiredModelAsset(
                            state,
                            2,
                            out var fileDataId,
                            out var modelPath))
                    {
                        return luaL_error(state, usage);
                    }

                    var noMip = OptionalBoolean(state, 3, false);
                    var resolvedFileDataId = ResolveFileAssetId(
                        runtime,
                        fileDataId,
                        modelPath);

                    if (IsCharacterModelSurface(value))
                        ResetCharacterModelRawResourceState(value);
                    else
                        ResetCharacterModelSourceState(value);
                    if (!IsAvailableModelResource(runtime, resolvedFileDataId))
                    {
                        var invalidAsset = modelPath ??
                                           fileDataId?.ToString() ??
                                           string.Empty;
                        return luaL_error(
                            state,
                            $"Invalid model file: {invalidAsset}");
                    }

                    value.ModelFileDataId = resolvedFileDataId;
                    value.ModelPath = modelPath;
                    value.ModelNoMip = noMip;
                    value.ModelResourceLoaded = true;
                    ApplyModelResourceMetadata(
                        runtime,
                        value,
                        resolvedFileDataId);
                    runtime.InvokeScript(value, "OnModelLoaded");
                    return 0;
                }
            case "SetSequence":
            case "SetSequenceTime":
                {
                    var usage = operation == "SetSequence"
                        ? "Usage: self:SetSequence(sequence)"
                        : "Usage: self:SetSequenceTime(sequence, timeOffset)";
                    if (!TryReadRequiredUInt32(state, 2, out var sequence))
                        return luaL_error(state, usage);

                    var timeOffset = 0;
                    if (operation == "SetSequenceTime" &&
                        !TryReadRequiredInt32(state, 3, out timeOffset))
                    {
                        return luaL_error(state, usage);
                    }
                    if (sequence >= 1858)
                    {
                        return luaL_error(
                            state,
                            "Sequence exceeds valid range of 0 - 1858");
                    }
                    if (HasLoadedModel(value))
                    {
                        value.ModelSequenceId = sequence;
                        value.ModelSequenceTimeOffset = timeOffset;
                        if (TryResolveModelSequence(
                                runtime,
                                value,
                                checked((ushort)sequence),
                                out var resolvedSequence,
                                out var selectedSequenceIndex,
                                out var resolvedSequenceIndex,
                                out var sequenceMetadata))
                        {
                            value.ModelResolvedSequenceId = resolvedSequence;
                            value.ModelSelectedSequenceIndex = selectedSequenceIndex;
                            value.ModelResolvedSequenceIndex = resolvedSequenceIndex;
                            value.ModelResolvedSequenceVariation =
                                sequenceMetadata.VariationIndex;
                            value.ModelResolvedSequenceDurationMilliseconds =
                                sequenceMetadata.DurationMilliseconds;
                            value.ModelSequenceElapsedMilliseconds =
                                timeOffset -
                                (runtime.IsProcessingModelSceneCallbacks ? 0 : 1);
                            value.ModelSequenceInitialElapsedMilliseconds =
                                value.ModelSequenceElapsedMilliseconds;
                            value.ModelSequencePlaybackClockMilliseconds = 0;
                            value.ModelSequencePlaybackSpeed = 1;
                            value.ModelSequenceRepeatCount = 1;
                            value.ModelSequenceLoops =
                                (sequenceMetadata.Flags & 1) == 0;
                            value.ModelSequencePlaying =
                                sequenceMetadata.DurationMilliseconds > 0 &&
                                (value.ModelSequenceLoops ||
                                 value.ModelSequenceElapsedMilliseconds <
                                 sequenceMetadata.DurationMilliseconds);
                            value.ModelSequencePlaybackRevision++;
                            runtime.InvokeScript(value, "OnAnimStarted");
                        }
                    }
                    return 0;
                }
            case "GetShadowEffect":
                lua_pushnumber(
                    state,
                    HasLoadedModel(value) &&
                    value.ModelRenderEffectKind == UiModelRenderEffectKind.Shadow
                        ? value.ModelShadowEffectStrength
                        : 0);
                return 1;
            case "SetShadowEffect":
                if (!TryReadRequiredFloat(state, 2, out var shadowStrength))
                    return luaL_error(state, "Usage: self:SetShadowEffect(strength)");
                if (HasLoadedModel(value))
                {
                    value.ModelShadowEffectStrength = Math.Clamp(
                        (float)shadowStrength,
                        0,
                        1);
                    value.ModelRenderEffectKind = value.ModelShadowEffectStrength > 0
                        ? UiModelRenderEffectKind.Shadow
                        : UiModelRenderEffectKind.None;
                    value.ModelShadowEffectState = null;
                    value.ModelDissolveEffectState = null;
                    value.ModelEdgeGlowEffectState = null;
                    value.ModelGradientMaskEnabled = false;
                }
                return 0;
            case "GetModelAlpha":
                lua_pushnumber(
                    state,
                    HasLoadedModel(value) ? value.ModelAlpha : 1);
                return 1;
            case "SetModelAlpha":
                if (!TryReadRequiredFloat(state, 2, out var modelAlpha))
                    return luaL_error(state, "Usage: self:SetModelAlpha(alpha)");
                if (HasLoadedModel(value))
                    value.ModelAlpha = Math.Clamp((float)modelAlpha, 0, 1);
                return 0;
            case "GetModelScale":
                lua_pushnumber(state, value.ModelScale);
                return 1;
            case "SetModelScale":
                if (!TryReadRequiredFloat(state, 2, out var modelScale))
                    return luaL_error(state, "Usage: self:SetModelScale(scale)");
                value.ModelScale = (float)modelScale;
                return 0;
            case "GetModelDrawLayer":
                lua_pushstring(state, value.ModelDrawLayer);
                lua_pushinteger(state, 0);
                return 2;
            case "SetModelDrawLayer":
                {
                    var modelDrawLayer = OptionalString(state, 2);
                    if (modelDrawLayer is null || !LayerNames.Contains(modelDrawLayer))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:SetModelDrawLayer(layer)");
                    }
                    value.ModelDrawLayer = modelDrawLayer.ToUpperInvariant();
                    return 0;
                }
            case "SetParticlesEnabled":
                if (!TryReadRequiredBoolean(state, 2, out var particlesEnabled))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetParticlesEnabled(enabled)");
                }
                if (HasLoadedModel(value))
                    value.ModelParticlesEnabled = particlesEnabled;
                return 0;
            case "SetGlow":
                if (!TryReadRequiredFloat(state, 2, out _))
                    return luaL_error(state, "Usage: self:SetGlow(glow)");
                return 0;
            case "SetUseGBuffer":
                if (!TryReadRequiredBoolean(state, 2, out var useGBuffer))
                    return luaL_error(state, "Usage: self:SetUseGBuffer(useGBuffer)");
                value.ModelUseGBuffer = useGBuffer;
                return 0;
            case "GetViewTranslation":
                {
                    var translation = value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase)
                        ? EnsureModelScene(value).ViewTranslation
                        : value.ModelViewTranslation;
                    lua_pushnumber(state, translation.X);
                    lua_pushnumber(state, translation.Y);
                    return 2;
                }
            case "SetViewTranslation":
                {
                    if (!TryReadRequiredFloat(state, 2, out var viewTranslationX) ||
                        !TryReadRequiredFloat(state, 3, out var viewTranslationY))
                    {
                        return luaL_error(
                            state,
                            value.ObjectType.Equals(
                                "ModelScene",
                                StringComparison.OrdinalIgnoreCase)
                                ? "Usage: self:SetViewTranslation(translation)"
                                : "Usage: self:SetViewTranslation(x, y)");
                    }
                    var translation = new Vector2(
                        (float)viewTranslationX,
                        (float)viewTranslationY);
                    if (value.ObjectType.Equals(
                            "ModelScene",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        EnsureModelScene(value).ViewTranslation = translation;
                    }
                    else
                    {
                        value.ModelViewTranslation = translation;
                    }
                    return 0;
                }
            case "IsUsingModelCenterToTransform":
                lua_pushboolean(state, value.ModelUseCenterToTransform ? 1 : 0);
                return 1;
            case "UseModelCenterToTransform":
                if (!TryReadRequiredBoolean(state, 2, out var useModelCenter))
                {
                    return luaL_error(
                        state,
                        "Usage: self:UseModelCenterToTransform(useCenter)");
                }
                value.ModelUseCenterToTransform = useModelCenter;
                return 0;
            case "SetTransform":
                {
                    const string usage =
                        "Usage: self:SetTransform([translation, rotation, scale])";
                    if (!TryReadOptionalVector3Table(
                            state,
                            2,
                            Vector3.Zero,
                            out var transformTranslation) ||
                        !TryReadOptionalVector3Table(
                            state,
                            3,
                            Vector3.Zero,
                            out var transformRotation) ||
                        !TryReadOptionalFloat(state, 4, 0, out var transformScale))
                    {
                        return luaL_error(state, usage);
                    }
                    if (HasLoadedModel(value))
                    {
                        value.ModelTransformTranslation = transformTranslation;
                        value.ModelTransformRotation = transformRotation;
                        value.ModelTransformScale = (float)transformScale;
                        value.ModelTransformEnabled = true;
                        if (TryBuildSimpleModelExplicitTransform(
                                transformTranslation,
                                transformRotation,
                                (float)transformScale,
                                out var transformMatrix))
                        {
                            value.ModelTransformMatrix = transformMatrix;
                            value.ModelWorldScale = transformMatrix.M11;
                        }
                    }
                    return 0;
                }
            case "ClearTransform":
                value.ModelTransformEnabled = false;
                return 0;
            case "GetWorldScale":
                lua_pushnumber(
                    state,
                    HasLoadedModel(value) ? value.ModelWorldScale : 1);
                return 1;
            case "TransformCameraSpaceToModelSpace":
                {
                    if (!TryReadRequiredVector3Table(state, 2, out var cameraSpace))
                    {
                        return luaL_error(
                            state,
                            "Usage: local modelPosition = " +
                            "self:TransformCameraSpaceToModelSpace(cameraPosition)");
                    }
                    var factor = GetSimpleModelCoordinateScale(runtime, value);
                    PushVector3Mixin(state, cameraSpace * factor, "Vector3DMixin");
                    return 1;
                }
            case "SetAllowOverlappedModels":
                if (!TryReadRequiredBoolean(state, 2, out var allowOverlappedModels))
                {
                    return luaL_error(
                        state,
                        "Usage: self:SetAllowOverlappedModels(allowOverlappedModels)");
                }
                EnsureModelScene(value).AllowOverlappedModels = allowOverlappedModels;
                return 0;
            case "GetAllowOverlappedModels":
                lua_pushboolean(state, EnsureModelScene(value).AllowOverlappedModels ? 1 : 0);
                return 1;
            case "SetViewInsets":
                {
                    var isModelScene = value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase);
                    var usage = isModelScene
                        ? "Usage: self:SetViewInsets(insets)"
                        : "Usage: self:SetViewInsets(left, right, top, bottom)";
                    if (!TryReadRequiredFloat(state, 2, out var left) ||
                        !TryReadRequiredFloat(state, 3, out var right) ||
                        !TryReadRequiredFloat(state, 4, out var top) ||
                        !TryReadRequiredFloat(state, 5, out var bottom))
                    {
                        return luaL_error(state, usage);
                    }
                    var insets = new UiInsets(
                        (float)left,
                        (float)right,
                        (float)top,
                        (float)bottom);
                    if (isModelScene)
                        EnsureModelScene(value).ViewInsets = insets;
                    else
                        value.ModelViewInsets = insets;
                    return 0;
                }
            case "GetViewInsets":
                {
                    var insets = value.ObjectType.Equals(
                        "ModelScene",
                        StringComparison.OrdinalIgnoreCase)
                        ? EnsureModelScene(value).ViewInsets
                        : value.ModelViewInsets;
                    lua_pushnumber(state, insets.Left);
                    lua_pushnumber(state, insets.Right);
                    lua_pushnumber(state, insets.Top);
                    lua_pushnumber(state, insets.Bottom);
                    return 4;
                }
            case "SetModelByFileID":
            case "SetModelByPath":
                {
                    var usage = operation == "SetModelByFileID"
                        ? "Usage: local success = self:SetModelByFileID(asset [, useMips])"
                        : "Usage: local success = self:SetModelByPath(asset [, useMips])";
                    if (!TryReadRequiredModelAsset(
                            state,
                            2,
                            out var fileDataId,
                            out var modelPath))
                    {
                        return luaL_error(state, usage);
                    }

                    var resolvedFileDataId = ResolveFileAssetId(
                        runtime,
                        fileDataId,
                        modelPath);
                    var useMips = OptionalBoolean(state, 3, false);
                    var success = IsAvailableModelResource(
                        runtime,
                        resolvedFileDataId);
                    if (success)
                    {
                        value.ModelFileDataId = resolvedFileDataId;
                        value.ModelCreatureDisplayId = null;
                        value.ModelPath = modelPath;
                        value.ModelUnitToken = null;
                        value.ModelGuildTabardInfo = null;
                        value.ModelNoMip = !useMips;
                        value.ModelResourceLoaded = true;
                        ApplyModelResourceMetadata(
                            runtime,
                            value,
                            resolvedFileDataId);
                        value.ModelPaused = false;
                        value.ModelGlobalPaused = false;
                        runtime.InvokeScript(value, "OnModelLoaded");
                        runtime.InvokeScript(value, "OnModelLoading");
                    }
                    lua_pushboolean(state, success ? 1 : 0);
                    return 1;
                }
            case "SetModelByCreatureDisplayID":
                {
                    const string usage =
                        "Usage: local success = self:SetModelByCreatureDisplayID(" +
                        "creatureDisplayID [, useActivePlayerCustomizations])";
                    if (!TryReadRequiredInt32(state, 2, out var creatureDisplayId))
                        return luaL_error(state, usage);
                    var success = creatureDisplayId > 0;
                    if (success)
                    {
                        value.ModelCreatureDisplayId = (uint)creatureDisplayId;
                        value.ModelFileDataId = null;
                        value.ModelPath = null;
                        value.ModelUnitToken = null;
                        value.ModelGuildTabardInfo = null;
                        value.ModelResourceLoaded = true;
                        value.ModelPaused = false;
                        value.ModelGlobalPaused = false;
                        value.ModelActiveBoundingBoxMinimum = null;
                        value.ModelActiveBoundingBoxMaximum = null;
                        value.ModelAnimationBoundingBoxMinimum = null;
                        value.ModelAnimationBoundingBoxMaximum = null;
                        value.ModelCollisionBoundingBoxMinimum = null;
                        value.ModelCollisionBoundingBoxMaximum = null;
                        value.ModelMaxBoundingBoxMinimum = null;
                        value.ModelMaxBoundingBoxMaximum = null;
                        value.ModelCenter = Vector3.Zero;
                        runtime.InvokeScript(value, "OnModelLoaded");
                    }
                    lua_pushboolean(state, success ? 1 : 0);
                    return 1;
                }
            case "SetModelByUnit":
                {
                    const string usage =
                        "Usage: local success = self:SetModelByUnit(unit " +
                        "[, sheatheWeapons, autoDress, hideWeapons, " +
                        "usePlayerNativeForm, holdBowString, customRaceID])";
                    var unitToken = OptionalString(state, 2);
                    if (unitToken is null)
                        return luaL_error(state, usage);
                    var success = runtime.Units.Find(unitToken) is not null;
                    if (success)
                    {
                        value.ModelFileDataId = null;
                        value.ModelCreatureDisplayId = null;
                        value.ModelPath = null;
                        value.ModelUnitToken = unitToken;
                        value.ModelGuildTabardInfo = ResolveModelGuildTabardInfo(
                            runtime,
                            unitToken);
                        value.ModelResourceLoaded = true;
                        value.ModelAutoDress = lua_gettop(state) < 4
                            ? true
                            : lua_toboolean(state, 4) != 0;
                        value.ModelPaused = false;
                        value.ModelGlobalPaused = false;
                        value.ModelActiveBoundingBoxMinimum = null;
                        value.ModelActiveBoundingBoxMaximum = null;
                        value.ModelAnimationBoundingBoxMinimum = null;
                        value.ModelAnimationBoundingBoxMaximum = null;
                        value.ModelCollisionBoundingBoxMinimum = null;
                        value.ModelCollisionBoundingBoxMaximum = null;
                        value.ModelMaxBoundingBoxMinimum = null;
                        value.ModelMaxBoundingBoxMaximum = null;
                        value.ModelCenter = Vector3.Zero;
                        runtime.InvokeScript(value, "OnModelLoaded");
                    }
                    lua_pushboolean(state, success ? 1 : 0);
                    return 1;
                }
            case "SetPosition":
                if (!TryReadRequiredVector3(state, 2, out var modelPosition))
                    return luaL_error(state, "Usage: self:SetPosition(position)");
                value.ModelPosition = modelPosition;
                return 0;
            case "GetPosition":
                lua_pushnumber(state, value.ModelPosition.X);
                lua_pushnumber(state, value.ModelPosition.Y);
                lua_pushnumber(state, value.ModelPosition.Z);
                return 3;
            case "SetYaw":
                if (!TryReadRequiredFloat(state, 2, out var modelYaw))
                    return luaL_error(state, "Usage: self:SetYaw(yaw)");
                value.ModelYaw = (float)modelYaw;
                return 0;
            case "SetFacing":
                if (!TryReadRequiredFloat(state, 2, out var modelFacing))
                    return luaL_error(state, "Usage: self:SetFacing(facing)");
                value.ModelYaw = (float)modelFacing;
                return 0;
            case "GetYaw":
                lua_pushnumber(state, value.ModelYaw);
                return 1;
            case "GetFacing":
                lua_pushnumber(state, value.ModelYaw);
                return 1;
            case "SetPitch":
                if (!TryReadRequiredFloat(state, 2, out var modelPitch))
                    return luaL_error(state, "Usage: self:SetPitch(pitch)");
                value.ModelPitch = (float)modelPitch;
                return 0;
            case "GetPitch":
                lua_pushnumber(state, value.ModelPitch);
                return 1;
            case "SetRoll":
                if (!TryReadRequiredFloat(state, 2, out var modelRoll))
                    return luaL_error(state, "Usage: self:SetRoll(roll)");
                value.ModelRoll = (float)modelRoll;
                return 0;
            case "GetRoll":
                lua_pushnumber(state, value.ModelRoll);
                return 1;
            case "GetModelPath":
                lua_pushstring(state, value.ModelPath ?? string.Empty);
                return 1;
            case "GetActiveBoundingBox":
                {
                    if (value.ModelActiveBoundingBoxMinimum is { } minimum &&
                        value.ModelActiveBoundingBoxMaximum is { } maximum)
                    {
                        lua_pushnumber(state, minimum.X);
                        lua_pushnumber(state, minimum.Y);
                        lua_pushnumber(state, minimum.Z);
                        lua_pushnumber(state, maximum.X);
                        lua_pushnumber(state, maximum.Y);
                        lua_pushnumber(state, maximum.Z);
                    }
                    else
                    {
                        for (var coordinate = 0; coordinate < 6; coordinate++)
                            lua_pushnil(state);
                    }
                    return 6;
                }
            case "GetMaxBoundingBox":
                {
                    if (value.ModelMaxBoundingBoxMinimum is { } minimum &&
                        value.ModelMaxBoundingBoxMaximum is { } maximum)
                    {
                        lua_pushnumber(state, minimum.X);
                        lua_pushnumber(state, minimum.Y);
                        lua_pushnumber(state, minimum.Z);
                        lua_pushnumber(state, maximum.X);
                        lua_pushnumber(state, maximum.Y);
                        lua_pushnumber(state, maximum.Z);
                    }
                    else
                    {
                        for (var coordinate = 0; coordinate < 6; coordinate++)
                            lua_pushnil(state);
                    }
                    return 6;
                }
            case "ClearModel":
                ClearModel(runtime, value);
                return 0;
            case "SetModelByHyperlink":
                {
                    const string usage =
                        "Usage: local success = self:SetModelByHyperlink(link)";
                    if (!HasRequiredValue(state, 2) ||
                        lua_isstring(state, 2) == 0 ||
                        lua_istable(state, 2) != 0)
                    {
                        return luaL_error(state, usage);
                    }
                    _ = lua_tostring(state, 2);
                    lua_pushboolean(state, 0);
                    return 1;
                }
            case "SetOwner":
                return SetTooltipOwner(runtime, value);
            case "GetOwner":
                {
                    var owner = value.Attributes.TryGetValue("TooltipOwnerId", out var ownerId) &&
                                ownerId is int storedOwnerId
                        ? runtime.Ui.Find(storedOwnerId)
                        : null;
                    runtime.PushObject(owner);
                    return 1;
                }
            case "IsOwned":
                {
                    var owner = GetObject(runtime, 2);
                    var isOwned =
                        owner is not null &&
                        value.Attributes.TryGetValue("TooltipOwnerId", out var ownerId) &&
                        ownerId is int storedOwnerId &&
                        storedOwnerId == owner.Id;
                    lua_pushboolean(state, isOwned ? 1 : 0);
                    return 1;
                }
            case "NumLines":
                lua_pushinteger(state, EnsureTooltip(value).Lines.Count);
                return 1;
            case "AddFontStrings":
                {
                    var left = GetObject(runtime, 2);
                    var right = GetObject(runtime, 3);
                    if (left?.Font is null || right?.Font is null)
                        return luaL_error(state, "Usage: self:AddFontStrings(leftFontString, rightFontString)");
                    if (EnsureTooltip(value).Lines.Count >= 1000)
                        return 0;
                    EnsureTooltip(value).Lines.Add(new UiTooltipLineState
                    {
                        LeftId = left.Id,
                        RightId = right.Id
                    });
                    LayoutTooltip(runtime, value);
                    return 0;
                }
            case "SetCustomWordWrapMinWidth":
                {
                    var tooltip = EnsureTooltip(value);
                    tooltip.CustomWordWrapMinWidth = lua_isnumber(state, 2) != 0
                        ? (float)lua_tonumber(state, 2)
                        : null;
                    LayoutTooltip(runtime, value);
                    return 0;
                }
            case "SetCustomLineSpacing":
                {
                    var tooltip = EnsureTooltip(value);
                    tooltip.CustomLineSpacing = lua_isnumber(state, 2) != 0
                        ? (float)lua_tonumber(state, 2)
                        : null;
                    LayoutTooltip(runtime, value);
                    return 0;
                }
            case "GetCustomLineSpacing":
                if (EnsureTooltip(value).CustomLineSpacing is { } customLineSpacing)
                {
                    lua_pushnumber(state, customLineSpacing);
                    return 1;
                }
                return 0;
            case "SetShrinkToFitWrapped":
                EnsureTooltip(value).ShrinkToFitWrapped =
                    lua_toboolean(state, 2) != 0;
                LayoutTooltip(runtime, value);
                return 0;
            case "SetAllowShowWithNoLines":
                EnsureTooltip(value).AllowShowWithNoLines =
                    lua_toboolean(state, 2) != 0;
                return 0;
            case "GetAnchorType":
                lua_pushstring(
                    state,
                    value.Attributes.TryGetValue("TooltipAnchor", out var anchorValue) &&
                    anchorValue is string anchorType
                        ? anchorType
                        : "ANCHOR_NONE");
                return 1;
            case "SetAnchorType":
                return SetTooltipAnchorType(runtime, value);
            case "AddLine":
                {
                    var line = OptionalString(state, 2) ?? string.Empty;
                    if (!TryReadTooltipColor(state, 3, out var color))
                        return luaL_error(state, "Usage: self:AddLine(text [, r, g, b, wrap, padding])");
                    AddTooltipLine(
                        runtime,
                        value,
                        line,
                        null,
                        color,
                        null,
                        OptionalBoolean(state, 6, false),
                        lua_isnumber(state, 7) != 0 ? (float)lua_tonumber(state, 7) : 0);
                    return 0;
                }
            case "AddDoubleLine":
                {
                    if (!TryReadTooltipColor(state, 4, out var leftColor) ||
                        !TryReadTooltipColor(state, 7, out var rightColor))
                    {
                        return luaL_error(
                            state,
                            "Usage: self:AddDoubleLine(leftText, rightText [, lr, lg, lb, rr, rg, rb, wrap, padding])");
                    }
                    var rightText = OptionalString(state, 3) ?? string.Empty;
                    AddTooltipLine(
                        runtime,
                        value,
                        OptionalString(state, 2) ?? string.Empty,
                        rightText,
                        leftColor,
                        rightColor,
                        rightText.Length == 0 && OptionalBoolean(state, 10, false),
                        lua_isnumber(state, 11) != 0 ? (float)lua_tonumber(state, 11) : 0);
                    return 0;
                }
            case "AddTexture":
                return AddTooltipTexture(runtime, value, atlas: false);
            case "AddAtlas":
                return AddTooltipTexture(runtime, value, atlas: true);
            case "AppendText":
                {
                    const string usage = "Usage: self:AppendText(\"text\")";
                    if (!TryReadRequiredString(state, 2, out var appendedText))
                        return luaL_error(state, usage);
                    var lines = EnsureTooltip(value).Lines;
                    if (lines.Count == 0)
                        return 0;
                    var left = runtime.Ui.Find(lines[^1].LeftId);
                    if (left?.Font is null)
                        return 0;
                    var nextText = string.Concat(left.Font.Text, appendedText);
                    left.TextValue = nextText;
                    left.Font.Text = nextText;
                    value.TextValue = string.Join(
                        '\n',
                        lines.Select(line => runtime.Ui.Find(line.LeftId)?.Font?.Text ?? string.Empty));
                    LayoutTooltip(runtime, value);
                    return 0;
                }
            case "FadeOut":
                {
                    var anchor = value.Attributes.TryGetValue(
                        "TooltipAnchor",
                        out var fadeAnchorValue)
                        ? fadeAnchorValue as string
                        : null;
                    if (anchor is
                        "ANCHOR_CURSOR" or
                        "ANCHOR_CURSOR_LEFT" or
                        "ANCHOR_CURSOR_RIGHT")
                    {
                        SetShown(runtime, value, false);
                    }
                    else
                    {
                        EnsureTooltip(value).FadeRemaining = 2;
                    }
                    return 0;
                }
            case "CopyTooltip":
                runtime.LastCopiedTooltipText = BuildTooltipCopyText(runtime, value);
                return 0;
            case "SetFrameStack":
                return 0;
            case "SetObjectTooltipPosition":
                return 0;
            case "ClearLines":
                ClearTooltip(runtime, value);
                return 0;
            case "GetLeftLine":
                {
                    if (!TryReadRequiredOneBasedIndex(state, 2, out var lineIndex))
                        return luaL_error(
                            state,
                            "Usage: local leftFontString = self:GetLeftLine(line)");
                    return PushTooltipLine(runtime, value, lineIndex, right: false);
                }
            case "GetRightLine":
                {
                    if (!TryReadRequiredOneBasedIndex(state, 2, out var lineIndex))
                        return luaL_error(
                            state,
                            "Usage: local rightFontString = self:GetRightLine(line)");
                    return PushTooltipLine(runtime, value, lineIndex, right: true);
                }
            default:
                runtime.Log.Write(EmulatorLogLevel.Trace, "api", $"No-op {value.ObjectType}:{operation}.");
                return 0;
        }
    }

    private static int CreateFrame(LuaRuntime runtime, bool forbidden = false)
    {
        var state = runtime.State;
        const string usage =
            "Usage: CreateFrame(\"frameType\" [, \"name\"] [, parent] [, \"template\"] [, id])";
        if (!TryReadRequiredString(state, 1, out var requestedObjectType))
            return luaL_error(state, usage);

        var name = OptionalString(state, 2);
        var templates = OptionalString(state, 4);
        UiObject? parent = null;
        if (lua_gettop(state) >= 3 && lua_isnil(state, 3) == 0)
        {
            parent = GetObject(runtime, 3);
            if (parent is null)
                return luaL_error(state, usage);
        }
        if (!runtime.TryResolveXmlCreateFrameObjectType(
                requestedObjectType,
                out var objectType))
        {
            return luaL_error(
                state,
                $"CreateFrame: Unknown frame type '{requestedObjectType}'");
        }

        var value = CreateObject(runtime, objectType, name, parent);
        if (forbidden)
            value.Forbidden = true;
        if (lua_gettop(state) >= 5 && lua_isnumber(state, 5) != 0)
            value.FrameId = unchecked((int)lua_tonumber(state, 5));
        runtime.PushObject(value);
        if (name is not null)
            SetGlobalObject(runtime, value);
        if (!runtime.ApplyXmlTemplates(value, parent, templates, requestedObjectType))
            ApplyFrameTemplates(runtime, value, templates);
        return 1;
    }

    private static int CreateFrameRegion(
        LuaRuntime runtime,
        UiObject owner,
        string objectType,
        string usage,
        bool supportsSubLevel)
    {
        var state = runtime.State;
        if (!TryReadOptionalString(state, 2, out var name) ||
            !TryReadOptionalString(state, 3, out var requestedDrawLayer) ||
            !TryReadOptionalString(state, 4, out var templateName))
        {
            return luaL_error(state, usage);
        }

        var drawLayer = "ARTWORK";
        if (requestedDrawLayer is not null)
        {
            if (!LayerNames.Contains(requestedDrawLayer))
                return luaL_error(state, usage);
            drawLayer = requestedDrawLayer.ToUpperInvariant();
        }

        var subLevel = 0;
        if (supportsSubLevel && HasRequiredValue(state, 5))
        {
            if (lua_isnumber(state, 5) == 0)
                return luaL_error(state, usage);
            var numericSubLevel = lua_tonumber(state, 5);
            if (!double.IsFinite(numericSubLevel) ||
                numericSubLevel is < sbyte.MinValue or > sbyte.MaxValue)
            {
                return luaL_error(state, usage);
            }
            subLevel = (int)Math.Truncate(numericSubLevel);
        }

        var fontTemplate = objectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) &&
                           templateName is not null &&
                           runtime.Ui.Find(templateName) is
                           {
                               ObjectType: "Font",
                               Font: not null
                           } resolvedFont
            ? resolvedFont
            : null;
        var hasXmlTemplate = runtime.XmlTemplatesExist(templateName);
        if (!hasXmlTemplate && fontTemplate is null)
        {
            runtime.Log.Warn("ui", $"Couldn't find inherited node \"{templateName}\"");
            lua_pushnil(state);
            return 1;
        }

        if (subLevel is < -8 or > 7)
        {
            runtime.Log.Warn("ui", "Sublevel must be between -8 and 7");
            lua_pushnil(state);
            return 1;
        }

        var value = CreateObject(runtime, objectType, name, owner, drawLayer, subLevel);
        if (value.Name is not null)
            SetGlobalObject(runtime, value);
        if (hasXmlTemplate)
            runtime.ApplyXmlTemplates(value, owner, templateName);
        else
            AssignFontObject(runtime, value, fontTemplate);
        runtime.PushObject(value);
        return 1;
    }

    private static int CreateModelSceneActor(LuaRuntime runtime, UiObject modelScene)
    {
        var state = runtime.State;
        var name = OptionalString(state, 2);
        var templateName = lua_type(state, 3) == LUA_TSTRING
            ? lua_tostring(state, 3)
            : null;

        if (!runtime.XmlTemplatesExist(templateName))
        {
            runtime.Log.Warn(
                "ui",
                $"CreateActor: Couldn't find inherited node \"{templateName}\"");
            return 0;
        }

        var actor = CreateObject(runtime, "ModelSceneActor", name, modelScene);
        if (actor.Name is not null)
            SetGlobalObject(runtime, actor);
        runtime.ApplyXmlTemplates(actor, modelScene, templateName);
        runtime.PushObject(actor);
        return 1;
    }

    private static void RegisterNamespace(
        lua_State state,
        string namespaceName,
        params string[] functions)
    {
        lua_newtable(state);
        foreach (var function in functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, GlobalCallback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, namespaceName);
    }

    private static void RegisterEditModeConstants(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        lua_newtable(state);
        SetTableNumber(state, "Modern", 0);
        SetTableNumber(state, "Classic", 1);
        lua_setfield(state, -2, "EditModePresetLayouts");

        lua_newtable(state);
        SetTableNumber(state, "MinValue", 0);
        SetTableNumber(state, "MaxValue", 1);
        SetTableNumber(state, "NumValues", 2);
        lua_setfield(state, -2, "EditModePresetLayoutsMeta");

        lua_newtable(state);
        SetTableNumber(state, "Preset", 0);
        SetTableNumber(state, "Account", 1);
        SetTableNumber(state, "Character", 2);
        SetTableNumber(state, "Override", 3);
        lua_setfield(state, -2, "EditModeLayoutType");

        lua_newtable(state);
        SetTableNumber(state, "MinValue", 0);
        SetTableNumber(state, "MaxValue", 3);
        SetTableNumber(state, "NumValues", 4);
        lua_setfield(state, -2, "EditModeLayoutTypeMeta");

        lua_newtable(state);
        string[] accountSettingNames =
        [
            "ShowGrid",
            "GridSpacing",
            "SettingsExpanded",
            "ShowTargetAndFocus",
            "ShowStanceBar",
            "ShowPetActionBar",
            "ShowPossessActionBar",
            "ShowCastBar",
            "ShowEncounterBar",
            "ShowExtraAbilities",
            "ShowBuffsAndDebuffs",
            "DeprecatedShowDebuffFrame",
            "ShowPartyFrames",
            "ShowRaidFrames",
            "ShowTalkingHeadFrame",
            "ShowVehicleLeaveButton",
            "ShowBossFrames",
            "ShowArenaFrames",
            "ShowLootFrame",
            "ShowHudTooltip",
            "ShowStatusTrackingBar2",
            "ShowDurabilityFrame",
            "EnableSnap",
            "EnableAdvancedOptions",
            "ShowPetFrame",
            "ShowTimerBars",
            "ShowVehicleSeatIndicator",
            "ShowArchaeologyBar",
            "ShowCooldownViewer",
            "ShowPersonalResourceDisplay",
            "ShowEncounterEvents",
            "ShowDamageMeter",
            "ShowExternalDefensives",
            "ShowRaidWarning",
            "ShowTotemActionBar",
            "ShowLossOfControl"
        ];
        for (var index = 0; index < accountSettingNames.Length; index++)
            SetTableNumber(state, accountSettingNames[index], index);
        lua_setfield(state, -2, "EditModeAccountSetting");

        lua_newtable(state);
        SetTableNumber(state, "MinValue", 0);
        SetTableNumber(state, "MaxValue", accountSettingNames.Length - 1);
        SetTableNumber(state, "NumValues", accountSettingNames.Length);
        lua_setfield(state, -2, "EditModeAccountSettingMeta");

        SetEnum(
            state,
            "ReputationSortType",
            ("None", 0),
            ("Account", 1),
            ("Character", 2));
        SetEnumMeta(state, "ReputationSortTypeMeta", 0, 2, 3);

        RegisterFontStringEnums(state);

        RegisterAccountStoreEnums(state);
        WowHousingApi.RegisterEnums(state);
        WowHouseEditorApi.RegisterEnums(state);
        WowHousingDecorApi.RegisterEnums(state);
        WowPingApi.RegisterEnums(state);
        WowUiWidgetManagerApi.RegisterEnums(state);
        WowBankApi.RegisterEnums(state);
        WowQuestLineApi.RegisterEnums(state);
        WowGossipInfoApi.RegisterEnums(state);
        lua_setglobal(state, "Enum");

        lua_newtable(state);
        lua_newtable(state);
        SetTableNumber(state, "EditModeDefaultLayout", 0);
        lua_setfield(state, -2, "EditModeLayoutConsts");
        lua_newtable(state);
        SetTableNumber(state, "EditModeMaxLayoutsPerType", 5);
        SetTableNumber(state, "EditModeMinGridSpacing", 20);
        SetTableNumber(state, "EditModeDefaultGridSpacing", 100);
        SetTableNumber(state, "EditModeMaxGridSpacing", 300);
        lua_setfield(state, -2, "EditModeConsts");
        lua_newtable(state);
        SetTableNumber(state, "PlunderstormStoreFrontID", 1);
        SetTableNumber(state, "WowhackStoreFrontID", 3);
        SetTableNumber(state, "PlunderstormPlunderCurrencyID", 3139);
        SetTableNumber(state, "KegLegRenownCurrencyID", 2814);
        lua_setfield(state, -2, "AccountStoreConsts");
        lua_newtable(state);
        SetTableNumber(state, "GLOBAL_RECOVERY_CATEGORY", 133);
        lua_setfield(state, -2, "SpellCooldownConsts");
        WowCurrencyInfoApi.RegisterConstants(state);
        lua_setglobal(state, "Constants");
    }

    private static void RegisterFontStringEnums(lua_State state)
    {
        SetEnum(
            state,
            "FontStringScaleAnimationMode",
            ("FontSize", 0),
            ("Vertex", 1));
        SetEnumMeta(state, "FontStringScaleAnimationModeMeta", 0, 1, 2);
    }

    private static void RegisterAccountStoreEnums(lua_State state)
    {
        SetEnum(
            state,
            "AccountStoreCategoryType",
            ("Creature", 1),
            ("TransmogSet", 2),
            ("Mount", 3),
            ("Icon", 4));
        SetEnumMeta(state, "AccountStoreCategoryTypeMeta", 1, 4, 4);
        SetEnum(
            state,
            "AccountStoreFrontFlag",
            ("Enabled", 1),
            ("PurchaseEnabled", 2),
            ("RefundEnabled", 4));
        SetEnumMeta(state, "AccountStoreFrontFlagMeta", 1, 4, 3);
        SetEnum(
            state,
            "AccountStoreItemFlag",
            ("DisplayDefaultArmor", 1),
            ("NotInGameReward", 2),
            ("DisplayAsNew", 4),
            ("DisplayOnly", 8));
        SetEnumMeta(state, "AccountStoreItemFlagMeta", 1, 8, 4);
        SetEnum(
            state,
            "AccountStoreItemMode",
            ("Normal", 1),
            ("Hidden", 2),
            ("Locked", 3));
        SetEnumMeta(state, "AccountStoreItemModeMeta", 1, 3, 3);
        SetEnum(
            state,
            "AccountStoreItemRewardType",
            ("Transmog", 1),
            ("Mount", 2),
            ("Pet", 3),
            ("Toy", 5),
            ("Illusion", 7),
            ("TransmogSet", 8),
            ("Tender", 9),
            ("Misc", 10),
            ("WarbandScene", 11));
        SetEnumMeta(state, "AccountStoreItemRewardTypeMeta", 1, 11, 9);
        SetEnum(
            state,
            "AccountStoreItemStatus",
            ("Unowned", 1),
            ("Refundable", 2),
            ("Owned", 3));
        SetEnumMeta(state, "AccountStoreItemStatusMeta", 1, 3, 3);
        SetEnum(
            state,
            "AccountStoreSettlementAction",
            ("NotSet", 0),
            ("Give", 1),
            ("Remove", 2));
        SetEnumMeta(state, "AccountStoreSettlementActionMeta", 0, 2, 3);
        SetEnum(
            state,
            "AccountStoreState",
            ("Available", 0),
            ("Unknown", 1),
            ("Unavailable", 2));
        SetEnumMeta(state, "AccountStoreStateMeta", 0, 2, 3);
        SetEnum(
            state,
            "AccountStoreTransactionResult",
            ("Success", 0),
            ("Incomplete", 1),
            ("UnknownError", 2),
            ("TransactionInProgress", 3),
            ("InsufficientFunds", 4),
            ("ItemUnknown", 5),
            ("ItemAlreadyOwned", 6),
            ("ItemNotOwned", 7),
            ("InvalidCurrencyType", 8),
            ("OwnedButRefundTimeExpired", 9),
            ("NotSupported", 10),
            ("Unavailable", 11),
            ("ProxyError", 12));
        SetEnumMeta(state, "AccountStoreTransactionResultMeta", 0, 12, 13);
        SetEnum(
            state,
            "AccountStoreTransactionType",
            ("Undefined", 0),
            ("Purchase", 1),
            ("Refund", 2),
            ("DebugResetHistory", 3),
            ("DebugRemoveItem", 4));
        SetEnumMeta(state, "AccountStoreTransactionTypeMeta", 0, 4, 5);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] values)
    {
        lua_newtable(state);
        foreach (var (valueName, value) in values)
            SetTableNumber(state, valueName, value);
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int minimum,
        int maximum,
        int count)
    {
        lua_newtable(state);
        SetTableNumber(state, "NumValues", count);
        SetTableNumber(state, "MinValue", minimum);
        SetTableNumber(state, "MaxValue", maximum);
        lua_setfield(state, -2, name);
    }

    private static void ApplyFrameTemplates(
        LuaRuntime runtime,
        UiObject value,
        string? templates)
    {
        if (string.IsNullOrWhiteSpace(templates))
            return;

        foreach (var template in templates.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!template.Equals("PortraitFrameTemplateMinimizable", StringComparison.OrdinalIgnoreCase) &&
                !template.Equals("PortraitFrameTemplate", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var state = runtime.State;
            lua_pushstring(state, "SetTitle");
            lua_pushcclosure(state, WidgetCallback, 1);
            lua_setfield(state, -2, "SetTitle");

            var titleContainer = CreateObject(runtime, "Frame", null, value);
            titleContainer.Height = 24;
            titleContainer.Anchors.Add(new UiAnchor("TOPLEFT", value.Id, "TOPLEFT", 0, 0));
            titleContainer.Anchors.Add(new UiAnchor("TOPRIGHT", value.Id, "TOPRIGHT", 0, 0));
            runtime.PushObject(titleContainer);

            var titleText = CreateObject(runtime, "FontString", null, titleContainer, "OVERLAY");
            titleText.Anchors.Add(new UiAnchor("CENTER", titleContainer.Id, "CENTER", 0, 0));
            runtime.PushObject(titleText);
            lua_setfield(state, -2, "TitleText");
            lua_setfield(state, -2, "TitleContainer");
            value.Attributes["TemplateTitleTextId"] = titleText.Id;

            var portraitContainer = CreateObject(runtime, "Frame", null, value);
            portraitContainer.Width = 60;
            portraitContainer.Height = 60;
            portraitContainer.Anchors.Add(new UiAnchor("TOPLEFT", value.Id, "TOPLEFT", -6, 6));
            runtime.PushObject(portraitContainer);

            var circleMask = CreateObject(runtime, "MaskTexture", null, portraitContainer, "ARTWORK");
            circleMask.AllPointsTargetId = portraitContainer.Id;
            runtime.PushObject(circleMask);
            lua_setfield(state, -2, "CircleMask");
            lua_setfield(state, -2, "PortraitContainer");
        }
    }

    private static int SetScript(LuaRuntime runtime, UiObject value)
    {
        var state = runtime.State;
        if (lua_type(state, 2) != LUA_TSTRING ||
            lua_type(state, 3) is not (LUA_TFUNCTION or LUA_TNIL))
        {
            runtime.Log.Warn(
                "ui",
                "SetScript: Usage: (\"frameScriptTypeName\", function)");
            return 0;
        }
        var name = lua_tostring(state, 2)!;
        if (!SupportsScript(value, name))
        {
            runtime.Log.Warn("ui", $"SetScript: Doesn't have a \"{name}\" script");
            return 0;
        }
        if (lua_isnil(state, 3) != 0)
        {
            runtime.SetScript(value, name, null);
            return 0;
        }
        runtime.SetScript(value, name, runtime.CaptureFunction(state, 3));
        EnableInputForScript(value, name);
        return 0;
    }

    private static void ResetObjectToDefaults(LuaRuntime runtime, UiObject value)
    {
        runtime.ClearScripts(value);

        if (value.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase) ||
            value.ObjectType.Equals(
                "ModelSceneActor",
                StringComparison.OrdinalIgnoreCase) ||
            value.AnimationGroup is not null ||
            value.Animation is not null ||
            value.ControlPoint is not null)
            return;

        runtime.ClearWindowHierarchy(value);
        value.Anchors.Clear();
        value.AllPointsTargetId = null;
        value.AnimationOffset = Vector2.Zero;
        value.AnimationScale = Vector2.One;
        value.AnimationScaleOriginPoint = "CENTER";
        value.AnimationScaleOriginOffset = Vector2.Zero;
        value.FontAnimationFontSizeScale = 1;
        value.FontAnimationVertexScale = 1;
        value.AnimationRotation = 0;
        value.AnimationRotationOriginPoint = "CENTER";
        value.AnimationRotationOriginOffset = Vector2.Zero;
        value.LineAnimationOffset = Vector2.Zero;
        value.LineAnimationScale = Vector2.One;
        value.LineAnimationScaleOriginPoint = "CENTER";
        value.LineAnimationScaleOriginOffset = Vector2.Zero;
        value.Scale = 1;
        value.Alpha = 1;
        value.VertexColor = Vector4.One;
        value.IgnoreParentAlpha = false;
        value.IgnoreParentScale = false;
        value.CollapsesLayout = false;
        value.MouseClickEnabled = false;
        value.MouseMotionEnabled = false;
        value.MouseWheelEnabled = false;
        value.KeyboardEnabled = false;
        value.FrameId = 0;
        value.PassThroughButtons.Clear();
        value.Movable = false;
        value.Resizable = false;
        value.ResizeMinimum = Vector2.Zero;
        value.ResizeMaximum = Vector2.Zero;
        value.UserPlaced = false;
        value.PropagateKeyboardInput = false;
        value.PropagateMouseClicks = false;
        value.PropagateMouseMotion = false;
        value.DontSavePosition = false;
        value.FlattensRenderLayers = false;
        value.IsFrameBuffer = false;
        value.HyperlinkPropagateToParent = false;
        value.IgnoreChildrenForBounds = false;
        value.GamePadButtonEnabled = false;
        value.GamePadStickEnabled = false;
        value.ClampedToScreen = false;
        value.ClampRectInsets = Vector4.Zero;
        value.HitRectInsets = default;
        value.ClipsChildren = false;
        value.EnabledDrawLayers.Clear();
        foreach (var layer in new[] { "BACKGROUND", "BORDER", "ARTWORK", "OVERLAY" })
            value.EnabledDrawLayers.Add(layer);
        value.HyperlinksEnabled = false;
        runtime.Ui.SetHighlightLocked(value, false);
        value.LastButtonClickTime = null;
        value.HasFrameAlphaGradient = false;
        Array.Clear(value.FrameAlphaGradientEdges);
        if (IsFrameObject(value))
        {
            runtime.Ui.SetFixedFrameLevel(value, false);
            runtime.Ui.SetFixedFrameStrata(value, false);
            runtime.Ui.SetFrameStrata(value, "MEDIUM");
            runtime.Ui.SetUseParentLevel(value, false);
            runtime.Ui.SetToplevel(value, false);
        }

        if (IsButton(value))
        {
            SetButtonVisualState(runtime, value, true, UiButtonState.Normal);
            value.ButtonStateLocked = false;
            value.LastButtonClickTime = null;
            value.MotionScriptsWhileDisabled = false;
            value.PushedTextOffset = Vector2.Zero;
            value.NormalFontObjectId = null;
            value.NormalFontObjectName = null;
            value.HighlightFontObjectId = null;
            value.HighlightFontObjectName = null;
            value.DisabledFontObjectId = null;
            value.DisabledFontObjectName = null;
            value.TextValue = string.Empty;
            if (value.ButtonFontStringId is { } buttonFontStringId &&
                runtime.Ui.Find(buttonFontStringId) is { } buttonFontString)
            {
                buttonFontString.TextValue = string.Empty;
                EnsureFont(buttonFontString).Text = string.Empty;
            }

            ClearButtonTexture(runtime, value, ButtonTextureKind.Normal);
            ClearButtonTexture(runtime, value, ButtonTextureKind.Pushed);
            ClearButtonTexture(runtime, value, ButtonTextureKind.Disabled);
            ClearButtonTexture(runtime, value, ButtonTextureKind.Highlight);

            value.ClickRegistrations.Clear();
            value.ClickRegistrations.Add("LeftButtonUp");
            value.MouseRegistrations.Clear();
            value.MouseRegistrations.Add("AnyDown");
            value.MouseRegistrations.Add("AnyUp");
            RefreshButtonFont(runtime, value);

            if (value.ObjectType.Equals("CheckButton", StringComparison.OrdinalIgnoreCase))
            {
                runtime.SetCheckButtonChecked(value, false);
                ClearButtonTexture(runtime, value, ButtonTextureKind.Checked);
                ClearButtonTexture(runtime, value, ButtonTextureKind.DisabledChecked);
            }
        }

        if (value.Font is not null &&
            (value.ObjectType.Equals(
                 "FontString",
                 StringComparison.OrdinalIgnoreCase) ||
             IsEditBox(value)))
        {
            value.TextValue = string.Empty;
            value.FontObjectId = null;
            value.Font = new UiFontState();
            value.FontAlphaGradientStart = ushort.MaxValue;
            value.FontAlphaGradientLength = 0;
            value.FontRotation = 0;
            value.FontScaleAnimationMode = 0;
            value.FontAnimationFontSizeScale = 1;
            value.FontAnimationVertexScale = 1;
            value.FontSmoothScaling = false;
            value.FontFixedColor = false;
        }

        if (IsEditBox(value))
        {
            value.TextValue = string.Empty;
            value.AutoFocus = true;
            value.MultiLine = false;
            var editBoxFont = EnsureFont(value);
            editBoxFont.JustifyHorizontal = "LEFT";
            editBoxFont.JustifyVertical = "MIDDLE";
            editBoxFont.WordWrap = false;
            editBoxFont.NonSpaceWrap = false;
            editBoxFont.HasLocalJustifyHorizontal = true;
            editBoxFont.HasLocalJustifyVertical = true;
            editBoxFont.LocalOverrides |=
                UiFontOverrides.JustifyHorizontal |
                UiFontOverrides.JustifyVertical |
                UiFontOverrides.WordWrap;
            value.EditBoxCountInvisibleLetters = false;
            value.EditBoxAltArrowKeyMode = false;
            value.EditBoxAlphabeticOnly = false;
            value.Attributes["Numeric"] = false;
            value.EditBoxNumericFullRange = false;
            value.EditBoxPassword = false;
            value.EditBoxBlinkSpeed = 0.5f;
            value.EditBoxMaximumBytes = 0;
            value.MaximumLetters = 0;
            value.EditBoxVisibleTextByteLimit = 0;
            value.TextInsets = default;
            value.EditBoxHighlightColor = Vector4.One;
            value.EditBoxHighlightStart = 0;
            value.EditBoxHighlightEnd = 0;
            value.EditBoxDisplayStart = 0;
            value.EditBoxCaretStops.Clear();
            value.CursorPosition = 0;
            if (runtime.Ui.FocusedObjectId == value.Id)
                runtime.SetKeyboardFocus(null);
        }

        if (IsScrollFrame(value))
        {
            value.HorizontalScroll = 0;
            value.VerticalScroll = 0;
            value.HorizontalScrollRange = 0;
            value.VerticalScrollRange = 0;
            UpdateScrollChildRect(runtime, value);
        }

        if (value.ObjectType.Equals(
                "StatusBar",
                StringComparison.OrdinalIgnoreCase))
        {
            var statusBar = EnsureStatusBar(value);
            statusBar.Minimum = 0;
            statusBar.Maximum = 1;
            statusBar.Value = 0;
            statusBar.RangeInitialized = true;
            statusBar.ValueInitialized = true;
            statusBar.TimerDuration = null;
            statusBar.TimerDirection = 0;
            statusBar.InterpolationActive = false;
            statusBar.DisplayNormalizedValue = 0;
            statusBar.Orientation = "HORIZONTAL";
            statusBar.RotatesTexture = false;
            statusBar.FillStyle = 0;

            var texture = StatusBarTexture(runtime, statusBar) ??
                          CreateObject(
                              runtime,
                              "Texture",
                              null,
                              value,
                              value.DrawLayer);
            runtime.Ui.Reparent(texture, value.Id);
            texture.AllPointsTargetId = value.Id;
            var textureState = EnsureTexture(texture);
            textureState.Asset = null;
            textureState.AtlasName = null;
            textureState.AtlasWidth = null;
            textureState.AtlasHeight = null;
            textureState.FileDataId = null;
            textureState.IsColor = false;
            textureState.Color = Vector4.One;
            textureState.VertexColor = Vector4.One;
            textureState.Desaturation = 0;
            textureState.Gradient = null;
            textureState.ClearAtlasRegion();
            statusBar.TextureId = texture.Id;
        }

        if (value.Texture is { } objectTextureState)
            ResetTextureToDefaults(objectTextureState);
        else if (value.Line is { } lineState)
            ResetTextureToDefaults(lineState.Texture);

        runtime.Ui.InvalidateLayout();
    }

    private static bool IsEditBox(UiObject value) =>
        value.ObjectType.Equals("EditBox", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("EventEditBox", StringComparison.OrdinalIgnoreCase);

    private static bool IsScrollFrame(UiObject value) =>
        value.ObjectType.Equals("ScrollFrame", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("EventScrollFrame", StringComparison.OrdinalIgnoreCase);

    internal static void EnableInputForScript(UiObject value, string scriptName)
    {
        switch (scriptName.ToLowerInvariant())
        {
            case "onmousewheel":
                value.MouseWheelEnabled = true;
                break;
            case "onenter":
            case "onleave":
            case "onmousedown":
            case "onmouseup":
                value.MouseEnabled = true;
                break;
        }
    }

    private static void NotifySizeChanged(LuaRuntime runtime, UiObject value)
    {
        if (value.ScriptReferences.ContainsKey("OnSizeChanged"))
            runtime.QueueSizeChanged(value);

        if (value.ParentId is { } parentId &&
            runtime.Ui.Find(parentId) is { } parent &&
            parent.ScrollChildId == value.Id)
            runtime.QueueScrollChildRect(parent);
    }

    internal static void UpdateScrollChildRect(LuaRuntime runtime, UiObject value)
    {
        runtime.Ui.InvalidateLayout();
        if (value.ScrollChildId is not { } childId || runtime.Ui.Find(childId) is null)
            return;

        var horizontal = CalculateScrollRange(runtime, value, horizontal: true);
        var vertical = CalculateScrollRange(runtime, value, horizontal: false);
        var changed =
            Math.Abs(horizontal - value.HorizontalScrollRange) >= 0.00000095367432f ||
            Math.Abs(vertical - value.VerticalScrollRange) >= 0.00000095367432f;
        value.HorizontalScrollRange = horizontal;
        value.VerticalScrollRange = vertical;
        if (changed)
            runtime.InvokeScript(value, "OnScrollRangeChanged", horizontal, vertical);
    }

    private static bool ScrollChildWouldCreateLoop(
        LuaRuntime runtime,
        UiObject scrollFrame,
        UiObject child)
    {
        for (UiObject? ancestor = scrollFrame;
             ancestor is not null;
             ancestor = ancestor.ParentId is { } parentId ? runtime.Ui.Find(parentId) : null)
        {
            if (ancestor.Id == child.Id)
                return true;
        }
        return false;
    }

    private static int SetTooltipOwner(LuaRuntime runtime, UiObject tooltip)
    {
        var owner = GetObject(runtime, 2);
        if (owner is null ||
            !WowWidgetApi.MethodsFor(owner.ObjectType).Contains("GetPoint") ||
            owner.Id == tooltip.Id ||
            TooltipOwnerWouldCreateLoop(runtime, tooltip, owner))
        {
            return luaL_error(runtime.State, "Usage: self:SetOwner(region)");
        }
        var anchor = OptionalString(runtime.State, 3) ?? "ANCHOR_NONE";
        var offsetX = (float)OptionalNumber(runtime.State, 4);
        var offsetY = (float)OptionalNumber(runtime.State, 5);
        if (!TryNormalizeTooltipAnchor(anchor, out var normalizedAnchor))
            return luaL_error(runtime.State, "Usage: self:SetOwner(region)");
        tooltip.Attributes["TooltipOwnerId"] = owner.Id;
        tooltip.Attributes["TooltipAnchor"] = normalizedAnchor;
        tooltip.Attributes["TooltipOffsetX"] = offsetX;
        tooltip.Attributes["TooltipOffsetY"] = offsetY;
        ClearTooltip(runtime, tooltip);
        ApplyTooltipAnchor(runtime, tooltip, owner, normalizedAnchor, offsetX, offsetY);
        return 0;
    }

    private static int SetTooltipAnchorType(LuaRuntime runtime, UiObject tooltip)
    {
        const string usage =
            "Usage: self:SetAnchorType( anchorType [,Xoffset] [,Yoffset] )";
        if (!TryReadRequiredString(runtime.State, 2, out var anchor) ||
            !TryNormalizeTooltipAnchor(anchor, out var normalizedAnchor))
        {
            return luaL_error(runtime.State, usage);
        }
        if (!TryReadOptionalFloat(runtime.State, 3, 0, out var offsetX) ||
            !TryReadOptionalFloat(runtime.State, 4, 0, out var offsetY))
        {
            return luaL_error(runtime.State, usage);
        }
        tooltip.Attributes["TooltipOffsetX"] = (float)offsetX;
        tooltip.Attributes["TooltipOffsetY"] = (float)offsetY;
        if (tooltip.Attributes.TryGetValue("TooltipOwnerId", out var ownerId) &&
            ownerId is int storedOwnerId &&
            runtime.Ui.Find(storedOwnerId) is { } owner)
        {
            tooltip.Attributes["TooltipAnchor"] = normalizedAnchor;
            ApplyTooltipAnchor(
                runtime,
                tooltip,
                owner,
                normalizedAnchor,
                (float)offsetX,
                (float)offsetY);
        }
        return 0;
    }

    private static void ApplyTooltipAnchor(
        LuaRuntime runtime,
        UiObject tooltip,
        UiObject owner,
        string normalizedAnchor,
        float offsetX,
        float offsetY)
    {
        if (!normalizedAnchor.Equals("ANCHOR_PRESERVE", StringComparison.Ordinal))
        {
            tooltip.Anchors.Clear();
            tooltip.AllPointsTargetId = null;
        }

        if (normalizedAnchor is not "ANCHOR_NONE" and not "ANCHOR_PRESERVE")
        {
            tooltip.Anchors.Add(normalizedAnchor switch
            {
                "ANCHOR_LEFT" =>
                    new UiAnchor("BOTTOMRIGHT", owner.Id, "TOPLEFT", offsetX, offsetY),
                "ANCHOR_RIGHT" =>
                    new UiAnchor("BOTTOMLEFT", owner.Id, "TOPRIGHT", offsetX, offsetY),
                "ANCHOR_BOTTOMLEFT" =>
                    new UiAnchor("TOPRIGHT", owner.Id, "BOTTOMLEFT", offsetX, offsetY),
                "ANCHOR_BOTTOM" =>
                    new UiAnchor("TOP", owner.Id, "BOTTOM", offsetX, offsetY),
                "ANCHOR_BOTTOMRIGHT" =>
                    new UiAnchor("TOPLEFT", owner.Id, "BOTTOMRIGHT", offsetX, offsetY),
                "ANCHOR_TOPLEFT" =>
                    new UiAnchor("BOTTOMLEFT", owner.Id, "TOPLEFT", offsetX, offsetY),
                "ANCHOR_TOP" =>
                    new UiAnchor("BOTTOM", owner.Id, "TOP", offsetX, offsetY),
                "ANCHOR_TOPRIGHT" =>
                    new UiAnchor("BOTTOMRIGHT", owner.Id, "TOPRIGHT", offsetX, offsetY),
                _ =>
                    new UiAnchor("BOTTOMLEFT", owner.Id, "TOPRIGHT", offsetX, offsetY)
            });
        }
        runtime.Ui.InvalidateLayout();
    }

    private static bool TryNormalizeTooltipAnchor(string value, out string normalized)
    {
        normalized = value.ToUpperInvariant();
        return normalized is
            "ANCHOR_LEFT" or "ANCHOR_RIGHT" or "ANCHOR_BOTTOMLEFT" or
            "ANCHOR_BOTTOM" or "ANCHOR_BOTTOMRIGHT" or "ANCHOR_TOPLEFT" or
            "ANCHOR_TOP" or "ANCHOR_TOPRIGHT" or "ANCHOR_CURSOR" or
            "ANCHOR_NONE" or "ANCHOR_PRESERVE" or "ANCHOR_CURSOR_LEFT" or
            "ANCHOR_CURSOR_RIGHT";
    }

    private static bool TooltipOwnerWouldCreateLoop(
        LuaRuntime runtime,
        UiObject tooltip,
        UiObject owner)
    {
        for (UiObject? ancestor = owner;
             ancestor is not null;
             ancestor = ancestor.ParentId is { } parentId ? runtime.Ui.Find(parentId) : null)
        {
            if (ancestor.Id == tooltip.Id)
                return true;
        }
        return false;
    }

    private static UiTooltipState EnsureTooltip(UiObject value) =>
        value.Tooltip ??= new UiTooltipState();

    private static bool TryReadTooltipColor(
        lua_State state,
        int start,
        out Vector4 color)
    {
        color = new Vector4(1, 209 / 255f, 0, 1);
        if (lua_isnumber(state, start) == 0)
            return true;
        if (lua_isnumber(state, start + 1) == 0 ||
            lua_isnumber(state, start + 2) == 0)
        {
            return false;
        }
        color.X = QuantizeNormalizedByte(lua_tonumber(state, start));
        color.Y = QuantizeNormalizedByte(lua_tonumber(state, start + 1));
        color.Z = QuantizeNormalizedByte(lua_tonumber(state, start + 2));
        return true;
    }

    private static void AddTooltipLine(
        LuaRuntime runtime,
        UiObject tooltip,
        string leftText,
        string? rightText,
        Vector4 leftColor,
        Vector4? rightColor,
        bool wrap,
        float leftPadding = 0)
    {
        var state = EnsureTooltip(tooltip);
        var hasRight = !string.IsNullOrEmpty(rightText);
        if (string.IsNullOrEmpty(leftText) && !hasRight ||
            state.Lines.Count >= 1000)
        {
            return;
        }
        var lineIndex = state.Lines.Count + 1;
        var left = EnsureTooltipFontString(runtime, tooltip, lineIndex, right: false);
        var right = EnsureTooltipFontString(runtime, tooltip, lineIndex, right: true);

        left.TextValue = leftText;
        left.Font!.Text = leftText;
        left.Font.Color = leftColor;
        left.Font.JustifyHorizontal = "LEFT";
        left.Shown = true;

        right.TextValue = rightText ?? string.Empty;
        right.Font!.Text = right.TextValue;
        right.Font.Color = rightColor ?? Vector4.One;
        right.Font.JustifyHorizontal = "RIGHT";
        right.Shown = hasRight;

        state.Lines.Add(new UiTooltipLineState
        {
            LeftId = left.Id,
            RightId = right.Id,
            Wrap = wrap,
            LeftPadding = leftPadding
        });
        tooltip.Font = null;
        tooltip.TextValue = string.IsNullOrEmpty(tooltip.TextValue)
            ? leftText
            : $"{tooltip.TextValue}\n{leftText}";
        LayoutTooltip(runtime, tooltip);
    }

    private static UiObject EnsureTooltipFontString(
        LuaRuntime runtime,
        UiObject tooltip,
        int lineIndex,
        bool right)
    {
        var side = right ? "Right" : "Left";
        var parentKey = $"Text{side}{lineIndex}";
        var globalName = tooltip.Name is null ? null : $"{tooltip.Name}Text{side}{lineIndex}";
        var existing = tooltip.Children
            .Select(runtime.Ui.Find)
            .FirstOrDefault(child =>
                child is { Font: not null } &&
                (string.Equals(child.ParentKey, parentKey, StringComparison.Ordinal) ||
                 globalName is not null &&
                 string.Equals(child.Name, globalName, StringComparison.Ordinal)));
        if (existing is not null)
            return existing;

        UiFontState? sourceFont = null;
        var styleIndex = lineIndex == 1 ? 1 : 2;
        var styleKey = $"Text{side}{styleIndex}";
        var styleName = tooltip.Name is null ? null : $"{tooltip.Name}Text{side}{styleIndex}";
        sourceFont = tooltip.Children
            .Select(runtime.Ui.Find)
            .FirstOrDefault(child =>
                child is { Font: not null } &&
                (string.Equals(child.ParentKey, styleKey, StringComparison.Ordinal) ||
                 styleName is not null &&
                 string.Equals(child.Name, styleName, StringComparison.Ordinal)))
            ?.Font;

        var created = CreateObject(runtime, "FontString", globalName, tooltip, "ARTWORK");
        if (sourceFont is not null)
            created.Font = CopyFont(sourceFont);
        else
            created.Font!.IsConfigured = true;
        created.Font!.Text = string.Empty;
        created.Font.JustifyHorizontal = right ? "RIGHT" : "LEFT";
        created.Font.JustifyVertical = "TOP";
        created.Shown = false;
        runtime.SetParentKey(created, parentKey, false);
        if (created.Name is not null)
            SetGlobalObject(runtime, created);
        return created;
    }

    private readonly record struct TooltipTextureInfo(
        float Width,
        float Height,
        float VerticalOffset,
        UiInsets Margin,
        int Anchor,
        int RelativeRegion,
        float Left,
        float Right,
        float Top,
        float Bottom,
        Vector4 VertexColor,
        float Desaturation);

    private static int AddTooltipTexture(
        LuaRuntime runtime,
        UiObject tooltip,
        bool atlas)
    {
        var state = runtime.State;
        var usage = atlas
            ? "Usage: self:AddAtlas(\"atlas\" [, minx, maxx, miny, maxy] or [, textureInfoTable])"
            : "Usage: self:AddTexture(UITextureAsset [, minx, maxx, miny, maxy] or [, textureInfoTable])";
        string? asset = null;
        uint? fileDataId = null;
        string? atlasName = null;
        if (atlas)
        {
            if (!TryReadRequiredAtlasName(state, 2, out var parsedAtlas))
                return luaL_error(state, usage);
            atlasName = parsedAtlas;
        }
        else if (!TryReadRequiredTextureAsset(state, 2, out asset, out fileDataId))
        {
            return luaL_error(state, usage);
        }

        if (!TryReadTooltipTextureInfo(state, out var info))
            return luaL_error(state, usage);
        var tooltipState = EnsureTooltip(tooltip);
        if (tooltipState.Textures.Count >= 30)
            return 0;

        var textureIndex = tooltipState.Textures.Count + 1;
        var parentKey = $"Texture{textureIndex}";
        var globalName = tooltip.Name is null ? null : $"{tooltip.Name}Texture{textureIndex}";
        var texture = tooltip.Children
            .Select(runtime.Ui.Find)
            .FirstOrDefault(child =>
                child?.Texture is not null &&
                (string.Equals(child.ParentKey, parentKey, StringComparison.Ordinal) ||
                 globalName is not null &&
                 string.Equals(child.Name, globalName, StringComparison.Ordinal)));
        if (texture is null)
        {
            texture = CreateObject(runtime, "Texture", globalName, tooltip, "ARTWORK");
            runtime.SetParentKey(texture, parentKey, false);
            if (texture.Name is not null)
                SetGlobalObject(runtime, texture);
        }

        var textureState = EnsureTexture(texture);
        ClearTextureAsset(textureState);
        textureState.Asset = asset;
        textureState.FileDataId = fileDataId;
        textureState.AtlasName = atlasName;
        textureState.LocalUv[0] = new Vector2(info.Left, info.Top);
        textureState.LocalUv[1] = new Vector2(info.Left, info.Bottom);
        textureState.LocalUv[2] = new Vector2(info.Right, info.Top);
        textureState.LocalUv[3] = new Vector2(info.Right, info.Bottom);
        textureState.ResolveUv();
        textureState.VertexColor = info.VertexColor;
        textureState.Desaturation = Math.Clamp(info.Desaturation, 0, 1);
        texture.Width = info.Width;
        texture.Height = info.Height;
        texture.Shown = true;
        tooltipState.Textures.Add(new UiTooltipTextureState
        {
            TextureId = texture.Id,
            LineIndex = Math.Max(0, tooltipState.Lines.Count - 1),
            Width = info.Width,
            Height = info.Height,
            VerticalOffset = info.VerticalOffset,
            Margin = info.Margin,
            Anchor = info.Anchor,
            RelativeRegion = info.RelativeRegion
        });
        LayoutTooltip(runtime, tooltip);
        return 0;
    }

    private static bool TryReadTooltipTextureInfo(
        lua_State state,
        out TooltipTextureInfo info)
    {
        var width = 12f;
        var height = 12f;
        var verticalOffset = 0f;
        var margin = new UiInsets(0, 8, 0, 0);
        var anchor = 0;
        var region = 0;
        var left = 0f;
        var right = 1f;
        var top = 0f;
        var bottom = 1f;
        var vertexColor = Vector4.One;
        var desaturation = 0f;

        if (lua_istable(state, 3) != 0)
        {
            width = ReadNumberField(state, 3, "width", width);
            height = ReadNumberField(state, 3, "height", height);
            verticalOffset = ReadNumberField(
                state,
                3,
                "verticalOffset",
                verticalOffset);
            desaturation = ReadNumberField(
                state,
                3,
                "desaturation",
                desaturation);
            anchor = ReadBoundedIntegerField(state, 3, "anchor", anchor, 0, 6);
            region = ReadBoundedIntegerField(state, 3, "region", region, 0, 1);

            lua_getfield(state, 3, "margin");
            if (lua_istable(state, -1) != 0)
            {
                margin = new UiInsets(
                    ReadNumberField(state, -1, "left", margin.Left),
                    ReadNumberField(state, -1, "right", margin.Right),
                    ReadNumberField(state, -1, "top", margin.Top),
                    ReadNumberField(state, -1, "bottom", margin.Bottom));
            }
            lua_pop(state, 1);

            lua_getfield(state, 3, "texCoords");
            if (lua_istable(state, -1) != 0)
            {
                left = ReadNumberField(state, -1, "left", left);
                right = ReadNumberField(state, -1, "right", right);
                top = ReadNumberField(state, -1, "top", top);
                bottom = ReadNumberField(state, -1, "bottom", bottom);
            }
            lua_pop(state, 1);

            lua_getfield(state, 3, "vertexColor");
            if (lua_istable(state, -1) != 0)
            {
                vertexColor = new Vector4(
                    Math.Clamp(ReadNumberField(state, -1, "r", 1), 0, 1),
                    Math.Clamp(ReadNumberField(state, -1, "g", 1), 0, 1),
                    Math.Clamp(ReadNumberField(state, -1, "b", 1), 0, 1),
                    Math.Clamp(ReadNumberField(state, -1, "a", 1), 0, 1));
            }
            lua_pop(state, 1);
        }
        else if (lua_isnumber(state, 3) != 0)
        {
            if (lua_isnumber(state, 4) == 0 ||
                lua_isnumber(state, 5) == 0 ||
                lua_isnumber(state, 6) == 0)
            {
                info = default;
                return false;
            }
            left = (float)lua_tonumber(state, 3);
            right = (float)lua_tonumber(state, 4);
            top = (float)lua_tonumber(state, 5);
            bottom = (float)lua_tonumber(state, 6);
        }

        info = new TooltipTextureInfo(
            width,
            height,
            verticalOffset,
            margin,
            anchor,
            region,
            left,
            right,
            top,
            bottom,
            vertexColor,
            desaturation);
        return true;
    }

    private static float ReadNumberField(
        lua_State state,
        int tableIndex,
        string field,
        float fallback)
    {
        lua_getfield(state, tableIndex, field);
        var value = lua_isnumber(state, -1) != 0
            ? (float)lua_tonumber(state, -1)
            : fallback;
        lua_pop(state, 1);
        return value;
    }

    private static int ReadBoundedIntegerField(
        lua_State state,
        int tableIndex,
        string field,
        int fallback,
        int minimum,
        int maximum)
    {
        lua_getfield(state, tableIndex, field);
        var value = lua_isnumber(state, -1) != 0
            ? (int)lua_tonumber(state, -1)
            : fallback;
        lua_pop(state, 1);
        return value >= minimum && value <= maximum ? value : fallback;
    }

    private static void LayoutTooltip(LuaRuntime runtime, UiObject tooltip)
    {
        var state = EnsureTooltip(tooltip);
        const float borderInset = 10;
        const float doubleLineSpacing = 38.4f;
        const float defaultWrappedContentWidth = 230.4f;
        var wrappedContentWidth =
            state.CustomWordWrapMinWidth ?? defaultWrappedContentWidth;
        var lineSpacing = state.CustomLineSpacing ?? 2;
        var leftInset = borderInset + state.Padding.Left;
        var rightInset = borderInset + state.Padding.Right;
        var topInset = borderInset + state.Padding.Top;
        var bottomInset = borderInset + state.Padding.Bottom;
        var contentWidth = 0f;

        for (var lineIndex = 0; lineIndex < state.Lines.Count; lineIndex++)
        {
            var line = state.Lines[lineIndex];
            var left = runtime.Ui.Find(line.LeftId);
            var right = runtime.Ui.Find(line.RightId);
            if (left?.Font is null || right?.Font is null)
                continue;

            var leftSize = MeasureText(
                runtime,
                left,
                ignoreWidthConstraint: true).Size;
            var rightSize = right.Shown
                ? MeasureText(runtime, right, ignoreWidthConstraint: true).Size
                : Vector2.Zero;
            var lineTextures = state.Textures
                .Where(texture => texture.LineIndex == lineIndex)
                .ToArray();
            var textureWidth = TooltipHorizontalExtent(lineTextures);
            var lineWidth =
                line.LeftPadding +
                leftSize.X +
                (right.Shown ? doubleLineSpacing + rightSize.X : 0) +
                textureWidth;
            contentWidth = Math.Max(
                contentWidth,
                line.Wrap
                    ? Math.Min(lineWidth, wrappedContentWidth)
                    : lineWidth);
        }

        contentWidth = Math.Max(
            contentWidth,
            Math.Max(0, state.MinimumWidth - leftInset - rightInset));

        if (state.ShrinkToFitWrapped &&
            !state.ForceMinimumWidth &&
            state.Lines.Any(line => line.Wrap))
        {
            var minimumContentWidth =
                Math.Max(0, state.MinimumWidth - leftInset - rightInset);
            var widestNonWrappedLine = minimumContentWidth;
            var widestWrappedRow = 0f;
            var hasWrappedLine = false;
            for (var lineIndex = 0; lineIndex < state.Lines.Count; lineIndex++)
            {
                var line = state.Lines[lineIndex];
                var left = runtime.Ui.Find(line.LeftId);
                var right = runtime.Ui.Find(line.RightId);
                if (left?.Font is null || right?.Font is null)
                    continue;

                var lineTextures = state.Textures
                    .Where(texture => texture.LineIndex == lineIndex)
                    .ToArray();
                var totalTextureWidth = TooltipHorizontalExtent(lineTextures);
                var rightSize = right.Shown
                    ? MeasureText(runtime, right, ignoreWidthConstraint: true).Size
                    : Vector2.Zero;
                var fixedWidth =
                    line.LeftPadding +
                    (right.Shown ? doubleLineSpacing + rightSize.X : 0) +
                    totalTextureWidth;
                if (!line.Wrap)
                {
                    widestNonWrappedLine = Math.Max(
                        widestNonWrappedLine,
                        MeasureText(runtime, left, ignoreWidthConstraint: true).Size.X +
                        fixedWidth);
                    continue;
                }

                hasWrappedLine = true;
                left.Font.WordWrap = true;
                left.Width = Math.Max(1, contentWidth - fixedWidth);
                widestWrappedRow = Math.Max(
                    widestWrappedRow,
                    MeasureText(runtime, left).Size.X + fixedWidth);
            }

            if (hasWrappedLine)
            {
                contentWidth = Math.Clamp(
                    widestWrappedRow,
                    widestNonWrappedLine,
                    contentWidth);
            }
        }

        var contentHeight = 0f;
        for (var lineIndex = 0; lineIndex < state.Lines.Count; lineIndex++)
        {
            var line = state.Lines[lineIndex];
            var left = runtime.Ui.Find(line.LeftId);
            var right = runtime.Ui.Find(line.RightId);
            if (left?.Font is null || right?.Font is null)
                continue;

            var lineTextures = state.Textures
                .Where(texture => texture.LineIndex == lineIndex)
                .ToArray();
            var leftReserve = TooltipTextureGroupExtent(
                lineTextures,
                relativeRegion: 0,
                leftSide: true);
            var rightReserve = TooltipTextureGroupExtent(
                lineTextures,
                relativeRegion: 1,
                leftSide: false);
            var totalTextureWidth = TooltipHorizontalExtent(lineTextures);
            var rightSize = right.Shown
                ? MeasureText(runtime, right, ignoreWidthConstraint: true).Size
                : Vector2.Zero;
            var leftWidth = line.Wrap
                ? Math.Max(
                    1,
                    contentWidth -
                    line.LeftPadding -
                    (right.Shown ? doubleLineSpacing + rightSize.X : 0) -
                    totalTextureWidth)
                : MeasureText(runtime, left, ignoreWidthConstraint: true).Size.X;
            left.Font.WordWrap = line.Wrap;
            left.Width = line.Wrap ? leftWidth : null;
            var leftSize = line.Wrap
                ? MeasureText(runtime, left).Size
                : MeasureText(runtime, left, ignoreWidthConstraint: true).Size;
            var lineHeight = Math.Max(leftSize.Y, rightSize.Y);
            if (lineTextures.Length > 0)
            {
                lineHeight = Math.Max(
                    lineHeight,
                    lineTextures.Max(texture =>
                        texture.Height + texture.Margin.Top + texture.Margin.Bottom));
            }

            left.Height = lineHeight;
            left.AllPointsTargetId = null;
            left.Anchors.Clear();
            left.Anchors.Add(new UiAnchor(
                "TOPLEFT",
                tooltip.Id,
                "TOPLEFT",
                leftInset + line.LeftPadding + leftReserve,
                -(topInset + contentHeight)));

            right.Width = null;
            right.Font.WordWrap = false;
            right.Height = lineHeight;
            right.AllPointsTargetId = null;
            right.Anchors.Clear();
            right.Anchors.Add(new UiAnchor(
                "TOPRIGHT",
                tooltip.Id,
                "TOPRIGHT",
                -(rightInset + rightReserve),
                -(topInset + contentHeight)));

            foreach (var textureState in lineTextures)
            {
                if (runtime.Ui.Find(textureState.TextureId) is { } texture)
                {
                    PositionTooltipTexture(
                        texture,
                        textureState,
                        textureState.RelativeRegion == 1 ? right : left);
                }
            }

            contentHeight += lineHeight + lineSpacing;
        }

        if (state.Lines.Count > 0)
            contentHeight -= lineSpacing;
        var naturalWidth = leftInset + contentWidth + rightInset;
        tooltip.Width = Math.Max(state.MinimumWidth, naturalWidth);
        tooltip.Height = topInset + contentHeight + bottomInset;
        runtime.Ui.InvalidateLayout();
        NotifySizeChanged(runtime, tooltip);
    }

    private static float TooltipHorizontalExtent(
        IReadOnlyList<UiTooltipTextureState> textures) =>
        TooltipTextureGroupExtent(textures, relativeRegion: 0, leftSide: true) +
        TooltipTextureGroupExtent(textures, relativeRegion: 0, leftSide: false) +
        TooltipTextureGroupExtent(textures, relativeRegion: 1, leftSide: true) +
        TooltipTextureGroupExtent(textures, relativeRegion: 1, leftSide: false);

    private static float TooltipTextureGroupExtent(
        IReadOnlyList<UiTooltipTextureState> textures,
        int relativeRegion,
        bool leftSide)
    {
        var maximum = 0f;
        foreach (var texture in textures)
        {
            if (texture.RelativeRegion != relativeRegion)
                continue;
            var belongsToSide = leftSide
                ? texture.Anchor is 0 or 1 or 2 or 6
                : texture.Anchor is 3 or 4 or 5 or 6;
            if (!belongsToSide)
                continue;

            var extent = texture.Anchor == 6
                ? leftSide
                    ? texture.Margin.Left
                    : texture.Margin.Right
                : texture.Width + texture.Margin.Left + texture.Margin.Right;
            maximum = Math.Max(maximum, extent);
        }
        return maximum;
    }

    private static void PositionTooltipTexture(
        UiObject texture,
        UiTooltipTextureState state,
        UiObject relative)
    {
        texture.AllPointsTargetId = null;
        texture.Anchors.Clear();
        if (state.Anchor == 6)
        {
            texture.AllPointsTargetId = relative.Id;
            return;
        }

        var (point, relativePoint, x, y) = state.Anchor switch
        {
            0 => (
                "TOPRIGHT",
                "TOPLEFT",
                -state.Margin.Right,
                state.VerticalOffset - state.Margin.Top),
            1 => (
                "RIGHT",
                "LEFT",
                -state.Margin.Right,
                state.VerticalOffset),
            2 => (
                "BOTTOMRIGHT",
                "BOTTOMLEFT",
                -state.Margin.Right,
                state.VerticalOffset + state.Margin.Bottom),
            3 => (
                "TOPLEFT",
                "TOPRIGHT",
                state.Margin.Left,
                state.VerticalOffset - state.Margin.Top),
            4 => (
                "LEFT",
                "RIGHT",
                state.Margin.Left,
                state.VerticalOffset),
            _ => (
                "BOTTOMLEFT",
                "BOTTOMRIGHT",
                state.Margin.Left,
                state.VerticalOffset + state.Margin.Bottom)
        };
        texture.Anchors.Add(new UiAnchor(point, relative.Id, relativePoint, x, y));
    }

    private static void ClearTooltip(LuaRuntime runtime, UiObject tooltip)
    {
        var state = EnsureTooltip(tooltip);
        foreach (var line in state.Lines)
        {
            ClearTooltipFontString(runtime.Ui.Find(line.LeftId));
            ClearTooltipFontString(runtime.Ui.Find(line.RightId));
        }
        foreach (var texture in state.Textures)
        {
            if (runtime.Ui.Find(texture.TextureId) is { } textureObject)
                textureObject.Shown = false;
        }
        state.Lines.Clear();
        state.Textures.Clear();
        state.CustomLineSpacing = null;
        state.CustomWordWrapMinWidth = null;
        state.ShrinkToFitWrapped = true;
        state.AllowShowWithNoLines = false;
        state.FadeRemaining = 0;
        tooltip.Font = null;
        tooltip.TextValue = string.Empty;
        tooltip.Width = Math.Max(state.MinimumWidth, 0);
        tooltip.Height = 0;
        runtime.Ui.InvalidateLayout();
    }

    private static string BuildTooltipCopyText(LuaRuntime runtime, UiObject tooltip)
    {
        var lines = new List<string>();
        foreach (var line in EnsureTooltip(tooltip).Lines)
        {
            var left = runtime.Ui.Find(line.LeftId)?.Font?.Text;
            var right = runtime.Ui.Find(line.RightId)?.Font?.Text;
            if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
                continue;
            lines.Add(string.IsNullOrEmpty(right)
                ? left ?? string.Empty
                : $"{left}\t\t\t\t{right}");
        }
        return string.Join('\n', lines);
    }

    private static void ClearTooltipFontString(UiObject? value)
    {
        if (value?.Font is null)
            return;
        value.TextValue = string.Empty;
        value.Font.Text = string.Empty;
        value.Shown = false;
    }

    private static int PushTooltipLine(
        LuaRuntime runtime,
        UiObject tooltip,
        uint lineIndex,
        bool right)
    {
        var lines = EnsureTooltip(tooltip).Lines;
        if (lineIndex >= lines.Count)
        {
            lua_pushnil(runtime.State);
            return 1;
        }

        var line = lines[(int)lineIndex];
        runtime.PushObject(runtime.Ui.Find(right ? line.RightId : line.LeftId));
        return 1;
    }

    private static int HookScript(LuaRuntime runtime, UiObject value)
    {
        var state = runtime.State;
        if (lua_type(state, 2) != LUA_TSTRING || lua_type(state, 3) != LUA_TFUNCTION)
        {
            runtime.Log.Warn(
                "ui",
                "HookScript: Usage: (\"frameScriptTypeName\", function[, bindingType])");
            return 0;
        }
        var name = lua_tostring(state, 2)!;
        if (!SupportsScript(value, name))
        {
            runtime.Log.Warn("ui", $"HookScript: Doesn't have a \"{name}\" script");
            return 0;
        }
        var next = runtime.CaptureFunction(state, 3);
        runtime.HookScript(value, name, next);
        EnableInputForScript(value, name);
        lua_pushboolean(state, 1);
        return 1;
    }

    private static void SetPoint(LuaRuntime runtime, UiObject value)
    {
        var state = runtime.State;
        var top = lua_gettop(state);
        if (top < 2 || lua_type(state, 2) != LUA_TSTRING)
        {
            runtime.Log.Warn(
                "ui",
                "SetPoint: Usage: (\"point\" [, region or nil] [, \"relativePoint\"] [, offsetX, offsetY])");
            return;
        }

        var point = lua_tostring(state, 2)!;
        if (!FramePointNames.Contains(point))
        {
            runtime.Log.Warn("ui", $"SetPoint: Invalid region point {point}");
            return;
        }
        point = point.ToUpperInvariant();
        var relative = runtime.Ui.Find(value.ParentId ?? runtime.Ui.UiParentId);
        var relativePoint = point;
        var x = 0f;
        var y = 0f;
        var offsetIndex = 3;

        if (top >= 3)
        {
            var argumentType = lua_type(state, 3);
            if (argumentType is LUA_TTABLE or LUA_TUSERDATA || argumentType == LUA_TSTRING)
            {
                relative = GetObject(runtime, 3);
                if (relative is null)
                {
                    if (argumentType == LUA_TSTRING)
                    {
                        runtime.Log.Warn(
                            "ui",
                            $"SetPoint: Couldn't find region named '{lua_tostring(state, 3)}'");
                    }
                    else
                    {
                        runtime.Log.Warn("ui", "SetPoint: Invalid relative region");
                    }
                    return;
                }
                offsetIndex = 4;
            }
            else if (argumentType == LUA_TNIL)
            {
                relative = runtime.Ui.Find(runtime.Ui.UiParentId);
                offsetIndex = 4;
            }
        }

        if (top >= offsetIndex && lua_type(state, offsetIndex) == LUA_TSTRING)
        {
            var requestedRelativePoint = lua_tostring(state, offsetIndex)!;
            if (!FramePointNames.Contains(requestedRelativePoint))
            {
                runtime.Log.Warn(
                    "ui",
                    $"SetPoint: Unknown region point {requestedRelativePoint}");
                return;
            }
            relativePoint = requestedRelativePoint.ToUpperInvariant();
            offsetIndex++;
        }
        if (top >= offsetIndex + 1 &&
            lua_isnumber(state, offsetIndex) != 0 &&
            lua_isnumber(state, offsetIndex + 1) != 0)
        {
            x = (float)lua_tonumber(state, offsetIndex);
            y = (float)lua_tonumber(state, offsetIndex + 1);
        }

        MaterializeAllPointsAnchors(value);
        var anchor = new UiAnchor(point, relative?.Id, relativePoint, x, y);
        var existingIndex = value.Anchors.FindIndex(existing =>
            existing.Point.Equals(point, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            value.Anchors[existingIndex] = anchor;
        else
            value.Anchors.Add(anchor);
        runtime.Ui.InvalidateRectValidity(value);
        runtime.Ui.InvalidateLayout();
        NotifySizeChanged(runtime, value);
    }

    private static void MaterializeAllPointsAnchors(UiObject value)
    {
        if (value.AllPointsTargetId is not { } targetId)
            return;

        value.Anchors.Clear();
        value.Anchors.Add(new UiAnchor("TOPLEFT", targetId, "TOPLEFT", 0, 0));
        value.Anchors.Add(new UiAnchor("BOTTOMRIGHT", targetId, "BOTTOMRIGHT", 0, 0));
        value.AllPointsTargetId = null;
    }

    private static float CalculateScrollRange(
        LuaRuntime runtime,
        UiObject value,
        bool horizontal)
    {
        var viewport = runtime.Ui.ResolveBounds(value.Id);
        var child = value.ScrollChildId is { } childId
            ? runtime.Ui.Find(childId)
            : null;
        var childBounds = child is not null
            ? runtime.Ui.ResolveScrollChildBoundsRect(child)
            : null;
        var scaledRange = horizontal
            ? Math.Max(0, childBounds?.Width - viewport.Width ?? 0)
            : Math.Max(0, childBounds?.Height - viewport.Height ?? 0);
        return Unscaled(runtime, value, scaledRange);
    }

    private static float Unscaled(LuaRuntime runtime, UiObject value, float coordinate)
    {
        var scale = runtime.Ui.LayoutScale(value);
        return MathF.Abs(scale) < 0.000001f ? 0 : coordinate / scale;
    }

    private static Vector2 ResolveUnrectedSize(LuaRuntime runtime, UiObject value)
    {
        var isFontString = value.ObjectType.Equals(
            "FontString",
            StringComparison.OrdinalIgnoreCase);
        var measuredText = value.Font is not null
            ? MeasureText(runtime, value).Size
            : Vector2.Zero;
        var intrinsicTextureSize = value.Texture is { AtlasName: null } texture
            ? new Vector2(
                texture.IntrinsicWidth.GetValueOrDefault(),
                texture.IntrinsicHeight.GetValueOrDefault())
            : Vector2.Zero;

        var width = value.Width is { } explicitWidth &&
                    !(isFontString && explicitWidth == 0)
            ? explicitWidth
            : value.Font is not null
                ? measuredText.X
                : intrinsicTextureSize.X;
        var height = value.Height is { } explicitHeight &&
                     !(isFontString && explicitHeight == 0)
            ? explicitHeight
            : value.Font is not null
                ? measuredText.Y
                : intrinsicTextureSize.Y;
        return new Vector2(width, height);
    }

    private static int GetPoint(LuaRuntime runtime, UiObject value)
    {
        var state = runtime.State;
        const string usage =
            "Usage: local point, relativeTo, relativePoint, offsetX, offsetY = " +
            "self:GetPoint([anchorIndex, resolveCollapsed])";
        var index = 0;
        if (HasRequiredValue(state, 2))
        {
            if (!TryReadRequiredUInt32(state, 2, out var oneBasedIndex) ||
                oneBasedIndex == 0)
                return luaL_error(state, usage);
            if (oneBasedIndex > int.MaxValue)
                return 0;
            index = (int)oneBasedIndex - 1;
        }
        var resolveCollapsed = OptionalBoolean(state, 3, false);
        if (value.AllPointsTargetId is { } allPointsTargetId)
        {
            if (index is < 0 or > 1)
                return 0;
            var allPointsName = index == 0 ? "TOPLEFT" : "BOTTOMRIGHT";
            var allPointsAnchor = new UiAnchor(
                allPointsName,
                allPointsTargetId,
                allPointsName,
                0,
                0);
            return PushPoint(
                runtime,
                resolveCollapsed
                    ? ResolveCollapsedAnchor(runtime, allPointsAnchor)
                    : allPointsAnchor);
        }
        if (index >= value.Anchors.Count)
            return 0;
        var point = value.Anchors[index];
        return PushPoint(
            runtime,
            resolveCollapsed ? ResolveCollapsedAnchor(runtime, point) : point);
    }

    private static int GetPointByName(LuaRuntime runtime, UiObject value)
    {
        const string usage =
            "Usage: local point, relativeTo, relativePoint, offsetX, offsetY = " +
            "self:GetPointByName(point [, resolveCollapsed])";
        var state = runtime.State;
        if (lua_gettop(state) < 2 ||
            lua_type(state, 2) != LUA_TSTRING)
            return luaL_error(state, usage);
        var requestedPoint = lua_tostring(state, 2)!;
        if (!FramePointNames.Contains(requestedPoint))
            return luaL_error(state, usage);
        requestedPoint = requestedPoint.ToUpperInvariant();
        var resolveCollapsed = OptionalBoolean(state, 3, false);
        if (value.AllPointsTargetId is { } allPointsTargetId &&
            (requestedPoint.Equals("TOPLEFT", StringComparison.OrdinalIgnoreCase) ||
             requestedPoint.Equals("BOTTOMRIGHT", StringComparison.OrdinalIgnoreCase)))
        {
            var allPointsAnchor = new UiAnchor(
                requestedPoint,
                allPointsTargetId,
                requestedPoint,
                0,
                0);
            return PushPoint(
                runtime,
                resolveCollapsed
                    ? ResolveCollapsedAnchor(runtime, allPointsAnchor)
                    : allPointsAnchor);
        }
        var point = value.Anchors.FirstOrDefault(anchor =>
            anchor.Point.Equals(requestedPoint, StringComparison.OrdinalIgnoreCase));
        if (point is null)
            return 0;

        return PushPoint(
            runtime,
            resolveCollapsed ? ResolveCollapsedAnchor(runtime, point) : point);
    }

    private static int PushPoint(LuaRuntime runtime, UiAnchor point)
    {
        lua_pushstring(runtime.State, point.Point);
        runtime.PushObject(
            point.RelativeToId is { } id
                ? runtime.Ui.Find(id)
                : null);
        lua_pushstring(runtime.State, point.RelativePoint);
        lua_pushnumber(runtime.State, point.X);
        lua_pushnumber(runtime.State, point.Y);
        return 5;
    }

    private static UiAnchor ResolveCollapsedAnchor(
        LuaRuntime runtime,
        UiAnchor original)
    {
        var current = original;
        var originalTargetId = original.RelativeToId;
        var visitedTargets = new HashSet<int>();
        while (current.RelativeToId is { } targetId &&
               runtime.Ui.Find(targetId) is { } target &&
               !runtime.Ui.IsVisible(target) &&
               target.CollapsesLayout &&
               visitedTargets.Add(targetId))
        {
            UiAnchor? replacement = null;
            if (target.AllPointsTargetId is { } allPointsTargetId &&
                current.RelativePoint is "TOPLEFT" or "BOTTOMRIGHT")
            {
                replacement = new UiAnchor(
                    current.RelativePoint,
                    allPointsTargetId,
                    current.RelativePoint,
                    0,
                    0);
            }
            else
            {
                replacement = target.Anchors.FirstOrDefault(anchor =>
                    anchor.Point.Equals(
                        current.RelativePoint,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (replacement is null || replacement.RelativeToId is null)
                break;
            current = replacement;
            if (current.RelativeToId == originalTargetId)
                break;
        }
        return current;
    }

    internal static void SetShown(LuaRuntime runtime, UiObject value, bool shown)
    {
        if (value.Shown == shown)
            return;

        ApplyVisibilityMutation(
            runtime,
            value,
            () =>
            {
                value.Shown = shown;
                if (shown)
                    runtime.Ui.Raise(value);
            });
    }

    private static void ReparentWithVisibility(
        LuaRuntime runtime,
        UiObject value,
        int? parentId)
    {
        if (value.ParentId == parentId)
            return;

        ApplyVisibilityMutation(
            runtime,
            value,
            () => runtime.Ui.Reparent(value, parentId));
    }

    private static void ApplyVisibilityMutation(
        LuaRuntime runtime,
        UiObject value,
        Action mutation)
    {
        var affected = EnumerateSubtree(runtime, value).ToArray();
        var visibilityBefore = affected.ToDictionary(
            item => item.Id,
            runtime.Ui.IsVisible);
        mutation();

        var transitions = EnumerateSubtreePostOrder(runtime, value)
            .Where(item =>
                visibilityBefore.TryGetValue(item.Id, out var wasVisible) &&
                wasVisible != runtime.Ui.IsVisible(item))
            .Select(item => (Item: item, IsVisible: runtime.Ui.IsVisible(item)))
            .ToArray();

        if (runtime.Ui.FocusedObjectId is { } focusedId &&
            transitions.Any(transition =>
                transition.Item.Id == focusedId && !transition.IsVisible))
        {
            runtime.SetKeyboardFocus(null);
        }

        foreach (var transition in transitions)
        {
            if (!transition.IsVisible && transition.Item.ColorSelect is not null)
                EndColorSelectInteraction(transition.Item);
            runtime.InvokeScript(
                transition.Item,
                transition.IsVisible ? "OnShow" : "OnHide");
            if (transition.IsVisible)
                ApplyEditBoxAutoFocusAfterShow(runtime, transition.Item);
        }
    }

    internal static void ApplyEditBoxAutoFocusAfterShow(
        LuaRuntime runtime,
        UiObject value)
    {
        if (runtime.Ui.FocusedObjectId is null &&
            IsEditBox(value) &&
            value.AutoFocus)
        {
            runtime.SetKeyboardFocus(value);
        }
    }

    private static IEnumerable<UiObject> EnumerateSubtreePostOrder(
        LuaRuntime runtime,
        UiObject root)
    {
        foreach (var childId in root.Children)
        {
            if (runtime.Ui.Find(childId) is not { } child)
                continue;
            foreach (var descendant in EnumerateSubtreePostOrder(runtime, child))
                yield return descendant;
        }
        yield return root;
    }

    private static IEnumerable<UiObject> EnumerateSubtree(LuaRuntime runtime, UiObject root)
    {
        yield return root;
        foreach (var childId in root.Children)
        {
            if (runtime.Ui.Find(childId) is not { } child)
                continue;
            foreach (var descendant in EnumerateSubtree(runtime, child))
                yield return descendant;
        }
    }

    private static void SetTexture(LuaRuntime runtime, UiObject value)
    {
        var state = runtime.State;
        var texture = EnsureTexture(value);
        ClearTextureAsset(texture);
        if (lua_isnumber(state, 2) != 0)
            texture.FileDataId = unchecked((uint)(int)lua_tonumber(state, 2));
        else if (lua_isstring(state, 2) != 0)
        {
            var asset = lua_tostring(state, 2)!;
            if (!runtime.ApplyAtlas(value, asset, useAtlasSize: false))
                texture.Asset = asset;
        }
        else
            return;

        texture.WrapHorizontal = ReadTextureWrapMode(state, 3, "CLAMP");
        texture.WrapVertical = ReadTextureWrapMode(state, 4, "CLAMP");
        texture.FilterMode = ReadTextureFilterMode(state, 5);
    }

    internal static void ClearTextureAsset(UiTextureState texture)
    {
        texture.IsColor = false;
        texture.Gradient = null;
        texture.FileDataId = null;
        texture.AtlasName = null;
        texture.AtlasWidth = null;
        texture.AtlasHeight = null;
        texture.IntrinsicWidth = null;
        texture.IntrinsicHeight = null;
        texture.SliceData = null;
        texture.Asset = null;
        texture.ClearAtlasRegion();
    }

    private static void ResetTextureToDefaults(UiTextureState texture)
    {
        ClearTextureAsset(texture);
        texture.LegacyMaskAsset = null;
        texture.PortraitUnitToken = null;
        texture.PortraitDisableMasking = false;
        texture.Color = Vector4.One;
        texture.VertexColor = Vector4.One;
        texture.BlendMode = "BLEND";
        texture.WrapHorizontal = "CLAMP";
        texture.WrapVertical = "CLAMP";
        texture.FilterMode = "LINEAR";
        texture.BlockingLoadRequested = false;
        texture.SnapToPixelGrid = true;
        texture.TexelSnappingBias = 0.3f;
        texture.HorizontallyTiled = false;
        texture.VerticallyTiled = false;
        texture.Desaturation = 0;
        texture.Rotation = 0;
        texture.RotationPoint = new Vector2(0.5f, 0.5f);
        texture.ResetTexCoord();
        Array.Clear(texture.VertexOffsets);
    }

    private static string ReadTextureWrapMode(lua_State state, int index, string fallback)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return fallback;
        if (lua_type(state, index) == LUA_TBOOLEAN)
            return lua_toboolean(state, index) != 0 ? "REPEAT" : "CLAMP";
        if (lua_type(state, index) != LUA_TSTRING)
            return fallback;

        var mode = (lua_tostring(state, index) ?? string.Empty).ToUpperInvariant();
        return mode is
            "CLAMP" or
            "REPEAT" or
            "CLAMPTOBLACK" or
            "CLAMPTOBLACKADDITIVE" or
            "CLAMPTOWHITE" or
            "MIRROR"
                ? mode
                : "CLAMP";
    }

    private static string ReadTextureFilterMode(lua_State state, int index)
    {
        var mode = OptionalString(state, index)?.ToUpperInvariant();
        return mode is "NEAREST" or "LINEAR" or "TRILINEAR" ? mode : "LINEAR";
    }

    private static void SetTexCoord(LuaRuntime runtime, UiObject value)
    {
        var state = runtime.State;
        var texture = EnsureTexture(value);
        if (value.MaskTextureIds.Count > 0 || texture.LegacyMaskAsset is not null)
        {
            runtime.Log.Warn(
                "ui",
                $"SetTexCoord: Cannot set tex coords when texture has mask " +
                $"[{runtime.GetDebugName(value, preferParentKey: true)}; " +
                $"{value.SourceLocation}].");
            return;
        }

        var argumentCount = lua_gettop(state) - 1;
        if (argumentCount is not (4 or 8))
        {
            runtime.Log.Warn(
                "ui",
                "SetTextCoord: Usage (minX, maxX, minY, maxY) or " +
                "(ULx, ULy, LLx, LLy, URx, URy, LRx, LRy)");
            return;
        }

        var coordinates = new float[argumentCount];
        for (var index = 0; index < argumentCount; index++)
        {
            coordinates[index] = (float)OptionalNumber(state, index + 2);
            if (coordinates[index] is < -10000 or > 10000)
            {
                runtime.Log.Warn("ui", "SetTextCoord: TexCoord out of range");
                return;
            }
        }

        if (argumentCount == 4)
        {
            var left = coordinates[0];
            var right = coordinates[1];
            var top = coordinates[2];
            var bottom = coordinates[3];
            texture.LocalUv[0] = new(left, top);
            texture.LocalUv[1] = new(left, bottom);
            texture.LocalUv[2] = new(right, top);
            texture.LocalUv[3] = new(right, bottom);
            texture.ResolveUv();
            return;
        }

        for (var index = 0; index < 4; index++)
            texture.LocalUv[index] = new Vector2(
                coordinates[index * 2],
                coordinates[index * 2 + 1]);
        texture.ResolveUv();
    }

    private static int SetSpriteSheetCell(lua_State state, UiTextureState texture)
    {
        const string usage =
            "Usage: self:SetSpriteSheetCell(cell, numRows, numColumns [, cellWidth, cellHeight])";
        if (!TryReadRequiredUInt32(state, 2, out var cell) ||
            !TryReadRequiredUInt32(state, 3, out var rowCount) ||
            !TryReadRequiredUInt32(state, 4, out var columnCount) ||
            !TryReadOptionalUInt32(state, 5, out var cellWidth) ||
            !TryReadOptionalUInt32(state, 6, out var cellHeight))
        {
            return luaL_error(state, usage);
        }

        if (rowCount == 0 || columnCount == 0)
            return 0;

        var normalizedCellWidth = 1f / columnCount;
        var normalizedCellHeight = 1f / rowCount;
        if (cellWidth is { } width &&
            cellHeight is { } height &&
            texture.IntrinsicWidth is > 0 and { } intrinsicWidth &&
            texture.IntrinsicHeight is > 0 and { } intrinsicHeight)
        {
            normalizedCellWidth = width / intrinsicWidth;
            normalizedCellHeight = height / intrinsicHeight;
        }

        var left = (cell % columnCount) * normalizedCellWidth;
        var top = (cell / columnCount) * normalizedCellHeight;
        var right = left + normalizedCellWidth;
        var bottom = top + normalizedCellHeight;
        texture.LocalUv[0] = new Vector2(left, top);
        texture.LocalUv[1] = new Vector2(left, bottom);
        texture.LocalUv[2] = new Vector2(right, top);
        texture.LocalUv[3] = new Vector2(right, bottom);
        texture.ResolveUv();
        return 0;
    }

    private static int SetGradient(lua_State state, UiTextureState texture)
    {
        const string usage = "Usage: self:SetGradient(orientation, minColor, maxColor)";
        var orientation = OptionalString(state, 2);
        if (orientation is null ||
            (!orientation.Equals("HORIZONTAL", StringComparison.OrdinalIgnoreCase) &&
             !orientation.Equals("VERTICAL", StringComparison.OrdinalIgnoreCase)) ||
            !TryReadRequiredColorTable(state, 3, out var minimum) ||
            !TryReadRequiredColorTable(state, 4, out var maximum))
        {
            return luaL_error(state, usage);
        }

        texture.Gradient = (
            orientation.ToUpperInvariant(),
            minimum,
            maximum);
        texture.IsColor = true;
        texture.AtlasName = null;
        texture.AtlasWidth = null;
        texture.AtlasHeight = null;
        texture.Asset = null;
        texture.FileDataId = null;
        texture.ClearAtlasRegion();
        return 0;
    }

    private enum ButtonTextureKind
    {
        Normal,
        Pushed,
        Disabled,
        Highlight,
        Checked,
        DisabledChecked
    }

    private static void SetButtonAtlas(
        LuaRuntime runtime,
        UiObject value,
        ButtonTextureKind kind)
    {
        var state = runtime.State;
        var usage = kind == ButtonTextureKind.Highlight
            ? "Usage: self:SetHighlightAtlas(atlas [, blendMode])"
            : $"Usage: self:Set{kind}Atlas(atlas)";
        if (lua_gettop(state) < 2 || lua_type(state, 2) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return;
        }

        var texture = EnsureButtonTextureObject(runtime, value, kind);
        var atlasName = OptionalString(state, 2);
        ClearTextureAsset(EnsureTexture(texture));
        if (!string.IsNullOrEmpty(atlasName))
            runtime.ApplyAtlas(texture, atlasName, false);

        if (kind == ButtonTextureKind.Highlight)
            EnsureTexture(texture).BlendMode = ReadButtonHighlightBlendMode(state, usage);
        AnchorButtonTexture(value, texture);
        AssignButtonTexture(runtime, value, kind, texture);
        UpdateHighlightTextureAsset(value, kind, texture);
    }

    private static void SetButtonTexture(
        LuaRuntime runtime,
        UiObject value,
        ButtonTextureKind kind)
    {
        var state = runtime.State;
        var usage = kind == ButtonTextureKind.Highlight
            ? "Usage: self:SetHighlightTexture(asset [, blendMode])"
            : $"Usage: self:Set{kind}Texture(asset)";
        if (lua_gettop(state) < 2)
        {
            luaL_error(state, usage);
            return;
        }

        var argumentType = lua_type(state, 2);
        var texture = GetObject(runtime, 2);
        if (texture is not null &&
            !texture.ObjectType.Equals("Texture", StringComparison.OrdinalIgnoreCase))
        {
            luaL_error(state, usage);
            return;
        }

        if (texture is null)
        {
            if (argumentType is not (LUA_TNIL or LUA_TNUMBER or LUA_TSTRING))
            {
                luaL_error(state, usage);
                return;
            }

            texture = EnsureButtonTextureObject(runtime, value, kind);
            ApplyButtonTextureAsset(runtime, texture, 2);
        }

        if (kind == ButtonTextureKind.Highlight)
            EnsureTexture(texture).BlendMode = ReadButtonHighlightBlendMode(state, usage);
        AnchorButtonTexture(value, texture);
        AssignButtonTexture(runtime, value, kind, texture);
        UpdateHighlightTextureAsset(value, kind, texture);
    }

    private static UiObject EnsureButtonTextureObject(
        LuaRuntime runtime,
        UiObject value,
        ButtonTextureKind kind)
    {
        if (ButtonTextureId(value, kind) is { } id &&
            runtime.Ui.Find(id) is { Texture: not null } existing)
        {
            return existing;
        }

        return CreateObject(
            runtime,
            "Texture",
            null,
            value,
            kind == ButtonTextureKind.Highlight ? "HIGHLIGHT" : "ARTWORK");
    }

    private static void AnchorButtonTexture(UiObject button, UiObject texture)
    {
        if (texture.AllPointsTargetId is null && texture.Anchors.Count == 0)
            texture.AllPointsTargetId = button.Id;
    }

    private static string ReadButtonHighlightBlendMode(lua_State state, string usage)
    {
        if (lua_gettop(state) < 3 || lua_isnil(state, 3) != 0)
            return "ADD";

        var blendMode = OptionalString(state, 3)?.ToUpperInvariant();
        if (blendMode is not ("DISABLE" or "BLEND" or "ALPHAKEY" or "ADD" or "MOD"))
        {
            luaL_error(state, usage);
            return "ADD";
        }
        return blendMode;
    }

    private static void ApplyButtonTextureAsset(
        LuaRuntime runtime,
        UiObject textureObject,
        int argumentIndex)
    {
        var state = runtime.State;
        var texture = EnsureTexture(textureObject);
        ClearTextureAsset(texture);
        if (lua_type(state, argumentIndex) == LUA_TNUMBER)
        {
            texture.FileDataId = (uint)Math.Max(0, lua_tonumber(state, argumentIndex));
            return;
        }

        var asset = OptionalString(state, argumentIndex);
        if (asset is null)
            return;

        if (!runtime.ApplyAtlas(textureObject, asset, useAtlasSize: false))
            texture.Asset = asset;
    }

    private static int? ButtonTextureId(UiObject value, ButtonTextureKind kind) =>
        kind switch
        {
            ButtonTextureKind.Normal => value.NormalTextureId,
            ButtonTextureKind.Pushed => value.PushedTextureId,
            ButtonTextureKind.Disabled => value.DisabledTextureId,
            ButtonTextureKind.Highlight => value.HighlightTextureId,
            ButtonTextureKind.Checked => value.CheckedTextureId,
            ButtonTextureKind.DisabledChecked => value.DisabledCheckedTextureId,
            _ => null
        };

    private static void AssignButtonTexture(
        LuaRuntime runtime,
        UiObject value,
        ButtonTextureKind kind,
        UiObject texture)
    {
        var previousId = ButtonTextureId(value, kind);
        var previousShown = previousId is { } previousShownId &&
                            runtime.Ui.Find(previousShownId) is { Shown: true };
        if (previousId is { } oldId &&
            oldId != texture.Id &&
            runtime.Ui.Find(oldId) is { } previous)
        {
            previous.Shown = false;
        }
        if (texture.ParentId != value.Id)
            runtime.Ui.Reparent(texture, value.Id);

        switch (kind)
        {
            case ButtonTextureKind.Normal:
                value.NormalTextureId = texture.Id;
                texture.Shown =
                    previousShown ||
                    value.Enabled && value.ButtonState == UiButtonState.Normal;
                break;
            case ButtonTextureKind.Pushed:
                value.PushedTextureId = texture.Id;
                texture.Shown =
                    previousShown ||
                    value.Enabled && value.ButtonState == UiButtonState.Pushed;
                break;
            case ButtonTextureKind.Disabled:
                value.DisabledTextureId = texture.Id;
                texture.Shown = previousShown || !value.Enabled;
                break;
            case ButtonTextureKind.Highlight:
                value.HighlightTextureId = texture.Id;
                texture.Shown = previousShown || texture.Shown;
                break;
            case ButtonTextureKind.Checked:
                value.CheckedTextureId = texture.Id;
                break;
            case ButtonTextureKind.DisabledChecked:
                value.DisabledCheckedTextureId = texture.Id;
                break;
        }
        if (kind is ButtonTextureKind.Checked or ButtonTextureKind.DisabledChecked)
            runtime.SetCheckButtonChecked(value, value.Checked);
    }

    private static void UpdateHighlightTextureAsset(
        UiObject value,
        ButtonTextureKind kind,
        UiObject texture)
    {
        if (kind != ButtonTextureKind.Highlight)
            return;

        value.HighlightTextureAsset = texture.Texture?.AtlasName is { } atlas
            ? $"atlas:{atlas}"
            : texture.Texture?.FileDataId is { } fileDataId
                ? fileDataId.ToString(CultureInfo.InvariantCulture)
                : texture.Texture?.Asset;
    }

    private static void ClearButtonTexture(
        LuaRuntime runtime,
        UiObject value,
        ButtonTextureKind kind)
    {
        if (ButtonTextureId(value, kind) is not { } id ||
            runtime.Ui.Find(id) is not { Texture: not null } texture)
        {
            return;
        }

        ClearTextureAsset(texture.Texture);
        if (kind == ButtonTextureKind.Highlight)
            texture.Texture.BlendMode = "ADD";
        UpdateHighlightTextureAsset(value, kind, texture);
    }

    internal static bool SetButtonVisualState(
        LuaRuntime runtime,
        UiObject value,
        bool enabled,
        UiButtonState buttonState)
    {
        var oldState = !value.Enabled
            ? 0
            : value.ButtonState == UiButtonState.Pushed
                ? 2
                : 1;
        var newState = !enabled
            ? 0
            : buttonState == UiButtonState.Pushed
                ? 2
                : 1;
        if (oldState == newState)
            return false;

        if (value.ButtonFontStringId is { } fontStringId &&
            runtime.Ui.Find(fontStringId) is { } fontString)
        {
            var pushedOffsetDelta = oldState == 2 && newState != 2
                ? -value.PushedTextOffset
                : oldState != 2 && newState == 2
                    ? value.PushedTextOffset
                    : Vector2.Zero;
            if (pushedOffsetDelta != Vector2.Zero)
            {
                for (var index = 0; index < fontString.Anchors.Count; index++)
                {
                    var anchor = fontString.Anchors[index];
                    fontString.Anchors[index] = anchor with
                    {
                        X = anchor.X + pushedOffsetDelta.X,
                        Y = anchor.Y + pushedOffsetDelta.Y
                    };
                }
                runtime.Ui.InvalidateLayout();
            }
        }

        var oldTextureId = oldState switch
        {
            0 => value.DisabledTextureId,
            1 => value.NormalTextureId,
            2 => value.PushedTextureId,
            _ => null
        };
        var newTextureId = newState switch
        {
            0 => value.DisabledTextureId,
            1 => value.NormalTextureId,
            2 => value.PushedTextureId,
            _ => null
        };
        var oldTexture = oldTextureId is { } oldId ? runtime.Ui.Find(oldId) : null;
        var newTexture = newTextureId is { } newId ? runtime.Ui.Find(newId) : null;

        if (oldTexture is not null && (newTexture is not null || newState == 1))
            oldTexture.Shown = false;
        if (newTexture is not null)
            newTexture.Shown = true;

        value.Enabled = enabled;
        value.ButtonState = enabled && buttonState == UiButtonState.Pushed
            ? UiButtonState.Pushed
            : UiButtonState.Normal;

        if (value.ObjectType.Equals("CheckButton", StringComparison.OrdinalIgnoreCase))
            runtime.SetCheckButtonChecked(value, value.Checked);
        runtime.Ui.UpdateHighlightDrawLayer(value);

        RefreshButtonFont(runtime, value);
        return true;
    }

    private static void SetButtonFontObject(
        LuaRuntime runtime,
        UiObject value,
        string operation)
    {
        var state = runtime.State;
        var fontObject = GetObject(runtime, 2);
        if (fontObject is null && lua_type(state, 2) == LUA_TSTRING)
            fontObject = runtime.Ui.Find(lua_tostring(state, 2)!);
        if (fontObject is null ||
            !fontObject.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase))
        {
            luaL_error(state, $"Usage: self:{operation}(\"fontname\" or fontObject)");
            return;
        }

        switch (operation)
        {
            case "SetNormalFontObject":
                value.NormalFontObjectId = fontObject.Id;
                value.NormalFontObjectName = fontObject.Name;
                break;
            case "SetHighlightFontObject":
                value.HighlightFontObjectId = fontObject.Id;
                value.HighlightFontObjectName = fontObject.Name;
                break;
            case "SetDisabledFontObject":
                value.DisabledFontObjectId = fontObject.Id;
                value.DisabledFontObjectName = fontObject.Name;
                break;
        }
        RefreshButtonFont(runtime, value);
    }

    private static void PushButtonFontObject(
        LuaRuntime runtime,
        int? objectId,
        string? objectName)
    {
        var value = objectId is { } id
            ? runtime.Ui.Find(id)
            : objectName is null
                ? null
                : runtime.Ui.Find(objectName);
        if (value is null)
            lua_pushnil(runtime.State);
        else
            runtime.PushObject(value);
    }

    internal static void InitializeButtonFontString(LuaRuntime runtime, UiObject value)
    {
        if (!IsButton(value))
            return;
        EnsureButtonFontString(runtime, value);
    }

    private static UiObject EnsureButtonFontString(LuaRuntime runtime, UiObject value)
    {
        if (value.ButtonFontStringId is { } fontStringId &&
            runtime.Ui.Find(fontStringId) is { Font: not null } existing)
        {
            existing.TextValue = value.TextValue;
            existing.Font.Text = value.TextValue;
            RefreshButtonFont(runtime, value);
            return existing;
        }

        var fontString = CreateObject(runtime, "FontString", null, value, "OVERLAY");
        fontString.TextValue = value.TextValue;
        fontString.Font!.Text = value.TextValue;
        value.ButtonFontStringId = fontString.Id;
        RefreshButtonFont(runtime, value);
        AnchorButtonFontString(value, fontString);
        return fontString;
    }

    private static void AnchorButtonFontString(UiObject button, UiObject fontString)
    {
        if (fontString.AllPointsTargetId is not null || fontString.Anchors.Count != 0)
            return;

        var point = EnsureFont(fontString).JustifyHorizontal.ToUpperInvariant() switch
        {
            "LEFT" => "LEFT",
            "RIGHT" => "RIGHT",
            _ => "CENTER"
        };
        fontString.Anchors.Add(new UiAnchor(point, button.Id, point, 0, 0));
    }

    internal static void RefreshButtonFont(LuaRuntime runtime, UiObject value)
    {
        if (value.ButtonFontStringId is not { } fontStringId ||
            runtime.Ui.Find(fontStringId) is not { } fontString)
            return;

        var text = fontString.Font?.Text ?? fontString.TextValue;

        var highlighted =
            value.Enabled &&
            (value.HighlightLocked || runtime.Ui.IsMouseMotionFocus(value));
        var sourceId = !value.Enabled
            ? value.DisabledFontObjectId
            : highlighted
                ? value.HighlightFontObjectId
                : value.NormalFontObjectId;
        var sourceName = !value.Enabled
            ? value.DisabledFontObjectName
            : highlighted
                ? value.HighlightFontObjectName
                : value.NormalFontObjectName;
        var source = sourceId is { } id
            ? runtime.Ui.Find(id)
            : sourceName is null
                ? null
                : runtime.Ui.Find(sourceName);
        if (source?.Font is { } sourceFont)
        {
            fontString.Font = CopyFont(sourceFont);
            fontString.FontObjectId = source.Id;
        }
        else if (value.Font is { } buttonFont)
        {
            fontString.Font = CopyFont(buttonFont);
            fontString.FontObjectId = value.FontObjectId;
        }
        else
        {
            fontString.Font ??= new UiFontState();
        }

        fontString.TextValue = text;
        fontString.Font.Text = text;
        runtime.Ui.InvalidateLayout();
    }

    internal static void SetObjectText(LuaRuntime runtime, UiObject value, string text)
    {
        value.TextValue = text;
        if (IsEditBox(value))
        {
            value.CursorPosition = text.Length;
            value.EditBoxCaretStops.Clear();
        }
        if (value.Font is not null)
            value.Font.Text = text;
        if (IsButton(value))
        {
            if (text.Length > 0 || value.ButtonFontStringId is not null)
            {
                var fontString = EnsureButtonFontString(runtime, value);
                fontString.TextValue = text;
                fontString.Font!.Text = text;
            }
        }
    }

    private static string ProcessStoredFontStringText(
        UiObject value,
        string text)
    {
        if (!value.ObjectType.Equals(
                "FontString",
                StringComparison.OrdinalIgnoreCase) &&
            !IsButton(value))
        {
            return text;
        }

        return WowTextMarkup.ProcessStoredGrammar(
            TruncateUtf8(text, 30_000));
    }

    private static bool IsButton(UiObject value) =>
        value.ObjectType.EndsWith("Button", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadLineAnchor(
        LuaRuntime runtime,
        UiObject owner,
        int index,
        out UiAnchor anchor)
    {
        var state = runtime.State;
        anchor = null!;
        if (!TryReadRequiredFramePoint(state, index, out var point))
            return false;

        UiObject? relative;
        var offsetIndex = index + 1;
        if (lua_gettop(state) < offsetIndex)
        {
            relative = runtime.Ui.Find(owner.ParentId ?? runtime.Ui.UiParentId);
        }
        else if (lua_isnil(state, offsetIndex) != 0)
        {
            relative = runtime.Ui.Find(runtime.Ui.UiParentId);
            offsetIndex++;
        }
        else if (lua_type(state, offsetIndex) == LUA_TSTRING)
        {
            relative = runtime.Ui.Find(lua_tostring(state, offsetIndex) ?? string.Empty);
            if (relative is null)
                return false;
            offsetIndex++;
        }
        else
        {
            relative = GetObject(runtime, offsetIndex);
            if (relative is null)
                return false;
            offsetIndex++;
        }

        if (relative?.Id == owner.Id)
            return false;
        var x = 0f;
        var y = 0f;
        if (lua_isnumber(state, offsetIndex) != 0 &&
            lua_isnumber(state, offsetIndex + 1) != 0)
        {
            x = (float)lua_tonumber(state, offsetIndex);
            y = (float)lua_tonumber(state, offsetIndex + 1);
        }
        anchor = new UiAnchor(
            point,
            relative?.Id,
            point,
            x,
            y);
        return true;
    }

    private static int PushLineAnchor(LuaRuntime runtime, UiAnchor? anchor)
    {
        if (anchor is null)
            return 0;

        var state = runtime.State;
        lua_pushstring(state, anchor.Point);
        if (anchor.RelativeToId is null ||
            anchor.RelativeToId == runtime.Ui.UiParentId)
        {
            lua_pushnil(state);
        }
        else
        {
            runtime.PushObject(runtime.Ui.Find(anchor.RelativeToId.Value));
        }
        lua_pushnumber(state, anchor.X);
        lua_pushnumber(state, anchor.Y);
        return 4;
    }

    private static UiTextureState EnsureTexture(UiObject value) =>
        value.Line?.Texture ?? (value.Texture ??= new UiTextureState());

    private static UiFontState EnsureFont(UiObject value)
    {
        if (value.Font is not null)
            return value.Font;

        value.Font = new UiFontState { Text = value.TextValue };
        if (IsEditBox(value))
        {
            value.Font.JustifyHorizontal = "LEFT";
            value.Font.JustifyVertical = value.MultiLine ? "TOP" : "MIDDLE";
            value.Font.WordWrap = value.MultiLine;
        }
        return value.Font;
    }

    internal static void TickMessageFrames(LuaRuntime runtime, float elapsedSeconds)
    {
        foreach (var value in runtime.Ui.Objects.Values
                     .Where(IsMessageFrame)
                     .ToArray())
        {
            RebuildMessageFrameLinePool(runtime, value);

            if (value.Messages.Count > 0 && runtime.Ui.IsVisible(value))
            {
                foreach (var message in value.Messages)
                    InsertMessageFrameMessage(runtime, value, message);
                value.Messages.Clear();
                LayoutMessageFrameLines(runtime, value);
                runtime.Ui.InvalidateLayout();
            }

            if (!value.MessageFading || elapsedSeconds <= 0)
                continue;

            var expiredAny = false;
            foreach (var line in ActiveMessageLines(value).ToArray())
            {
                if (line.TimeVisible != 0)
                {
                    line.TimeVisible -= elapsedSeconds;
                    if (line.TimeVisible >= 0)
                        continue;
                    if (line.FadeDuration != 0)
                    {
                        line.TimeVisible = 0;
                        continue;
                    }
                }
                else if (line.FadeDuration != 0)
                {
                    line.FadeDuration -= elapsedSeconds;
                    if (line.FadeDuration > 0)
                    {
                        var denominator = value.MessageFadeDuration;
                        var normalized = denominator == 0
                            ? 0
                            : Math.Max(0, line.FadeDuration / denominator);
                        if (runtime.Ui.Find(line.FontStringId) is { } fadingFontString)
                        {
                            fadingFontString.Alpha = MathF.Pow(
                                normalized,
                                value.MessageFadePower);
                        }
                        continue;
                    }
                }

                DeactivateMessageLine(runtime, line);
                expiredAny = true;
            }

            if (!expiredAny)
                continue;

            StablePartitionActiveMessageLines(value);
            LayoutMessageFrameLines(runtime, value);
            runtime.Ui.InvalidateLayout();
        }
    }

    private static bool IsMessageFrame(UiObject value) =>
        value.ObjectType.EndsWith("MessageFrame", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<UiMessageFrameLine> ActiveMessageLines(UiObject value) =>
        value.MessageLines.Take(value.MessageLineCapacity).Where(line => line.Active);

    private static UiMessageFrameLine? FindActiveMessageLine(
        LuaRuntime runtime,
        UiObject value,
        uint messageId) =>
        value.MessageLines
            .Take(value.MessageLineCapacity)
            .FirstOrDefault(line =>
                line.Active &&
                line.MessageId == messageId &&
                runtime.Ui.Find(line.FontStringId) is not null);

    private static void RebuildMessageFrameLinePool(LuaRuntime runtime, UiObject value)
    {
        var font = EnsureFont(value);
        font.Text = string.Empty;
        var scale = runtime.Ui.LayoutScale(value);
        var bounds = runtime.Ui.ResolveBounds(value.Id);
        var logicalHeight = scale > 0 ? bounds.Height / scale : 0;
        var logicalWidth = scale > 0 ? bounds.Width / scale : 0;
        var usableHeight = Math.Max(
            0,
            logicalHeight - value.MessageInsets.Top - value.MessageInsets.Bottom);
        var lineAdvance = (font.FontSize + font.Spacing) * font.TextScale;
        var capacity = lineAdvance > 0
            ? Math.Max(0, (int)(usableHeight / lineAdvance))
            : 0;
        if (lineAdvance > 0 &&
            MathF.Abs((capacity + 1) * lineAdvance - usableHeight) <
            0.00000023841858f)
        {
            capacity++;
        }

        if (capacity < value.MessageLineCapacity)
        {
            foreach (var line in value.MessageLines
                         .Skip(capacity)
                         .Take(value.MessageLineCapacity - capacity))
            {
                DeactivateMessageLine(runtime, line);
            }
        }

        while (value.MessageLines.Count < capacity)
        {
            var fontString = CreateObject(runtime, "FontString", null, value, "ARTWORK");
            fontString.Shown = false;
            fontString.Font = CopyFont(font);
            fontString.Font.Text = string.Empty;
            value.MessageLines.Add(new UiMessageFrameLine
            {
                FontStringId = fontString.Id
            });
        }

        value.MessageLineCapacity = capacity;
        var usableWidth = Math.Max(
            0,
            logicalWidth - value.MessageInsets.Left - value.MessageInsets.Right);
        foreach (var line in value.MessageLines.Take(capacity))
        {
            if (runtime.Ui.Find(line.FontStringId) is not { } fontString)
                continue;
            var existingText = fontString.Font?.Text ?? string.Empty;
            var existingColor = fontString.Font?.Color ?? Vector4.One;
            fontString.Font = CopyFont(font);
            fontString.Font.Text = existingText;
            fontString.Font.Color = existingColor;
            fontString.FontObjectId = value.FontObjectId;
            fontString.Width = usableWidth;
            fontString.Height = null;
        }

        LayoutMessageFrameLines(runtime, value);
    }

    private static void InsertMessageFrameMessage(
        LuaRuntime runtime,
        UiObject value,
        UiMessageFrameMessage message)
    {
        if (value.MessageLineCapacity <= 0)
            return;

        RecycleMessageLine(runtime, value, 0);
        var line = value.MessageLines[0];
        if (runtime.Ui.Find(line.FontStringId) is not { Font: { } fontStringFont } fontString)
            return;

        var baseColor = EnsureFont(value).Color;
        fontStringFont.Text = message.Text;
        fontStringFont.Color = new Vector4(
            message.Color?.X ?? baseColor.X,
            message.Color?.Y ?? baseColor.Y,
            message.Color?.Z ?? baseColor.Z,
            message.Alpha is { } alpha ? alpha / 255f : baseColor.W);
        fontString.Alpha = 1;
        fontString.Shown = true;
        line.Active = true;
        line.MessageId = message.MessageId;
        line.TimeVisible = value.MessageTimeVisible;
        line.FadeDuration = value.MessageFadeDuration;

        var wrappedLineCount = MeasureText(runtime, fontString).LineCount;
        if (wrappedLineCount <= 1 || value.MessageLineCapacity <= wrappedLineCount)
            return;

        var reserveIndex = value.MessageInsertMode.Equals(
            "TOP",
            StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
        for (var index = 1; index < wrappedLineCount; index++)
            RecycleMessageLine(runtime, value, reserveIndex);
    }

    private static void RecycleMessageLine(
        LuaRuntime runtime,
        UiObject value,
        int targetIndex)
    {
        var capacity = value.MessageLineCapacity;
        if (capacity <= 0 || targetIndex < 0 || targetIndex >= capacity)
            return;

        var recycled = value.MessageLines[capacity - 1];
        for (var index = capacity - 1; index > targetIndex; index--)
            value.MessageLines[index] = value.MessageLines[index - 1];
        value.MessageLines[targetIndex] = recycled;
        DeactivateMessageLine(runtime, recycled);
    }

    private static void DeactivateMessageLine(
        LuaRuntime runtime,
        UiMessageFrameLine line)
    {
        if (runtime.Ui.Find(line.FontStringId) is { } fontString)
        {
            fontString.Shown = false;
            fontString.Alpha = 1;
        }
        line.Active = false;
        line.MessageId = 0;
        line.TimeVisible = 0;
        line.FadeDuration = 0;
    }

    private static void StablePartitionActiveMessageLines(UiObject value)
    {
        var capacity = value.MessageLineCapacity;
        if (capacity <= 1)
            return;
        var visiblePool = value.MessageLines.Take(capacity).ToArray();
        var next = visiblePool.Where(line => line.Active)
            .Concat(visiblePool.Where(line => !line.Active))
            .ToArray();
        for (var index = 0; index < capacity; index++)
            value.MessageLines[index] = next[index];
    }

    private static void LayoutMessageFrameLines(LuaRuntime runtime, UiObject value)
    {
        var font = EnsureFont(value);
        var lineAdvance = (font.FontSize + font.Spacing) * font.TextScale;
        var bottomInsertion = value.MessageInsertMode.Equals(
            "BOTTOM",
            StringComparison.OrdinalIgnoreCase);
        for (var index = 0; index < value.MessageLineCapacity; index++)
        {
            var row = bottomInsertion
                ? value.MessageLineCapacity - 1 - index
                : index;
            if (runtime.Ui.Find(value.MessageLines[index].FontStringId) is not { } fontString)
                continue;
            fontString.Anchors.Clear();
            fontString.Anchors.Add(new UiAnchor(
                "TOPLEFT",
                value.Id,
                "TOPLEFT",
                value.MessageInsets.Left,
                -(value.MessageInsets.Top + row * lineAdvance)));
        }
    }

    private static void ClearMessageFrame(LuaRuntime runtime, UiObject value)
    {
        foreach (var line in value.MessageLines.Take(value.MessageLineCapacity))
            DeactivateMessageLine(runtime, line);
        value.Messages.Clear();
        EnsureFont(value).Text = string.Empty;
    }

    private static bool IsSimpleHtml(UiObject value) =>
        value.ObjectType.Equals("SimpleHTML", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadSimpleHtmlTextType(
        lua_State state,
        int index,
        out string textType)
    {
        textType = string.Empty;
        if (lua_type(state, index) != LUA_TSTRING)
            return false;
        textType = lua_tostring(state, index)?.ToUpperInvariant() ?? string.Empty;
        return textType is "P" or "H1" or "H2" or "H3";
    }

    private static UiFontState EnsureHtmlFont(UiObject value, string textType)
    {
        if (!value.HtmlFonts.TryGetValue(textType, out var font))
        {
            font = CopyFont(EnsureFont(value));
            value.HtmlFonts[textType] = font;
        }
        if (textType.Equals("P", StringComparison.OrdinalIgnoreCase))
            value.Font = font;
        return font;
    }

    private static bool TryGetCallFont(
        UiObject value,
        lua_State state,
        out UiFontState font)
    {
        if (!IsSimpleHtml(value))
        {
            font = EnsureFont(value);
            return true;
        }
        if (!TryReadSimpleHtmlTextType(state, 2, out var textType))
        {
            font = null!;
            return false;
        }
        font = EnsureHtmlFont(value, textType);
        return true;
    }

    private static void AssignHtmlFontObject(
        LuaRuntime runtime,
        UiObject value,
        string textType,
        UiObject? source)
    {
        var local = EnsureHtmlFont(value, textType);
        var inherited = source?.Font is { } sourceFont
            ? CopyFont(sourceFont)
            : new UiFontState();
        ApplyLocalFontOverrides(local, inherited);
        inherited.Text = textType == "P" ? value.TextValue : local.Text;
        inherited.LocalOverrides = local.LocalOverrides;
        value.HtmlFonts[textType] = inherited;
        if (textType == "P")
            value.Font = inherited;
        if (source is null)
            value.HtmlFontObjectIds.Remove(textType);
        else
            value.HtmlFontObjectIds[textType] = source.Id;
        UpdateSimpleHtmlContentHeight(value);
        runtime.Ui.InvalidateLayout();
    }

    private static void UpdateSimpleHtmlContentHeight(UiObject value)
    {
        if (!IsSimpleHtml(value))
            return;
        RefreshSimpleHtmlContentStyles(value);
        if (value.Owner is not { } ui || value.HtmlContentNodes.Count == 0)
        {
            value.HtmlContentHeight = 0;
            return;
        }

        ui.InvalidateLayout();
        var ownerBounds = ui.ResolveBounds(value.Id);
        var bottom = ownerBounds.Top;
        foreach (var node in value.HtmlContentNodes)
        {
            if (ui.Find(node.RegionId) is { } region)
                bottom = Math.Min(bottom, ui.ResolveBounds(region.Id).Bottom);
        }
        value.HtmlContentHeight = Math.Max(0, ownerBounds.Top - bottom);
    }

    private static void RebuildSimpleHtmlContent(
        LuaRuntime runtime,
        UiObject value,
        string source)
    {
        foreach (var node in value.HtmlContentNodes.ToArray())
            runtime.Ui.RemoveInternalSubtree(node.RegionId);
        value.HtmlContentNodes.Clear();

        var parsed = ParseSimpleHtmlContent(
            source,
            value.HtmlIgnoreMarkup,
            value.HtmlHyperlinkFormat);
        UiObject? previous = null;
        foreach (var node in parsed)
        {
            if (node.IsRule)
            {
                var rule = runtime.Ui.Create("Texture", null, value.Id, "ARTWORK");
                if (!runtime.ApplyAtlas(rule, "Book-line", false))
                {
                    rule.Texture!.IsColor = true;
                    rule.Texture.Color = new Vector4(1, 1, 1, 0.5f);
                    rule.Height = 1;
                }
                AnchorSimpleHtmlRegion(rule, value, previous);
                value.HtmlContentNodes.Add(new UiHtmlContentNode
                {
                    RegionId = rule.Id
                });
                previous = rule;
                continue;
            }

            var fontString = runtime.Ui.Create("FontString", null, value.Id, "ARTWORK");
            var font = CopyFont(EnsureHtmlFont(value, node.TextType));
            font.Text = node.Text;
            font.JustifyHorizontal = node.Align;
            font.JustifyVertical = "TOP";
            fontString.Font = font;
            fontString.TextValue = node.Text;
            if (node.IsSpacer)
                fontString.Height = Math.Max(0, font.FontSize * font.TextScale);
            AnchorSimpleHtmlRegion(fontString, value, previous);
            value.HtmlContentNodes.Add(new UiHtmlContentNode
            {
                RegionId = fontString.Id,
                TextType = node.TextType,
                Align = node.Align
            });
            previous = fontString;
        }

        UpdateSimpleHtmlContentHeight(value);
    }

    private static void AnchorSimpleHtmlRegion(
        UiObject region,
        UiObject owner,
        UiObject? previous)
    {
        if (previous is null)
        {
            region.Anchors.Add(new UiAnchor("TOPLEFT", owner.Id, "TOPLEFT", 0, 0));
            region.Anchors.Add(new UiAnchor("TOPRIGHT", owner.Id, "TOPRIGHT", 0, 0));
            return;
        }

        region.Anchors.Add(new UiAnchor(
            "TOPLEFT",
            previous.Id,
            "BOTTOMLEFT",
            0,
            0));
        region.Anchors.Add(new UiAnchor(
            "TOPRIGHT",
            previous.Id,
            "BOTTOMRIGHT",
            0,
            0));
    }

    private static void RefreshSimpleHtmlContentStyles(UiObject value)
    {
        if (value.Owner is not { } ui)
            return;

        foreach (var node in value.HtmlContentNodes)
        {
            if (node.TextType is null ||
                ui.Find(node.RegionId) is not { Font: { } current } region)
            {
                continue;
            }

            var replacement = CopyFont(EnsureHtmlFont(value, node.TextType));
            replacement.Text = current.Text;
            replacement.JustifyHorizontal = node.Align;
            replacement.JustifyVertical = "TOP";
            region.Font = replacement;
            if (region.Height.HasValue)
                region.Height = Math.Max(0, replacement.FontSize * replacement.TextScale);
        }
    }

    private static IReadOnlyList<SimpleHtmlParsedNode> ParseSimpleHtmlContent(
        string source,
        bool ignoreMarkup,
        string hyperlinkFormat)
    {
        if (ignoreMarkup)
            return SplitSimpleHtmlText(source, "P", "LEFT");

        try
        {
            var document = XDocument.Parse(source, LoadOptions.PreserveWhitespace);
            var root = document.Root;
            if (root is null ||
                !root.Name.LocalName.Equals("HTML", StringComparison.OrdinalIgnoreCase))
            {
                return SplitSimpleHtmlText(source, "P", "LEFT");
            }

            var body = root.Elements().FirstOrDefault(element =>
                element.Name.LocalName.Equals("BODY", StringComparison.OrdinalIgnoreCase));
            if (body is null)
                return SplitSimpleHtmlText(source, "P", "LEFT");

            var result = new List<SimpleHtmlParsedNode>();
            foreach (var element in body.Elements())
            {
                var name = element.Name.LocalName.ToUpperInvariant();
                if (name is "P" or "H1" or "H2" or "H3")
                {
                    var align = ParseSimpleHtmlAlignment(element.Attribute("align")?.Value);
                    var text = NormalizeSimpleHtmlText(
                        BuildSimpleHtmlText(element, hyperlinkFormat));
                    result.AddRange(SplitSimpleHtmlText(text, name, align));
                }
                else if (name == "BR")
                {
                    result.Add(new SimpleHtmlParsedNode(
                        string.Empty,
                        "P",
                        "LEFT",
                        IsSpacer: true));
                }
                else if (name == "HR")
                {
                    result.Add(new SimpleHtmlParsedNode(
                        string.Empty,
                        string.Empty,
                        ParseSimpleHtmlAlignment(element.Attribute("align")?.Value, "CENTER"),
                        IsRule: true));
                }
            }
            return result;
        }
        catch
        {
            return SplitSimpleHtmlText(source, "P", "LEFT");
        }
    }

    private static string BuildSimpleHtmlText(XElement element, string hyperlinkFormat)
    {
        var builder = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;
                case XElement child when
                    child.Name.LocalName.Equals("BR", StringComparison.OrdinalIgnoreCase):
                    builder.Append("|n");
                    break;
                case XElement child when
                    child.Name.LocalName.Equals("A", StringComparison.OrdinalIgnoreCase):
                    {
                        var href = child.Attribute("href")?.Value;
                        var label = child.Value;
                        if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(label))
                            builder.Append(FormatSimpleHtmlHyperlink(hyperlinkFormat, href, label));
                        break;
                    }
            }
        }
        return builder.ToString();
    }

    private static string FormatSimpleHtmlHyperlink(
        string format,
        string href,
        string label)
    {
        var first = format.IndexOf("%s", StringComparison.Ordinal);
        if (first < 0)
            return format;
        var result = format[..first] + href + format[(first + 2)..];
        var second = result.IndexOf("%s", first + href.Length, StringComparison.Ordinal);
        return second < 0
            ? result
            : result[..second] + label + result[(second + 2)..];
    }

    private static string NormalizeSimpleHtmlText(string text)
    {
        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '|' &&
                index + 1 < text.Length &&
                text[index + 1] == 'n')
            {
                if (builder.Length > 0 && builder[^1] == ' ')
                    builder.Length--;
                builder.Append("|n");
                index++;
                pendingSpace = false;
                continue;
            }

            if (char.IsWhiteSpace(text[index]))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace &&
                !(builder.Length >= 2 &&
                  builder[^2] == '|' &&
                  builder[^1] == 'n'))
            {
                builder.Append(' ');
            }
            pendingSpace = false;
            builder.Append(text[index]);
        }
        if (builder.Length > 0 && builder[^1] == ' ')
            builder.Length--;
        return builder.ToString();
    }

    private static IReadOnlyList<SimpleHtmlParsedNode> SplitSimpleHtmlText(
        string text,
        string textType,
        string align)
    {
        const int maximumChunkLength = 0x1fff;
        if (text.Length <= maximumChunkLength)
            return [new SimpleHtmlParsedNode(text, textType, align)];

        var result = new List<SimpleHtmlParsedNode>();
        var remaining = text;
        while (remaining.Length > maximumChunkLength)
        {
            var split = remaining.LastIndexOf("|n", maximumChunkLength, StringComparison.Ordinal);
            var skip = 2;
            if (split <= 0)
            {
                split = remaining.LastIndexOf(' ', maximumChunkLength);
                skip = 1;
            }
            if (split <= 0)
            {
                split = maximumChunkLength;
                skip = 0;
            }
            result.Add(new SimpleHtmlParsedNode(remaining[..split], textType, align));
            remaining = remaining[(split + skip)..];
        }
        result.Add(new SimpleHtmlParsedNode(remaining, textType, align));
        return result;
    }

    private static string ParseSimpleHtmlAlignment(
        string? value,
        string defaultValue = "LEFT") =>
        value?.ToUpperInvariant() switch
        {
            "LEFT" => "LEFT",
            "CENTER" => "CENTER",
            "RIGHT" => "RIGHT",
            _ => defaultValue
        };

    private sealed record SimpleHtmlParsedNode(
        string Text,
        string TextType,
        string Align,
        bool IsSpacer = false,
        bool IsRule = false);

    private static bool WouldCreateFontObjectLoop(
        LuaRuntime runtime,
        UiObject value,
        UiObject source)
    {
        var current = source;
        var visited = new HashSet<int>();
        while (visited.Add(current.Id))
        {
            if (current.Id == value.Id)
                return true;
            if (current.FontObjectId is not { } parentId ||
                runtime.Ui.Find(parentId) is not { } parent)
            {
                return false;
            }
            current = parent;
        }
        return true;
    }

    private static UiObject? ResolveFontObjectForAlphabet(
        LuaRuntime runtime,
        UiObject value,
        int alphabet)
    {
        UiObject? current = value;
        var visited = new HashSet<int>();
        while (current is not null && visited.Add(current.Id))
        {
            if (current.FontFamilyMemberIds[alphabet] is { } memberId)
                return runtime.Ui.Find(memberId);
            current = current.FontObjectId is { } parentId
                ? runtime.Ui.Find(parentId)
                : null;
        }
        return null;
    }

    private static bool TryParseFontAlphabet(string? value, out int alphabet)
    {
        alphabet = value?.ToLowerInvariant() switch
        {
            "roman" => 0,
            "korean" => 1,
            "simplifiedchinese" => 2,
            "traditionalchinese" => 3,
            "russian" => 4,
            _ => -1
        };
        return alphabet >= 0;
    }

    private static string FontAlphabetName(int alphabet) => alphabet switch
    {
        0 => "Roman",
        1 => "Korean",
        2 => "SimplifiedChinese",
        3 => "TraditionalChinese",
        4 => "Russian",
        _ => "unknown"
    };

    internal static int CurrentFontAlphabet(LuaRuntime runtime)
    {
        return runtime.Localization.CurrentLocale switch
        {
            WowClientLocale.KoKR => 1,
            WowClientLocale.ZhCN => 2,
            WowClientLocale.ZhTW => 3,
            WowClientLocale.RuRU => 4,
            _ => 0
        };
    }

    private static void CopyFontObjectState(
        LuaRuntime runtime,
        UiObject destination,
        UiObject source)
    {
        destination.Font = source.Font is { } sourceFont
            ? CopyFont(sourceFont)
            : new UiFontState();
        Array.Clear(destination.FontFamilyMemberIds);
        for (var alphabet = 0; alphabet < source.FontFamilyMemberIds.Length; alphabet++)
        {
            if (source.FontFamilyMemberIds[alphabet] is not { } sourceMemberId ||
                runtime.Ui.Find(sourceMemberId) is not { } sourceMember)
            {
                continue;
            }

            var memberCopy = CreateObject(runtime, "Font", null, null);
            CopyFontObjectState(runtime, memberCopy, sourceMember);
            destination.FontFamilyMemberIds[alphabet] = memberCopy.Id;
        }
        destination.FontObjectId = source.FontObjectId;
        PropagateFontInheritance(runtime, destination);
    }

    internal static void AssignFontObject(
        LuaRuntime runtime,
        UiObject value,
        UiObject? source)
    {
        var local = EnsureFont(value);
        var inherited = source?.Font is { } sourceFont
            ? CopyFont(sourceFont)
            : new UiFontState();
        if (!inherited.HasLocalJustifyHorizontal)
        {
            inherited.JustifyHorizontal = local.JustifyHorizontal;
            inherited.HasLocalJustifyHorizontal = local.HasLocalJustifyHorizontal;
        }
        if (!inherited.HasLocalJustifyVertical)
        {
            inherited.JustifyVertical = local.JustifyVertical;
            inherited.HasLocalJustifyVertical = local.HasLocalJustifyVertical;
        }
        ApplyLocalFontOverrides(local, inherited);
        inherited.Text = local.Text;
        inherited.LocalOverrides = local.LocalOverrides;
        value.Font = inherited;
        value.FontObjectId = source?.Id;
        PropagateFontInheritance(runtime, value);
        runtime.Ui.InvalidateLayout();
    }

    private static void ApplyLocalFontOverrides(UiFontState local, UiFontState target)
    {
        var overrides = local.LocalOverrides;
        if ((overrides & UiFontOverrides.FontPath) != 0)
        {
            target.FontPath = local.FontPath;
            target.IsConfigured = local.IsConfigured;
        }
        if ((overrides & UiFontOverrides.FontSize) != 0)
            target.FontSize = local.FontSize;
        if ((overrides & UiFontOverrides.FontFlags) != 0)
            target.FontFlags = local.FontFlags;
        if ((overrides & UiFontOverrides.TextScale) != 0)
            target.TextScale = local.TextScale;
        if ((overrides & UiFontOverrides.Color) != 0)
            target.Color = local.Color;
        if ((overrides & UiFontOverrides.ShadowColor) != 0)
            target.ShadowColor = local.ShadowColor;
        if ((overrides & UiFontOverrides.ShadowOffset) != 0)
            target.ShadowOffset = local.ShadowOffset;
        if ((overrides & UiFontOverrides.JustifyHorizontal) != 0)
            target.JustifyHorizontal = local.JustifyHorizontal;
        if ((overrides & UiFontOverrides.JustifyVertical) != 0)
            target.JustifyVertical = local.JustifyVertical;
        if ((overrides & UiFontOverrides.Spacing) != 0)
            target.Spacing = local.Spacing;
        if ((overrides & UiFontOverrides.MaximumLines) != 0)
            target.MaximumLines = local.MaximumLines;
        if ((overrides & UiFontOverrides.IndentedWordWrap) != 0)
            target.IndentedWordWrap = local.IndentedWordWrap;
        if ((overrides & UiFontOverrides.WordWrap) != 0)
            target.WordWrap = local.WordWrap;
        if ((overrides & UiFontOverrides.NonSpaceWrap) != 0)
            target.NonSpaceWrap = local.NonSpaceWrap;
        if ((overrides & UiFontOverrides.CanBeUserScaled) != 0)
            target.CanBeUserScaled = local.CanBeUserScaled;
        target.HasLocalJustifyHorizontal |=
            (overrides & UiFontOverrides.JustifyHorizontal) != 0;
        target.HasLocalJustifyVertical |=
            (overrides & UiFontOverrides.JustifyVertical) != 0;
    }

    private static void MarkFontOverride(
        LuaRuntime runtime,
        UiObject owner,
        UiFontState font,
        UiFontOverrides value)
    {
        font.LocalOverrides |= value;
        if (ReferenceEquals(owner.Font, font))
        {
            PropagateFontInheritance(runtime, owner);
            if (IsButton(owner))
                RefreshButtonFont(runtime, owner);
        }
    }

    private static void PropagateFontInheritance(LuaRuntime runtime, UiObject source)
    {
        var dependents = runtime.Ui.FontDependents(source.Id);
        foreach (var dependent in dependents)
        {
            var local = EnsureFont(dependent);
            var inherited = source.Font is { } sourceFont
                ? CopyFont(sourceFont)
                : new UiFontState();
            ApplyLocalFontOverrides(local, inherited);
            inherited.Text = local.Text;
            inherited.LocalOverrides = local.LocalOverrides;
            dependent.Font = inherited;
            PropagateFontInheritance(runtime, dependent);
        }
    }

    private static UiFontState CopyFont(UiFontState source) =>
        new()
        {
            Text = source.Text,
            FontPath = source.FontPath,
            FontSize = source.FontSize,
            FontFlags = source.FontFlags,
            TextScale = source.TextScale,
            Color = source.Color,
            ShadowColor = source.ShadowColor,
            ShadowOffset = source.ShadowOffset,
            JustifyHorizontal = source.JustifyHorizontal,
            JustifyVertical = source.JustifyVertical,
            HasLocalJustifyHorizontal = source.HasLocalJustifyHorizontal,
            HasLocalJustifyVertical = source.HasLocalJustifyVertical,
            Spacing = source.Spacing,
            MaximumLines = source.MaximumLines,
            IndentedWordWrap = source.IndentedWordWrap,
            WordWrap = source.WordWrap,
            NonSpaceWrap = source.NonSpaceWrap,
            CanBeUserScaled = source.CanBeUserScaled,
            IsConfigured = source.IsConfigured,
            LocalOverrides = source.LocalOverrides
        };

    private enum ColorSelectTexturePart
    {
        Wheel,
        WheelThumb,
        Value,
        ValueThumb,
        Alpha,
        AlphaThumb
    }

    private static UiObject? ColorSelectTexture(LuaRuntime runtime, int? id) =>
        id is { } textureId ? runtime.Ui.Find(textureId) : null;

    private static int PushColorSelectTexture(LuaRuntime runtime, int? id)
    {
        runtime.PushObject(ColorSelectTexture(runtime, id));
        return 1;
    }

    private static int SetColorSelectSourceTexture(
        LuaRuntime runtime,
        UiObject owner,
        ColorSelectTexturePart part)
    {
        var state = runtime.State;
        var operation = part switch
        {
            ColorSelectTexturePart.Wheel => "SetColorWheelTexture",
            ColorSelectTexturePart.Value => "SetColorValueTexture",
            _ => "SetColorAlphaTexture"
        };
        var usage = $"Usage: self:{operation}(texture)";
        var texture = GetObject(runtime, 2);
        if (texture?.Texture is null)
            return luaL_error(state, usage);

        AttachColorSelectTexture(runtime, owner, texture, part);
        RefreshColorSelectVisuals(runtime, owner);
        return 0;
    }

    private static int SetColorSelectThumbTexture(
        LuaRuntime runtime,
        UiObject owner,
        ColorSelectTexturePart part)
    {
        var state = runtime.State;
        var operation = part switch
        {
            ColorSelectTexturePart.WheelThumb => "SetColorWheelThumbTexture",
            ColorSelectTexturePart.ValueThumb => "SetColorValueThumbTexture",
            _ => "SetColorAlphaThumbTexture"
        };
        var usage = $"Usage: self:{operation}(texture)";
        var argumentType = lua_type(state, 2);
        if (argumentType is not (LUA_TNUMBER or LUA_TSTRING))
            return luaL_error(state, usage);

        var colorSelect = EnsureColorSelect(owner);
        var previousId = part switch
        {
            ColorSelectTexturePart.WheelThumb => colorSelect.WheelThumbTextureId,
            ColorSelectTexturePart.ValueThumb => colorSelect.ValueThumbTextureId,
            _ => colorSelect.AlphaThumbTextureId
        };
        var texture = ColorSelectTexture(runtime, previousId) ??
                      CreateObject(runtime, "Texture", null, owner, "OVERLAY");
        var textureState = EnsureTexture(texture);
        ClearTextureAsset(textureState);
        if (argumentType == LUA_TNUMBER)
        {
            if (!TryReadRequiredUInt32(state, 2, out var fileDataId))
                return luaL_error(state, usage);
            textureState.FileDataId = fileDataId;
        }
        else
        {
            textureState.Asset = OptionalString(state, 2);
        }

        AttachColorSelectTexture(runtime, owner, texture, part);
        RefreshColorSelectVisuals(runtime, owner);
        return 0;
    }

    private static void AttachColorSelectTexture(
        LuaRuntime runtime,
        UiObject owner,
        UiObject texture,
        ColorSelectTexturePart part)
    {
        var colorSelect = EnsureColorSelect(owner);
        var previousId = part switch
        {
            ColorSelectTexturePart.Wheel => colorSelect.WheelTextureId,
            ColorSelectTexturePart.WheelThumb => colorSelect.WheelThumbTextureId,
            ColorSelectTexturePart.Value => colorSelect.ValueTextureId,
            ColorSelectTexturePart.ValueThumb => colorSelect.ValueThumbTextureId,
            ColorSelectTexturePart.Alpha => colorSelect.AlphaTextureId,
            _ => colorSelect.AlphaThumbTextureId
        };
        if (previousId is { } oldId &&
            oldId != texture.Id &&
            runtime.Ui.Find(oldId) is { } previous)
        {
            previous.Shown = false;
        }

        if (texture.ParentId != owner.Id)
            runtime.Ui.Reparent(texture, owner.Id);
        texture.DrawLayer = part is ColorSelectTexturePart.WheelThumb or
            ColorSelectTexturePart.ValueThumb or ColorSelectTexturePart.AlphaThumb
            ? "OVERLAY"
            : "ARTWORK";
        texture.SubLevel = 0;
        texture.Shown = true;
        var textureState = EnsureTexture(texture);
        textureState.IsColorSelectWheel = part == ColorSelectTexturePart.Wheel;
        if (part is ColorSelectTexturePart.Wheel or
            ColorSelectTexturePart.Value or ColorSelectTexturePart.Alpha)
        {
            ClearTextureAsset(textureState);
        }

        switch (part)
        {
            case ColorSelectTexturePart.Wheel:
                colorSelect.WheelTextureId = texture.Id;
                break;
            case ColorSelectTexturePart.WheelThumb:
                colorSelect.WheelThumbTextureId = texture.Id;
                break;
            case ColorSelectTexturePart.Value:
                colorSelect.ValueTextureId = texture.Id;
                break;
            case ColorSelectTexturePart.ValueThumb:
                colorSelect.ValueThumbTextureId = texture.Id;
                break;
            case ColorSelectTexturePart.Alpha:
                colorSelect.AlphaTextureId = texture.Id;
                break;
            case ColorSelectTexturePart.AlphaThumb:
                colorSelect.AlphaThumbTextureId = texture.Id;
                break;
        }
    }

    internal static void RefreshColorSelectVisuals(
        LuaRuntime runtime,
        UiObject owner)
    {
        if (owner.ColorSelect is not { } colorSelect)
            return;

        if (ColorSelectTexture(runtime, colorSelect.WheelTextureId) is { Texture: { } wheel })
            wheel.IsColorSelectWheel = true;

        var fullRgb = QuantizedColorSelectRgb(colorSelect);
        var fullValueRgb = QuantizedHsvToRgb(
            colorSelect.Hue,
            colorSelect.Saturation,
            1);
        if (ColorSelectTexture(runtime, colorSelect.ValueTextureId) is { Texture: { } value })
        {
            value.IsColorSelectWheel = false;
            value.IsColor = true;
            value.Color = Vector4.One;
            value.VertexColor = Vector4.One;
            value.Gradient = (
                "VERTICAL",
                new Vector4(0, 0, 0, 1),
                new Vector4(fullValueRgb, 1));
        }
        if (ColorSelectTexture(runtime, colorSelect.AlphaTextureId) is { Texture: { } alpha })
        {
            alpha.IsColorSelectWheel = false;
            alpha.IsColor = true;
            alpha.Color = Vector4.One;
            alpha.VertexColor = Vector4.One;
            alpha.Gradient = (
                "VERTICAL",
                new Vector4(fullRgb, 0),
                new Vector4(fullRgb, 1));
        }
        runtime.Ui.InvalidateLayout();
    }

    private static void CommitColorSelect(LuaRuntime runtime, UiObject owner)
    {
        var colorSelect = EnsureColorSelect(owner);
        colorSelect.Dirty = false;
        RefreshColorSelectVisuals(runtime, owner);
        var rgb = QuantizedColorSelectRgb(colorSelect);
        runtime.InvokeScript(owner, "OnColorSelect", rgb.X, rgb.Y, rgb.Z);
    }

    internal static void TickColorSelects(LuaRuntime runtime)
    {
        foreach (var value in runtime.Ui.Objects.Values
                     .Where(value =>
                         value.ColorSelect is { Dirty: true } &&
                         runtime.Ui.IsVisible(value))
                     .ToArray())
        {
            CommitColorSelect(runtime, value);
        }
    }

    internal static bool BeginColorSelectInteraction(
        LuaRuntime runtime,
        UiObject owner,
        Vector2 cursor)
    {
        if (owner.ColorSelect is not { } colorSelect)
            return false;

        colorSelect.SelectingWheel = IsColorSelectTextureHit(
            runtime,
            colorSelect.WheelTextureId,
            cursor);
        colorSelect.SelectingValue = IsColorSelectTextureHit(
            runtime,
            colorSelect.ValueTextureId,
            cursor);
        colorSelect.SelectingAlpha = IsColorSelectTextureHit(
            runtime,
            colorSelect.AlphaTextureId,
            cursor);
        UpdateColorSelectFromCursor(runtime, owner, cursor);
        return colorSelect.SelectingWheel ||
               colorSelect.SelectingValue ||
               colorSelect.SelectingAlpha;
    }

    internal static void EndColorSelectInteraction(UiObject owner)
    {
        if (owner.ColorSelect is not { } colorSelect)
            return;
        colorSelect.SelectingWheel = false;
        colorSelect.SelectingValue = false;
        colorSelect.SelectingAlpha = false;
    }

    internal static void UpdateColorSelectFromCursor(
        LuaRuntime runtime,
        UiObject owner,
        Vector2 cursor)
    {
        if (owner.ColorSelect is not { } colorSelect)
            return;
        var changed = false;
        if (colorSelect.SelectingWheel &&
            colorSelect.WheelTextureId is { } wheelId)
        {
            var bounds = runtime.Ui.ResolveBounds(wheelId);
            if (bounds.Width > float.Epsilon && bounds.Height > float.Epsilon)
            {
                var x = (cursor.X - bounds.Center.X) * 2 / bounds.Width;
                var y = (cursor.Y - bounds.Center.Y) * 2 / bounds.Height;
                colorSelect.Hue =
                    (MathF.Atan2(y, x) + MathF.PI) * 180 / MathF.PI;
                colorSelect.Saturation = MathF.Min(
                    MathF.Sqrt(x * x + y * y),
                    1);
                changed = true;
            }
        }
        if (colorSelect.SelectingValue &&
            colorSelect.ValueTextureId is { } valueId)
        {
            var bounds = runtime.Ui.ResolveBounds(valueId);
            if (bounds.Height > float.Epsilon)
            {
                colorSelect.Value = Math.Clamp(
                    (cursor.Y - bounds.Bottom) / bounds.Height,
                    0,
                    1);
                changed = true;
            }
        }
        if (colorSelect.SelectingAlpha &&
            colorSelect.AlphaTextureId is { } alphaId)
        {
            var bounds = runtime.Ui.ResolveBounds(alphaId);
            if (bounds.Height > float.Epsilon)
            {
                colorSelect.Alpha = Math.Clamp(
                    (cursor.Y - bounds.Bottom) / bounds.Height,
                    0,
                    1);
                changed = true;
            }
        }
        if (!changed)
            return;
        colorSelect.Dirty = true;
        runtime.Ui.InvalidateLayout();
    }

    private static bool IsColorSelectTextureHit(
        LuaRuntime runtime,
        int? textureId,
        Vector2 cursor) =>
        textureId is { } id &&
        runtime.Ui.Find(id) is { } texture &&
        runtime.Ui.IsVisible(texture) &&
        runtime.Ui.ResolveBounds(id).Contains(cursor);

    private static float QuantizeColorSelectInput(float value)
    {
        var clamped = Math.Clamp(value, 0, 1);
        return MathF.Floor(clamped * 255 + 0.5f) / 255;
    }

    private static Vector3 QuantizedColorSelectRgb(UiColorSelectState value) =>
        QuantizedHsvToRgb(
            value.Hue,
            value.Saturation,
            value.Value);

    private static Vector3 QuantizedHsvToRgb(
        float hue,
        float saturation,
        float value)
    {
        var raw = HsvToRgb(hue, saturation, value);
        return new Vector3(
            MathF.Truncate(raw.X * 255) / 255,
            MathF.Truncate(raw.Y * 255) / 255,
            MathF.Truncate(raw.Z * 255) / 255);
    }

    private static Vector3 HsvToRgb(float hue, float saturation, float value)
    {
        if ((hue == -1 && saturation == 0) || saturation == 0)
            return new Vector3(value);
        if (hue < 0 || hue > 360 ||
            saturation < 0 || saturation > 1 ||
            value < 0 || value > 1)
        {
            return Vector3.Zero;
        }

        var scaled = hue / 60;
        var sector = Math.Min((int)scaled, 5);
        var fraction = scaled - sector;
        var p = (1 - saturation) * value;
        var q = (1 - fraction * saturation) * value;
        var t = (1 - (1 - fraction) * saturation) * value;
        return sector switch
        {
            0 => new Vector3(value, t, p),
            1 => new Vector3(q, value, p),
            2 => new Vector3(p, value, t),
            3 => new Vector3(p, q, value),
            4 => new Vector3(t, p, value),
            _ => new Vector3(value, p, q)
        };
    }

    private static void SetColorSelectRgb(
        UiColorSelectState colorSelect,
        Vector3 rgb)
    {
        var maximum = MathF.Max(rgb.X, MathF.Max(rgb.Y, rgb.Z));
        var minimum = MathF.Min(rgb.X, MathF.Min(rgb.Y, rgb.Z));
        var delta = maximum - minimum;
        colorSelect.Value = maximum;
        if (maximum == 0 || delta == 0)
        {
            colorSelect.Hue = -1;
            colorSelect.Saturation = 0;
            return;
        }

        colorSelect.Saturation = delta / maximum;
        float hue;
        if (maximum == rgb.X)
            hue = (rgb.Y - rgb.Z) / delta;
        else if (maximum == rgb.Y)
            hue = (rgb.Z - rgb.X) / delta + 2;
        else
            hue = (rgb.X - rgb.Y) / delta + 4;
        hue *= 60;
        if (hue < 0)
            hue += 360;
        colorSelect.Hue = hue;
    }

    private static UiLineState EnsureLine(UiObject value) =>
        value.Line ??= new UiLineState();

    private static UiStatusBarState EnsureStatusBar(UiObject value) =>
        value.StatusBar ??= new UiStatusBarState();

    private static UiCooldownState EnsureCooldown(UiObject value) =>
        value.Cooldown ??= new UiCooldownState();

    private static UiColorSelectState EnsureColorSelect(UiObject value) =>
        value.ColorSelect ??= new UiColorSelectState();

    private static UiBlobState EnsureBlob(UiObject value) =>
        value.Blob ??= new UiBlobState();

    private static UiMinimapState EnsureMinimap(UiObject value) =>
        value.Minimap ??= new UiMinimapState();

    private static UiFogOfWarState EnsureFogOfWar(UiObject value) =>
        value.FogOfWar ??= new UiFogOfWarState();

    private static UiObject EnsureFogOfWarBackgroundTexture(
        LuaRuntime runtime,
        UiObject owner)
    {
        var fog = EnsureFogOfWar(owner);
        var texture = fog.BackgroundTextureId is { } textureId
            ? runtime.Ui.Find(textureId)
            : null;
        if (texture is null)
        {
            texture = CreateObject(runtime, "Texture", null, owner, "ARTWORK");
            texture.AllPointsTargetId = owner.Id;
            fog.BackgroundTextureId = texture.Id;
        }

        texture.DrawLayer = "ARTWORK";
        texture.SubLevel = 0;
        texture.Shown = true;
        AttachFogOfWarMasks(runtime, fog, texture);
        return texture;
    }

    private static IReadOnlyList<UiObject> EnsureFogOfWarMaskTextures(
        LuaRuntime runtime,
        UiObject owner)
    {
        var fog = EnsureFogOfWar(owner);
        var result = new UiObject[fog.MaskTextureIds.Length];
        for (var index = 0; index < fog.MaskTextureIds.Length; index++)
        {
            var mask = fog.MaskTextureIds[index] is { } maskId
                ? runtime.Ui.Find(maskId)
                : null;
            if (mask is null)
            {
                mask = CreateObject(runtime, "MaskTexture", null, owner, "ARTWORK");
                mask.Shown = false;
                fog.MaskTextureIds[index] = mask.Id;
            }

            mask.DrawLayer = "ARTWORK";
            mask.SubLevel = 0;
            result[index] = mask;
        }

        if (fog.BackgroundTextureId is { } backgroundId &&
            runtime.Ui.Find(backgroundId) is { } background)
        {
            AttachFogOfWarMasks(runtime, fog, background);
        }

        return result;
    }

    private static void AttachFogOfWarMasks(
        LuaRuntime runtime,
        UiFogOfWarState fog,
        UiObject background)
    {
        foreach (var maskId in fog.MaskTextureIds)
        {
            if (maskId is not { } id ||
                runtime.Ui.Find(id) is not { ObjectType: "MaskTexture" })
            {
                continue;
            }

            if (!background.MaskTextureIds.Contains(id))
                background.MaskTextureIds.Add(id);
        }
    }

    private static void SynchronizeFogOfWarBackgroundTexture(
        LuaRuntime runtime,
        UiObject owner)
    {
        var fog = EnsureFogOfWar(owner);
        var texture = EnsureFogOfWarBackgroundTexture(runtime, owner);
        var textureState = EnsureTexture(texture);
        ClearTextureAsset(textureState);
        textureState.HorizontallyTiled = fog.BackgroundTextureTilesHorizontally;
        textureState.VerticallyTiled = fog.BackgroundTextureTilesVertically;
        textureState.WrapHorizontal = fog.BackgroundTextureTilesHorizontally
            ? "REPEAT"
            : "CLAMP";
        textureState.WrapVertical = fog.BackgroundTextureTilesVertically
            ? "REPEAT"
            : "CLAMP";

        if (!string.IsNullOrEmpty(fog.BackgroundAtlas))
        {
            runtime.ApplyAtlas(texture, fog.BackgroundAtlas, false);
        }
        else if (fog.BackgroundTextureFileDataId is { } fileDataId)
        {
            textureState.FileDataId = fileDataId;
        }
        else
        {
            textureState.Asset = fog.BackgroundTexture;
        }

        runtime.Ui.InvalidateLayout();
    }

    private static void SynchronizeFogOfWarMaskTextures(
        LuaRuntime runtime,
        UiObject owner)
    {
        var fog = EnsureFogOfWar(owner);
        foreach (var mask in EnsureFogOfWarMaskTextures(runtime, owner))
        {
            var textureState = EnsureTexture(mask);
            ClearTextureAsset(textureState);
            textureState.HorizontallyTiled = false;
            textureState.VerticallyTiled = false;
            textureState.WrapHorizontal = "CLAMP";
            textureState.WrapVertical = "CLAMP";
            if (!string.IsNullOrEmpty(fog.MaskAtlas))
            {
                runtime.ApplyAtlas(mask, fog.MaskAtlas, false);
            }
            else if (fog.MaskTextureFileDataId is { } fileDataId)
            {
                textureState.FileDataId = fileDataId;
            }
            else
            {
                textureState.Asset = fog.MaskTexture;
            }
        }

        runtime.Ui.InvalidateLayout();
    }

    internal static void TickFogOfWarFrames(LuaRuntime runtime)
    {
        foreach (var owner in runtime.Ui.Objects.Values
                     .Where(value =>
                         value.FogOfWar is not null &&
                         runtime.Ui.IsVisible(value))
                     .ToArray())
        {
            UpdateFogOfWarMasks(runtime, owner);
        }
    }

    private static void UpdateFogOfWarMasks(LuaRuntime runtime, UiObject owner)
    {
        var fog = EnsureFogOfWar(owner);
        if (fog.BackgroundTextureId is not { } backgroundId ||
            runtime.Ui.Find(backgroundId) is not { } background ||
            fog.MaskTextureIds.Any(id => id is null) ||
            background.MaskTextureIds.Count == 0)
        {
            return;
        }

        var units = FogOfWarRevealUnits(runtime);
        for (var index = 0; index < fog.MaskTextureIds.Length; index++)
        {
            var mask = runtime.Ui.Find(fog.MaskTextureIds[index]!.Value)!;
            if (index >= units.Count ||
                units[index]?.Position is not { } position ||
                !TryResolveFogOfWarMaskRect(
                    runtime,
                    owner,
                    fog,
                    position,
                    out var center,
                    out var size))
            {
                mask.Shown = false;
                continue;
            }

            mask.Anchors.Clear();
            mask.AllPointsTargetId = null;
            mask.Anchors.Add(
                new UiAnchor(
                    "CENTER",
                    owner.Id,
                    "BOTTOMLEFT",
                    center.X,
                    center.Y));
            mask.Width = size.X;
            mask.Height = size.Y;
            mask.Shown = true;
        }

        runtime.Ui.InvalidateLayout();
    }

    private static IReadOnlyList<WowUnitState?> FogOfWarRevealUnits(LuaRuntime runtime)
    {
        var category = runtime.Group.Instance.IsPresent
            ? runtime.Group.Instance
            : runtime.Group.Home.IsPresent
                ? runtime.Group.Home
                : null;
        if (category is null)
            return [runtime.Units.Player];

        var categoryId = ReferenceEquals(category, runtime.Group.Instance)
            ? (int)WowPartyCategory.Instance
            : (int)WowPartyCategory.Home;
        var count = Math.Min(category.GroupMemberCount, 3);
        var result = new WowUnitState?[count];
        for (var index = 1; index <= count; index++)
        {
            result[index - 1] = runtime.Units.All.Values.FirstOrDefault(
                                    unit =>
                                        unit.RaidIndexByPartyCategory.TryGetValue(
                                            categoryId,
                                            out var categoryIndex) &&
                                        categoryIndex == index) ??
                                runtime.Units.All.Values.FirstOrDefault(
                                    unit => unit.RaidIndex == index) ??
                                (index == 1
                                    ? runtime.Units.Player
                                    : runtime.Units.Find($"party{index - 1}"));
        }

        return result;
    }

    private static bool TryResolveFogOfWarMaskRect(
        LuaRuntime runtime,
        UiObject owner,
        UiFogOfWarState fog,
        WowUnitPositionState unit,
        out Vector2 center,
        out Vector2 size)
    {
        center = default;
        size = default;
        if (runtime.MapProvider is not { } mapProvider)
            return false;

        const double nativeMaskDiameter = 200;
        if (!mapProvider.TryProjectWorldPositionBounds(
                fog.UiMapId,
                unit.MapId,
                unit.X,
                unit.Y,
                nativeMaskDiameter * fog.MaskScalar,
                out var first,
                out var second))
        {
            return false;
        }

        var scale = runtime.Ui.LayoutScale(owner);
        if (scale <= float.Epsilon)
            return false;
        var bounds = runtime.Ui.ResolveBounds(owner.Id);
        var localWidth = bounds.Width / scale;
        var localHeight = bounds.Height / scale;
        size = new Vector2(
            (float)(Math.Abs(second.X - first.X) * localWidth),
            (float)(Math.Abs(second.Y - first.Y) * localHeight));
        if (size.X <= float.Epsilon || size.Y <= float.Epsilon)
            return false;

        center = new Vector2(
            (float)((first.X + second.X) * 0.5 * localWidth),
            (float)((1 - (first.Y + second.Y) * 0.5) * localHeight));
        return true;
    }

    internal static void TickUnitPositionFrames(LuaRuntime runtime)
    {
        foreach (var owner in runtime.Ui.Objects.Values
                     .Where(value =>
                         value.UnitPosition is not null &&
                         runtime.Ui.IsVisible(value))
                     .ToArray())
        {
            UpdateUnitPositionTextures(runtime, owner, force: false);
            UpdateUnitPositionPlayerPing(runtime, owner);
        }
    }

    internal static void TickSimpleModelSequences(
        LuaRuntime runtime,
        double elapsedSeconds)
    {
        var loadedModels = runtime.Ui.Objects.Values
            .Where(value => value.ModelResourceLoaded)
            .ToArray();
        var elapsedMilliseconds = elapsedSeconds * 1000;
        if (elapsedMilliseconds <= 0)
            return;

        foreach (var model in loadedModels)
            TickCharacterRotationAnimation(runtime, model);

        foreach (var model in loadedModels.Where(
                     value => value.ModelPendingAnimationRequests.Count > 0))
        {
            PromoteReadyModelAnimationRequests(runtime, model);
        }

        var activeModels = loadedModels
            .Where(value => !value.ModelPaused)
            .ToArray();
        foreach (var model in activeModels)
            model.ModelGlobalSequenceElapsedMilliseconds += elapsedMilliseconds;

        foreach (var model in activeModels.Where(
                     value => value.ModelSequenceBlendState is not null))
        {
            TickModelSequenceBlendState(model, elapsedMilliseconds);
        }

        foreach (var model in activeModels.Where(value => value.ModelSequencePlaying))
        {
            var duration = model.ModelResolvedSequenceDurationMilliseconds;
            if (duration == 0)
            {
                model.ModelSequencePlaying = false;
                continue;
            }

            var previousElapsed = model.ModelSequenceElapsedMilliseconds;
            model.ModelSequencePlaybackClockMilliseconds += elapsedMilliseconds;
            var elapsedTicks = Math.Floor(
                model.ModelSequencePlaybackClockMilliseconds);
            var currentElapsed =
                model.ModelSequenceInitialElapsedMilliseconds +
                (int)((float)elapsedTicks * model.ModelSequencePlaybackSpeed);
            model.ModelSequenceElapsedMilliseconds = currentElapsed;
            var playbackRevision = model.ModelSequencePlaybackRevision;

            if (!model.ModelSequenceLoops)
            {
                var totalDuration = unchecked(
                    duration * Math.Max(model.ModelSequenceRepeatCount, 1));
                var completed = model.ModelSequencePlaybackSpeed < 0
                    ? previousElapsed > 0 && currentElapsed <= 0
                    : previousElapsed < totalDuration &&
                      currentElapsed >= totalDuration;
                if (completed)
                {
                    model.ModelSequenceElapsedMilliseconds =
                        model.ModelSequencePlaybackSpeed < 0
                            ? 0
                            : totalDuration;
                    model.ModelSequencePlaying = false;
                    runtime.InvokeModelSceneScript(model, "OnAnimFinished");
                }
                continue;
            }

            var previousCycle = Math.Floor(
                previousElapsed / duration);
            var currentCycle = Math.Floor(
                currentElapsed / duration);
            var completedCycles = (int)Math.Min(
                Math.Abs(currentCycle - previousCycle),
                100);
            for (var index = 0; index < completedCycles; index++)
            {
                runtime.InvokeModelSceneScript(model, "OnAnimFinished");
                if (model.ModelSequencePlaybackRevision != playbackRevision ||
                    !model.ModelResourceLoaded ||
                    !model.ModelSequencePlaying)
                {
                    break;
                }
            }
        }

        foreach (var model in activeModels)
            TickCharacterModelAnimationKit(runtime, model, elapsedMilliseconds);

        foreach (var model in activeModels)
        {
            if (!model.ModelHasCustomCamera &&
                model.ModelSelectedCameraIndex is { } cameraIndex &&
                cameraIndex < model.ModelCameras.Count)
            {
                ApplySimpleModelCameraSnapshot(
                    model,
                    model.ModelCameras[(int)cameraIndex]);
            }
        }
    }

    private static void TickModelSequenceBlendState(
        UiObject model,
        double elapsedMilliseconds)
    {
        var state = model.ModelSequenceBlendState;
        if (state is null)
            return;

        state.TransitionElapsedMilliseconds += elapsedMilliseconds;
        if (state.SequencePlaying)
        {
            state.SequencePlaybackClockMilliseconds += elapsedMilliseconds;
            var elapsedTicks = Math.Floor(
                state.SequencePlaybackClockMilliseconds);
            var currentElapsed =
                state.SequenceInitialElapsedMilliseconds +
                (int)((float)elapsedTicks * state.SequencePlaybackSpeed);
            state.SequenceElapsedMilliseconds = currentElapsed;

            if (!state.SequenceLoops)
            {
                var totalDuration = unchecked(
                    state.SequenceDurationMilliseconds *
                    Math.Max(state.SequenceRepeatCount, 1));
                var completed = state.SequencePlaybackSpeed < 0
                    ? currentElapsed <= 0
                    : currentElapsed >= totalDuration;
                if (completed)
                {
                    state.SequenceElapsedMilliseconds =
                        state.SequencePlaybackSpeed < 0
                            ? 0
                            : totalDuration;
                    state.SequencePlaying = false;
                }
            }
        }

        var transitionEnd = state.TransitionEndOffsetMilliseconds != 0
            ? state.TransitionEndOffsetMilliseconds
            : state.TransitionDurationMilliseconds;
        if (state.TransitionElapsedMilliseconds >= transitionEnd)
        {
            model.ModelSequenceBlendState = null;
        }
    }

    internal static void UpdateSimpleModelTransforms(LuaRuntime runtime)
    {
        foreach (var model in runtime.Ui.Objects.Values
                     .Where(value => value.ModelResourceLoaded)
                     .ToArray())
        {
            UpdateAutomaticModelTransform(runtime, model);
            if (model.ModelCharacterCameraActive)
                RefreshCharacterModelCamera(runtime, model);
            UpdateSimpleModelRenderCameraState(model);
        }
    }

    internal static void TickMinimaps(LuaRuntime runtime)
    {
        foreach (var minimap in runtime.Ui.Objects.Values
                     .Where(value => value.Minimap is not null)
                     .Select(value => value.Minimap!))
        {
            if (!minimap.PingActive)
                continue;

            minimap.PingElapsed =
                (float)Math.Max(runtime.Time - minimap.PingStartedAt, 0);
            if (minimap.PingElapsed > minimap.PingDuration)
                minimap.PingActive = false;
        }
    }

    private static void PingMinimapLocation(
        LuaRuntime runtime,
        UiObject owner,
        UiMinimapState minimap,
        float localX,
        float localY)
    {
        if (runtime.Units.Player.Position is not { } playerPosition)
            return;

        var ownerScale = runtime.Ui.LayoutScale(owner);
        if (ownerScale <= float.Epsilon)
            return;
        var bounds = runtime.Ui.ResolveBounds(owner.Id);
        var width = bounds.Width / ownerScale;
        var height = bounds.Height / ownerScale;
        if (width <= float.Epsilon || height <= float.Epsilon)
            return;

        var radius = GetMinimapWorldRadius(runtime);
        minimap.PingWorldX =
            playerPosition.X + localY * (radius * 2 / width);
        minimap.PingWorldY =
            playerPosition.Y - localX * (radius * 2 / height);
        minimap.PingWorldMapId = playerPosition.MapId;
        minimap.HasPingWorldPosition = true;
        minimap.PingActive = true;
        minimap.PingStartedAt = runtime.Time;
        minimap.PingElapsed = 0;
        minimap.PingDuration = 5;

        ResolveMinimapPingPosition(
            runtime,
            minimap,
            out var normalizedX,
            out var normalizedY);
        runtime.TriggerEvent(
            "MINIMAP_PING",
            "player",
            normalizedX,
            normalizedY);
    }

    private static void ResolveMinimapPingPosition(
        LuaRuntime runtime,
        UiMinimapState minimap,
        out float normalizedX,
        out float normalizedY)
    {
        normalizedX = 0;
        normalizedY = 0;
        if (runtime.Units.Player.Position is not { } playerPosition)
            return;

        var radius = GetMinimapWorldRadius(runtime);
        if (radius <= float.Epsilon)
            return;

        normalizedX =
            -(minimap.PingWorldY - playerPosition.Y) * (0.5f / radius);
        normalizedY =
            (minimap.PingWorldX - playerPosition.X) * (0.5f / radius);
    }

    private static float GetMinimapWorldRadius(LuaRuntime runtime)
    {
        var zoom = Math.Clamp(
            runtime.Minimap.Zoom,
            0,
            StandardMinimapWorldRadii.Length - 1);
        return StandardMinimapWorldRadii[zoom];
    }

    private static void UpdateUnitPositionTextures(
        LuaRuntime runtime,
        UiObject owner,
        bool force)
    {
        var state = EnsureUnitPosition(owner);
        if (!state.UnitsFinalized && !force)
            return;

        var ownerScale = runtime.Ui.LayoutScale(owner);
        if (ownerScale <= float.Epsilon)
            return;
        var ownerBounds = runtime.Ui.ResolveBounds(owner.Id);
        var localWidth = ownerBounds.Width / ownerScale;
        var localHeight = ownerBounds.Height / ownerScale;

        foreach (var entry in state.Units.Values)
        {
            var texture = EnsureUnitPositionUnitTexture(
                runtime,
                owner,
                state,
                entry,
                out var created);
            var unit = runtime.Units.All.Values.FirstOrDefault(
                candidate => candidate.Guid.Equals(
                    entry.UnitGuid,
                    StringComparison.OrdinalIgnoreCase));
            if (unit?.Position is not { } position ||
                !TryResolveUnitPositionPoint(
                    runtime,
                    owner,
                    state.UiMapId,
                    position,
                    out var center))
            {
                texture.Shown = false;
                continue;
            }

            if (created || force)
                SynchronizeUnitPositionUnitTexture(runtime, texture, entry);

            var textureState = EnsureTexture(texture);
            var requestedWidth = entry.Width;
            var requestedHeight = entry.Height;
            if (requestedWidth == 0 &&
                requestedHeight == 0 &&
                textureState.AtlasName is not null)
            {
                requestedWidth = textureState.AtlasWidth.GetValueOrDefault();
                requestedHeight = textureState.AtlasHeight.GetValueOrDefault();
            }

            texture.Width = Math.Clamp(requestedWidth, 0, localWidth);
            texture.Height = Math.Clamp(requestedHeight, 0, localHeight);
            texture.DrawLayer = "ARTWORK";
            texture.SubLevel = entry.SubLayer;
            texture.VertexColor = entry.Color;
            textureState.VertexColor = entry.Color;
            textureState.Rotation = entry.ShowFacing ? position.Facing : 0;
            textureState.RotationPoint = new Vector2(0.5f, 0.5f);
            texture.Anchors.Clear();
            texture.AllPointsTargetId = null;
            texture.Anchors.Add(
                new UiAnchor(
                    "CENTER",
                    owner.Id,
                    "BOTTOMLEFT",
                    center.X,
                    center.Y));
            texture.Shown = true;
        }

        runtime.Ui.InvalidateLayout();
        state.MouseOverUnits.Clear();
        if (!ownerBounds.Contains(runtime.Ui.CursorPosition))
            return;

        foreach (var entry in state.Units.Values)
        {
            if (entry.TextureId is not { } textureId ||
                runtime.Ui.Find(textureId) is not { Shown: true } texture ||
                !runtime.Ui.ResolveBounds(texture.Id).Contains(runtime.Ui.CursorPosition))
            {
                continue;
            }
            state.MouseOverUnits.Add(entry.Unit);
        }
    }

    private static UiObject EnsureUnitPositionUnitTexture(
        LuaRuntime runtime,
        UiObject owner,
        UiUnitPositionState state,
        UiUnitPositionEntry entry,
        out bool created)
    {
        var texture = entry.TextureId is { } textureId
            ? runtime.Ui.Find(textureId)
            : null;
        created = texture is null;
        if (texture is not null)
            return texture;

        if (state.UnitTexturePool.Count > 0)
        {
            var last = state.UnitTexturePool.Count - 1;
            texture = runtime.Ui.Find(state.UnitTexturePool[last]);
            state.UnitTexturePool.RemoveAt(last);
        }
        texture ??= CreateObject(runtime, "Texture", null, owner, "ARTWORK");
        entry.TextureId = texture.Id;
        texture.Scale = 1;
        return texture;
    }

    private static void SynchronizeUnitPositionUnitTexture(
        LuaRuntime runtime,
        UiObject texture,
        UiUnitPositionEntry entry)
    {
        var textureState = EnsureTexture(texture);
        ClearTextureAsset(textureState);
        textureState.ResetTexCoord();
        textureState.WrapHorizontal = "CLAMP";
        textureState.WrapVertical = "CLAMP";
        textureState.FilterMode = "LINEAR";
        if (entry.FileDataId is { } fileDataId)
        {
            textureState.FileDataId = fileDataId;
        }
        else if (!string.IsNullOrEmpty(entry.Asset) &&
                 !runtime.ApplyAtlas(texture, entry.Asset, false))
        {
            textureState.Asset = entry.Asset;
        }
    }

    private static void SynchronizeUnitPositionPingTextures(
        LuaRuntime runtime,
        UiObject owner)
    {
        var state = EnsureUnitPosition(owner);
        if (state.PlayerPingTextureIds[0] is null)
        {
            for (var index = 0; index < state.PlayerPingTextureIds.Length; index++)
            {
                var texture = CreateObject(
                    runtime,
                    "Texture",
                    null,
                    owner,
                    "ARTWORK");
                texture.Shown = state.PlayerPingActive;
                state.PlayerPingTextureIds[index] = texture.Id;
            }

            var center = runtime.Ui.Find(state.PlayerPingTextureIds[0]!.Value)!;
            for (var index = 1; index < state.PlayerPingTextureIds.Length; index++)
            {
                var texture = runtime.Ui.Find(state.PlayerPingTextureIds[index]!.Value)!;
                texture.Anchors.Add(
                    new UiAnchor("CENTER", center.Id, "CENTER", 0, 0));
            }
        }

        foreach (var (textureType, descriptor) in state.PlayerPingTextures)
        {
            if (textureType is < 0 or > 2 ||
                state.PlayerPingTextureIds[textureType] is not { } textureId ||
                runtime.Ui.Find(textureId) is not { } texture)
            {
                continue;
            }

            var textureState = EnsureTexture(texture);
            ClearTextureAsset(textureState);
            textureState.ResetTexCoord();
            textureState.WrapHorizontal = "CLAMP";
            textureState.WrapVertical = "CLAMP";
            textureState.FilterMode = "LINEAR";
            if (descriptor.FileDataId is { } fileDataId)
                textureState.FileDataId = fileDataId;
            else
                textureState.Asset = descriptor.Asset;
            texture.Width = descriptor.Width;
            texture.Height = descriptor.Height;
        }

        runtime.Ui.InvalidateLayout();
    }

    private static void UpdateUnitPositionPlayerPing(
        LuaRuntime runtime,
        UiObject owner)
    {
        var state = EnsureUnitPosition(owner);
        if (!state.PlayerPingActive)
            return;

        var elapsed = Math.Max(runtime.Time - state.PlayerPingStartedAt, 0);
        if (elapsed >= state.PlayerPingDuration ||
            runtime.Units.Player.Position is not { } playerPosition ||
            !TryResolveUnitPositionPoint(
                runtime,
                owner,
                state.UiMapId,
                playerPosition,
                out var center))
        {
            state.PlayerPingActive = false;
            state.PlayerPingStartedAt = 0;
            HideUnitPositionPingTextures(runtime, state);
            return;
        }

        var fadeStart = state.PlayerPingDuration - state.PlayerPingFadeDuration;
        var alpha = state.PlayerPingFadeDuration > 0 && elapsed > fadeStart
            ? 1 - (float)((elapsed - fadeStart) / state.PlayerPingFadeDuration)
            : 1;
        alpha = Math.Clamp(alpha, 0, 1);

        if (state.PlayerPingTextureIds[0] is not { } centerId ||
            runtime.Ui.Find(centerId) is not { } centerTexture)
        {
            return;
        }

        centerTexture.Anchors.Clear();
        centerTexture.AllPointsTargetId = null;
        centerTexture.Anchors.Add(
            new UiAnchor(
                "CENTER",
                owner.Id,
                "BOTTOMLEFT",
                center.X / state.PlayerPingScale,
                center.Y / state.PlayerPingScale));

        for (var index = 0; index < state.PlayerPingTextureIds.Length; index++)
        {
            if (state.PlayerPingTextureIds[index] is not { } textureId ||
                runtime.Ui.Find(textureId) is not { } texture)
            {
                continue;
            }

            texture.Scale = state.PlayerPingScale;
            texture.Alpha = alpha;
            texture.Shown = true;
            if (index == 2)
            {
                EnsureTexture(texture).Rotation =
                    -(float)(elapsed % 1.5 / 1.5 * Math.Tau);
            }
        }

        runtime.Ui.InvalidateLayout();
    }

    private static void HideUnitPositionPingTextures(
        LuaRuntime runtime,
        UiUnitPositionState state)
    {
        foreach (var textureId in state.PlayerPingTextureIds)
        {
            if (textureId is { } id && runtime.Ui.Find(id) is { } texture)
                texture.Shown = false;
        }
        runtime.Ui.InvalidateLayout();
    }

    private static bool TryResolveUnitPositionPoint(
        LuaRuntime runtime,
        UiObject owner,
        int uiMapId,
        WowUnitPositionState position,
        out Vector2 center)
    {
        center = default;
        if (runtime.MapProvider?.TryProjectWorldPosition(
                uiMapId,
                position.MapId,
                position.X,
                position.Y,
                out var projected) != true)
        {
            return false;
        }

        var scale = runtime.Ui.LayoutScale(owner);
        if (scale <= float.Epsilon)
            return false;
        var bounds = runtime.Ui.ResolveBounds(owner.Id);
        center = new Vector2(
            (float)(projected.X * bounds.Width / scale),
            (float)((1 - projected.Y) * bounds.Height / scale));
        return true;
    }

    private static UiUnitPositionState EnsureUnitPosition(UiObject value) =>
        value.UnitPosition ??= new UiUnitPositionState();

    private static UiMovieState EnsureMovie(UiObject value) =>
        value.Movie ??= new UiMovieState();

    private static ModelSceneState EnsureModelScene(UiObject value) =>
        value.ModelScene ??= new ModelSceneState();

    private static float GetSimpleModelCameraFacing(UiObject value)
    {
        var delta = value.ModelCameraPosition - value.ModelCameraTarget;
        return delta.X == 0 && delta.Y == 0
            ? 0
            : MathF.Atan2(delta.Y, delta.X);
    }

    private static bool TryBuildSimpleModelExplicitTransform(
        Vector3 translation,
        Vector3 rotation,
        float scale,
        out Matrix4x4 matrix)
    {
        var (sinX, cosX) = MathF.SinCos(rotation.X);
        var (sinY, cosY) = MathF.SinCos(rotation.Y);
        var (sinZ, cosZ) = MathF.SinCos(rotation.Z);

        matrix = new Matrix4x4(
            scale * cosY * cosZ,
            scale * (cosY * sinZ * cosX + sinY * sinX),
            scale * (cosY * sinZ * sinX - sinY * cosX),
            0,
            -scale * sinZ,
            scale * cosZ * cosX,
            scale * cosZ * sinX,
            0,
            scale * sinY * cosZ,
            scale * (sinY * sinZ * cosX - cosY * sinX),
            scale * (sinY * sinZ * sinX + cosY * cosX),
            0,
            translation.X,
            translation.Y,
            translation.Z,
            1);

        return IsAcceptedM2TransformMatrix(matrix);
    }

    private static float GetSimpleModelCoordinateScale(
        LuaRuntime runtime,
        UiObject value) =>
        value.ModelScale *
        (IsCharacterModelWidget(value)
            ? value.ModelDisplayScaleMultiplier
            : 1) *
        runtime.Ui.EffectiveScale(value) *
        runtime.Ui.NormalizedScreenHeight *
        1.6666666f;

    private static bool IsCharacterModelWidget(UiObject value) =>
        value.ObjectType.Equals("PlayerModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("CharacterModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("TabardModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("DressUpModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("CinematicModel", StringComparison.OrdinalIgnoreCase);

    private static void UpdateAutomaticModelTransform(
        LuaRuntime runtime,
        UiObject value)
    {
        if (value.ObjectType.Equals(
                "ModelSceneActor",
                StringComparison.OrdinalIgnoreCase))
        {
            UpdateModelSceneActorAutomaticTransform(runtime, value);
            return;
        }

        UpdateSimpleModelAutomaticTransform(runtime, value);
    }

    private static void UpdateSimpleModelAutomaticTransform(
        LuaRuntime runtime,
        UiObject value)
    {
        if (value.ModelTransformEnabled || !HasLoadedModel(value))
            return;

        var matrix = CreateSimpleModelAutomaticTransform(
            runtime,
            value,
            value.ModelYaw);

        if (!IsAcceptedM2TransformMatrix(matrix))
            return;

        value.ModelTransformMatrix = matrix;
        value.ModelWorldScale = matrix.M11;
    }

    private static Matrix4x4 CreateSimpleModelAutomaticTransform(
        LuaRuntime runtime,
        UiObject value,
        float yaw)
    {
        var factor = GetSimpleModelCoordinateScale(runtime, value);
        var scaledCenter = value.ModelCenter * factor;
        var matrix =
            Matrix4x4.CreateTranslation(-scaledCenter) *
            Matrix4x4.CreateFromQuaternion(
                Quaternion.CreateFromYawPitchRoll(
                    yaw,
                    value.ModelPitch,
                    value.ModelRoll));
        if (!value.ModelUseCenterToTransform)
            matrix *= Matrix4x4.CreateTranslation(scaledCenter);
        matrix *= Matrix4x4.CreateTranslation(value.ModelPosition * factor);

        ScaleModelTransformBasis(ref matrix, factor);
        return matrix;
    }

    private static void UpdateModelSceneActorAutomaticTransform(
        LuaRuntime runtime,
        UiObject value)
    {
        if (value.ModelTransformEnabled || !HasLoadedModel(value))
            return;

        var factor = GetSimpleModelCoordinateScale(runtime, value);
        var scaledCenter = value.ModelCenter * factor;
        var matrix =
            Matrix4x4.CreateTranslation(-scaledCenter) *
            Matrix4x4.CreateFromQuaternion(
                Quaternion.CreateFromYawPitchRoll(
                    value.ModelYaw,
                    value.ModelPitch,
                    value.ModelRoll));

        if (!value.ModelUseCenterForOriginX ||
            !value.ModelUseCenterForOriginY ||
            !value.ModelUseCenterForOriginZ)
        {
            matrix *= Matrix4x4.CreateTranslation(
                value.ModelUseCenterForOriginX ? 0 : scaledCenter.X,
                value.ModelUseCenterForOriginY ? 0 : scaledCenter.Y,
                value.ModelUseCenterForOriginZ ? 0 : scaledCenter.Z);
        }
        matrix *= Matrix4x4.CreateTranslation(value.ModelPosition * factor);

        ScaleModelTransformBasis(ref matrix, factor);
        if (!IsAcceptedM2TransformMatrix(matrix))
            return;

        value.ModelTransformMatrix = matrix;
        value.ModelWorldScale = matrix.M11;
    }

    private static void ScaleModelTransformBasis(
        ref Matrix4x4 matrix,
        float factor)
    {
        matrix.M11 *= factor;
        matrix.M12 *= factor;
        matrix.M13 *= factor;
        matrix.M21 *= factor;
        matrix.M22 *= factor;
        matrix.M23 *= factor;
        matrix.M31 *= factor;
        matrix.M32 *= factor;
        matrix.M33 *= factor;
    }

    private static bool IsAcceptedM2TransformMatrix(Matrix4x4 matrix) =>
        float.IsFinite(matrix.M11) &&
        float.IsFinite(matrix.M21) &&
        float.IsFinite(matrix.M31) &&
        float.IsFinite(matrix.M41) &&
        matrix.M11 * matrix.M11 +
            matrix.M12 * matrix.M12 +
            matrix.M13 * matrix.M13 > 1.1920929e-7f;

    private static void RefreshCharacterModelCamera(
        LuaRuntime runtime,
        UiObject value)
    {
        if (!HasLoadedModel(value) ||
            !IsCharacterModelCameraSurface(value) ||
            value.ModelTransformEnabled)
            return;

        SampledModelCamera camera;
        if (TryGetModelCameraByLookupId(value, 1, out var normalCamera))
        {
            camera = SampleSimpleModelCamera(value, normalCamera);
            camera = TransformSampledModelCamera(
                camera,
                CreateSimpleModelAutomaticTransform(runtime, value, 0));
            camera = camera with
            {
                Position = camera.Position - value.ModelPosition,
                Target = camera.Target - value.ModelPosition
            };
        }
        else
        {
            var factor = GetSimpleModelCoordinateScale(runtime, value);
            camera = new SampledModelCamera(
                new Vector3(5.5555558f, 0, 2.4166667f) * factor,
                value.ModelCenter * factor,
                0,
                .5f,
                1 / 36f,
                0);
        }

        if (value.ModelPortraitZoom != 0 &&
            TryGetModelCameraByLookupId(value, 0, out var portraitCamera))
        {
            var portrait = SampleSimpleModelCamera(value, portraitCamera);
            portrait = TransformSampledModelCamera(
                portrait,
                CreateSimpleModelAutomaticTransform(runtime, value, 0));
            var zoom = value.ModelPortraitZoom;
            camera = camera with
            {
                Position = Vector3.Lerp(
                    camera.Position,
                    portrait.Position - value.ModelPosition,
                    zoom),
                Target = Vector3.Lerp(
                    camera.Target,
                    portrait.Target - value.ModelPosition,
                    zoom),
                FieldOfView = NormalizeCharacterModelFieldOfView(
                    camera.FieldOfView +
                    (portrait.FieldOfView - camera.FieldOfView) * zoom)
            };
        }

        if (value.ModelCamDistanceScale != 1)
        {
            var delta = camera.Position - camera.Target;
            var scale = MathF.Max(value.ModelCamDistanceScale, .1f);
            var scaledDelta = delta * scale;
            if (scaledDelta.LengthSquared() < .010000001f)
                scaledDelta = Vector3.Normalize(delta) * .1f;
            camera = camera with
            {
                Position = camera.Target + scaledDelta
            };
        }

        ApplySampledModelCamera(value, camera);
        value.ModelCharacterCameraActive = true;
        value.ModelHasCurrentCamera = true;
        value.ModelHasCustomCamera = false;
        value.ModelCameraIndex = null;
        value.ModelSelectedCameraIndex = null;
    }

    private static SampledModelCamera TransformSampledModelCamera(
        SampledModelCamera camera,
        Matrix4x4 transform) =>
        camera with
        {
            Position = Vector3.Transform(camera.Position, transform),
            Target = Vector3.Transform(camera.Target, transform)
        };

    private static bool IsCharacterModelCameraSurface(UiObject value) =>
        value.ObjectType.Equals("PlayerModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("DressUpModel", StringComparison.OrdinalIgnoreCase) ||
        value.ObjectType.Equals("TabardModel", StringComparison.OrdinalIgnoreCase);

    private static bool IsCharacterModelSurface(UiObject value) =>
        MatchesObjectType(value, "PlayerModel");

    private static bool TryGetModelCameraByLookupId(
        UiObject value,
        uint lookupId,
        out WowModelCameraMetadata camera)
    {
        camera = null!;
        if (lookupId >= value.ModelCameraLookupIndices.Count)
            return false;

        var cameraIndex = value.ModelCameraLookupIndices[(int)lookupId];
        if (cameraIndex == ushort.MaxValue || cameraIndex >= value.ModelCameras.Count)
            return false;

        camera = value.ModelCameras[cameraIndex];
        return true;
    }

    private static float NormalizeCharacterModelFieldOfView(float value) =>
        value is > 0 and < MathF.PI ? value : MathF.PI / 2;

    private static void UpdateSimpleModelRenderCameraState(UiObject value)
    {
        if (!value.ModelHasCurrentCamera)
        {
            value.ModelRenderCameraState = null;
            return;
        }

        if (value.ModelCameraNearClip is > 0.1f and < 0.30000001f)
            value.ModelCameraNearClip = 0.1f;

        var forward = NormalizeModelCameraVector(
            value.ModelCameraTarget - value.ModelCameraPosition);
        var (sinRoll, cosRoll) = MathF.SinCos(value.ModelCameraRoll);
        var swapSeed =
            MathF.Abs(
                MathF.Abs(
                    (forward.X + forward.Y) * sinRoll +
                    forward.Z * cosRoll) -
                1) < float.Epsilon;
        var seedUp = swapSeed
            ? new Vector3(sinRoll, cosRoll, sinRoll)
            : new Vector3(sinRoll, sinRoll, cosRoll);
        var initialRight = Vector3.Cross(seedUp, forward);
        var up = NormalizeModelCameraVector(
            Vector3.Cross(forward, initialRight));
        var right = NormalizeModelCameraVector(Vector3.Cross(up, forward));

        var fieldOfView = value.ModelCameraFieldOfView >= 0
            ? MathF.Min(value.ModelCameraFieldOfView, MathF.Tau)
            : 0;
        var nearClip = value.ModelCameraNearClip > 0
            ? value.ModelCameraNearClip
            : 0;
        var farClip = value.ModelCameraFarClip > 0
            ? value.ModelCameraFarClip
            : 0;
        value.ModelRenderCameraState = new UiModelRenderCameraState(
            forward,
            right,
            up,
            value.ModelCameraPosition,
            fieldOfView,
            nearClip,
            farClip,
            1);
    }

    private static Vector3 NormalizeModelCameraVector(Vector3 value)
    {
        var lengthSquared = value.LengthSquared();
        return lengthSquared > 2.3841858e-7f
            ? value / MathF.Sqrt(lengthSquared)
            : value;
    }

    private static float GetSimpleModelCameraDistance(UiObject value)
    {
        var delta = value.ModelCameraPosition - value.ModelCameraTarget;
        return MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
    }

    private static void SetSimpleModelCameraPosition(
        UiObject value,
        Vector3 requestedPosition)
    {
        if (!value.ModelHasCustomCamera)
            return;

        var delta = requestedPosition - value.ModelCameraTarget;
        var distanceSquared = delta.X * delta.X + delta.Y * delta.Y;
        if (distanceSquared >= 0.010000001f)
        {
            value.ModelCameraPosition = requestedPosition;
            value.ModelCameraDistance = MathF.Sqrt(distanceSquared);
            return;
        }

        var facing = GetSimpleModelCameraFacing(value);
        value.ModelCameraPosition = new Vector3(
            value.ModelCameraTarget.X + MathF.Cos(facing) * 0.1f,
            value.ModelCameraTarget.Y + MathF.Sin(facing) * 0.1f,
            requestedPosition.Z);
        value.ModelCameraDistance = 0.1f;
    }

    private static void SetSimpleModelCameraTarget(
        UiObject value,
        Vector3 requestedTarget)
    {
        if (!value.ModelHasCustomCamera)
            return;

        var delta = value.ModelCameraPosition - requestedTarget;
        var distanceSquared = delta.X * delta.X + delta.Y * delta.Y;
        if (distanceSquared >= 0.010000001f)
        {
            value.ModelCameraTarget = requestedTarget;
            value.ModelCameraDistance = MathF.Sqrt(distanceSquared);
            return;
        }

        var facing = GetSimpleModelCameraFacing(value);
        value.ModelCameraTarget = new Vector3(
            value.ModelCameraPosition.X - MathF.Cos(facing) * 0.1f,
            value.ModelCameraPosition.Y - MathF.Sin(facing) * 0.1f,
            requestedTarget.Z);
        value.ModelCameraDistance = 0.1f;
    }

    private static bool HasLoadedModel(UiObject value) =>
        value.ModelResourceLoaded;

    private static void ApplyModelResourceMetadata(
        LuaRuntime runtime,
        UiObject value,
        uint fileDataId)
    {
        value.ModelAvailableAnimationIds.Clear();
        value.ModelAnimationIdsInResourceOrder.Clear();
        value.ModelSequencesInResourceOrder.Clear();
        value.ModelAnimationFiles.Clear();
        value.ModelPendingAnimationRequests.Clear();
        value.ModelGlobalSequenceDurationsMilliseconds.Clear();
        value.ModelCameras.Clear();
        value.ModelCameraLookupIndices.Clear();
        value.ModelCharacterCameraActive = false;
        value.ModelSelectedCameraIndex = null;
        ResetResolvedModelSequencePlayback(value);
        value.ModelHasAttachmentPoints = false;
        value.ModelActiveBoundingBoxMinimum = null;
        value.ModelActiveBoundingBoxMaximum = null;
        value.ModelAnimationBoundingBoxMinimum = null;
        value.ModelAnimationBoundingBoxMaximum = null;
        value.ModelCollisionBoundingBoxMinimum = null;
        value.ModelCollisionBoundingBoxMaximum = null;
        value.ModelMaxBoundingBoxMinimum = null;
        value.ModelMaxBoundingBoxMaximum = null;
        value.ModelCenter = Vector3.Zero;

        var metadata = runtime.ModelResourceProvider?.GetMetadata(fileDataId);
        if (metadata is null)
        {
            if (value.ModelCameraIndex is { } unresolvedCameraIndex)
                SelectSimpleModelCamera(value, unresolvedCameraIndex);
            return;
        }

        foreach (var sequence in metadata.Sequences)
        {
            value.ModelSequencesInResourceOrder.Add(sequence);
            if (value.ModelAvailableAnimationIds.Add(sequence.AnimationId))
                value.ModelAnimationIdsInResourceOrder.Add(sequence.AnimationId);
        }
        value.ModelAnimationFiles.AddRange(metadata.AnimationFiles);
        value.ModelHasAttachmentPoints = metadata.AttachmentCount > 0;
        value.ModelGlobalSequenceDurationsMilliseconds.AddRange(
            metadata.GlobalSequenceDurationsMilliseconds);
        value.ModelCameras.AddRange(metadata.Cameras);
        value.ModelCameraLookupIndices.AddRange(metadata.CameraLookupIndices);

        if (value.ObjectType.Equals(
                "ModelSceneActor",
                StringComparison.OrdinalIgnoreCase))
        {
            ApplyModelSceneActorAnimationState(runtime, value);
            if (value.ModelAnimationKitId is { } pendingAnimationKitId &&
                runtime.ModelResourceProvider is { } animationKitProvider &&
                animationKitProvider.TryGetAnimationKit(
                    pendingAnimationKitId,
                    out _))
            {
                PlayCharacterModelAnimationKit(
                    runtime,
                    value,
                    pendingAnimationKitId,
                    value.ModelAnimationKitLooping);
            }
            ApplyModelSceneActorBoundingBoxes(runtime, value, metadata);
            UpdateModelSceneActorAutomaticTransform(runtime, value);
            return;
        }

        if (metadata.BoundingBoxMinimum is { } minimum &&
            metadata.BoundingBoxMaximum is { } maximum &&
            (maximum.X >= minimum.X ||
             maximum.Y >= minimum.Y ||
             maximum.Z >= minimum.Z))
        {
            value.ModelCenter = (minimum + maximum) * .5f;
        }

        UpdateAutomaticModelTransform(runtime, value);

        if (IsCharacterModelSurface(value))
            ApplyCharacterModelDefaultResourceAnimation(runtime, value);

        if (IsCharacterModelCameraSurface(value))
            RefreshCharacterModelCamera(runtime, value);

        if (value.ModelCameraIndex is { } pendingCameraIndex)
            SelectSimpleModelCamera(value, pendingCameraIndex);
    }

    private static void ApplyModelSceneActorBoundingBoxes(
        LuaRuntime runtime,
        UiObject actor,
        WowModelResourceMetadata metadata)
    {
        if (TryGetValidModelBoundingBox(
                metadata.BoundingBoxMinimum,
                metadata.BoundingBoxMaximum,
                out var maximumMinimum,
                out var maximumMaximum))
        {
            actor.ModelMaxBoundingBoxMinimum = maximumMinimum;
            actor.ModelMaxBoundingBoxMaximum = maximumMaximum;
        }

        if (metadata.HasCollisionGeometry &&
            TryGetValidModelBoundingBox(
                metadata.CollisionBoundingBoxMinimum,
                metadata.CollisionBoundingBoxMaximum,
                out var collisionMinimum,
                out var collisionMaximum))
        {
            actor.ModelCollisionBoundingBoxMinimum = collisionMinimum;
            actor.ModelCollisionBoundingBoxMaximum = collisionMaximum;
        }

        RefreshModelSceneActorActiveBoundingBox(runtime, actor);
        actor.ModelCenter = actor.ModelActiveBoundingBoxMinimum is { } minimum &&
                            actor.ModelActiveBoundingBoxMaximum is { } maximum
            ? (minimum + maximum) * .5f
            : Vector3.Zero;
    }

    private static void RefreshModelSceneActorActiveBoundingBox(
        LuaRuntime runtime,
        UiObject actor)
    {
        actor.ModelAnimationBoundingBoxMinimum = null;
        actor.ModelAnimationBoundingBoxMaximum = null;
        if (TryResolveModelAnimationId(
                runtime,
                actor,
                actor.ModelAnimationId,
                out var resolvedAnimationId))
        {
            var sequence = actor.ModelSequencesInResourceOrder.FirstOrDefault(
                candidate => candidate.AnimationId == resolvedAnimationId);
            if (sequence is not null &&
                TryGetValidModelBoundingBox(
                    sequence.BoundingBoxMinimum,
                    sequence.BoundingBoxMaximum,
                    out var sequenceMinimum,
                    out var sequenceMaximum))
            {
                actor.ModelAnimationBoundingBoxMinimum = sequenceMinimum;
                actor.ModelAnimationBoundingBoxMaximum = sequenceMaximum;
            }
        }

        UpdateModelSceneActorActiveBoundingBox(actor);
    }

    private static void UpdateModelSceneActorActiveBoundingBox(UiObject actor)
    {
        var useCollision = actor.ModelPreferCollisionBounds &&
                           actor.ModelCollisionBoundingBoxMinimum.HasValue &&
                           actor.ModelCollisionBoundingBoxMaximum.HasValue;
        actor.ModelActiveBoundingBoxMinimum = useCollision
            ? actor.ModelCollisionBoundingBoxMinimum
            : actor.ModelAnimationBoundingBoxMinimum;
        actor.ModelActiveBoundingBoxMaximum = useCollision
            ? actor.ModelCollisionBoundingBoxMaximum
            : actor.ModelAnimationBoundingBoxMaximum;
    }

    private static bool TryGetValidModelBoundingBox(
        Vector3? minimum,
        Vector3? maximum,
        out Vector3 validMinimum,
        out Vector3 validMaximum)
    {
        validMinimum = minimum.GetValueOrDefault();
        validMaximum = maximum.GetValueOrDefault();
        return minimum.HasValue &&
               maximum.HasValue &&
               validMaximum.X >= validMinimum.X &&
               validMaximum.Y >= validMinimum.Y &&
               validMaximum.Z >= validMinimum.Z;
    }

    private static void SelectSimpleModelCamera(UiObject value, uint cameraIndex)
    {
        value.ModelCharacterCameraActive = false;
        if (!HasLoadedModel(value))
        {
            value.ModelCameraIndex = cameraIndex;
            return;
        }

        value.ModelCameraIndex = null;
        value.ModelHasCustomCamera = false;
        value.ModelSelectedCameraIndex = null;
        if (cameraIndex >= value.ModelCameras.Count)
        {
            value.ModelHasCurrentCamera = false;
            value.ModelCameraPosition = Vector3.Zero;
            value.ModelCameraTarget = Vector3.Zero;
            value.ModelCameraDistance = 0;
            value.ModelCameraRoll = 0;
            return;
        }

        ApplySimpleModelCameraSnapshot(value, value.ModelCameras[(int)cameraIndex]);
        value.ModelHasCurrentCamera = true;
        value.ModelSelectedCameraIndex = cameraIndex;
    }

    private static void ApplySimpleModelCameraSnapshot(
        UiObject value,
        WowModelCameraMetadata camera)
    {
        ApplySampledModelCamera(value, SampleSimpleModelCamera(value, camera));
    }

    private static SampledModelCamera SampleSimpleModelCamera(
        UiObject value,
        WowModelCameraMetadata camera)
    {
        var sequenceIndex = Math.Max(value.ModelResolvedSequenceIndex, 0);
        var sequenceTime = WowModelSequencePlayback.ResolveSampleTimeMilliseconds(value);

        var positionOffset = Vector3.Zero;
        WowModelAnimationTrackSampler.TrySample(
            camera.PositionTrack,
            sequenceIndex,
            sequenceTime,
            value.ModelGlobalSequenceElapsedMilliseconds,
            value.ModelGlobalSequenceDurationsMilliseconds,
            out positionOffset);
        var targetOffset = Vector3.Zero;
        WowModelAnimationTrackSampler.TrySample(
            camera.TargetTrack,
            sequenceIndex,
            sequenceTime,
            value.ModelGlobalSequenceElapsedMilliseconds,
            value.ModelGlobalSequenceDurationsMilliseconds,
            out targetOffset);
        var roll =
            WowModelAnimationTrackSampler.TrySample(
                camera.RollTrack,
                sequenceIndex,
                sequenceTime,
                value.ModelGlobalSequenceElapsedMilliseconds,
                value.ModelGlobalSequenceDurationsMilliseconds,
                out var sampledRoll)
                ? sampledRoll
                : 0;
        var fieldOfView =
            WowModelAnimationTrackSampler.TrySample(
                camera.FieldOfViewTrack,
                sequenceIndex,
                sequenceTime,
                value.ModelGlobalSequenceElapsedMilliseconds,
                value.ModelGlobalSequenceDurationsMilliseconds,
                out var sampledFieldOfView)
                ? sampledFieldOfView
                : MathF.PI / 2;
        return new SampledModelCamera(
            camera.Position + positionOffset,
            camera.Target + targetOffset,
            roll,
            fieldOfView is > 0 and < MathF.PI
                ? fieldOfView
                : MathF.PI / 2,
            camera.NearClip,
            camera.FarClip);
    }

    private static void ApplySampledModelCamera(
        UiObject value,
        SampledModelCamera camera)
    {
        value.ModelCameraPosition = camera.Position;
        value.ModelCameraTarget = camera.Target;
        value.ModelCameraDistance = GetSimpleModelCameraDistance(value);
        value.ModelCameraRoll = camera.Roll;
        value.ModelCameraFieldOfView = camera.FieldOfView;
        value.ModelCameraNearClip = camera.NearClip;
        value.ModelCameraFarClip = camera.FarClip;
    }

    private readonly record struct SampledModelCamera(
        Vector3 Position,
        Vector3 Target,
        float Roll,
        float FieldOfView,
        float NearClip,
        float FarClip);

    private static bool TryResolveModelSequence(
        LuaRuntime runtime,
        UiObject value,
        ushort requestedAnimationId,
        out ushort resolvedAnimationId,
        out int selectedSequenceIndex,
        out int sequenceIndex,
        out WowModelSequenceMetadata sequence)
        => TryResolveModelSequence(
            runtime,
            value,
            requestedAnimationId,
            explicitVariation: null,
            out resolvedAnimationId,
            out selectedSequenceIndex,
            out sequenceIndex,
            out sequence);

    private static bool TryResolveModelSequence(
        LuaRuntime runtime,
        UiObject value,
        ushort requestedAnimationId,
        int? explicitVariation,
        out ushort resolvedAnimationId,
        out int selectedSequenceIndex,
        out int sequenceIndex,
        out WowModelSequenceMetadata sequence)
    {
        if (!TryResolveModelAnimationId(
                runtime,
                value,
                requestedAnimationId,
                out resolvedAnimationId))
        {
            selectedSequenceIndex = -1;
            sequenceIndex = -1;
            sequence = default!;
            return false;
        }

        var animationId = resolvedAnimationId;
        sequenceIndex = value.ModelSequencesInResourceOrder.FindIndex(
            candidate => candidate.AnimationId == animationId);
        if (sequenceIndex < 0)
        {
            selectedSequenceIndex = -1;
            sequence = default!;
            return false;
        }

        var firstSequenceIndex = sequenceIndex;
        if (explicitVariation is not { } variation ||
            !TrySelectModelSequenceVariation(
                value.ModelSequencesInResourceOrder,
                firstSequenceIndex,
                variation,
                out sequenceIndex))
        {
            sequenceIndex = SelectRandomModelSequenceVariation(
                value.ModelSequencesInResourceOrder,
                firstSequenceIndex);
        }
        selectedSequenceIndex = sequenceIndex;
        if (!TryResolveModelAliasSequence(
                value.ModelSequencesInResourceOrder,
                sequenceIndex,
                out sequenceIndex))
        {
            sequence = default!;
            return false;
        }
        sequence = value.ModelSequencesInResourceOrder[sequenceIndex];
        resolvedAnimationId = sequence.AnimationId;
        return true;
    }

    private static bool TrySelectModelSequenceVariation(
        IReadOnlyList<WowModelSequenceMetadata> sequences,
        int firstSequenceIndex,
        int variation,
        out int sequenceIndex)
    {
        sequenceIndex = firstSequenceIndex;
        if (variation < 0)
            return false;

        while (variation-- > 0)
        {
            if ((uint)sequenceIndex >= (uint)sequences.Count ||
                sequences[sequenceIndex].VariationNext < 0)
            {
                return false;
            }
            sequenceIndex = unchecked(
                (ushort)sequences[sequenceIndex].VariationNext);
        }
        return (uint)sequenceIndex < (uint)sequences.Count;
    }

    private static bool TryResolveModelAnimationId(
        LuaRuntime runtime,
        UiObject value,
        ushort requestedAnimationId,
        out ushort resolvedAnimationId)
    {
        if (value.ModelAvailableAnimationIds.Contains(requestedAnimationId))
        {
            resolvedAnimationId = requestedAnimationId;
            return true;
        }

        var provider = runtime.ModelResourceProvider;
        var visited = new HashSet<ushort>();
        var candidate = requestedAnimationId;
        while (visited.Add(candidate) &&
               provider?.TryGetAnimationFallback(candidate, out var fallback) == true &&
               fallback.FallbackAnimationId != candidate)
        {
            candidate = fallback.FallbackAnimationId;
            if (value.ModelAvailableAnimationIds.Contains(candidate))
            {
                resolvedAnimationId = candidate;
                return true;
            }
        }

        if (value.ModelAvailableAnimationIds.Contains(0))
        {
            resolvedAnimationId = 0;
            return true;
        }
        if (value.ModelAvailableAnimationIds.Contains(147))
        {
            resolvedAnimationId = 147;
            return true;
        }
        if (value.ModelAnimationIdsInResourceOrder.Count > 0)
        {
            resolvedAnimationId = value.ModelAnimationIdsInResourceOrder[0];
            return true;
        }

        resolvedAnimationId = 0;
        return false;
    }

    private static int SelectRandomModelSequenceVariation(
        IReadOnlyList<WowModelSequenceMetadata> sequences,
        int firstSequenceIndex)
    {
        var randomWeight = (uint)Random.Shared.Next(0x8000);
        var sequenceIndex = firstSequenceIndex;
        while ((uint)sequenceIndex < (uint)sequences.Count)
        {
            var sequence = sequences[sequenceIndex];
            if (randomWeight < sequence.VariationWeight)
                return sequenceIndex;

            randomWeight -= sequence.VariationWeight;
            if (sequence.VariationNext < 0)
                break;

            sequenceIndex = unchecked((ushort)sequence.VariationNext);
        }

        return firstSequenceIndex;
    }

    private static void StartCharacterRotationAnimation(
        LuaRuntime runtime,
        UiObject value,
        float previousRotation,
        float requestedRotation,
        bool animate)
    {
        if (!animate ||
            HasLoadedModel(value) && value.ModelAnimationId != 0)
        {
            return;
        }

        value.ModelRotationAnimating = true;
        value.ModelRotationResumeSkipFrame = true;
        value.ModelRotationResumeTickMilliseconds = unchecked(
            runtime.FrameTime.TickMilliseconds + 100);

        if (!HasLoadedModel(value) ||
            value.ModelAnimationId != 0 ||
            requestedRotation == previousRotation)
        {
            return;
        }

        var turnAnimationId = requestedRotation > previousRotation
            ? (ushort)11
            : (ushort)12;
        if (!value.ModelAvailableAnimationIds.Contains(turnAnimationId) ||
            !TryResolveModelSequence(
                runtime,
                value,
                turnAnimationId,
                out var resolvedAnimationId,
                out var selectedSequenceIndex,
                out var resolvedSequenceIndex,
                out var sequence))
        {
            return;
        }

        value.ModelRotationTurnAnimationId = turnAnimationId;
        var request = new WowModelPendingAnimationRequest(
            turnAnimationId,
            -1,
            1,
            0,
            1,
            resolvedAnimationId,
            selectedSequenceIndex,
            resolvedSequenceIndex);
        if (!QueueModelAnimationRequestIfPayloadUnavailable(
                runtime,
                value,
                request,
                sequence))
        {
            StartResolvedModelAnimation(runtime, value, request, sequence);
        }
    }

    private static void TickCharacterRotationAnimation(
        LuaRuntime runtime,
        UiObject value)
    {
        if (!value.ModelRotationAnimating)
            return;
        if (!HasLoadedModel(value))
        {
            CancelCharacterRotationAnimation(value);
            return;
        }
        if (value.ModelRotationResumeSkipFrame)
        {
            value.ModelRotationResumeSkipFrame = false;
            return;
        }
        if (unchecked((int)(
                runtime.FrameTime.TickMilliseconds -
                value.ModelRotationResumeTickMilliseconds)) <= 0)
        {
            return;
        }

        var shouldResumeDesiredAnimation =
            value.ModelResolvedSequenceId != value.ModelAnimationId;
        CancelCharacterRotationAnimation(value);
        if (shouldResumeDesiredAnimation)
            ApplyModelSceneActorAnimationState(runtime, value);
    }

    private static void CancelCharacterRotationAnimation(UiObject value)
    {
        value.ModelRotationAnimating = false;
        value.ModelRotationResumeSkipFrame = false;
        value.ModelRotationResumeTickMilliseconds = 0;
        value.ModelRotationTurnAnimationId = null;
    }

    private static void ApplyModelSceneActorAnimationState(
        LuaRuntime runtime,
        UiObject actor)
    {
        ApplyModelAnimationState(
            runtime,
            actor,
            actor.ModelAnimationId,
            actor.ModelAnimationVariation,
            actor.ModelAnimationSpeed,
            actor.ModelAnimationTimeOffsetMilliseconds,
            IsCharacterModelSurface(actor)
                ? actor.ModelDoBlend ? 1 : 0
                : actor.ModelAnimationBlendOperation);
        if (actor.ObjectType.Equals(
                "ModelSceneActor",
                StringComparison.OrdinalIgnoreCase))
        {
            RefreshModelSceneActorActiveBoundingBox(runtime, actor);
        }
    }

    private static void PlayCharacterModelAnimationKit(
        LuaRuntime runtime,
        UiObject model,
        int animationKitId,
        bool looping)
    {
        var provider = runtime.ModelResourceProvider;
        var stoppedRepresentedLoop =
            model.ModelAnimationKitId is not null &&
            model.ModelAnimationKitLooping;

        if (provider is null)
        {
            ClearCharacterModelAnimationKitState(model);
            model.ModelAnimationKitId = animationKitId;
            model.ModelAnimationKitLooping = looping;
            return;
        }

        if (!provider.TryGetAnimationKit(animationKitId, out var animationKit))
        {
            if (stoppedRepresentedLoop)
            {
                ClearCharacterModelAnimationKitState(model);
                ApplyModelSceneActorAnimationState(runtime, model);
            }
            return;
        }

        ClearCharacterModelAnimationKitState(model);
        model.ModelAnimationKitId = animationKitId;
        model.ModelAnimationKitLooping = looping;
        model.ModelAnimationKitOneShotDurationMilliseconds =
            animationKit.OneShotDurationMilliseconds;
        model.ModelAnimationKitStopId =
            animationKit.OneShotStopAnimationKitId;

        var segmentStates = animationKit.Segments
            .Where(segment => CanResolveAnimationKitSegment(
                runtime,
                model,
                segment))
            .Select(segment => new WowAnimationKitSegmentRuntimeState(segment))
            .ToArray();
        if (segmentStates.Length == 0)
        {
            ClearCharacterModelAnimationKitState(model);
            ApplyModelSceneActorAnimationState(runtime, model);
            return;
        }

        var animationKitState = new WowAnimationKitRuntimeState(
            animationKit,
            segmentStates);
        model.ModelAnimationKitRuntimeState = animationKitState;
        foreach (var segmentState in segmentStates)
        {
            var segment = segmentState.Definition;
            if (segment.StartCondition == 0)
            {
                ScheduleCharacterModelAnimationKitSegmentStart(
                    model,
                    segmentState,
                    segment.StartConditionDelayMilliseconds);
            }

            if (segment.EndCondition == 2)
            {
                ScheduleCharacterModelAnimationKitSegmentEnd(
                    model,
                    segmentState,
                    segment.EndConditionDelayMilliseconds);
            }
        }

        ProcessCharacterModelAnimationKitTransitions(
            runtime,
            model,
            animationKitState,
            processSuccessor: false);
        if (model.ModelAnimationKitRuntimeState == animationKitState)
            RefreshRepresentedCharacterModelAnimationKitSegment(runtime, model);
    }

    private static void ApplyCharacterModelSpellVisualKit(
        LuaRuntime runtime,
        UiObject model,
        uint spellVisualKitId,
        bool oneShot)
    {
        if (spellVisualKitId == 0)
        {
            model.ModelAppliedSpellVisualKits.Clear();
            model.ModelSpellVisualKitId = null;
            model.ModelSpellVisualOneShot = false;
            ClearOrdinaryModelRenderEffect(model);
            return;
        }

        WowSpellVisualKitDefinition definition;
        if (runtime.ModelResourceProvider is { } provider)
        {
            if (!provider.TryGetSpellVisualKit(spellVisualKitId, out definition))
                return;
        }
        else
        {
            definition = new WowSpellVisualKitDefinition(spellVisualKitId, []);
        }

        model.ModelAppliedSpellVisualKits.Add(
            new WowSpellVisualKitApplication(definition, oneShot));
        model.ModelSpellVisualKitId = spellVisualKitId;
        model.ModelSpellVisualOneShot = oneShot;

        if (runtime.ModelResourceProvider is not { } resourceProvider)
            return;

        foreach (var effect in definition.Effects)
        {
            switch (effect.EffectType)
            {
                case 11 when resourceProvider.TryGetDissolveEffect(
                    effect.Effect,
                    out var dissolveEffect):
                    ApplyCharacterModelDissolveEffect(model, dissolveEffect);
                    break;
                case 7 when resourceProvider.TryGetShadowyEffect(
                    effect.Effect,
                    out var shadowyEffect):
                    ApplyCharacterModelShadowyEffect(model, shadowyEffect);
                    break;
                case 12 when resourceProvider.TryGetEdgeGlowEffect(
                    effect.Effect,
                    out var edgeGlowEffect):
                    ApplyCharacterModelEdgeGlowEffect(model, edgeGlowEffect);
                    break;
            }
        }
    }

    private static void ApplyCharacterModelShadowyEffect(
        UiObject model,
        WowShadowyEffectDefinition effect)
    {
        var primary = UnpackShadowyEffectColor(effect.PrimaryColor);
        var secondary = (effect.Flags & 1) != 0
            ? UnpackShadowyEffectColor(effect.SecondaryColor)
            : RoundTripRgbThroughHsv(primary);

        model.ModelShadowEffectState = new UiModelShadowEffectState(
            new Vector4(primary, effect.InnerStrength),
            new Vector4(secondary, effect.OuterStrength));
        model.ModelShadowEffectStrength = effect.Value;
        model.ModelDissolveEffectState = null;
        model.ModelEdgeGlowEffectState = null;
        model.ModelRenderEffectKind = UiModelRenderEffectKind.Shadow;
        model.ModelGradientMaskEnabled = false;
        model.ModelDesaturation = 0;
    }

    private static void ApplyCharacterModelDissolveEffect(
        UiObject model,
        WowDissolveEffectDefinition effect)
    {
        model.ModelDissolveEffectState = new UiModelDissolveEffectState(
            effect,
            effect.EndValue);
        model.ModelShadowEffectStrength = effect.EndValue;
        model.ModelShadowEffectState = null;
        model.ModelEdgeGlowEffectState = null;
        model.ModelRenderEffectKind = UiModelRenderEffectKind.Dissolve;
        model.ModelGradientMaskEnabled = false;
        model.ModelDesaturation = 0;
    }

    private static void ApplyCharacterModelEdgeGlowEffect(
        UiObject model,
        WowEdgeGlowEffectDefinition effect)
    {
        model.ModelEdgeGlowEffectState = new UiModelEdgeGlowEffectState(
            effect.GlowColor,
            effect.GlowMultiplier,
            effect.FresnelCoefficient,
            (effect.Flags & 1) != 0);
        model.ModelShadowEffectStrength = 0;
        model.ModelShadowEffectState = null;
        model.ModelDissolveEffectState = null;
        model.ModelRenderEffectKind = UiModelRenderEffectKind.EdgeGlow;
        model.ModelGradientMaskEnabled = false;
        model.ModelDesaturation = 0;
    }

    private static Vector3 UnpackShadowyEffectColor(uint packedColor) =>
        new(
            ((packedColor >> 16) & 0xFF) / 255f,
            ((packedColor >> 8) & 0xFF) / 255f,
            (packedColor & 0xFF) / 255f);

    private static Vector3 RoundTripRgbThroughHsv(Vector3 rgb)
    {
        var maximum = MathF.Max(rgb.X, MathF.Max(rgb.Y, rgb.Z));
        var minimum = MathF.Min(rgb.X, MathF.Min(rgb.Y, rgb.Z));
        if (maximum == 0)
            return Vector3.Zero;

        var saturation = (maximum - minimum) / maximum;
        if (saturation == 0)
            return new Vector3(maximum);

        var difference = maximum - minimum;
        float hue;
        if (maximum == rgb.X)
            hue = (rgb.Y - rgb.Z) / difference;
        else if (maximum == rgb.Y)
            hue = ((rgb.Z - rgb.X) / difference) + 2;
        else
            hue = ((rgb.X - rgb.Y) / difference) + 4;

        hue *= 60;
        if (hue < 0)
            hue += 360;

        var sectorPosition = hue / 60;
        var sector = Math.Min((int)sectorPosition, 5);
        var fraction = sectorPosition - sector;
        var low = (1 - saturation) * maximum;
        var falling = (1 - (fraction * saturation)) * maximum;
        var rising = (1 - ((1 - fraction) * saturation)) * maximum;
        return sector switch
        {
            0 => new Vector3(maximum, rising, low),
            1 => new Vector3(falling, maximum, low),
            2 => new Vector3(low, maximum, rising),
            3 => new Vector3(low, falling, maximum),
            4 => new Vector3(rising, low, maximum),
            _ => new Vector3(maximum, low, falling)
        };
    }

    private static void ClearOrdinaryModelRenderEffect(UiObject model)
    {
        model.ModelRenderEffectKind = UiModelRenderEffectKind.None;
        model.ModelShadowEffectStrength = 0;
        model.ModelShadowEffectState = null;
        model.ModelDissolveEffectState = null;
        model.ModelEdgeGlowEffectState = null;
        model.ModelGradientMaskEnabled = false;
        model.ModelDesaturation = 0;
    }

    private static void StartCharacterModelAnimationKitSegment(
        LuaRuntime runtime,
        UiObject model,
        WowAnimationKitRuntimeState animationKitState,
        WowAnimationKitSegmentRuntimeState segmentState)
    {
        segmentState.StartDeadlineMilliseconds = null;
        if (segmentState.PlaybackState ==
            WowAnimationKitSegmentPlaybackState.Playing)
        {
            return;
        }

        var segment = segmentState.Definition;
        var requestedVariation = segmentState.InheritedVariation ??
            GetAnimationKitForcedVariation(segment);
        if (!TryResolveModelSequence(
                runtime,
                model,
                segment.AnimationId,
                requestedVariation,
                out var resolvedAnimationId,
                out _,
                out var resolvedSequenceIndex,
                out var sequence))
        {
            segmentState.PlaybackState =
                WowAnimationKitSegmentPlaybackState.Stopped;
            return;
        }

        var speed = float.IsFinite(segment.Speed) ? segment.Speed : 1;
        var repeatCount = segment.EndCondition == 0 &&
                          segment.EndConditionParameter != 0
            ? unchecked((byte)segment.EndConditionParameter)
            : unchecked((byte)SelectModelSequenceRepeatCount(sequence));
        segmentState.PlaybackState =
            WowAnimationKitSegmentPlaybackState.Playing;
        segmentState.ResolvedAnimationId = resolvedAnimationId;
        segmentState.ResolvedVariation = sequence.VariationIndex;
        segmentState.ResolvedSequenceIndex = resolvedSequenceIndex;
        segmentState.SequenceDurationMilliseconds = sequence.DurationMilliseconds;
        segmentState.RepeatCount = repeatCount;
        segmentState.PlaybackSpeed = speed;
        segmentState.StartElapsedMilliseconds =
            model.ModelAnimationKitElapsedMilliseconds;

        ScheduleCharacterModelAnimationKitAutomaticEnd(model, segmentState);
        NotifyCharacterModelAnimationKitSegmentStarted(
            model,
            animationKitState,
            segmentState);
    }

    private static bool CanResolveAnimationKitSegment(
        LuaRuntime runtime,
        UiObject model,
        WowAnimationKitSegmentDefinition segment) =>
        TryResolveModelSequence(
            runtime,
            model,
            segment.AnimationId,
            GetAnimationKitForcedVariation(segment),
            out _,
            out _,
            out _,
            out _);

    private static int? GetAnimationKitForcedVariation(
        WowAnimationKitSegmentDefinition segment) =>
        (segment.SegmentFlags & 2) != 0
            ? segment.ForcedVariation
            : null;

    private static void TickCharacterModelAnimationKit(
        LuaRuntime runtime,
        UiObject model,
        double elapsedMilliseconds)
    {
        if (model.ModelAnimationKitId is null ||
            (model.ModelAnimationKitOneShotDurationMilliseconds == 0 &&
             model.ModelAnimationKitRuntimeState is null))
        {
            return;
        }

        model.ModelAnimationKitElapsedMilliseconds += elapsedMilliseconds;
        if (model.ModelAnimationKitOneShotDurationMilliseconds > 0 &&
            model.ModelAnimationKitElapsedMilliseconds >=
            model.ModelAnimationKitOneShotDurationMilliseconds)
        {
            CompleteCharacterModelAnimationKit(
                runtime,
                model,
                processSuccessor: true);
            return;
        }

        var animationKitState = model.ModelAnimationKitRuntimeState;
        if (animationKitState is null)
            return;

        ProcessCharacterModelAnimationKitTransitions(
            runtime,
            model,
            animationKitState,
            processSuccessor: true);
        if (model.ModelAnimationKitRuntimeState == animationKitState)
            RefreshRepresentedCharacterModelAnimationKitSegment(runtime, model);
    }

    private static void ScheduleCharacterModelAnimationKitSegmentStart(
        UiObject model,
        WowAnimationKitSegmentRuntimeState segmentState,
        uint delayMilliseconds)
    {
        if (segmentState.StartDeadlineMilliseconds is not null ||
            segmentState.PlaybackState ==
            WowAnimationKitSegmentPlaybackState.Playing)
        {
            return;
        }

        segmentState.StartDeadlineMilliseconds =
            model.ModelAnimationKitElapsedMilliseconds + delayMilliseconds;
    }

    private static void ScheduleCharacterModelAnimationKitSegmentEnd(
        UiObject model,
        WowAnimationKitSegmentRuntimeState segmentState,
        double delayMilliseconds)
    {
        if (segmentState.EndDeadlineMilliseconds is not null ||
            segmentState.PlaybackState ==
            WowAnimationKitSegmentPlaybackState.Stopped)
        {
            return;
        }

        segmentState.EndDeadlineMilliseconds =
            model.ModelAnimationKitElapsedMilliseconds + delayMilliseconds;
    }

    private static void ProcessCharacterModelAnimationKitTransitions(
        LuaRuntime runtime,
        UiObject model,
        WowAnimationKitRuntimeState animationKitState,
        bool processSuccessor)
    {
        while (true)
        {
            var elapsed = model.ModelAnimationKitElapsedMilliseconds;
            var start = animationKitState.Segments
                .Where(segment =>
                    segment.StartDeadlineMilliseconds is { } deadline &&
                    deadline <= elapsed)
                .OrderBy(segment => segment.StartDeadlineMilliseconds)
                .ThenBy(segment => segment.Definition.OrderIndex)
                .FirstOrDefault();
            var end = animationKitState.Segments
                .Where(segment =>
                    segment.EndDeadlineMilliseconds is { } deadline &&
                    deadline <= elapsed)
                .OrderBy(segment => segment.EndDeadlineMilliseconds)
                .ThenBy(segment => segment.Definition.OrderIndex)
                .FirstOrDefault();
            if (start is null && end is null)
                break;

            if (start is not null &&
                (end is null ||
                 start.StartDeadlineMilliseconds <=
                 end.EndDeadlineMilliseconds))
            {
                StartCharacterModelAnimationKitSegment(
                    runtime,
                    model,
                    animationKitState,
                    start);
            }
            else
            {
                StopCharacterModelAnimationKitSegment(
                    runtime,
                    model,
                    animationKitState,
                    end!,
                    processSuccessor);
            }

            if (model.ModelAnimationKitRuntimeState != animationKitState)
                return;
        }
    }

    private static void ScheduleCharacterModelAnimationKitAutomaticEnd(
        UiObject model,
        WowAnimationKitSegmentRuntimeState segmentState)
    {
        var segment = segmentState.Definition;
        var autoStopAtAnimationEnd =
            model.ModelAnimationKitOneShotDurationMilliseconds == 0 &&
            !model.ModelAnimationKitLooping;
        switch (segment.EndCondition)
        {
            case 0:
                if (MathF.Abs(segmentState.PlaybackSpeed) <= 0.00001f &&
                    !autoStopAtAnimationEnd)
                {
                    return;
                }
                ScheduleCharacterModelAnimationKitSegmentEnd(
                    model,
                    segmentState,
                    GetCharacterModelAnimationKitRemainingDuration(segmentState) +
                    segment.EndConditionDelayMilliseconds);
                break;
            case 1 when autoStopAtAnimationEnd:
                ScheduleCharacterModelAnimationKitSegmentEnd(
                    model,
                    segmentState,
                    GetCharacterModelAnimationKitRemainingDuration(segmentState));
                break;
            case 3:
                ScheduleCharacterModelAnimationKitSegmentEnd(
                    model,
                    segmentState,
                    segment.EndConditionDelayMilliseconds);
                break;
            case 4 when segment.OrderIndex == segment.StartConditionParameter:
                ScheduleCharacterModelAnimationKitSegmentEnd(
                    model,
                    segmentState,
                    segment.EndConditionDelayMilliseconds);
                break;
        }
    }

    private static double GetCharacterModelAnimationKitRemainingDuration(
        WowAnimationKitSegmentRuntimeState segmentState)
    {
        var speed = segmentState.PlaybackSpeed;
        var absoluteSpeed = MathF.Abs(speed);
        var totalDuration = unchecked((int)(
            segmentState.SequenceDurationMilliseconds * segmentState.RepeatCount));
        if (absoluteSpeed <= 0.00001f || totalDuration <= 0)
            return 0;

        var current = unchecked((int)
            segmentState.Definition.AnimationStartTimeMilliseconds);
        if (speed > 0)
        {
            var position = PositiveModulo(current, totalDuration);
            return (int)((totalDuration - position) / speed);
        }

        var reversePosition = PositiveModulo(current - 1, totalDuration);
        return (int)((reversePosition + 1) / absoluteSpeed);
    }

    private static int PositiveModulo(int value, int modulus)
    {
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static void NotifyCharacterModelAnimationKitSegmentStarted(
        UiObject model,
        WowAnimationKitRuntimeState animationKitState,
        WowAnimationKitSegmentRuntimeState source)
    {
        foreach (var target in animationKitState.Segments)
        {
            var definition = target.Definition;
            if (definition.StartCondition == 1 &&
                definition.StartConditionParameter ==
                source.Definition.OrderIndex &&
                definition.OrderIndex != source.Definition.OrderIndex)
            {
                InheritCharacterModelAnimationKitVariation(source, target);
                ScheduleCharacterModelAnimationKitSegmentStart(
                    model,
                    target,
                    definition.StartConditionDelayMilliseconds);
            }

            if (definition.EndCondition == 4 &&
                unchecked((byte)definition.EndConditionParameter) ==
                source.Definition.OrderIndex)
            {
                ScheduleCharacterModelAnimationKitSegmentEnd(
                    model,
                    target,
                    definition.EndConditionDelayMilliseconds);
            }
        }
    }

    private static void NotifyCharacterModelAnimationKitSegmentEnded(
        UiObject model,
        WowAnimationKitRuntimeState animationKitState,
        WowAnimationKitSegmentRuntimeState source)
    {
        foreach (var target in animationKitState.Segments)
        {
            var definition = target.Definition;
            if (definition.StartCondition == 2 &&
                definition.StartConditionParameter ==
                source.Definition.OrderIndex)
            {
                InheritCharacterModelAnimationKitVariation(source, target);
                ScheduleCharacterModelAnimationKitSegmentStart(
                    model,
                    target,
                    definition.StartConditionDelayMilliseconds);
            }

            if (definition.EndCondition == 5 &&
                unchecked((byte)definition.EndConditionParameter) ==
                source.Definition.OrderIndex &&
                definition.OrderIndex != source.Definition.OrderIndex)
            {
                ScheduleCharacterModelAnimationKitSegmentEnd(
                    model,
                    target,
                    definition.EndConditionDelayMilliseconds);
            }
        }
    }

    private static void InheritCharacterModelAnimationKitVariation(
        WowAnimationKitSegmentRuntimeState source,
        WowAnimationKitSegmentRuntimeState target)
    {
        if ((target.Definition.SegmentFlags & 0x40) != 0)
            target.InheritedVariation = source.ResolvedVariation;
    }

    private static void StopCharacterModelAnimationKitSegment(
        LuaRuntime runtime,
        UiObject model,
        WowAnimationKitRuntimeState animationKitState,
        WowAnimationKitSegmentRuntimeState segmentState,
        bool processSuccessor)
    {
        segmentState.StartDeadlineMilliseconds = null;
        segmentState.EndDeadlineMilliseconds = null;
        if (segmentState.PlaybackState ==
            WowAnimationKitSegmentPlaybackState.Stopped)
        {
            return;
        }

        segmentState.PlaybackState =
            WowAnimationKitSegmentPlaybackState.Stopped;
        NotifyCharacterModelAnimationKitSegmentEnded(
            model,
            animationKitState,
            segmentState);

        var loopIndex = segmentState.Definition.LoopToSegmentIndex;
        var autoStopAtAnimationEnd =
            model.ModelAnimationKitOneShotDurationMilliseconds == 0 &&
            !model.ModelAnimationKitLooping;
        if (!autoStopAtAnimationEnd &&
            loopIndex >= 0 &&
            loopIndex < animationKitState.Segments.Count)
        {
            var loopTarget = animationKitState.Segments[loopIndex];
            if ((segmentState.Definition.SegmentFlags & 4) != 0)
                loopTarget.InheritedVariation = segmentState.ResolvedVariation;
            ScheduleCharacterModelAnimationKitSegmentStart(model, loopTarget, 0);
        }

        var allStopped = animationKitState.Segments.All(segment =>
            segment.PlaybackState ==
            WowAnimationKitSegmentPlaybackState.Stopped &&
            segment.StartDeadlineMilliseconds is null);
        if (allStopped)
        {
            CompleteCharacterModelAnimationKit(
                runtime,
                model,
                processSuccessor);
        }
    }

    private static void RefreshRepresentedCharacterModelAnimationKitSegment(
        LuaRuntime runtime,
        UiObject model)
    {
        var animationKitState = model.ModelAnimationKitRuntimeState;
        var represented = animationKitState?.Segments
            .Where(segment =>
                segment.PlaybackState ==
                WowAnimationKitSegmentPlaybackState.Playing)
            .OrderBy(segment => !TargetsWholeModel(segment.Definition))
            .ThenBy(segment => segment.Definition.OrderIndex)
            .FirstOrDefault();
        if (represented is null)
        {
            var hadRepresentedSegment = model.ModelAnimationKitSegmentId is not null;
            ClearRepresentedCharacterModelAnimationKitSegment(model);
            if (hadRepresentedSegment || model.ModelAnimationKitId is not null)
                ApplyModelSceneActorAnimationState(runtime, model);
            return;
        }

        var definition = represented.Definition;
        if (model.ModelAnimationKitSegmentId == definition.SegmentId)
            return;

        model.ModelAnimationKitSegmentId = definition.SegmentId;
        model.ModelAnimationKitSegmentOrderIndex = definition.OrderIndex;
        model.ModelAnimationKitSegmentUsesBoneSet = definition.BoneSets.Count > 0;
        ApplyModelAnimationState(
            runtime,
            model,
            definition.AnimationId,
            represented.ResolvedVariation,
            represented.PlaybackSpeed,
            unchecked((int)definition.AnimationStartTimeMilliseconds),
            1);
        model.ModelSequenceRepeatCount = represented.RepeatCount;
    }

    private static bool TargetsWholeModel(
        WowAnimationKitSegmentDefinition segment) =>
        segment.BoneSets.Count == 0 ||
        segment.BoneSets.Any(boneSet => boneSet.BoneDataId == 0);

    private static void ClearRepresentedCharacterModelAnimationKitSegment(
        UiObject model)
    {
        model.ModelAnimationKitSegmentId = null;
        model.ModelAnimationKitSegmentOrderIndex = 0;
        model.ModelAnimationKitSegmentUsesBoneSet = false;
    }

    private static void CompleteCharacterModelAnimationKit(
        LuaRuntime runtime,
        UiObject model,
        bool processSuccessor)
    {
        var stopAnimationKitId = processSuccessor
            ? model.ModelAnimationKitStopId
            : (ushort)0;
        ClearCharacterModelAnimationKitState(model);
        if (stopAnimationKitId != 0)
        {
            PlayCharacterModelAnimationKit(
                runtime,
                model,
                stopAnimationKitId,
                looping: false);
            if (model.ModelAnimationKitId is not null)
                return;
        }

        if (HasLoadedModel(model))
            ApplyModelSceneActorAnimationState(runtime, model);
    }

    private static void StopCharacterModelAnimationKit(
        LuaRuntime runtime,
        UiObject model,
        bool restoreBase)
    {
        var hadAnimationKit = model.ModelAnimationKitId is not null;
        ClearCharacterModelAnimationKitState(model);
        if (restoreBase && hadAnimationKit && HasLoadedModel(model))
            ApplyModelSceneActorAnimationState(runtime, model);
    }

    private static void ClearCharacterModelAnimationKitState(UiObject model)
    {
        model.ModelAnimationKitId = null;
        model.ModelAnimationKitLooping = false;
        ClearRepresentedCharacterModelAnimationKitSegment(model);
        model.ModelAnimationKitOneShotDurationMilliseconds = 0;
        model.ModelAnimationKitElapsedMilliseconds = 0;
        model.ModelAnimationKitRuntimeState = null;
        model.ModelAnimationKitStopId = 0;
    }

    private static void ApplyCharacterModelDefaultResourceAnimation(
        LuaRuntime runtime,
        UiObject model)
    {
        ApplyModelAnimationState(
            runtime,
            model,
            0,
            -1,
            1,
            0,
            0);
    }

    private static void ApplyModelAnimationState(
        LuaRuntime runtime,
        UiObject actor,
        ushort requestedAnimationId,
        int requestedVariation,
        float playbackSpeed,
        int timeOffsetMilliseconds,
        int blendOperation)
    {
        if (!HasLoadedModel(actor) ||
            !TryResolveModelSequence(
                runtime,
                actor,
                requestedAnimationId,
                requestedVariation >= 0
                    ? requestedVariation
                    : null,
                out var resolvedAnimationId,
                out var selectedSequenceIndex,
                out var resolvedSequenceIndex,
                out var sequence))
        {
            ResetResolvedModelSequencePlayback(actor);
            return;
        }

        var request = new WowModelPendingAnimationRequest(
            requestedAnimationId,
            requestedVariation,
            playbackSpeed,
            timeOffsetMilliseconds,
            blendOperation,
            resolvedAnimationId,
            selectedSequenceIndex,
            resolvedSequenceIndex);
        if (QueueModelAnimationRequestIfPayloadUnavailable(
                runtime,
                actor,
                request,
                sequence))
        {
            return;
        }

        StartResolvedModelAnimation(runtime, actor, request, sequence);
    }

    private static void StartResolvedModelAnimation(
        LuaRuntime runtime,
        UiObject actor,
        WowModelPendingAnimationRequest request,
        WowModelSequenceMetadata sequence)
    {
        var blendState = CreateModelSequenceBlendState(
            runtime,
            actor,
            request.ResolvedSequenceIndex,
            sequence,
            request.BlendOperation,
            request.TimeOffsetMilliseconds);
        ResetResolvedModelSequencePlayback(actor);

        var repeatCount = SelectModelSequenceRepeatCount(sequence);
        var playbackSpeed = NormalizeModelSequencePlaybackSpeed(
            request.PlaybackSpeed);
        var totalDuration = unchecked(
            sequence.DurationMilliseconds * repeatCount);
        var initialElapsed = CalculateInitialModelSequenceElapsed(
            request.TimeOffsetMilliseconds,
            playbackSpeed,
            totalDuration,
            includeForwardTick: !runtime.IsProcessingModelSceneCallbacks);

        actor.ModelSequenceId = request.RequestedAnimationId;
        actor.ModelSequenceTimeOffset = request.TimeOffsetMilliseconds;
        actor.ModelResolvedSequenceId = request.ResolvedAnimationId;
        actor.ModelSelectedSequenceIndex = request.SelectedSequenceIndex;
        actor.ModelResolvedSequenceIndex = request.ResolvedSequenceIndex;
        actor.ModelResolvedSequenceVariation = sequence.VariationIndex;
        actor.ModelResolvedSequenceDurationMilliseconds =
            sequence.DurationMilliseconds;
        actor.ModelSequenceInitialElapsedMilliseconds = initialElapsed;
        actor.ModelSequenceElapsedMilliseconds = initialElapsed;
        actor.ModelSequencePlaybackClockMilliseconds = 0;
        actor.ModelSequencePlaybackSpeed = playbackSpeed;
        actor.ModelSequenceRepeatCount = repeatCount;
        actor.ModelSequenceLoops = (sequence.Flags & 1) == 0;
        actor.ModelSequenceBlendState = blendState;

        var hasEffectiveSpeed =
            float.IsFinite(playbackSpeed) &&
            MathF.Abs(playbackSpeed) > 0.00001f;
        actor.ModelSequencePlaying =
            sequence.DurationMilliseconds > 0 &&
            totalDuration > 0 &&
            hasEffectiveSpeed &&
            (actor.ModelSequenceLoops ||
             (playbackSpeed < 0
                 ? initialElapsed > 0
                 : initialElapsed < totalDuration));
        actor.ModelSequencePlaybackRevision++;
    }

    private static bool QueueModelAnimationRequestIfPayloadUnavailable(
        LuaRuntime runtime,
        UiObject actor,
        WowModelPendingAnimationRequest request,
        WowModelSequenceMetadata sequence)
    {
        if (!TryGetExternalAnimationFileDataId(
                actor,
                sequence,
                out var animationFileDataId))
        {
            return false;
        }

        var modelFileDataId = actor.ModelFileDataId.GetValueOrDefault();
        var payloadState = animationFileDataId != 0 && modelFileDataId != 0
            ? runtime.ModelResourceProvider?.GetAnimationSequencePayloadState(
                modelFileDataId,
                animationFileDataId)
            : WowModelAnimationPayloadState.Pending;
        if (payloadState == WowModelAnimationPayloadState.Resident)
        {
            return false;
        }

        if (payloadState == WowModelAnimationPayloadState.Failed)
        {
            actor.ModelPendingAnimationRequests.RemoveAll(
                value => value.ResolvedSequenceIndex == request.ResolvedSequenceIndex);
            return true;
        }

        var existingIndex = actor.ModelPendingAnimationRequests.FindIndex(
            value => value.ResolvedSequenceIndex == request.ResolvedSequenceIndex);
        if (existingIndex >= 0)
            actor.ModelPendingAnimationRequests[existingIndex] = request;
        else
            actor.ModelPendingAnimationRequests.Add(request);
        return true;
    }

    private static void PromoteReadyModelAnimationRequests(
        LuaRuntime runtime,
        UiObject actor)
    {
        foreach (var request in actor.ModelPendingAnimationRequests.ToArray())
        {
            if ((uint)request.ResolvedSequenceIndex >=
                (uint)actor.ModelSequencesInResourceOrder.Count)
            {
                actor.ModelPendingAnimationRequests.Remove(request);
                continue;
            }

            var sequence =
                actor.ModelSequencesInResourceOrder[request.ResolvedSequenceIndex];
            if (TryGetExternalAnimationFileDataId(
                    actor,
                    sequence,
                    out var animationFileDataId))
            {
                var modelFileDataId = actor.ModelFileDataId.GetValueOrDefault();
                if (animationFileDataId == 0 || modelFileDataId == 0)
                {
                    continue;
                }


                var payloadState = runtime.ModelResourceProvider?
                    .GetAnimationSequencePayloadState(
                        modelFileDataId,
                        animationFileDataId) ??
                    WowModelAnimationPayloadState.Pending;
                if (payloadState == WowModelAnimationPayloadState.Pending)
                    continue;
                if (payloadState == WowModelAnimationPayloadState.Failed)
                {
                    actor.ModelPendingAnimationRequests.Remove(request);
                    continue;
                }
            }

            actor.ModelPendingAnimationRequests.Remove(request);
            StartResolvedModelAnimation(runtime, actor, request, sequence);
        }
    }

    private static bool TryGetExternalAnimationFileDataId(
        UiObject actor,
        WowModelSequenceMetadata sequence,
        out uint fileDataId)
    {
        fileDataId = 0;
        if ((sequence.Flags & 0x20) != 0)
            return false;

        foreach (var file in actor.ModelAnimationFiles)
        {
            if (file.AnimationId == sequence.AnimationId &&
                file.VariationIndex == sequence.VariationIndex)
            {
                fileDataId = file.FileDataId;
                break;
            }
        }
        return true;
    }

    private static WowModelSequenceBlendState? CreateModelSequenceBlendState(
        LuaRuntime runtime,
        UiObject actor,
        int newSequenceIndex,
        WowModelSequenceMetadata newSequence,
        int blendOperation,
        int timeOffsetMilliseconds)
    {
        if (blendOperation == 0)
            return null;

        var currentSecondary = actor.ModelSequenceBlendState;
        var useCurrentSecondary =
            currentSecondary is not null &&
            WowModelSequencePlayback.ResolveSecondaryPoseWeight(
                currentSecondary) > 0.5f;
        var previousSequenceIndex = useCurrentSecondary
            ? currentSecondary!.SequenceIndex
            : actor.ModelResolvedSequenceIndex;
        if ((uint)previousSequenceIndex >=
            (uint)actor.ModelSequencesInResourceOrder.Count)
        {
            return null;
        }

        var previousSequence =
            actor.ModelSequencesInResourceOrder[previousSequenceIndex];
        var transitionDuration = newSequence.BlendInMilliseconds;
        if ((previousSequence.Flags & 0x200) != 0 &&
            previousSequence.BlendOutMilliseconds != 0)
        {
            transitionDuration = previousSequence.BlendOutMilliseconds;
        }
        if (transitionDuration < 16)
            return null;
        var transitionEndOffset = unchecked(
            transitionDuration +
            (!runtime.IsProcessingModelSceneCallbacks ? 1u : 0u));

        if (actor.ModelResolvedSequenceIndex == newSequenceIndex)
        {
            var playbackDelta = Math.Abs(
                actor.ModelSequenceElapsedMilliseconds -
                timeOffsetMilliseconds);
            if (playbackDelta <= transitionDuration)
                return null;
        }

        if (useCurrentSecondary)
        {
            return new WowModelSequenceBlendState
            {
                SequenceIndex = currentSecondary!.SequenceIndex,
                SequenceDurationMilliseconds =
                    currentSecondary.SequenceDurationMilliseconds,
                SequenceInitialElapsedMilliseconds =
                    currentSecondary.SequenceElapsedMilliseconds,
                SequenceElapsedMilliseconds =
                    currentSecondary.SequenceElapsedMilliseconds,
                SequencePlaybackSpeed =
                    currentSecondary.SequencePlaybackSpeed,
                SequenceRepeatCount = currentSecondary.SequenceRepeatCount,
                SequencePlaying = currentSecondary.SequencePlaying,
                SequenceLoops = currentSecondary.SequenceLoops,
                TransitionDurationMilliseconds = transitionDuration,
                TransitionEndOffsetMilliseconds = transitionEndOffset
            };
        }

        return new WowModelSequenceBlendState
        {
            SequenceIndex = actor.ModelResolvedSequenceIndex,
            SequenceDurationMilliseconds =
                actor.ModelResolvedSequenceDurationMilliseconds,
            SequenceInitialElapsedMilliseconds =
                actor.ModelSequenceElapsedMilliseconds,
            SequenceElapsedMilliseconds =
                actor.ModelSequenceElapsedMilliseconds,
            SequencePlaybackSpeed = actor.ModelSequencePlaybackSpeed,
            SequenceRepeatCount = actor.ModelSequenceRepeatCount,
            SequencePlaying = actor.ModelSequencePlaying,
            SequenceLoops = actor.ModelSequenceLoops,
            TransitionDurationMilliseconds = transitionDuration,
            TransitionEndOffsetMilliseconds = transitionEndOffset
        };
    }

    private static uint SelectModelSequenceRepeatCount(
        WowModelSequenceMetadata sequence)
    {
        var repetitionRange =
            sequence.MaximumRepetitions - sequence.MinimumRepetitions;
        var randomValue = Random.Shared.Next(0x8000);
        var selected = unchecked((uint)(
            sequence.MinimumRepetitions +
            (long)repetitionRange * randomValue / 0x8000));
        return selected == 0 ? 1 : selected;
    }

    private static float NormalizeModelSequencePlaybackSpeed(float speed)
    {
        if (speed > 10_000)
            return 1;
        if (speed < -10_000)
            return -1;
        return speed;
    }

    private static double CalculateInitialModelSequenceElapsed(
        int timeOffsetMilliseconds,
        float playbackSpeed,
        uint totalDurationMilliseconds,
        bool includeForwardTick)
    {
        if (!float.IsFinite(playbackSpeed))
            return timeOffsetMilliseconds;
        if (playbackSpeed == 0)
            return timeOffsetMilliseconds;

        var inverseSpeed = MathF.Abs(playbackSpeed) <= 0.00001f
            ? 0
            : 1 / playbackSpeed;
        if (playbackSpeed > 0)
        {
            var elapsedTicks =
                (int)(timeOffsetMilliseconds * MathF.Abs(inverseSpeed)) -
                (includeForwardTick ? 1 : 0);
            return (int)(elapsedTicks * playbackSpeed);
        }

        if (totalDurationMilliseconds == 0)
            return 0;
        var reverseOffset = unchecked(
            (totalDurationMilliseconds - (uint)timeOffsetMilliseconds) %
            totalDurationMilliseconds);
        var reverseTicks =
            (int)(reverseOffset * MathF.Abs(inverseSpeed));
        return totalDurationMilliseconds +
               (int)(reverseTicks * playbackSpeed);
    }

    private static void ResetResolvedModelSequencePlayback(UiObject model)
    {
        model.ModelResolvedSequenceId = null;
        model.ModelSelectedSequenceIndex = -1;
        model.ModelResolvedSequenceIndex = -1;
        model.ModelResolvedSequenceVariation = 0;
        model.ModelResolvedSequenceDurationMilliseconds = 0;
        model.ModelSequenceElapsedMilliseconds = 0;
        model.ModelSequenceInitialElapsedMilliseconds = 0;
        model.ModelSequencePlaybackClockMilliseconds = 0;
        model.ModelSequencePlaybackSpeed = 1;
        model.ModelSequenceRepeatCount = 1;
        model.ModelSequencePlaying = false;
        model.ModelSequenceLoops = false;
        model.ModelSequenceBlendState = null;
        model.ModelSequencePlaybackRevision++;
    }

    private static bool TryResolveModelAliasSequence(
        IReadOnlyList<WowModelSequenceMetadata> sequences,
        int selectedSequenceIndex,
        out int playbackSequenceIndex)
    {
        const uint aliasFlag = 0x40;
        var visited = new HashSet<int>();
        playbackSequenceIndex = selectedSequenceIndex;
        while ((uint)playbackSequenceIndex < (uint)sequences.Count &&
               (sequences[playbackSequenceIndex].Flags & aliasFlag) != 0)
        {
            if (!visited.Add(playbackSequenceIndex))
                return false;

            var aliasNext = sequences[playbackSequenceIndex].AliasNext;
            if (aliasNext < 0)
                return false;
            playbackSequenceIndex = unchecked((ushort)aliasNext);
        }

        return (uint)playbackSequenceIndex < (uint)sequences.Count;
    }

    private static void ClearModel(LuaRuntime runtime, UiObject value)
    {
        if (value.ObjectType.Equals(
                "ModelSceneActor",
                StringComparison.OrdinalIgnoreCase))
        {
            ClearModelSceneActorResourceState(value);
            runtime.InvokeScript(value, "OnModelCleared");
            return;
        }

        if (IsCharacterModelSurface(value))
            ResetCharacterModelRawResourceState(value);
        else
            ResetCharacterModelSourceState(value);
        value.ModelPaused = false;
        value.ModelGlobalPaused = false;
    }

    private static void ClearModelSceneActorResourceState(UiObject value)
    {
        value.ModelFileDataId = null;
        value.ModelBoneFileDataId = 0;
        value.ModelCreatureDisplayId = null;
        value.ModelPath = null;
        value.ModelUnitToken = null;
        value.ModelGuildTabardInfo = null;
        value.ModelResourceLoaded = false;
        value.ModelNoMip = false;
        value.ModelHasAttachmentPoints = false;
        value.ModelActiveBoundingBoxMinimum = null;
        value.ModelActiveBoundingBoxMaximum = null;
        value.ModelAnimationBoundingBoxMinimum = null;
        value.ModelAnimationBoundingBoxMaximum = null;
        value.ModelCollisionBoundingBoxMinimum = null;
        value.ModelCollisionBoundingBoxMaximum = null;
        value.ModelMaxBoundingBoxMinimum = null;
        value.ModelMaxBoundingBoxMaximum = null;
        value.ModelCenter = Vector3.Zero;
        value.ModelPaused = false;
        value.ModelGlobalPaused = false;
        value.ModelAnimationKitId = null;
        value.ModelAnimationKitLooping = false;
        value.ModelAvailableAnimationIds.Clear();
        value.ModelAnimationIdsInResourceOrder.Clear();
        value.ModelSequencesInResourceOrder.Clear();
        value.ModelAnimationFiles.Clear();
        value.ModelPendingAnimationRequests.Clear();
        value.ModelGlobalSequenceDurationsMilliseconds.Clear();
        value.ModelCameras.Clear();
        value.ModelCameraLookupIndices.Clear();
        value.ModelCharacterCameraActive = false;
        value.ModelSelectedCameraIndex = null;
        ResetResolvedModelSequencePlayback(value);
    }

    private static void ResetCharacterModelRawResourceState(UiObject value)
    {
        value.ModelFileDataId = null;
        value.ModelBoneFileDataId = 0;
        value.ModelPath = null;
        value.ModelResourceLoaded = false;
        value.ModelNoMip = false;
        ResetCharacterModelResourceBoundState(value);
        ResetSimpleModelResourceState(value);
    }

    private static void ResetCharacterModelSourceState(UiObject value)
    {
        value.ModelFileDataId = null;
        value.ModelBoneFileDataId = 0;
        value.ModelCreatureDisplayId = null;
        value.ModelPath = null;
        value.ModelUnitToken = null;
        value.ModelGuildTabardInfo = null;
        value.ModelResourceLoaded = false;
        value.ModelDisplayId = 0;
        value.ModelMountDisplayId = 0;
        value.ModelCreatureId = 0;
        value.ModelItemId = 0;
        value.ModelItemAppearanceModifierId = 0;
        value.ModelItemVisualId = 0;
        value.ModelItemAppearanceId = 0;
        value.ModelItemSubclass = -1;
        value.ModelBarberShopAlternateForm = false;
        value.ModelUseNativeForm = true;
        value.ModelDoBlend = true;
        value.ModelDisplayScaleMultiplier = 1;
        value.ModelActiveBoundingBoxMinimum = null;
        value.ModelActiveBoundingBoxMaximum = null;
        value.ModelAnimationBoundingBoxMinimum = null;
        value.ModelAnimationBoundingBoxMaximum = null;
        value.ModelCollisionBoundingBoxMinimum = null;
        value.ModelCollisionBoundingBoxMaximum = null;
        value.ModelMaxBoundingBoxMinimum = null;
        value.ModelMaxBoundingBoxMaximum = null;
        value.ModelNoMip = false;
        ResetCharacterModelAnimationState(value);
        ResetSimpleModelResourceState(value);
    }

    private static WowClubFinderTabardInfoState? ResolveModelGuildTabardInfo(
        LuaRuntime runtime,
        string unitToken)
    {
        if (runtime.Guild.TabardInfoByUnit.TryGetValue(
                unitToken,
                out var unitTabard))
        {
            return unitTabard;
        }

        return unitToken.Equals("player", StringComparison.OrdinalIgnoreCase)
            ? runtime.Guild.DefaultTabardInfo
            : null;
    }

    private static void ResetCharacterModelAnimationState(UiObject value)
    {
        value.ModelAnimationId = 0;
        value.ModelAnimationVariation = -1;
        value.ModelAnimationFrozenFrame = -1;
        value.ModelAnimationSpeed = 1;
        value.ModelAnimationTimeOffsetMilliseconds = 0;
        CancelCharacterRotationAnimation(value);
        ResetCharacterModelResourceBoundState(value);
    }

    private static void ResetCharacterModelResourceBoundState(UiObject value)
    {
        ClearCharacterModelAnimationKitState(value);
        value.ModelAppliedSpellVisualKits.Clear();
        value.ModelSpellVisualKitId = null;
        value.ModelSpellVisualOneShot = false;
        value.ModelAvailableAnimationIds.Clear();
        value.ModelAnimationIdsInResourceOrder.Clear();
        value.ModelSequencesInResourceOrder.Clear();
        value.ModelAnimationFiles.Clear();
        value.ModelPendingAnimationRequests.Clear();
        value.ModelGlobalSequenceDurationsMilliseconds.Clear();
        value.ModelCameras.Clear();
        value.ModelCameraLookupIndices.Clear();
        value.ModelCharacterCameraActive = false;
        value.ModelSelectedCameraIndex = null;
        value.ModelActiveBoundingBoxMinimum = null;
        value.ModelActiveBoundingBoxMaximum = null;
        value.ModelAnimationBoundingBoxMinimum = null;
        value.ModelAnimationBoundingBoxMaximum = null;
        value.ModelCollisionBoundingBoxMinimum = null;
        value.ModelCollisionBoundingBoxMaximum = null;
        value.ModelMaxBoundingBoxMinimum = null;
        value.ModelMaxBoundingBoxMaximum = null;
    }

    private static void ResetSimpleModelResourceState(UiObject value)
    {
        value.ModelBoneFileDataId = 0;
        value.ModelHasAttachmentPoints = false;
        value.ModelAlpha = value.Alpha;
        value.ModelParticlesEnabled = true;
        value.ModelSequenceId = 0;
        value.ModelSequenceTimeOffset = 0;
        value.ModelResolvedSequenceId = null;
        value.ModelSelectedSequenceIndex = -1;
        value.ModelResolvedSequenceIndex = -1;
        value.ModelResolvedSequenceVariation = 0;
        value.ModelResolvedSequenceDurationMilliseconds = 0;
        value.ModelSequenceElapsedMilliseconds = 0;
        value.ModelSequenceInitialElapsedMilliseconds = 0;
        value.ModelSequencePlaybackClockMilliseconds = 0;
        value.ModelSequencePlaybackSpeed = 1;
        value.ModelSequenceRepeatCount = 1;
        value.ModelGlobalSequenceElapsedMilliseconds = 0;
        value.ModelSequencePlaying = false;
        value.ModelSequenceLoops = false;
        value.ModelSequencePlaybackRevision++;
        value.ModelIconTextureFileDataId = null;
        value.ModelIconTexturePath = null;
        value.ModelShadowEffectStrength = 0;
        value.ModelShadowEffectState = null;
        value.ModelDissolveEffectState = null;
        value.ModelEdgeGlowEffectState = null;
        value.ModelRenderEffectKind = UiModelRenderEffectKind.None;
        value.ModelGradientMaskEnabled = false;
        value.ModelDesaturation = 0;
        value.ModelPaused = false;
        value.ModelCenter = Vector3.Zero;
        value.ModelRenderCameraState = null;
        value.ModelCharacterCameraActive = false;
    }

    private static bool TryReadRequiredModelAsset(
        lua_State state,
        int index,
        out uint? fileDataId,
        out string? path)
    {
        fileDataId = null;
        path = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return false;

        if (lua_type(state, index) == LUA_TNUMBER)
        {
            if (!TryReadRequiredUInt32(state, index, out var id))
                return false;
            fileDataId = id;
            return true;
        }

        if (lua_type(state, index) != LUA_TSTRING)
            return false;
        path = lua_tostring(state, index);
        return true;
    }

    private static uint ResolveFileAssetId(
        LuaRuntime runtime,
        uint? fileDataId,
        string? path)
    {
        if (fileDataId is { } numericId)
            return numericId;
        if (path is null)
            return 0;
        return runtime.ModelResourceProvider?.ResolveFileDataId(path) ?? 0;
    }

    private static bool IsAvailableModelResource(
        LuaRuntime runtime,
        uint fileDataId)
    {
        if (fileDataId == 0)
            return false;
        return runtime.ModelResourceProvider is not { } provider ||
               provider.FileExists(fileDataId) ||
               provider.SimulateUnresolvedModels;
    }

    private static int StringSplitTable(lua_State state)
    {
        if (!TryReadRequiredString(state, 1, out var delimiters) ||
            !TryReadRequiredString(state, 2, out var value) ||
            (lua_gettop(state) >= 3 && lua_isnil(state, 3) == 0 && lua_isnumber(state, 3) == 0))
        {
            return luaL_error(state, "Usage: strsplittable(delimiters, string [, pieces])");
        }

        var maximumPieces = lua_gettop(state) >= 3 && lua_isnil(state, 3) == 0
            ? (int)lua_tonumber(state, 3)
            : 0;
        lua_newtable(state);
        var piece = 1;
        var start = 0;
        if (maximumPieces >= 0 && maximumPieces != 1 && delimiters.Length > 0)
        {
            for (var index = 0; index < value.Length; index++)
            {
                if (!delimiters.Contains(value[index]))
                    continue;

                lua_pushstring(state, value[start..index]);
                lua_rawseti(state, -2, piece++);
                start = index + 1;
                if (piece == maximumPieces)
                    break;
            }
        }

        lua_pushstring(state, value[start..]);
        lua_rawseti(state, -2, piece);
        return 1;
    }

    private static int FastRandom(lua_State state)
    {
        var argumentCount = lua_gettop(state);
        var sample = Random.Shared.NextDouble();
        if (argumentCount == 0)
        {
            lua_pushnumber(state, sample);
            return 1;
        }
        if (argumentCount > 2 || lua_isnumber(state, 1) == 0 ||
            (argumentCount == 2 && lua_isnumber(state, 2) == 0))
        {
            return luaL_error(state, "wrong number of arguments");
        }

        var lower = argumentCount == 1 ? 1 : (int)lua_tonumber(state, 1);
        var upper = (int)lua_tonumber(state, argumentCount);
        if (lower > upper)
            return luaL_error(state, "interval is empty");

        var value = Math.Floor((upper - (double)lower + 1) * sample) + lower;
        lua_pushnumber(state, value);
        return 1;
    }

    private static (string? Asset, uint? FileDataId) ReadTextureAsset(
        lua_State state,
        int index) =>
        lua_type(state, index) == LUA_TNUMBER
            ? (null, (uint)Math.Max(0, lua_tonumber(state, index)))
            : (OptionalString(state, index), null);

    private static bool TryReadRequiredTextureAsset(
        lua_State state,
        int index,
        out string? asset,
        out uint? fileDataId)
    {
        asset = null;
        fileDataId = null;
        if (!HasRequiredValue(state, index))
            return false;
        if (lua_type(state, index) == LUA_TNUMBER)
        {
            if (!TryReadRequiredUInt32(state, index, out var id))
                return false;
            fileDataId = id;
            return true;
        }
        if (lua_type(state, index) != LUA_TSTRING)
            return false;
        asset = lua_tostring(state, index);
        return true;
    }

    private static bool TryReadRequiredAtlasName(
        lua_State state,
        int index,
        out string atlasName)
    {
        atlasName = string.Empty;
        if (!HasRequiredValue(state, index) ||
            lua_isstring(state, index) == 0 ||
            lua_istable(state, index) != 0)
            return false;
        atlasName = lua_tostring(state, index) ?? string.Empty;
        return true;
    }

    private static int PushTextureAsset(lua_State state, string? asset, uint? fileDataId)
    {
        if (fileDataId is { } id)
            lua_pushinteger(state, id);
        else
            PushOptionalString(state, asset);
        return 1;
    }

    private static bool TrySetMinimapBlobTexture(
        lua_State state,
        UiMinimapBlobStyle style,
        string component)
    {
        if (!TryReadRequiredTextureAsset(
                state,
                2,
                out var asset,
                out var fileDataId))
            return false;
        switch (component)
        {
            case "inside":
                style.InsideTexture = asset;
                style.InsideTextureFileDataId = fileDataId;
                break;
            case "outside":
                style.OutsideTexture = asset;
                style.OutsideTextureFileDataId = fileDataId;
                break;
            case "ring":
                style.RingTexture = asset;
                style.RingTextureFileDataId = fileDataId;
                break;
        }
        return true;
    }

    private static void DesaturateHierarchy(
        LuaRuntime runtime,
        UiObject root,
        float desaturation,
        bool excludeRoot)
    {
        foreach (var childId in root.Children)
        {
            if (runtime.Ui.Find(childId) is not { } child)
                continue;
            if (child.IsRegion)
            {
                if (!excludeRoot)
                    ApplyDesaturation(runtime, child, desaturation);
            }
            else if (WowWidgetApi.IsFrameWidget(child.ObjectType))
            {
                DesaturateHierarchy(runtime, child, desaturation, false);
            }
        }
    }

    private static void ApplyDesaturation(
        LuaRuntime runtime,
        UiObject value,
        float desaturation)
    {
        if (value.Texture is { } texture)
            texture.Desaturation = desaturation;

        if (value.StatusBar is { } statusBar)
        {
            if (statusBar.TextureId is { } textureId &&
                runtime.Ui.Find(textureId)?.Texture is { } statusBarTexture)
                statusBarTexture.Desaturation = desaturation;
        }

        if (value.ModelScene is not null ||
            value.ObjectType.Equals("ModelSceneActor", StringComparison.OrdinalIgnoreCase) ||
            value.ObjectType.EndsWith("Model", StringComparison.OrdinalIgnoreCase))
            value.ModelDesaturation = desaturation;
    }

    private static bool IsFontStringTruncated(LuaRuntime runtime, UiObject value)
    {
        return ResolveFontStringDisplayText(runtime, value).WasTruncated;
    }

    private static UiDisplayTextResult ResolveFontStringDisplayText(
        LuaRuntime runtime,
        UiObject value)
    {
        var font = EnsureFont(value);
        if (!font.IsConfigured || font.Text.Length == 0)
            return new UiDisplayTextResult(font.Text, false);

        var scale = runtime.Ui.LayoutScale(value);
        var bounds = runtime.Ui.ResolveBounds(value.Id);
        if (MathF.Abs(scale) < 0.000001f)
            return new UiDisplayTextResult(font.Text, false);

        var widthConstrained = UiSystem.IsWidthConstrained(value);
        var heightConstrained = UiSystem.IsHeightConstrained(value);
        var availableWidth = widthConstrained
            ? bounds.Width / scale
            : float.PositiveInfinity;
        var availableHeight = heightConstrained
            ? bounds.Height / scale
            : float.PositiveInfinity;
        if (!widthConstrained &&
            !heightConstrained &&
            font.MaximumLines == 0)
        {
            return new UiDisplayTextResult(font.Text, false);
        }

        var effectiveScale = runtime.Ui.EffectiveScale(value);
        var lineHeight = UiTextLineMetrics.ResolveLogicalLineHeight(
            font.FontSize,
            font.TextScale,
            runtime.Ui.PhysicalHeight,
            effectiveScale,
            value.FontSmoothScaling);
        var lineAdvance =
            lineHeight +
            UiTextLineMetrics.ResolveLogicalSpacing(
                font.Spacing,
                runtime.Ui.PhysicalHeight,
                effectiveScale);
        var fittingHeight = font.MaximumLines > 0
            ? font.MaximumLines * lineAdvance
            : Math.Max(lineAdvance, availableHeight);
        return UiDisplayTextFitter.Resolve(
            font.Text,
            candidate =>
            {
                var measurement = MeasureText(
                    runtime,
                    value,
                    ignoreMaximumLines: true,
                    textOverride: candidate,
                    availableWidthOverride: availableWidth);
                return (!widthConstrained ||
                        measurement.Size.X <= availableWidth + 0.001f) &&
                       (!heightConstrained && font.MaximumLines == 0 ||
                        measurement.Size.Y <= fittingHeight + 0.001f);
            });
    }

    private static void SetNativeStatusBarValue(
        LuaRuntime runtime,
        UiObject value,
        UiStatusBarState statusBar,
        double requestedValue,
        int interpolation)
    {
        if (!statusBar.RangeInitialized)
            return;

        statusBar.InterpolationActive = interpolation == 1;
        var next = Math.Clamp(
            (float)requestedValue,
            (float)statusBar.Minimum,
            (float)statusBar.Maximum);
        if (statusBar.ValueInitialized &&
            (float)statusBar.Value == next)
        {
            return;
        }

        statusBar.Value = next;
        statusBar.ValueInitialized = true;
        if (!statusBar.InterpolationActive)
            statusBar.DisplayNormalizedValue =
                runtime.StatusBarTargetNormalized(statusBar);
        runtime.Ui.InvalidateLayout();
        runtime.InvokeScript(value, "OnValueChanged", next);
    }

    private static bool TryReadRequiredDurationObject(
        lua_State state,
        int index,
        out UiDurationState duration) =>
        WowDurationApi.TryRead(state, index, out duration);

    private static bool TryReadOptionalStatusBarTimerDirection(
        lua_State state,
        int index,
        out int direction)
    {
        direction = 0;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number != Math.Truncate(number))
            return false;
        direction = (int)number;
        return direction is 0 or 1;
    }

    private static void PushDurationObject(
        lua_State state,
        UiDurationState duration) =>
        WowDurationApi.Push(state, duration);

    private static UiObject? StatusBarTexture(
        LuaRuntime runtime,
        UiStatusBarState statusBar) =>
        statusBar.TextureId is { } textureId
            ? runtime.Ui.Find(textureId)
            : null;

    private static bool TryReadRequiredStatusBarFillStyle(
        lua_State state,
        int index,
        out int fillStyle)
    {
        fillStyle = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number != Math.Truncate(number))
            return false;
        fillStyle = (int)number;
        return fillStyle is >= 0 and <= 3;
    }

    private static bool TryReadOptionalStatusBarInterpolation(
        lua_State state,
        int index,
        out int interpolation)
    {
        interpolation = 0;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number != Math.Truncate(number))
            return false;
        interpolation = (int)number;
        return interpolation is 0 or 1;
    }

    private static bool HasRequiredNormalizedColor(lua_State state, int start)
    {
        if (lua_isnumber(state, start) == 0 ||
            lua_isnumber(state, start + 1) == 0 ||
            lua_isnumber(state, start + 2) == 0)
        {
            return false;
        }
        return start + 3 > lua_gettop(state) ||
               lua_isnil(state, start + 3) != 0 ||
               lua_isnumber(state, start + 3) != 0;
    }

    private static int SetStatusBarTexture(LuaRuntime runtime, UiObject value)
    {
        var state = runtime.State;
        if (lua_gettop(state) < 2)
            return luaL_error(
                state,
                "Usage: local success = self:SetStatusBarTexture(asset)");
        const string usage =
            "Usage: local success = self:SetStatusBarTexture(asset)";
        var statusBar = EnsureStatusBar(value);
        var argumentType = lua_type(state, 2);
        var suppliedTexture = GetObject(runtime, 2);
        if (suppliedTexture is not null && suppliedTexture.Texture is null)
            return luaL_error(state, usage);
        if (suppliedTexture is null &&
            argumentType is not (LUA_TNIL or LUA_TNUMBER or LUA_TSTRING))
        {
            return luaL_error(state, usage);
        }
        uint? requestedFileDataId = null;
        if (argumentType == LUA_TNUMBER)
        {
            if (!TryReadRequiredUInt32(state, 2, out var fileDataId))
                return luaL_error(state, usage);
            requestedFileDataId = fileDataId;
        }

        var previous = StatusBarTexture(runtime, statusBar);
        if (suppliedTexture is not null &&
            suppliedTexture.Id == previous?.Id)
        {
            lua_pushboolean(state, 1);
            return 1;
        }

        UiObject? texture = suppliedTexture;
        var success = true;

        if (texture is null)
        {
            texture = previous ??
                      CreateObject(runtime, "Texture", null, value, "ARTWORK");
            texture.AllPointsTargetId = value.Id;
            ClearTextureAsset(EnsureTexture(texture));

            if (requestedFileDataId is { } fileDataId)
            {
                EnsureTexture(texture).FileDataId = fileDataId;
            }
            else if (OptionalString(state, 2) is { } asset)
            {
                if (!runtime.ApplyAtlas(texture, asset, useAtlasSize: false))
                    EnsureTexture(texture).Asset = asset;
            }
            else
            {
                success = false;
            }
        }

        if (previous is not null && previous.Id != texture.Id)
            previous.Shown = false;
        if (texture.ParentId != value.Id)
            runtime.Ui.Reparent(texture, value.Id);
        texture.DrawLayer = "ARTWORK";
        texture.SubLevel = 0;
        texture.Shown = true;
        if (texture.AllPointsTargetId is null && texture.Anchors.Count == 0)
            texture.AllPointsTargetId = value.Id;

        statusBar.TextureId = texture.Id;
        runtime.Ui.InvalidateLayout();
        lua_pushboolean(state, success ? 1 : 0);
        return 1;
    }

    private static void SetStatusBarColorFill(LuaRuntime runtime, UiObject value)
    {
        var statusBar = EnsureStatusBar(value);
        var texture = statusBar.TextureId is { } existingId
            ? runtime.Ui.Find(existingId)
            : null;
        texture ??= CreateObject(runtime, "Texture", null, value, "ARTWORK");
        if (texture.ParentId != value.Id)
            runtime.Ui.Reparent(texture, value.Id);
        texture.DrawLayer = "ARTWORK";
        texture.SubLevel = 0;
        texture.Shown = true;
        texture.AllPointsTargetId = value.Id;

        var textureState = EnsureTexture(texture);
        textureState.IsColor = true;
        textureState.Asset = null;
        textureState.AtlasName = null;
        textureState.AtlasWidth = null;
        textureState.AtlasHeight = null;
        textureState.FileDataId = null;
        textureState.Gradient = null;
        textureState.ClearAtlasRegion();
        textureState.Color = ReadNormalizedColor(runtime.State, 2, 1);
        textureState.VertexColor = Vector4.One;
        statusBar.TextureId = texture.Id;
    }

    private readonly record struct TextMeasurement(Vector2 Size, int LineCount);

    private sealed record FontStringScreenLine(
        IReadOnlyList<int> ByteBoundaries,
        float Left,
        float Bottom,
        float Top,
        float HitBottom,
        float CharacterWidth)
    {
        public int StartByteOffset => ByteBoundaries[0];
        public int EndByteOffset => ByteBoundaries[^1];
        public float Right => Left + (ByteBoundaries.Count - 1) * CharacterWidth;

        public int CharacterIndexAtOrBefore(int byteOffset)
        {
            var index = 0;
            while (index + 1 < ByteBoundaries.Count &&
                   ByteBoundaries[index + 1] <= byteOffset)
            {
                index++;
            }
            return index;
        }
    }

    private static bool TryBuildFontStringScreenLines(
        LuaRuntime runtime,
        UiObject value,
        out List<FontStringScreenLine> result,
        bool applySpanIndent = false)
    {
        result = [];
        var font = EnsureFont(value);
        if (font.Text.Length == 0 || !font.IsConfigured)
            return false;

        var displayText = ResolveFontStringDisplayText(runtime, value).Text;
        var plainText = WowTextMarkup.PlainText(displayText)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var nativeEffectiveScale = runtime.Ui.EffectiveScale(value);
        var layoutScale = runtime.Ui.LayoutScale(value);
        var logicalLineHeight = UiTextLineMetrics.ResolveLogicalLineHeight(
            font.FontSize,
            font.TextScale,
            runtime.Ui.PhysicalHeight,
            nativeEffectiveScale,
            value.FontSmoothScaling);
        var characterWidth = logicalLineHeight * 0.54f * layoutScale;
        var bounds = runtime.Ui.ResolveBounds(value.Id);
        var maximumCharacters = font.WordWrap && bounds.Width > 0 && characterWidth > 0
            ? Math.Max(1, (int)MathF.Floor(bounds.Width / characterWidth))
            : int.MaxValue;
        var indentWidth = font.IndentedWordWrap
            ? UiTextLineMetrics.ResolveLogicalIndentedWordWrapWidth(
                  runtime.Ui.PhysicalHeight,
                  nativeEffectiveScale) *
              layoutScale
            : 0;
        var continuationMaximumCharacters =
            maximumCharacters != int.MaxValue && characterWidth > 0
                ? Math.Max(
                    1,
                    (int)MathF.Floor(
                        MathF.Max(0, bounds.Width - indentWidth) /
                        characterWidth))
                : int.MaxValue;
        var segments = new List<IReadOnlyList<int>>();
        var logicalByteStart = 0;
        foreach (var logicalLine in plainText.Split('\n'))
        {
            var runes = logicalLine.EnumerateRunes().ToArray();
            var byteBoundaries = new int[runes.Length + 1];
            byteBoundaries[0] = logicalByteStart;
            for (var index = 0; index < runes.Length; index++)
            {
                byteBoundaries[index + 1] =
                    byteBoundaries[index] + runes[index].Utf8SequenceLength;
            }
            foreach (var segment in WrappedRuneLineSegments(
                         runes,
                         segments.Count == 0
                             ? maximumCharacters
                             : continuationMaximumCharacters,
                         continuationMaximumCharacters,
                         font.NonSpaceWrap))
            {
                segments.Add(
                    byteBoundaries
                        .Skip(segment.Start)
                        .Take(segment.Length + 1)
                        .ToArray());
            }
            logicalByteStart =
                byteBoundaries[^1] + 1;
        }
        if (segments.Count > 1023)
            segments.RemoveRange(1023, segments.Count - 1023);
        if (segments.Count == 0)
            return false;

        var maximumWidth = segments.Max(
            segment => (segment.Count - 1) * characterWidth);
        var lineHeight = logicalLineHeight * layoutScale;
        var lineAdvance =
            lineHeight +
            UiTextLineMetrics.ResolveLogicalSpacing(
                font.Spacing,
                runtime.Ui.PhysicalHeight,
                nativeEffectiveScale) *
            layoutScale;
        var totalHeight = lineHeight + Math.Max(0, segments.Count - 1) * lineAdvance;
        var topLeft = runtime.Ui.ResolveTextTopLeft(value, maximumWidth, totalHeight);
        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            var width = (segment.Count - 1) * characterWidth;
            var horizontalOffset = font.JustifyHorizontal.ToUpperInvariant() switch
            {
                "RIGHT" => maximumWidth - width,
                "CENTER" => (maximumWidth - width) * 0.5f,
                _ => 0
            };
            if (applySpanIndent && index > 0)
                horizontalOffset += indentWidth;
            var top = topLeft.Y - index * lineAdvance;
            result.Add(
                new FontStringScreenLine(
                    segment,
                    topLeft.X + horizontalOffset,
                    top - lineHeight,
                    top,
                    top - lineAdvance,
                    characterWidth));
        }
        return true;
    }

    private static IEnumerable<(int Start, int Length)> WrappedRuneLineSegments(
        IReadOnlyList<Rune> line,
        int firstMaximumCharacters,
        int continuationMaximumCharacters,
        bool nonSpaceWrap)
    {
        var maximumCharacters = firstMaximumCharacters;
        if (line.Count == 0 ||
            maximumCharacters == int.MaxValue ||
            line.Count <= maximumCharacters)
        {
            yield return (0, line.Count);
            yield break;
        }

        var start = 0;
        while (start < line.Count)
        {
            var remaining = line.Count - start;
            if (remaining <= maximumCharacters)
            {
                yield return (start, remaining);
                yield break;
            }

            var limit = start + maximumCharacters;
            var breakAt = -1;
            for (var index = limit - 1; index >= start; index--)
            {
                if (Rune.IsWhiteSpace(line[index]))
                {
                    breakAt = index;
                    break;
                }
            }
            if (breakAt < start)
            {
                if (!nonSpaceWrap)
                {
                    yield return (start, remaining);
                    yield break;
                }
                breakAt = limit;
                yield return (start, maximumCharacters);
                start = breakAt;
            }
            else
            {
                yield return (start, Math.Max(0, breakAt - start));
                start = breakAt + 1;
            }

            while (start < line.Count && Rune.IsWhiteSpace(line[start]))
                start++;
            maximumCharacters = continuationMaximumCharacters;
        }
    }

    private static TextMeasurement MeasureText(
        LuaRuntime runtime,
        UiObject value,
        UiFontState? fontOverride = null,
        bool ignoreMaximumLines = false,
        bool ignoreWidthConstraint = false,
        string? textOverride = null,
        float? availableWidthOverride = null)
    {
        var font = fontOverride ?? EnsureFont(value);
        var measuredText = textOverride ?? font.Text;
        if (measuredText.Length == 0 || !font.IsConfigured)
            return new TextMeasurement(Vector2.Zero, 0);

        var plainText = value.EditBoxPassword &&
                        fontOverride is null &&
                        textOverride is null
            ? new string('*', Encoding.UTF8.GetByteCount(value.TextValue))
            : WowTextMarkup.PlainText(measuredText)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');
        var effectiveScale = runtime.Ui.EffectiveScale(value);
        var lineHeight = UiTextLineMetrics.ResolveLogicalLineHeight(
            font.FontSize,
            font.TextScale,
            runtime.Ui.PhysicalHeight,
            effectiveScale,
            value.FontSmoothScaling);
        if (!(lineHeight > 0))
            return new TextMeasurement(Vector2.Zero, 0);
        var positiveShadowWidth =
            UiTextLineMetrics.ResolveLogicalPositiveShadowWidth(
                font.ShadowOffset.X,
                runtime.Ui.PhysicalHeight,
                effectiveScale);
        float? availableWidth = ignoreWidthConstraint
            ? null
            : availableWidthOverride ?? value.Width;
        if (!ignoreWidthConstraint &&
            IsEditBox(value) &&
            availableWidth is { } editBoxWidth)
        {
            availableWidth = Math.Max(
                0,
                editBoxWidth - value.TextInsets.X - value.TextInsets.Y);
        }
        var maximumWidth = font.WordWrap && availableWidth is > 0
            ? MathF.Max(0, availableWidth.Value - positiveShadowWidth)
            : float.PositiveInfinity;
        var indentWidth = font.IndentedWordWrap
            ? UiTextLineMetrics.ResolveLogicalIndentedWordWrapWidth(
                runtime.Ui.PhysicalHeight,
                effectiveScale)
            : 0;
        var continuationMaximumWidth = float.IsFinite(maximumWidth)
            ? MathF.Max(0, maximumWidth - indentWidth)
            : float.PositiveInfinity;
        var measuredLines = UiMeasuredTextWrapping.Wrap(
                plainText,
                maximumWidth,
                continuationMaximumWidth,
                font.NonSpaceWrap,
                line => runtime.Ui.MeasureTextAdvance(font, line, lineHeight))
            .ToList();
        if (!ignoreMaximumLines &&
            font.MaximumLines > 0 &&
            measuredLines.Count > font.MaximumLines)
        {
            measuredLines.RemoveRange(
                font.MaximumLines,
                measuredLines.Count - font.MaximumLines);
        }
        if (measuredLines.Count == 0)
            measuredLines.Add(string.Empty);

        var width = measuredLines
                        .Select(line => runtime.Ui.MeasureTextAdvance(
                            font,
                            line,
                            lineHeight))
                        .DefaultIfEmpty(0)
                        .Max() +
                    positiveShadowWidth;
        var spacing = UiTextLineMetrics.ResolveLogicalSpacing(
            font.Spacing,
            runtime.Ui.PhysicalHeight,
            effectiveScale);
        var height =
            measuredLines.Count * lineHeight +
            Math.Max(0, measuredLines.Count - 1) * spacing;
        return new TextMeasurement(
            new Vector2(width, height),
            measuredLines.Count);
    }

    private static bool IsFrameObject(UiObject value) =>
        value.Animation is null &&
        !value.IsRegion &&
        !value.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase) &&
        !value.ObjectType.Equals("AnimationGroup", StringComparison.OrdinalIgnoreCase) &&
        !value.ObjectType.Equals("Animation", StringComparison.OrdinalIgnoreCase) &&
        !value.ObjectType.Equals("Alpha", StringComparison.OrdinalIgnoreCase) &&
        !value.ObjectType.Equals("Translation", StringComparison.OrdinalIgnoreCase) &&
        !value.ObjectType.Equals("Rotation", StringComparison.OrdinalIgnoreCase) &&
        !value.ObjectType.Equals("Scale", StringComparison.OrdinalIgnoreCase) &&
        !value.ObjectType.Equals("ModelSceneActor", StringComparison.OrdinalIgnoreCase);

    private static string ExpandAnimationTargetName(
        LuaRuntime runtime,
        UiObject animation,
        string targetName)
    {
        if (!targetName.Contains("$parent", StringComparison.OrdinalIgnoreCase))
            return targetName;
        var owner = animation.ParentId is { } groupId &&
                    runtime.Ui.Find(groupId)?.ParentId is { } ownerId
            ? runtime.Ui.Find(ownerId)
            : null;
        while (owner is not null && string.IsNullOrWhiteSpace(owner.Name))
        {
            owner = owner.ParentId is { } parentId ? runtime.Ui.Find(parentId) : null;
        }
        return owner?.Name is { } ownerName
            ? targetName.Replace(
                "$parent",
                ownerName,
                StringComparison.OrdinalIgnoreCase)
            : targetName;
    }

    internal static UiObject? GetObject(LuaRuntime runtime, int index) =>
        GetObject(runtime, runtime.State, index);

    private static unsafe UiObject? GetObject(
        LuaRuntime runtime,
        lua_State state,
        int index)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        if (lua_isstring(state, index) != 0 && lua_istable(state, index) == 0)
            return runtime.Ui.Find(lua_tostring(state, index) ?? string.Empty);
        if (lua_istable(state, index) == 0)
            return null;
        var absolute = AbsoluteIndex(state, index);
        lua_pushstring(state, "__id");
        lua_rawget(state, absolute);
        var id = lua_isnumber(state, -1) != 0 ? (int)lua_tonumber(state, -1) : 0;
        lua_pop(state, 1);
        if (id == 0)
        {
            lua_rawgeti(state, absolute, 0);
            if (lua_type(state, -1) == LUA_TLIGHTUSERDATA)
                id = (int)(nint)lua_touserdata(state, -1);
            lua_pop(state, 1);
        }
        return runtime.Ui.Find(id);
    }

    private static Vector4 ReadVector4(lua_State state, int start, float defaultAlpha) =>
        new(
            (float)OptionalNumber(state, start),
            (float)OptionalNumber(state, start + 1),
            (float)OptionalNumber(state, start + 2),
            (float)OptionalNumber(state, start + 3, defaultAlpha));

    private static int PushVector2(lua_State state, Vector2 value)
    {
        lua_pushnumber(state, value.X);
        lua_pushnumber(state, value.Y);
        return 2;
    }

    private static int PushColorMixin(lua_State state, Vector4 color)
    {
        lua_getglobal(state, "CreateColor");
        if (lua_isfunction(state, -1) != 0)
        {
            lua_pushnumber(state, color.X);
            lua_pushnumber(state, color.Y);
            lua_pushnumber(state, color.Z);
            lua_pushnumber(state, color.W);
            if (lua_pcall(state, 4, 1, 0) == 0)
                return 1;
            lua_pop(state, 1);
        }
        else
        {
            lua_pop(state, 1);
        }

        lua_newtable(state);
        lua_pushnumber(state, color.X);
        lua_setfield(state, -2, "r");
        lua_pushnumber(state, color.Y);
        lua_setfield(state, -2, "g");
        lua_pushnumber(state, color.Z);
        lua_setfield(state, -2, "b");
        lua_pushnumber(state, color.W);
        lua_setfield(state, -2, "a");
        return 1;
    }

    internal static void PushItemTransmogInfo(
        lua_State state,
        UiItemTransmogInfo info)
    {
        lua_newtable(state);
        var tableIndex = AbsoluteIndex(state, -1);
        lua_pushinteger(state, info.AppearanceId);
        lua_setfield(state, tableIndex, "appearanceID");
        lua_pushinteger(state, info.SecondaryAppearanceId);
        lua_setfield(state, tableIndex, "secondaryAppearanceID");
        lua_pushinteger(state, info.IllusionId);
        lua_setfield(state, tableIndex, "illusionID");

        lua_getglobal(state, "Mixin");
        if (lua_isfunction(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        lua_pushvalue(state, tableIndex);
        lua_getglobal(state, "ItemTransmogInfoMixin");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 3);
            return;
        }

        if (lua_pcall(state, 2, 1, 0) == 0)
        {
            lua_remove(state, tableIndex);
            return;
        }

        lua_pop(state, 1);
    }

    private static bool TryReadItemTransmogInfo(
        lua_State state,
        int index,
        out UiItemTransmogInfo info)
    {
        info = default;
        if (index > lua_gettop(state) || lua_istable(state, index) == 0)
            return false;

        var tableIndex = AbsoluteIndex(state, index);
        if (!TryReadItemTransmogInfoField(
                state,
                tableIndex,
                "appearanceID",
                out var appearanceId) ||
            !TryReadItemTransmogInfoField(
                state,
                tableIndex,
                "secondaryAppearanceID",
                out var secondaryAppearanceId) ||
            !TryReadItemTransmogInfoField(
                state,
                tableIndex,
                "illusionID",
                out var illusionId))
        {
            return false;
        }

        info = new UiItemTransmogInfo(
            appearanceId,
            secondaryAppearanceId,
            illusionId);
        return true;
    }

    private static bool TryReadItemTransmogInfoField(
        lua_State state,
        int tableIndex,
        string field,
        out int value)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_isnumber(state, -1) == 0)
        {
            lua_pop(state, 1);
            value = 0;
            return false;
        }

        value = unchecked((int)lua_tonumber(state, -1));
        lua_pop(state, 1);
        return true;
    }

    private static bool TryReadRequiredVector2(
        lua_State state,
        int start,
        out Vector2 value)
    {
        value = default;
        if (!TryReadRequiredFloat(state, start, out var x) ||
            !TryReadRequiredFloat(state, start + 1, out var y))
            return false;
        value = new Vector2((float)x, (float)y);
        return true;
    }

    private static Vector4 ReadNormalizedColor(lua_State state, int start, float defaultAlpha) =>
        new(
            QuantizeNormalizedByte(OptionalNumber(state, start)),
            QuantizeNormalizedByte(OptionalNumber(state, start + 1)),
            QuantizeNormalizedByte(OptionalNumber(state, start + 2)),
            QuantizeNormalizedByte(OptionalNumber(state, start + 3, defaultAlpha)));

    private static bool TryReadRequiredInt32(lua_State state, int index, out int value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < int.MinValue || number > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static bool TryReadSecretAspectMask(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number))
            return false;
        var truncated = Math.Truncate(number);
        if (truncated is < 0 or > 0x7FFFFF)
            return false;
        value = (uint)truncated;
        return true;
    }

    private static bool TryReadOptionalInt32(
        lua_State state,
        int index,
        int fallback,
        out int value)
    {
        if (!HasRequiredValue(state, index))
        {
            value = fallback;
            return true;
        }
        return TryReadRequiredInt32(state, index, out value);
    }

    private static bool TryReadRequiredUInt32(lua_State state, int index, out uint value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || !IsUInt32(number))
            return false;
        value = (uint)number;
        return true;
    }

    private static bool TryReadRequiredByte(lua_State state, int index, out byte value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < byte.MinValue or > byte.MaxValue)
            return false;
        value = (byte)(int)number;
        return true;
    }

    private static bool TryReadOptionalInt8(
        lua_State state,
        int index,
        out sbyte value)
    {
        value = 0;
        if (!HasRequiredValue(state, index))
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < sbyte.MinValue or > sbyte.MaxValue)
            return false;
        value = (sbyte)number;
        return true;
    }

    private static bool TryParseNonzeroNativeInteger(
        string text,
        out int value)
    {
        value = 0;
        var index = 0;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
            index++;
        var negative = false;
        if (index < text.Length && text[index] is '+' or '-')
        {
            negative = text[index] == '-';
            index++;
        }

        var numberBase = 10;
        if (index + 1 < text.Length &&
            text[index] == '0' &&
            text[index + 1] is 'x' or 'X')
        {
            numberBase = 16;
            index += 2;
        }

        var sawDigit = false;
        var sawNonzeroDigit = false;
        for (; index < text.Length; index++)
        {
            var digit = text[index] switch
            {
                >= '0' and <= '9' => text[index] - '0',
                >= 'a' and <= 'f' => text[index] - 'a' + 10,
                >= 'A' and <= 'F' => text[index] - 'A' + 10,
                _ => -1
            };
            if (digit < 0 || digit >= numberBase)
                break;
            sawDigit = true;
            sawNonzeroDigit |= digit != 0;
        }

        if (!sawDigit || !sawNonzeroDigit)
            return false;
        value = negative ? -1 : 1;
        return true;
    }

    private static void SetEditBoxInputMode(
        UiObject value,
        bool alphabeticOnly,
        bool numericFullRange,
        bool password,
        bool numeric = false)
    {
        value.EditBoxAlphabeticOnly = alphabeticOnly;
        value.EditBoxNumericFullRange = numericFullRange;
        value.EditBoxPassword = password;
        value.Attributes["Numeric"] = numeric || numericFullRange;
    }

    private static void ReapplyEditBoxTextRules(LuaRuntime runtime, UiObject value)
    {
        var filtered = EditBoxTextRules.ApplyReplacement(value, value.TextValue);
        if (filtered.Equals(value.TextValue, StringComparison.Ordinal))
            return;
        SetObjectText(runtime, value, filtered);
        runtime.Ui.InvalidateLayout();
        runtime.QueueEditBoxTextChanged(value, false);
        runtime.InvokeScript(value, "OnTextSet");
    }

    private static string TruncateUtf8(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes)
            return value;

        var result = new StringBuilder(value.Length);
        var usedBytes = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (usedBytes + rune.Utf8SequenceLength > maximumBytes)
                break;
            result.Append(rune);
            usedBytes += rune.Utf8SequenceLength;
        }
        return result.ToString();
    }

    private static void EnsureEditBoxHistoryCapacity(UiObject value)
    {
        while (value.EditBoxHistory.Count < value.EditBoxHistoryLines)
            value.EditBoxHistory.Add(null);
        if (value.EditBoxHistory.Count > value.EditBoxHistoryLines)
        {
            value.EditBoxHistory.RemoveRange(
                value.EditBoxHistoryLines,
                value.EditBoxHistory.Count - value.EditBoxHistoryLines);
        }
    }

    private static bool TryReadRequiredNormalizedColor(
        lua_State state,
        int start,
        out Vector4 color)
    {
        color = default;
        if (lua_gettop(state) < start + 2 ||
            lua_isnumber(state, start) == 0 ||
            lua_isnumber(state, start + 1) == 0 ||
            lua_isnumber(state, start + 2) == 0)
        {
            return false;
        }

        var alpha = 1d;
        if (lua_gettop(state) >= start + 3 && lua_isnil(state, start + 3) == 0)
        {
            if (lua_isnumber(state, start + 3) == 0)
                return false;
            alpha = lua_tonumber(state, start + 3);
        }

        color = new Vector4(
            QuantizeNormalizedByte(lua_tonumber(state, start)),
            QuantizeNormalizedByte(lua_tonumber(state, start + 1)),
            QuantizeNormalizedByte(lua_tonumber(state, start + 2)),
            QuantizeNormalizedByte(alpha));
        return true;
    }

    private static bool TryReadRequiredNormalizedRgb(
        lua_State state,
        int start,
        out Vector3 color)
    {
        color = default;
        if (lua_gettop(state) < start + 2 ||
            lua_isnumber(state, start) == 0 ||
            lua_isnumber(state, start + 1) == 0 ||
            lua_isnumber(state, start + 2) == 0)
        {
            return false;
        }

        color = new Vector3(
            QuantizeNormalizedByte(lua_tonumber(state, start)),
            QuantizeNormalizedByte(lua_tonumber(state, start + 1)),
            QuantizeNormalizedByte(lua_tonumber(state, start + 2)));
        return true;
    }

    private static bool TryReadOptionalMessageColor(
        lua_State state,
        int start,
        out Vector3? color)
    {
        color = null;
        if (start > lua_gettop(state) || lua_isnil(state, start) != 0)
            return true;
        if (lua_gettop(state) < start + 2 ||
            lua_isnumber(state, start) == 0 ||
            lua_isnumber(state, start + 1) == 0 ||
            lua_isnumber(state, start + 2) == 0)
        {
            return false;
        }

        var red = lua_tonumber(state, start);
        var green = lua_tonumber(state, start + 1);
        var blue = lua_tonumber(state, start + 2);
        if (!double.IsFinite(red) ||
            !double.IsFinite(green) ||
            !double.IsFinite(blue))
        {
            return false;
        }

        color = new Vector3(
            QuantizeNormalizedByte(red),
            QuantizeNormalizedByte(green),
            QuantizeNormalizedByte(blue));
        return true;
    }

    private static bool TryReadOptionalNormalizedByte(
        lua_State state,
        int index,
        out byte? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number))
            return false;
        value = (byte)MathF.Floor(Math.Clamp((float)number, 0, 1) * 255 + 0.5f);
        return true;
    }

    private static bool TryReadOptionalUInt32(
        lua_State state,
        int index,
        out uint? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (!TryReadRequiredUInt32(state, index, out var parsed))
            return false;
        value = parsed;
        return true;
    }

    private static float QuantizeNormalizedByte(double value) =>
        MathF.Floor(Math.Clamp((float)value, 0, 1) * 255 + 0.5f) / 255;

    private static float QuantizeNormalizedByteTruncated(double value) =>
        MathF.Truncate(Math.Clamp((float)value, 0, 1) * 255) / 255;

    private static bool IsUInt32(double value) =>
        value >= 0 &&
        value <= uint.MaxValue &&
        Math.Truncate(value) == value;

    private static bool TryReadRequiredOneBasedIndex(
        lua_State state,
        int index,
        out uint zeroBasedIndex)
    {
        zeroBasedIndex = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < 0 || number > uint.MaxValue)
            return false;
        var shifted = number - 1;
        var nativeIndex = shifted < int.MinValue || shifted > int.MaxValue
            ? int.MinValue
            : (int)shifted;
        zeroBasedIndex = unchecked((uint)nativeIndex);
        return true;
    }

    private static bool TryNormalizeFontFlags(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrEmpty(value))
            return true;

        string[] flags =
        [
            "OUTLINE",
            "THICKOUTLINE",
            "MONOCHROME",
            "FILTER",
            "FIXEDHEIGHT",
            "NEVERCULL",
            "SLUG"
        ];
        var matched = flags.Where(flag =>
            value.Contains(flag, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matched.Length == 0)
            return false;
        normalized = string.Join(", ", matched);
        return true;
    }

    private static string FormatFontFlags(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var present = value.Split(
                [',', ' ', '|'],
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] nativeOrder =
        [
            "OUTLINE",
            "THICKOUTLINE",
            "MONOCHROME",
            "FILTER",
            "FIXEDHEIGHT",
            "NEVERCULL",
            "SLUG"
        ];
        return string.Join(", ", nativeOrder.Where(present.Contains));
    }

    private static Vector3 ReadVector3(lua_State state, int start) =>
        new(
            (float)OptionalNumber(state, start),
            (float)OptionalNumber(state, start + 1),
            (float)OptionalNumber(state, start + 2));

    private static bool TryReadRequiredVector3(
        lua_State state,
        int start,
        out Vector3 value)
    {
        value = default;
        if (!TryReadRequiredFloat(state, start, out var x) ||
            !TryReadRequiredFloat(state, start + 1, out var y) ||
            !TryReadRequiredFloat(state, start + 2, out var z))
        {
            return false;
        }
        value = new Vector3((float)x, (float)y, (float)z);
        return true;
    }

    private static bool TryReadRequiredVector3Table(
        lua_State state,
        int index,
        out Vector3 value)
    {
        value = default;
        if (index > lua_gettop(state) || lua_istable(state, index) == 0)
            return false;
        var absolute = AbsoluteIndex(state, index);
        if (!TryReadRequiredTableFloat(state, absolute, "x", out var x) ||
            !TryReadRequiredTableFloat(state, absolute, "y", out var y) ||
            !TryReadRequiredTableFloat(state, absolute, "z", out var z))
        {
            return false;
        }
        value = new Vector3((float)x, (float)y, (float)z);
        return true;
    }

    private static bool TryReadOptionalVector3Table(
        lua_State state,
        int index,
        Vector3 fallback,
        out Vector3 value)
    {
        value = fallback;
        return index > lua_gettop(state) ||
               lua_isnil(state, index) != 0 ||
               TryReadRequiredVector3Table(state, index, out value);
    }

    private static bool TryReadRequiredTableFloat(
        lua_State state,
        int tableIndex,
        string field,
        out double value)
    {
        lua_getfield(state, tableIndex, field);
        var valid = TryReadRequiredFloat(state, -1, out value);
        lua_pop(state, 1);
        return valid;
    }

    private static Vector4 ReadColorTable(lua_State state, int index)
    {
        if (lua_istable(state, index) == 0)
            return Vector4.One;
        var absolute = AbsoluteIndex(state, index);
        return new Vector4(
            ReadTableNumber(state, absolute, "r", 1),
            ReadTableNumber(state, absolute, "g", 1),
            ReadTableNumber(state, absolute, "b", 1),
            ReadTableNumber(state, absolute, "a", 1));
    }

    private static bool TryReadRequiredColorTable(
        lua_State state,
        int index,
        out Vector4 color)
    {
        color = Vector4.One;
        if (index > lua_gettop(state) || lua_istable(state, index) == 0)
            return false;

        var absolute = AbsoluteIndex(state, index);
        if (!TryReadNormalizedTableNumber(state, absolute, "r", out var red) ||
            !TryReadNormalizedTableNumber(state, absolute, "g", out var green) ||
            !TryReadNormalizedTableNumber(state, absolute, "b", out var blue) ||
            !TryReadNormalizedTableNumber(state, absolute, "a", out var alpha))
        {
            return false;
        }

        color = new Vector4(red, green, blue, alpha);
        return true;
    }

    private static bool TryReadNormalizedTableNumber(
        lua_State state,
        int tableIndex,
        string field,
        out float value)
    {
        lua_getfield(state, tableIndex, field);
        var valid = lua_isnumber(state, -1) != 0;
        value = valid ? QuantizeNormalizedByte(lua_tonumber(state, -1)) : 0;
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadRequiredModelLight(
        lua_State state,
        int index,
        out UiModelLightState light)
    {
        light = new UiModelLightState();
        if (index > lua_gettop(state) || lua_istable(state, index) == 0)
            return false;

        var absolute = AbsoluteIndex(state, index);
        lua_getfield(state, absolute, "omnidirectional");
        var omnidirectional = lua_toboolean(state, -1) != 0;
        lua_pop(state, 1);

        lua_getfield(state, absolute, "point");
        var hasPoint = TryReadRequiredVector3Table(state, -1, out var point);
        lua_pop(state, 1);

        lua_getfield(state, absolute, "ambientIntensity");
        var ambientIntensityValue = 0d;
        var hasAmbientIntensity = lua_isnil(state, -1) != 0 ||
                                  TryReadRequiredFloat(
                                      state,
                                      -1,
                                      out ambientIntensityValue);
        var ambientIntensity = lua_isnil(state, -1) != 0
            ? 0
            : (float)ambientIntensityValue;
        lua_pop(state, 1);

        lua_getfield(state, absolute, "ambientColor");
        var hasAmbientColor = TryReadOptionalModelLightColor(
            state,
            -1,
            out var ambientColor);
        lua_pop(state, 1);

        lua_getfield(state, absolute, "diffuseIntensity");
        var diffuseIntensityValue = 0d;
        var hasDiffuseIntensity = lua_isnil(state, -1) != 0 ||
                                  TryReadRequiredFloat(
                                      state,
                                      -1,
                                      out diffuseIntensityValue);
        var diffuseIntensity = lua_isnil(state, -1) != 0
            ? 0
            : (float)diffuseIntensityValue;
        lua_pop(state, 1);

        lua_getfield(state, absolute, "diffuseColor");
        var hasDiffuseColor = TryReadOptionalModelLightColor(
            state,
            -1,
            out var diffuseColor);
        lua_pop(state, 1);

        if (!hasPoint ||
            !hasAmbientIntensity ||
            !hasAmbientColor ||
            !hasDiffuseIntensity ||
            !hasDiffuseColor)
        {
            return false;
        }

        light = new UiModelLightState
        {
            Omnidirectional = omnidirectional,
            Point = point,
            AmbientIntensity = ambientIntensity,
            AmbientColor = ambientColor,
            DiffuseIntensity = diffuseIntensity,
            DiffuseColor = diffuseColor
        };
        return true;
    }

    private static bool TryReadOptionalModelLightColor(
        lua_State state,
        int index,
        out Vector3? color)
    {
        color = null;
        if (lua_isnil(state, index) != 0)
            return true;
        if (lua_istable(state, index) == 0)
            return false;

        var absolute = AbsoluteIndex(state, index);
        if (!TryReadNormalizedTableNumber(state, absolute, "r", out var red) ||
            !TryReadNormalizedTableNumber(state, absolute, "g", out var green) ||
            !TryReadNormalizedTableNumber(state, absolute, "b", out var blue))
        {
            return false;
        }

        color = new Vector3(red, green, blue);
        return true;
    }

    private static Vector3 ReadVector3Table(lua_State state, int index)
    {
        if (lua_istable(state, index) == 0)
            return Vector3.Zero;
        var absolute = AbsoluteIndex(state, index);
        return new Vector3(
            ReadTableNumber(state, absolute, "x", 0),
            ReadTableNumber(state, absolute, "y", 0),
            ReadTableNumber(state, absolute, "z", 0));
    }

    private static bool TryReadRequiredVector2Table(
        lua_State state,
        int index,
        out Vector2 value)
    {
        value = default;
        if (index > lua_gettop(state) || lua_istable(state, index) == 0)
            return false;

        var absolute = AbsoluteIndex(state, index);
        lua_getfield(state, absolute, "x");
        var hasX = TryReadRequiredFloat(state, -1, out var x);
        lua_pop(state, 1);
        lua_getfield(state, absolute, "y");
        var hasY = TryReadRequiredFloat(state, -1, out var y);
        lua_pop(state, 1);
        if (!hasX || !hasY)
            return false;

        value = new Vector2((float)x, (float)y);
        return true;
    }

    private static void PushModelLight(lua_State state, UiModelLightState light)
    {
        lua_newtable(state);
        lua_pushboolean(state, light.Omnidirectional ? 1 : 0);
        lua_setfield(state, -2, "omnidirectional");
        PushVector3Table(state, light.Point);
        lua_setfield(state, -2, "point");
        SetTableNumber(state, "ambientIntensity", light.AmbientIntensity);
        if (light.AmbientColor is { } ambientColor)
        {
            PushColorTable(state, ambientColor);
            lua_setfield(state, -2, "ambientColor");
        }
        SetTableNumber(state, "diffuseIntensity", light.DiffuseIntensity);
        if (light.DiffuseColor is { } diffuseColor)
        {
            PushColorTable(state, diffuseColor);
            lua_setfield(state, -2, "diffuseColor");
        }
    }

    private static void PushVector3Table(lua_State state, Vector3 value)
    {
        lua_newtable(state);
        SetTableNumber(state, "x", value.X);
        SetTableNumber(state, "y", value.Y);
        SetTableNumber(state, "z", value.Z);
    }

    private static void PushVector3Mixin(
        lua_State state,
        Vector3 value,
        string mixinName)
    {
        PushVector3Table(state, value);
        var tableIndex = AbsoluteIndex(state, -1);
        lua_getglobal(state, "Mixin");
        if (lua_isfunction(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        lua_pushvalue(state, tableIndex);
        lua_getglobal(state, mixinName);
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 3);
            return;
        }

        if (lua_pcall(state, 2, 1, 0) == 0)
        {
            lua_remove(state, tableIndex);
            return;
        }
        lua_pop(state, 1);
    }

    private static void PushVector2Table(lua_State state, Vector2 value)
    {
        lua_newtable(state);
        SetTableNumber(state, "x", value.X);
        SetTableNumber(state, "y", value.Y);
    }

    private static void PushColorTable(lua_State state, Vector3 value)
    {
        lua_newtable(state);
        SetTableNumber(state, "r", value.X);
        SetTableNumber(state, "g", value.Y);
        SetTableNumber(state, "b", value.Z);
    }

    private static float ReadTableNumber(lua_State state, int index, string field, float fallback)
    {
        lua_getfield(state, index, field);
        var result = lua_isnumber(state, -1) != 0 ? (float)lua_tonumber(state, -1) : fallback;
        lua_pop(state, 1);
        return result;
    }

    private static void SetTableNumber(lua_State state, string field, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetTableString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetTableBoolean(lua_State state, string field, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static int RegisterDragButtons(lua_State state, HashSet<string> destination)
    {
        const string usage = "Usage: self:RegisterForDrag(buttons)";
        var registrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 2; index <= lua_gettop(state); index++)
        {
            if (lua_type(state, index) != LUA_TSTRING ||
                UiObject.NormalizeMouseButtonName(lua_tostring(state, index)) is not
                { } registration)
            {
                return luaL_error(state, usage);
            }
            registrations.Add(registration);
        }
        destination.Clear();
        destination.UnionWith(registrations);
        return 0;
    }

    private static int RegisterMouseButtons(
        lua_State state,
        int start,
        HashSet<string> destination,
        string usage)
    {
        var registrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = start; index <= lua_gettop(state); index++)
        {
            if (lua_type(state, index) != LUA_TSTRING ||
                !TryCanonicalizeMouseButtonRegistration(
                    lua_tostring(state, index) ?? string.Empty,
                    out var registration))
            {
                return luaL_error(state, usage);
            }

            registrations.Add(registration);
        }

        destination.Clear();
        destination.UnionWith(registrations);
        return 0;
    }

    private static bool TryCanonicalizeMouseButtonRegistration(
        string value,
        out string registration)
    {
        registration = UiObject.NormalizeMouseButtonRegistration(value) ?? string.Empty;
        return registration.Length != 0;
    }

    private static int PushChildren(LuaRuntime runtime, UiObject value, bool regions)
    {
        var count = 0;
        foreach (var id in value.Children)
        {
            var child = runtime.Ui.Find(id);
            if (child is null ||
                (regions ? !child.IsRegion : !WowWidgetApi.IsFrameWidget(child.ObjectType)))
                continue;
            runtime.PushObject(child);
            count++;
        }
        return count;
    }

    private static int PushAnimations(LuaRuntime runtime, UiObject value)
    {
        var count = 0;
        foreach (var childId in value.Children)
        {
            var child = runtime.Ui.Find(childId);
            if (child?.Animation is null)
                continue;

            runtime.PushObject(child);
            count++;
        }
        return count;
    }

    private static double AnimationGroupDuration(LuaRuntime runtime, UiObject value) =>
        value.Children
            .Select(runtime.Ui.Find)
            .Where(child => child?.Animation is not null)
            .Cast<UiObject>()
            .GroupBy(child => child.Animation!.Order)
            .Sum(order => order.Max(child =>
                child.Animation!.StartDelay +
                child.Animation.Duration +
                child.Animation.EndDelay));

    private static void PlayAnimationGroup(
        LuaRuntime runtime,
        UiObject value,
        UiAnimationGroupState animationGroup,
        bool reverse,
        double offset)
    {
        runtime.PlayAnimationGroup(value, reverse, offset);
    }

    private static void StopAnimationGroup(
        LuaRuntime runtime,
        UiObject value,
        UiAnimationGroupState animationGroup)
    {
        runtime.StopAnimationGroup(value, true);
    }

    private static void PlayAnimation(
        LuaRuntime runtime,
        UiObject value,
        UiAnimationState animation)
    {
        if (animation.PlaybackState == 1)
            return;
        animation.ManuallyStopped = false;
        animation.PlaybackState = 1;
        runtime.InvokeScript(value, "OnPlay");
    }

    private static void StopAnimation(
        LuaRuntime runtime,
        UiObject value,
        UiAnimationState animation)
    {
        if (animation.PlaybackState == 0)
            return;
        animation.Elapsed = 0;
        animation.Progress = 0;
        animation.SmoothProgress = 0;
        animation.PlaybackState = 0;
        animation.ManuallyStopped = true;
        runtime.InvokeScript(value, "OnStop", true);
    }

    private static string? OptionalString(lua_State state, int index)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return lua_tostring(state, index);
    }

    private static bool TryReadRequiredString(
        lua_State state,
        int index,
        out string value)
    {
        value = string.Empty;
        if (!HasRequiredValue(state, index) ||
            lua_isstring(state, index) == 0 ||
            lua_istable(state, index) != 0)
        {
            return false;
        }
        value = lua_tostring(state, index) ?? string.Empty;
        return true;
    }

    private static bool TryReadOptionalString(
        lua_State state,
        int index,
        out string? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isstring(state, index) == 0 || lua_istable(state, index) != 0)
            return false;
        value = lua_tostring(state, index);
        return value is not null;
    }

    private static bool TryReadOptionalFramePoint(
        lua_State state,
        int index,
        out string value)
    {
        value = "BOTTOMRIGHT";
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_type(state, index) != LUA_TSTRING)
            return false;
        var requested = lua_tostring(state, index);
        if (requested is null || !FramePointNames.Contains(requested))
            return false;
        value = requested.ToUpperInvariant();
        return true;
    }

    private static bool TryReadRequiredFramePoint(
        lua_State state,
        int index,
        out string value)
    {
        value = string.Empty;
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TSTRING)
            return false;
        var requested = lua_tostring(state, index);
        if (requested is null || !FramePointNames.Contains(requested))
            return false;
        value = requested.ToUpperInvariant();
        return true;
    }

    private static bool MatchesObjectType(UiObject value, string requestedType)
    {
        if (value.ObjectType.Equals(requestedType, StringComparison.OrdinalIgnoreCase))
            return true;

        if (requestedType.Equals("FrameScript_Object", StringComparison.OrdinalIgnoreCase) ||
            requestedType.Equals("Object", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var isPlayerModel =
            value.ObjectType.Equals("PlayerModel", StringComparison.OrdinalIgnoreCase) ||
            value.ObjectType.Equals("CharacterModel", StringComparison.OrdinalIgnoreCase) ||
            value.ObjectType.Equals("TabardModel", StringComparison.OrdinalIgnoreCase) ||
            value.ObjectType.Equals("DressUpModel", StringComparison.OrdinalIgnoreCase) ||
            value.ObjectType.Equals("CinematicModel", StringComparison.OrdinalIgnoreCase);
        if (isPlayerModel &&
            (requestedType.Equals("PlayerModel", StringComparison.OrdinalIgnoreCase) ||
             requestedType.Equals("Model", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if ((value.ObjectType.Equals(
                 "ArchaeologyDigSiteFrame",
                 StringComparison.OrdinalIgnoreCase) ||
             value.ObjectType.Equals("QuestPOIFrame", StringComparison.OrdinalIgnoreCase) ||
             value.ObjectType.Equals(
                 "ScenarioPOIFrame",
                 StringComparison.OrdinalIgnoreCase)) &&
            requestedType.Equals("BlobFrame", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.ObjectType.Equals("ModelSceneActor", StringComparison.OrdinalIgnoreCase) ||
            value.ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value.Animation is not null)
        {
            if (requestedType.Equals("Animation", StringComparison.OrdinalIgnoreCase))
                return true;

            if (value.ObjectType.Equals("LineScale", StringComparison.OrdinalIgnoreCase) &&
                requestedType.Equals("Scale", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (value.ObjectType.Equals(
                    "LineTranslation",
                    StringComparison.OrdinalIgnoreCase) &&
                requestedType.Equals("Translation", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (value.IsRegion)
        {
            if (requestedType.Equals("Region", StringComparison.OrdinalIgnoreCase) ||
                requestedType.Equals("SimpleRegion", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return (value.ObjectType.Equals("MaskTexture", StringComparison.OrdinalIgnoreCase) ||
                    value.ObjectType.Equals("Line", StringComparison.OrdinalIgnoreCase)) &&
                   requestedType.Equals("Texture", StringComparison.OrdinalIgnoreCase);
        }

        if (!WowWidgetApi.IsFrameWidget(value.ObjectType))
            return false;
        if (requestedType.Equals("Frame", StringComparison.OrdinalIgnoreCase) ||
            requestedType.Equals("Region", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return value.ObjectType.Equals("CheckButton", StringComparison.OrdinalIgnoreCase) &&
               requestedType.Equals("Button", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SupportsScript(UiObject value, string scriptName)
    {
        var objectType = value.ObjectType;
        if (objectType.Equals("AnimationGroup", StringComparison.OrdinalIgnoreCase))
            return AnimationGroupScriptNames.Contains(scriptName);
        if (value.Animation is not null)
            return AnimationScriptNames.Contains(scriptName);
        if (objectType.Equals("ModelSceneActor", StringComparison.OrdinalIgnoreCase))
            return ModelSceneActorScriptNames.Contains(scriptName);
        if (objectType.Equals("Font", StringComparison.OrdinalIgnoreCase))
            return scriptName.Equals("OnLoad", StringComparison.OrdinalIgnoreCase);

        var isFrame = WowWidgetApi.IsFrameWidget(objectType);
        if (!isFrame && !value.IsRegion)
            return false;
        if (RegionScriptNames.Contains(scriptName) ||
            isFrame && FrameScriptNames.Contains(scriptName))
        {
            return true;
        }

        if (objectType.Equals("Button", StringComparison.OrdinalIgnoreCase) ||
            objectType.Equals("CheckButton", StringComparison.OrdinalIgnoreCase) ||
            objectType.EndsWith("Button", StringComparison.OrdinalIgnoreCase))
        {
            return ButtonScriptNames.Contains(scriptName);
        }
        if (objectType.EndsWith("EditBox", StringComparison.OrdinalIgnoreCase))
            return EditBoxScriptNames.Contains(scriptName);
        if (objectType.Equals("ScrollFrame", StringComparison.OrdinalIgnoreCase) ||
            objectType.Equals("EventScrollFrame", StringComparison.OrdinalIgnoreCase))
        {
            return scriptName.Equals("OnScrollRangeChanged", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnVerticalScroll", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnHorizontalScroll", StringComparison.OrdinalIgnoreCase);
        }
        if (objectType.Equals("Slider", StringComparison.OrdinalIgnoreCase) ||
            objectType.Equals("StatusBar", StringComparison.OrdinalIgnoreCase))
        {
            return scriptName.Equals("OnValueChanged", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnMinMaxChanged", StringComparison.OrdinalIgnoreCase);
        }
        if (objectType.Equals("GameTooltip", StringComparison.OrdinalIgnoreCase))
        {
            return scriptName.Equals("OnTooltipSetDefaultAnchor", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnTooltipCleared", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnTooltipSetFrameStack", StringComparison.OrdinalIgnoreCase);
        }
        if (objectType.Equals("Browser", StringComparison.OrdinalIgnoreCase))
        {
            return scriptName.Equals("OnEscapePressed", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnEditFocusLost", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnEditFocusGained", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnExternalLink", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnButtonUpdate", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnError", StringComparison.OrdinalIgnoreCase) ||
                   scriptName.Equals("OnRequestNewSize", StringComparison.OrdinalIgnoreCase);
        }
        if (objectType.Equals("MovieFrame", StringComparison.OrdinalIgnoreCase))
            return scriptName.Equals("OnMovieFinished", StringComparison.OrdinalIgnoreCase);
        if (objectType.Equals("Cooldown", StringComparison.OrdinalIgnoreCase))
            return scriptName.Equals("OnCooldownDone", StringComparison.OrdinalIgnoreCase);
        if (objectType.Equals("ColorSelect", StringComparison.OrdinalIgnoreCase))
            return scriptName.Equals("OnColorSelect", StringComparison.OrdinalIgnoreCase);
        if (objectType.Equals("FogOfWarFrame", StringComparison.OrdinalIgnoreCase) ||
            objectType.Equals("Minimap", StringComparison.OrdinalIgnoreCase) ||
            objectType.Equals("SimpleMinimap", StringComparison.OrdinalIgnoreCase))
        {
            return scriptName.Equals("OnUiMapChanged", StringComparison.OrdinalIgnoreCase);
        }
        if (objectType.Contains("Model", StringComparison.OrdinalIgnoreCase))
        {
            if (scriptName.Equals("OnModelLoaded", StringComparison.OrdinalIgnoreCase) ||
                scriptName.Equals("OnAnimStarted", StringComparison.OrdinalIgnoreCase) ||
                scriptName.Equals("OnAnimFinished", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (objectType.Equals("DressUpModel", StringComparison.OrdinalIgnoreCase) ||
                objectType.Equals("ModelScene", StringComparison.OrdinalIgnoreCase))
            {
                return scriptName.Equals("OnDressModel", StringComparison.OrdinalIgnoreCase);
            }
            if (objectType.Equals("CinematicModel", StringComparison.OrdinalIgnoreCase))
                return scriptName.Equals("OnPanFinished", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    internal static bool IsRecognizedUnitToken(string value)
    {
        var token = value.ToLowerInvariant();

        if (token is
            "player" or
            "pet" or
            "vehicle" or
            "target" or
            "targettarget" or
            "focus" or
            "focustarget" or
            "mouseover" or
            "npc" or
            "questnpc" or
            "softenemy" or
            "softfriend" or
            "softinteract" or
            "anyenemy" or
            "anyfriend" or
            "anyinteract" or
            "anytarget" or
            "none")
        {
            return true;
        }

        return HasIndexedUnitToken(token, "party", 4) ||
               HasIndexedUnitToken(token, "partypet", 4) ||
               HasIndexedUnitToken(token, "raid", 40) ||
               HasIndexedUnitToken(token, "raidpet", 40) ||
               HasIndexedUnitToken(token, "boss", 5) ||
               HasIndexedUnitToken(token, "arena", 5) ||
               HasIndexedUnitToken(token, "arenapet", 5) ||
               HasIndexedUnitToken(token, "nameplate", 150) ||
               HasIndexedUnitToken(token, "commentator", 30) ||
               HasIndexedUnitToken(token, "spectateda", 15) ||
               HasIndexedUnitToken(token, "spectatedb", 15) ||
               HasIndexedUnitToken(token, "spectatedpeta", 15) ||
               HasIndexedUnitToken(token, "spectatedpetb", 15);
    }

    private static bool HasIndexedUnitToken(
        string token,
        string prefix,
        int maximumIndex)
    {
        if (!token.StartsWith(prefix, StringComparison.Ordinal) ||
            token.Length == prefix.Length ||
            !int.TryParse(
                token.AsSpan(prefix.Length),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var index))
        {
            return false;
        }
        return index is >= 1 && index <= maximumIndex;
    }

    private static double OptionalNumber(lua_State state, int index, double fallback = 0)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return fallback;
        return lua_tonumber(state, index);
    }

    private static bool TryReadRequiredCooldownTimeMilliseconds(
        lua_State state,
        int index,
        out int milliseconds)
    {
        milliseconds = 0;
        if (index > lua_gettop(state) ||
            lua_isnumber(state, index) == 0)
        {
            return false;
        }

        var seconds = lua_tonumber(state, index);
        if (!double.IsFinite(seconds))
            return false;

        milliseconds = ConvertDurationSecondsToMilliseconds(seconds);
        return true;
    }

    private enum CooldownTexturePart
    {
        Swipe,
        Edge,
        Bling
    }

    private static int SetCooldownTexture(
        lua_State state,
        UiCooldownState cooldown,
        CooldownTexturePart part)
    {
        var usage = part switch
        {
            CooldownTexturePart.Swipe =>
                "Usage: self:SetSwipeTexture(texture [, color])",
            CooldownTexturePart.Edge =>
                "Usage: self:SetEdgeTexture(texture [, color])",
            _ =>
                "Usage: self:SetBlingTexture(texture [, color])"
        };
        if (!TryReadRequiredTextureAsset(
                state,
                2,
                out var asset,
                out var fileDataId))
        {
            return luaL_error(state, usage);
        }

        Vector4? color = null;
        if (HasRequiredValue(state, 3))
        {
            if (!TryReadRequiredNormalizedColor(state, 3, out var parsedColor))
                return luaL_error(state, usage);
            color = parsedColor;
        }

        switch (part)
        {
            case CooldownTexturePart.Swipe:
                cooldown.SwipeTextureAsset = asset;
                cooldown.SwipeTextureFileDataId = fileDataId;
                if (color is { } swipeColor)
                    cooldown.SwipeColor = swipeColor;
                break;
            case CooldownTexturePart.Edge:
                cooldown.EdgeTextureAsset = asset;
                cooldown.EdgeTextureFileDataId = fileDataId;
                if (color is { } edgeColor)
                    cooldown.EdgeColor = edgeColor;
                break;
            case CooldownTexturePart.Bling:
                cooldown.BlingTextureAsset = asset;
                cooldown.BlingTextureFileDataId = fileDataId;
                if (color is { } blingColor)
                    cooldown.BlingColor = blingColor;
                break;
        }

        return 0;
    }

    internal static UiObject EnsureCooldownFontString(
        LuaRuntime runtime,
        UiObject owner,
        UiCooldownState cooldown)
    {
        if (cooldown.CountdownFontStringId is { } fontStringId &&
            runtime.Ui.Find(fontStringId) is { } existing)
        {
            return existing;
        }

        var fontString = CreateObject(
            runtime,
            "FontString",
            null,
            owner,
            "OVERLAY");
        fontString.AllPointsTargetId = owner.Id;
        fontString.MouseEnabled = false;
        fontString.Shown = false;
        cooldown.CountdownFontStringId = fontString.Id;
        return fontString;
    }

    internal static void ApplyCooldownFont(
        LuaRuntime runtime,
        UiCooldownState cooldown)
    {
        if (cooldown.CountdownFontStringId is not { } fontStringId ||
            runtime.Ui.Find(fontStringId) is not { } fontString)
        {
            return;
        }

        var source = string.IsNullOrEmpty(cooldown.CountdownFontName)
            ? null
            : runtime.Ui.Find(cooldown.CountdownFontName);
        AssignFontObject(runtime, fontString, source);
    }

    internal static void HideCooldownFontString(
        LuaRuntime runtime,
        UiCooldownState cooldown)
    {
        if (cooldown.CountdownFontStringId is not { } fontStringId ||
            runtime.Ui.Find(fontStringId) is not { } fontString)
        {
            return;
        }

        fontString.Shown = false;
        if (fontString.Font is { } font)
            font.Text = string.Empty;
    }

    private static void PauseCooldown(
        LuaRuntime runtime,
        UiCooldownState cooldown)
    {
        cooldown.PausedElapsedMilliseconds = unchecked(
            CooldownClockMilliseconds(runtime, cooldown.UsesUnixClock) -
            cooldown.StartTimeMilliseconds);
        cooldown.Paused = true;
    }

    internal static int CooldownClockMilliseconds(
        LuaRuntime runtime,
        bool useUnixClock)
    {
        if (useUnixClock)
        {
            return unchecked(
                (int)runtime.DateAndTime.CurrentTime.ToUnixTimeMilliseconds());
        }
        return unchecked((int)(runtime.Time * 1000));
    }

    internal static void ClearCooldownState(UiCooldownState cooldown)
    {
        cooldown.StartTimeMilliseconds = 0;
        cooldown.DisplayDurationMilliseconds = 0;
        cooldown.ModRate = 1;
        cooldown.ZeroDurationDisplay = false;
        cooldown.UsesUnixClock = false;
        cooldown.ElapsedDisplayMilliseconds = 0;
        cooldown.CompletionBlingActive = false;
    }

    private static void SetNativeCooldown(
        LuaRuntime runtime,
        UiCooldownState cooldown,
        int startMilliseconds,
        int durationMilliseconds,
        float requestedModRate,
        bool useUnixClock)
    {
        if (cooldown.DisplayDurationMilliseconds != 0 &&
            durationMilliseconds == 0)
        {
            return;
        }

        if (startMilliseconds == 0)
        {
            if (cooldown.ZeroDurationDisplay)
            {
                cooldown.ZeroDurationDisplay = false;
                cooldown.StartTimeMilliseconds = 0;
            }
            return;
        }

        var modRate =
            MathF.Abs(requestedModRate) < 2.3841858e-7f ||
            requestedModRate < 0
                ? 1f
                : requestedModRate;
        cooldown.StartTimeMilliseconds = startMilliseconds;
        if (cooldown.Paused)
        {
            cooldown.PausedElapsedMilliseconds = unchecked(
                CooldownClockMilliseconds(runtime, useUnixClock) -
                startMilliseconds);
        }
        cooldown.DisplayDurationMilliseconds = NativeCooldownRoundToInt(
            (float)durationMilliseconds / modRate + 0.5f);
        cooldown.ModRate = modRate;
        cooldown.ZeroDurationDisplay = durationMilliseconds == 0;
        cooldown.UsesUnixClock = useUnixClock;
        cooldown.ElapsedDisplayMilliseconds = 0;
        cooldown.CompletionBlingActive = false;

        if (cooldown.StartTimeMilliseconds == 0 &&
            cooldown.DisplayDurationMilliseconds == 0)
        {
            ClearCooldownState(cooldown);
        }
    }

    internal static int CooldownElapsedDisplayMilliseconds(
        int currentClockMilliseconds,
        UiCooldownState cooldown)
    {
        var current = unchecked((uint)currentClockMilliseconds);
        var start = unchecked((uint)cooldown.StartTimeMilliseconds);
        var elapsed = start != 0 && current > start ? current - start : 0;
        return NativeCooldownRoundToInt(elapsed / cooldown.ModRate + 0.5f);
    }

    private static int NativeCooldownReportedDuration(UiCooldownState cooldown) =>
        NativeCooldownRoundToInt(
            (float)cooldown.DisplayDurationMilliseconds *
            cooldown.ModRate +
            0.5f);

    private static int NativeCooldownRoundToInt(float value)
    {
        if (!float.IsFinite(value) ||
            value < int.MinValue ||
            value >= 2_147_483_648f)
        {
            return int.MinValue;
        }
        return (int)value;
    }

    private static int ConvertAnimationTimeOffsetToMilliseconds(
        double timeOffsetSeconds)
    {
        var milliseconds = (float)timeOffsetSeconds * 1000f;
        if (!float.IsFinite(milliseconds) ||
            milliseconds < long.MinValue ||
            milliseconds > long.MaxValue)
        {
            return 0;
        }
        return unchecked((int)(long)milliseconds);
    }

    private static int ConvertDurationSecondsToMilliseconds(double durationSeconds)
    {
        var milliseconds = durationSeconds * 1000.0;
        if (!double.IsFinite(milliseconds) ||
            milliseconds < long.MinValue ||
            milliseconds > long.MaxValue)
        {
            return 0;
        }
        return unchecked((int)(long)milliseconds);
    }

    private static bool TryReadOptionalFloat(
        lua_State state,
        int index,
        out double value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        value = lua_tonumber(state, index);
        return !double.IsNaN(value) && value is >= -float.MaxValue and <= float.MaxValue;
    }

    private static bool TryReadOptionalFloat(
        lua_State state,
        int index,
        double fallback,
        out double value)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
        {
            value = fallback;
            return true;
        }
        return TryReadOptionalFloat(state, index, out value);
    }

    private static bool TryReadRequiredFloat(
        lua_State state,
        int index,
        out double value)
    {
        value = 0;
        return index <= lua_gettop(state) &&
               lua_isnil(state, index) == 0 &&
               TryReadOptionalFloat(state, index, out value);
    }

    private static bool HasRequiredValue(lua_State state, int index) =>
        index <= lua_gettop(state) && lua_isnil(state, index) == 0;

    private static bool TryReadRequiredBoolean(
        lua_State state,
        int index,
        out bool value)
    {
        value = false;
        if (index > lua_gettop(state))
            return false;
        value = lua_toboolean(state, index) != 0;
        return true;
    }

    private static int Utf16PositionFromUtf8ByteOffset(string text, int byteOffset)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var position = Math.Clamp(byteOffset, 0, bytes.Length);
        while (position < bytes.Length && (bytes[position] & 0xC0) == 0x80)
            position++;
        return Encoding.UTF8.GetCharCount(bytes.AsSpan(0, position));
    }

    private static int Utf8CharacterCount(ReadOnlySpan<char> text)
    {
        var count = 0;
        foreach (var _ in text.EnumerateRunes())
            count++;
        return count;
    }

    private static bool OptionalBoolean(lua_State state, int index, bool fallback)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return fallback;
        return lua_toboolean(state, index) != 0;
    }

    private static object? ReadPrimitive(lua_State state, int index) =>
        index > lua_gettop(state) ? null : lua_type(state, index) switch
        {
            LUA_TBOOLEAN => lua_toboolean(state, index) != 0,
            LUA_TNUMBER => lua_tonumber(state, index),
            LUA_TSTRING => lua_tostring(state, index),
            _ => null
        };

    private static bool PushAttributeValue(
        LuaRuntime runtime,
        UiObject value,
        string name)
    {
        var state = runtime.State;
        if (value.AttributeReferences.TryGetValue(name, out var reference))
        {
            lua_rawgeti(state, LUA_REGISTRYINDEX, reference);
            return true;
        }

        if (!value.Attributes.TryGetValue(name, out var attribute) ||
            attribute is null)
        {
            return false;
        }

        PushPrimitive(state, attribute);
        return true;
    }

    private static void PushPrimitive(lua_State state, object? value)
    {
        switch (value)
        {
            case bool boolean:
                lua_pushboolean(state, boolean ? 1 : 0);
                break;
            case double number:
                lua_pushnumber(state, number);
                break;
            case string text:
                lua_pushstring(state, text);
                break;
            default:
                lua_pushnil(state);
                break;
        }
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX ? index : lua_gettop(state) + index + 1;

    internal static void RegisterClosureGlobal(lua_State state, string name, lua_CFunction callback)
    {
        lua_pushstring(state, name);
        lua_pushcclosure(state, callback, 1);
        lua_setglobal(state, name);
    }

    internal static LuaRuntime GetRuntime(lua_State state) =>
        TryGetRuntime(state, out var runtime)
            ? runtime!
            : throw new InvalidOperationException("Lua state is not attached to an emulator runtime.");

    internal static bool TryGetRuntime(
        lua_State state,
        out LuaRuntime? runtime)
    {
        if (Runtimes.TryGetValue(state, out runtime))
            return true;

        lua_getfield(state, LUA_REGISTRYINDEX, RuntimeRegistryKey);
        var id = lua_isnumber(state, -1) != 0 ? (long)lua_tonumber(state, -1) : 0;
        lua_pop(state, 1);
        if (id <= 0 || !RuntimesById.TryGetValue(id, out var resolved))
            return false;

        runtime = resolved;
        Runtimes[state] = resolved;
        return true;
    }
}
