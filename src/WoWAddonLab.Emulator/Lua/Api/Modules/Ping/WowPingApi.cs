using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPingApi : LuaApiModule
{
    private const byte DefaultContextualPingType = 5;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetContextualPingTypeForUnit",
        "GetCooldownInfo",
        "GetDefaultPingOptions",
        "GetTextureKitForType",
        "IsPingSystemEnabled",
        "SendMacroPing",
        "TogglePingListener"
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
        lua_setglobal(state, "C_Ping");
    }

    internal static void RegisterEnums(lua_State state)
    {
        SetEnum(
            state,
            "PingSubjectType",
            "Attack",
            "Warning",
            "Assist",
            "OnMyWay",
            "AlertThreat",
            "AlertNotThreat");
        SetEnum(
            state,
            "PingResult",
            "Success",
            "FailedGeneric",
            "FailedSpamming",
            "FailedDisabledByLeader",
            "FailedDisabledBySettings",
            "FailedOutOfPingArea",
            "FailedSquelched",
            "FailedUnspecified");
        SetEnum(state, "PingMode", "KeyDown", "ClickDrag");
        SetFlagsEnum(
            state,
            "PingTypeFlags",
            [("DefaultPing", 1)]);
    }

    private static int Dispatch(lua_State state)
    {
        var ping = LuaBindings.GetRuntime(state).Ping;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetContextualPingTypeForUnit":
            {
                var target = OptionalString(
                    state,
                    1,
                    Usage(operation, "[targetUnit]"));
                var type = target is not null &&
                    ping.ContextualTypeByTarget.TryGetValue(
                        target,
                        out var contextualType)
                    ? contextualType
                    : DefaultContextualPingType;
                lua_pushnumber(state, type);
                return 1;
            }
            case "GetCooldownInfo":
                lua_createtable(state, 0, 2);
                SetNumber(
                    state,
                    "startTimeMs",
                    ping.CooldownInfo.StartTimeMs);
                SetNumber(
                    state,
                    "endTimeMs",
                    ping.CooldownInfo.EndTimeMs);
                return 1;
            case "GetDefaultPingOptions":
                lua_createtable(state, ping.DefaultOptions.Count, 0);
                for (var index = 0; index < ping.DefaultOptions.Count; index++)
                {
                    var option = ping.DefaultOptions[index];
                    lua_createtable(state, 0, 3);
                    SetNumber(state, "orderIndex", option.OrderIndex);
                    SetNumber(state, "type", option.Type);
                    SetOptionalString(
                        state,
                        "uiTextureKitID",
                        option.UiTextureKitId);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetTextureKitForType":
            {
                var type = RequiredPingType(
                    state,
                    1,
                    Usage(operation, "type"));
                ping.TextureKitByType.TryGetValue(type, out var textureKit);
                if (textureKit is null)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, textureKit);
                return 1;
            }
            case "IsPingSystemEnabled":
                lua_pushboolean(state, 1);
                return 1;
            case "SendMacroPing":
            {
                var type = OptionalPingType(
                    state,
                    1,
                    Usage(operation, "[type, targetToken]"));
                var target = OptionalString(
                    state,
                    2,
                    Usage(operation, "[type, targetToken]"));
                Record(ping, operation, type, target);
                return 0;
            }
            case "TogglePingListener":
            {
                var down = RequiredBoolean(
                    state,
                    1,
                    Usage(operation, "down"));
                ping.IsListenerDown = down;
                Record(ping, operation, down);
                return 0;
            }
            default:
                return 0;
        }
    }

    private static byte RequiredPingType(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((byte)luaL_error(state, usage));
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
            return unchecked((byte)luaL_error(state, usage));
        var value = unchecked((byte)(int)number);
        if (value > 5)
            return unchecked((byte)luaL_error(state, usage));
        return value;
    }

    private static byte? OptionalPingType(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredPingType(state, index, usage);
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return null;
        }
        return lua_tostring(state, index);
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) == LUA_TNONE)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static void Record(
        WowPingState ping,
        string operation,
        params object?[] arguments) =>
        ping.Requests.Add(new WowPingRequestState(operation, arguments));

    private static string Usage(string operation, string arguments) =>
        $"Usage: C_Ping.{operation}({arguments})";

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
        params string[] members)
    {
        lua_createtable(state, 0, members.Length);
        for (var value = 0; value < members.Length; value++)
            SetNumber(state, members[value], value);
        lua_setfield(state, -2, name);

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", members.Length);
        SetNumber(state, "MinValue", 0);
        SetNumber(state, "MaxValue", members.Length - 1);
        lua_setfield(state, -2, $"{name}Meta");
    }

    private static void SetFlagsEnum(
        lua_State state,
        string name,
        IReadOnlyList<(string Name, double Value)> members)
    {
        lua_createtable(state, 0, members.Count);
        foreach (var member in members)
            SetNumber(state, member.Name, member.Value);
        lua_setfield(state, -2, name);

        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", members.Count);
        SetNumber(state, "MinValue", members.Min(member => member.Value));
        SetNumber(state, "MaxValue", members.Max(member => member.Value));
        lua_setfield(state, -2, $"{name}Meta");
    }
}
