using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowBarberShopApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "ApplyCustomizationChoices", "Cancel", "ClearPreviewChoices",
        "GetAvailableCustomizations", "GetCurrentCameraZoom", "GetCurrentCharacterData",
        "GetCurrentCost", "GetViewingChrModel", "HasAlteredForm", "HasAnyChanges",
        "HasCustomizationFeature", "IsViewingAlteredForm", "MarkCustomizationChoiceAsSeen",
        "MarkCustomizationOptionAsSeen", "PreviewCustomizationChoice",
        "RandomizeCustomizationChoices", "ResetCameraRotation", "ResetCustomizationChoices",
        "RotateCamera", "SaveSeenChoices", "SetCameraDistanceOffset", "SetCameraZoomLevel",
        "SetCustomizationChoice", "SetModelDressState", "SetSelectedSex",
        "SetViewingAlteredForm", "SetViewingChrModel", "SetViewingShapeshiftForm", "ZoomCamera"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_BarberShop");
    }

    private static int Dispatch(lua_State state)
    {
        var barberShop = LuaBindings.GetRuntime(state).BarberShop;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ApplyCustomizationChoices":
                barberShop.ApplyCustomizationChoicesRequests++;
                lua_pushboolean(
                    state,
                    barberShop.ApplyCustomizationChoicesResult ? 1 : 0);
                if (barberShop.ApplyCustomizationChoicesResult)
                    barberShop.HasAnyChanges = false;
                return 1;
            case "Cancel":
                barberShop.CancelRequests++;
                return 0;
            case "ClearPreviewChoices":
            {
                var clearSavedChoices = OptionalBoolean(
                    state,
                    1,
                    false,
                    "Usage: C_BarberShop.ClearPreviewChoices([clearSavedChoices])");
                barberShop.ClearPreviewChoicesRequests++;
                barberShop.LastClearSavedChoices = clearSavedChoices;
                barberShop.PreviewChoices.Clear();
                if (clearSavedChoices)
                    barberShop.SavedPreviewChoices.Clear();
                return 0;
            }
            case "GetAvailableCustomizations":
                if (barberShop.AvailableCustomizations is null)
                    return 0;
                PushCustomizationCategories(
                    state,
                    barberShop.AvailableCustomizations);
                return 1;
            case "GetCurrentCameraZoom":
                lua_pushnumber(state, barberShop.CurrentCameraZoom);
                return 1;
            case "GetCurrentCharacterData":
                if (barberShop.CurrentCharacterData is null)
                    return 0;
                PushCharacterData(state, barberShop.CurrentCharacterData);
                return 1;
            case "GetCurrentCost":
                lua_pushnumber(state, barberShop.CurrentCost);
                return 1;
            case "GetViewingChrModel":
                PushOptionalInteger(state, barberShop.ViewingChrModelId);
                return 1;
            case "HasAlteredForm":
                lua_pushboolean(state, barberShop.HasAlteredForm ? 1 : 0);
                return 1;
            case "HasAnyChanges":
                lua_pushboolean(state, barberShop.HasAnyChanges ? 1 : 0);
                return 1;
            case "HasCustomizationFeature":
            {
                var featureMask = RequiredCustomizationFeatureMask(
                    state,
                    1,
                    "Usage: local hasCustomizationFeature = C_BarberShop.HasCustomizationFeature(featureMask)");
                lua_pushboolean(
                    state,
                    (barberShop.CustomizationFeatureMask & featureMask) != 0
                        ? 1
                        : 0);
                return 1;
            }
            case "IsViewingAlteredForm":
                lua_pushboolean(
                    state,
                    barberShop.IsOpen && barberShop.IsViewingAlteredForm ? 1 : 0);
                return 1;
            case "MarkCustomizationChoiceAsSeen":
            {
                var choiceId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_BarberShop.MarkCustomizationChoiceAsSeen(choiceID)");
                barberShop.SeenChoiceIds.Add(choiceId);
                barberShop.LastSeenChoiceId = choiceId;
                return 0;
            }
            case "MarkCustomizationOptionAsSeen":
            {
                var optionId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_BarberShop.MarkCustomizationOptionAsSeen(optionID)");
                barberShop.SeenOptionIds.Add(optionId);
                barberShop.LastSeenOptionId = optionId;
                return 0;
            }
            case "PreviewCustomizationChoice":
            {
                const string usage =
                    "Usage: C_BarberShop.PreviewCustomizationChoice(optionID, choiceID)";
                var optionId = RequiredInt32(state, 1, usage);
                var choiceId = RequiredInt32(state, 2, usage);
                barberShop.PreviewCustomizationChoiceRequests++;
                barberShop.LastPreviewCustomizationChoice =
                    new WowBarberShopOptionChoice(optionId, choiceId);
                if (barberShop.IsOpen)
                    barberShop.PreviewChoices[optionId] = choiceId;
                return 0;
            }
            case "RandomizeCustomizationChoices":
                barberShop.RandomizeCustomizationChoicesRequests++;
                if (barberShop.IsOpen)
                    barberShop.HasAnyChanges = true;
                return 0;
            case "ResetCameraRotation":
                barberShop.ResetCameraRotationRequests++;
                if (barberShop.IsOpen)
                {
                    barberShop.CameraRotationDegrees =
                        barberShop.InitialCameraRotationDegrees;
                }
                return 0;
            case "ResetCustomizationChoices":
                barberShop.ResetCustomizationChoicesRequests++;
                if (barberShop.IsOpen)
                {
                    barberShop.SelectedChoices.Clear();
                    barberShop.HasAnyChanges = false;
                }
                return 0;
            case "RotateCamera":
            {
                var differenceDegrees = RequiredFiniteNumber(
                    state,
                    1,
                    "Usage: C_BarberShop.RotateCamera(diffDegrees)");
                barberShop.RotateCameraRequests++;
                barberShop.LastCameraRotationDifferenceDegrees =
                    differenceDegrees;
                if (barberShop.IsOpen)
                    barberShop.CameraRotationDegrees += differenceDegrees;
                return 0;
            }
            case "SaveSeenChoices":
                barberShop.SaveSeenChoicesRequests++;
                return 0;
            case "SetCameraDistanceOffset":
            {
                var offset = RequiredFiniteNumber(
                    state,
                    1,
                    "Usage: C_BarberShop.SetCameraDistanceOffset(offset)");
                barberShop.SetCameraDistanceOffsetRequests++;
                if (barberShop.IsOpen)
                    barberShop.CameraDistanceOffset = (float)offset;
                return 0;
            }
            case "SetCameraZoomLevel":
            {
                const string usage =
                    "Usage: C_BarberShop.SetCameraZoomLevel(zoomLevel [, keepCustomZoom])";
                var zoomLevel = RequiredInt32(state, 1, usage);
                var keepCustomZoom = OptionalBoolean(state, 2, false, usage);
                barberShop.SetCameraZoomLevelRequests++;
                barberShop.LastKeepCustomZoom = keepCustomZoom;
                if (barberShop.IsOpen &&
                    (!barberShop.HasCustomCameraZoom || !keepCustomZoom))
                {
                    barberShop.CurrentCameraZoom = Math.Clamp(zoomLevel, 0, 100);
                    barberShop.HasCustomCameraZoom = false;
                }
                return 0;
            }
            case "SetCustomizationChoice":
            {
                const string usage =
                    "Usage: C_BarberShop.SetCustomizationChoice(optionID, choiceID)";
                var optionId = RequiredInt32(state, 1, usage);
                var choiceId = RequiredInt32(state, 2, usage);
                barberShop.SetCustomizationChoiceRequests++;
                barberShop.LastCustomizationChoice =
                    new WowBarberShopOptionChoice(optionId, choiceId);
                if (barberShop.IsOpen)
                {
                    barberShop.SelectedChoices[optionId] = choiceId;
                    barberShop.HasAnyChanges = true;
                }
                return 0;
            }
            case "SetModelDressState":
            {
                barberShop.SetModelDressStateRequests++;
                var dressed = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_BarberShop.SetModelDressState(dressedState)");
                if (barberShop.IsOpen)
                    barberShop.ModelDressed = dressed;
                return 0;
            }
            case "SetSelectedSex":
            {
                barberShop.SetSelectedSexRequests++;
                var sex = RequiredSex(
                    state,
                    1,
                    "Usage: C_BarberShop.SetSelectedSex(sex)");
                if (barberShop.IsOpen)
                {
                    barberShop.SelectedSex = sex;
                    barberShop.HasAnyChanges = true;
                }
                return 0;
            }
            case "SetViewingAlteredForm":
            {
                barberShop.SetViewingAlteredFormRequests++;
                var isViewingAlteredForm = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_BarberShop.SetViewingAlteredForm(isViewingAlteredForm)");
                if (barberShop.IsOpen)
                    barberShop.IsViewingAlteredForm = isViewingAlteredForm;
                return 0;
            }
            case "SetViewingChrModel":
            {
                const string usage =
                    "Usage: C_BarberShop.SetViewingChrModel([chrModelID, spellShapeshiftFormID])";
                var chrModelId = OptionalInt32(state, 1, usage);
                var spellShapeshiftFormId = OptionalInt32(state, 2, usage);
                barberShop.SetViewingChrModelRequests++;
                if (barberShop.IsOpen)
                {
                    barberShop.ViewingChrModelId = chrModelId;
                    barberShop.ViewingSpellShapeshiftFormId =
                        spellShapeshiftFormId;
                }
                return 0;
            }
            case "SetViewingShapeshiftForm":
            {
                var shapeshiftFormId = OptionalInt32(
                    state,
                    1,
                    "Usage: C_BarberShop.SetViewingShapeshiftForm([shapeshiftFormID])");
                barberShop.SetViewingShapeshiftFormRequests++;
                if (barberShop.IsOpen)
                {
                    barberShop.ViewingShapeshiftFormId =
                        shapeshiftFormId ?? 0;
                }
                return 0;
            }
            case "ZoomCamera":
            {
                var zoomAmount = RequiredInt32(
                    state,
                    1,
                    "Usage: C_BarberShop.ZoomCamera(zoomAmount)");
                barberShop.ZoomCameraRequests++;
                barberShop.LastCameraZoomAmount = zoomAmount;
                if (barberShop.IsOpen)
                {
                    barberShop.CurrentCameraZoom =
                        Math.Clamp(
                            barberShop.CurrentCameraZoom + zoomAmount,
                            0,
                            100);
                    barberShop.HasCustomCameraZoom = true;
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static void PushCustomizationCategories(
        lua_State state,
        IReadOnlyList<WowBarberShopCustomizationCategory> categories)
    {
        lua_createtable(state, categories.Count, 0);
        for (var index = 0; index < categories.Count; index++)
        {
            PushCustomizationCategory(state, categories[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCustomizationCategory(
        lua_State state,
        WowBarberShopCustomizationCategory category)
    {
        lua_createtable(state, 0, 14);
        SetInteger(state, "id", category.Id);
        SetInteger(state, "orderIndex", category.OrderIndex);
        SetOptionalString(state, "name", category.Name);
        SetString(state, "icon", category.Icon);
        SetString(state, "selectedIcon", category.SelectedIcon);
        SetBoolean(state, "undressModel", category.UndressModel);
        SetBoolean(state, "subcategory", category.Subcategory);
        SetInteger(state, "cameraZoomLevel", category.CameraZoomLevel);
        SetNumber(
            state,
            "cameraDistanceOffset",
            category.CameraDistanceOffset);
        SetOptionalInteger(
            state,
            "spellShapeshiftFormID",
            category.SpellShapeshiftFormId);
        SetOptionalInteger(state, "chrModelID", category.ChrModelId);
        PushCustomizationOptions(state, category.Options);
        lua_setfield(state, -2, "options");
        SetBoolean(state, "hasNewChoices", category.HasNewChoices);
        SetBoolean(
            state,
            "needsNativeFormCategory",
            category.NeedsNativeFormCategory);
    }

    private static void PushCustomizationOptions(
        lua_State state,
        IReadOnlyList<WowBarberShopCustomizationOption> options)
    {
        lua_createtable(state, options.Count, 0);
        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            lua_createtable(state, 0, 8);
            SetInteger(state, "id", option.Id);
            SetOptionalString(state, "name", option.Name);
            SetInteger(state, "orderIndex", option.OrderIndex);
            SetUnsignedInteger(state, "optionType", option.OptionType);
            PushCustomizationChoices(state, option.Choices);
            lua_setfield(state, -2, "choices");
            SetOptionalInteger(
                state,
                "currentChoiceIndex",
                option.CurrentChoiceIndex);
            SetBoolean(state, "hasNewChoices", option.HasNewChoices);
            SetBoolean(state, "isSound", option.IsSound);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCustomizationChoices(
        lua_State state,
        IReadOnlyList<WowBarberShopCustomizationChoice> choices)
    {
        lua_createtable(state, choices.Count, 0);
        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            lua_createtable(state, 0, 9);
            SetInteger(state, "id", choice.Id);
            SetOptionalString(state, "name", choice.Name);
            SetBoolean(state, "ineligibleChoice", choice.IneligibleChoice);
            SetBoolean(state, "isNew", choice.IsNew);
            PushOptionalColor(state, choice.SwatchColor1);
            lua_setfield(state, -2, "swatchColor1");
            PushOptionalColor(state, choice.SwatchColor2);
            lua_setfield(state, -2, "swatchColor2");
            SetOptionalInteger(state, "soundKit", choice.SoundKitId);
            SetBoolean(state, "isLocked", choice.IsLocked);
            SetOptionalString(state, "lockedText", choice.LockedText);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCharacterData(
        lua_State state,
        WowBarberShopCharacterData characterData)
    {
        lua_createtable(state, 0, 5);
        SetOptionalString(state, "name", characterData.Name);
        SetOptionalString(state, "fileName", characterData.FileName);
        if (characterData.AlternateFormRaceData is { } alternateForm)
            PushAlternateFormRaceData(state, alternateForm);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, "alternateFormRaceData");
        SetString(
            state,
            "createScreenIconAtlas",
            characterData.CreateScreenIconAtlas);
        SetInteger(state, "sex", characterData.Sex);
    }

    private static void PushAlternateFormRaceData(
        lua_State state,
        WowBarberShopAlternateFormRaceData raceData)
    {
        lua_createtable(state, 0, 4);
        SetInteger(state, "raceID", raceData.RaceId);
        SetOptionalString(state, "name", raceData.Name);
        SetOptionalString(state, "fileName", raceData.FileName);
        SetString(
            state,
            "createScreenIconAtlas",
            raceData.CreateScreenIconAtlas);
    }

    private static void PushOptionalColor(
        lua_State state,
        WowBarberShopColor? color)
    {
        if (color is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_createtable(state, 0, 4);
        SetNumber(state, "r", color.Red);
        SetNumber(state, "g", color.Green);
        SetNumber(state, "b", color.Blue);
        SetNumber(state, "a", color.Alpha);
        ApplyMixinToTopTable(state, "ColorMixin");
    }

    private static void ApplyMixinToTopTable(lua_State state, string mixinName)
    {
        var target = AbsoluteIndex(state, -1);
        lua_getglobal(state, mixinName);
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        var mixin = AbsoluteIndex(state, -1);
        lua_pushnil(state);
        while (lua_next(state, mixin) != 0)
        {
            lua_pushvalue(state, -2);
            lua_pushvalue(state, -2);
            lua_settable(state, target);
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredInt32(state, index, usage);
    }

    private static double RequiredFiniteNumber(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value))
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        bool defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return defaultValue;
        return RequiredBoolean(state, index, usage);
    }

    private static int RequiredCustomizationFeatureMask(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < 0 || value > 0x7F)
            return luaL_error(state, usage);
        return value;
    }

    private static byte RequiredSex(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value is < 0 or > 4)
            return unchecked((byte)luaL_error(state, usage));
        return (byte)value;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value.HasValue)
            lua_pushnumber(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetUnsignedInteger(
        lua_State state,
        string field,
        uint value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        PushOptionalInteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }
}
