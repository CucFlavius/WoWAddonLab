using System.Collections;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowUiWidgetManagerApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] VisualizationFunctions =
    [
        "GetBulletTextListWidgetVisualizationInfo",
        "GetButtonHeaderWidgetVisualizationInfo",
        "GetCaptureBarWidgetVisualizationInfo",
        "GetCaptureZoneVisualizationInfo",
        "GetDiscreteProgressStepsVisualizationInfo",
        "GetDoubleIconAndTextWidgetVisualizationInfo",
        "GetDoubleStateIconRowVisualizationInfo",
        "GetDoubleStatusBarWidgetVisualizationInfo",
        "GetFillUpFramesWidgetVisualizationInfo",
        "GetHorizontalCurrenciesWidgetVisualizationInfo",
        "GetIconAndTextWidgetVisualizationInfo",
        "GetIconTextAndBackgroundWidgetVisualizationInfo",
        "GetIconTextAndCurrenciesWidgetVisualizationInfo",
        "GetItemDisplayVisualizationInfo",
        "GetMapPinAnimationWidgetVisualizationInfo",
        "GetPreyHuntProgressWidgetVisualizationInfo",
        "GetScenarioHeaderCurrenciesAndBackgroundWidgetVisualizationInfo",
        "GetScenarioHeaderDelvesWidgetVisualizationInfo",
        "GetScenarioHeaderTimerWidgetVisualizationInfo",
        "GetSpacerVisualizationInfo",
        "GetSpellDisplayVisualizationInfo",
        "GetStackedResourceTrackerWidgetVisualizationInfo",
        "GetStatusBarWidgetVisualizationInfo",
        "GetTextColumnRowVisualizationInfo",
        "GetTextureAndTextVisualizationInfo",
        "GetTextureAndTextRowVisualizationInfo",
        "GetTextureWithAnimationVisualizationInfo",
        "GetTextWithStateWidgetVisualizationInfo",
        "GetTextWithSubtextWidgetVisualizationInfo",
        "GetTugOfWarWidgetVisualizationInfo",
        "GetUnitPowerBarWidgetVisualizationInfo",
        "GetZoneControlVisualizationInfo"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetAllWidgetsBySetID",
                     "GetBelowMinimapWidgetSetID",
                     "GetObjectiveTrackerWidgetSetID",
                     "GetPowerBarWidgetSetID",
                     "GetTopCenterWidgetSetID",
                     "GetWidgetSetInfo",
                     "RegisterUnitForWidgetUpdates",
                     "SetProcessingUnit",
                     "SetProcessingUnitGuid",
                     "UnregisterUnitForWidgetUpdates"
                 }.Concat(VisualizationFunctions))
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_UIWidgetManager");
    }

    internal static void RegisterEnums(lua_State state)
    {
        SetEnum(state, "WidgetShownState", "Hidden", "Shown");
        SetEnum(state, "WidgetEnabledState", "Disabled", "Yellow", "Red", "White", "Green", "Artifact", "Black", "BrightBlue");
        SetEnum(state, "WidgetAnimationType", "None", "Fade");
        SetEnum(state, "UIWidgetTooltipLocation", "Default", "BottomLeft", "Left", "TopLeft", "Top", "TopRight", "Right", "BottomRight", "Bottom");
        SetEnum(state, "UIWidgetButtonIconType", "Exit", "Speak", "Undo", "Checkmark", "RedX");
        SetEnum(state, "UIWidgetButtonEnabledState", "Disabled", "Enabled");
        SetEnum(state, "UIWidgetSpellButtonCooldownType", "HideCooldown", "ShowCooldown", "ShowCooldownAndDisableOnCooldown");
        SetEnum(state, "CaptureBarWidgetFillDirectionType", "RightToLeft", "LeftToRight");
        SetEnum(state, "WidgetGlowAnimType", "None", "Pulse");
        SetEnum(state, "ZoneControlMode", "BothStatesAreGood", "State1IsGood", "State2IsGood", "NeitherStateIsGood");
        SetEnum(state, "ZoneControlLeadingEdgeType", "NoLeadingEdge", "UseLeadingEdge");
        SetEnum(state, "ZoneControlDangerFlashType", "ShowOnGoodStates", "ShowOnBadStates", "ShowOnBoth", "ShowOnNeither");
        SetEnum(state, "ZoneControlState", "State1", "State2");
        SetEnum(state, "ZoneControlActiveState", "Inactive", "Active");
        SetEnum(state, "ZoneControlFillType", "SingleFillClockwise", "SingleFillCounterClockwise", "DoubleFillClockwise", "DoubleFillCounterClockwise");
        SetEnum(state, "IconState", "Hidden", "ShowState1", "ShowState2");
        SetEnum(state, "StatusBarValueTextType", "Hidden", "Percentage", "Value", "Time", "TimeShowOneLevelOnly", "ValueOverMax", "ValueOverMaxNormalized");
        SetEnum(state, "UIWidgetMotionType", "Instant", "Smooth");
        SetEnum(state, "UIWidgetFontType", "Normal", "Shadow", "Outline");
        SetEnum(state, "UIWidgetTextSizeType", "Small12Pt", "Medium16Pt", "Large24Pt", "Huge27Pt", "Standard14Pt", "Small10Pt", "Small11Pt", "Medium18Pt", "Large20Pt");
        SetEnum(state, "WidgetIconSizeType", "Small", "Medium", "Large", "Standard");
        SetEnum(state, "UIWidgetUpdateAnimType", "None", "Flash", "FlashAndAnimateNumber");
        SetEnum(state, "IconAndTextWidgetState", "Hidden", "Shown", "ShownWithDynamicIconFlashing", "ShownWithDynamicIconNotFlashing");
        SetEnum(state, "IconAndTextShiftTextType", "None", "ShiftText");
        SetEnum(state, "ItemDisplayTextDisplayStyle", "WorldQuestReward", "ItemNameAndInfoText", "ItemNameOnlyCentered", "PlayerChoiceReward");
        SetEnum(state, "UIWidgetOverrideState", "Inactive", "Active");
        SetEnum(state, "MapPinAnimationType", "None", "Pulse");
        SetEnum(state, "PreyHuntProgressState", "Cold", "Warm", "Hot", "Final");
        SetEnum(state, "SpellDisplayIconDisplayType", "Buff", "Debuff", "Circular", "NoBorder");
        SetEnum(state, "SpellDisplayTextShownStateType", "Shown", "Hidden");
        SetEnum(state, "WidgetTextHorizontalAlignmentType", "Left", "Center", "Right");
        SetEnum(state, "SpellDisplayTint", "None", "Red");
        SetEnum(state, "WidgetShowGlowState", "HideGlow", "ShowGlow");
        SetEnum(state, "UIWidgetRewardShownState", "Hidden", "ShownEarned", "ShownUnearned");
        SetEnum(state, "StatusBarOverrideBarTextShownType", "Never", "Always", "OnlyOnMouseover", "OnlyNotOnMouseover");
        SetEnum(state, "StatusBarColorTintValue", "None", "Black", "White", "Red", "Yellow", "Orange", "Purple", "Green", "Blue");
        SetEnum(state, "WidgetOpacityType", "OneHundred", "Ninety", "Eighty", "Seventy", "Sixty", "Fifty", "Forty", "Thirty", "Twenty", "Ten", "Zero");
        SetEnum(state, "UIWidgetTextureAndTextSizeType", "Small", "Medium", "Large", "Huge", "Standard", "Medium2");
        SetEnum(state, "UIWidgetTextFormatType", "None", "TimeOneLevel", "TimeTwoLevel", "LeadingZeroesWithSixDigits");
        SetEnum(state, "WidgetIconSourceType", "Spell", "Item");
        SetEnum(state, "TugOfWarStyleValue", "DefaultYellow", "ArchaeologyBrown");
        SetEnum(state, "TugOfWarMarkerArrowShownState", "Never", "Always", "FlashOnMove");
        SetEnum(state, "UIWidgetBlendModeType", "Opaque", "Additive");
        SetEnum(state, "WidgetUnitPowerBarFlashMomentType", "FlashWhenMax", "FlashWhenMin", "NeverFlash");
        SetEnum(state, "ItemDisplayTooltipEnabledType", "Enabled", "Disabled");
        SetEnum(state, "WidgetCurrencyClass", "Currency", "Item");
    }

    private static int Dispatch(lua_State state)
    {
        var widgets = LuaBindings.GetRuntime(state).UiWidgets;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetObjectiveTrackerWidgetSetID":
                return PushInteger(state, widgets.ObjectiveTrackerWidgetSetId);
            case "GetBelowMinimapWidgetSetID":
                return PushInteger(state, widgets.BelowMinimapWidgetSetId);
            case "GetPowerBarWidgetSetID":
                return PushInteger(state, widgets.PowerBarWidgetSetId);
            case "GetTopCenterWidgetSetID":
                return PushInteger(state, widgets.TopCenterWidgetSetId);
            case "GetWidgetSetInfo":
            {
                var setId = RequiredInt32(
                    state,
                    1,
                    "Usage: local widgetSetInfo = C_UIWidgetManager.GetWidgetSetInfo(widgetSetID)");
                if (!widgets.WidgetSets.TryGetValue(setId, out var setInfo))
                    return 0;
                lua_createtable(state, 0, 2);
                SetNumber(state, "layoutDirection", setInfo.LayoutDirection);
                SetNumber(state, "verticalPadding", setInfo.VerticalPadding);
                return 1;
            }
            case "GetAllWidgetsBySetID":
            {
                var setId = RequiredInt32(
                    state,
                    1,
                    "Usage: local widgets = C_UIWidgetManager.GetAllWidgetsBySetID(setID)");
                widgets.WidgetsBySetId.TryGetValue(setId, out var setWidgets);
                setWidgets ??= [];
                lua_createtable(state, setWidgets.Count, 0);
                for (var index = 0; index < setWidgets.Count; index++)
                {
                    lua_createtable(state, 0, 4);
                    SetNumber(state, "widgetID", setWidgets[index].WidgetId);
                    SetNumber(
                        state,
                        "widgetSetID",
                        setWidgets[index].WidgetSetId ?? setId);
                    SetNumber(state, "widgetType", setWidgets[index].WidgetType);
                    SetOptionalString(state, "unitToken", setWidgets[index].UnitToken);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "RegisterUnitForWidgetUpdates":
            {
                const string usage =
                    "Usage: C_UIWidgetManager.RegisterUnitForWidgetUpdates(unitToken [, isGuid])";
                var unit = RequiredString(state, 1, usage);
                var isGuid = lua_toboolean(state, 2) != 0;
                (isGuid ? widgets.RegisteredUnitGuids : widgets.RegisteredUnitTokens).Add(unit);
                return 0;
            }
            case "UnregisterUnitForWidgetUpdates":
            {
                const string usage =
                    "Usage: C_UIWidgetManager.UnregisterUnitForWidgetUpdates(unitToken [, isGuid])";
                var unit = RequiredString(state, 1, usage);
                var isGuid = lua_toboolean(state, 2) != 0;
                (isGuid ? widgets.RegisteredUnitGuids : widgets.RegisteredUnitTokens).Remove(unit);
                return 0;
            }
            case "SetProcessingUnit":
                widgets.ProcessingUnit = OptionalUnitToken(
                    state,
                    "Usage: C_UIWidgetManager.SetProcessingUnit([unit])");
                widgets.ProcessingUnitIsGuid = false;
                return 0;
            case "SetProcessingUnitGuid":
                widgets.ProcessingUnit = OptionalString(
                    state,
                    1,
                    "Usage: C_UIWidgetManager.SetProcessingUnitGuid([unit])");
                widgets.ProcessingUnitIsGuid = true;
                return 0;
        }

        if (VisualizationFunctions.Contains(operation))
        {
            var widgetId = RequiredInt32(
                state,
                1,
                $"Usage: local widgetInfo = C_UIWidgetManager.{operation}(widgetID)");
            if (!widgets.VisualizationInfo.TryGetValue(
                    (operation, widgetId),
                    out var visualization))
            {
                lua_pushnil(state);
                return 1;
            }
            PushValue(state, visualization);
            return 1;
        }
        return 0;
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static string RequiredString(lua_State state, int index, string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredString(state, index, usage);
    }

    private static string? OptionalUnitToken(lua_State state, string usage)
    {
        var unitToken = OptionalString(state, 1, usage);
        if (unitToken is not null && !LuaBindings.IsRecognizedUnitToken(unitToken))
        {
            luaL_error(state, usage);
            return null;
        }
        return unitToken;
    }

    private static int PushInteger(lua_State state, int value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static void PushValue(lua_State state, object? value)
    {
        switch (value)
        {
            case null:
                lua_pushnil(state);
                break;
            case bool boolean:
                lua_pushboolean(state, boolean ? 1 : 0);
                break;
            case string text:
                lua_pushstring(state, text);
                break;
            case byte or short or int or long or float or double or decimal:
                lua_pushnumber(state, Convert.ToDouble(value));
                break;
            case IReadOnlyDictionary<string, object?> dictionary:
                lua_createtable(state, 0, dictionary.Count);
                foreach (var pair in dictionary)
                {
                    PushValue(state, pair.Value);
                    lua_setfield(state, -2, pair.Key);
                }
                break;
            case IEnumerable enumerable:
                lua_newtable(state);
                var index = 1;
                foreach (var item in enumerable)
                {
                    PushValue(state, item);
                    lua_rawseti(state, -2, index++);
                }
                break;
            default:
                lua_pushnil(state);
                break;
        }
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalString(
        lua_State state,
        string key,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params string[] memberNames)
    {
        lua_createtable(state, 0, memberNames.Length);
        for (var value = 0; value < memberNames.Length; value++)
            SetNumber(state, memberNames[value], value);
        lua_setfield(state, -2, name);

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", memberNames.Length);
        SetNumber(state, "MinValue", 0);
        SetNumber(state, "MaxValue", memberNames.Length - 1);
        lua_setfield(state, -2, $"{name}Meta");
    }
}
