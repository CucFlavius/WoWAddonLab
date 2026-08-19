using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCursorApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ClearCursor",
        "ClearCursorHoveredItem",
        "CursorHasItem",
        "CursorHasMacro",
        "CursorHasMoney",
        "CursorHasSpell",
        "DeleteCursorItem",
        "DropCursorMoney",
        "EquipCursorItem",
        "GetCursorInfo",
        "GetCursorMoney",
        "PickupPlayerMoney",
        "ResetCursor",
        "SellCursorItem",
        "SetCursor",
        "SetCursorByMode",
        "SetCursorHoveredItem",
        "SetCursorHoveredItemTradeItem",
        "SetCursorVirtualItem"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        lua_newtable(state);
        lua_pushstring(state, "GetCursorItem");
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, "GetCursorItem");
        lua_setglobal(state, "C_Cursor");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var cursor = runtime.Cursor;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetCursorItem":
                if (cursor.GetItemLocation() is not { } location)
                    return 0;
                WowItemApi.PushItemLocation(state, location);
                return 1;
            case "GetCursorMoney":
                lua_pushnumber(state, cursor.Money);
                return 1;
            case "CursorHasMoney":
                return PushHasPayload(state, cursor, WowCursorPayloadKind.Money);
            case "CursorHasItem":
                return PushHasPayload(state, cursor, WowCursorPayloadKind.Item);
            case "CursorHasMacro":
                return PushHasPayload(state, cursor, WowCursorPayloadKind.Macro);
            case "CursorHasSpell":
                return PushHasPayload(state, cursor, WowCursorPayloadKind.Spell);
            case "SetCursor":
                return SetCursor(state, runtime);
            case "SetCursorByMode":
                return SetCursorByMode(state, runtime);
            case "GetCursorInfo":
                return PushCursorInfo(state, cursor);
            case "PickupPlayerMoney":
                return PickupPlayerMoney(state, runtime);
            case "ClearCursor":
                cursor.ClearPayload();
                return 0;
            case "DropCursorMoney":
                if (cursor.Payload?.Kind == WowCursorPayloadKind.Money &&
                    cursor.Money > 0)
                {
                    cursor.ClearPayload();
                }
                return 0;
            case "ResetCursor":
                cursor.HardwareMode = null;
                runtime.Ui.CursorAsset = null;
                return 0;
            case "ClearCursorHoveredItem":
                cursor.HoveredItem = null;
                return 0;
            case "SetCursorHoveredItemTradeItem":
                if (lua_type(state, 1) != LUA_TBOOLEAN)
                    return luaL_error(
                        state,
                        "Usage: SetCursorHoveredItemTradeItem(isTradeItem)");
                cursor.HoveredItemIsTradeItem = lua_toboolean(state, 1) != 0;
                return 0;
            case "SetCursorHoveredItem":
                if (lua_gettop(state) < 1 || lua_isnil(state, 1) != 0)
                    return luaL_error(state, "Usage: SetCursorHoveredItem(itemLocation)");
                cursor.HoveredItem = new object();
                return 0;
            case "EquipCursorItem":
                if (lua_type(state, 1) != LUA_TNUMBER ||
                    !double.IsFinite(lua_tonumber(state, 1)) ||
                    lua_tonumber(state, 1) < 1)
                {
                    return luaL_error(state, "Usage: EquipCursorItem(index)");
                }
                return 0;
            case "DeleteCursorItem":
            case "SellCursorItem":
            case "SetCursorVirtualItem":
                return 0;
            default:
                return 0;
        }
    }

    private static int SetCursor(lua_State state, LuaRuntime runtime)
    {
        var type = lua_type(state, 1);
        if (type is LUA_TNONE or LUA_TNIL)
        {
            runtime.Cursor.HardwareMode = null;
            runtime.Ui.CursorAsset = null;
            lua_pushboolean(state, 1);
            return 1;
        }

        if (type != LUA_TSTRING)
            return luaL_error(state, "Usage: local result = SetCursor([name])");

        var asset = lua_tostring(state, 1);
        var alreadyActive = runtime.Cursor.HardwareMode is null &&
                            string.Equals(
                                runtime.Ui.CursorAsset,
                                asset,
                                StringComparison.OrdinalIgnoreCase);
        runtime.Cursor.HardwareMode = null;
        runtime.Ui.CursorAsset = asset;
        lua_pushboolean(state, alreadyActive ? 1 : 0);
        return 1;
    }

    private static int SetCursorByMode(lua_State state, LuaRuntime runtime)
    {
        if (lua_type(state, 1) != LUA_TNUMBER)
            return luaL_error(state, "Usage: local result = SetCursorByMode(mode)");

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) ||
            value != Math.Truncate(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, "Usage: local result = SetCursorByMode(mode)");
        }

        if (value is < 0 or > 91)
            return luaL_error(state, "Usage: local result = SetCursorByMode(mode)");

        var mode = (int)value;
        var accepted = mode is > 0 and < 91;
        if (accepted)
        {
            runtime.Cursor.HardwareMode = mode;
            runtime.Ui.CursorAsset = null;
        }
        lua_pushboolean(state, accepted ? 1 : 0);
        return 1;
    }

    private static int PickupPlayerMoney(lua_State state, LuaRuntime runtime)
    {
        const string usage = "Usage: PickupPlayerMoney(amount)";
        if (lua_type(state, 1) != LUA_TNUMBER)
            return luaL_error(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < 0 || value > ulong.MaxValue)
            return luaL_error(state, usage);

        var requested = (ulong)value;
        var available = (ulong)Math.Max(0, runtime.Client.Money);
        if (requested > 0 &&
            requested <= available &&
            requested < 100_000_000_000UL)
        {
            runtime.Cursor.SetMoney(requested);
        }
        return 0;
    }

    private static int PushHasPayload(
        lua_State state,
        WowCursorState cursor,
        WowCursorPayloadKind kind)
    {
        lua_pushboolean(state, cursor.Payload?.Kind == kind ? 1 : 0);
        return 1;
    }

    private static int PushCursorInfo(lua_State state, WowCursorState cursor)
    {
        var payload = cursor.Payload;
        if (payload is null)
            return 0;

        foreach (var value in payload.InfoValues)
            PushScalar(state, value);
        return payload.InfoValues.Count;
    }

    private static void PushScalar(lua_State state, object? value)
    {
        switch (value)
        {
            case null:
                lua_pushnil(state);
                break;
            case string text:
                lua_pushstring(state, text);
                break;
            case bool flag:
                lua_pushboolean(state, flag ? 1 : 0);
                break;
            case byte or sbyte or short or ushort or int or uint or long or ulong or
                float or double or decimal:
                lua_pushnumber(state, Convert.ToDouble(value));
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported cursor-info scalar type {value.GetType().FullName}.");
        }
    }
}
