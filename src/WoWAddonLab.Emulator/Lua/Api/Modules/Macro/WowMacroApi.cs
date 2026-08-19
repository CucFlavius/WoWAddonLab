using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMacroApi : LuaApiModule
{
    private const uint QuestionMarkIconFileDataId = 134400;
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly lua_CFunction NamespaceCallback = DispatchNamespace;

    private static readonly string[] Functions =
    [
        "CreateMacro",
        "DeleteMacro",
        "EditMacro",
        "GetMacroIndexByName",
        "GetMacroBody",
        "GetMacroInfo",
        "GetMacroIcons",
        "GetMacroItem",
        "GetMacroItemIcons",
        "GetMacroSpell",
        "GetLooseMacroIcons",
        "GetLooseMacroItemIcons",
        "GetNumMacros",
        "PickupMacro"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetMacroName", "GetSelectedMacroIcon", "RunMacroText",
                     "SetMacroExecuteLineCallback"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, NamespaceCallback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Macro");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var macros = runtime.Macros;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetNumMacros":
                lua_pushinteger(state, macros.Account.Count);
                lua_pushinteger(state, macros.Character.Count);
                return 2;
            case "GetMacroInfo":
                return PushMacro(state, runtime, ResolveMacro(state, macros));
            case "GetMacroBody":
            {
                var macro = ResolveMacro(state, macros);
                if (macro is null)
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_pushstring(state, macro.Body);
                return 1;
            }
            case "GetLooseMacroIcons":
                return PushIconTable(
                    state,
                    runtime.MacroIconProvider?.LooseSpellIcons ?? [],
                    "Usage: GetLooseMacroIcons([table])");
            case "GetLooseMacroItemIcons":
                return PushIconTable(
                    state,
                    runtime.MacroIconProvider?.LooseItemIcons ?? [],
                    "Usage: GetLooseMacroItemIcons([table])");
            case "GetMacroIcons":
                return PushIconTable(
                    state,
                    runtime.MacroIconProvider?.SpellIcons ?? [],
                    "Usage: GetMacroIcons([table])");
            case "GetMacroItemIcons":
                return PushIconTable(
                    state,
                    runtime.MacroIconProvider?.ItemIcons ?? [],
                    "Usage: GetMacroItemIcons([table])");
            case "GetMacroIndexByName":
            {
                if (lua_isstring(state, 1) == 0)
                    return luaL_error(state, "Usage: GetMacroIndexByName(name)");
                lua_pushinteger(state, macros.FindIndexByName(lua_tostring(state, 1)));
                return 1;
            }
            case "CreateMacro":
            {
                const string usage =
                    "Usage: CreateMacro(name, iconFileName, body, perCharacter)";
                if (lua_isstring(state, 1) == 0 || lua_isstring(state, 2) == 0)
                    return luaL_error(state, usage);

                var name = lua_tostring(state, 1) ?? string.Empty;
                var icon = lua_tostring(state, 2) ?? string.Empty;
                if (name.Length == 0)
                    return luaL_error(state, "CreateMacro() failed, no name specified");
                if (icon.Length == 0)
                    return luaL_error(state, "CreateMacro() failed, no icon specified");

                var index = macros.Create(
                    name,
                    icon,
                    lua_isstring(state, 3) != 0 ? lua_tostring(state, 3) ?? string.Empty : string.Empty,
                    lua_toboolean(state, 4) != 0);
                if (index == 0)
                    return luaL_error(
                        state,
                        $"CreateMacro() failed, already have {WowMacroState.MaximumAccountMacros} macros");

                runtime.TriggerEvent("UPDATE_MACROS");
                lua_pushinteger(state, index);
                return 1;
            }
            case "EditMacro":
            {
                var resolvedIndex = ResolveMacroIndex(state, macros);
                var replaceIcon = lua_isstring(state, 3) != 0;
                var index = macros.Edit(
                    resolvedIndex,
                    lua_isstring(state, 2) != 0 ? lua_tostring(state, 2) : null,
                    replaceIcon ? lua_tostring(state, 3) : null,
                    replaceIcon,
                    lua_isstring(state, 4) != 0 ? lua_tostring(state, 4) : null);
                if (index == 0)
                    lua_pushinteger(state, 0);
                else
                {
                    runtime.TriggerEvent("UPDATE_MACROS");
                    lua_pushinteger(state, index);
                }
                return 1;
            }
            case "DeleteMacro":
                if (macros.Delete(ResolveMacroIndex(state, macros)))
                    runtime.TriggerEvent("UPDATE_MACROS");
                return 0;
            case "PickupMacro":
            {
                var index = ResolveMacroIndex(state, macros);
                if (index != 0)
                    macros.PickedUpMacroIndex = index;
                return 0;
            }
            case "GetMacroItem":
            {
                var macro = ResolveMacro(state, macros);
                if (macro?.ItemName is null || macro.ItemLink is null)
                    return 0;
                lua_pushstring(state, macro.ItemName);
                lua_pushstring(state, macro.ItemLink);
                return 2;
            }
            case "GetMacroSpell":
            {
                var macro = ResolveMacro(state, macros);
                if (macro?.SpellId is not int spellId)
                    return 0;
                lua_pushinteger(state, spellId);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static int DispatchNamespace(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetMacroName":
            {
                if (lua_isnumber(state, 1) == 0 || lua_tonumber(state, 1) < 1)
                    return luaL_error(
                        state,
                        "Usage: local name = C_Macro.GetMacroName(macroId)");
                var macro = runtime.Macros.Find((int)lua_tonumber(state, 1));
                if (macro is null)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, macro.Name);
                return 1;
            }
            case "GetSelectedMacroIcon":
            {
                if (lua_isnumber(state, 1) == 0 || lua_tonumber(state, 1) < 1)
                    return luaL_error(
                        state,
                        "Usage: local textureNum = C_Macro.GetSelectedMacroIcon(macroId)");
                var macro = runtime.Macros.Find((int)lua_tonumber(state, 1));
                lua_pushnumber(state, ResolveSelectedIcon(runtime, macro?.Icon));
                return 1;
            }
            case "SetMacroExecuteLineCallback":
                if (lua_type(state, 1) != LUA_TNONE &&
                    lua_type(state, 1) != LUA_TNIL &&
                    lua_isfunction(state, 1) == 0)
                    return luaL_error(
                        state,
                        "Usage: C_Macro.SetMacroExecuteLineCallback(cb)");
                runtime.ReleaseReference(runtime.Macros.ExecuteLineCallbackReference);
                runtime.Macros.ExecuteLineCallbackReference = lua_isfunction(state, 1) != 0
                    ? runtime.CaptureFunction(state, 1)
                    : 0;
                return 0;
            case "RunMacroText":
            {
                const string usage = "Usage: C_Macro.RunMacroText(text, button)";
                if (lua_isstring(state, 1) == 0 || lua_isstring(state, 2) == 0)
                    return luaL_error(state, usage);
                var text = lua_tostring(state, 1);
                if (text is null || runtime.Macros.ExecuteLineCallbackReference <= 0)
                    return 0;
                foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
                {
                    if (line.Length > 0)
                    {
                        runtime.InvokeReference(
                            runtime.Macros.ExecuteLineCallbackReference,
                            null,
                            line);
                    }
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static uint ResolveSelectedIcon(LuaRuntime runtime, object? icon)
    {
        switch (icon)
        {
            case double number when number > 0 && number <= uint.MaxValue:
                return (uint)number;
            case string number when uint.TryParse(number, out var fileDataId) &&
                                    fileDataId > 0:
                return fileDataId;
            case string path:
                return runtime.MacroIconProvider?.ResolveFileDataId(path) ??
                       QuestionMarkIconFileDataId;
            default:
                return QuestionMarkIconFileDataId;
        }
    }

    private static uint? ResolveInfoIcon(LuaRuntime runtime, object? icon)
    {
        switch (icon)
        {
            case double number when number > 0 && number <= uint.MaxValue:
                return (uint)number;
            case string number when uint.TryParse(number, out var fileDataId) &&
                                    fileDataId > 0:
                return fileDataId;
            case string path:
                return runtime.MacroIconProvider?.ResolveFileDataId(path);
            default:
                return null;
        }
    }

    private static int PushMacro(lua_State state, LuaRuntime runtime, WowMacroInfo? macro)
    {
        if (macro is null)
            return 0;
        lua_pushstring(state, macro.Name);
        var icon = ResolveInfoIcon(runtime, macro.Icon);
        if (icon is uint fileDataId)
            lua_pushnumber(state, fileDataId);
        else
            lua_pushnil(state);
        lua_pushstring(state, macro.Body);
        return 3;
    }

    private static int PushIconTable<T>(
        lua_State state,
        IReadOnlyList<T> icons,
        string usage)
    {
        var argumentType = lua_type(state, 1);
        if (argumentType != LUA_TNONE && argumentType != LUA_TNIL &&
            argumentType != LUA_TTABLE)
            return luaL_error(state, usage);

        if (argumentType == LUA_TTABLE)
            lua_pushvalue(state, 1);
        else
            lua_newtable(state);

        var tableIndex = lua_gettop(state);
        var index = (int)lua_objlen(state, tableIndex) + 1;
        foreach (var icon in icons)
        {
            switch (icon)
            {
                case string name:
                    lua_pushstring(state, name);
                    break;
                case uint fileDataId:
                    lua_pushnumber(state, fileDataId);
                    break;
                default:
                    lua_pushnil(state);
                    break;
            }
            lua_rawseti(state, tableIndex, index++);
        }
        return 1;
    }

    private static WowMacroInfo? ResolveMacro(lua_State state, WowMacroState macros)
    {
        var index = ResolveMacroIndex(state, macros);
        return index == 0 ? null : macros.Find(index);
    }

    private static int ResolveMacroIndex(lua_State state, WowMacroState macros)
    {
        if (lua_isnumber(state, 1) != 0)
        {
            var index = (int)lua_tonumber(state, 1);
            return macros.Find(index) is null ? 0 : index;
        }
        return lua_isstring(state, 1) != 0
            ? macros.FindIndexByName(lua_tostring(state, 1))
            : 0;
    }
}
