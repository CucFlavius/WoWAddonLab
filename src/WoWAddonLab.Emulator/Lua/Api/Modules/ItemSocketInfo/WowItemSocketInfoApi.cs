using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowItemSocketInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "AcceptSockets", "ClickSocketButton", "CloseSocketInfo",
        "CompleteSocketing", "GetCurrUIType", "GetExistingSocketInfo",
        "GetExistingSocketLink", "GetNewSocketInfo", "GetNewSocketLink",
        "GetNumSockets", "GetSocketItemBoundTradeable", "GetSocketItemInfo",
        "GetSocketItemRefundable", "GetSocketTypes",
        "HasBoundGemProposed", "IsArtifactRelicItem"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ItemSocketInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var socketInfo = runtime.ItemSocketInfo;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AcceptSockets":
                socketInfo.AcceptSocketsRequests++;
                return 0;
            case "ClickSocketButton":
                socketInfo.ClickSocketButtonRequests++;
                socketInfo.LastClickedSocketIndex = RequiredZeroBasedIndex(
                    state,
                    "Usage: C_ItemSocketInfo.ClickSocketButton(index)");
                return 0;
            case "CloseSocketInfo":
                socketInfo.CloseSocketInfoRequests++;
                socketInfo.IsOpen = false;
                return 0;
            case "CompleteSocketing":
                socketInfo.CompleteSocketingRequests++;
                return 0;
            case "GetCurrUIType":
                lua_pushnumber(state, socketInfo.CurrentUiType);
                return 1;
            case "GetExistingSocketInfo":
            {
                var socket = GetSocket(
                    state,
                    socketInfo,
                    "Usage: local name, icon, gemMatchesSocket = " +
                    "C_ItemSocketInfo.GetExistingSocketInfo(index)");
                PushOptionalString(state, socket?.ExistingName);
                PushOptionalInteger(state, socket?.ExistingIconFileDataId);
                lua_pushboolean(
                    state,
                    socket?.ExistingGemMatchesSocket == true ? 1 : 0);
                return 3;
            }
            case "GetExistingSocketLink":
            {
                var socket = GetSocket(
                    state,
                    socketInfo,
                    "Usage: local existingSocketLink = " +
                    "C_ItemSocketInfo.GetExistingSocketLink(index)");
                PushOptionalString(state, socket?.ExistingLink);
                return 1;
            }
            case "GetNewSocketInfo":
            {
                var socket = GetSocket(
                    state,
                    socketInfo,
                    "Usage: local name, icon, gemMatchesSocket = " +
                    "C_ItemSocketInfo.GetNewSocketInfo(index)");
                PushOptionalString(state, socket?.NewName);
                PushOptionalInteger(state, socket?.NewIconFileDataId);
                lua_pushboolean(
                    state,
                    socket?.NewGemMatchesSocket == true ? 1 : 0);
                return 3;
            }
            case "GetNewSocketLink":
            {
                var socket = GetSocket(
                    state,
                    socketInfo,
                    "Usage: local newSocketLink = " +
                    "C_ItemSocketInfo.GetNewSocketLink(index)");
                PushOptionalString(state, socket?.NewLink);
                return 1;
            }
            case "GetNumSockets":
                lua_pushnumber(state, socketInfo.Sockets.Count);
                return 1;
            case "GetSocketItemBoundTradeable":
                lua_pushboolean(
                    state,
                    socketInfo.SocketItemBoundTradeable ? 1 : 0);
                return 1;
            case "GetSocketItemInfo":
                PushOptionalString(state, socketInfo.SocketItemName);
                PushOptionalInteger(
                    state,
                    socketInfo.SocketItemIconFileDataId);
                lua_pushnumber(state, socketInfo.SocketItemQuality);
                return 3;
            case "GetSocketItemRefundable":
                lua_pushboolean(
                    state,
                    socketInfo.SocketItemRefundable ? 1 : 0);
                return 1;
            case "GetSocketTypes":
            {
                var socket = GetSocket(
                    state,
                    socketInfo,
                    "Usage: local socketType = " +
                    "C_ItemSocketInfo.GetSocketTypes(index)");
                PushOptionalString(state, socket?.SocketType);
                return 1;
            }
            case "HasBoundGemProposed":
                lua_pushboolean(
                    state,
                    socketInfo.HasBoundGemProposed ? 1 : 0);
                return 1;
            case "IsArtifactRelicItem":
            {
                var itemId = RequiredItemId(
                    state,
                    runtime.Items,
                    "Usage: local isArtifactRelicItem = " +
                    "C_ItemSocketInfo.IsArtifactRelicItem(info)");
                lua_pushboolean(
                    state,
                    itemId is { } id &&
                    socketInfo.ArtifactRelicItemIds.Contains(id)
                        ? 1
                        : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static WowItemSocketState? GetSocket(
        lua_State state,
        WowItemSocketInfoState socketInfo,
        string usage)
    {
        var index = RequiredZeroBasedIndex(state, usage);
        return index < socketInfo.Sockets.Count
            ? socketInfo.Sockets[(int)index]
            : null;
    }

    private static uint RequiredZeroBasedIndex(
        lua_State state,
        string usage)
    {
        if (lua_gettop(state) < 1 || lua_isnumber(state, 1) == 0)
            return RaiseIndexError(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > uint.MaxValue)
        {
            return RaiseIndexError(state, usage);
        }

        return unchecked((uint)(long)Math.Truncate(value - 1.0));
    }

    private static uint RaiseIndexError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static int? RequiredItemId(
        lua_State state,
        WowItemState items,
        string usage)
    {
        if (lua_gettop(state) < 1)
            return RaiseItemError(state, usage);

        if (lua_isnumber(state, 1) != 0)
        {
            var value = lua_tonumber(state, 1);
            if (!double.IsFinite(value) ||
                value is < int.MinValue or > int.MaxValue)
            {
                return RaiseItemError(state, usage);
            }
            return (int)value;
        }

        if (lua_type(state, 1) != LUA_TSTRING)
            return RaiseItemError(state, usage);

        var text = lua_tostring(state, 1) ?? string.Empty;
        if (TryParseItemId(text, out var itemId))
            return itemId;

        return items.Items.Values.FirstOrDefault(item =>
            string.Equals(item.Name, text, StringComparison.Ordinal) ||
            string.Equals(item.Link, text, StringComparison.Ordinal))?.ItemId;
    }

    private static int? RaiseItemError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return null;
    }

    private static bool TryParseItemId(string text, out int itemId)
    {
        if (int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out itemId))
        {
            return true;
        }

        var marker = text.IndexOf(
            "item:",
            StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
            return false;
        marker += "item:".Length;
        var end = marker;
        while (end < text.Length && char.IsAsciiDigit(text[end]))
            end++;
        return end > marker &&
            int.TryParse(
                text.AsSpan(marker, end - marker),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out itemId);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void PushOptionalInteger(lua_State state, uint? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 1);
        SetInteger(state, "Default", 0);
        lua_setfield(state, -2, "ItemSocketInfoUIType");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 1);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 0);
        lua_setfield(state, -2, "ItemSocketInfoUITypeMeta");
        lua_pop(state, 1);
    }

    private static void SetInteger(
        lua_State state,
        string field,
        int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }
}
