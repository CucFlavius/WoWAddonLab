using LuaNET.Lua51;
using WoWAddonLab.Emulator.UI;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowBindingApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetBindingKey", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetBindingAction", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetBinding", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetBindingName", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetBindingText", Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsBindingForGamePad", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetModifiedClick", Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsModifiedClick", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetNumBindings", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetCurrentBindingSet", Callback);
        LuaBindings.RegisterClosureGlobal(state, "LoadBindings", Callback);
        LuaBindings.RegisterClosureGlobal(state, "SaveBindings", Callback);
        LuaBindings.RegisterClosureGlobal(state, "SetBinding", Callback);
        LuaBindings.RegisterClosureGlobal(state, "SetModifiedClick", Callback);
        LuaBindings.RegisterClosureGlobal(state, "RunBinding", Callback);
        foreach (var function in new[]
                 {
                     "ClearOverrideBindings",
                     "SetOverrideBinding",
                     "SetOverrideBindingClick",
                     "SetOverrideBindingItem",
                     "SetOverrideBindingMacro",
                     "SetOverrideBindingSpell"
                 })
        {
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
        }

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "ActivateBindingContext",
                     "DeactivateBindingContext",
                     "GetBindingByKey",
                     "GetBindingContextForAction",
                     "GetBindingIndex",
                     "GetCustomBindingType",
                     "GetSearchTagsForAction",
                     "GetTurnStrafeStyle",
                     "IsBindingContextActive",
                     "SetTurnStrafeStyle",
                     "UpdateTurnStrafeBindingsForCharacter"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_KeyBindings");
        RegisterEnums(state);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var bindings = runtime.Bindings;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetBindingText":
            {
                lua_pushstring(state, lua_isstring(state, 1) != 0
                    ? lua_tostring(state, 1) ?? string.Empty
                    : string.Empty);
                return 1;
            }
            case "IsBindingForGamePad":
            {
                var binding = RequiredString(
                    state,
                    "Usage: local isGamePad = IsBindingForGamePad(\"binding\")");
                var isGamePad = binding
                    .Split('-', StringSplitOptions.RemoveEmptyEntries)
                    .Any(part => part.StartsWith("PAD", StringComparison.OrdinalIgnoreCase));
                lua_pushboolean(state, isGamePad ? 1 : 0);
                return 1;
            }
            case "GetBindingName":
            {
                var command = RequiredString(state, "Usage: GetBindingName(\"COMMAND\")");
                if (runtime.GlobalStringProvider?.Strings.TryGetValue(
                        $"BINDING_NAME_{command}",
                        out var name) == true)
                    lua_pushstring(state, name);
                else
                    lua_pushstring(state, command);
                return 1;
            }
            case "GetBinding":
            {
                if (lua_isnumber(state, 1) == 0)
                    return luaL_error(state, "Usage: GetBinding(index, [AlwaysIncludeGamepad])");
                var index = (int)lua_tonumber(state, 1);
                if (index < 1 || index > bindings.All.Count)
                    return luaL_error(state, "GetBinding: out of range index");
                var binding = bindings.All.ElementAt(index - 1);
                lua_pushstring(state, binding.Key);
                lua_pushstring(state, bindings.GetCategory(binding.Key));
                foreach (var key in binding.Value)
                    lua_pushstring(state, key);
                return 2 + binding.Value.Count;
            }
            case "GetBindingKey":
            {
                var command = RequiredString(
                    state,
                    "Usage: GetBindingKey(\"COMMAND\", [\"AlwaysIncludeGamepad\"])");
                var keys = bindings.GetKeys(command);
                foreach (var key in keys)
                    lua_pushstring(state, key);
                return keys.Count;
            }
            case "GetBindingAction":
            {
                var key = RequiredString(
                    state,
                    "Usage: GetBindingAction(\"KEY\"[, checkOverride, context])");
                var context = OptionalBindingContext(state, 3);
                var checkOverride = lua_toboolean(state, 2) != 0;
                lua_pushstring(state, checkOverride
                    ? bindings.GetEffectiveAction(key, context)
                    : bindings.GetAction(key, context));
                return 1;
            }
            case "ClearOverrideBindings":
            {
                var owner = RequiredOwner(
                    runtime,
                    state,
                    "Usage: ClearOverrideBindings(owner)");
                bindings.ClearOverrideBindings(owner.Id);
                return 0;
            }
            case "SetOverrideBinding":
            case "SetOverrideBindingClick":
            case "SetOverrideBindingItem":
            case "SetOverrideBindingMacro":
            case "SetOverrideBindingSpell":
                return SetOverrideBinding(runtime, state, operation);
            case "GetNumBindings":
                lua_pushinteger(state, bindings.All.Count);
                return 1;
            case "GetCurrentBindingSet":
                lua_pushinteger(state, bindings.CurrentSet);
                return 1;
            case "LoadBindings":
            {
                var bindingSet = (int)lua_tonumber(state, 1);
                if (bindingSet is < 0 or > 2)
                    return luaL_error(state, "Usage: LoadBindings(0||1||2)");
                runtime.TriggerEvent("UPDATE_BINDINGS");
                return 0;
            }
            case "SaveBindings":
            {
                var bindingSet = (int)lua_tonumber(state, 1);
                if (bindingSet is not (1 or 2))
                    return luaL_error(state, "Usage: SaveBindings(1||2)");
                bindings.CurrentSet = bindingSet;
                runtime.TriggerEvent("UPDATE_BINDINGS");
                return 0;
            }
            case "SetBinding":
            {
                var key = RequiredString(
                    state,
                    "Usage: SetBinding(\"KEY\"[, \"COMMAND\", \"CONTEXT\"])");
                var command = lua_tostring(state, 2);
                var context = OptionalBindingContext(state, 3);
                var changed = bindings.SetBinding(key, command, context);
                if (changed)
                    runtime.TriggerEvent("UPDATE_BINDINGS");
                lua_pushboolean(state, changed ? 1 : 0);
                return 1;
            }
            case "RunBinding":
            {
                var command = RequiredString(state, "Usage: RunBinding(\"COMMAND\"[, \"KEYSTATE\"])");
                var keyState = lua_isnoneornil(state, 2) != 0
                    ? "down"
                    : lua_tostring(state, 2);
                if (keyState is null ||
                    !keyState.Equals("down", StringComparison.OrdinalIgnoreCase) &&
                    !keyState.Equals("up", StringComparison.OrdinalIgnoreCase))
                    return luaL_error(state, "Usage: RunBinding(\"COMMAND\"[, \"KEYSTATE\"])");
                runtime.ExecuteBinding(command, keyState.ToLowerInvariant());
                return 0;
            }
            case "GetModifiedClick":
            {
                var action = RequiredString(state, "Usage: GetModifiedClick(\"action\")");
                if (!bindings.HasModifiedClickAction(action))
                    lua_pushnil(state);
                else
                    lua_pushstring(state, bindings.GetModifiedClick(action));
                return 1;
            }
            case "SetModifiedClick":
            {
                var action = RequiredString(
                    state,
                    "Usage: SetModifiedClick(\"action\", \"binding\")");
                var modifier = lua_tostring(state, 2);
                if (!bindings.SetModifiedClick(action, modifier))
                {
                    return luaL_error(
                        state,
                        $"SetModifiedClick(): Unknown action ({action}) or binding ({modifier ?? string.Empty})");
                }
                runtime.TriggerEvent("UPDATE_BINDINGS");
                return 0;
            }
            case "IsModifiedClick":
            {
                var action = lua_tostring(state, 1);
                if (action is null)
                {
                    lua_pushboolean(
                        state,
                        runtime.ControlDown || runtime.ShiftDown || runtime.AltDown ? 1 : 0);
                    return 1;
                }

                var configured = bindings.GetModifiedClick(action);
                var matches = configured switch
                {
                    "CTRL" => runtime.ControlDown && !runtime.ShiftDown && !runtime.AltDown,
                    "SHIFT" => runtime.ShiftDown && !runtime.ControlDown && !runtime.AltDown,
                    "ALT" => runtime.AltDown && !runtime.ControlDown && !runtime.ShiftDown,
                    _ => false
                };
                lua_pushboolean(state, matches ? 1 : 0);
                return 1;
            }
            case "ActivateBindingContext":
            {
                var context = RequiredEnum(
                    state,
                    1,
                    0,
                    9,
                    "Usage: C_KeyBindings.ActivateBindingContext(newContext)");
                bindings.ActiveContexts.Add(context);
                return 0;
            }
            case "DeactivateBindingContext":
            {
                var context = RequiredEnum(
                    state,
                    1,
                    0,
                    9,
                    "Usage: C_KeyBindings.DeactivateBindingContext(context)");
                bindings.ActiveContexts.Remove(context);
                return 0;
            }
            case "GetBindingByKey":
            {
                var key = RequiredString(
                    state,
                    "Usage: local binding = C_KeyBindings.GetBindingByKey(action [, context])");
                var action = bindings.GetAction(key, OptionalBindingContext(state, 2));
                if (action.Length == 0)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, action);
                return 1;
            }
            case "GetBindingContextForAction":
            {
                var action = RequiredString(
                    state,
                    "Usage: local context = C_KeyBindings.GetBindingContextForAction(action)");
                lua_pushinteger(state, bindings.GetContext(action));
                return 1;
            }
            case "GetBindingIndex":
            {
                var action = RequiredString(
                    state,
                    "Usage: local bindingIndex = C_KeyBindings.GetBindingIndex(action)");
                if (bindings.GetBindingIndex(action) is { } bindingIndex)
                    lua_pushinteger(state, bindingIndex);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "GetCustomBindingType":
            {
                var bindingIndex = RequiredOneBasedIndex(
                    state,
                    "Usage: local customBindingType = C_KeyBindings.GetCustomBindingType(bindingIndex)");
                if (bindings.GetCustomBindingType(bindingIndex) is { } customType)
                    lua_pushinteger(state, customType);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "GetSearchTagsForAction":
            {
                var action = RequiredString(
                    state,
                    "Usage: local searchTags = C_KeyBindings.GetSearchTagsForAction(action)");
                if (bindings.GetSearchTags(action) is not { } tags)
                {
                    lua_pushnil(state);
                    return 1;
                }

                lua_createtable(state, tags.Count, 0);
                for (var index = 0; index < tags.Count; index++)
                {
                    lua_pushstring(state, tags[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetTurnStrafeStyle":
                lua_pushinteger(state, bindings.TurnStrafeStyle);
                return 1;
            case "IsBindingContextActive":
            {
                var context = RequiredEnum(
                    state,
                    1,
                    0,
                    9,
                    "Usage: local isActive = C_KeyBindings.IsBindingContextActive(context)");
                lua_pushboolean(state, bindings.ActiveContexts.Contains(context) ? 1 : 0);
                return 1;
            }
            case "SetTurnStrafeStyle":
                bindings.TurnStrafeStyle = RequiredEnum(
                    state,
                    1,
                    0,
                    2,
                    "Usage: C_KeyBindings.SetTurnStrafeStyle(style)");
                return 0;
            case "UpdateTurnStrafeBindingsForCharacter":
                return 0;
            default:
                return 0;
        }
    }

    private static string RequiredString(lua_State state, string usage)
    {
        if (lua_isstring(state, 1) == 0)
            luaL_error(state, usage);
        return lua_tostring(state, 1) ?? string.Empty;
    }

    private static UiObject RequiredOwner(
        LuaRuntime runtime,
        lua_State state,
        string usage)
    {
        var owner = LuaBindings.GetObject(runtime, 1);
        if (owner is null || owner.IsRegion)
            luaL_error(state, usage);
        return owner!;
    }

    private static int SetOverrideBinding(
        LuaRuntime runtime,
        lua_State state,
        string operation)
    {
        var usage = operation switch
        {
            "SetOverrideBinding" =>
                "Usage: SetOverrideBinding(owner, isPriority, \"KEY\"[, \"COMMAND\"])",
            "SetOverrideBindingClick" =>
                "Usage: SetOverrideBindingClick(owner, isPriority, \"KEY\", \"buttonName\"[, \"mouseButton\"])",
            "SetOverrideBindingItem" =>
                "Usage: SetOverrideBindingItem(owner, isPriority, \"KEY\", \"itemname\")",
            "SetOverrideBindingMacro" =>
                "Usage: SetOverrideBindingMacro(owner, isPriority, \"KEY\", \"macroname\"|macroid)",
            _ => "Usage: SetOverrideBindingSpell(owner, isPriority, \"KEY\", \"spellname\")"
        };
        var owner = RequiredOwner(runtime, state, usage);
        if (lua_isstring(state, 3) == 0)
            return luaL_error(state, usage);

        var key = lua_tostring(state, 3) ?? string.Empty;
        var priority = lua_type(state, 2) == LUA_TBOOLEAN && lua_toboolean(state, 2) != 0;
        string? action;
        if (operation == "SetOverrideBinding")
        {
            action = lua_isstring(state, 4) != 0 ? lua_tostring(state, 4) : null;
        }
        else
        {
            if (lua_isstring(state, 4) == 0)
                return luaL_error(state, usage);
            var target = lua_tostring(state, 4) ?? string.Empty;
            action = operation switch
            {
                "SetOverrideBindingClick" =>
                    $"CLICK {target}:{(lua_isstring(state, 5) != 0 ? lua_tostring(state, 5) : "LeftButton")}",
                "SetOverrideBindingItem" => $"ITEM {target}",
                "SetOverrideBindingMacro" => $"MACRO {target}",
                _ => $"SPELL {target}"
            };
        }

        runtime.Bindings.SetOverrideBinding(owner.Id, priority, key, action);
        return 0;
    }

    private static int? OptionalBindingContext(lua_State state, int index)
    {
        if (lua_isnumber(state, index) == 0)
            return null;
        var context = (int)lua_tonumber(state, index);
        return context is >= 0 and <= 9 ? context : null;
    }

    private static int RequiredEnum(
        lua_State state,
        int index,
        int minimum,
        int maximum,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = (int)lua_tonumber(state, index);
        return value >= minimum && value <= maximum ? value : luaL_error(state, usage);
    }

    private static int RequiredOneBasedIndex(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);
        var index = (int)lua_tonumber(state, 1);
        return index > 0 ? index : luaL_error(state, usage);
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        SetEnum(
            state,
            "BindingContext",
            ("None", 0),
            ("HousingEditor", 1),
            ("HousingEditorBasicDecorMode", 2),
            ("HousingEditorExpertDecorMode", 3),
            ("HousingEditorCustomizeMode", 4),
            ("HousingEditorCleanupMode", 5),
            ("HousingEditorLayoutMode", 6),
            ("HousingEditorBasicAndExpertDecorMode", 7),
            ("HousingEditorExteriorCustomizationMode", 8),
            ("ReservedFutureFeatureBinding01", 9));
        SetEnum(state, "BindingSet", ("Default", 0), ("Account", 1), ("Character", 2), ("Current", 3));
        SetEnum(state, "CustomBindingType", ("VoicePushToTalk", 0));
        SetEnum(state, "TurnStrafeStyle", ("Modern", 0), ("Legacy", 1), ("Custom", 2));
        SetMeta(state, "BindingContextMeta", 10, 0, 9);
        SetMeta(state, "BindingSetMeta", 4, 0, 3);
        SetMeta(state, "CustomBindingTypeMeta", 1, 0, 0);
        SetMeta(state, "TurnStrafeStyleMeta", 3, 0, 2);
        lua_setglobal(state, "Enum");
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] entries)
    {
        lua_newtable(state);
        foreach (var entry in entries)
        {
            lua_pushinteger(state, entry.Value);
            lua_setfield(state, -2, entry.Name);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetMeta(
        lua_State state,
        string name,
        int numValues,
        int minValue,
        int maxValue)
    {
        lua_newtable(state);
        PushIntegerField(state, "NumValues", numValues);
        PushIntegerField(state, "MinValue", minValue);
        PushIntegerField(state, "MaxValue", maxValue);
        lua_setfield(state, -2, name);
    }

    private static void PushIntegerField(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }
}
