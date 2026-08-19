using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowClickBindingsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanSpellBeClickBound", "ExecuteBinding", "GetBindingType",
        "GetEffectiveInteractionButton", "GetProfileInfo", "GetTutorialShown",
        "ResetCurrentProfile", "SetProfileByInfo", "SetTutorialShown"
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
        lua_setglobal(state, "C_ClickBindings");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var bindings = runtime.ClickBindings;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CanSpellBeClickBound":
                if (!TryReadSpellIdentifier(state, bindings, out var spellId))
                    return luaL_error(
                        state,
                        "Usage: local canBeBound = C_ClickBindings.CanSpellBeClickBound(spellID)");
                lua_pushboolean(state, bindings.CanSpellBeClickBound(spellId) ? 1 : 0);
                return 1;
            case "GetTutorialShown":
                lua_pushboolean(state, bindings.TutorialShown ? 1 : 0);
                return 1;
            case "SetTutorialShown":
                bindings.TutorialShown = true;
                return 0;
            case "ResetCurrentProfile":
                bindings.Profile.Clear();
                return 0;
            case "GetProfileInfo":
                PushProfile(state, bindings.Profile);
                return 1;
            case "SetProfileByInfo":
                if (!TryReadProfile(state, bindings, out var profile))
                    return luaL_error(
                        state,
                        "Usage: C_ClickBindings.SetProfileByInfo(infoVec)");
                ReplaceProfile(bindings, profile);
                return 0;
            case "GetBindingType":
            {
                if (!TryReadBindingKey(
                        state,
                        "Usage: local type = C_ClickBindings.GetBindingType(button, modifiers)",
                        out var button,
                        out var modifiers))
                {
                    return luaL_error(
                        state,
                        "Usage: local type = C_ClickBindings.GetBindingType(button, modifiers)");
                }
                var match = FindBinding(button, modifiers, bindings.Profile);
                lua_pushinteger(
                    state,
                    match?.Type ?? DefaultInteractionType(button, modifiers));
                return 1;
            }
            case "GetEffectiveInteractionButton":
            {
                const string usage =
                    "Usage: local effectiveButton = C_ClickBindings.GetEffectiveInteractionButton(button, modifiers)";
                if (!TryReadBindingKey(state, usage, out var button, out var modifiers))
                    return luaL_error(state, usage);
                var match = FindBinding(button, modifiers, bindings.Profile);
                var effectiveButton = match is { Type: 3, ActionId: 1 }
                    ? "LeftButton"
                    : match is { Type: 3, ActionId: 2 }
                        ? "RightButton"
                        : DefaultInteractionButton(button, modifiers);
                if (effectiveButton is null)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, effectiveButton);
                return 1;
            }
            case "ExecuteBinding":
            {
                const string usage =
                    "Usage: C_ClickBindings.ExecuteBinding(targetToken, button, modifiers)";
                if (!TryReadRequiredString(state, 1, out var targetToken) ||
                    !TryReadRequiredString(state, 2, out var button) ||
                    !TryReadRequiredUInt32(state, 3, out var modifiers))
                {
                    return luaL_error(state, usage);
                }
                if (runtime.Client.InCombatLockdown)
                    return 0;
                var match = FindBinding(button, modifiers, bindings.Profile);
                var canonicalButton = CanonicalButton(button);
                if (match is not null &&
                    canonicalButton is not null &&
                    targetToken.Length != 0)
                {
                    bindings.LastExecutedBinding = new WowExecutedClickBindingState(
                        targetToken,
                        canonicalButton,
                        modifiers,
                        match.Type,
                        match.ActionId);
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static WowClickBindingInfoState? FindBinding(
        string button,
        uint modifiers,
        IEnumerable<WowClickBindingInfoState> profile)
    {
        var canonicalButton = CanonicalButton(button);
        if (canonicalButton is null)
            return null;
        return profile.FirstOrDefault(
            value => value.Button.Equals(canonicalButton, StringComparison.Ordinal) &&
                     value.Modifiers == modifiers);
    }

    private static int DefaultInteractionType(string button, uint modifiers) =>
        DefaultInteractionButton(button, modifiers) is null ? 0 : 3;

    private static string? DefaultInteractionButton(string button, uint modifiers)
    {
        if (modifiers != 0)
            return null;
        var canonical = CanonicalButton(button);
        return canonical is "LeftButton" or "RightButton" ? canonical : null;
    }

    private static void PushProfile(
        lua_State state,
        IEnumerable<WowClickBindingInfoState> profile)
    {
        lua_newtable(state);
        var index = 1;
        foreach (var binding in profile
                     .Where(value => value.ActionId != 0)
                     .OrderBy(value => ButtonMask(value.Button))
                     .ThenBy(value => value.Modifiers))
        {
            lua_newtable(state);
            SetInteger(state, "type", binding.Type);
            SetInteger(state, "actionID", binding.ActionId);
            SetString(state, "button", binding.Button);
            SetNumber(state, "modifiers", binding.Modifiers);
            lua_rawseti(state, -2, index++);
        }
    }

    private static bool TryReadProfile(
        lua_State state,
        WowClickBindingsState bindings,
        out List<WowClickBindingInfoState> profile)
    {
        profile = [];
        if (lua_type(state, 1) != LUA_TTABLE)
            return false;
        var count = (int)lua_objlen(state, 1);
        for (var index = 1; index <= count; index++)
        {
            lua_rawgeti(state, 1, index);
            if (lua_type(state, -1) != LUA_TTABLE ||
                !TryReadEnumField(state, -1, "type", out var type) ||
                !TryReadInt32Field(state, -1, "actionID", out var actionId) ||
                !TryReadStringField(state, -1, "button", out var button) ||
                !TryReadUInt32Field(state, -1, "modifiers", out var modifiers))
            {
                lua_pop(state, 1);
                profile.Clear();
                return false;
            }
            profile.Add(new WowClickBindingInfoState(type, actionId, button, modifiers));
            lua_pop(state, 1);
        }
        return true;
    }

    private static void ReplaceProfile(
        WowClickBindingsState bindings,
        IEnumerable<WowClickBindingInfoState> source)
    {
        var profile = new Dictionary<(int ButtonMask, uint Modifiers), WowClickBindingInfoState>();
        var hasTargetInteraction = false;
        var hasContextMenuInteraction = false;
        foreach (var value in source)
        {
            if (value.Type == 3)
            {
                if (value.ActionId == 1)
                {
                    if (hasTargetInteraction)
                        continue;
                    hasTargetInteraction = true;
                }
                else if (value.ActionId == 2)
                {
                    if (hasContextMenuInteraction)
                        continue;
                    hasContextMenuInteraction = true;
                }
            }

            var buttonMask = ButtonMask(value.Button);
            if (buttonMask == 0 ||
                value.Type == 0 ||
                value.ActionId <= 0 ||
                value.Type == 3 && value.ActionId is not (1 or 2) ||
                value.Type == 1 && !bindings.CanSpellBeClickBound(value.ActionId))
            {
                continue;
            }

            var canonicalButton = ButtonName(buttonMask);
            profile[(buttonMask, value.Modifiers)] = value with { Button = canonicalButton };
        }

        bindings.Profile.Clear();
        foreach (var value in profile
                     .OrderBy(pair => pair.Key.ButtonMask)
                     .ThenBy(pair => pair.Key.Modifiers)
                     .Select(pair => pair.Value))
        {
            bindings.Profile.Add(value);
        }
    }

    private static bool TryReadBindingKey(
        lua_State state,
        string usage,
        out string button,
        out uint modifiers)
    {
        _ = usage;
        button = string.Empty;
        modifiers = 0;
        return TryReadRequiredString(state, 1, out button) &&
               TryReadRequiredUInt32(state, 2, out modifiers);
    }

    private static bool TryReadSpellIdentifier(
        lua_State state,
        WowClickBindingsState bindings,
        out int spellId)
    {
        spellId = 0;
        if (lua_type(state, 1) == LUA_TNUMBER)
        {
            var value = lua_tonumber(state, 1);
            if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
                return false;
            spellId = (int)value;
            return true;
        }
        if (lua_type(state, 1) != LUA_TSTRING)
            return false;

        var valueText = lua_tostring(state, 1) ?? string.Empty;
        var spellMarker = valueText.IndexOf("spell:", StringComparison.OrdinalIgnoreCase);
        if (spellMarker >= 0)
        {
            var identifier = valueText[(spellMarker + 6)..];
            var terminator = identifier.IndexOfAny([':', '|']);
            if (terminator >= 0)
                identifier = identifier[..terminator];
            if (identifier.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(
                    identifier.AsSpan(2),
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out spellId);
            }
            return int.TryParse(
                identifier,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out spellId);
        }

        bindings.SpellIdsByName.TryGetValue(valueText, out spellId);
        return true;
    }

    private static bool TryReadRequiredString(
        lua_State state,
        int index,
        out string value)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            value = string.Empty;
            return false;
        }
        value = lua_tostring(state, index) ?? string.Empty;
        return true;
    }

    private static bool TryReadRequiredUInt32(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (lua_type(state, index) != LUA_TNUMBER)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < uint.MinValue ||
            number > uint.MaxValue ||
            Math.Truncate(number) != number)
        {
            return false;
        }
        value = (uint)number;
        return true;
    }

    private static bool TryReadEnumField(
        lua_State state,
        int index,
        string name,
        out int value)
    {
        if (!TryReadInt32Field(state, index, name, out value))
            return false;
        return value is >= 0 and <= 4;
    }

    private static bool TryReadInt32Field(
        lua_State state,
        int index,
        string name,
        out int value)
    {
        var absolute = index < 0 ? lua_gettop(state) + index + 1 : index;
        lua_getfield(state, absolute, name);
        var valid = lua_type(state, -1) == LUA_TNUMBER;
        var number = valid ? lua_tonumber(state, -1) : 0;
        valid = valid &&
                double.IsFinite(number) &&
                number >= int.MinValue &&
                number <= int.MaxValue &&
                Math.Truncate(number) == number;
        value = valid ? (int)number : 0;
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadUInt32Field(
        lua_State state,
        int index,
        string name,
        out uint value)
    {
        var absolute = index < 0 ? lua_gettop(state) + index + 1 : index;
        lua_getfield(state, absolute, name);
        var valid = TryReadRequiredUInt32(state, -1, out value);
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadStringField(
        lua_State state,
        int index,
        string name,
        out string value)
    {
        var absolute = index < 0 ? lua_gettop(state) + index + 1 : index;
        lua_getfield(state, absolute, name);
        var valid = TryReadRequiredString(state, -1, out value);
        lua_pop(state, 1);
        return valid;
    }

    private static string? CanonicalButton(string value)
    {
        var mask = ButtonMask(value);
        return mask == 0 ? null : ButtonName(mask);
    }

    private static int ButtonMask(string value)
    {
        if (value.Equals("LeftButton", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (value.Equals("MiddleButton", StringComparison.OrdinalIgnoreCase))
            return 2;
        if (value.Equals("RightButton", StringComparison.OrdinalIgnoreCase))
            return 4;
        if (!value.StartsWith("Button", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(value.AsSpan(6), out var number) ||
            number is < 4 or > 31)
        {
            return 0;
        }
        return 1 << (number - 1);
    }

    private static string ButtonName(int mask)
    {
        if (mask == 1)
            return "LeftButton";
        if (mask == 2)
            return "MiddleButton";
        if (mask == 4)
            return "RightButton";
        return $"Button{System.Numerics.BitOperations.TrailingZeroCount((uint)mask) + 1}";
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, uint value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }
}
