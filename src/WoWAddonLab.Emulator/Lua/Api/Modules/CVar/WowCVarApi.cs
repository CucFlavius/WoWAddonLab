using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCVarApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetCVar",
        "GetCVarBitfield",
        "GetCVarBool",
        "GetCVarDefault",
        "GetCVarInfo",
        "RegisterCVar",
        "ResetTestCVars",
        "SetCVar",
        "SetCVarBitfield"
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
        lua_setglobal(state, "C_CVar");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var cvars = runtime.CVars;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var usage = operation switch
        {
            "GetCVar" => "Usage: local value = C_CVar.GetCVar(name)",
            "GetCVarBitfield" =>
                "Usage: local value = C_CVar.GetCVarBitfield(name, index)",
            "GetCVarBool" => "Usage: local value = C_CVar.GetCVarBool(name)",
            "GetCVarDefault" =>
                "Usage: local defaultValue = C_CVar.GetCVarDefault(name)",
            "GetCVarInfo" =>
                "Usage: local value, defaultValue, isStoredServerAccount, " +
                "isStoredServerCharacter, isLockedFromUser, isSecure, " +
                "isReadOnly = C_CVar.GetCVarInfo(name)",
            "RegisterCVar" => "Usage: C_CVar.RegisterCVar(name [, value])",
            "SetCVar" =>
                "Usage: local success = C_CVar.SetCVar(name [, value])",
            "SetCVarBitfield" =>
                "Usage: local success = " +
                "C_CVar.SetCVarBitfield(name, index, value)",
            _ => string.Empty
        };
        string? name = null;
        if (operation != "ResetTestCVars" &&
            !TryReadRequiredString(state, 1, out name))
        {
            return luaL_error(state, usage);
        }
        name ??= string.Empty;

        switch (operation)
        {
            case "GetCVar":
                PushOptionalString(state, cvars.TryGet(name, out var value) ? value.Value : null);
                return 1;
            case "GetCVarDefault":
                if (!cvars.TryGet(name, out var defaultValue))
                    return 0;
                lua_pushstring(state, defaultValue.DefaultValue);
                return 1;
            case "GetCVarBool":
                if (!cvars.TryGet(name, out var booleanValue))
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_pushboolean(state, IsTrue(booleanValue.Value) ? 1 : 0);
                return 1;
            case "GetCVarBitfield":
            {
                if (!TryReadRequiredOneBasedIndex(
                        state,
                        2,
                        out var zeroBasedIndex))
                {
                    return luaL_error(state, usage);
                }
                if (!cvars.TryGet(name, out var bitfield))
                {
                    lua_pushnil(state);
                    return 1;
                }

                lua_pushboolean(
                    state,
                    ReadBitfield(bitfield.Value, zeroBasedIndex) ? 1 : 0);
                return 1;
            }
            case "GetCVarInfo":
                if (!cvars.TryGet(name, out var info))
                    return 0;
                lua_pushstring(state, info.Value);
                lua_pushstring(state, info.DefaultValue);
                lua_pushboolean(state, info.IsStoredServerAccount ? 1 : 0);
                lua_pushboolean(state, info.IsStoredServerCharacter ? 1 : 0);
                lua_pushboolean(state, info.IsLockedFromUser ? 1 : 0);
                lua_pushboolean(state, info.IsSecure ? 1 : 0);
                lua_pushboolean(state, info.IsReadOnly ? 1 : 0);
                return 7;
            case "RegisterCVar":
                if (!TryReadOptionalString(state, 2, out var registeredDefault))
                    return luaL_error(state, usage);
                cvars.Register(name, registeredDefault);
                return 0;
            case "ResetTestCVars":
                return 0;
            case "SetCVar":
            {
                if (!TryReadOptionalString(state, 2, out var newValue))
                    return luaL_error(state, usage);
                var success = TrySet(cvars, name, newValue ?? "0");
                if (!success)
                    return 0;
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "SetCVarBitfield":
            {
                if (!TryReadRequiredOneBasedIndex(
                        state,
                        2,
                        out var zeroBasedIndex) ||
                    lua_isnoneornil(state, 3) != 0)
                {
                    return luaL_error(state, usage);
                }
                if (zeroBasedIndex / 6 + 1 >= byte.MaxValue)
                {
                    return luaL_error(
                        state,
                        "Predicate check failed: IsIndexInRange");
                }
                if (!cvars.TryGet(name, out var bitfield) || !CanSet(bitfield))
                {
                    return 0;
                }

                bitfield.Value = WriteBitfield(
                    bitfield.Value,
                    zeroBasedIndex,
                    lua_toboolean(state, 3) != 0);
                lua_pushboolean(state, 1);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static bool TrySet(WowCVarState cvars, string name, string? value)
    {
        if (!cvars.TryGet(name, out var entry) || !CanSet(entry))
            return false;
        cvars.SetValue(name, value ?? string.Empty);
        return true;
    }

    private static bool CanSet(WowCVarEntry entry) =>
        !entry.IsReadOnly && !entry.IsSecure && !entry.IsLockedFromUser;

    private static bool TryReadRequiredString(
        lua_State state,
        int index,
        out string? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
            return false;
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
        return TryReadRequiredString(state, index, out value);
    }

    private static bool TryReadRequiredOneBasedIndex(
        lua_State state,
        int index,
        out uint zeroBasedIndex)
    {
        zeroBasedIndex = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < 0 or > uint.MaxValue)
            return false;
        zeroBasedIndex = unchecked((uint)number - 1);
        return true;
    }

    private static bool ReadBitfield(string value, uint zeroBasedIndex)
    {
        var characterIndex = zeroBasedIndex / 6 + 1;
        return characterIndex < value.Length &&
               (value[(int)characterIndex] & (1 << (int)(zeroBasedIndex % 6))) != 0;
    }

    private static string WriteBitfield(
        string value,
        uint zeroBasedIndex,
        bool enabled)
    {
        var characters = value.ToCharArray().ToList();
        if (characters.Count == 0)
            characters.Add('\x02');
        else
            characters[0] = '\x02';

        var characterIndex = checked((int)(zeroBasedIndex / 6 + 1));
        if (enabled)
        {
            while (characters.Count <= characterIndex)
                characters.Add('@');
            characters[characterIndex] =
                (char)(characters[characterIndex] |
                       (1 << (int)(zeroBasedIndex % 6)));
        }
        else if (characterIndex < characters.Count)
        {
            characters[characterIndex] =
                (char)(characters[characterIndex] &
                       ~(1 << (int)(zeroBasedIndex % 6)));
            while (characters.Count > 1 && characters[^1] == '@')
                characters.RemoveAt(characters.Count - 1);
        }
        return new string([.. characters]);
    }

    private static bool IsTrue(string value) =>
        value.Length > 0 &&
        value[0] switch
        {
            '0' or 'F' or 'N' or 'f' or 'n' => false,
            >= '1' and <= '9' or 'T' or 'Y' or 't' or 'y' => true,
            _ => value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                 value.Equals("enabled", StringComparison.OrdinalIgnoreCase)
        };

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }
}
